# Code Conversion Guidelines

## Overview
This document provides code conversion guidelines. When refactoring or adding new code, follow these patterns for consistency.

## Naming Conventions

### Classes
- Use PascalCase: `MeltingProcessor`, `HotSpringTracker`
- End tracker classes with `Tracker`: `HotSpringTracker`, `SnowBlockTracker`
- End processor classes with `Processor`: `MeltingProcessor`
- Sub-systems that are not mod systems should be plain classes (no `ModSystem` inheritance unless registered with the mod loader)

### Methods
- Use PascalCase: `TickMeltTrackedSnow()`, `ScanForHotSpringsNearPlayers()`
- Use descriptive names that explain what the method does
- Prefix boolean methods with `Is` or `Has`: `IsHotBacteria()`, `IsMeltableBlock()`
- Prefix methods that may not succeed with `Try`: `TryMeltBlock()`, `TryTrackNewSnowBlock()`
- Use `Handle` prefix for event handlers: `HandleBlockPlaced()`, `HandleBlockBroken()`
- Use `Tick` prefix for methods called on a game tick: `TickMeltTrackedSnow()`

### Constants
- Use PascalCase: `SnowBlock`, `SnowLayerPrefix`, `MaxMeltChance`
- Use descriptive names that explain the value
- Prefer named constants over magic numbers (see Magic Numbers section)

## Code Style

### Magic Numbers
Never use raw numeric or string literals inline. Define them as named constants at the class level:

```csharp
// ✅ Good
private const int ScanRadiusBlocks = 100;
private const int ScanDepthBlocks = 10;
private const int MeltTickIntervalMs = 1000;
private const double MaxMeltChance = 0.9;

api.Event.RegisterGameTickListener(_ => TickMeltTrackedSnow(), MeltTickIntervalMs, MeltTickInitialDelayMs);

// ❌ Avoid
api.Event.RegisterGameTickListener(_ => TickMeltTrackedSnow(), 1000, 100);
```

### Method Size and Splitting
Keep methods focused on a single responsibility. If a method has:
- More than 3 levels of nesting, extract the inner logic into a helper
- Multiple distinct phases (iterate players → scan blocks → register results), split into one method per phase
- Both "check if exists" and "create new" paths, split into `TryUpdate...` and `TryCreate...`

```csharp
// ✅ Good — each method does one thing
private void ScanForHotSpringsNearPlayers()
{
    foreach (var player in serverApi.Server.Players)
        ScanAroundPlayer(player, blockAccessor);
}

private void ScanAroundPlayer(IServerPlayer player, IBlockAccessor blockAccessor) { ... }
private void CheckBlockForHotSpring(IBlockAccessor blockAccessor, BlockPos position) { ... }

// ❌ Avoid — one method doing iteration, coordinate math, block checks, and registration
private void ScanNearPlayers() { /* 60+ lines with 4 levels of nesting */ }
```

### Avoid Duplicate Logic
If the same data or calculation exists in two places, expose it from the authoritative source:

```csharp
// ✅ Good — single source of truth for radius
public static int GetRadiusForTemperature(int temperature)
{
    if (TemperatureMapping.TryGetValue(temperature, out var mapping))
        return mapping.Radius;
    return 1;
}

// ❌ Avoid — duplicating the mapping in a switch expression elsewhere
```

### Unused `using` Directives
Remove all unused `using` directives. Every `using` statement at the top of a file must be referenced by the code in that file. Do not leave unused imports "for convenience" or "for future use."

```csharp
// ✅ Good — only usings that are actually referenced
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;

// ❌ Avoid — usings not referenced by any code in the file
using System.Linq;
using System.Collections.Concurrent;
using Vintagestory.API.Client;
```

### Dead Code
Remove unused fields, properties, and parameters immediately. If a field is always set to the same value and never read, it's dead code. If a class member exists "for future use," remove it until it's actually needed.

### Comment Punctuation
All comment sentences must end with a full stop. This applies to XML doc comments, inline comments, and summary tags:

```csharp
// ✅ Good
/// <summary>
/// Calculates the melting chance for a snow block based on distance.
/// Uses linear falloff from the base chance at distance 0 to zero at the effective radius.
/// </summary>

// Blocks directly on the bacteria get the maximum possible chance.
if (distance == 0)
    return MaxMeltingChance;

// ❌ Avoid
/// <summary>
/// Calculates the melting chance for a snow block based on distance
/// </summary>

// Blocks directly on the bacteria get the maximum possible chance
```

### Inline Comments for Complex Logic
Add inline comments to explain non-obvious logic, especially:
- Why a particular approach was chosen (not just what it does).
- Edge cases and fallback behaviour.
- Relationships between constants in different classes.
- Anything a reader might need to look up to understand.

```csharp
// ✅ Good — explains the "why"
// Cannot modify the collection during enumeration, so collect melted positions.
List<BlockPos>? toUntrack = null;

// Include the melting extension in the scan radius so all blocks
// that could have a non-zero melting chance are discovered.
int springRadius = HotSpringTracker.GetRadiusForTemperature(temperature) + MeltingRadiusExtensionBlocks;

// ❌ Avoid — restates the code without adding insight
// Add to list.
toUntrack.Add(snowData.Position);
```

### Method Comments
Always include XML documentation comments for public methods with `<param>` and `<returns>` elements:

```csharp
/// <summary>
/// Gets the temperature of a hot bacteria block
/// </summary>
/// <param name="block">The block to check</param>
/// <returns>The temperature (55, 65, 74, or 87), or -1 if not a hot bacteria block</returns>
public static int GetTemperature(Block block)
{
    // implementation
}
```

### Constructor Comments
Include `<param>` elements for all constructor parameters:

```csharp
/// <summary>
/// Initializes a new instance of the MeltingProcessor class
/// </summary>
/// <param name="api">The server API</param>
/// <param name="hotSpringTracker">The hot spring tracker</param>
/// <param name="snowBlockTracker">The snow block tracker</param>
public MeltingProcessor(ICoreServerAPI api, HotSpringTracker hotSpringTracker, SnowBlockTracker snowBlockTracker)
{
    this.api = api;
    this.hotSpringTracker = hotSpringTracker;
    this.snowBlockTracker = snowBlockTracker;
}
```

### Property Comments
Include `<summary>` for properties. Add `<value>` if the property description needs more detail:

```csharp
/// <summary>
/// Position of the hot spring bacteria block
/// </summary>
public BlockPos Position { get; set; }
```

### Parameter Validation
Check for null parameters and return appropriate defaults:

```csharp
public static bool IsMeltableBlock(Block? block)
{
    if (block == null)
        return false;
    
    // rest of implementation
}
```

### Return Values
- Use `-1` as a sentinel value for "not found" or "invalid" in numeric methods
- Return empty collections instead of null for list-returning methods
- Expose read-only views (`IReadOnlyCollection<T>`) instead of copying collections when callers only need to iterate

### Target-Typed `new`
When the type is already declared on the left side, use `new()` instead of repeating the type:

```csharp
// ✅ Good
HotSpringTracker tracker = new();
Dictionary<BlockPos, HotSpring> hotSprings = new();
List<BlockPos> toUntrack = new();

// ❌ Avoid
HotSpringTracker tracker = new HotSpringTracker();
Dictionary<BlockPos, HotSpring> hotSprings = new Dictionary<BlockPos, HotSpring>();
List<BlockPos> toUntrack = new List<BlockPos>();
```

Note: Only simplify when the type is explicit on the left. Do not use `new()` with `var` or in ambiguous contexts.

### Block Code Matching
Use precise `block.Code.Path` comparisons instead of broad `Contains()` checks to avoid false positives:

```csharp
// ✅ Good — precise matching
var path = block.Code.Path;
if (path == "snow" || path.StartsWith("snowlayer-"))
    return true;
if (path == "ice" || path == "lakeice" || path == "glacierice")
    return true;

// ❌ Avoid — matches unrelated blocks like "device" or "snowberry"
if (blockCode.Contains("ice")) return true;
if (blockCode.Contains("snow")) return true;
```

Use `block.Code.ToString()` only when you need the full domain-qualified code (e.g., for hot spring bacteria regex matching).

### Regex Usage
For regex patterns used in hot-path code, compile them into a static readonly field:

```csharp
// ✅ Good
private static readonly Regex TemperatureRegex = new(@"-(\d+)deg$", RegexOptions.Compiled);

// ❌ Avoid — allocates a new regex on every call
Regex.Match(blockCode, @"-(\d+)deg$");
```

### Expression-Bodied Members
Use `=>` for one-line methods and properties:

```csharp
// ✅ Good
public int Count => hotSprings.Count;
public static bool IsHotBacteria(Block block) => GetTemperature(block) != -1;

// ❌ Avoid for one-liners
public int Count
{
    get { return hotSprings.Count; }
}
```

### Braces on Control Statements
Omit `{}` for single-line `if`, `else`, `for`, and `foreach` statements — unless the statement is part of an if/else chain where another branch has multiple lines:

```csharp
// ✅ Good — single-line body, no braces
if (block == null)
    return false;

for (int x = minX; x <= maxX; x++)
    CheckBlock(x);

// ✅ Good — multi-line branch forces braces on all branches
if (distance < existingData.ClosestHotSpringDistance)
{
    shouldUpdate = true;
}
else if (distance == existingData.ClosestHotSpringDistance)
{
    HotSpring? data = hotSpringTracker.GetHotSpringData(hotSpringPos.Value);
    if (data != null && data.Temperature > existingData.HotSpringTemperature)
        shouldUpdate = true;
}

// ❌ Avoid — inconsistent braces in the same if/else chain
if (distance < existingData.ClosestHotSpringDistance)
    shouldUpdate = true;
else if (distance == existingData.ClosestHotSpringDistance)
{
    HotSpring? data = hotSpringTracker.GetHotSpringData(hotSpringPos.Value);
    if (data != null && data.Temperature > existingData.HotSpringTemperature)
        shouldUpdate = true;
}
```

### Type Declarations
Always use explicit types. Never use `var`:

```csharp
// ✅ Good
IBlockAccessor blockAccessor = api.World.BlockAccessor;
List<BlockPos> snowBlocks = new();
Dictionary<int, Block>? decors = blockAccessor.GetSubDecors(position);

// ❌ Avoid
var blockAccessor = api.World.BlockAccessor;
var snowBlocks = new List<BlockPos>();
var decors = blockAccessor.GetSubDecors(position);
```

### Collection Exposure
When exposing internal collections for iteration, prefer `IReadOnlyCollection<T>` over allocating a new list:

```csharp
// ✅ Good — no allocation, callers can iterate directly
public IReadOnlyCollection<SnowBlockData> GetTrackedSnowBlocks()
{
    return trackedSnow.Values;
}

// ❌ Avoid — allocates a new list every call
public List<SnowBlockData> GetTrackedSnowBlocks()
{
    return new List<SnowBlockData>(trackedSnow.Values);
}
```

### Block Position Handling
Always use `Vintagestory.API.MathTools.BlockPos` for block operations:

```csharp
var pos = new BlockPos(x, y, z);
var belowPos = new BlockPos(position.X, position.Y - 1, position.Z);
```

### Block Accessor Usage
Cache block accessor when doing multiple operations:

```csharp
var blockAccessor = api.World.BlockAccessor;
// Use blockAccessor for all block operations
```

## Common Patterns

### Block Identification
Use `BlockIdentifiers` class for block checks. Use the specific helper for the check you need:

```csharp
if (BlockIdentifiers.IsHotBacteria(block))
{
    int temperature = BlockIdentifiers.GetTemperature(block);
}

if (BlockIdentifiers.IsMeltableBlock(block))
{
    // implementation
}

if (BlockIdentifiers.IsIceBlock(block))
{
    // ice-specific logic
}
```

### Snow Level Handling
Use `GetSnowLevel()` and `GetSnowLayerBlock()` from `MeltingProcessor`:

```csharp
int currentLevel = GetSnowLevel(block);
Block newBlock = GetSnowLayerBlock(newLevel);
```

### Radius Calculations
Use `HotSpringTracker.GetRadiusForTemperature()` — the single source of truth for temperature-to-radius mapping:

```csharp
int radius = HotSpringTracker.GetRadiusForTemperature(temperature);
```

## File Organization

### BlockIdentifiers.cs
- Static class for block identification utilities
- Contains `IsHotBacteria()`, `IsMeltableBlock()`, `IsIceBlock()`, `GetTemperature()`
- No state, all static methods
- Defines block code constants (`SnowBlock`, `SnowLayerPrefix`, `IceBlock`, etc.)

### Tracker Classes
- Track game state: `HotSpringTracker`, `SnowBlockTracker`
- Provide methods to register, unregister, and retrieve tracked data
- Store data in private collections with read-only public access
- Own their domain-specific calculations (e.g., `HotSpringTracker` owns radius lookup, `SnowBlockTracker` owns melting chance calculation)

### Processor Classes
- Process game logic on a tick basis: `MeltingProcessor`
- Accept dependencies via constructor
- Provide a single public tick method (e.g., `TickMeltTrackedSnow()`)
- Split internal logic into focused private methods (`TryMeltBlock`, `DecrementSnowLayer`, `MeltIceBlock`)

### Sub-System Classes
- Feature sub-systems like `MeltSnowSystem` are plain classes (not `ModSystem`)
- Expose `Initialize(ICoreAPI)` and `InitializeServer(ICoreServerAPI)` methods
- Instantiated and called by the main `ModSystem` class
- Only inherit `ModSystem` if the class needs to be discovered and loaded by the game's mod loader

### ModSystem Classes
- Main mod system class: `HotSpringsExpandedModSystem`
- Inherits from `ModSystem`
- Thin orchestrator — delegates to sub-system classes
- Override `Start()`, `StartServerSide()`, `StartClientSide()`
