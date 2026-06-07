# Design Document

## Overview

This document describes the technical design for the "melt-snow" feature that adds snow melting behavior to hot springs in the Vintage Story mod.

## Components and Interfaces

### MeltSnowSystem
- **Type**: Plain class (not ModSystem)
- **Responsibility**: Feature orchestrator — scanning, event handlers, initialization.
- **Interfaces**: Exposes `Initialize(ICoreAPI)` and `InitializeServer(ICoreServerAPI)` called by the main ModSystem.

### HotSpringTracker
- **Type**: Stateful tracker class
- **Responsibility**: Register, unregister, and query hot spring bacteria blocks. Owns temperature-to-radius mapping.
- **Key Methods**: `RegisterHotSpring(BlockPos, int)`, `UnregisterHotSpring(BlockPos)`, `GetHotSpringData(BlockPos)`, `GetAllHotSprings()`, `GetRadiusForTemperature(int)`.

### SnowBlockTracker
- **Type**: Stateful tracker class
- **Responsibility**: Track snow/ice blocks within range of hot springs. Manages melting chance state.
- **Key Methods**: `TrackSnowBlock(BlockPos, Block, IBlockAccessor, HotSpringTracker)`, `UntrackSnowBlock(BlockPos)`, `GetTrackedSnowBlocks()`.

### MeltingProcessor
- **Type**: Processing class
- **Responsibility**: Per-tick melting logic — iterates tracked snow blocks, rolls melting chance, modifies world blocks.
- **Key Methods**: `TickMeltTrackedSnow()`, `TryMeltBlock(SnowBlockData)`, `DecrementSnowLayer(BlockPos, Block)`, `MeltIceBlock(BlockPos)`.

### BlockIdentifiers
- **Type**: Static utility class
- **Responsibility**: Block identification helpers (hot bacteria detection, snow/ice detection, temperature extraction).
- **Key Methods**: `IsHotBacteria(Block)`, `IsMeltableBlock(Block)`, `IsIceBlock(Block)`, `GetTemperature(Block)`.

## Data Models

### HotSpring
```csharp
public class HotSpring
{
    public BlockPos Position { get; set; }
    public int Temperature { get; set; }  // 55, 65, 74, or 87
    public int Radius { get; set; }       // Calculated from temperature (1-6)
    public double BaseMeltingChance { get; set; }
    public double BaseIceMeltingChance { get; set; }
}
```

### SnowBlockData
```csharp
public class SnowBlockData
{
    public BlockPos Position { get; set; }
    public int ClosestHotSpringDistance { get; set; }
    public int HotSpringTemperature { get; set; }
    public double MeltingChance { get; set; }
}
```

## Architecture

### System Components

```
MeltSnowSystem (ModSystem)
├── HotSpringTracker
│   ├── Tracks hot spring bacterial blocks
│   ├── Stores temperature and position data
│   └── Manages radius/chance mapping
├── SnowBlockTracker
│   ├── Tracks snow/ice blocks in range
│   └── Manages melting state
└── MeltingProcessor
    ├── Processes all hot springs each tick
    └── Applies melting logic
```

## Core Logic

### Temperature to Radius/Chance Mapping

| Temperature | Radius | Base Chance (Snow) | Base Chance (Ice) |
|-------------|--------|-------------------|-------------------|
| 55°C        | 1      | 10%               | 5%                |
| 65°C        | 2      | 15%               | 7.5%              |
| 74°C        | 3      | 20%               | 10%               |
| 87°C        | 6      | 25%               | 12.5%             |

### Melting Logic

1. **Detection Phase** (every tick):
   - Process all hot springs each tick
   - Identify all snow/ice blocks within radius
   - Calculate combined melting chance (capped at 100%)

2. **Melting Phase**:
   - For each tracked snow/ice block:
     - Check if block is on hot bacteria (100% chance) or nearby
     - Roll against melting chance
     - If snow: reduce level by 1 (or remove if level 0)
     - If ice: convert to water block

3. **Exclusion Rules**:
   - Hot bacteria blocks are never melted
   - Blocks outside radius are ignored
   - Multiple springs: chances stack (capped at 100%)

### Block Identification

#### Hot Bacteria Block IDs
- `hotspringbacteriasmooth-55deg`: Low-temperature bacteria (55°C)
- `hotspringbacteriasmooth-65deg`: Medium-temperature bacteria (65°C)
- `hotspringbacteriasmooth-74deg`: High-temperature bacteria (74°C)
- `hotspringbacteria-87deg`: Very high-temperature bacteria (87°C)

#### Snow Block Types
- `snow` (snow block)
- `snowlayer` (snow layers)
- `ice` (ice blocks)

## Event Handlers

### World Generation Events
```csharp
// When hot spring bacteria is generated
OnBlockGenerated(BlockPos pos, Block block)
{
    if (IsHotBacteria(block))
    {
        tracker.RegisterHotSpring(pos, GetTemperature(block));
    }
}
```

### Chunk Events
```csharp
// When chunk is loaded
OnChunkLoaded(Chunk chunk)
{
    chunkManager.StartTracking(chunk);
}

// When chunk is unloaded
OnChunkUnloaded(Chunk chunk)
{
    chunkManager.DropTracking(chunk);
}
```

### Block Events
```csharp
// When block is placed
OnBlockPlaced(BlockPos pos, Block block)
{
    if (IsHotBacteria(block))
    {
        tracker.RegisterHotSpring(pos, GetTemperature(block));
    }
    else if (IsSnowBlock(block))
    {
        tracker.TrackSnowBlock(pos, block);
    }
}

// When block is removed
OnBlockRemoved(BlockPos pos, Block block)
{
    if (IsHotBacteria(block))
    {
        tracker.UnregisterHotSpring(pos);
    }
    else if (IsSnowBlock(block))
    {
        tracker.UntrackSnowBlock(pos);
    }
}
```

## Server-Client Synchronization

### State Synchronization
- Server tracks all hot springs and snow blocks
- Clients predict melting state based on server updates
- No visual effects - only state synchronization

### Network Packets
```csharp
// Server to Client
PacketType.MeltSnowUpdate
{
    BlockPos HotSpringPosition;
    List<BlockPos> AffectedSnowBlocks;
    float MeltingChance;
}
```

## Performance Considerations

### Spatial Partitioning
- Use chunk-based bucketing for hot spring tracking
- Only process one hot spring per tick
- Cache radius calculations per temperature

### Tick Management
- Process all hot springs every tick (50ms)
- No chunk pause state - tracking continues until chunk unloads

### Memory Optimization
- Only track snow blocks within radius of active hot springs
- Remove entries when snow blocks are no longer relevant
- Use struct-based positions for memory efficiency

## Implementation Files

### File Structure
```
HotSpringsExpandedModSystem.cs
├── MeltSnowSystem.cs          (Main mod system)
├── HotSpringTracker.cs        (Hot spring tracking)
├── SnowBlockTracker.cs        (Snow block tracking)
├── MeltingProcessor.cs        (Melting logic)
└── BlockIdentifiers.cs        (Block ID utilities)
```

### Key Methods

#### MeltSnowSystem
- `Start(ICoreAPI)`: Initialize system
- `StartServerSide(ICoreServerAPI)`: Register event handlers and initialize trackers
- `StartClientSide(ICoreClientAPI)`: Initialize client-side system
- `RegisterEventHandlers(ICoreServerAPI)`: Register all event handlers
- `ScanNearPlayers()`: Scan for hot springs near players every 10 seconds
- `GetRadiusForTemperature(int)`: Get melting radius for temperature
- `FindAndTrackSnowNearHotSpring(IBlockAccessor, BlockPos, int)`: Find and track snow blocks
- `OnDidPlaceBlock(...)`: Handle block placement
- `OnDidBreakBlock(...)`: Handle block removal
- `OnSaveGameLoaded()`: Handle world load

#### HotSpringTracker
- `RegisterHotSpring(BlockPos, int)`: Add hot spring
- `UnregisterHotSpring(BlockPos)`: Remove hot spring
- `GetHotSpringData(BlockPos)`: Get hot spring info
- `GetAllHotSprings()`: Get all tracked hot springs
- `Count`: Get number of tracked hot springs

#### SnowBlockTracker
- `TrackSnowBlock(BlockPos, Block, IBlockAccessor, HotSpringTracker)`: Start tracking
- `UntrackSnowBlock(BlockPos)`: Stop tracking
- `GetTrackedSnowBlocks()`: Get all tracked blocks
- `UpdateMeltingChance(BlockPos, float)`: Update chance
- `Count`: Get number of tracked snow blocks

#### MeltingProcessor
- `ProcessAllHotSprings()`: Process all hot springs each tick
- `ProcessHotSpring(BlockPos)`: Process one hot spring
- `FindSnowBlocksInRadius(BlockPos, int)`: Find snow blocks within radius
- `IsBlockOnHotBacteria(BlockPos)`: Check if block is on hot bacteria
- `IsBlockIce(BlockPos)`: Check if block is ice
- `ApplyMelting(BlockPos, float, bool)`: Apply melting to block
- `ReduceSnowLevel(BlockPos, Block)`: Reduce snow level
- `GetSnowLevel(Block)`: Get snow level from block
- `GetSnowLayerBlock(int)`: Get snow layer block by level

## Correctness Properties

Property 1: Combined melting chance from overlapping hot springs must never exceed 100%.
**Validates: Requirements 3.7, 6.8**

Property 2: Hot spring bacteria blocks are never melted or modified by the melting system.
**Validates: Requirements 4.1, 4.2**

Property 3: Snow levels always remain in the valid range 1-8; a level-1 snow layer is removed entirely rather than decremented to 0.
**Validates: Requirements 7.4, 7.6**

Property 4: Ice blocks convert to their corresponding water block (not removed to air).
**Validates: Requirements 7.3**

Property 5: Only blocks within the calculated radius for a given temperature are eligible for melting.
**Validates: Requirements 2.1, 2.3**

Property 6: A block that is removed from the world (broken or melted) is immediately untracked. No stale entries remain in the tracker.
**Validates: Requirements 6.2, 6.3**

Property 7: When a chunk unloads, all tracked blocks in that chunk are dropped. No processing occurs on unloaded chunks.
**Validates: Requirements 6.4**

## Error Handling

- **Null block access**: All block identification methods guard against null Block references and return safe defaults (false or -1).
- **Missing block at position**: Before melting, the processor re-reads the block at the tracked position. If the block is no longer meltable (e.g., already replaced by another mod or player), it is silently untracked.
- **Invalid temperature**: If a bacteria block code does not match the expected temperature regex, `GetTemperature` returns -1 and the block is not registered as a hot spring.
- **World not loaded**: Tick processing checks that the world and block accessor are available before iterating tracked blocks. If the world is not ready, the tick is skipped.
- **Concurrent modification**: Collections are not modified during enumeration. Blocks to untrack are collected into a separate list and removed after iteration completes.

## Testing Strategy

### Unit Tests
- Temperature to radius/chance mapping
- Melting chance calculation (including cap)
- Block identification (hot bacteria vs snow)

### Integration Tests
- Hot spring registration/unregistration
- Snow block tracking/untracking
- Chunk load/unload behavior
- Multiple overlapping springs

### Manual Testing
- Place hot spring bacteria near snow
- Verify snow melts at expected rate
- Verify ice melts slower than snow
- Verify bacteria blocks are not affected
- Verify multiple springs increase melting rate
