import bpy
from pathlib import Path
from mathutils import Vector


ROOT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\Checkpoint1_Trunk")
INPUT = ROOT / "ApexShift_OldTree_Checkpoint1_Trunk.blend"
OUTPUT_DIR = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\Checkpoint2_Branches")
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
OUTPUT_BLEND = OUTPUT_DIR / "ApexShift_OldTree_Checkpoint2_Branches.blend"
RENDERS = {
    "3_4": OUTPUT_DIR / "checkpoint2_3_4.png",
    "front": OUTPUT_DIR / "checkpoint2_front.png",
    "side": OUTPUT_DIR / "checkpoint2_side.png",
}


def add_segment(name, p0, p1, r0, r1, parts):
    p0, p1 = Vector(p0), Vector(p1)
    direction = p1 - p0
    bpy.ops.mesh.primitive_cone_add(
        vertices=12, radius1=r0, radius2=r1,
        depth=direction.length, location=(p0 + p1) * 0.5,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    obj.rotation_mode = "XYZ"
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    parts.append(obj)


def add_joint(name, location, scale, parts):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    parts.append(obj)


def branch_chain(label, points, radii, parts):
    for i in range(len(points) - 1):
        add_segment(f"_{label}_segment_{i:02d}", points[i], points[i + 1], radii[i], radii[i + 1], parts)
    for i, point in enumerate(points[:-1]):
        radius = radii[i] * 1.18
        add_joint(f"_{label}_joint_{i:02d}", point, (radius, radius * 0.92, radius * 0.92), parts)


def build_branches(trunk):
    parts = [trunk]
    # Three primary asymmetrical limbs, rooted deep inside the existing trunk mass.
    branch_chain("left_primary", [(-0.08, 0.03, 3.05), (-0.72, 0.18, 3.62), (-1.42, 0.24, 4.28), (-2.15, 0.34, 4.72), (-2.72, 0.48, 4.86)], [0.42, 0.34, 0.24, 0.14, 0.055], parts)
    branch_chain("right_primary", [(0.04, 0.05, 3.35), (0.70, -0.14, 3.88), (1.36, -0.34, 4.42), (2.05, -0.48, 4.75), (2.62, -0.58, 4.92)], [0.44, 0.35, 0.24, 0.14, 0.05], parts)
    branch_chain("back_primary", [(0.02, 0.08, 4.00), (0.18, 0.72, 4.52), (0.36, 1.40, 4.98), (0.52, 2.02, 5.25)], [0.34, 0.26, 0.15, 0.045], parts)

    # Secondary limbs continue from the primary limbs, preserving a readable hierarchy.
    branch_chain("left_secondary", [(-1.30, 0.23, 4.18), (-1.62, -0.30, 4.72), (-1.94, -0.62, 5.12)], [0.18, 0.105, 0.038], parts)
    branch_chain("left_upper_secondary", [(-1.98, 0.34, 4.62), (-2.20, 0.90, 5.10), (-2.30, 1.22, 5.40)], [0.12, 0.075, 0.032], parts)
    branch_chain("right_secondary", [(1.28, -0.32, 4.36), (1.24, 0.25, 4.94), (1.14, 0.66, 5.35)], [0.16, 0.095, 0.035], parts)
    branch_chain("right_upper_secondary", [(2.02, -0.48, 4.73), (2.32, -0.98, 5.20), (2.47, -1.28, 5.50)], [0.11, 0.068, 0.03], parts)
    branch_chain("back_secondary", [(0.34, 1.35, 4.96), (-0.18, 1.74, 5.42), (-0.48, 2.02, 5.70)], [0.105, 0.06, 0.028], parts)
    branch_chain("crown_left", [(-0.08, 0.10, 4.70), (-0.52, 0.20, 5.24), (-0.84, 0.28, 5.76)], [0.22, 0.12, 0.035], parts)
    branch_chain("crown_right", [(0.10, 0.12, 4.86), (0.58, 0.12, 5.34), (0.92, 0.20, 5.82)], [0.20, 0.11, 0.032], parts)

    # Deliberately broken low branch: thick base, irregular tapered end, no foliage.
    branch_chain("broken_branch", [(0.40, 0.12, 2.55), (0.98, 0.18, 2.78), (1.55, 0.24, 2.92), (1.92, 0.30, 3.00)], [0.30, 0.22, 0.14, 0.07], parts)
    add_joint("_broken_end", (1.92, 0.30, 3.00), (0.12, 0.10, 0.09), parts)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = trunk
    bpy.ops.object.join()
    trunk = bpy.context.object
    trunk.name = "AS_OldTree_TrunkAndBranches"

    remesh = trunk.modifiers.new("Organic_Branch_Junctions", "REMESH")
    remesh.mode = "VOXEL"
    remesh.voxel_size = 0.075
    remesh.use_smooth_shade = True
    bpy.context.view_layer.objects.active = trunk
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    bark = bpy.data.materials.get("M_OldTree_Bark_Blockout")
    if bark is None:
        bark = bpy.data.materials.new("M_OldTree_Bark_Blockout")
        bark.diffuse_color = (0.18, 0.055, 0.018, 1.0)
    trunk.data.materials.clear()
    trunk.data.materials.append(bark)
    for poly in trunk.data.polygons:
        poly.use_smooth = True
    return trunk


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def render_views():
    scene = bpy.context.scene
    cam = bpy.data.objects.get("Checkpoint1_Camera")
    if cam is None:
        raise RuntimeError("Checkpoint1_Camera not found")
    views = {
        "3_4": (10.5, -12.0, 7.0),
        "front": (0.0, -14.0, 4.0),
        "side": (14.0, 0.0, 4.3),
    }
    for key, location in views.items():
        cam.location = location
        look_at(cam, (0.0, 0.15, 3.0))
        scene.render.filepath = str(RENDERS[key])
        bpy.ops.render.render(write_still=True)


def diagnostics(trunk):
    corners = [trunk.matrix_world @ Vector(c) for c in trunk.bound_box]
    lo = [min(v[i] for v in corners) for i in range(3)]
    hi = [max(v[i] for v in corners) for i in range(3)]
    tris = sum(max(0, len(p.vertices) - 2) for p in trunk.data.polygons)
    visible = [o.name for o in bpy.context.scene.objects if o.visible_get() and not o.hide_render]
    print("\n=== CHECKPOINT 2 DIAGNOSTICS ===")
    print("ALL_OBJECTS:", sorted(o.name for o in bpy.context.scene.objects))
    print("VISIBLE_IN_RENDER:", sorted(visible))
    print("TRUNK_BRANCH_BBOX_MIN:", [round(x, 3) for x in lo])
    print("TRUNK_BRANCH_BBOX_MAX:", [round(x, 3) for x in hi])
    print("TRUNK_BRANCH_TRIS:", tris)
    print("TRUNK_BRANCH_MESH_EXISTS:", trunk.type == "MESH")
    print("TRUNK_BRANCH_MATERIAL:", [m.name for m in trunk.data.materials if m is not None])
    print("TRUNK_BRANCH_COLLECTION:", trunk.users_collection[0].name if trunk.users_collection else None)
    print("FOLIAGE_OBJECTS:", [o.name for o in bpy.context.scene.objects if "leaf" in o.name.lower() or "foliage" in o.name.lower()])
    for key, path in RENDERS.items():
        print(f"RENDER_{key.upper()}:", path)
    print("================================\n")


def main():
    bpy.ops.wm.open_mainfile(filepath=str(INPUT))
    trunk = bpy.data.objects.get("AS_OldTree_TrunkMesh")
    if trunk is None or trunk.type != "MESH":
        raise RuntimeError("Checkpoint 1 trunk mesh missing")
    trunk = build_branches(trunk)
    render_views()
    diagnostics(trunk)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND))
    print("BLEND:", OUTPUT_BLEND)


if __name__ == "__main__":
    main()
