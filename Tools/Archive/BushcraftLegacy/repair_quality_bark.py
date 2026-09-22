import bpy
import json
from pathlib import Path
from mathutils import Vector

INPUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\PolishPass3_OldTree\ApexShift_OldTree_PolishPass3.blend")
OUT = INPUT.parent


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def repair_material():
    mat = bpy.data.materials.get("M_OldTree_Bark_QualityPass") or bpy.data.materials.new("M_OldTree_Bark_QualityPass")
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    tex = nodes.new("ShaderNodeTexCoord")
    mapping = nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (2.2, 2.2, 0.55)
    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 5.2
    noise.inputs["Detail"].default_value = 6.5
    noise.inputs["Roughness"].default_value = 0.78
    wave = nodes.new("ShaderNodeTexWave")
    wave.wave_type = "BANDS"
    wave.bands_direction = "X"
    wave.inputs["Scale"].default_value = 6.0
    wave.inputs["Distortion"].default_value = 3.2
    wave.inputs["Detail"].default_value = 5.0
    mix = nodes.new("ShaderNodeMixRGB")
    mix.blend_type = "MULTIPLY"
    mix.inputs["Fac"].default_value = 0.38
    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.018, 0.004, 0.001, 1)
    ramp.color_ramp.elements[1].color = (0.19, 0.045, 0.010, 1)
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.34
    bump.inputs["Distance"].default_value = 0.075
    rough = nodes.new("ShaderNodeMapRange")
    rough.inputs["To Min"].default_value = 0.78
    rough.inputs["To Max"].default_value = 0.97
    links.new(tex.outputs["Generated"], mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], noise.inputs["Vector"])
    links.new(mapping.outputs["Vector"], wave.inputs["Vector"])
    links.new(noise.outputs["Fac"], mix.inputs[1])
    links.new(wave.outputs["Color"], mix.inputs[2])
    links.new(mix.outputs["Color"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(mix.outputs["Color"], bump.inputs["Height"])
    links.new(noise.outputs["Fac"], rough.inputs["Value"])
    links.new(rough.outputs["Result"], bsdf.inputs["Roughness"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    trunk = bpy.data.objects.get("AS_OldTree_TrunkAndBranches")
    trunk.data.materials.clear()
    trunk.data.materials.append(mat)


def render_and_export():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1100
    scene.render.resolution_y = 1100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    cam = bpy.data.objects.get("Checkpoint1_Camera")
    cam.location = (10.8, -12.6, 7.2)
    look_at(cam, (0.0, 0.15, 3.2))
    render = OUT / "ApexShift_OldTree_PolishPass3_Render_Corrected.png"
    scene.render.filepath = str(render)
    bpy.ops.render.render(write_still=True)
    trunk = bpy.data.objects["AS_OldTree_TrunkAndBranches"]
    foliage = [o for o in bpy.data.objects if o.name.startswith("AS_OldTree_Foliage") or o.name.startswith("AS_OldTree_QualityFoliage") or o.name.startswith("AS_OldTree_CrownMass")]
    for lod, ratio in [(0, 1.0), (1, 0.43), (2, 0.16)]:
        selected = []
        for source in [trunk] + foliage:
            obj = source if lod == 0 else source.copy()
            if lod != 0:
                obj.data = source.data.copy()
                obj.name = f"AS_OldTree_Corrected_LOD{lod}_{source.name}"
                bpy.context.scene.collection.objects.link(obj)
                mod = obj.modifiers.new("Corrected_Decimate", "DECIMATE")
                mod.ratio = max(0.08, ratio)
                bpy.context.view_layer.objects.active = obj
                bpy.ops.object.modifier_apply(modifier=mod.name)
            obj.select_set(True)
            selected.append(obj)
        bpy.context.view_layer.objects.active = selected[0]
        bpy.ops.export_scene.fbx(filepath=str(OUT / f"ApexShift_OldTree_Corrected_LOD{lod}.fbx"), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False)
        bpy.ops.wm.obj_export(filepath=str(OUT / f"ApexShift_OldTree_Corrected_LOD{lod}.obj"), export_selected_objects=True, forward_axis="NEGATIVE_Z", up_axis="Y")
        bpy.ops.object.select_all(action="DESELECT")
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / "ApexShift_OldTree_PolishPass3_Corrected.blend"))
    report = {"material_fix": "vertical bark grooves corrected to bands along X with noise breakup", "render": str(render), "base_file": str(INPUT)}
    (OUT / "ApexShift_OldTree_PolishPass3_Corrected_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    bpy.ops.wm.open_mainfile(filepath=str(INPUT))
    repair_material()
    render_and_export()
