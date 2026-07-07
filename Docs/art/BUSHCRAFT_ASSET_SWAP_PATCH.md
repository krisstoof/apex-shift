# Bushcraft asset swap patch

This patch adds an editor-side importer/binder for generated Apex Shift bushcraft assets.

## Source folders

Run the Blender generator locally so one of these folders exists next to the Unity `Assets/` folder:

```text
ApexShift_Bushcraft_Output_v2/
ApexShift_Bushcraft_Output_v2_3_vegetation/
```

Expected generated structure:

```text
ApexShift_Bushcraft_Output_v2/Items/Models/
ApexShift_Bushcraft_Output_v2/Resources/Models/
ApexShift_Bushcraft_Output_v2/Placeables/Models/
```

The tool copies `.fbx`, `.obj`, and `.mtl` files into:

```text
Assets/_Project/Art/Bushcraft/Items/Models/
Assets/_Project/Art/Bushcraft/Resources/Models/
Assets/_Project/Art/Bushcraft/Placeables/Models/
```

## Unity menu

Open Unity and run:

```text
Apex Shift -> Art -> Bushcraft -> Import Generated Assets And Bind PrefabRegistry
```

This does two things:

1. Copies generated models from the local generator output into `Assets/_Project/Art/Bushcraft`.
2. Rebinds `Assets/_Project/Data/World/PrefabRegistry.asset`.

## What gets rebound

### Resource/ecosystem prefabs

The ecosystem/world generator uses `PrefabRegistry.ResourcePrefabs`, so this patch updates resource prefabs, not only building prefabs.

Mappings include:

- `ConiferTree` -> `conifer_tree_stylized`, `conifer_tree_a/b/c_stylized`, `conifer_sapling_a/b/c_stylized`
- `LeafyTree` -> `leafy_tree_stylized`, `leafy_tree_a/b/c_stylized`, `leafy_sapling_a/b/c_stylized`
- `DryTree` -> `dry_tree_stylized`, `dry_tree_a/b/c_stylized`, `dry_sapling_a/b_stylized`
- `Rock` -> `rock_stylized`
- `GreenBush` -> `green_bush_stylized`, `green_bush_a/b/c_stylized`, `forest_shrub_a/b_stylized`
- `DryBush` -> `dry_bush_stylized`, `dry_bush_a/b/c_stylized`
- `GrassOrFlower` -> `grass_or_flower_stylized`, `tall_grass_clump_a/b_stylized`, `wildflower_patch_a/b_stylized`
- `BerryBush` -> `berry_bush_stylized`, `berry_bush_a/b/c_stylized`

### Building/placeable prefabs

The tool also rebinds:

- `campfire`
- `storage_box`
- `tent`
- `wall`
- `trap`

## Important note

This patch does not commit binary generated `.fbx`, `.obj`, or `.blend` assets by itself. It adds the Unity-side importer/binder so the generated files from `ApexShift_Bushcraft_Output_v2` can be copied and wired into the game deterministically.

After running the Unity menu command, commit the imported models and the modified `PrefabRegistry.asset` if you want the actual binary assets stored in Git.
