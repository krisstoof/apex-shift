import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

INPUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\QualityPass_OldTree\ApexShift_OldTree_QualityPass.blend")
OUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\PolishPass3_OldTree")
OUT.mkdir(parents=True, exist_ok=True)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def compact_root_skirt(trunk):
    # A second, restrained pass: shorten minor tips while preserving three heavy buttresses.
    dominant = [0.10, 2.35, 4.05]
    for v in trunk.data.vertices:
        p = v.co
        radius = math.hypot(p.x, p.y)
        if p.z < 0.48 and radius > 0.72:
            angle = math.atan2(p.y, p.x)
            nearest = min(abs(math.atan2(math.sin(angle - a), math.cos(angle - a))) for a in dominant)
            factor = 1.0 if nearest < 0.42 else (0.72 if nearest < 0.90 else 0.48)
            p.x *= factor
            p.y *= factor
    trunk.data.update()


def add_crown_mass(name, center, spread, count, mats, seed):
    import random
    rng = random.Random(seed)
    verts, faces, mids = [], [], []
    center = Vector(center)
    for _ in range(count):
        # Elongated leaf groups follow the branch direction instead of forming spheres.
        c = center + Vector((rng.uniform(-spread[0], spread[0]), rng.uniform(-spread[1], spread[1]), rng.uniform(-spread[2], spread[2])))
        axis = Vector((rng.uniform(-0.3, 0.3), rng.uniform(-0.3, 0.3), rng.uniform(0.15, 0.9))).normalized()
        length = rng.uniform(0.25, 0.42)
        width = length * rng.uniform(0.45, 0.65)
        side = axis.cross(Vector((0, 0, 1)))
        if side.length < 0.1:
            side = axis.cross(Vector((0, 1, 0)))
        side.normalize()
        start = len(verts)
        verts += [tuple(c - axis * length * 0.5), tuple(c + side * width * 0.5), tuple(c + axis * length * 0.5), tuple(c - side * width * 0.5)]
        faces += [(start, start + 1, start + 2), (start, start + 2, start + 3)]
        mids += [rng.randrange(3), rng.randrange(3)]
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    for mat in mats:
        mesh.materials.append(mat)
    for poly, mi in zip(mesh.polygons, mids):
        poly.material_index = mi
    return obj


def strengthen_foliage():
    mats = []
    for name, color in [("M_Polish3_Leaf_Deep", (0.018, 0.09, 0.008, 1)), ("M_Polish3_Leaf_Green", (0.055, 0.19, 0.018, 1)), ("M_Polish3_Leaf_Light", (0.14, 0.31, 0.035, 1))]:
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = color
        mat.use_nodes = True
        mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = color
        mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.92
        mats.append(mat)
    created = []
    created.append(add_crown_mass("AS_OldTree_CrownMass_Left", (-1.85, 0.35, 5.18), (0.52, 0.38, 0.38), 26, mats, 101))
    created.append(add_crown_mass("AS_OldTree_CrownMass_Right", (1.95, -0.42, 5.12), (0.56, 0.42, 0.40), 29, mats, 102))
    created.append(add_crown_mass("AS_OldTree_CrownMass_Top", (0.05, 0.18, 5.78), (0.58, 0.42, 0.45), 31, mats, 103))
    created.append(add_crown_mass("AS_OldTree_CrownMass_Back", (0.20, 1.42, 5.20), (0.44, 0.34, 0.36), 21, mats, 104))
    return created


def remove_previous_polish_lods():
    for obj in list(bpy.data.objects):
        if obj.name.startswith("AS_OldTree_Polish3_LOD"):
            bpy.data.objects.remove(obj, do_unlink=True)


def render_final():
    scene = bpy.context.scene
    cam = bpy.data.objects.get("Checkpoint1_Camera")
    cam.location = (10.8, -12.6, 7.2)
    look_at(cam, (0.0, 0.15, 3.2))
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1100
    scene.render.resolution_y = 1100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(OUT / "ApexShift_OldTree_PolishPass3_Render.png")
    bpy.ops.render.render(write_still=True)


def export_lods(trunk, foliage):
    remove_previous_polish_lods()
    results = []
    for lod, ratio in [(0, 1.0), (1, 0.43), (2, 0.16)]:
        sources = [trunk] + foliage
        selected = []
        for source in sources:
            if lod == 0:
                obj = source
            else:
                obj = source.copy()
                obj.data = source.data.copy()
                obj.name = f"AS_OldTree_Polish3_LOD{lod}_{source.name}"
                bpy.context.scene.collection.objects.link(obj)
                mod = obj.modifiers.new("Polish3_Decimate", "DECIMATE")
                mod.ratio = max(0.08, ratio)
                bpy.context.view_layer.objects.active = obj
                bpy.ops.object.modifier_apply(modifier=mod.name)
            obj.select_set(True)
            selected.append(obj)
        bpy.context.view_layer.objects.active = selected[0]
        fbx = OUT / f"ApexShift_OldTree_Polish3_LOD{lod}.fbx"
        obj_path = OUT / f"ApexShift_OldTree_Polish3_LOD{lod}.obj"
        bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False)
        bpy.ops.wm.obj_export(filepath=str(obj_path), export_selected_objects=True, forward_axis="NEGATIVE_Z", up_axis="Y")
        tris = sum(sum(max(0, len(p.vertices) - 2) for p in o.data.polygons) for o in selected)
        results.append({"lod": lod, "tris": tris, "fbx": str(fbx), "obj": str(obj_path)})
        bpy.ops.object.select_all(action="DESELECT")
    return results


def main():
    bpy.ops.wm.open_mainfile(filepath=str(INPUT))
    trunk = bpy.data.objects["AS_OldTree_TrunkAndBranches"]
    compact_root_skirt(trunk)
    extra = strengthen_foliage()
    all_foliage = [o for o in bpy.data.objects if o.name.startswith("AS_OldTree_Foliage") or o.name.startswith("AS_OldTree_QualityFoliage") or o.name.startswith("AS_OldTree_CrownMass")]
    render_final()
    exports = export_lods(trunk, all_foliage)
    report = {"base_file": str(INPUT), "restart": False, "changes": ["compacted minor root tips", "added four branch-following foliage masses", "preserved trunk, branches and existing foliage"], "exports": exports, "render": str(OUT / "ApexShift_OldTree_PolishPass3_Render.png")}
    (OUT / "ApexShift_OldTree_PolishPass3_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "ApexShift_OldTree_PolishPass3.blend"))
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
