import bpy
import json
import math
import random
from pathlib import Path
from mathutils import Vector

INPUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\PaintedTexturePass_OldTree\ApexShift_OldTree_PaintedTexture.blend")
OUT = INPUT.parent


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def make_soil_material():
    mat = bpy.data.materials.get("M_OldTree_Soil_Organic") or bpy.data.materials.new("M_OldTree_Soil_Organic")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (0.018, 0.022, 0.012, 1)
    bsdf.inputs["Roughness"].default_value = 1.0
    return mat


def organic_soil():
    old = bpy.data.objects.get("AS_OldTree_SoilCollar")
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    n = 28
    rng = random.Random(551)
    verts = [(0.0, 0.0, 0.025)]
    radii = []
    for i in range(n):
        a = (math.tau * i) / n
        r = 2.15 + rng.uniform(-0.16, 0.20)
        radii.append(r)
        verts.append((math.cos(a) * r, math.sin(a) * r, -0.065 + rng.uniform(-0.025, 0.02)))
    faces = []
    for i in range(n):
        faces.append((0, i + 1, ((i + 1) % n) + 1))
    mesh = bpy.data.meshes.new("AS_OldTree_OrganicSoilMesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    soil = bpy.data.objects.new("AS_OldTree_SoilCollar", mesh)
    bpy.context.scene.collection.objects.link(soil)
    soil.data.materials.append(make_soil_material())
    for p in mesh.polygons:
        p.use_smooth = True
    return soil


def darken_ground():
    ground = bpy.data.objects.get("Checkpoint1_RenderGround")
    if not ground:
        return
    mat = ground.data.materials[0] if ground.data.materials else bpy.data.materials.new("M_OldTree_Ground_Organic")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (0.035, 0.045, 0.028, 1)
    bsdf.inputs["Roughness"].default_value = 1.0
    if not ground.data.materials:
        ground.data.materials.append(mat)


def render():
    scene = bpy.context.scene
    cam = bpy.data.objects["Checkpoint1_Camera"]
    cam.location = (10.8, -12.6, 7.2)
    look_at(cam, (0, 0.15, 3.1))
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1100
    scene.render.resolution_y = 1100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    path = OUT / "ApexShift_OldTree_PaintedTexture_OrganicGround_Render.png"
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    return path


def main():
    bpy.ops.wm.open_mainfile(filepath=str(INPUT))
    soil = organic_soil()
    darken_ground()
    path = render()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "ApexShift_OldTree_PaintedTexture_OrganicGround.blend"))
    report = {"base_file": str(INPUT), "change": "replaced polygonal soil disk with irregular low organic soil collar and darkened ground", "soil_object": soil.name, "render": str(path)}
    (OUT / "ApexShift_OldTree_PaintedTexture_OrganicGround_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
