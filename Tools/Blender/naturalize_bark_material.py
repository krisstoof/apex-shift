import bpy
import json
from pathlib import Path
from mathutils import Vector

INPUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\PolishPass3_OldTree\ApexShift_OldTree_PolishPass3_Corrected.blend")
OUT = INPUT.parent


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def natural_bark():
    mat = bpy.data.materials.get("M_OldTree_Bark_Natural") or bpy.data.materials.new("M_OldTree_Bark_Natural")
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    tex = nodes.new("ShaderNodeTexCoord")
    mapping = nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (3.0, 3.0, 0.30)
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 4.6
    noise.inputs["Detail"].default_value = 8.0
    noise.inputs["Roughness"].default_value = 0.82
    noise.inputs["Distortion"].default_value = 0.22
    noise2 = nodes.new("ShaderNodeTexNoise")
    noise2.inputs["Scale"].default_value = 13.0
    noise2.inputs["Detail"].default_value = 3.0
    mix = nodes.new("ShaderNodeMixRGB")
    mix.blend_type = "MULTIPLY"
    mix.inputs["Fac"].default_value = 0.24
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.22
    ramp.color_ramp.elements[0].color = (0.012, 0.002, 0.001, 1)
    ramp.color_ramp.elements[1].position = 0.80
    ramp.color_ramp.elements[1].color = (0.145, 0.026, 0.006, 1)
    mid = ramp.color_ramp.elements.new(0.52)
    mid.color = (0.055, 0.009, 0.002, 1)
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.25
    bump.inputs["Distance"].default_value = 0.065
    rough = nodes.new("ShaderNodeMapRange")
    rough.inputs["To Min"].default_value = 0.80
    rough.inputs["To Max"].default_value = 0.98
    links.new(tex.outputs["Generated"], mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
    links.new(mapping.outputs["Vector"], noise2.inputs["Vector"])
    links.new(noise.outputs["Fac"], mix.inputs[1])
    links.new(noise2.outputs["Fac"], mix.inputs[2])
    links.new(mix.outputs["Color"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(mix.outputs["Color"], bump.inputs["Height"])
    links.new(noise.outputs["Fac"], rough.inputs["Value"])
    links.new(rough.outputs["Result"], bsdf.inputs["Roughness"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    trunk = bpy.data.objects["AS_OldTree_TrunkAndBranches"]
    trunk.data.materials.clear()
    trunk.data.materials.append(mat)


def render():
    scene = bpy.context.scene
    cam = bpy.data.objects["Checkpoint1_Camera"]
    cam.location = (10.8, -12.6, 7.2)
    look_at(cam, (0.0, 0.15, 3.2))
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1100
    scene.render.resolution_y = 1100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    path = OUT / "ApexShift_OldTree_Final_NaturalBark_Render.png"
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    return path


def main():
    bpy.ops.wm.open_mainfile(filepath=str(INPUT))
    natural_bark()
    path = render()
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "ApexShift_OldTree_Final_NaturalBark.blend"))
    report = {"base_file": str(INPUT), "material": "M_OldTree_Bark_Natural", "change": "irregular vertically stretched noise replaced regular bark bands", "render": str(path)}
    (OUT / "ApexShift_OldTree_Final_NaturalBark_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
