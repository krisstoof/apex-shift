import bpy
import math
import random
from pathlib import Path
from mathutils import Vector


INPUT = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\Checkpoint2_Branches\ApexShift_OldTree_Checkpoint2_Branches.blend")
OUTPUT_DIR = Path(r"C:\Users\kriss\apex-shift\Tools\Blender\Checkpoint3_Foliage")
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
OUTPUT_BLEND = OUTPUT_DIR / "ApexShift_OldTree_Checkpoint3_Foliage.blend"
RENDERS = {
    "3_4": OUTPUT_DIR / "checkpoint3_3_4.png",
    "front": OUTPUT_DIR / "checkpoint3_front.png",
    "side": OUTPUT_DIR / "checkpoint3_side.png",
}


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def make_leaf_materials():
    mats = []
    for name, color in [
        ("M_OldTree_Leaf_Dark", (0.035, 0.13, 0.018, 1.0)),
        ("M_OldTree_Leaf_Mid", (0.075, 0.24, 0.025, 1.0)),
        ("M_OldTree_Leaf_Light", (0.16, 0.34, 0.045, 1.0)),
    ]:
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = color
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = 0.88
        mats.append(mat)
    return mats


def create_foliage():
    # Anchors are on existing branch endpoints or immediately inside their junctions.
    anchors = [
        ((-2.56, 0.44, 4.82), (0.46, 0.38, 0.36), 18),
        ((-1.92, -0.59, 5.10), (0.40, 0.34, 0.34), 15),
        ((-2.25, 1.16, 5.37), (0.38, 0.32, 0.32), 14),
        ((-0.46, 2.00, 5.66), (0.40, 0.34, 0.35), 15),
        ((-0.78, 0.27, 5.72), (0.44, 0.36, 0.38), 16),
        ((2.47, -0.56, 4.90), (0.46, 0.38, 0.37), 18),
        ((2.42, -1.24, 5.47), (0.38, 0.32, 0.32), 14),
        ((1.14, 0.64, 5.31), (0.42, 0.34, 0.36), 15),
        ((0.48, 2.00, 5.25), (0.42, 0.34, 0.35), 15),
        ((0.88, 0.19, 5.78), (0.43, 0.35, 0.38), 16),
        ((-0.02, 0.10, 5.50), (0.50, 0.40, 0.44), 18),
    ]
    rng = random.Random(91731)
    verts, faces, mids = [], [], []
    for anchor, spread, count in anchors:
        center = Vector(anchor)
        for _ in range(count):
            direction = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-0.7, 1.0)))
            if direction.length < 0.1:
                direction = Vector((0, 0, 1))
            direction.normalize()
            # Keep every card close to its branch anchor, with a bias outward/upward.
            offset = Vector((
                rng.uniform(-spread[0], spread[0]),
                rng.uniform(-spread[1], spread[1]),
                rng.uniform(-spread[2], spread[2]),
            ))
            offset.z += 0.10
            c = center + offset
            length = rng.uniform(0.24, 0.38)
            width = length * rng.uniform(0.42, 0.62)
            axis = direction * length
            side = direction.cross(Vector((0, 0, 1)))
            if side.length < 0.08:
                side = direction.cross(Vector((0, 1, 0)))
            side.normalize()
            roll = rng.uniform(-0.9, 0.9)
            side = (side * math.cos(roll) + direction.cross(side) * math.sin(roll)).normalized()
            base = c - axis * 0.48
            tip = c + axis * 0.52
            left = c + side * (width * 0.5) - axis * 0.04
            right = c - side * (width * 0.5) - axis * 0.04
            start = len(verts)
            verts.extend([tuple(base), tuple(left), tuple(tip), tuple(right)])
            faces.extend([(start, start + 1, start + 2), (start, start + 2, start + 3)])
            mat_id = rng.choices([0, 1, 2], weights=[0.35, 0.48, 0.17])[0]
            mids.extend([mat_id, mat_id])

    mesh = bpy.data.meshes.new("AS_OldTree_FoliageMesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    foliage = bpy.data.objects.new("AS_OldTree_Foliage", mesh)
    bpy.context.scene.collection.objects.link(foliage)
    for mat in make_leaf_materials():
        mesh.materials.append(mat)
    for poly, mat_id in zip(mesh.polygons, mids):
        poly.material_index = mat_id
        poly.use_smooth = False
    return foliage, anchors


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


def diagnostics(foliage, anchors):
    branch = bpy.data.objects.get("AS_OldTree_TrunkAndBranches")
    tris = sum(max(0, len(p.vertices) - 2) for p in foliage.data.polygons)
    print("\n=== CHECKPOINT 3 DIAGNOSTICS ===")
    print("ALL_OBJECTS:", sorted(o.name for o in bpy.context.scene.objects))
    print("VISIBLE_IN_RENDER:", sorted(o.name for o in bpy.context.scene.objects if o.visible_get() and not o.hide_render))
    print("BRANCH_MESH_EXISTS:", branch is not None and branch.type == "MESH")
    print("FOLIAGE_MESH_EXISTS:", foliage is not None and foliage.type == "MESH")
    print("FOLIAGE_TRIS:", tris)
    print("FOLIAGE_MATERIALS:", [m.name for m in foliage.data.materials if m is not None])
    print("FOLIAGE_ANCHORS:", len(anchors))
    print("FOLIAGE_COLLECTION:", foliage.users_collection[0].name if foliage.users_collection else None)
    print("PLACEHOLDER_OBJECTS:", [o.name for o in bpy.context.scene.objects if "placeholder" in o.name.lower() or "human" in o.name.lower()])
    for key, path in RENDERS.items():
        print(f"RENDER_{key.upper()}:", path)
    print("================================\n")


def main():
    bpy.ops.wm.open_mainfile(filepath=str(INPUT))
    branch = bpy.data.objects.get("AS_OldTree_TrunkAndBranches")
    if branch is None or branch.type != "MESH":
        raise RuntimeError("Checkpoint 2 branch mesh missing")
    foliage, anchors = create_foliage()
    render_views()
    diagnostics(foliage, anchors)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_BLEND))
    print("BLEND:", OUTPUT_BLEND)


if __name__ == "__main__":
    main()
