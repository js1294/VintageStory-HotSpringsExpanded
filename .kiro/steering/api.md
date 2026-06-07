# Vintage Story API Reference

## Block API

### Block Properties
- `Block.Id` (int): Unique block ID number
- `Block.Code` (AssetLocation): Unique domain + code identifier (e.g., "game:snow")
- Use `block.Code.ToString()` to get the string identifier for comparison

### Block Position
- Use `Vintagestory.API.MathTools.BlockPos` for all block position operations
- The API uses its own `BlockPos` struct, not a custom wrapper
- When passing positions to API methods, cast or convert to `Vintagestory.API.MathTools.BlockPos`

## Chunk API

### IWorldChunk Interface
- `ChunkIndex` (long): Unique chunk identifier (packed X and Z coordinates)
- `World` (IWorldAccessor): Reference to the world
- `Data` (IChunkBlocks): Block data storage
- `Lighting` (IChunkLight): Lighting data
- `Entities` (Entity[]): Array of entities in the chunk

### Chunk Coordinates
- Chunk X and Z are packed into a single `long` value
- Extract coordinates: `int chunkX = (int)(chunkIndex & 0xFFFF);`
- Extract Z: `int chunkZ = (int)((chunkIndex >> 16) & 0xFFFF);`

## Event Handlers

### World Events
- `api.Event.ChunkColumnLoaded += Action<IWorldChunk>`: Called when a chunk column is loaded
- `api.Event.ChunkColumnUnloaded += Action<IWorldChunk>`: Called when a chunk column is unloaded
- `api.Event.DidPlaceBlock += Action<IBlockAccessor, BlockPos, Block>`: Called when a block is placed
- `api.Event.DidBreakBlock += Action<IBlockAccessor, BlockPos, Block, int>`: Called when a block is broken
- `api.Event.SaveGameLoaded += Action`: Called when the save game is loaded

### Tick Events
- `api.Event.RegisterGameTickListener(Action<float>, int, int)`: Register a handler that runs every game tick
- Use `UnregisterGameTickListener(long listenerId)` to remove handlers

### Event API Access
- Events are accessed via `api.Event` (IEventAPI on client, IServerEventAPI on server)
- Use `+=` to subscribe to events, `-=` to unsubscribe

## Snow Block Mechanics

### Snow Levels
- Snow exists in 8 levels (1-8), where 1 is minimum and 8 is full block
- Block codes: `snowlayer-1` through `snowlayer-8` for layers, `snowblock` for full block (changed from `snow` in VS 1.22)
- Ice block code: `ice`, `lakeice`, `glacierice`

### Melting Behavior
- Snow reduces level by 1 per successful melt attempt
- Ice converts to water blocks (like normal ice melting)
- Ice melts slower than snow (base 5% vs 10% chance per tick)

## Block Identification

### Hot Spring Bacteria
- `hotspringbacteriasmooth-55deg`: 55°C (base melting chance)
- `hotspringbacteriasmooth-65deg`: 65°C (increased radius and chance)
- `hotspringbacteriasmooth-74deg`: 74°C (further increased)
- `hotspringbacteria-87deg`: 87°C (maximum radius and chance)

## Performance Considerations

### Chunk Scanning
- Scan chunks during load/unload events
- Use chunk-based bucketing for efficient lookups
- Drop tracking when chunks unload (no pause/resume)

### Tick Processing
- Use 10-second intervals (200 ticks) for heavy operations
- Process one hot spring per tick (round-robin) to distribute load
- Cache radius calculations per temperature

## Build Configuration

### Target Framework
- Use `net10.0` to match the Vintage Story API
- The API is built against .NET 10, so the project must target the same framework

### Block ID Comparison
- Always use `block.Code.ToString()` for string-based block identification
- `Block.Id` is an int and should not be compared to strings
- Block codes are unique identifiers like "game:snow" or "hotspringsexpanded:customblock"
