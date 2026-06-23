# Requirements Document

## Introduction

This feature extends the existing salt water crop damage mechanic in Vintage Story to also apply to boiling water blocks found in hot springs. In the base game, salt water already damages and kills crops planted on nearby farmland. Boiling water in hot springs contains toxic minerals and heavy metals that are equally harmful to plant life, and should behave identically to salt water in regard to crop damage.

Investigation of the game confirmed that salt water crop damage is **data-driven**, not hardcoded per-block: the vanilla farmland system scans nearby fluid blocks and kills crops on adjacent farmland when a fluid's top-level `liquidCode` field equals `"saltwater"`. Because the behavior is controlled by a block data field, this feature is implemented as a **pure JSON asset patch** that changes the boiling water block's `liquidCode` to `"saltwater"`. No C# code, Harmony patch, tick listener, event handler, or server-side registration is required — vanilla already does all of the work once the boiling water block carries the salt-water liquid code.

> **Implementation note (verified at build time).** An earlier draft of this spec assumed a `waterType` attribute drove salt-water crop damage. Verification against game 1.22 vanilla assets and the `vssurvivalmod` source showed no such attribute exists; detection is `Block.LiquidCode == "saltwater"` (in `BESoilNutrition` → `BEFarmland`). The feature was re-scoped to **Option A**: patch `/liquidCode` to `"saltwater"`. Accepted trade-off: `liquidCode` also feeds other systems (container fill, drinking, freezing, liquid spreading), so boiling water sharing the salt-water liquid code may cause side effects there.

The goal is to make boiling water reuse the exact vanilla salt water crop damage code path by tagging the boiling water block so the game treats it as toxic water. This keeps the implementation minimal, maximally consistent with vanilla behavior, and resilient across game updates.

### Key Behavior

- **Damage Mechanism**: Identical to salt water — vanilla's existing farmland/crop logic withers and kills crops near the tagged fluid.
- **Damage Source**: The boiling water liquid block, tagged via a JSON-patched `liquidCode` set to `"saltwater"`.
- **Damage Radius and Behavior**: Inherited from vanilla salt water exactly; the patch does not and cannot customize the radius.
- **Implementation**: A single JSON patch file under `assets/hotspringsexpanded/patches/`, applied at asset-load time.
- **Processing**: Entirely handled by vanilla's existing server-side farmland processing, which already propagates results to clients.

## Background: Salt Water Crop Damage in Vintage Story

The vanilla game already implements crop damage from salt water. When crops are planted on farmland near salt water, they die and are converted to dead crop blocks. The vanilla farmland system identifies a crop-damaging fluid by its `liquidCode` field; fluids whose `liquidCode` is `"saltwater"` trigger crop death. The damage radius, timing, death reason, and dead-crop outcome are all defined by vanilla. This feature reuses that same data-driven mechanism for boiling water rather than implementing a parallel system.

## Background: JSON Asset Patch Approach

Vintage Story supports modifying another mod's or the base game's JSON assets at load time without compiled code. A patch file is a JSON array of patch instructions; each instruction targets a base asset by its domain-qualified `AssetLocation`, specifies an operation (`add`, `replace`, etc.), a JSON Pointer `path`, and a `value`.

This feature changes the boiling water block's `liquidCode` to `"saltwater"` using a patch of the form:

```json
[
  {
    "file": "game:blocktypes/liquid/boilingwater",
    "op": "replace",
    "path": "/liquidCode",
    "value": "saltwater"
  }
]
```

The `liquidCode` field and the `"saltwater"` value were verified against the target game version's asset definitions (`game:blocktypes/liquid/saltwater` has `liquidCode: "saltwater"`; `game:blocktypes/liquid/boilingwater` has `liquidCode: "boilingwater"`). See `.kiro/steering/json-patches.md` for the patching reference.

## Background: Boiling Water Blocks

Hot springs contain boiling water as liquid blocks. The vanilla boiling water block is a single base definition (`game:blocktypes/liquid/boilingwater`) from which many variant blocks are generated, covering flow directions (n, ne, e, se, s, sw, w, nw, d, still) and water levels (1 through 7). Examples of generated variants: `boilingwater-still-7`, `boilingwater-n-3`, `boilingwater-d-1`.

Because variants inherit attributes from the base definition, patching the base file applies the water-type attribute to every boiling water variant automatically. The boiling water contains toxic minerals and heavy metals from deep underground that contaminate surrounding soil and kill plant roots.

## Glossary

- **Patch**: The JSON asset patch file under `assets/hotspringsexpanded/patches/` that changes the boiling water block's `liquidCode` to `"saltwater"`.
- **BoilingWater**: The vanilla liquid block defined at `game:blocktypes/liquid/boilingwater`, including all generated variants (all directions, all levels, still).
- **SaltWater**: The existing salt water liquid block in Vintage Story that already damages crops via its `liquidCode` of `"saltwater"`.
- **WaterTypeAttribute**: The data field on a fluid block that the vanilla farmland system reads to decide whether the fluid damages nearby crops. For game 1.22 this is the block's top-level `liquidCode` field, and the crop-damaging value is `"saltwater"`.
- **Crop**: Any planted crop block in Vintage Story that vanilla can damage via toxic water.
- **DeadCrop**: The block vanilla produces when a crop is killed, recording the death reason and retaining seed inventory.
- **VanillaFarmlandSystem**: The base-game server-side system that scans for tagged fluids near farmland and kills crops accordingly.

## Requirements

### Requirement 1: Tag Boiling Water as Crop-Damaging via the Water-Type Attribute

**User Story:** As a mod developer, I want the boiling water block to be tagged with the salt water-type attribute, so that vanilla recognizes it as crop-damaging toxic water without any custom code.

**Background:** The vanilla farmland system identifies crop-damaging fluids by reading the water-type attribute on the fluid block. Adding that attribute to the boiling water block via a JSON patch makes vanilla treat boiling water exactly as it treats salt water.

#### Acceptance Criteria

1. THE Patch SHALL add the salt WaterTypeAttribute to the BoilingWater base block definition (`game:blocktypes/liquid/boilingwater`) so that the VanillaFarmlandSystem recognizes BoilingWater as a crop-damaging fluid.
2. WHEN game assets are loaded, THE Patch SHALL apply the WaterTypeAttribute at asset-load time so that the attribute is present on the BoilingWater block before world processing begins.
3. WHERE BoilingWater variants are generated from the base definition, THE Patch SHALL cause every variant to inherit the WaterTypeAttribute, including all direction suffixes (n, ne, e, se, s, sw, w, nw, d, still) and all water levels 1 through 7.
4. THE Patch SHALL target the BoilingWater base definition file rather than individual variant blocks, so that variant coverage is achieved through inheritance.
5. THE Patch SHALL set the WaterTypeAttribute to the same value that the VanillaFarmlandSystem uses to identify SaltWater as crop-damaging.

### Requirement 2: Reuse the Vanilla Salt Water Crop Damage Code Path

**User Story:** As a mod developer, I want boiling water to share the exact vanilla salt water crop damage code, so that both water types behave identically and no parallel logic exists.

**Background:** Because the behavior is data-driven, tagging boiling water with the salt water-type attribute means the same vanilla code that damages crops near salt water runs unchanged for boiling water. There is no mod-side damage logic to maintain.

#### Acceptance Criteria

1. WHEN the VanillaFarmlandSystem evaluates a fluid for crop damage, THE Patch SHALL cause BoilingWater to pass the same WaterTypeAttribute check that SaltWater passes.
2. THE Patch SHALL NOT introduce any separate crop damage processing, death handling, or behavioral divergence for BoilingWater; all crop damage SHALL be performed by the unchanged VanillaFarmlandSystem.
3. WHEN BoilingWater kills a Crop, THE VanillaFarmlandSystem SHALL produce a DeadCrop block using the same mechanism and death reason it uses for SaltWater.
4. THE feature SHALL be implemented solely as the JSON Patch and SHALL NOT include C# code, Harmony patches, tick listeners, event handlers, or server-side registration.

### Requirement 3: Inherit Vanilla Salt Water Crop Damage Behavior and Radius

**User Story:** As a player, I want crops near boiling water to be damaged the same way crops near salt water are, so that farming near hot springs has a consequence consistent with the rest of the game.

**Background:** A pure JSON attribute patch cannot customize the damage radius or any other behavioral parameter — those are defined by the VanillaFarmlandSystem. Boiling water therefore adopts whatever range, timing, and behavior vanilla applies to salt water.

#### Acceptance Criteria

1. THE feature SHALL apply vanilla's native salt water crop damage behavior to BoilingWater, including whatever damage range the VanillaFarmlandSystem uses for SaltWater.
2. THE Patch SHALL NOT define, override, or customize the crop damage radius, timing, or any other behavioral parameter beyond setting the WaterTypeAttribute.
3. WHEN a Crop is within vanilla's salt water damage range of a BoilingWater block, THE VanillaFarmlandSystem SHALL subject that Crop to crop damage.
4. IF a Crop is outside vanilla's salt water damage range of all BoilingWater blocks, THEN THE VanillaFarmlandSystem SHALL NOT apply damage to that Crop on account of BoilingWater.
5. WHERE vanilla's salt water behavior changes across game versions, THE feature SHALL adopt the updated behavior automatically because BoilingWater carries the same WaterTypeAttribute as SaltWater.

### Requirement 4: Consistent Crop Death Behavior

**User Story:** As a player, I want crops killed by boiling water to behave the same as crops killed by salt water, so that the game experience is consistent.

**Background:** When salt water kills a crop in vanilla Vintage Story, the crop is replaced with a dead crop block that records the death reason and retains seeds. Because the same vanilla code runs for boiling water, the outcome is identical by construction.

#### Acceptance Criteria

1. WHEN a Crop is killed by BoilingWater, THE VanillaFarmlandSystem SHALL replace it with a DeadCrop block at the same position, identical to the SaltWater outcome.
2. WHEN a Crop is killed by BoilingWater, THE VanillaFarmlandSystem SHALL record the same death reason on the DeadCrop block as it records for SaltWater.
3. WHEN a Crop is killed by BoilingWater, THE VanillaFarmlandSystem SHALL retain the original crop's seed inventory in the DeadCrop block exactly as it does for SaltWater.
4. WHEN a DeadCrop block created by BoilingWater is broken, THE VanillaFarmlandSystem SHALL drop seeds in the same quantity and with the same occasional-return behavior as a SaltWater DeadCrop, and SHALL NOT produce harvest drops (grain, vegetables, or other crop produce).
5. THE feature SHALL NOT alter any aspect of vanilla DeadCrop creation, seed retention, or drop behavior, since BoilingWater reuses the unchanged VanillaFarmlandSystem.

### Requirement 5: Vanilla Server-Side Processing

**User Story:** As a mod developer, I want boiling water crop damage to run through vanilla's existing server-side processing, so that behavior is consistent for all players in multiplayer without any mod-side side handling.

**Background:** Vanilla's farmland crop damage already runs on the server side and propagates block changes to connected clients. Because the feature only adds an attribute, no mod code registers listeners and no side-specific registration is needed.

#### Acceptance Criteria

1. THE feature SHALL rely entirely on the VanillaFarmlandSystem's existing server-side crop damage processing and SHALL NOT register any mod-side tick listeners or event handlers.
2. WHEN assets are loaded, THE Patch SHALL apply the WaterTypeAttribute so that all subsequent crop damage involving BoilingWater is handled by the VanillaFarmlandSystem server-side, exactly as for SaltWater.
3. WHEN the VanillaFarmlandSystem replaces a Crop with a DeadCrop block, THE feature SHALL rely on vanilla's existing block-change propagation to update all connected clients.
4. WHILE running in single-player mode, THE feature SHALL rely on the VanillaFarmlandSystem's server-side processing on the local server, identical to its behavior for SaltWater.
