# Apex Shift low-poly placeable structures

Real 3D OBJ assets for Unity, matching the generated reference art style.

## Files

- `storage_box_low_poly.obj` - storage chest / container
- `campfire_low_poly.obj` - fire source prop
- `wall_low_poly.obj` - defensive palisade wall segment
- `trap_low_poly.obj` - spike trap
- `tent_low_poly.obj` - small A-frame survival tent
- `low_poly_placeables.mtl` - shared flat-color materials

## Unity import

1. Copy this folder into `Assets/_Project/Art/Placeables/LowPolyStructures/`.
2. Unity will import each `.obj` with material slots from `low_poly_placeables.mtl`.
3. Create prefabs from each OBJ and add:
   - `PlaceableStructureRuntime`
   - `BoxCollider`
4. Wire the prefabs into `PrefabRegistry > Building Prefabs` using ids:
   - `storage_box`
   - `campfire`
   - `wall`
   - `trap`
   - `tent`

The #42 runtime patch includes primitive fallback visuals, so gameplay works before these OBJ prefabs are assigned.
