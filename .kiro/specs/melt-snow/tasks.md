# Implementation Plan:

## Overview

Implementation tasks for the "melt-snow" feature that adds snow and ice melting behavior near hot spring bacteria blocks in Vintage Story.

## Tasks

- [x] 1. Create BlockIdentifiers utility class with hot bacteria block IDs, snow block type identifiers, `IsHotBacteria(Block)`, `IsSnowBlock(Block)`, and `GetTemperature(Block)` methods.
- [x] 2. Create data structures: `HotSpring` class (Position, Temperature, Radius, BaseMeltingChance), `SnowBlockData` class (Position, IsIce, CurrentMeltingChance, IsBeingTracked), and `BlockPos` struct for memory efficiency.
- [x] 3. Create `HotSpringTracker` class with `RegisterHotSpring(BlockPos, int)`, `UnregisterHotSpring(BlockPos)`, `GetHotSpringData(BlockPos)`, `GetNearbyHotSprings(BlockPos, int)`, and temperature-to-radius/chance mapping.
- [x] 4. Implement event handlers for hot springs: register `OnBlockGenerated`, `OnBlockPlaced`, and `OnBlockRemoved` handlers for hot spring registration/unregistration.
- [x] 5. Create `SnowBlockTracker` class with `TrackSnowBlock(BlockPos, Block)`, `UntrackSnowBlock(BlockPos)`, `GetTrackedSnowBlocks()`, and `UpdateMeltingChance(BlockPos, float)` methods.
- [x] 6. Implement event handlers for snow blocks: register `OnBlockPlaced` and `OnBlockRemoved` handlers for snow block tracking/untracking.
- [x] 7. Create `MeltingProcessor` class with `ProcessAllHotSprings()`, `ProcessHotSpring(BlockPos)`, `FindSnowBlocksInRadius(BlockPos, int)`, `ApplyMelting(BlockPos, float, bool)`, snow level reduction logic, ice-to-water conversion, `GetSnowLevel(Block)`, and `GetSnowLayerBlock(int)`.
- [x] 8. Implement tick processing: create `OnTick` handler with 50ms tick interval, process all hot springs each tick, and drop tracking when chunks unload.
- [x] 9. Implement chunk management: tracking handled by trackers, drop tracking when chunks unload.
- [x] 10. Implement client-side handling: initialize client-side system with no client-side melting logic; server handles all melting.
- [x] 11. Create `MeltSnowSystem` class and integrate with ModSystem: implement `Start(ICoreAPI)`, `StartServerSide(ICoreServerAPI)`, `StartClientSide(ICoreClientAPI)`, initialize all trackers and processors, and register all event handlers.
- [x] 12. Implement world load/unload handling: `OnSaveGameLoaded()` method to initialize tracking data on world load.
- [x] 13. Write unit tests for temperature-to-radius/chance mapping, melting chance calculation (including 100% cap), and block identification methods.
- [x] 14. Write integration tests for hot spring registration/unregistration, snow block tracking/untracking, chunk load/unload behavior, and multiple overlapping springs.
- [x] 15. Perform manual testing: hot spring near snow melts correctly, hot spring near ice melts slower, bacteria blocks are not affected, and multiple springs increase melting rate.

## Task Dependency Graph

```json
{
  "waves": [
    [1, 2],
    [3, 5],
    [4, 6, 7],
    [8, 9, 10],
    [11],
    [12],
    [13],
    [14],
    [15]
  ]
}
```

## Notes

- All tasks are completed. The implementation follows the architecture described in the design document.
- Tasks 1-2 are foundational (utilities and data structures), tasks 3-6 handle tracking, tasks 7-8 implement core melting logic, and tasks 9-12 wire everything together.
- Testing tasks (13-15) depend on the full integration being complete.
