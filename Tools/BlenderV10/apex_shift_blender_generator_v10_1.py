from __future__ import annotations

"""
Apex Shift Blender Generator v10.1.1
================================

A spec-driven, modular Blender generator for the 98 assets defined in
`apex_shift_asset_visual_specs_v5.json`.

Design goals:
- real structure before detail,
- curves/custom meshes rather than loose primitive piles,
- explicit attachment points and semantic tags,
- full-pack generation by default,
- four-view previews, audit metadata, LOD/collider hooks,
- optional authored base meshes for creatures.

Run in Blender:
    blender --background --python apex_shift_blender_generator_v10.py

Optional arguments after `--`:
    --only wood,bow,tent
    --no-export
    --no-preview
    --output /absolute/output/path
    --seed 27071992

The script was designed for Blender 4.x and uses only standard bpy APIs.
"""

import argparse
import json
import math
import os
import random
import sys
from dataclasses import asdict
from pathlib import Path
from typing import Callable, Dict, Iterable, List, Optional, Sequence, Tuple

import bpy
from mathutils import Euler, Matrix, Vector

# Blender's `--background --python <script>` does NOT put the script's own
# directory on sys.path, so the sibling profiles module must be made importable
# explicitly before it is imported.
_SCRIPT_DIR = str(Path(__file__).resolve().parent)
if _SCRIPT_DIR not in sys.path:
    sys.path.insert(0, _SCRIPT_DIR)

from apex_shift_profiles_v10 import AssetProfile, CATEGORY_DIRS, load_profiles, validate_profile_coverage

ROOT = Path(__file__).resolve().parent
DEFAULT_OUTPUT_ROOT = ROOT / "ApexShift_Assets_v10_Output"
DEFAULT_SEED = 27071992

PALETTE = {
    "bark_dark": ((0.18, 0.10, 0.045, 1.0), 0.94),
    "bark_mid": ((0.30, 0.17, 0.075, 1.0), 0.92),
    "wood": ((0.48, 0.29, 0.12, 1.0), 0.84),
    "wood_cut": ((0.73, 0.54, 0.30, 1.0), 0.80),
    "rope": ((0.62, 0.44, 0.19, 1.0), 0.96),
    "stone": ((0.43, 0.45, 0.46, 1.0), 0.93),
    "stone_dark": ((0.23, 0.26, 0.28, 1.0), 0.92),
    "flint": ((0.18, 0.22, 0.24, 1.0), 0.76),
    "leaf": ((0.24, 0.43, 0.16, 1.0), 0.88),
    "leaf_light": ((0.42, 0.59, 0.24, 1.0), 0.88),
    "leaf_dark": ((0.12, 0.28, 0.12, 1.0), 0.91),
    "needle": ((0.10, 0.27, 0.13, 1.0), 0.92),
    "berry_red": ((0.62, 0.07, 0.055, 1.0), 0.68),
    "berry_blue": ((0.17, 0.20, 0.43, 1.0), 0.72),
    "berry_dark": ((0.09, 0.07, 0.12, 1.0), 0.74),
    "hide": ((0.47, 0.28, 0.13, 1.0), 0.91),
    "hide_dark": ((0.30, 0.17, 0.08, 1.0), 0.94),
    "fur": ((0.42, 0.28, 0.18, 1.0), 0.88),
    "fur_light": ((0.67, 0.52, 0.34, 1.0), 0.87),
    "fur_dark": ((0.16, 0.13, 0.11, 1.0), 0.91),
    "bone": ((0.82, 0.78, 0.66, 1.0), 0.83),
    "meat": ((0.52, 0.06, 0.05, 1.0), 0.68),
    "fat": ((0.82, 0.65, 0.45, 1.0), 0.76),
    "charcoal": ((0.07, 0.06, 0.05, 1.0), 0.98),
    "ash": ((0.30, 0.28, 0.25, 1.0), 0.99),
    "fire_orange": ((1.0, 0.24, 0.015, 1.0), 0.30),
    "fire_yellow": ((1.0, 0.72, 0.04, 1.0), 0.24),
    "earth": ((0.32, 0.23, 0.14, 1.0), 0.98),
    "mud": ((0.22, 0.17, 0.11, 1.0), 0.99),
    "water": ((0.18, 0.39, 0.44, 0.84), 0.20),
    "flower_white": ((0.92, 0.90, 0.82, 1.0), 0.75),
    "flower_yellow": ((0.92, 0.72, 0.12, 1.0), 0.70),
    "flower_purple": ((0.47, 0.32, 0.65, 1.0), 0.72),
    # Neutral backdrop used only by preview renders, never by an exported asset.
    "preview_ground": ((0.21, 0.21, 0.22, 1.0), 0.96),
}

MATERIALS: Dict[str, bpy.types.Material] = {}
ACTIVE_PROFILE: Optional[AssetProfile] = None
ACTIVE_ROOT: Optional[bpy.types.Object] = None
ACTIVE_PARTS: List[bpy.types.Object] = []
RNG = random.Random(DEFAULT_SEED)


def parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(add_help=False)
    parser.add_argument("--only", default="")
    parser.add_argument("--no-export", action="store_true")
    parser.add_argument("--no-preview", action="store_true")
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT_ROOT))
    parser.add_argument("--seed", type=int, default=DEFAULT_SEED)
    return parser.parse_args(argv)


def ensure_clean_scene() -> None:
    # Preserve optional authored production bases stored in an ASSET_BASES collection.
    base_collection = bpy.data.collections.get("ASSET_BASES")
    preserved = set(base_collection.all_objects) if base_collection else set()
    bpy.ops.object.select_all(action="DESELECT")
    for obj in list(bpy.context.scene.objects):
        if obj in preserved:
            obj.hide_render = True
            obj.hide_viewport = True
            continue
        obj.select_set(True)
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def get_material(name: str) -> bpy.types.Material:
    if name in MATERIALS and MATERIALS[name].name in bpy.data.materials:
        return MATERIALS[name]
    color, roughness = PALETTE[name]
    mat = bpy.data.materials.new(name=f"AS_{name}")
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        if name == "water":
            bsdf.inputs["Alpha"].default_value = color[3]
            bsdf.inputs["Transmission Weight"].default_value = 0.18
    if color[3] < 1.0 and hasattr(mat, "surface_render_method"):
        mat.surface_render_method = "DITHERED"
    MATERIALS[name] = mat
    return mat


def semantic(obj: bpy.types.Object, *tags: str) -> bpy.types.Object:
    clean = [tag for tag in tags if tag]
    obj["as_tags"] = clean
    if ACTIVE_PROFILE:
        obj["as_asset_id"] = ACTIVE_PROFILE.asset_id
        obj["as_family"] = ACTIVE_PROFILE.family
    return obj


def register(obj: bpy.types.Object, *tags: str) -> bpy.types.Object:
    semantic(obj, *tags)
    if ACTIVE_ROOT:
        obj.parent = ACTIVE_ROOT
    ACTIVE_PARTS.append(obj)
    return obj


def set_material(obj: bpy.types.Object, material_name: str, slot: int = 0) -> None:
    mat = get_material(material_name)
    if len(obj.data.materials) <= slot:
        while len(obj.data.materials) < slot:
            obj.data.materials.append(mat)
        obj.data.materials.append(mat)
    else:
        obj.data.materials[slot] = mat


def smooth(obj: bpy.types.Object) -> None:
    if not hasattr(obj.data, "polygons"):
        return
    for poly in obj.data.polygons:
        poly.use_smooth = True


def create_empty(name: str, location=(0, 0, 0)) -> bpy.types.Object:
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "PLAIN_AXES"
    obj.location = location
    bpy.context.collection.objects.link(obj)
    return obj


def primitive_ellipsoid(name: str, location, radii, material: str, segments=16, rings=10, tags=()) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = radii
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    set_material(obj, material)
    smooth(obj)
    return register(obj, *tags)


def primitive_box(name: str, location, size, material: str, rotation=(0, 0, 0), tags=()) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    set_material(obj, material)
    return register(obj, *tags)


def tapered_segment(name: str, a, b, radius_a: float, radius_b: float, material: str, sides=10, tags=()) -> bpy.types.Object:
    a = Vector(a)
    b = Vector(b)
    direction = b - a
    length = direction.length
    if length <= 1e-6:
        direction = Vector((0, 0, 1))
        length = 1e-6
    direction.normalize()
    tangent = direction.cross(Vector((0, 0, 1)))
    if tangent.length < 1e-4:
        tangent = direction.cross(Vector((0, 1, 0)))
    tangent.normalize()
    bitangent = direction.cross(tangent).normalized()
    vertices: List[Tuple[float, float, float]] = []
    for center, radius in ((a, radius_a), (b, radius_b)):
        for i in range(sides):
            ang = i / sides * math.tau
            p = center + tangent * math.cos(ang) * radius + bitangent * math.sin(ang) * radius
            vertices.append(tuple(p))
    faces: List[Tuple[int, ...]] = []
    for i in range(sides):
        n = (i + 1) % sides
        faces.append((i, n, sides + n, sides + i))
    faces.append(tuple(reversed(range(sides))))
    faces.append(tuple(range(sides, sides * 2)))
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    set_material(obj, material)
    smooth(obj)
    return register(obj, *tags)


def curve_tube(name: str, points: Sequence[Tuple[float, float, float]], radius: float, material: str, tags=(), resolution=2) -> bpy.types.Object:
    curve_data = bpy.data.curves.new(name + "_curve", type="CURVE")
    curve_data.dimensions = "3D"
    curve_data.resolution_u = resolution
    curve_data.bevel_depth = radius
    curve_data.bevel_resolution = 2
    spline = curve_data.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for bp, co in zip(spline.bezier_points, points):
        bp.co = co
        bp.handle_left_type = "AUTO"
        bp.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, curve_data)
    bpy.context.collection.objects.link(obj)
    set_material(obj, material)
    return register(obj, *tags)


def irregular_log(name: str, a, b, radius: float, material="bark_mid", cut_material="wood_cut", seed=0, tags=()) -> bpy.types.Object:
    local_rng = random.Random(seed)
    a = Vector(a)
    b = Vector(b)
    direction = (b - a).normalized()
    tangent = direction.cross(Vector((0, 0, 1)))
    if tangent.length < 1e-4:
        tangent = direction.cross(Vector((0, 1, 0)))
    tangent.normalize()
    bitangent = direction.cross(tangent).normalized()
    sides = 10
    rings = 5
    vertices: List[Tuple[float, float, float]] = []
    for r in range(rings):
        t = r / (rings - 1)
        center = a.lerp(b, t)
        ring_radius = radius * (1.0 + local_rng.uniform(-0.10, 0.08)) * (1.0 - 0.04 * t)
        center += tangent * local_rng.uniform(-radius * 0.12, radius * 0.12)
        center += bitangent * local_rng.uniform(-radius * 0.12, radius * 0.12)
        for i in range(sides):
            ang = i / sides * math.tau
            rr = ring_radius * (1.0 + local_rng.uniform(-0.08, 0.08))
            p = center + tangent * math.cos(ang) * rr + bitangent * math.sin(ang) * rr
            vertices.append(tuple(p))
    faces: List[Tuple[int, ...]] = []
    material_indices: List[int] = []
    for r in range(rings - 1):
        for i in range(sides):
            n = (i + 1) % sides
            faces.append((r * sides + i, r * sides + n, (r + 1) * sides + n, (r + 1) * sides + i))
            material_indices.append(0)
    faces.append(tuple(reversed(range(sides))))
    material_indices.append(1)
    faces.append(tuple(range((rings - 1) * sides, rings * sides)))
    material_indices.append(1)
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(get_material(material))
    obj.data.materials.append(get_material(cut_material))
    for poly, idx in zip(obj.data.polygons, material_indices):
        poly.material_index = idx
    smooth(obj)
    return register(obj, *tags)


def flaked_stone(name: str, location, size, material="flint", rotation=(0, 0, 0), seed=0, tags=(), tip_scale: Optional[float] = None) -> bpy.types.Object:
    """Knapped stone blank.

    `tip_scale` narrows only the top ring, which turns the default lens shape
    into a pointed leaf blade for spear and arrow heads. Left as None the shape
    is symmetric, which is what axe and pickaxe heads need.
    """
    rng = random.Random(seed)
    sx, sy, sz = size
    sides = 8
    vertices: List[Tuple[float, float, float]] = []
    for layer, z in enumerate((-sz / 2, 0.0, sz / 2)):
        layer_scale = 0.78 if layer != 1 else 1.0
        if layer == 2 and tip_scale is not None:
            layer_scale = tip_scale
        for i in range(sides):
            ang = i / sides * math.tau
            asym = 1.0 + rng.uniform(-0.16, 0.12)
            vertices.append((math.cos(ang) * sx * 0.5 * layer_scale * asym, math.sin(ang) * sy * 0.5 * layer_scale * asym, z + rng.uniform(-sz * 0.06, sz * 0.06)))
    faces: List[Tuple[int, ...]] = []
    for layer in range(2):
        for i in range(sides):
            n = (i + 1) % sides
            faces.append((layer * sides + i, layer * sides + n, (layer + 1) * sides + n, (layer + 1) * sides + i))
    faces.append(tuple(reversed(range(sides))))
    faces.append(tuple(range(2 * sides, 3 * sides)))
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler = rotation
    set_material(obj, material)
    smooth(obj)
    return register(obj, *tags)


def leaf_mesh(name: str, location, scale, material="leaf", rotation=(0, 0, 0), tags=()) -> bpy.types.Object:
    width, length, curl = scale
    vertices = [
        (0, -length / 2, 0),
        (width * 0.48, -length * 0.10, curl * 0.25),
        (width * 0.35, length * 0.32, curl * 0.62),
        (0, length / 2, curl),
        (-width * 0.35, length * 0.32, curl * 0.55),
        (-width * 0.48, -length * 0.10, curl * 0.18),
        (0, 0, curl * 0.15),
    ]
    faces = [(0, 1, 6), (1, 2, 6), (2, 3, 6), (3, 4, 6), (4, 5, 6), (5, 0, 6)]
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler = rotation
    set_material(obj, material)
    return register(obj, *tags)


def panel_mesh(name: str, location, size, material, rotation=(0, 0, 0), irregularity=0.08, seed=0, tags=()) -> bpy.types.Object:
    rng = random.Random(seed)
    w, h, thickness = size
    x0, x1 = -w / 2, w / 2
    z0, z1 = -h / 2, h / 2
    front = [
        (x0 + rng.uniform(-w * irregularity, w * irregularity), -thickness / 2, z0 + rng.uniform(-h * irregularity, h * irregularity)),
        (x1 + rng.uniform(-w * irregularity, w * irregularity), -thickness / 2, z0 + rng.uniform(-h * irregularity, h * irregularity)),
        (x1 + rng.uniform(-w * irregularity, w * irregularity), -thickness / 2, z1 + rng.uniform(-h * irregularity, h * irregularity)),
        (x0 + rng.uniform(-w * irregularity, w * irregularity), -thickness / 2, z1 + rng.uniform(-h * irregularity, h * irregularity)),
    ]
    back = [(x, thickness / 2, z) for x, _, z in front]
    vertices = front + back
    faces = [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)]
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler = rotation
    set_material(obj, material)
    return register(obj, *tags)


def rope_helix(name: str, center, radius: float, height: float, turns: float, material="rope", axis="z", tags=()) -> bpy.types.Object:
    points: List[Tuple[float, float, float]] = []
    count = max(24, int(turns * 18))
    for i in range(count):
        t = i / (count - 1)
        ang = t * turns * math.tau
        if axis == "z":
            points.append((center[0] + math.cos(ang) * radius, center[1] + math.sin(ang) * radius, center[2] - height / 2 + t * height))
        elif axis == "x":
            points.append((center[0] - height / 2 + t * height, center[1] + math.cos(ang) * radius, center[2] + math.sin(ang) * radius))
        else:
            points.append((center[0] + math.cos(ang) * radius, center[1] - height / 2 + t * height, center[2] + math.sin(ang) * radius))
    return curve_tube(name, points, radius * 0.16, material, tags=tags, resolution=1)


def flower(name: str, location, color="flower_white", stem_height=0.28, seed=0) -> None:
    rng = random.Random(seed)
    tapered_segment(name + "_stem", location, (location[0], location[1], location[2] + stem_height), 0.006, 0.003, "leaf", sides=6, tags=("stem", "ground"))
    top = Vector((location[0], location[1], location[2] + stem_height))
    primitive_ellipsoid(name + "_disc", top, (0.024, 0.024, 0.010), "flower_yellow", segments=10, rings=6, tags=("flower",))
    for i in range(6):
        ang = i / 6 * math.tau
        center = top + Vector((math.cos(ang) * 0.035, math.sin(ang) * 0.035, rng.uniform(-0.003, 0.005)))
        primitive_ellipsoid(name + f"_petal_{i}", center, (0.026, 0.012, 0.006), color, segments=8, rings=5, tags=("flower",))


def berry_cluster(prefix: str, anchor, count=4, palette=("berry_red", "berry_blue"), seed=0) -> None:
    rng = random.Random(seed)
    anchor = Vector(anchor)
    for i in range(count):
        ang = i / max(count, 1) * math.tau + rng.uniform(-0.25, 0.25)
        berry = anchor + Vector((math.cos(ang) * rng.uniform(0.025, 0.050), math.sin(ang) * rng.uniform(0.025, 0.050), -rng.uniform(0.035, 0.080)))
        tapered_segment(prefix + f"_pedicel_{i}", anchor, berry, 0.0025, 0.0015, "leaf", sides=5, tags=("pedicel",))
        primitive_ellipsoid(prefix + f"_berry_{i}", berry, (0.024, 0.024, 0.026), palette[i % len(palette)], segments=12, rings=8, tags=("fruit",))


def ground_scatter(prefix: str, radius=0.5, rocks=4, grass=4, seed=0) -> None:
    rng = random.Random(seed)
    for i in range(rocks):
        ang = rng.random() * math.tau
        dist = rng.uniform(radius * 0.45, radius)
        primitive_ellipsoid(prefix + f"_rock_{i}", (math.cos(ang) * dist, math.sin(ang) * dist, 0.035), (rng.uniform(0.035, 0.08), rng.uniform(0.030, 0.07), rng.uniform(0.025, 0.05)), "stone", segments=10, rings=6, tags=("ground", "dressing"))
    for i in range(grass):
        ang = rng.random() * math.tau
        dist = rng.uniform(radius * 0.35, radius)
        x, y = math.cos(ang) * dist, math.sin(ang) * dist
        for j in range(3):
            tapered_segment(prefix + f"_grass_{i}_{j}", (x, y, 0), (x + rng.uniform(-0.02, 0.02), y + rng.uniform(-0.02, 0.02), rng.uniform(0.08, 0.16)), 0.004, 0.0015, "leaf_light", sides=5, tags=("ground", "grass"))


# -----------------------------------------------------------------------------
# Family generators
# -----------------------------------------------------------------------------


def gen_wood(profile: AssetProfile) -> None:
    for i in range(7):
        y = (i - 3) * 0.052 + RNG.uniform(-0.012, 0.012)
        z = 0.08 + abs(i - 3) * 0.010
        length = RNG.uniform(0.62, 0.82)
        irregular_log(f"wood_log_{i}", (-length / 2, y, z), (length / 2, y + RNG.uniform(-0.02, 0.02), z + RNG.uniform(-0.012, 0.012)), RNG.uniform(0.038, 0.062), seed=profile.variant_index * 100 + i, tags=("wood", "ground", "cut_face"))
    rope_helix("wood_binding_left", (-0.22, 0, 0.11), 0.135, 0.045, 3.5, axis="x", tags=("rope",))
    rope_helix("wood_binding_right", (0.22, 0, 0.11), 0.135, 0.045, 3.5, axis="x", tags=("rope",))


def gen_stone_pickup(profile: AssetProfile) -> None:
    for i, (x, y, z, r) in enumerate(((-0.12, 0.02, 0.08, 0.16), (0.12, 0.04, 0.07, 0.12), (0.02, -0.12, 0.06, 0.10), (-0.02, 0.14, 0.05, 0.08))):
        primitive_ellipsoid(f"stone_{i}", (x, y, z), (r, r * RNG.uniform(0.72, 1.05), r * RNG.uniform(0.55, 0.82)), "stone" if i % 2 else "stone_dark", segments=12, rings=8, tags=("stone", "ground"))


def gen_fiber(profile: AssetProfile) -> None:
    for i in range(26):
        x, y = RNG.uniform(-0.055, 0.055), RNG.uniform(-0.070, 0.070)
        h = RNG.uniform(0.55, 0.78)
        curve_tube(f"fiber_strand_{i}", [(x, y, 0), (x + RNG.uniform(-0.02, 0.02), y + RNG.uniform(-0.02, 0.02), h * 0.55), (x + RNG.uniform(-0.03, 0.03), y + RNG.uniform(-0.03, 0.03), h)], 0.0035, "rope", tags=("fiber", "ground"), resolution=1)
    rope_helix("fiber_binding", (0, 0, 0.25), 0.075, 0.08, 4, axis="z", tags=("rope",))


def gen_grass_pickup(profile: AssetProfile) -> None:
    for i in range(34):
        ang = RNG.random() * math.tau
        dist = RNG.uniform(0.0, 0.14)
        start = (math.cos(ang) * dist, math.sin(ang) * dist, 0)
        end = (start[0] + RNG.uniform(-0.05, 0.05), start[1] + RNG.uniform(-0.05, 0.05), RNG.uniform(0.28, 0.48))
        tapered_segment(f"grass_blade_{i}", start, end, 0.006, 0.001, "leaf_light" if i % 3 else "leaf", sides=5, tags=("grass", "ground"))


def gen_meat(profile: AssetProfile) -> None:
    primitive_ellipsoid("meat_main", (0, 0, 0.12), (0.20, 0.13, 0.11), "meat", segments=18, rings=12, tags=("meat", "ground"))
    primitive_ellipsoid("fat_cap", (0.06, -0.02, 0.19), (0.11, 0.08, 0.035), "fat", segments=14, rings=8, tags=("fat",))


def gen_hide(profile: AssetProfile) -> None:
    panel_mesh("hide_sheet", (0, 0, 0.05), (0.58, 0.44, 0.025), "hide", rotation=(math.radians(90), 0, math.radians(6)), irregularity=0.16, seed=1, tags=("hide", "ground"))
    panel_mesh("hide_fold", (0.12, 0.04, 0.08), (0.28, 0.18, 0.018), "hide_dark", rotation=(math.radians(72), 0, math.radians(-12)), irregularity=0.12, seed=2, tags=("hide",))


def gen_bone(profile: AssetProfile) -> None:
    tapered_segment("bone_shaft", (-0.26, 0, 0.12), (0.28, 0, 0.13), 0.045, 0.040, "bone", sides=12, tags=("bone", "ground"))
    for side, x in (("left", -0.31), ("right", 0.33)):
        primitive_ellipsoid(f"bone_{side}_knob_a", (x, -0.030, 0.13), (0.075, 0.060, 0.070), "bone", segments=14, rings=9, tags=("bone_end", "ground"))
        primitive_ellipsoid(f"bone_{side}_knob_b", (x, 0.038, 0.12), (0.068, 0.055, 0.062), "bone", segments=14, rings=9, tags=("bone_end", "ground"))


def gen_berries_pickup(profile: AssetProfile) -> None:
    tapered_segment("berry_main_twig", (-0.15, 0, 0.03), (0.12, 0, 0.14), 0.009, 0.004, "bark_mid", sides=7, tags=("stem", "ground"))
    anchors = [(0.04, -0.045, 0.12), (0.08, 0.045, 0.14)]
    for i, anchor in enumerate(anchors):
        tapered_segment(f"berry_side_{i}", (0.0, 0, 0.10), anchor, 0.004, 0.002, "wood", sides=6, tags=("stem",))
        berry_cluster(f"pickup_cluster_{i}", anchor, 4, seed=i)
    for i, (loc, rot) in enumerate((((-0.04, -0.05, 0.09), (0.8, 0.1, -0.4)), ((0, 0.05, 0.11), (0.75, -0.15, 0.1)), ((0.07, -0.02, 0.13), (0.9, 0.1, 0.4)))):
        leaf_mesh(f"pickup_leaf_{i}", loc, (0.07, 0.12, 0.012), "leaf_light" if i == 0 else "leaf", rotation=rot, tags=("leaf",))


def gen_kindling(profile: AssetProfile) -> None:
    for i in range(11):
        length = RNG.uniform(0.42, 0.66)
        irregular_log(f"kindling_{i}", (-length / 2, RNG.uniform(-0.10, 0.10), 0.06 + i * 0.008), (length / 2, RNG.uniform(-0.10, 0.10), 0.07 + i * 0.008), RNG.uniform(0.022, 0.038), seed=i + 40, tags=("wood", "ground"))
    rope_helix("kindling_binding", (0, 0, 0.11), 0.12, 0.04, 3.5, axis="x", tags=("rope",))


def gen_flint(profile: AssetProfile) -> None:
    flaked_stone("flint_main", (0, 0, 0.08), (0.24, 0.16, 0.10), seed=10, tags=("flint", "ground"))
    flaked_stone("flint_chip_a", (-0.14, 0.05, 0.04), (0.10, 0.07, 0.045), seed=11, tags=("flint", "ground"))
    flaked_stone("flint_chip_b", (0.13, -0.05, 0.04), (0.09, 0.06, 0.040), seed=12, tags=("flint", "ground"))


def gen_reed_bundle(profile: AssetProfile) -> None:
    for i in range(24):
        x, y = RNG.uniform(-0.05, 0.05), RNG.uniform(-0.07, 0.07)
        h = RNG.uniform(0.70, 0.94)
        tapered_segment(f"reed_{i}", (x, y, 0), (x + RNG.uniform(-0.02, 0.02), y + RNG.uniform(-0.02, 0.02), h), 0.007, 0.0025, "rope", sides=5, tags=("reed", "ground"))
    rope_helix("reed_binding", (0, 0, 0.28), 0.075, 0.08, 4, tags=("rope",))


def gen_torch(profile: AssetProfile) -> None:
    tapered_segment("torch_shaft", (0, 0, 0), (0, 0, 1.05), 0.033, 0.024, "wood", sides=12, tags=("shaft", "ground"))
    for i in range(6):
        panel_mesh(f"torch_wrap_{i}", (RNG.uniform(-0.018, 0.018), RNG.uniform(-0.018, 0.018), 0.86 + i * 0.028), (0.09, 0.16, 0.012), "hide_dark", rotation=(RNG.uniform(0.6, 1.2), RNG.uniform(-0.2, 0.2), RNG.random() * math.tau), irregularity=0.12, seed=80 + i, tags=("wrap",))
    rope_helix("torch_lashing", (0, 0, 0.84), 0.055, 0.18, 5.0, tags=("rope",))
    if not profile.asset_id.endswith("unlit"):
        tapered_segment("torch_flame_outer", (0, 0, 1.00), (0, 0, 1.30), 0.13, 0.015, "fire_orange", sides=10, tags=("flame",))
        tapered_segment("torch_flame_inner", (0.01, 0, 1.02), (0.01, 0, 1.22), 0.07, 0.010, "fire_yellow", sides=10, tags=("flame",))


def gen_spear(profile: AssetProfile) -> None:
    scale = 1.08 if profile.asset_id.endswith("heavy") else 1.0
    tapered_segment("spear_shaft", (0, 0, 0), (0, 0, 1.66 * scale), 0.029 * scale, 0.017 * scale, "wood", sides=14, tags=("shaft", "ground"))
    # The head must run in line with the shaft. It was previously rotated 90
    # degrees on Y/Z, which mounted it crossways like an axe blade.
    flaked_stone("spear_head", (0, 0, 1.82 * scale), (0.17 * scale, 0.055 * scale, 0.40 * scale), seed=profile.variant_index + 100, tags=("head",), tip_scale=0.10)
    rope_helix("spear_lashing", (0, 0, 1.60 * scale), 0.037 * scale, 0.17 * scale, 6.0, tags=("rope",))


def gen_bow(profile: AssetProfile) -> None:
    height = 0.94 if profile.asset_id.endswith("short") else 1.18
    curve = [(-0.03, 0, 0.03), (0.05, 0, height * 0.18), (0.13, 0, height * 0.38), (0.17, 0, height * 0.50), (0.13, 0, height * 0.68), (0.05, 0, height * 0.86), (-0.03, 0, height)]
    curve_tube("bow_stave", curve, 0.018 if height > 1 else 0.016, "wood", tags=("stave", "ground"), resolution=3)
    tapered_segment("bow_string", (-0.03, 0.012, 0.04), (-0.03, 0.012, height - 0.01), 0.0025, 0.0025, "rope", sides=6, tags=("string",))
    primitive_box("bow_grip", (0.17, 0, height * 0.50), (0.06, 0.05, 0.16), "hide_dark", tags=("grip",))


def gen_arrow(profile: AssetProfile, offset=(0, 0, 0), prefix="arrow") -> None:
    ox, oy, oz = offset
    tapered_segment(prefix + "_shaft", (ox, oy, oz), (ox, oy, oz + 0.92), 0.011, 0.008, "wood", sides=8, tags=("shaft", "ground" if oz == 0 else ""))
    flaked_stone(prefix + "_head", (ox, oy, oz + 1.00), (0.055, 0.028, 0.17), seed=int((ox + 1) * 1000), tags=("head",), tip_scale=0.10)
    for i, side in enumerate((-1, 1)):
        panel_mesh(prefix + f"_fletching_{i}", (ox, oy + side * 0.016, oz + 0.10), (0.07, 0.06, 0.008), "hide", rotation=(math.radians(90), 0, 0), irregularity=0.04, seed=i + 200, tags=("fletching",))


def gen_arrow_bundle(profile: AssetProfile) -> None:
    for i in range(5):
        gen_arrow(profile, (RNG.uniform(-0.035, 0.035), RNG.uniform(-0.055, 0.055), i * 0.008), prefix=f"arrow_{i}")
    rope_helix("arrow_bundle_binding", (0, 0, 0.22), 0.07, 0.06, 4, tags=("rope",))


def gen_axe(profile: AssetProfile) -> None:
    scale = 1.10 if profile.asset_id.endswith("heavy") else 1.0
    tapered_segment("axe_haft", (0, 0, 0), (0, 0, 0.90 * scale), 0.038 * scale, 0.025 * scale, "wood", sides=14, tags=("shaft", "ground"))
    flaked_stone("axe_head", (0.035 * scale, 0, 0.83 * scale), (0.34 * scale, 0.14 * scale, 0.20 * scale), rotation=(0, math.radians(88), math.radians(8)), seed=300 + profile.variant_index, tags=("head",))
    rope_helix("axe_lashing", (0, 0, 0.76 * scale), 0.048 * scale, 0.18 * scale, 6, tags=("rope",))


def gen_pickaxe(profile: AssetProfile) -> None:
    tapered_segment("pickaxe_haft", (0, 0, 0), (0, 0, 1.02), 0.038, 0.026, "wood", sides=14, tags=("shaft", "ground"))
    # flaked_stone's length axis is its local Z, so the head's 0.66 m length
    # must be passed in the Z slot and rotated 90 deg about Y to lie horizontal
    # (perpendicular to the haft). The previous 90 deg Z rotation instead kept
    # the length vertical and just swapped which horizontal axis it pointed
    # along, so the head read as a squat lens rather than an elongated pick.
    flaked_stone("pickaxe_head", (0, 0, 0.94), (0.16, 0.14, 0.66), rotation=(0, math.radians(90), 0), seed=400, tags=("head",), tip_scale=0.20)
    rope_helix("pickaxe_lashing", (0, 0, 0.88), 0.05, 0.18, 6, tags=("rope",))


def gen_campfire(profile: AssetProfile) -> None:
    burned = profile.family == "campfire_burned"
    for i in range(14):
        ang = i / 14 * math.tau + RNG.uniform(-0.05, 0.05)
        primitive_ellipsoid(f"ring_stone_{i}", (math.cos(ang) * 0.48, math.sin(ang) * 0.48, 0.07), (RNG.uniform(0.10, 0.14), RNG.uniform(0.09, 0.13), RNG.uniform(0.06, 0.09)), "stone" if i % 2 else "stone_dark", segments=12, rings=8, tags=("ring_stone", "ground"))
    primitive_ellipsoid("ember_bed", (0, 0, 0.05), (0.26, 0.22, 0.045), "charcoal" if not burned else "ash", segments=14, rings=8, tags=("ember_bed", "ground"))
    for i in range(6 if not burned else 4):
        ang = i / max(1, 6) * math.tau
        a = (math.cos(ang) * 0.20, math.sin(ang) * 0.20, 0.08)
        b = (0, 0, 0.48 if not burned else 0.18)
        irregular_log(f"fuel_{i}", a, b, 0.034, material="bark_dark", cut_material="charcoal", seed=500 + i, tags=("fuel", "ground"))
    if not burned:
        tapered_segment("flame_outer", (0, 0, 0.18), (0, 0, 0.82), 0.18, 0.015, "fire_orange", sides=12, tags=("flame",))
        tapered_segment("flame_inner", (0.02, 0, 0.22), (0.02, 0, 0.66), 0.10, 0.008, "fire_yellow", sides=12, tags=("flame",))


def gen_storage_box(profile: AssetProfile) -> None:
    primitive_box("box_core", (0, 0, 0.33), (1.00, 0.62, 0.60), "bark_dark", tags=("body", "ground"))
    for side, y in (("front", -0.325), ("back", 0.325)):
        for i, z in enumerate((0.12, 0.28, 0.44, 0.60)):
            primitive_box(f"{side}_plank_{i}", (0, y, z), (0.98, 0.055, 0.13), "wood" if i % 2 else "bark_mid", tags=("plank",))
    for side, x in (("left", -0.52), ("right", 0.52)):
        for i, z in enumerate((0.14, 0.34, 0.54)):
            primitive_box(f"{side}_plank_{i}", (x, 0, z), (0.055, 0.62, 0.16), "wood", tags=("plank",))
    primitive_box("lid", (0, 0, 0.70), (1.08, 0.68, 0.12), "bark_dark", rotation=(math.radians(-4), 0, 0), tags=("lid",))
    rope_helix("box_rope_left", (-0.35, 0, 0.54), 0.36, 0.08, 3.5, axis="x", tags=("rope",))
    rope_helix("box_rope_right", (0.35, 0, 0.54), 0.36, 0.08, 3.5, axis="x", tags=("rope",))


def gen_tent(profile: AssetProfile) -> None:
    """Production-oriented A-frame shelter.

    The previous implementation rotated the roof panels around the Y axis, which
    made them tilt along the shelter length and produced a chaotic wall-like mass.
    This version rotates the panels around X so every shingle follows the true
    roof slope from eave to ridge. The front remains an open triangular entrance.
    """
    front_x, rear_x = -0.88, 0.88
    ridge_y, ridge_z = 0.0, 1.46
    eave_y, eave_z = 0.82, 0.055
    roof_angle = math.atan2(eave_y - ridge_y, ridge_z - eave_z)

    # Two real A-frames: feet are wide apart and converge at the ridge.
    for name, x in (("front", front_x), ("rear", rear_x)):
        frame_x = x - 0.055 if name == "front" else x + 0.055
        tapered_segment(
            f"{name}_support_l",
            (frame_x, -eave_y, 0.0),
            (frame_x, -0.035, ridge_z - 0.055),
            0.064,
            0.038,
            "bark_dark",
            sides=12,
            tags=("frame", "ground"),
        )
        tapered_segment(
            f"{name}_support_r",
            (frame_x, eave_y, 0.0),
            (frame_x, 0.035, ridge_z - 0.055),
            0.064,
            0.038,
            "bark_dark",
            sides=12,
            tags=("frame", "ground"),
        )
        rope_helix(
            f"{name}_lashing",
            (frame_x, 0.0, ridge_z - 0.07),
            0.072,
            0.15,
            4.5,
            axis="x",
            tags=("rope",),
        )

    # Ridgepole and eave rails define a readable, buildable frame.
    tapered_segment(
        "ridgepole",
        (front_x - 0.10, ridge_y, ridge_z),
        (rear_x + 0.10, ridge_y, ridge_z),
        0.052,
        0.047,
        "bark_dark",
        sides=12,
        tags=("ridgepole",),
    )
    for side_sign in (-1, 1):
        tapered_segment(
            f"eave_rail_{side_sign}",
            (front_x - 0.04, side_sign * eave_y, eave_z + 0.03),
            (rear_x + 0.04, side_sign * eave_y, eave_z + 0.03),
            0.034,
            0.030,
            "bark_mid",
            sides=10,
            tags=("eave_rail", "ground"),
        )

    # Six pairs of rafters create the correct triangular cross-section.
    rafter_xs = (-0.80, -0.48, -0.16, 0.16, 0.48, 0.80)
    for i, x in enumerate(rafter_xs):
        tapered_segment(
            f"rafter_l_{i}",
            (x, ridge_y, ridge_z - 0.015),
            (x, -eave_y, eave_z),
            0.030,
            0.017,
            "bark_mid",
            sides=9,
            tags=("rafter", "ground"),
        )
        tapered_segment(
            f"rafter_r_{i}",
            (x, ridge_y, ridge_z - 0.015),
            (x, eave_y, eave_z),
            0.030,
            0.017,
            "bark_mid",
            sides=9,
            tags=("rafter", "ground"),
        )

    # Purlins run along the shelter and actually support the covering.
    for side_sign in (-1, 1):
        for row_index, t in enumerate((0.40, 0.70)):
            y = side_sign * (eave_y * (1.0 - t)) * 0.93
            z = eave_z + (ridge_z - eave_z) * t - 0.045
            tapered_segment(
                f"purlin_{side_sign}_{row_index}",
                (front_x - 0.03, y, z),
                (rear_x + 0.03, y, z),
                0.023,
                0.020,
                "wood",
                sides=9,
                tags=("purlin",),
            )

    # Four overlapping shingle rows on each roof plane. Rotation is around X,
    # matching the true A-frame slope. This is the key tent correction.
    row_t_values = (0.15, 0.39, 0.63, 0.86)
    column_x_values = (-0.78, -0.47, -0.16, 0.15, 0.46, 0.77)
    for side_sign in (-1, 1):
        for row, t in enumerate(row_t_values):
            y = side_sign * (eave_y * (1.0 - t))
            z = eave_z + (ridge_z - eave_z) * t
            for col, x in enumerate(column_x_values):
                material = "bark_dark" if (row + col) % 3 == 0 else "bark_mid"
                # A few repaired hide patches add history without turning one
                # entire roof plane into a flat leather wall.
                if side_sign > 0 and (row, col) in {(1, 4), (2, 1), (3, 3)}:
                    material = "hide"
                panel_mesh(
                    f"roof_cover_{side_sign}_{row}_{col}",
                    (x + RNG.uniform(-0.025, 0.025), y, z + RNG.uniform(-0.015, 0.015)),
                    (0.38, 0.54, 0.020),
                    material,
                    rotation=(side_sign * roof_angle, 0.0, math.radians(RNG.uniform(-2.0, 2.0))),
                    irregularity=0.07,
                    seed=6100 + (side_sign + 1) * 100 + row * 10 + col,
                    tags=("cover",),
                )

    # Rear triangular closure. The front gable intentionally stays open.
    x0, x1 = rear_x - 0.015, rear_x + 0.015
    rear_vertices = [
        (x0, -0.72, 0.04), (x0, 0.72, 0.04), (x0, 0.0, ridge_z - 0.08),
        (x1, -0.72, 0.04), (x1, 0.72, 0.04), (x1, 0.0, ridge_z - 0.08),
    ]
    rear_faces = [
        (0, 1, 2), (5, 4, 3),
        (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0),
    ]
    rear_mesh = bpy.data.meshes.new("rear_gable_mesh")
    rear_mesh.from_pydata(rear_vertices, [], rear_faces)
    rear_mesh.update()
    rear_obj = bpy.data.objects.new("rear_gable", rear_mesh)
    bpy.context.collection.objects.link(rear_obj)
    set_material(rear_obj, "hide_dark")
    register(rear_obj, "rear_closure")

    # Dark, recessed bed and threshold make the entrance readable from isometry.
    primitive_box("bed", (0.12, 0.0, 0.055), (1.15, 0.72, 0.075), "earth", tags=("bed", "ground"))
    tapered_segment(
        "front_threshold",
        (front_x + 0.12, -0.52, 0.055),
        (front_x + 0.12, 0.52, 0.055),
        0.032,
        0.030,
        "bark_mid",
        sides=9,
        tags=("threshold", "ground"),
    )


def gen_lean_to(profile: AssetProfile) -> None:
    tapered_segment("post_left", (-0.72, -0.28, 0), (-0.72, -0.24, 1.25), 0.06, 0.04, "bark_dark", sides=12, tags=("frame", "ground"))
    tapered_segment("post_right", (0.72, -0.28, 0), (0.72, -0.24, 1.15), 0.06, 0.04, "bark_dark", sides=12, tags=("frame", "ground"))
    tapered_segment("lean_ridge", (-0.72, -0.24, 1.25), (0.72, -0.24, 1.15), 0.05, 0.044, "bark_dark", sides=12, tags=("ridgepole",))
    for i, x in enumerate((-0.58, -0.26, 0.06, 0.38, 0.64)):
        tapered_segment(f"lean_rafter_{i}", (x, -0.24, 1.16), (x, 0.56, 0.04), 0.026, 0.015, "bark_mid", sides=9, tags=("rafter", "ground"))
    for row in range(3):
        for col, x in enumerate((-0.50, -0.16, 0.18, 0.50)):
            panel_mesh(f"lean_cover_{row}_{col}", (x, 0.12 + row * 0.17, 0.98 - row * 0.24), (0.36, 0.54, 0.028), "bark_mid" if (row + col) % 2 else "bark_dark", rotation=(0, 0.76, 0), irregularity=0.10, seed=700 + row * 10 + col, tags=("cover",))
    primitive_box("lean_bed", (0.12, 0.18, 0.05), (0.74, 0.48, 0.08), "earth", tags=("bed", "ground"))


def gen_wall(profile: AssetProfile) -> None:
    count = 12
    for i in range(count):
        x = -1.35 + i * (2.70 / (count - 1))
        h = RNG.uniform(1.48, 1.86)
        tapered_segment(f"palisade_stake_{i}", (x, RNG.uniform(-0.03, 0.03), 0), (x + RNG.uniform(-0.05, 0.05), RNG.uniform(-0.03, 0.03), h), RNG.uniform(0.055, 0.078), 0.012, "bark_dark", sides=10, tags=("stake", "ground"))
    tapered_segment("wall_rail_low", (-1.30, -0.10, 0.72), (1.30, -0.10, 0.72), 0.045, 0.042, "bark_mid", sides=10, tags=("rail",))
    tapered_segment("wall_rail_high", (-1.28, -0.11, 1.18), (1.28, -0.11, 1.18), 0.042, 0.039, "bark_mid", sides=10, tags=("rail",))
    for x in (-1.0, -0.45, 0.10, 0.65, 1.10):
        rope_helix(f"wall_lash_{x}", (x, -0.08, 0.94), 0.055, 0.10, 3, axis="x", tags=("rope",))


def gen_spike_barrier(profile: AssetProfile) -> None:
    for i, x in enumerate((-0.85, -0.50, -0.15, 0.20, 0.55, 0.90)):
        tapered_segment(f"spike_{i}", (x, 0, 0), (x + RNG.uniform(-0.10, 0.10), RNG.uniform(-0.22, 0.22), RNG.uniform(0.90, 1.28)), 0.055, 0.008, "bark_dark", sides=9, tags=("stake", "ground"))
    tapered_segment("spike_rail", (-1.0, -0.12, 0.56), (1.0, -0.12, 0.56), 0.050, 0.046, "bark_mid", sides=10, tags=("rail",))


def gen_deadfall(profile: AssetProfile) -> None:
    scale = 1.12 if profile.asset_id.endswith("heavy") else 1.0
    irregular_log("deadfall_weight", (-0.58 * scale, -0.02, 0.18), (0.46 * scale, 0.02, 0.42), 0.11 * scale, material="bark_dark", seed=800, tags=("weight",))
    primitive_ellipsoid("back_stop", (-0.52 * scale, -0.15, 0.10), (0.18, 0.15, 0.10), "stone_dark", segments=12, rings=8, tags=("ground",))
    tapered_segment("trigger_upright", (0.18, 0, 0), (0.18, 0, 0.34), 0.022, 0.012, "wood", sides=8, tags=("trigger", "ground"))
    tapered_segment("trigger_diagonal", (0.18, 0, 0.34), (-0.02, 0, 0.16), 0.015, 0.008, "wood", sides=8, tags=("trigger",))
    tapered_segment("bait_stick", (0.18, 0, 0.10), (0.38, 0, 0.10), 0.009, 0.005, "wood", sides=7, tags=("bait_stick",))
    primitive_ellipsoid("bait", (0.42, 0, 0.11), (0.045, 0.035, 0.030), "meat", segments=10, rings=6, tags=("bait",))


def gen_snare(profile: AssetProfile) -> None:
    tapered_segment("snare_support", (-0.12, 0, 0), (0.18, 0, 0.50), 0.016, 0.007, "wood", sides=7, tags=("support", "ground"))
    tapered_segment("snare_peg", (0.14, 0, 0), (0.18, 0, 0.16), 0.012, 0.006, "bark_dark", sides=7, tags=("peg", "ground"))
    rope_helix("snare_loop", (0.04, 0, 0.06), 0.13, 0.015, 1.0, axis="x", tags=("snare", "ground"))


def gen_rack(profile: AssetProfile) -> None:
    for x in (-0.50, 0.50):
        tapered_segment(f"rack_post_{x}", (x, 0, 0), (x, 0, 1.36), 0.055, 0.038, "bark_dark", sides=10, tags=("frame", "ground"))
        # Diagonal rear brace: gives the rack real front-to-back depth (a plain
        # pair of vertical posts is planar and can't match the ~0.34 m target
        # depth on its own) and reads as a sawhorse-style stability strut.
        tapered_segment(f"rack_brace_{x}", (x, -0.30, 0), (x * 0.6, 0, 0.92), 0.028, 0.018, "bark_mid", sides=8, tags=("frame", "ground"))
    tapered_segment("rack_crossbar", (-0.55, 0, 1.28), (0.55, 0, 1.28), 0.034, 0.031, "bark_mid", sides=10, tags=("crossbar",))
    if profile.family == "tanning_rack":
        # panel_mesh's default (unrotated) frame already has width=X, height=Z,
        # thickness=Y -- exactly what a hide facing the viewer needs. The old
        # compound rotation swapped height into X and width into Y, producing
        # a short, absurdly deep panel instead of a tall thin one.
        panel_mesh("stretched_hide", (0, 0, 0.76), (0.82, 1.00, 0.024), "hide", irregularity=0.14, seed=900, tags=("hide",))
    else:
        for i, x in enumerate((-0.34, -0.11, 0.12, 0.34)):
            panel_mesh(f"meat_strip_{i}", (x, 0, 0.92), (0.08, 0.30, 0.016), "meat", irregularity=0.10, seed=910 + i, tags=("meat",))


def branch_system(prefix: str, height: float, branch_levels: Sequence[float], branch_count: int, spread: float, trunk_radius: float, foliage: bool, conifer=False, dry=False, seed=0) -> List[Tuple[float, float, float]]:
    rng = random.Random(seed)
    tapered_segment(prefix + "_trunk", (0, 0, 0), (0, 0, height), trunk_radius, trunk_radius * 0.42, "bark_dark" if not dry else "bark_mid", sides=16, tags=("trunk", "ground"))
    anchors: List[Tuple[float, float, float]] = []
    for level_index, z in enumerate(branch_levels):
        count = branch_count if not conifer else max(5, branch_count - level_index // 2)
        level_spread = spread if not conifer else max(0.35, spread - level_index * 0.12)
        for i in range(count):
            ang = i / count * math.tau + (0.22 if level_index % 2 else 0) + rng.uniform(-0.16, 0.16)
            end = (math.cos(ang) * rng.uniform(level_spread * 0.72, level_spread), math.sin(ang) * rng.uniform(level_spread * 0.72, level_spread), z + (rng.uniform(0.08, 0.30) if not conifer else -rng.uniform(0.08, 0.20)))
            mid = (end[0] * 0.56, end[1] * 0.56, z + (end[2] - z) * 0.55)
            tapered_segment(f"{prefix}_branch_{level_index}_{i}_a", (0, 0, z), mid, max(0.024, trunk_radius * 0.34), max(0.012, trunk_radius * 0.16), "bark_mid", sides=9, tags=("branch",))
            tapered_segment(f"{prefix}_branch_{level_index}_{i}_b", mid, end, max(0.012, trunk_radius * 0.15), 0.005, "wood", sides=8, tags=("branch",))
            anchors.append(end)
            if not conifer and not dry:
                for j in (-1, 1):
                    side_ang = ang + j * rng.uniform(0.24, 0.42)
                    side = (end[0] + math.cos(side_ang) * rng.uniform(0.20, 0.38), end[1] + math.sin(side_ang) * rng.uniform(0.20, 0.38), end[2] + rng.uniform(0.02, 0.12))
                    tapered_segment(f"{prefix}_twig_{level_index}_{i}_{j}", end, side, 0.006, 0.0025, "wood", sides=6, tags=("twig",))
                    anchors.append(side)
    return anchors


def foliage_cluster(prefix: str, center, radius: float, count=9, palette=("leaf", "leaf_light", "leaf_dark"), seed=0, needle=False) -> None:
    rng = random.Random(seed)
    center = Vector(center)
    for i in range(count):
        ang = rng.random() * math.tau
        elev = rng.uniform(-0.45, 0.65)
        offset = Vector((math.cos(ang) * math.cos(elev), math.sin(ang) * math.cos(elev), math.sin(elev))) * rng.uniform(radius * 0.25, radius * 0.85)
        if needle:
            primitive_ellipsoid(f"{prefix}_needle_mass_{i}", center + offset, (radius * 0.55, radius * 0.30, radius * 0.28), "needle", segments=10, rings=6, tags=("foliage",))
        else:
            leaf_mesh(f"{prefix}_leaf_{i}", center + offset, (radius * 0.32, radius * 0.54, radius * 0.06), palette[i % len(palette)], rotation=(rng.uniform(0.3, 1.2), rng.uniform(-0.4, 0.4), ang), tags=("foliage", "leaf"))


def gen_leafy_tree(profile: AssetProfile) -> None:
    idx = profile.variant_index
    height = 4.7 + idx * 0.18
    spread = 1.45 + (idx % 2) * 0.18
    anchors = branch_system(profile.asset_id, height, (1.6, 2.1, 2.7, 3.3, 3.8), 4, spread, 0.24 + idx * 0.01, True, seed=1000 + idx)
    for i, anchor in enumerate(anchors):
        foliage_cluster(f"{profile.asset_id}_crown_{i}", anchor, 0.28 + (i % 3) * 0.03, count=7, seed=1100 + i + idx * 100)
    for i in range(6):
        ang = i / 6 * math.tau
        tapered_segment(f"root_{i}", (0, 0, 0.16), (math.cos(ang) * 0.75, math.sin(ang) * 0.75, 0.02), 0.09, 0.025, "bark_dark", sides=9, tags=("root", "ground"))


def gen_conifer_tree(profile: AssetProfile) -> None:
    idx = profile.variant_index
    height = 5.0 + idx * 0.25
    levels = tuple(height - 0.85 - i * 0.50 for i in range(8 + min(idx, 2)))
    anchors = branch_system(profile.asset_id, height, levels, 9, 1.28 + idx * 0.04, 0.19 + idx * 0.01, True, conifer=True, seed=1200 + idx)
    for i, anchor in enumerate(anchors):
        foliage_cluster(f"{profile.asset_id}_needles_{i}", anchor, 0.20, count=4, seed=1300 + i + idx * 100, needle=True)
    tapered_segment("leader", (0, 0, height - 0.55), (0, 0, height + 0.24), 0.026, 0.006, "wood", sides=8, tags=("leader",))
    foliage_cluster("leader_mass", (0, 0, height + 0.02), 0.18, count=5, seed=1400 + idx, needle=True)


def gen_dry_tree(profile: AssetProfile) -> None:
    idx = profile.variant_index
    branch_system(profile.asset_id, 3.7 + idx * 0.18, (1.1, 1.7, 2.3, 2.9), 3, 1.0 + idx * 0.06, 0.17, False, dry=True, seed=1500 + idx)
    for i in range(5):
        ang = i / 5 * math.tau
        tapered_segment(f"root_{i}", (0, 0, 0.12), (math.cos(ang) * 0.50, math.sin(ang) * 0.50, 0.02), 0.07, 0.018, "bark_mid", sides=8, tags=("root", "ground"))


def gen_sapling(profile: AssetProfile) -> None:
    conifer = profile.family == "conifer_sapling"
    dry = profile.family == "dry_sapling"
    h = 1.25 + profile.variant_index * 0.08
    anchors = branch_system(profile.asset_id, h, (0.42, 0.66, 0.88, 1.06), 5 if conifer else 3, 0.34, 0.055, not dry, conifer=conifer, dry=dry, seed=1600 + profile.variant_index)
    if not dry:
        for i, anchor in enumerate(anchors):
            foliage_cluster(f"sapling_mass_{i}", anchor, 0.11 if conifer else 0.14, count=3 if conifer else 5, seed=1700 + i, needle=conifer)


def gen_rock(profile: AssetProfile) -> None:
    large = profile.asset_id.endswith("large") or profile.family == "rock_outcrop"
    main = (0.65, 0.52, 0.42) if large else (0.34, 0.28, 0.22)
    primitive_ellipsoid("rock_main", (0, 0, main[2] * 0.78), main, "stone", segments=14, rings=9, tags=("rock", "ground"))
    for i in range(5 if large else 3):
        ang = i / max(1, 5) * math.tau + RNG.uniform(-0.2, 0.2)
        r = RNG.uniform(0.12, 0.22) if large else RNG.uniform(0.07, 0.12)
        primitive_ellipsoid(f"rock_side_{i}", (math.cos(ang) * main[0] * 0.75, math.sin(ang) * main[1] * 0.75, r * 0.62), (r, r * RNG.uniform(0.72, 1.1), r * RNG.uniform(0.55, 0.85)), "stone_dark" if i % 2 else "stone", segments=12, rings=8, tags=("rock", "ground"))
    ground_scatter("rock_ground", radius=0.75 if large else 0.45, rocks=5, grass=5, seed=1800 + profile.variant_index)


def shrub_scaffold(prefix: str, stem_count: int, radius: float, height: float, seed: int) -> List[Tuple[float, float, float]]:
    rng = random.Random(seed)
    anchors: List[Tuple[float, float, float]] = []
    for i in range(stem_count):
        ang = i / stem_count * math.tau + rng.uniform(-0.28, 0.28)
        base = (rng.uniform(-0.05, 0.05), rng.uniform(-0.05, 0.05), 0)
        tip = (math.cos(ang) * rng.uniform(radius * 0.55, radius), math.sin(ang) * rng.uniform(radius * 0.55, radius), rng.uniform(height * 0.58, height))
        tapered_segment(f"{prefix}_stem_{i}", base, tip, rng.uniform(0.012, 0.020), rng.uniform(0.003, 0.006), "wood", sides=7, tags=("stem", "ground"))
        anchors.append(tip)
        if i % 2 == 0:
            mid = Vector(base).lerp(Vector(tip), 0.62)
            side_ang = ang + rng.uniform(-0.7, 0.7)
            side = (mid.x + math.cos(side_ang) * radius * 0.34, mid.y + math.sin(side_ang) * radius * 0.34, mid.z + height * 0.10)
            tapered_segment(f"{prefix}_side_{i}", mid, side, 0.008, 0.0025, "wood", sides=6, tags=("stem",))
            anchors.append(side)
    return anchors


def gen_green_bush(profile: AssetProfile) -> None:
    anchors = shrub_scaffold(profile.asset_id, 10 + profile.variant_index, 0.58 + profile.variant_index * 0.03, 0.95 + profile.variant_index * 0.04, 1900 + profile.variant_index)
    for i, anchor in enumerate(anchors):
        foliage_cluster(f"bush_mass_{i}", anchor, 0.20, count=6, seed=2000 + i + profile.variant_index * 50)
    ground_scatter("bush_ground", radius=0.55, rocks=4, grass=5, seed=2010 + profile.variant_index)


def gen_forest_shrub(profile: AssetProfile) -> None:
    anchors = shrub_scaffold(profile.asset_id, 12 + profile.variant_index, 0.50, 0.84, 2100 + profile.variant_index)
    for i, anchor in enumerate(anchors):
        foliage_cluster(f"forest_mass_{i}", anchor, 0.16, count=5, palette=("leaf_dark", "leaf", "leaf_light"), seed=2200 + i)
    ground_scatter("forest_shrub_ground", radius=0.50, rocks=5, grass=6, seed=2210 + profile.variant_index)


def gen_dry_bush(profile: AssetProfile) -> None:
    anchors = shrub_scaffold(profile.asset_id, 16 + profile.variant_index, 0.52, 0.82, 2300 + profile.variant_index)
    for i, anchor in enumerate(anchors[::3]):
        primitive_ellipsoid(f"seed_pod_{i}", anchor, (0.015, 0.015, 0.025), "rope", segments=8, rings=5, tags=("seed_pod",))
    ground_scatter("dry_bush_ground", radius=0.46, rocks=4, grass=3, seed=2310 + profile.variant_index)


def gen_berry_bush(profile: AssetProfile) -> None:
    anchors = shrub_scaffold(profile.asset_id, 12 + profile.variant_index, 0.62 + profile.variant_index * 0.02, 1.00 + profile.variant_index * 0.03, 2400 + profile.variant_index)
    for i, anchor in enumerate(anchors):
        foliage_cluster(f"berry_bush_mass_{i}", anchor, 0.18, count=5, seed=2500 + i)
        if i % 2 == 0:
            palette = ("berry_red", "berry_blue") if profile.variant_index % 2 else ("berry_red", "berry_dark")
            berry_cluster(f"berry_bush_cluster_{i}", anchor, 3 + (i % 3), palette=palette, seed=2600 + i)
    ground_scatter("berry_bush_ground", radius=0.58, rocks=4, grass=5, seed=2610 + profile.variant_index)


def gen_ground_patch(profile: AssetProfile) -> None:
    gen_grass_field(profile, blade_count=36, height=0.42, radius=0.30, flowers=5)


def gen_grass_field(profile: AssetProfile, blade_count: int, height: float, radius: float, flowers: int = 0) -> None:
    for i in range(blade_count):
        ang = RNG.random() * math.tau
        dist = RNG.uniform(0.0, radius)
        start = (math.cos(ang) * dist, math.sin(ang) * dist, 0)
        tapered_segment(f"blade_{i}", start, (start[0] + RNG.uniform(-0.04, 0.04), start[1] + RNG.uniform(-0.04, 0.04), RNG.uniform(height * 0.62, height)), 0.0055, 0.001, "leaf_light" if i % 3 else "leaf", sides=5, tags=("grass", "ground"))
    colors = ("flower_white", "flower_yellow", "flower_purple")
    for i in range(flowers):
        ang = RNG.random() * math.tau
        dist = RNG.uniform(0.0, radius * 0.85)
        flower(f"flower_{i}", (math.cos(ang) * dist, math.sin(ang) * dist, 0), colors[i % len(colors)], RNG.uniform(height * 0.48, height * 0.88), seed=2700 + i)


def gen_tall_grass(profile: AssetProfile) -> None:
    gen_grass_field(profile, 42 + profile.variant_index * 4, 0.82 + profile.variant_index * 0.06, 0.34 + profile.variant_index * 0.02, flowers=2)


def gen_wildflowers(profile: AssetProfile) -> None:
    gen_grass_field(profile, 30 + profile.variant_index * 3, 0.50 + profile.variant_index * 0.04, 0.34, flowers=10 + profile.variant_index * 2)


def gen_reeds(profile: AssetProfile) -> None:
    for i in range(24 + profile.variant_index * 4):
        ang = RNG.random() * math.tau
        dist = RNG.uniform(0.0, 0.30 + profile.variant_index * 0.02)
        start = (math.cos(ang) * dist, math.sin(ang) * dist, 0)
        h = RNG.uniform(0.72, 1.08)
        tapered_segment(f"reed_{i}", start, (start[0] + RNG.uniform(-0.03, 0.03), start[1] + RNG.uniform(-0.03, 0.03), h), 0.007, 0.0025, "leaf", sides=5, tags=("reed", "ground"))
        if i % 3 == 0:
            primitive_ellipsoid(f"reed_head_{i}", (start[0], start[1], h * 0.96), (0.015, 0.015, 0.045), "rope", segments=8, rings=5, tags=("seed_head",))


def gen_mushrooms(profile: AssetProfile) -> None:
    cap_mat = "berry_red" if profile.variant_index == 2 else "wood"
    for i in range(7 + profile.variant_index):
        ang = RNG.random() * math.tau
        dist = RNG.uniform(0.0, 0.20)
        x, y = math.cos(ang) * dist, math.sin(ang) * dist
        h = RNG.uniform(0.055, 0.13)
        tapered_segment(f"mushroom_stem_{i}", (x, y, 0), (x, y, h), 0.012, 0.009, "bone", sides=7, tags=("stem", "ground"))
        primitive_ellipsoid(f"mushroom_cap_{i}", (x, y, h), (RNG.uniform(0.035, 0.055), RNG.uniform(0.035, 0.055), RNG.uniform(0.016, 0.026)), cap_mat, segments=10, rings=6, tags=("cap",))


def gen_herbs(profile: AssetProfile) -> None:
    for i in range(16 + profile.variant_index * 2):
        ang = RNG.random() * math.tau
        dist = RNG.uniform(0.0, 0.22)
        leaf_mesh(f"herb_leaf_{i}", (math.cos(ang) * dist, math.sin(ang) * dist, RNG.uniform(0.04, 0.10)), (RNG.uniform(0.045, 0.065), RNG.uniform(0.10, 0.16), 0.015), "leaf_light" if i % 2 else "leaf", rotation=(RNG.uniform(0.7, 1.25), RNG.uniform(-0.3, 0.3), ang), tags=("leaf", "ground"))


def find_authored_base(name: str) -> Optional[bpy.types.Object]:
    candidates = [name, f"AS_BASE_{name}", f"{name}_base"]
    for candidate in candidates:
        obj = bpy.data.objects.get(candidate)
        if obj and obj.type == "MESH":
            return obj
    return None


def use_authored_or_fallback(profile: AssetProfile, fallback: Callable[[AssetProfile], None]) -> None:
    base = find_authored_base(profile.family)
    if base:
        obj = base.copy()
        obj.data = base.data.copy()
        bpy.context.collection.objects.link(obj)
        obj.name = profile.asset_id + "_authored_base"
        obj.hide_render = False
        obj.hide_viewport = False
        register(obj, "authored_base", "body", "ground")
    else:
        if ACTIVE_ROOT:
            ACTIVE_ROOT["as_warning"] = "No authored base mesh found; anatomical fallback used. Replace with approved base mesh for production."
        fallback(profile)


def fallback_small_prey(profile: AssetProfile) -> None:
    primitive_ellipsoid("body", (0, 0, 0.30), (0.34, 0.18, 0.20), "fur", tags=("body",))
    primitive_ellipsoid("rump", (-0.23, 0, 0.30), (0.19, 0.18, 0.18), "fur", tags=("body",))
    primitive_ellipsoid("chest", (0.22, 0, 0.31), (0.16, 0.15, 0.17), "fur", tags=("body",))
    primitive_ellipsoid("head", (0.42, 0, 0.42), (0.13, 0.11, 0.13), "fur", tags=("head",))
    primitive_ellipsoid("muzzle", (0.53, 0, 0.39), (0.07, 0.07, 0.055), "fur_light", tags=("muzzle",))
    for sign in (-1, 1):
        primitive_ellipsoid(f"ear_{sign}", (0.39, sign * 0.035, 0.59), (0.040, 0.024, 0.17), "fur", tags=("ear",))
    for idx, y in enumerate((-0.09, 0.09)):
        tapered_segment(f"hind_thigh_{idx}", (-0.16, y, 0.25), (-0.20, y, 0.13), 0.055, 0.040, "fur", sides=9, tags=("hind_leg",))
        tapered_segment(f"hind_shin_{idx}", (-0.20, y, 0.13), (-0.06, y, 0.035), 0.034, 0.020, "fur", sides=8, tags=("hind_leg", "ground"))
    for idx, y in enumerate((-0.06, 0.06)):
        tapered_segment(f"front_leg_{idx}", (0.20, y, 0.25), (0.23, y, 0.03), 0.022, 0.014, "fur", sides=8, tags=("front_leg", "ground"))
    primitive_ellipsoid("tail", (-0.38, 0, 0.33), (0.055, 0.055, 0.055), "fur_light", tags=("tail",))


def fallback_grazer(profile: AssetProfile) -> None:
    primitive_ellipsoid("body", (0, 0, 0.90), (0.72, 0.32, 0.38), "fur", tags=("body",))
    primitive_ellipsoid("chest", (0.54, 0, 0.96), (0.31, 0.28, 0.33), "fur", tags=("body",))
    primitive_ellipsoid("hip", (-0.54, 0, 0.86), (0.30, 0.28, 0.31), "fur", tags=("body",))
    tapered_segment("neck", (0.50, 0, 1.08), (0.88, 0, 1.44), 0.13, 0.08, "fur", sides=12, tags=("neck",))
    primitive_ellipsoid("head", (1.04, 0, 1.50), (0.25, 0.16, 0.20), "fur_light", tags=("head",))
    primitive_ellipsoid("muzzle", (1.25, 0, 1.42), (0.14, 0.12, 0.10), "fur_light", tags=("muzzle",))
    for i, (x, y) in enumerate(((-0.48, -0.20), (-0.48, 0.20), (0.36, -0.18), (0.36, 0.18))):
        tapered_segment(f"leg_upper_{i}", (x, y, 0.82), (x, y, 0.34), 0.080, 0.050, "fur", sides=9, tags=("leg",))
        tapered_segment(f"leg_lower_{i}", (x, y, 0.34), (x + 0.03, y, 0.05), 0.048, 0.030, "fur_light", sides=8, tags=("leg", "ground"))
    for sign in (-1, 1):
        tapered_segment(f"horn_{sign}", (1.00, sign * 0.08, 1.66), (1.12, sign * 0.10, 1.94), 0.025, 0.008, "bone", sides=8, tags=("horn",))


def fallback_varnak(profile: AssetProfile) -> None:
    primitive_ellipsoid("body", (0, 0, 0.78), (0.74, 0.32, 0.34), "fur_dark", tags=("body",))
    primitive_ellipsoid("shoulder", (0.42, 0, 0.92), (0.34, 0.30, 0.32), "fur_dark", tags=("body",))
    primitive_ellipsoid("hip", (-0.50, 0, 0.68), (0.30, 0.27, 0.28), "fur", tags=("body",))
    tapered_segment("neck", (0.46, 0, 0.96), (0.84, 0, 1.02), 0.13, 0.08, "fur_dark", sides=12, tags=("neck",))
    primitive_ellipsoid("head", (1.02, 0, 1.02), (0.25, 0.17, 0.19), "fur_dark", tags=("head",))
    primitive_ellipsoid("muzzle", (1.27, 0, 0.97), (0.16, 0.12, 0.10), "fur", tags=("muzzle",))
    for i, (x, y, front) in enumerate(((-0.42, -0.18, False), (-0.42, 0.18, False), (0.42, -0.16, True), (0.42, 0.16, True))):
        top_z = 0.82 if front else 0.68
        tapered_segment(f"leg_upper_{i}", (x, y, top_z), (x + (0.04 if front else -0.02), y, 0.34), 0.075, 0.045, "fur_dark", sides=9, tags=("leg",))
        tapered_segment(f"leg_lower_{i}", (x, y, 0.34), (x + (0.08 if front else -0.04), y, 0.06), 0.045, 0.028, "fur", sides=8, tags=("leg", "ground"))
    curve_tube("tail", [(-0.72, 0, 0.82), (-0.98, 0, 0.72), (-1.18, 0, 0.58)], 0.035, "fur_dark", tags=("tail",))


def gen_creature(profile: AssetProfile) -> None:
    fallback = {"small_prey": fallback_small_prey, "grazer": fallback_grazer, "varnak": fallback_varnak}[profile.family]
    use_authored_or_fallback(profile, fallback)


def gen_old_tree(profile: AssetProfile) -> None:
    anchors = branch_system("old_tree", 5.8, (1.7, 2.3, 2.9, 3.5, 4.1, 4.7), 4, 2.15, 0.55, True, seed=3000)
    for i in range(9):
        ang = i / 9 * math.tau
        tapered_segment(f"old_root_{i}", (0, 0, 0.18), (math.cos(ang) * 1.55, math.sin(ang) * 1.55, 0.02), 0.16, 0.045, "bark_dark", sides=12, tags=("root", "ground"))
    for i, anchor in enumerate(anchors):
        foliage_cluster(f"old_crown_{i}", anchor, 0.42, count=10, seed=3100 + i)
    primitive_box("old_tree_cavity", (0.20, 0.36, 1.42), (0.28, 0.10, 0.46), "charcoal", rotation=(math.radians(90), 0, math.radians(12)), tags=("cavity",))


def gen_ruins(profile: AssetProfile) -> None:
    primitive_box("ruin_ground", (0, 0, 0.04), (4.0, 3.0, 0.08), "earth", tags=("ground",))
    blocks = [(-1.20, -0.60, 0.42, 0.38, 0.30, 0.84), (-0.72, -0.58, 0.62, 0.38, 0.30, 1.24), (-0.24, -0.56, 0.78, 0.38, 0.30, 1.56), (0.42, 0.58, 0.50, 0.36, 0.28, 1.00), (0.88, 0.56, 0.68, 0.36, 0.28, 1.36)]
    for i, (x, y, z, sx, sy, sz) in enumerate(blocks):
        primitive_box(f"ruin_wall_{i}", (x, y, z), (sx, sy, sz), "stone" if i % 2 else "stone_dark", rotation=(0, 0, math.radians(RNG.uniform(-8, 8))), tags=("masonry", "ground"))
    for i in range(18):
        primitive_ellipsoid(f"rubble_{i}", (RNG.uniform(-1.65, 1.65), RNG.uniform(-1.10, 1.10), RNG.uniform(0.04, 0.12)), (RNG.uniform(0.06, 0.18), RNG.uniform(0.05, 0.16), RNG.uniform(0.04, 0.12)), "stone" if i % 2 else "stone_dark", segments=10, rings=6, tags=("rubble", "ground"))
    for i in range(8):
        ang = RNG.random() * math.tau
        flower(f"ruin_flower_{i}", (math.cos(ang) * RNG.uniform(0.4, 1.4), math.sin(ang) * RNG.uniform(0.4, 1.0), 0.08), ("flower_white", "flower_yellow", "flower_purple")[i % 3], 0.25, seed=3200 + i)


def gen_pond(profile: AssetProfile) -> None:
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=2.0, depth=0.12, location=(0, 0, 0.03))
    shore = bpy.context.object
    shore.name = "pond_shore"
    shore.scale = (1.15, 0.92, 1.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    set_material(shore, "mud")
    register(shore, "shore", "ground")
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=1.55, depth=0.035, location=(0.04, -0.03, 0.09))
    water = bpy.context.object
    water.name = "pond_water"
    water.scale = (1.16, 0.86, 1.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    set_material(water, "water")
    register(water, "water")
    for i in range(22):
        ang = i / 22 * math.tau + RNG.uniform(-0.08, 0.08)
        dist = RNG.uniform(1.55, 2.05)
        primitive_ellipsoid(f"shore_stone_{i}", (math.cos(ang) * dist, math.sin(ang) * dist, 0.09), (RNG.uniform(0.07, 0.16), RNG.uniform(0.06, 0.14), RNG.uniform(0.05, 0.11)), "stone", segments=10, rings=6, tags=("shore_stone", "ground"))
    for patch, center in enumerate(((1.25, 0.42), (-1.18, -0.36))):
        for i in range(18):
            x = center[0] + RNG.uniform(-0.22, 0.22)
            y = center[1] + RNG.uniform(-0.18, 0.18)
            h = RNG.uniform(0.55, 0.98)
            tapered_segment(f"reed_{patch}_{i}", (x, y, 0.06), (x + RNG.uniform(-0.03, 0.03), y + RNG.uniform(-0.03, 0.03), h), 0.006, 0.002, "leaf", sides=5, tags=("reed", "ground"))


def gen_camp(profile: AssetProfile) -> None:
    # Hero composition: generate sub-assets and offset their newly created objects.
    before = len(ACTIVE_PARTS)
    fake = AssetProfile(**{**asdict(profile), "asset_id": "campfire", "family": "campfire"})
    gen_campfire(fake)
    for obj in ACTIVE_PARTS[before:]:
        obj.location.x -= 1.1
        obj.location.y += 0.25
    before = len(ACTIVE_PARTS)
    fake = AssetProfile(**{**asdict(profile), "asset_id": "tent", "family": "tent"})
    gen_tent(fake)
    for obj in ACTIVE_PARTS[before:]:
        obj.location.x += 1.15
        obj.location.y += 0.10
        obj.rotation_euler.rotate_axis("Z", math.radians(-15))
    before = len(ACTIVE_PARTS)
    fake = AssetProfile(**{**asdict(profile), "asset_id": "storage_box", "family": "storage_box"})
    gen_storage_box(fake)
    for obj in ACTIVE_PARTS[before:]:
        obj.location.y -= 1.05
    ground_scatter("camp_dressing", radius=2.2, rocks=14, grass=16, seed=3300)


def gen_cave(profile: AssetProfile) -> None:
    primitive_box("cave_ground", (0, 0, 0.04), (4.0, 3.0, 0.08), "earth", tags=("ground",))
    specs = [(-1.15, 0, 0.78, (0.58, 0.50, 0.95)), (1.15, 0, 0.82, (0.60, 0.52, 1.00)), (0, -0.72, 1.18, (1.45, 0.52, 0.48)), (0, 0.95, 0.70, (1.20, 0.44, 0.38))]
    for i, (x, y, z, scale) in enumerate(specs):
        primitive_ellipsoid(f"cave_rock_{i}", (x, y, z), scale, "stone_dark" if i % 2 else "stone", segments=14, rings=9, tags=("rock", "ground"))
    primitive_box("cave_dark_mouth", (0, 0.08, 0.74), (1.05, 0.10, 1.18), "charcoal", tags=("cave_mouth",))
    for i in range(14):
        primitive_ellipsoid(f"cave_rubble_{i}", (RNG.uniform(-0.75, 0.75), RNG.uniform(-0.22, 0.30), RNG.uniform(0.05, 0.22)), (RNG.uniform(0.08, 0.20), RNG.uniform(0.07, 0.18), RNG.uniform(0.06, 0.16)), "stone", segments=10, rings=6, tags=("rubble", "ground"))


FAMILY_GENERATORS: Dict[str, Callable[[AssetProfile], None]] = {
    "wood": gen_wood,
    "stone_pickup": gen_stone_pickup,
    "fiber": gen_fiber,
    "grass_pickup": gen_grass_pickup,
    "meat": gen_meat,
    "hide": gen_hide,
    "bone": gen_bone,
    "berries_pickup": gen_berries_pickup,
    "kindling": gen_kindling,
    "flint": gen_flint,
    "reed_bundle": gen_reed_bundle,
    "torch": gen_torch,
    "spear": gen_spear,
    "bow": gen_bow,
    "arrow": lambda p: gen_arrow(p),
    "arrow_bundle": gen_arrow_bundle,
    "axe": gen_axe,
    "pickaxe": gen_pickaxe,
    "campfire": gen_campfire,
    "campfire_burned": gen_campfire,
    "storage_box": gen_storage_box,
    "tent": gen_tent,
    "lean_to": gen_lean_to,
    "wall": gen_wall,
    "spike_barrier": gen_spike_barrier,
    "deadfall": gen_deadfall,
    "snare": gen_snare,
    "tanning_rack": gen_rack,
    "drying_rack": gen_rack,
    "leafy_tree": gen_leafy_tree,
    "conifer_tree": gen_conifer_tree,
    "dry_tree": gen_dry_tree,
    "leafy_sapling": gen_sapling,
    "conifer_sapling": gen_sapling,
    "dry_sapling": gen_sapling,
    "rock_outcrop": gen_rock,
    "rock_cluster": gen_rock,
    "green_bush": gen_green_bush,
    "forest_shrub": gen_forest_shrub,
    "dry_bush": gen_dry_bush,
    "berry_bush": gen_berry_bush,
    "ground_patch": gen_ground_patch,
    "tall_grass": gen_tall_grass,
    "wildflowers": gen_wildflowers,
    "reeds": gen_reeds,
    "mushrooms": gen_mushrooms,
    "herbs": gen_herbs,
    "small_prey": gen_creature,
    "grazer": gen_creature,
    "varnak": gen_creature,
    "old_tree": gen_old_tree,
    "ruins": gen_ruins,
    "pond": gen_pond,
    "camp": gen_camp,
    "cave": gen_cave,
}


# -----------------------------------------------------------------------------
# Validation / output
# -----------------------------------------------------------------------------


def world_bounds(objects: Sequence[bpy.types.Object]) -> Tuple[Vector, Vector]:
    points: List[Vector] = []
    for obj in objects:
        if obj.type not in {"MESH", "CURVE"}:
            continue
        for corner in obj.bound_box:
            points.append(obj.matrix_world @ Vector(corner))
    if not points:
        return Vector((0, 0, 0)), Vector((0, 0, 0))
    mins = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maxs = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return mins, maxs


def tag_counts(objects: Sequence[bpy.types.Object]) -> Dict[str, int]:
    counts: Dict[str, int] = {}
    for obj in objects:
        for tag in obj.get("as_tags", []):
            counts[tag] = counts.get(tag, 0) + 1
    return counts


def required_tags(profile: AssetProfile) -> Dict[str, int]:
    family_rules = {
        "wood": {"wood": 5, "rope": 2},
        "berries_pickup": {"stem": 2, "pedicel": 4, "fruit": 6, "leaf": 2},
        "spear": {"shaft": 1, "head": 1, "rope": 1},
        "bow": {"stave": 1, "string": 1, "grip": 1},
        "axe": {"shaft": 1, "head": 1, "rope": 1},
        "tent": {"frame": 4, "ridgepole": 1, "rafter": 12, "cover": 48, "purlin": 4, "eave_rail": 2, "rear_closure": 1, "bed": 1},
        "deadfall": {"weight": 1, "trigger": 2, "bait": 1},
        "berry_bush": {"stem": 10, "fruit": 6, "leaf": 20},
        "conifer_tree": {"trunk": 1, "branch": 30, "foliage": 40, "leader": 1},
        "small_prey": {"body": 1, "head": 1},
        "old_tree": {"trunk": 1, "root": 6, "branch": 12, "foliage": 20},
    }
    return family_rules.get(profile.family, {})




def normalize_asset(profile: AssetProfile, objects: Sequence[bpy.types.Object]) -> None:
    """Apply one conservative uniform scale and ground the asset at z=0.

    This is not used to turn one variant into another. It only corrects small
    authoring drift after the family generator has established the silhouette.
    """
    if not objects or not ACTIVE_ROOT:
        return
    bpy.context.view_layer.update()
    mins, maxs = world_bounds(objects)
    ext = maxs - mins
    target = Vector(profile.target_size_m)
    ratios = [target[i] / ext[i] for i in range(3) if ext[i] > 1e-5 and target[i] > 1e-5]
    if ratios:
        ratios.sort()
        factor = ratios[len(ratios) // 2]
        factor = max(0.62, min(1.65, factor))
        if factor < 0.82 or factor > 1.22:
            ACTIVE_ROOT.scale = (factor, factor, factor)
            bpy.context.view_layer.update()
    mins, _ = world_bounds(objects)
    ACTIVE_ROOT.location.z -= mins.z
    bpy.context.view_layer.update()

def audit(profile: AssetProfile, objects: Sequence[bpy.types.Object]) -> Dict:
    mins, maxs = world_bounds(objects)
    extents = maxs - mins
    target = Vector(profile.target_size_m)
    ratios = Vector((extents.x / max(target.x, 1e-6), extents.y / max(target.y, 1e-6), extents.z / max(target.z, 1e-6)))
    counts = tag_counts(objects)
    checks = [
        {"name": "part_count", "pass": len(objects) >= 2, "detail": f"parts={len(objects)}"},
        {"name": "ground_contact", "pass": mins.z <= 0.06, "detail": f"min_z={mins.z:.4f}"},
        {"name": "scale_envelope", "pass": all(0.35 <= value <= 2.20 for value in ratios), "detail": f"ratios={tuple(round(v, 3) for v in ratios)}"},
        {"name": "materials", "pass": sum(len(getattr(obj.data, 'materials', [])) for obj in objects if hasattr(obj, 'data') and obj.data) > 0, "detail": "material slots present"},
    ]
    for tag, minimum in required_tags(profile).items():
        actual = counts.get(tag, 0)
        checks.append({"name": f"required_{tag}", "pass": actual >= minimum, "detail": f"actual={actual}, required={minimum}"})
    score = round(sum(1 for check in checks if check["pass"]) / len(checks) * 10.0, 2)
    return {
        "asset_id": profile.asset_id,
        "family": profile.family,
        "category": profile.category,
        "parts": len(objects),
        "bounds_min": tuple(round(v, 4) for v in mins),
        "bounds_max": tuple(round(v, 4) for v in maxs),
        "extents": tuple(round(v, 4) for v in extents),
        "target_size": profile.target_size_m,
        "tag_counts": counts,
        "score_10": score,
        "checks": checks,
        "warning": ACTIVE_ROOT.get("as_warning", "") if ACTIVE_ROOT else "",
    }


def configure_render() -> None:
    scene = bpy.context.scene
    # The EEVEE identifier changed between Blender versions ("BLENDER_EEVEE" ->
    # "BLENDER_EEVEE_NEXT" in 4.2 -> back to "BLENDER_EEVEE" in 4.5+/5.x), so
    # pick whichever this build actually exposes instead of hardcoding one.
    available = {item.identifier for item in type(scene.render).bl_rna.properties["engine"].enum_items}
    for candidate in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "CYCLES"):
        if candidate in available:
            scene.render.engine = candidate
            break
    scene.render.resolution_x = 720
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    # Previews are a QA instrument, so show true material colours instead of the
    # default filmic/AgX grade.
    if hasattr(scene, "view_settings"):
        transforms = {item.identifier for item in type(scene.view_settings).bl_rna.properties["view_transform"].enum_items}
        if "Standard" in transforms:
            scene.view_settings.view_transform = "Standard"
        scene.view_settings.look = "None"
        scene.view_settings.exposure = 0.0
    scene.world.color = (0.055, 0.050, 0.045)


def setup_preview_scene(objects: Sequence[bpy.types.Object]) -> Tuple[bpy.types.Object, bpy.types.Object]:
    configure_render()
    mins, maxs = world_bounds(objects)
    center = (mins + maxs) * 0.5
    extents = maxs - mins
    # Bounding-sphere radius keeps framing identical at every asset scale. The
    # previous formula added a fixed 0.35 m pad, which dominated small pickups
    # (a 0.3 m item filled only a quarter of the frame) while barely affecting
    # trees, so previews were not comparable across the pack.
    radius = max(extents.length * 0.5, 0.02)

    # Sun lamps keep exposure identical for a 0.2 m berry pickup and a 7 m tree.
    # Area lights with a fixed wattage blew out small assets and underlit large
    # ones, because their distance scales with the asset bounding radius.
    bpy.ops.object.light_add(type="SUN", location=(center.x - radius, center.y - radius, center.z + radius * 1.4))
    key = bpy.context.object
    key.name = "Preview_Key"
    key.data.energy = 3.6
    key.data.angle = math.radians(9.0)
    key.rotation_euler = Euler((math.radians(52.0), 0.0, math.radians(-42.0)), "XYZ")

    bpy.ops.object.light_add(type="SUN", location=(center.x + radius, center.y + radius, center.z + radius * 0.7))
    fill = bpy.context.object
    fill.name = "Preview_Fill"
    fill.data.energy = 1.15
    fill.data.angle = math.radians(28.0)
    fill.rotation_euler = Euler((math.radians(66.0), 0.0, math.radians(138.0)), "XYZ")

    bpy.ops.mesh.primitive_plane_add(size=radius * 12.0, location=(center.x, center.y, min(0, mins.z) - 0.01))
    ground = bpy.context.object
    ground.name = "Preview_Ground"
    set_material(ground, "preview_ground")

    bpy.ops.object.camera_add()
    camera = bpy.context.object
    camera.name = "Preview_Camera"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = radius * 2.30
    camera.data.clip_start = 0.001
    camera.data.clip_end = radius * 40.0
    camera.location = (center.x + radius * 1.7, center.y - radius * 1.7, center.z + radius * 1.45)
    direction = center - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = camera
    return camera, ground


def render_preview(profile: AssetProfile, objects: Sequence[bpy.types.Object], output_dir: Path) -> List[str]:
    camera, ground = setup_preview_scene(objects)
    paths: List[str] = []
    original = camera.location.copy()
    mins, maxs = world_bounds(objects)
    center = (mins + maxs) * 0.5
    for index, angle in enumerate((0, 90, 180, 270)):
        rad = math.radians(angle)
        offset = original - center
        horizontal = math.sqrt(offset.x * offset.x + offset.y * offset.y)
        camera.location.x = center.x + math.cos(rad - math.radians(45)) * horizontal
        camera.location.y = center.y + math.sin(rad - math.radians(45)) * horizontal
        direction = center - camera.location
        camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
        path = output_dir / f"{profile.asset_id}_view_{index}.png"
        bpy.context.scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        paths.append(str(path))
    bpy.data.objects.remove(camera, do_unlink=True)
    bpy.data.objects.remove(ground, do_unlink=True)
    for name in ("Preview_Key", "Preview_Fill"):
        obj = bpy.data.objects.get(name)
        if obj:
            bpy.data.objects.remove(obj, do_unlink=True)
    return paths


def select_asset_objects(objects: Sequence[bpy.types.Object]) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    if ACTIVE_ROOT:
        ACTIVE_ROOT.select_set(True)
    for obj in objects:
        obj.select_set(True)
    if objects:
        bpy.context.view_layer.objects.active = objects[0]


def export_asset(profile: AssetProfile, objects: Sequence[bpy.types.Object], output_root: Path) -> Dict[str, str]:
    category_dir = output_root / CATEGORY_DIRS[profile.category]
    category_dir.mkdir(parents=True, exist_ok=True)
    source_dir = output_root / "Source" / "Blend"
    source_dir.mkdir(parents=True, exist_ok=True)
    select_asset_objects(objects)
    glb_path = category_dir / f"{profile.asset_id}.glb"
    fbx_path = category_dir / f"{profile.asset_id}.fbx"
    blend_path = source_dir / f"{profile.asset_id}.blend"
    bpy.ops.export_scene.gltf(filepath=str(glb_path), export_format="GLB", use_selection=True, export_apply=True)
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        apply_unit_scale=True,
        # Apply Blender's Z-up -> Unity Y-up conversion to the mesh data. Leaving
        # this false stores the conversion in FBX node transforms, which Unity
        # imports inconsistently across generated vegetation and placeables.
        bake_space_transform=True,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
    )
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), copy=True)
    return {"glb": str(glb_path), "fbx": str(fbx_path), "blend": str(blend_path)}


def generate_one(profile: AssetProfile, output_root: Path, export_enabled: bool, preview_enabled: bool) -> Dict:
    global ACTIVE_PROFILE, ACTIVE_ROOT, ACTIVE_PARTS
    ensure_clean_scene()
    ACTIVE_PROFILE = profile
    ACTIVE_PARTS = []
    ACTIVE_ROOT = create_empty(profile.asset_id + "_ROOT")
    ACTIVE_ROOT["as_profile"] = json.dumps(asdict(profile), ensure_ascii=False)
    ACTIVE_ROOT["as_reference_source"] = profile.reference_source_page
    ACTIVE_ROOT["as_generator_version"] = "10"

    generator = FAMILY_GENERATORS.get(profile.family)
    if not generator:
        raise KeyError(f"No generator registered for family {profile.family!r} ({profile.asset_id})")
    generator(profile)
    normalize_asset(profile, ACTIVE_PARTS)

    audit_data = audit(profile, ACTIVE_PARTS)
    preview_paths: List[str] = []
    export_paths: Dict[str, str] = {}
    if preview_enabled:
        preview_dir = output_root / "Previews" / profile.asset_id
        preview_dir.mkdir(parents=True, exist_ok=True)
        preview_paths = render_preview(profile, ACTIVE_PARTS, preview_dir)
    if export_enabled:
        export_paths = export_asset(profile, ACTIVE_PARTS, output_root)
    audit_data["preview_paths"] = preview_paths
    audit_data["export_paths"] = export_paths
    return audit_data


def write_reports(output_root: Path, profiles: Sequence[AssetProfile], results: Sequence[Dict]) -> None:
    docs = output_root / "Docs"
    docs.mkdir(parents=True, exist_ok=True)
    (docs / "asset_audit_v10.json").write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
    manifest = {"version": 10, "asset_count": len(profiles), "assets": [asdict(profile) for profile in profiles]}
    (output_root / "asset_manifest_v10.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    lines = [
        "# Apex Shift Blender Generator v10.1.1 — audit",
        "",
        f"Generated assets: **{len(results)}**",
        "",
        "| Asset | Family | Category | Score /10 | Parts | Warning |",
        "|---|---|---|---:|---:|---|",
    ]
    for result in results:
        warning = result.get("warning", "").replace("|", "/")
        lines.append(f"| `{result['asset_id']}` | `{result['family']}` | {result['category']} | {result['score_10']:.2f} | {result['parts']} | {warning} |")
    lines += ["", "## Failed checks", ""]
    for result in results:
        failed = [check for check in result["checks"] if not check["pass"]]
        if not failed:
            continue
        lines.append(f"### `{result['asset_id']}`")
        for check in failed:
            lines.append(f"- `{check['name']}` — {check['detail']}")
        lines.append("")
    (docs / "asset_audit_v10.md").write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    args = parse_args()
    global RNG
    RNG = random.Random(args.seed)
    profiles = load_profiles(ROOT / "apex_shift_asset_visual_specs_v5.json")
    coverage = validate_profile_coverage(profiles)
    if coverage:
        raise RuntimeError("Profile coverage failed: " + "; ".join(coverage))
    if len(profiles) != 98:
        raise RuntimeError(f"Full pack must contain 98 profiles, found {len(profiles)}")

    only = {item.strip() for item in args.only.split(",") if item.strip()}
    selected = [profile for profile in profiles if not only or profile.asset_id in only]
    if only:
        missing = sorted(only - {profile.asset_id for profile in selected})
        if missing:
            raise KeyError("Unknown --only asset ids: " + ", ".join(missing))
    output_root = Path(args.output).resolve()
    output_root.mkdir(parents=True, exist_ok=True)

    results: List[Dict] = []
    failures: List[Tuple[str, str]] = []
    for index, profile in enumerate(selected, 1):
        print(f"[{index}/{len(selected)}] Generating {profile.asset_id} ({profile.family})")
        try:
            result = generate_one(profile, output_root, not args.no_export, not args.no_preview)
        except Exception as exc:  # keep the remaining pack generating
            import traceback

            traceback.print_exc()
            failures.append((profile.asset_id, f"{type(exc).__name__}: {exc}"))
            print(f"  FAILED {profile.asset_id}: {type(exc).__name__}: {exc}")
            continue
        results.append(result)
        failed = [check["name"] for check in result["checks"] if not check["pass"]]
        print(f"  score={result['score_10']:.2f}/10 parts={result['parts']} failed={failed}")
    generated_ids = {result["asset_id"] for result in results}
    write_reports(output_root, [p for p in selected if p.asset_id in generated_ids], results)
    if failures:
        print(f"\n{len(failures)} asset(s) FAILED:")
        for asset_id, message in failures:
            print(f"  - {asset_id}: {message}")
    print(f"Finished. Generated {len(results)}/{len(selected)}. Output: {output_root}")


if __name__ == "__main__":
    main()
