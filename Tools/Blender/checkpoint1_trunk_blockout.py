import bpy
import math
from pathlib import Path
from mathutils import Vector


ROOT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\Checkpoint1_Trunk")
ROOT.mkdir(parents=True, exist_ok=True)
BLEND_PATH = ROOT / "ApexShift_OldTree_Checkpoint1_Trunk.blend"
RENDER_34 = ROOT / "checkpoint1_3_4.png"
RENDER_FRONT = ROOT / "checkpoint1_front.png"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.name != "Collection":
            bpy.data.collections.remove(collection)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)


def bark_material():
    mat = bpy.data.materials.new("M_OldTree_Bark_Blockout")
    mat.diffuse_color = (0.18, 0.055, 0.018, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (0.16, 0.045, 0.012, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.9
    return mat


def add_organic_lump(name, location, scale, parts):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    parts.append(obj)


def add_tapered_segment(name, p0, p1, r0, r1, parts):
    p0, p1 = Vector(p0), Vector(p1)
    direction = p1 - p0
    bpy.ops.mesh.primitive_cone_add(
        vertices=12,
        radius1=r0,
        radius2=r1,
        depth=direction.length,
        location=(p0 + p1) * 0.5,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    obj.rotation_mode = "XYZ"
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    parts.append(obj)


def build_trunk():
    parts = []

    # Uneven overlapping masses make the silhouette broad and old rather than cylindrical.
    trunk_centers = [
        ((0.00, 0.00, 0.35), (1.02, 0.92, 0.52)),
        ((0.04, -0.02, 0.95), (0.88, 0.80, 0.72)),
        ((-0.08, 0.04, 1.65), (0.78, 0.72, 0.82)),
        ((0.10, 0.08, 2.35), (0.68, 0.62, 0.78)),
        ((-0.02, -0.06, 3.05), (0.60, 0.56, 0.75)),
        ((0.16, 0.02, 3.72), (0.53, 0.48, 0.70)),
        ((0.03, 0.13, 4.32), (0.43, 0.40, 0.61)),
        ((-0.16, 0.06, 4.86), (0.34, 0.32, 0.51)),
    ]
    for i, (location, scale) in enumerate(trunk_centers):
        add_organic_lump(f"_trunk_mass_{i:02d}", location, scale, parts)

    # Subtle leaning structural continuation, keeping one coherent mass.
    add_tapered_segment("_trunk_lean", (-0.16, 0.06, 4.55), (0.22, 0.18, 5.72), 0.37, 0.18, parts)
    add_organic_lump("_trunk_crown_base", (0.22, 0.18, 5.62), (0.34, 0.31, 0.42), parts)

    # Eight large buttress roots, each overlapping deeply into the base.
    roots = [
        ((0.00, 0.00, 0.36), (2.35, 0.18, 0.10), 0.52),
        ((0.02, 0.00, 0.40), (1.70, 1.20, 0.12), 0.48),
        ((-0.05, 0.02, 0.39), (-1.75, 1.25, 0.10), 0.48),
        ((-0.08, 0.00, 0.34), (-2.30, -0.18, 0.08), 0.54),
        ((0.05, -0.02, 0.39), (1.40, -1.55, 0.11), 0.46),
        ((-0.02, -0.02, 0.36), (-1.20, -1.70, 0.10), 0.46),
        ((0.00, 0.04, 0.37), (0.15, 2.00, 0.10), 0.45),
        ((0.00, -0.02, 0.34), (0.20, -2.05, 0.09), 0.43),
    ]
    for i, (p0, p1, r) in enumerate(roots):
        add_tapered_segment(f"_root_{i+1:02d}", p0, p1, r, 0.055, parts)
        midpoint = (Vector(p0) * 0.55 + Vector(p1) * 0.45)
        add_organic_lump(f"_root_buttress_{i+1:02d}", midpoint, (r * 1.25, r * 0.72, r * 0.72), parts)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    trunk = bpy.context.object
    trunk.name = "AS_OldTree_TrunkMesh"

    remesh = trunk.modifiers.new("Organic_Joined_Trunk", "REMESH")
    remesh.mode = "VOXEL"
    remesh.voxel_size = 0.085
    remesh.use_smooth_shade = True
    bpy.context.view_layer.objects.active = trunk
    bpy.ops.object.modifier_apply(modifier=remesh.name)

    trunk.data.materials.append(bark_material())
    for poly in trunk.data.polygons:
        poly.use_smooth = True
    return trunk


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def setup_scene(trunk):
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False

    world = bpy.data.worlds.new("Checkpoint1_World")
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.025, 0.035, 0.05, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.35

    cam_data = bpy.data.cameras.new("Checkpoint1_Camera")
    cam = bpy.data.objects.new("Checkpoint1_Camera", cam_data)
    bpy.context.scene.collection.objects.link(cam)
    cam.data.lens = 58
    scene.camera = cam

    sun_data = bpy.data.lights.new("Checkpoint1_Key", "AREA")
    sun_data.energy = 1100
    sun_data.shape = "DISK"
    sun_data.size = 5.0
    key = bpy.data.objects.new("Checkpoint1_Key", sun_data)
    bpy.context.scene.collection.objects.link(key)
    key.location = (5.5, -6.0, 8.5)
    look_at(key, (0, 0, 2.3))

    fill_data = bpy.data.lights.new("Checkpoint1_Fill", "AREA")
    fill_data.energy = 550
    fill_data.size = 4.0
    fill = bpy.data.objects.new("Checkpoint1_Fill", fill_data)
    bpy.context.scene.collection.objects.link(fill)
    fill.location = (-5.0, -2.0, 4.5)
    look_at(fill, (0, 0, 2.0))

    # Ground is part of the render setup, not the asset and is explicitly excluded from diagnostics as an asset object.
    ground_mat = bpy.data.materials.new("Checkpoint1_Ground")
    ground_mat.diffuse_color = (0.025, 0.03, 0.022, 1)
    bpy.ops.mesh.primitive_plane_add(size=16, location=(0, 0, -0.035))
    ground = bpy.context.object
    ground.name = "Checkpoint1_RenderGround"
    ground.data.materials.append(ground_mat)

    def render(path, location):
        cam.location = location
        look_at(cam, (0.0, 0.0, 2.55))
        scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)

    render(RENDER_34, (8.8, -10.5, 6.5))
    render(RENDER_FRONT, (0.0, -12.0, 3.6))


def diagnostics(trunk):
    visible = [o.name for o in bpy.context.scene.objects if o.visible_get() and not o.hide_render]
    corners = [trunk.matrix_world @ Vector(c) for c in trunk.bound_box]
    lo = [min(v[i] for v in corners) for i in range(3)]
    hi = [max(v[i] for v in corners) for i in range(3)]
    tris = sum(max(0, len(p.vertices) - 2) for p in trunk.data.polygons)
    print("\n=== CHECKPOINT 1 DIAGNOSTICS ===")
    print("ALL_OBJECTS:", sorted(o.name for o in bpy.context.scene.objects))
    print("VISIBLE_IN_RENDER:", sorted(visible))
    print("TRUNK_BBOX_MIN:", [round(x, 3) for x in lo])
    print("TRUNK_BBOX_MAX:", [round(x, 3) for x in hi])
    print("TRUNK_TRIS:", tris)
    print("TRUNK_MESH_EXISTS:", trunk is not None and trunk.type == "MESH")
    print("TRUNK_MATERIAL:", [m.name for m in trunk.data.materials])
    print("TRUNK_IN_RENDERED_COLLECTION:", trunk.users_collection[0].name if trunk.users_collection else None)
    print("RENDER_3_4:", RENDER_34)
    print("RENDER_FRONT:", RENDER_FRONT)
    print("================================\n")


def main():
    clear_scene()
    trunk = build_trunk()
    setup_scene(trunk)
    diagnostics(trunk)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print("BLEND:", BLEND_PATH)


if __name__ == "__main__":
    main()
