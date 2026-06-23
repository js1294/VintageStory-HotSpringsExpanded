# Implementation Plan: Crop Damage from Boiling Water

## Overview

This feature is a **pure JSON asset patch** — no C# code, no Harmony patch, no tick listener, no mod-owned logic. The mod ships a single patch file that changes the boiling water base definition's `liquidCode` to `"saltwater"`, after which vanilla's farmland system treats boiling water exactly like salt water for all crop-damage behavior.

The work is just two build steps: (1) verify the exact field/value (and the base definition's shape) against vanilla assets, then (2) author the patch file using the verified field/value and the `replace` operation. Step 1 gated step 2 and confirmed the original `waterType` attribute does not exist — detection is `liquidCode == "saltwater"`, so the feature was re-scoped to patch `/liquidCode` (Option A). There are no unit tests and no property-based tests (the design defines no correctness properties); the user does all testing in-game. Patching reference: `.kiro/steering/json-patches.md`.

## Tasks

- [x] 1. Verify the attribute key/value and the base definition shape against vanilla assets
  - Inspect the vanilla salt water definition `game:blocktypes/liquid/saltwater` (under the install's `assets/survival/`) and cross-check the farmland definitions to confirm the exact attribute key (provisionally `waterType`) and value (provisionally `salt`) the crop-damage code actually reads.
  - Inspect `game:blocktypes/liquid/boilingwater` to confirm the domain-qualified target and whether the base definition already has an `attributes` object. If `attributes` exists, the patch adds the leaf at `/attributes/{key}`; if it is absent, the patch must add the object (`"path": "/attributes", "value": { "{key}": "{value}" }`) because a JSON Pointer cannot create missing parents.
  - This step gates the patch: a wrong key/value or wrong shape produces no error and no effect. Record the confirmed key, value, and patch shape for task 2.
  - _Requirements: 1.1, 1.3, 1.4, 1.5, 2.1_

- [x] 2. Create the JSON patch file
  - Create `HotSpringsExpanded/assets/hotspringsexpanded/patches/boilingwater-liquidcode.json` as a JSON array of patch instructions, per `.kiro/steering/json-patches.md`.
  - Target the base definition `game:blocktypes/liquid/boilingwater` (no `.json` extension), using `op: "replace"`, `path: "/liquidCode"`, `value: "saltwater"` (verified in task 1).
  - Patching the base covers every variant (all directions, levels 1-7) through inheritance.
  - _Requirements: 1.1, 1.2, 1.4, 1.5, 2.2, 2.4_

- [x] 3. User verifies in-game and checks the load logs
  - Launch with `/errorreporter 1`, place boiling water near crops, and confirm they die and leave a dead-crop block the same as crops near salt water; confirm the patch shows as applied in `server-main.txt` / `client-main.txt`.

## Notes

- This feature has **no mod-owned code**, so there are no unit tests and no property-based tests; the design defines no correctness properties.
- Task 1 verified that vanilla keys salt-water crop damage on `liquidCode == "saltwater"` (not a `waterType` attribute). The feature was re-scoped to **Option A**: patch boiling water's `/liquidCode` to `"saltwater"`. Accepted trade-off: `liquidCode` is shared with other systems (container fill, drinking, freezing, spreading), so side effects there are possible.
- The damage radius, timing, death reason, and dead-crop mechanics are all vanilla's and are not customized by the patch.
- Task 3 is performed by the user in-game; tasks 1 and 2 are the only build steps. Patching reference: `.kiro/steering/json-patches.md`.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1"] },
    { "id": 1, "tasks": ["2"] }
  ]
}
```
