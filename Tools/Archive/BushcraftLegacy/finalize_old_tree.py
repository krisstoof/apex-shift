import bpy
import json
import math
from pathlib import Path
from mathutils import Vector


INPUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\Checkpoint3_Foliage\ApexShift_OldTree_Checkpoint3_Foliage.blend")
OUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\Final_OldTree")
OUT.mkdir(parents=True, exist_ok=True)
BLEND = OUT / "ApexShift_OldTree_Final.blend"


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def create_bark_material():
    mat = bpy.data.materials.get("M_OldTree_Bark_Final") or bpy.data.materials.new("M_OldTree_Bark_Final")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 4.8
    noise.inputs["Detail"].default_value = 6.0
    noise.inputs["Roughness"].default_value = 0.78
    mapping = nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (1.5, 1.5, 0.42)
    tex = nodes.new("ShaderNodeTexCoord")
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.025, 0.006, 0.002, 1)
    ramp.color_ramp.elements[0].position = 0.25
    ramp.color_ramp.elements[1].color = (0.22, 0.055, 0.012, 1)
    ramp.color_ramp.elements[1].position = 0.78
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.32
    bump.inputs["Distance"].default_value = 0.075
    bsdf.inputs["Roughness"].default_value = 0.9
    links.new(tex.outputs["Generated"], mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def improve_materials():
    bark = create_bark_material()
    trunk = bpy.data.objects.get("AS_OldTree_TrunkAndBranches")
    trunk.data.materials.clear()
    trunk.data.materials.append(bark)
    # Add subtle variation to foliage materials without changing topology.
    for obj in bpy.context.scene.objects:
        if obj.name == "AS_OldTree_Foliage":
            for mat in obj.data.materials:
                if mat:
                    mat.use_nodes = True
                    bsdf = mat.node_tree.nodes.get("Principled BSDF")
                    if bsdf:
                        bsdf.inputs["Roughness"].default_value = 0.92
    return trunk


def improve_lighting():
    scene = bpy.context.scene
    world = scene.world or bpy.data.worlds.new("Final_World")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (0.012, 0.022, 0.035, 1)
    bg.inputs["Strength"].default_value = 0.28
    key = bpy.data.objects.get("Checkpoint1_Key")
    fill = bpy.data.objects.get("Checkpoint1_Fill")
    if key:
        key.data.energy = 1350
        key.data.color = (1.0, 0.72, 0.48)
        key.location = (5.8, -6.2, 8.8)
        look_at(key, (0, 0, 3.0))
    if fill:
        fill.data.energy = 720
        fill.data.color = (0.45, 0.62, 1.0)
        fill.location = (-5.0, -3.0, 5.2)
        look_at(fill, (0, 0, 2.8))
    rim_data = bpy.data.lights.get("Final_Rim_Data") or bpy.data.lights.new("Final_Rim_Data", "AREA")
    rim_data.energy = 950
    rim_data.color = (0.38, 0.55, 1.0)
    rim_data.shape = "DISK"
    rim_data.size = 3.0
    rim = bpy.data.objects.get("Final_Rim") or bpy.data.objects.new("Final_Rim", rim_data)
    if not rim.users_collection:
        scene.collection.objects.link(rim)
    rim.location = (-4.0, 5.0, 7.5)
    look_at(rim, (0, 0, 3.4))


def ensure_collection(name):
    coll = bpy.data.collections.get(name)
    if coll is None:
        coll = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(coll)
    return coll


def move_to(obj, coll):
    for old in list(obj.users_collection):
        old.objects.unlink(obj)
    coll.objects.link(obj)


def create_lods():
    trunk = bpy.data.objects["AS_OldTree_TrunkAndBranches"]
    foliage = bpy.data.objects["AS_OldTree_Foliage"]
    source_collection = trunk.users_collection[0]
    lod_collections = [ensure_collection(f"AS_OldTree_LOD{i}") for i in range(3)]
    move_to(trunk, lod_collections[0])
    move_to(foliage, lod_collections[0])
    results = []
    for i, ratio in enumerate((1.0, 0.48, 0.18)):
        if i == 0:
            t, f = trunk, foliage
        else:
            t = trunk.copy(); t.data = trunk.data.copy(); t.name = f"AS_OldTree_LOD{i}_TrunkAndBranches"; lod_collections[i].objects.link(t)
            f = foliage.copy(); f.data = foliage.data.copy(); f.name = f"AS_OldTree_LOD{i}_Foliage"; lod_collections[i].objects.link(f)
            for obj, r in ((t, ratio), (f, max(0.12, ratio))):
                mod = obj.modifiers.new(f"LOD{i}_Decimate", "DECIMATE")
                mod.ratio = r
                bpy.context.view_layer.objects.active = obj
                obj.select_set(True)
                bpy.ops.object.modifier_apply(modifier=mod.name)
                obj.select_set(False)
        results.append((i, t, f, sum(max(0, len(p.vertices)-2) for p in t.data.polygons) + sum(max(0, len(p.vertices)-2) for p in f.data.polygons)))
    return results


def export_lods(lods):
    reports = []
    for i, trunk, foliage, tris in lods:
        for obj in bpy.context.selected_objects:
            obj.select_set(False)
        trunk.select_set(True); foliage.select_set(True)
        bpy.context.view_layer.objects.active = trunk
        fbx = OUT / f"ApexShift_OldTree_LOD{i}.fbx"
        obj_path = OUT / f"ApexShift_OldTree_LOD{i}.obj"
        bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False)
        if hasattr(bpy.ops.wm, "obj_export"):
            bpy.ops.wm.obj_export(filepath=str(obj_path), export_selected_objects=True, forward_axis="NEGATIVE_Z", up_axis="Y")
        else:
            bpy.ops.export_scene.obj(filepath=str(obj_path), use_selection=True)
        reports.append({"lod": i, "tris": tris, "fbx": str(fbx), "obj": str(obj_path)})
    bpy.ops.object.select_all(action="DESELECT")
    return reports


def render_final():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1000
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    cam = bpy.data.objects.get("Checkpoint1_Camera")
    cam.location = (10.5, -12.0, 7.0)
    look_at(cam, (0, 0.15, 3.0))
    path = OUT / "ApexShift_OldTree_Final_Render.png"
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    return path


def main():
    bpy.ops.wm.open_mainfile(filepath=str(INPUT))
    trunk = improve_materials()
    improve_lighting()
    lods = create_lods()
    exports = export_lods(lods)
    render = render_final()
    report = {"asset": "ApexShift Old Tree", "lods": [{"lod": i, "tris": tris} for i, _, _, tris in lods], "exports": exports, "render": str(render), "trunk_material": "M_OldTree_Bark_Final"}
    (OUT / "ApexShift_OldTree_Final_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
    print("\n=== FINALIZATION COMPLETE ===")
    print(json.dumps(report, indent=2))
    print("BLEND:", BLEND)


if __name__ == "__main__":
    main()
