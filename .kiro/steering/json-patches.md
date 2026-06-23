---
inclusion: manual
---

# Vintage Story JSON Asset Patches

Vintage Story ships a patching system that performs pinpoint modifications on JSON assets without replacing the whole file. This is the preferred way to tweak existing block, item, entity, or recipe definitions (from the base game or another mod) because it preserves other mods' changes to the same file and survives game updates better than full overrides.

A mod that contains only patches (no `.dll`/`.cs`) is a **content mod**. Adding C# makes it a **code mod**. This mod (HotSpringsExpanded) is already a code mod, but patches still live in the asset folder and are loaded the same way.

The patch syntax is based on [RFC 6902 (JSON Patch)](https://tools.ietf.org/html/rfc6902), with two Vintage-Story-specific additions: `addmerge` and `addeach`. Content was rephrased for compliance with licensing restrictions; see the [official wiki](https://wiki.vintagestory.at/index.php/Modding:JSON_Patching) and [JsonPatch JSON docs](https://apidocs.vintagestory.at/json-docs/jsondocs/Vintagestory.ServerMods.NoObf.JsonPatch.html) for the authoritative reference.

## When to Use a JSON Patch

Use a JSON patch when the behavior you want is already driven by a **data field** on an existing asset and you only need to add or change that field. Examples:

- Tagging a fluid so the farmland system treats it like salt water.
- Changing drops, damage, material, resistance, or attributes on an existing block/item/entity.
- Disabling a vanilla recipe or asset.

Use **C# / Harmony code patching** instead when the behavior is hardcoded in compiled logic with no data field to flip, or when hundreds of objects would need patching (code patching can be more efficient at scale).

## File Location

Patch files live under the mod's asset domain in a `patches` folder:

```
assets/
└── hotspringsexpanded/
    └── patches/
        └── boilingwater-watertype.json
```

The engine loads every `.json` file in `patches/` during asset loading. File names are **arbitrary** — the file's contents decide what gets patched, not its name — but use sensible names (and subfolders) for organization. Do not place patches in the `game` domain; assets distributed under `game` can be overwritten by other mods sharing the same path and name.

## Patch File Format

A patch file is a **JSON array** of patch instructions. Each instruction is an object:

```json
[
  {
    "file": "game:blocktypes/liquid/boilingwater",
    "op": "addmerge",
    "path": "/attributes/waterTightContainerProps",
    "value": { "...": "..." }
  }
]
```

### Fields

| Field       | Required | Purpose |
|-------------|----------|---------|
| `file`      | yes | Target asset as a domain-qualified `AssetLocation` (e.g. `game:blocktypes/liquid/boilingwater`). The `.json` extension is optional. The base game's `survival`, `game`, and `creative` folders all use the domain `game`. |
| `op`        | yes | The operation (see below). |
| `path`      | yes | A JSON Pointer into the target document (e.g. `/attributes/waterType`, `/drops/-`, `/server/behaviors/10/aitasks/0/damage`). |
| `value`     | for add/replace/addmerge/addeach | The value to insert. Any JSON type (string, number, bool, object, array). |
| `fromPath`  | for move/copy | The source JSON Pointer to move or copy from. |
| `side`      | no | `Client`, `Server`, or `Universal` (default `Universal`). Set this when the target asset only loads on one side to avoid log warnings. |
| `enabled`   | no | `true` (default) or `false`. Set `false` to keep a patch in the file but skip it. |
| `condition` | no | Apply the patch only when a world-config value matches (see Conditional Patches). |
| `dependsOn` | no | Apply the patch only when (or unless) other mods are installed (see Conditional Patches). |

### Operations (`op`)

| `op`       | Behavior |
|------------|----------|
| `add`      | Adds/sets the value at `path`. For an array, `/-` appends and `/N` inserts at index N. **If `path` points at an existing array or object, `add` REPLACES it.** |
| `addmerge` | Like `add`, but if the target is an existing **array** it appends, and if the target is an existing **object** it merges. Safer than `add`; prefer it. Added in 1.19.4. |
| `addeach`  | Inserts **multiple** entries into an array in one operation. Both the `value` and the target must be arrays. Entries are inserted in normal (non-reversed) order. |
| `replace`  | Replaces an existing value at `path`. |
| `remove`   | Removes the value at `path`. |
| `move`     | Moves the value from `fromPath` to `path`. Useful for reordering wildcard map entries. |
| `copy`     | Copies the value from `fromPath` to `path`, merging into an existing destination (array values are appended). |

There is **no** `dummy` op. The valid set is exactly: `add`, `addmerge`, `addeach`, `remove`, `replace`, `move`, `copy`.

### `add` vs `addmerge` (important)

`add` and `addmerge` behave identically when:
- the target does not exist (both create it),
- `path` ends with `-` (both append to the array),
- `path` ends with a number (both insert at that index),
- `path` points at an existing primitive (both replace it).

They differ when the target is an **existing array or object**: `add` overwrites it entirely, while `addmerge` appends (array) or merges (object). Because a future game version may add the array/object you are targeting, `add` can silently clobber vanilla data after an update. **Default to `addmerge`** unless you specifically want replace-on-collision behavior.

## Writing the `path` (JSON Pointer)

The `path` walks the target JSON:
- Object keys are written by name: `/attributes/waterType`.
- Array elements are referenced by **0-based index**: `/server/behaviors/10`.
- `/-` means "append to the end of this array" — preferred over a hard-coded index when other mods may also add entries.

A JSON Pointer **cannot create missing intermediate parents**. If `/attributes` does not exist on the target, patching `/attributes/waterType` fails. In that case, add the parent object instead:

```json
[
  { "file": "game:blocktypes/liquid/boilingwater", "op": "addmerge", "path": "/attributes", "value": { "waterType": "salt" } }
]
```

## Disabling an Asset

Most assets honor an `enabled` flag even when it is not written in the file. To disable one (e.g. remove a vanilla recipe), add it set to `false`:

```json
[
  { "file": "game:recipes/grid/somerecipe", "op": "add", "path": "/enabled", "value": false }
]
```

## Conditional Patches

### Apply only on a side

```json
[
  { "file": "game:blocktypes/liquid/boilingwater", "op": "addmerge", "path": "/attributes/waterType", "value": "salt", "side": "Server" }
]
```

### Apply only if another mod is (or isn't) present — `dependsOn`

`dependsOn` is an array of `{ "modid": string, "invert": bool }`. The patch applies only if every listed mod is installed. Set `"invert": true` on an entry to apply only when that mod is **absent**.

```json
[
  {
    "file": "game:blocktypes/liquid/boilingwater",
    "op": "addmerge",
    "path": "/attributes/waterType",
    "value": "salt",
    "dependsOn": [ { "modid": "somemod" } ]
  }
]
```

### Apply only when a world-config value matches — `condition`

`condition` gates the patch on the currently loaded world config:
- `when` — the world-config key to read (required).
- `isValue` — the value that key must equal for the patch to apply.
- `useValue` — if `true`, the world-config value is substituted in as the patch `value` (instead of comparing). Use one of `isValue` or `useValue`.

```json
[
  {
    "file": "game:blocktypes/liquid/boilingwater",
    "op": "addmerge",
    "path": "/attributes/waterType",
    "value": "salt",
    "condition": { "when": "someWorldConfigKey", "isValue": "true" }
  }
]
```

## Targeting Variant Blocks

Many vanilla assets are **variant blocks** generated from a single base file via `variantgroups` (e.g. `boilingwater-still-7`, `boilingwater-n-3`). Patch the **base definition file** (e.g. `game:blocktypes/liquid/boilingwater`) and every generated variant inherits the change — you generally do not patch each variant individually. You can also extend variant generation itself by appending to `/variantgroups/-`.

## Testing and Debugging Patches

### When the patch errors

The game logs patch failures to the in-game Story log and to `server-main.txt`, and the message is precise. A failed patch reports:
- which patch index failed (patches are 0-based, so "Patch 0" is the first in the file),
- the mod and file the patch came from,
- how far down the `path` it could traverse before the next key was missing, with the JSON found at that point.

Run with `/errorreporter 1` to surface load-time issues on startup.

### When the patch silently does nothing

A patch can "succeed" yet have no effect — typically a wrong `path` or a wrong `value` that the game simply never reads. There is no error in this case. Diagnose by:
1. Checking the load logs for the applied-vs-failed patch summary.
2. Inspecting the loaded asset (base and a few variants) to confirm the field is actually present.
3. Confirming the in-game behavior you expected.

Because a wrong field name fails silently, always confirm the **exact** key and value against the real vanilla asset (shipped under the install's `assets/survival/`, `assets/game/`, `assets/creative/`) before relying on a patch — and re-confirm when targeting a new game version.

## Tooling and Conflicts

- **ModMaker 3000™**: a command-line tool shipped at the top level of the game install. Edit vanilla assets, then run it to generate a set of patches from your changes. Clean up the generated file names/domain before shipping.
- **Mod conflicts**: when two mods patch the same asset, one can override the other, and "patching the patches" does not work. Use the [Compatibility Library](https://wiki.vintagestory.at/Modding:CompatibilityLib) to manage conflicts and dependencies.

## References

- [Vintage Story Wiki — Modding: JSON Patching](https://wiki.vintagestory.at/index.php/Modding:JSON_Patching) (verified for game version 1.21.6)
- [JSON Docs — JsonPatch class](https://apidocs.vintagestory.at/json-docs/jsondocs/Vintagestory.ServerMods.NoObf.JsonPatch.html)
- [JSON Docs — EnumJsonPatchOp](https://apidocs.vintagestory.at/json-docs/jsondocs/Vintagestory.ServerMods.NoObf.EnumJsonPatchOp.html)
- [JSON Docs — PatchCondition](https://apidocs.vintagestory.at/json-docs/jsondocs/Vintagestory.ServerMods.NoObf.PatchCondition.html)
- [JSON Docs — PatchModDependence](https://apidocs.vintagestory.at/json-docs/jsondocs/Vintagestory.ServerMods.NoObf.PatchModDependence.html)
- Source: [JsonPatchLoader.cs](https://github.com/anegostudios/vsessentialsmod/blob/master/Loading/JsonPatchLoader.cs)
