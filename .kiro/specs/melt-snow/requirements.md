# Requirements Document

## Introduction

This feature adds snow melting behavior to hot springs in the Vintage Story game. Hot springs are natural world generation features that contain hot bacteria at different temperatures. The mod will make snow and ice melt when they come into contact with hot springs, but NOT on the hot bacteria blocks themselves. The melting effect will extend to snow/ice blocks directly surrounding the hot spring.

### Melting Behavior Details

- **Trigger Mechanism**: Snow melting is checked every 10 seconds (200 ticks at 20 ticks/second) to avoid performance impact
- **Melting Process**: Snow blocks reduce their level incrementally (snow already has multiple levels/amounts in the game). Ice blocks convert to water blocks like normal ice melting does.
- **No Progress Bar**: There is no visual progress bar for melting
- **Melting Chance**:
  - **100% chance** when snow/ice lands directly on hot bacteria blocks
  - **~10% base chance per tick** when snow/ice is near (but not on) hot bacteria blocks
  - **5% base chance per tick** for ice blocks (ice takes longer to melt)
- **Radius Behavior**: All radiuses use Euclidean distance (3D). Higher temperature bacteria have a larger radius (1-6 blocks).
- **Water Handling**: Melted snow does not become water blocks. Ice converts to water blocks like normal ice melting. Water freezing in cold biomes is not in scope.
- **Processing**: All hot springs are processed every tick. Melting always happens (no batching limit).
- **Client-Side**: No client-side prediction or visual effects. Server handles all melting logic.
- **Overlap Behavior**: Multiple hot springs overlapping increase the melting chance. The maximum melting chance is capped at 100%.
- **Chunk Management**: Chunks are never paused. If a chunk is unloaded during processing, the tracking for that chunk is dropped.
- **Backwards Compatibility**: Not applicable. This mod supports Vintage Story v1.22.0-rc.8 and later.

## Background: Hot Springs in Vintage Story

### World Generation
Hot springs are natural world generation features that appear organically in the world during map generation. They are not placed by players but form alongside other world features.

### Hot Spring Structure
- **Gravel Type**: Hot springs generate with a unique gravel type (typically topped with bacterial mats)
- **Bacterial Mats**: Colorful thermophilic bacteria that indicate the hot spring's temperature
- **No Hot Spring Block**: There is no single "hot spring block" - the bacteria themselves represent the heat source
- **Temperature Indication**: The bacteria have specific temperatures that indicate the heat level, but the temperature is not stored as a block property

### Bacteria Types and Temperatures
Hot springs generate with the following bacterial mat types (block IDs):
- **hotspringbacteriasmooth-55deg**: Low-temperature bacteria (55°C)
- **hotspringbacteriasmooth-65deg**: Medium-temperature bacteria (65°C)
- **hotspringbacteriasmooth-74deg**: High-temperature bacteria (74°C)
- **hotspringbacteria-87deg**: Very high-temperature bacteria (87°C)

### Snow Block Mechanics
- Snow exists in 8 layers (0-7), where 0 is no snow and 7 is full snow block
- Snow layers reduce by 1 level per successful melt attempt
- When snow reaches level 0, it is removed from the world
- Ice blocks convert to water blocks (like normal ice melting)

### Temperature Mapping
The bacteria temperatures affect melting behavior:
- **55°C**: Base melting chance (~10% for snow, ~5% for ice), base radius (1 block)
- **65°C**: Increased melting chance and radius
- **74°C**: Further increased melting chance and radius
- **87°C**: Maximum melting chance and largest radius (6 blocks)

**Important**: The temperature is represented by the bacteria block type, not stored as a block property. We identify the temperature by the specific bacteria block ID.

### Generation Context
- Hot springs are typically found on **suevite stones** (meteor impact sites) or other specific rock types
- They generate naturally during world generation alongside other features
- The heat from hot springs affects nearby blocks and entities

### Key Behaviors
- Snow melts on contact with hot springs
- The melting effect extends to snow blocks surrounding the hot spring
- Bacteria blocks themselves are NOT melted (they are the heat source)

## Glossary

- **HotSpring**: A collection of bacterial blocks in Vintage Story that emit heat at different temperatures
- **HotBacteria**: The bacterial blocks within a hot spring that emit heat (identified by their specific block IDs representing 55°C, 65°C, 74°C, 87°C)
- **SnowBlock**: A snow block in the game world (includes snow layers, snow blocks, and ice)
- **MeltSnow**: The feature that causes snow/ice to melt when in contact with hot springs
- **System**: The MeltSnow mod system that implements the snow melting behavior
- **BacteriaTemperature**: The temperature level of hot bacteria (55°C, 65°C, 74°C, or 87°C) which determines melting chance and radius

## Requirements

### Requirement 1: Detect Hot Spring Bacteria Blocks

**User Story:** As a mod developer, I want the system to detect when hot spring bacteria blocks are generated or loaded, so that I can track which blocks need to melt surrounding snow.

**Background:** Hot springs are natural world generation features that appear organically during map generation. They are typically found on suevite stones (meteor impact sites) or other specific rock types. Hot springs contain multiple bacterial blocks at different temperatures (55°C, 65°C, 74°C, 87°C), and higher temperature bacteria should melt surrounding snow faster and affect a larger radius. There is no single "hot spring block" - the bacteria themselves represent the heat source.

#### Acceptance Criteria

1. WHEN a hot spring bacterial block is generated or loaded in the world, THE System SHALL register it for snow melting tracking
2. WHILE a hot spring bacterial block exists in the world, THE System SHALL maintain its tracking state including its bacteria temperature
3. IF a hot spring bacterial block is removed from the world, THEN THE System SHALL stop tracking it
4. WHEN hot bacteria blocks are detected, THE System SHALL track them separately to exclude them from snow melting operations
5. WHEN hot bacteria with higher temperature (65°C, 74°C, 87°C) is detected, THE System SHALL increase the melting chance and radius for that hot spring
6. WHEN hot bacteria with 55°C is detected, THE System SHALL use base melting chance and radius
7. WHEN a hot spring is registered, THE System SHALL store its position, temperature, radius, and base melting chances

### Requirement 2: Identify Snow Blocks in Range

**User Story:** As a player, I want snow to melt around hot springs, so that hot springs clear snow from their immediate area.

**Background:** The snow melting effect should extend to snow/ice blocks directly surrounding the hot spring, but NOT on the hot bacteria blocks themselves (which are the heat source). Higher temperature bacteria should have a larger melting radius.

#### Acceptance Criteria

1. WHEN a hot spring is active, THE System SHALL identify all SnowBlock instances within a radius determined by the bacteria temperature (1-6 blocks, based on temperature)
2. WHILE a snow block is within range of a hot spring, THE System SHALL track it for melting
3. WHERE a snow block is not within range of a hot spring, THE System SHALL NOT melt it
4. WHERE a block is identified as hot bacteria, THE System SHALL exclude it from snow melting operations
5. WHEN a snow block is adjacent to a hot spring but not on a hot bacteria block, THE System SHALL attempt to melt it
6. WHEN a hot spring has higher temperature bacteria (65°C, 74°C, 87°C), THE System SHALL increase the radius at which snow blocks are identified for melting
7. WHEN a snow block is directly above a hot bacteria block, THE System SHALL mark it for 100% melting chance

### Requirement 3: Melt Snow on Contact

**User Story:** As a player, I want snow to melt when it touches a hot spring, so that hot springs create clear areas around them.

#### Acceptance Criteria

1. WHEN a snow/ice block is within range of a hot spring, THE System SHALL attempt to melt it based on the melting chance
2. WHILE a snow/ice block remains within range of a hot spring, THE System SHALL continue to attempt melting it each tick
3. IF a snow/ice block is removed from the world, THEN THE System SHALL stop attempting to melt it
4. WHEN snow/ice lands directly on a hot bacteria block, THE System SHALL melt it with 100% chance
5. WHEN snow/ice is near (but not on) a hot bacteria block, THE System SHALL melt it with ~10% chance per tick (or 5% for ice)
6. WHEN a hot spring has higher temperature bacteria (65°C, 74°C, 87°C), THE System SHALL increase the melting chance for nearby snow/ice
7. WHEN multiple hot springs overlap, THE System SHALL increase the melting chance (capped at 100%)

### Requirement 4: Preserve Hot Bacteria Blocks

**User Story:** As a mod developer, I want hot bacteria blocks to remain unchanged, so that the hot spring's core functionality is preserved.

**Background:** Hot bacteria blocks are the source of heat emission in hot springs. They should never be melted or modified, as this would break the hot spring's core functionality. Higher temperature bacteria should melt surrounding snow faster and affect a larger radius.

#### Acceptance Criteria

1. WHILE a hot bacteria block exists, THE System SHALL NOT melt or modify it
2. WHERE a block is identified as hot bacteria, THE System SHALL exclude it from snow melting operations
3. WHEN a hot spring contains multiple bacteria types, THE System SHALL preserve all of them
4. IF a snow/ice block is directly adjacent to a hot bacteria block, THE System SHALL attempt to melt the snow/ice block (hot spring heat extends to surrounding area)
5. IF a snow/ice block is directly on top of a hot bacteria block, THE System SHALL NOT melt it (bacteria blocks are the heat source and should remain unchanged)
6. WHEN a hot spring has higher temperature bacteria (65°C, 74°C, 87°C), THE System SHALL increase the radius at which snow/ice blocks are affected
7. WHEN a snow/ice block is directly above a hot bacteria block, THE System SHALL NOT melt the bacteria block (only the snow/ice above it)

### Requirement 5: Server-Side Execution

**User Story:** As a mod developer, I want snow melting to occur on the server side, so that the behavior is consistent for all players in multiplayer.

#### Acceptance Criteria

1. WHEN a hot spring is active, THE System SHALL perform snow melting on the server side
2. WHILE snow melting occurs, THE System SHALL update the world state on the server
3. WHERE the system is running on the client side only, THE System SHALL NOT perform snow melting
4. CLIENTS SHALL NOT predict melting state (server handles all logic)

### Requirement 6: Performance Optimization

**User Story:** As a mod developer, I want the system to efficiently track and update snow melting, so that performance is not significantly impacted.

**Background:** Hot springs generate naturally during world generation and may be scattered across the world. The system uses efficient data structures (Dictionary for O(1) lookups) to track them and their associated snow melting zones. All hot springs are processed every tick. Melting always happens (no batching limit).

#### Acceptance Criteria

1. WHILE tracking hot springs, THE System SHALL use efficient data structures for block lookups (Dictionary for O(1) lookups)
2. WHEN a snow/ice block changes state, THE System SHALL update its tracking state immediately
3. WHERE a hot spring is removed, THE System SHALL stop tracking it (no explicit cleanup required)
4. WHEN a chunk is unloaded, THE System SHALL drop tracking for hot springs in that chunk
5. WHEN a chunk is loaded, THE System SHALL begin tracking for hot springs in that chunk
6. THE System SHALL check melting every tick (50ms) to ensure responsive melting
7. THE System SHALL process all hot springs each tick (no round-robin)
8. WHEN multiple hot springs overlap, THE System SHALL increase the melting chance (capped at 100%)
### Requirement 7: Snow and Ice Block Support

**User Story:** As a player, I want both snow and ice to melt around hot springs, so that the melting effect is consistent across all cold blocks.

**Background:** The system should affect snow blocks, snow layers, and ice blocks. Snow blocks reduce their level incrementally (no water blocks). Ice blocks convert to water blocks like normal ice melting does. Ice takes longer to melt (reduced base chance of 5% vs 10% for snow).

#### Acceptance Criteria

1. WHEN a snow block is within range of a hot spring, THE System SHALL attempt to melt it
2. WHEN an ice block is within range of a hot spring, THE System SHALL attempt to melt it
3. WHEN an ice block melts, THE System SHALL convert it to water blocks (like normal ice melting)
4. WHEN a snow block melts, THE System SHALL reduce its level incrementally (no water blocks)
5. THE System SHALL use a 5% base melting chance for ice blocks (vs 10% for snow)
6. WHEN a snow block reaches level 0, THE System SHALL remove it from the world
7. WHEN a snow block is directly above a hot bacteria block, THE System SHALL melt it with 100% chance
