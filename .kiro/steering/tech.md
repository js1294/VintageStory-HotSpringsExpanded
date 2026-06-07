# Tech Stack and Build System

## Project Type
Vintage Story mod (C# code mod)

## Tech Stack
- **Language**: C# 12 (.NET 10.0)
- **Game API**: Vintage Story API
- **Target Game Version**: 1.22.2
- **Dependencies**:
  - VintagestoryAPI (core API - required by all mods)
  - VSSurvivalMod, VSEssentials, VSCreativeMod (vanilla mods)
  - Newtonsoft.Json (JSON serialization)
  - 0Harmony (patching/harmony library)
  - protobuf-net (serialization)
  - cairo-sharp (graphics)
  - Microsoft.Data.Sqlite (database)

## Build System
- **SDK**: Microsoft.NET.Sdk
- **Output Path**: `bin\$(Configuration)\Mods\mod`
- **Configuration**: Debug/Release
- **Environment Variable**: `$(VINTAGE_STORY)` must point to Vintage Story installation

## Common Commands

### Build
```bash
dotnet build
```

### Build Configuration
```bash
dotnet build -c Release
dotnet build -c Debug
```

### Run (Client)
Use the "Client" launch profile from Visual Studio or run:
```bash
dotnet run --project HotSpringsExpanded.csproj
```
(Requires `$(VINTAGE_STORY)` environment variable pointing to Vintage Story installation)

### Run (Server)
Use the "Server" launch profile from Visual Studio

## Development Environment

### Recommended IDEs
- **Visual Studio Community** (Windows/macOS) - Best C# support, used by VS team
- **JetBrains Rider** - Cross-platform, excellent .NET support
- **Visual Studio Code** - Lightweight, good for beginners

## Mod System Lifecycle

### Execute Order
Mods can control load order by overriding `ExecuteOrder()` returning 0.0-1.0:
- Worldgen: GenTerra (0), RockStrata (0.1), Deposits (0.2), Caves (0.3), Blocklayers (0.4)
- Asset Loading: Json Overrides (0.05), Block/Item Loader (0.2), Recipes (1.0)

### Side Detection
- `api.Side` returns Client/Server/Shared
- Use `ShouldLoad(EnumAppSide side)` to control which sides load the mod
- Use `ICoreClientAPI.IsSinglePlayer` to detect single-player mode

## Debugging

### Debugging Tips
- Use breakpoints and Visual Studio's debug features
- Enable `/errorreporter 1` to see errors on startup
- Check `server-main.txt` and `client-main.txt` logs
- Use `/entity debug 1` for entity debugging
- Hot reload works with compiled mods in Visual Studio and Rider

## Important API Notes

### Target Framework
- Use `net10.0` to match the Vintage Story API
- The API is built against .NET 10, so the project must target the same framework
- Mismatch causes `System.Runtime` version errors

### Block API
- `Block.Id` is an `int` (unique block ID number)
- `Block.Code` is an `AssetLocation` - use `block.Code.ToString()` for string comparison
- Never compare `Block.Id` directly to strings

### Block Position
- Use `Vintagestory.API.MathTools.BlockPos` for all block position operations
- The API uses its own `BlockPos` struct, not a custom wrapper
- When passing positions to API methods, cast or convert to `Vintagestory.API.MathTools.BlockPos`

### Chunk API
- `IWorldChunk.ChunkIndex` is a `long` containing packed X and Z coordinates
- Extract coordinates: `int chunkX = (int)(chunkIndex & 0xFFFF);`
- Extract Z: `int chunkZ = (int)((chunkIndex >> 16) & 0xFFFF);`

### Event Handlers
- Events are accessed via `api.Event` (IEventAPI on client, IServerEventAPI on server)
- Use `+=` to subscribe to events, `-=` to unsubscribe
- Use `UnregisterGameTickListener(long listenerId)` to remove tick handlers

### Common API Errors and Fixes

#### Error: CS0246 - Type or namespace 'Chunk' could not be found
**Cause**: The API uses `IWorldChunk` interface, not a `Chunk` class
**Fix**: Use `IWorldChunk` instead of `Chunk`

#### Error: CS0246 - Type or namespace 'ICoreServerAPI' could not be found
**Cause**: Missing using directive for `Vintagestory.API.Server`
**Fix**: Add `using Vintagestory.API.Server;`

#### Error: CS1705 - System.Runtime version mismatch
**Cause**: Project targets .NET 8.0 but API is built against .NET 10
**Fix**: Update `<TargetFramework>net10.0</TargetFramework>` in .csproj

#### Error: CS0029 - Cannot implicitly convert type 'int' to 'string'
**Cause**: `Block.Id` returns an `int`, not a string
**Fix**: Use `block.Code.ToString()` for string-based block identification

#### Error: CS1503 - Cannot convert from 'HotSpringsExpanded.BlockPos' to 'Vintagestory.API.MathTools.BlockPos'
**Cause**: Custom BlockPos struct not compatible with API's BlockPos
**Fix**: Use `Vintagestory.API.MathTools.BlockPos` directly or cast with `(Vintagestory.API.MathTools.BlockPos)myBlockPos`

#### Error: CS1061 - 'IWorldChunk' does not contain a definition for 'ChunkIndex'
**Cause**: `IWorldChunk` doesn't have a `ChunkIndex` property
**Fix**: Use `chunk.MapChunk.MapRegion.ChunkIndex3D(chunk.X, 0, chunk.Z)` to get chunk index

#### Error: CS1061 - 'IServerWorldAccessor' does not contain a definition for 'RegisterOnChunkLoaded'
**Cause**: Event handler names are different in the API
**Fix**: Use correct event names:
- `api.Event.ChunkColumnLoaded` instead of `RegisterOnChunkLoaded`
- `api.Event.ChunkColumnUnloaded` instead of `RegisterOnChunkUnloaded`
- `api.Event.DidPlaceBlock` instead of `RegisterOnBlockPlaced`
- `api.Event.DidBreakBlock` instead of `RegisterOnBlockRemoved`
- `api.Event.SaveGameLoaded` instead of `RegisterOnWorldLoad`
- `api.Event.RegisterGameTickListener` instead of `RegisterGameTickHandler`

#### Warning: CS0675 - Bitwise-or operator used on a sign-extended operand
**Cause**: Using `int` for chunk key calculation instead of `long`
**Fix**: Use `long` for chunk key: `((long)chunkZ << 16) | (chunkX & 0xFFFF)`

#### Warning: CS0414 - Field is assigned but its value is never used
**Cause**: Field declared but not used in the code
**Fix**: Remove unused fields or use them appropriately
