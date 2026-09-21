# Bushcraft v9 Full Pack Fixed — generation QA

Generated on 2026-07-19 through Blender MCP into an isolated Unity snapshot.

## Package completeness

- Manifest records: **98**
- Items: **22**
- Placeables: **12**
- Resources: **56**
- Creatures: **3**
- Landmarks: **5**
- BLEND files: **98**
- FBX files: **98**
- OBJ/MTL pairs: **98**
- Preview PNG files: **98**
- Empty generated files: **0**
- Total package size: approximately **473.5 MB**

The snapshot does not replace `Assets/_Project/Art/Bushcraft` and does not modify `PrefabRegistry.asset`.

## Blender 5.1 compatibility fixes applied during generation

The supplied v9 script could not complete without session-level compatibility fixes. The generation run added:

- `fiber` as an alias of the existing `rope` material;
- a missing `fire_cluster` helper used by torch and campfire generators;
- a missing `primitive_torus` helper used by `snare_trap`.

These fixes affected only the Blender MCP generation session; the source file in `Downloads` was not modified.

## Bounds warnings

The generated manifest reports `FAILED_BOUNDS` for **30/98** assets:

`wood`, `fiber`, `hide`, `spear`, `bow`, `axe`, `pickaxe`, `storage_box`, `leafy_tree`, `green_bush`, `leafy_tree_a`, `leafy_tree_c`, `dry_tree_c`, `ruins_landmark`, `pond_landmark`, `camp_landmark`, `cave_landmark`, `kindling_bundle`, `flint_shard`, `reed_bundle`, `arrow_bundle`, `arrow`, `spear_heavy`, `bow_short`, `axe_heavy`, `lean_to_shelter`, `tanning_rack`, `drying_rack`, `rock_cluster_small`, `mushroom_patch_b`.

Exact required and exported bounds are preserved in `bushcraft_v9_manifest.json`.

## Visual QA findings

- `bow` has a readable continuous bow silhouette and visible string.
- `tent` has a readable frame and dense overlapping roof covering.
- `spear` has a transversely oriented stone head and reads closer to a small pickaxe; `spear_heavy` inherits this issue.
- `torch_unlit` incorrectly contains a flame because its catalog entry reuses `gen_torch_realistic`.
- `small_prey` reads as a very simplified round rabbit and needs an anatomy pass before production use.
- The art direction remains stylized/hand-painted rather than photorealistic.
- Procedural Blender material detail is not baked, so Unity FBX/OBJ appearance may differ from the rendered previews.

## Promotion recommendation

Keep this folder as a review snapshot. Before copying assets into the runtime-owned Bushcraft directory, correct the spear family, make `torch_unlit` genuinely unlit, review all bounds warnings, verify Unity materials/scales, and add or validate colliders and LODs.
