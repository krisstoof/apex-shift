import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

INPUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\HandPaintedPass_OldTree\ApexShift_OldTree_HandPainted.blend")
OUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\PaintedTexturePass_OldTree")
OUT.mkdir(parents=True, exist_ok=True)


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def make_painted_image():
    size = 512
    image = bpy.data.images.get("OldTree_Bark_HandPainted_Atlas") or bpy.data.images.new("OldTree_Bark_HandPainted_Atlas", width=size, height=size)
    pixels = [0.0] * (size * size * 4)
    # Broad, deliberate painted strokes; no high-frequency procedural pattern.
    strokes = [
        (0.08, (0.045, 0.012, 0.004)), (0.19, (0.16, 0.040, 0.010)),
        (0.31, (0.075, 0.015, 0.004)), (0.43, (0.23, 0.065, 0.015)),
        (0.57, (0.095, 0.022, 0.006)), (0.68, (0.19, 0.050, 0.012)),
        (0.81, (0.055, 0.012, 0.004)), (0.92, (0.135, 0.032, 0.008)),
    ]
    for y in range(size):
        v = y / (size - 1)
        for x in range(size):
            u = x / (size - 1)
            nearest = min(abs(u - s[0]) for s in strokes)
            stroke = min(strokes, key=lambda s: abs(u - s[0]))
            influence = max(0.0, 1.0 - nearest / 0.115)
            # Hand-painted vertical variation, with a few intentional bark breaks.
            break_a = math.exp(-((u - 0.38) ** 2) / 0.0025) * math.exp(-((v - 0.58) ** 2) / 0.030)
            break_b = math.exp(-((u - 0.73) ** 2) / 0.0040) * math.exp(-((v - 0.30) ** 2) / 0.018)
            warm = 0.04 * math.sin(v * math.pi * 3.0) + 0.025 * math.sin((u + v) * math.pi * 5.0)
            base = [0.06, 0.014, 0.004]
            color = [base[i] * (1.0 - influence) + stroke[1][i] * influence for i in range(3)]
            color[0] += warm + 0.09 * (break_a + break_b)
            color[1] += warm * 0.32 + 0.025 * (break_a + break_b)
            color[2] += warm * 0.08
            idx = (y * size + x) * 4
            pixels[idx:idx + 4] = [max(0.0, min(1.0, c)) for c in (*color, 1.0)]
    image.pixels = pixels
    image.filepath_raw = str(OUT / "OldTree_Bark_HandPainted_Atlas.png")
    image.file_format = "PNG"
    image.save()
    return image


def apply_painted_material(trunk, image):
    mat = bpy.data.materials.get("M_OldTree_Bark_AtlasPaint") or bpy.data.materials.new("M_OldTree_Bark_AtlasPaint")
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = image
    tex.interpolation = "Linear"
    tex.projection = "FLAT"
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.13
    bump.inputs["Distance"].default_value = 0.045
    links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(tex.outputs["Color"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.90
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    trunk.data.materials.clear()
    trunk.data.materials.append(mat)
    bpy.ops.object.select_all(action="DESELECT")
    trunk.select_set(True)
    bpy.context.view_layer.objects.active = trunk
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=1.05, island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")


def add_soil_collar():
    old = bpy.data.objects.get("AS_OldTree_SoilCollar")
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    mat = bpy.data.materials.get("M_OldTree_Soil") or bpy.data.materials.new("M_OldTree_Soil")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (0.025, 0.018, 0.010, 1)
    bsdf.inputs["Roughness"].default_value = 0.98
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=(0, 0, -0.08))
    soil = bpy.context.object
    soil.name = "AS_OldTree_SoilCollar"
    soil.scale = (2.45, 2.20, 0.18)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    soil.data.materials.append(mat)
    for poly in soil.data.polygons:
        poly.use_smooth = True
    return soil


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


def main():
    bpy.ops.wm.open_mainfile(filepath=str(INPUT))
    trunk = bpy.data.objects["AS_OldTree_TrunkAndBranches"]
    image = make_painted_image()
    apply_painted_material(trunk, image)
    soil = add_soil_collar()
    close = render("01_painted_bark_close.png", (5.8, -7.2, 4.0), (0, 0, 2.9))
    final = render("02_painted_texture_final_3_4.png", (10.8, -12.6, 7.2), (0, 0.15, 3.1))
    front = render("03_painted_texture_front.png", (0, -14.0, 4.6), (0, 0.15, 3.25))
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "ApexShift_OldTree_PaintedTexture.blend"))
    report = {
        "base_file": str(INPUT),
        "restart": False,
        "changes": ["hand-authored broad bark atlas", "UV unwrapped trunk for painted texture", "added irregular soil collar to partially embed roots"],
        "soil_object": soil.name,
        "renders": [str(close), str(final), str(front)],
        "texture": str(OUT / "OldTree_Bark_HandPainted_Atlas.png"),
        "remaining": "For a final art pass, replace the generated atlas with a hand-painted texture authored in an external paint package or by an artist.",
    }
    (OUT / "ApexShift_OldTree_PaintedTexture_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
