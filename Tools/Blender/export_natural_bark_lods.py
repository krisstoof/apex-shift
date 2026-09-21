import bpy
from pathlib import Path

BLEND = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\PolishPass3_OldTree\ApexShift_OldTree_Final_NaturalBark.blend")
OUT = BLEND.parent


def main():
    bpy.ops.wm.open_mainfile(filepath=str(BLEND))
    groups = {
        0: [o for o in bpy.data.objects if o.name == "AS_OldTree_TrunkAndBranches" or o.name.startswith("AS_OldTree_Foliage") or o.name.startswith("AS_OldTree_QualityFoliage") or o.name.startswith("AS_OldTree_CrownMass")],
        1: [o for o in bpy.data.objects if o.name.startswith("AS_OldTree_Corrected_LOD1_")],
        2: [o for o in bpy.data.objects if o.name.startswith("AS_OldTree_Corrected_LOD2_")],
    }
    for lod, objects in groups.items():
        bpy.ops.object.select_all(action="DESELECT")
        for obj in objects:
            obj.select_set(True)
        if not objects:
            continue
        bpy.context.view_layer.objects.active = objects[0]
        bpy.ops.export_scene.fbx(filepath=str(OUT / f"ApexShift_OldTree_NaturalBark_LOD{lod}.fbx"), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False)
        bpy.ops.wm.obj_export(filepath=str(OUT / f"ApexShift_OldTree_NaturalBark_LOD{lod}.obj"), export_selected_objects=True, forward_axis="NEGATIVE_Z", up_axis="Y")
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
    print("Exported natural bark LOD0-LOD2")


if __name__ == "__main__":
    main()
