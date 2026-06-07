# Project Structure

## Root Level
| File/Folder | Purpose |
|-------------|---------|
| `HotSpringsExpanded.csproj` | .NET project file with Vintage Story references |
| `HotSpringsExpandedModSystem.cs` | Main mod system class (inherits ModSystem) — entry point |
| `modinfo.json` | Mod metadata (ID, version, dependencies, type) |
| `modicon.png` | Optional mod icon (32x32 PNG) |
| `assets/` | Game assets (blocks, items, lang, textures, etc.) |
| `bin/` | Build output (gitignored) |
| `obj/` | Build artifacts (gitignored) |
| `Properties/` | Visual Studio launch profiles |
| `Models/` | Pure data classes (no behavior) |
| `Tracking/` | State management — register/unregister/query tracked blocks |
| `Processing/` | Game logic — orchestration and per-tick processing |
| `Utilities/` | Static helpers and low-level types |

## Source Code Structure
```
/
├── HotSpringsExpandedModSystem.cs      # Entry point (ModSystem)
├── Models/
│   ├── HotSpring.cs                    # Hot spring data (position, temp, radius, chances)
│   └── SnowBlockData.cs               # Tracked snow block data (position, distance, chance)
├── Tracking/
│   ├── HotSpringTracker.cs            # Tracks registered hot springs
│   └── SnowBlockTracker.cs            # Tracks snow/ice blocks near hot springs
├── Processing/
│   ├── MeltSnowSystem.cs             # Feature orchestrator (scanning, event handlers)
│   └── MeltingProcessor.cs           # Per-tick melting logic
└── Utilities/
    ├── BlockIdentifiers.cs            # Static block identification helpers
    └── BlockPos.cs                    # BlockPos wrapper struct
```

### Folder Guidelines
- **Models/**: Data-only classes with properties and no logic. If a class only has getters/setters, it belongs here.
- **Tracking/**: Classes that own a collection of tracked state and provide CRUD-style access. They may contain domain-specific calculations (e.g., melting chance).
- **Processing/**: Classes that perform actions on the world each tick or in response to events. They depend on trackers and utilities.
- **Utilities/**: Stateless static classes and structural types used across the project.
- **Root**: Only the `ModSystem` entry point lives here.

## Assets Structure
```
assets/
└── hotspringsexpanded/          # Mod ID folder (lowercase)
    ├── lang/                    # Localization files
    │   └── en.json              # English translations
    ├── configs/                 # Configuration files
    ├── sounds/                  # Sound files
    ├── textures/                # Texture images
    ├── models/                  # 3D models
    ├── shapes/                  # Block/item shapes
    ├── blocktypes/              # Block definitions (JSON)
    ├── itemtypes/               # Item definitions (JSON)
    ├── entities/                # Entity definitions (JSON)
    └── ...                      # Other mod assets
```

## Code Conventions

### Namespace
- Matches mod ID (e.g., `HotSpringsExpanded`)

### Mod System Class
- Name: `{ModName}ModSystem` (e.g., `HotSpringsExpandedModSystem`)
- Inherits from `ModSystem`
- Override `Start(ICoreAPI)` for shared initialization
- Override `StartServerSide(ICoreServerAPI)` for server-only code
- Override `StartClientSide(ICoreClientAPI)` for client-only code

### API Access
- `ICoreAPI`: Base API available on all sides
- `ICoreServerAPI`: Server-side API (world access, commands)
- `ICoreClientAPI`: Client-side API (rendering, UI)

### Side Detection
- `api.Side`: Returns Client/Server/Shared enum
- `ShouldLoad(EnumAppSide side)`: Control which sides load mod
- `ICoreClientAPI.IsSinglePlayer`: Check if single-player

### Mod ID in Code
- Use lowercase with underscores (e.g., `hotspringsexpanded`)
- Used in language keys: `{modid}:{key}` (e.g., `hotspringsexpanded:hello`)

## Mod Loading

### Mod Types
- **Compiled**: Pre-built DLL (faster, supports mod interaction)
- **Source**: C# files compiled at runtime (slower, easier iteration)

### Load Order
- Mods load in ascending order of `ExecuteOrder()` return value
- Default: 0.1
- Adjust for dependencies (e.g., worldgen mods load early)

### Mod Interaction
- Access other mods via `api.ModLoader.GetMod("modid")`
- Get mod system: `api.ModLoader.GetModSystem<T>()`
- Mods must be compiled to interact with each other

## World Access

### Important Notes
- Mods load before world is loaded
- Cannot access world directly in `Start()`
- Register to events for world interaction
- Use `api.World` for block/entity access

### Efficient Search
- **Entities**: Use `EntityPartitioning` for bucketed searches
- **Blocks**: Use POI Registry for rare block searches
- **Area scans**: Use `api.World.GetBlockAccessorPrefetch()`

### Block Position Types
- The Vintage Story API uses `Vintagestory.API.MathTools.BlockPos` for all block operations
- When working with block positions, convert to `Vintagestory.API.MathTools.BlockPos` before passing to API methods
- Example: `(Vintagestory.API.MathTools.BlockPos)myBlockPos`

### Chunk Operations
- Chunks are identified by a packed `long` value from `IWorldChunk.ChunkIndex`
- Extract X: `int chunkX = (int)(chunkIndex & 0xFFFF);`
- Extract Z: `int chunkZ = (int)((chunkIndex >> 16) & 0xFFFF);`
- Use chunk-based bucketing for efficient lookups
- Drop tracking when chunks unload (no pause/resume)
