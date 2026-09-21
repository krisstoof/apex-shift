import bpy
import json
import math
import random
from pathlib import Path
from mathutils import Vector


INPUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\Final_OldTree\ApexShift_OldTree_Final.blend")
OUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\QualityPass_OldTree")
OUT.mkdir(parents=True, exist_ok=True)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def bark_material():
    mat = bpy.data.materials.get("M_OldTree_Bark_QualityPass") or bpy.data.materials.new("M_OldTree_Bark_QualityPass")
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    tex = nodes.new("ShaderNodeTexCoord")
    mapping = nodes.new("ShaderNodeMapping")
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 5.5
    noise.inputs["Detail"].default_value = 7.0
    noise.inputs["Roughness"].default_value = 0.76
    mapping.inputs["Scale"].default_value = (2.0, 2.0, 0.32)
    wave = nodes.new("ShaderNodeTexWave")
    wave.wave_type = "BANDS"
    wave.bands_direction = "Z"
    wave.inputs["Scale"].default_value = 7.0
    wave.inputs["Distortion"].default_value = 5.0
    wave.inputs["Detail"].default_value = 4.0
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.018, 0.004, 0.001, 1)
    ramp.color_ramp.elements[0].position = 0.24
    ramp.color_ramp.elements[1].color = (0.16, 0.032, 0.008, 1)
    ramp.color_ramp.elements[1].position = 0.78
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.48
    bump.inputs["Distance"].default_value = 0.10
    rough = nodes.new("ShaderNodeMapRange")
    rough.inputs["From Min"].default_value = 0.0
    rough.inputs["From Max"].default_value = 1.0
    rough.inputs["To Min"].default_value = 0.76
    rough.inputs["To Max"].default_value = 0.97
    links.new(tex.outputs["Generated"], mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(wave.outputs["Color"], bump.inputs["Height"])
    links.new(noise.outputs["Fac"], rough.inputs["Value"])
    links.new(rough.outputs["Result"], bsdf.inputs["Roughness"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def apply_root_correction(trunk):
    # Compress the small, evenly spaced star-like roots while preserving 2-3 dominant buttresses.
    dominant = [0.15, 2.35, 4.0]
    for v in trunk.data.vertices:
        p = v.co
        if p.z < 0.70 and math.hypot(p.x, p.y) > 0.55:
            angle = math.atan2(p.y, p.x)
            nearest = min(abs(math.atan2(math.sin(angle - a), math.cos(angle - a))) for a in dominant)
            keep = 0.98 if nearest < 0.42 else (0.82 if nearest < 0.85 else 0.62)
            height_fade = max(0.0, min(1.0, (0.70 - p.z) / 0.70))
            factor = 1.0 - (1.0 - keep) * height_fade
            p.x *= factor
            p.y *= factor
    trunk.data.update()
    trunk.data.materials.clear()
    trunk.data.materials.append(bark_material())
    displace = trunk.modifiers.new("Subtle_Bark_Form_Breakup", "DISPLACE")
    tex = bpy.data.textures.new("OldTree_Bark_Large_Grooves", type="CLOUDS")
    tex.noise_scale = 0.42
    tex.noise_depth = 2
    displace.texture = tex
    displace.strength = 0.035
    displace.mid_level = 0.52
    displace.texture_coords = "GLOBAL"
    bpy.context.view_layer.objects.active = trunk
    bpy.ops.object.modifier_apply(modifier=displace.name)
    return trunk


def add_segment(p0, p1, r0, r1, parts, name):
    p0, p1 = Vector(p0), Vector(p1)
    d = p1 - p0
    bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=r0, radius2=r1, depth=d.length, location=(p0 + p1) * 0.5)
    o = bpy.context.object
    o.name = name
    o.rotation_mode = "QUATERNION"
    o.rotation_quaternion = d.to_track_quat("Z", "Y")
    o.rotation_mode = "XYZ"
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    parts.append(o)


def add_joint(p, scale, parts, name):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1, location=p)
    o = bpy.context.object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    parts.append(o)


def add_character_branch(trunk):
    parts = [trunk]
    points = [(-0.18, 0.12, 4.25), (-0.52, 0.28, 4.82), (-0.95, 0.38, 5.32), (-1.22, 0.44, 5.72)]
    radii = [0.28, 0.19, 0.105, 0.035]
    for i in range(len(points) - 1):
        add_segment(points[i], points[i + 1], radii[i], radii[i + 1], parts, f"_quality_branch_{i}")
    add_joint(points[1], (0.21, 0.18, 0.19), parts, "_quality_branch_junction")
    # Strong broken limb with a heavy base and angled cut end.
    broken = [(0.44, 0.08, 2.62), (1.02, 0.20, 2.92), (1.48, 0.38, 3.34), (1.78, 0.56, 3.62)]
    br = [0.30, 0.22, 0.13, 0.075]
    for i in range(len(broken) - 1):
        add_segment(broken[i], broken[i + 1], br[i], br[i + 1], parts, f"_quality_broken_{i}")
    add_joint(broken[-1], (0.13, 0.11, 0.10), parts, "_quality_broken_end")
    bpy.ops.object.select_all(action="DESELECT")
    for o in parts:
        o.select_set(True)
    bpy.context.view_layer.objects.active = trunk
    bpy.ops.object.join()
    trunk = bpy.context.object
    trunk.name = "AS_OldTree_TrunkAndBranches"
    remesh = trunk.modifiers.new("QualityPass_Organic_Junctions", "REMESH")
    remesh.mode = "VOXEL"
    remesh.voxel_size = 0.068
    remesh.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    trunk.data.materials.clear()
    trunk.data.materials.append(bark_material())
    for p in trunk.data.polygons:
        p.use_smooth = True
    return trunk


def add_foliage_pass():
    anchors = [
        ((-2.52, 0.45, 4.86), (0.62, 0.48, 0.45), 26),
        ((-1.84, -0.58, 5.10), (0.54, 0.43, 0.42), 22),
        ((-2.18, 1.10, 5.38), (0.52, 0.42, 0.42), 21),
        ((-0.46, 2.00, 5.66), (0.56, 0.44, 0.43), 22),
        ((-0.86, 0.35, 5.72), (0.64, 0.48, 0.50), 27),
        ((2.43, -0.55, 4.90), (0.62, 0.48, 0.45), 27),
        ((2.35, -1.20, 5.47), (0.50, 0.40, 0.40), 20),
        ((1.12, 0.66, 5.32), (0.58, 0.45, 0.44), 23),
        ((0.48, 2.00, 5.25), (0.54, 0.42, 0.43), 22),
        ((0.88, 0.18, 5.80), (0.62, 0.48, 0.47), 25),
        ((-0.02, 0.10, 5.52), (0.72, 0.52, 0.56), 28),
    ]
    rng = random.Random(92341)
    verts, faces, mids = [], [], []
    for anchor, spread, count in anchors:
        center = Vector(anchor)
        for _ in range(count):
            d = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-0.55, 1.0))).normalized()
            c = center + Vector((rng.uniform(-spread[0], spread[0]), rng.uniform(-spread[1], spread[1]), rng.uniform(-spread[2], spread[2]) + 0.10))
            length = rng.uniform(0.24, 0.40)
            width = length * rng.uniform(0.44, 0.64)
            side = d.cross(Vector((0, 0, 1)))
            if side.length < 0.1:
                side = d.cross(Vector((0, 1, 0)))
            side.normalize()
            axis = d * length
            start = len(verts)
            verts += [tuple(c - axis * 0.48), tuple(c + side * width * 0.5 - axis * 0.04), tuple(c + axis * 0.52), tuple(c - side * width * 0.5 - axis * 0.04)]
            faces += [(start, start + 1, start + 2), (start, start + 2, start + 3)]
            mids += [rng.randrange(3), rng.randrange(3)]
    mesh = bpy.data.meshes.new("AS_OldTree_QualityFoliageMesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("AS_OldTree_QualityFoliage", mesh)
    bpy.context.scene.collection.objects.link(obj)
    for name, color in [("M_Leaf_Deep", (0.025, 0.11, 0.012, 1)), ("M_Leaf_Green", (0.07, 0.24, 0.02, 1)), ("M_Leaf_Sun", (0.19, 0.38, 0.045, 1))]:
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = color
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = 0.9
        mesh.materials.append(mat)
    for p, mi in zip(mesh.polygons, mids):
        p.material_index = mi
    return obj


def render(path, location):
    scene = bpy.context.scene
    cam = bpy.data.objects.get("Checkpoint1_Camera")
    cam.location = location
    look_at(cam, (0, 0.15, 3.15))
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1000
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def remove_old_lods():
    for obj in list(bpy.data.objects):
        if obj.name.startswith("AS_OldTree_LOD1_") or obj.name.startswith("AS_OldTree_LOD2_"):
            bpy.data.objects.remove(obj, do_unlink=True)


def export_updated_lods(trunk, foliage_objects):
    results = []
    for lod, ratio in [(0, 1.0), (1, 0.46), (2, 0.18)]:
        selected = []
        for source in [trunk] + foliage_objects:
            obj = source if lod == 0 else source.copy()
            if lod != 0:
                obj.data = source.data.copy()
                obj.name = f"AS_OldTree_LOD{lod}_{source.name}"
                bpy.context.scene.collection.objects.link(obj)
                mod = obj.modifiers.new("LOD_Decimate", "DECIMATE")
                mod.ratio = max(0.10, ratio)
                bpy.context.view_layer.objects.active = obj
                bpy.ops.object.modifier_apply(modifier=mod.name)
            obj.select_set(True)
            selected.append(obj)
        bpy.context.view_layer.objects.active = selected[0]
        fbx = OUT / f"ApexShift_OldTree_QualityPass_LOD{lod}.fbx"
        obj_path = OUT / f"ApexShift_OldTree_QualityPass_LOD{lod}.obj"
        bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False)
        bpy.ops.wm.obj_export(filepath=str(obj_path), export_selected_objects=True, forward_axis="NEGATIVE_Z", up_axis="Y")
        tris = sum(sum(max(0, len(p.vertices) - 2) for p in o.data.polygons) for o in selected)
        results.append({"lod": lod, "tris": tris, "fbx": str(fbx), "obj": str(obj_path)})
        bpy.ops.object.select_all(action="DESELECT")
    return results


def main():
    bpy.ops.wm.open_mainfile(filepath=str(INPUT))
    trunk = bpy.data.objects["AS_OldTree_TrunkAndBranches"]
    foliage = bpy.data.objects.get("AS_OldTree_Foliage")
    trunk = apply_root_correction(trunk)
    render(OUT / "pass1_trunk_roots_3_4.png", (10.5, -12.0, 7.0))
    trunk = add_character_branch(trunk)
    render(OUT / "pass2_branches_3_4.png", (10.5, -12.0, 7.0))
    render(OUT / "pass2_branches_side.png", (14.0, 0.0, 4.3))
    extra_foliage = add_foliage_pass()
    foliage_objects = [o for o in [foliage, extra_foliage] if o]
    render(OUT / "final_quality_pass_3_4.png", (10.5, -12.0, 7.0))
    remove_old_lods()
    exports = export_updated_lods(trunk, foliage_objects)
    report = {
        "base_file": str(INPUT),
        "restart": False,
        "changes": {
            "trunk": "large-form displacement, stronger vertical bark material, subtle surface breakup",
            "roots": "compressed uniform radial tips; dominant buttresses preserved",
            "branches": "added asymmetrical character branch and heavier broken limb",
            "foliage": "expanded asymmetric clusters anchored to existing branch-end zones",
            "material": "procedural bark with color variation, roughness variation and vertical groove bump",
        },
        "exports": exports,
        "renders": [str(p) for p in [OUT / "pass1_trunk_roots_3_4.png", OUT / "pass2_branches_3_4.png", OUT / "pass2_branches_side.png", OUT / "final_quality_pass_3_4.png"]],
    }
    (OUT / "ApexShift_OldTree_QualityPass_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "ApexShift_OldTree_QualityPass.blend"))
    print("\n=== QUALITY PASS COMPLETE ===")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
