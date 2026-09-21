import bpy
import json
import math
import random
from pathlib import Path
from mathutils import Vector

INPUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\PolishPass3_OldTree\ApexShift_OldTree_Final_NaturalBark.blend")
OUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\HandPaintedPass_OldTree")
OUT.mkdir(parents=True, exist_ok=True)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def make_material(name, color, roughness=0.88):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = roughness
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    mat.diffuse_color = (*color, 1)
    return mat


def handpainted_bark_palette():
    return [
        make_material("M_Bark_Painted_Dark", (0.035, 0.008, 0.003), 0.94),
        make_material("M_Bark_Painted_Mid", (0.12, 0.028, 0.008), 0.91),
        make_material("M_Bark_Painted_Warm", (0.26, 0.075, 0.018), 0.88),
        make_material("M_Bark_Painted_Highlight", (0.39, 0.13, 0.035), 0.86),
    ]


def assign_painted_bark(trunk):
    mats = handpainted_bark_palette()
    trunk.data.materials.clear()
    for mat in mats:
        trunk.data.materials.append(mat)
    # Deliberate broad paint zones: large vertical strokes and a few controlled dark breaks.
    for poly in trunk.data.polygons:
        c = poly.center
        angle = math.atan2(c.y, c.x)
        sector = (angle + math.pi) / (2 * math.pi)
        broad = int(sector * 5.0) % 4
        if c.z < 0.65 and (c.x * c.x + c.y * c.y) > 0.5:
            idx = 1 if broad in (1, 2) else 0
        elif c.z > 4.2 and c.x < -0.35:
            idx = 2
        elif c.z > 3.0 and c.y > 0.20:
            idx = 3 if broad == 2 else 2
        elif c.z < 2.0 and c.x > 0.30:
            idx = 1
        else:
            idx = 1 if broad in (0, 3) else 2
        poly.material_index = idx
    return mats


def make_leaf_palette():
    return [
        make_material("M_Leaf_Painterly_Deep", (0.018, 0.075, 0.008), 0.94),
        make_material("M_Leaf_Painterly_Green", (0.055, 0.18, 0.014), 0.92),
        make_material("M_Leaf_Painterly_Olive", (0.16, 0.29, 0.025), 0.90),
        make_material("M_Leaf_Painterly_Sun", (0.28, 0.40, 0.05), 0.88),
    ]


def create_painterly_mass(name, center, spread, count, seed, mats):
    rng = random.Random(seed)
    verts, faces, mids = [], [], []
    center = Vector(center)
    for i in range(count):
        # Hand-directed oblong clumps with an open center, rather than a spherical blob.
        t = i / max(1, count - 1)
        side_bias = -1.0 if i % 3 == 0 else (1.0 if i % 3 == 1 else 0.0)
        c = center + Vector((
            side_bias * spread[0] * 0.38 + rng.uniform(-spread[0] * 0.55, spread[0] * 0.55),
            rng.uniform(-spread[1], spread[1]),
            (t - 0.5) * spread[2] * 0.55 + rng.uniform(-spread[2] * 0.45, spread[2] * 0.45),
        ))
        direction = Vector((side_bias * 0.45 + rng.uniform(-0.32, 0.32), rng.uniform(-0.25, 0.25), rng.uniform(0.18, 0.86))).normalized()
        length = rng.uniform(0.25, 0.42)
        width = length * rng.uniform(0.45, 0.68)
        side = direction.cross(Vector((0, 0, 1)))
        if side.length < 0.1:
            side = direction.cross(Vector((0, 1, 0)))
        side.normalize()
        start = len(verts)
        verts += [tuple(c - direction * length * 0.5), tuple(c + side * width * 0.5), tuple(c + direction * length * 0.5), tuple(c - side * width * 0.5)]
        faces += [(start, start + 1, start + 2), (start, start + 2, start + 3)]
        base = 1 if i % 5 else 0
        mids += [min(3, base + rng.randrange(2)), min(3, base + rng.randrange(2))]
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


def rebuild_crown_masses():
    mats = make_leaf_palette()
    # Keep existing foliage and add three designed masses to make 7 readable crown groups.
    masses = [
        ("AS_OldTree_PaintedMass_Left", (-1.92, 0.32, 5.22), (0.68, 0.42, 0.52), 34, 701),
        ("AS_OldTree_PaintedMass_Right", (1.96, -0.38, 5.16), (0.72, 0.45, 0.48), 36, 702),
        ("AS_OldTree_PaintedMass_Top", (0.00, 0.18, 5.82), (0.74, 0.43, 0.54), 38, 703),
    ]
    return [create_painterly_mass(*spec, mats) for spec in masses]


def render(name, location, target):
    scene = bpy.context.scene
    cam = bpy.data.objects["Checkpoint1_Camera"]
    cam.location = location
    look_at(cam, target)
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1100
    scene.render.resolution_y = 1100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    path = OUT / name
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    return path


def export_asset(trunk, foliage):
    exports = []
    for lod, ratio in [(0, 1.0), (1, 0.45), (2, 0.17)]:
        selected = []
        for source in [trunk] + foliage:
            obj = source if lod == 0 else source.copy()
            if lod != 0:
                obj.data = source.data.copy()
                obj.name = f"AS_OldTree_HandPainted_LOD{lod}_{source.name}"
                bpy.context.scene.collection.objects.link(obj)
                mod = obj.modifiers.new("HandPainted_LOD_Decimate", "DECIMATE")
                mod.ratio = max(0.08, ratio)
                bpy.context.view_layer.objects.active = obj
                bpy.ops.object.modifier_apply(modifier=mod.name)
            obj.select_set(True)
            selected.append(obj)
        bpy.context.view_layer.objects.active = selected[0]
        fbx = OUT / f"ApexShift_OldTree_HandPainted_LOD{lod}.fbx"
        obj_path = OUT / f"ApexShift_OldTree_HandPainted_LOD{lod}.obj"
        bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False)
        bpy.ops.wm.obj_export(filepath=str(obj_path), export_selected_objects=True, forward_axis="NEGATIVE_Z", up_axis="Y")
        tris = sum(sum(max(0, len(p.vertices) - 2) for p in o.data.polygons) for o in selected)
        exports.append({"lod": lod, "tris": tris, "fbx": str(fbx), "obj": str(obj_path)})
        bpy.ops.object.select_all(action="DESELECT")
    return exports


def main():
    bpy.ops.wm.open_mainfile(filepath=str(INPUT))
    trunk = bpy.data.objects["AS_OldTree_TrunkAndBranches"]
    assign_painted_bark(trunk)
    form_render = render("01_form_3_4.png", (10.8, -12.6, 7.2), (0, 0.15, 3.0))
    close_render = render("02_trunk_handpainted_close.png", (5.6, -7.0, 4.0), (0.0, 0.0, 2.9))
    masses = rebuild_crown_masses()
    all_foliage = [o for o in bpy.data.objects if o.name.startswith("AS_OldTree_Foliage") or o.name.startswith("AS_OldTree_QualityFoliage") or o.name.startswith("AS_OldTree_CrownMass") or o.name.startswith("AS_OldTree_PaintedMass")]
    front = render("03_crown_front.png", (0.0, -14.0, 4.6), (0, 0.15, 3.25))
    low = render("04_crown_low_angle.png", (9.4, -11.0, 2.3), (0, 0.25, 4.0))
    final = render("05_final_handpainted_3_4.png", (10.8, -12.6, 7.2), (0, 0.15, 3.0))
    exports = export_asset(trunk, all_foliage)
    report = {
        "base_file": str(INPUT),
        "restart": False,
        "changes": {
            "trunk": "broad hand-directed material zones and preserved large sculptural forms",
            "roots": "preserved current varied root skirt and heavy base silhouette",
            "branches": "preserved current asymmetrical hierarchy and broken limb",
            "crown": "added three designed painterly masses; total crown reads as seven major groups",
            "hand_painted_feel": "flat controlled bark palette, limited noise, broad dark/mid/warm/highlight paint zones",
            "material": "replaced procedural bark pattern with hand-painted style palette",
        },
        "renders": [str(p) for p in [form_render, close_render, front, low, final]],
        "exports": exports,
        "remaining": "Optional next pass: author a painted bark atlas/vertex-color texture for even more concept-art specificity.",
    }
    (OUT / "ApexShift_OldTree_HandPainted_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "ApexShift_OldTree_HandPainted.blend"))
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
