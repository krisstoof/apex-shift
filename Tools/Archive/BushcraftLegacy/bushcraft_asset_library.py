"""
Reusable Blender builders for Apex Shift bushcraft assets.

This module favors stylized mid-poly building blocks over placeholder meshes.
It is intended to be imported from Blender and executed through Blender MCP or
`blender --python`.
"""

from __future__ import annotations

import math
import random
from pathlib import Path
from typing import Iterable, Sequence

import bpy
import bmesh
from mathutils import Euler, Matrix, Vector


REPO_ROOT = Path(__file__).resolve().parents[2]
ASSET_ROOT = REPO_ROOT / "Assets" / "_Project" / "Art" / "Bushcraft"
MATERIAL_ROOT = ASSET_ROOT / "Materials"


def _link_object(obj: bpy.types.Object, collection: bpy.types.Collection | None = None) -> bpy.types.Object:
    target = collection or bpy.context.collection
    if obj.name not in target.objects:
        target.objects.link(obj)
    return obj


def _new_mesh_object(name: str, collection: bpy.types.Collection | None = None) -> bpy.types.Object:
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    return _link_object(obj, collection)


def _apply_matrix_to_bmesh(bm: bmesh.types.BMesh, matrix: Matrix) -> None:
    for vert in bm.verts:
        vert.co = matrix @ vert.co


def _finish_mesh(obj: bpy.types.Object, bm: bmesh.types.BMesh) -> bpy.types.Object:
    bm.normal_update()
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    return obj


def ensure_collection(name: str) -> bpy.types.Collection:
    collection = bpy.data.collections.get(name)
    if collection:
        return collection
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    return collection


def cleanup_generated_collection(name: str) -> None:
    collection = bpy.data.collections.get(name)
    if not collection:
        return
    for obj in list(collection.objects):
        bpy.data.objects.remove(obj, do_unlink=True)


def create_branch_segment(
    name: str,
    length: float = 1.2,
    radius_start: float = 0.10,
    radius_end: float = 0.07,
    bend: float = 0.10,
    cut_bias: float = 0.08,
    collection: bpy.types.Collection | None = None,
) -> bpy.types.Object:
    obj = _new_mesh_object(name, collection)
    bm = bmesh.new()
    geom = bmesh.ops.create_cone(
        bm,
        cap_ends=True,
        cap_tris=False,
        segments=10,
        radius1=radius_start,
        radius2=radius_end,
        depth=length,
    )
    _apply_matrix_to_bmesh(bm, Matrix.Rotation(math.radians(90.0), 4, "Y"))
    for vert in bm.verts:
        t = (vert.co.x / max(length * 0.5, 0.001)) * 0.5 + 0.5
        vert.co.z += math.sin(t * math.pi) * bend
        if vert.co.x > 0:
            vert.co.x += cut_bias * 0.5
        else:
            vert.co.x -= cut_bias
    return _finish_mesh(obj, bm)


def create_split_log(
    name: str,
    length: float = 1.1,
    radius: float = 0.16,
    flatness: float = 0.72,
    collection: bpy.types.Collection | None = None,
) -> bpy.types.Object:
    obj = create_branch_segment(name, length=length, radius_start=radius, radius_end=radius * 0.9, bend=0.05, collection=collection)
    for vert in obj.data.vertices:
        if vert.co.z > 0:
            vert.co.z *= flatness
        vert.co.y *= 0.9 + random.uniform(-0.05, 0.05)
    obj.data.update()
    return obj


def create_plank(
    name: str,
    length: float = 1.4,
    width: float = 0.35,
    thickness: float = 0.08,
    warp: float = 0.04,
    collection: bpy.types.Collection | None = None,
) -> bpy.types.Object:
    obj = _new_mesh_object(name, collection)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    scale = Matrix.Diagonal(Vector((length * 0.5, width * 0.5, thickness * 0.5, 1.0)))
    _apply_matrix_to_bmesh(bm, scale)
    for vert in bm.verts:
        side = 1.0 if vert.co.x > 0 else -1.0
        vert.co.z += side * warp
        vert.co.y += random.uniform(-0.025, 0.025)
    return _finish_mesh(obj, bm)


def create_rock_chunk(
    name: str,
    radius: float = 0.45,
    roughness: float = 0.18,
    collection: bpy.types.Collection | None = None,
) -> bpy.types.Object:
    obj = _new_mesh_object(name, collection)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=radius)
    for vert in bm.verts:
        direction = vert.co.normalized()
        vert.co += direction * random.uniform(-roughness, roughness)
        vert.co.z *= 0.85 + random.uniform(-0.05, 0.08)
    return _finish_mesh(obj, bm)


def create_rope_binding(
    name: str,
    turns: int = 4,
    radius: float = 0.08,
    width: float = 0.22,
    collection: bpy.types.Collection | None = None,
) -> bpy.types.Object:
    curve = bpy.data.curves.new(name, type="CURVE")
    curve.dimensions = "3D"
    spline = curve.splines.new("POLY")
    spline.points.add(turns * 3)
    points = []
    for i in range(turns * 4):
        angle = (i / 4.0) * math.pi * 0.5
        x = math.cos(angle) * radius
        y = math.sin(angle) * radius
        z = (i / max(turns * 4 - 1, 1) - 0.5) * width
        points.append((x, y, z, 1.0))
    for point, coords in zip(spline.points, points):
        point.co = coords
    curve.bevel_depth = 0.012
    curve.bevel_resolution = 2
    obj = bpy.data.objects.new(name, curve)
    return _link_object(obj, collection)


def create_fiber_bundle(
    name: str,
    strand_count: int = 14,
    length: float = 0.95,
    spread: float = 0.18,
    collection: bpy.types.Collection | None = None,
) -> bpy.types.Object:
    parts = []
    for index in range(strand_count):
        strand = create_branch_segment(
            f"{name}_strand_{index:02d}",
            length=length * random.uniform(0.75, 1.1),
            radius_start=0.018,
            radius_end=0.006,
            bend=random.uniform(0.02, 0.09),
            collection=collection,
        )
        strand.rotation_euler = Euler(
            (
                random.uniform(-0.35, 0.35),
                random.uniform(-0.25, 0.25),
                random.uniform(-0.55, 0.55),
            ),
            "XYZ",
        )
        strand.location = Vector(
            (
                random.uniform(-spread, spread),
                random.uniform(-spread * 0.4, spread * 0.4),
                random.uniform(-spread * 0.25, spread * 0.25),
            )
        )
        parts.append(strand)
    return join_objects(parts, name)


def create_bone_piece(
    name: str,
    length: float = 0.95,
    shaft_radius: float = 0.08,
    head_radius: float = 0.14,
    collection: bpy.types.Collection | None = None,
) -> bpy.types.Object:
    obj = _new_mesh_object(name, collection)
    bm = bmesh.new()
    bmesh.ops.create_cone(
        bm,
        cap_ends=True,
        cap_tris=False,
        segments=12,
        radius1=shaft_radius,
        radius2=shaft_radius * 0.9,
        depth=length,
    )
    _apply_matrix_to_bmesh(bm, Matrix.Rotation(math.radians(90.0), 4, "Y"))
    for sign in (-1, 1):
        geom = bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=6, radius=head_radius)
        matrix = Matrix.Translation(Vector((sign * length * 0.5, 0.0, 0.0)))
        for vert in geom["verts"]:
            vert.co = matrix @ vert.co
            vert.co.y *= 0.82
            vert.co.z *= 0.82
    return _finish_mesh(obj, bm)


def create_hide_sheet(
    name: str,
    width: float = 1.1,
    depth: float = 0.9,
    collection: bpy.types.Collection | None = None,
) -> bpy.types.Object:
    obj = _new_mesh_object(name, collection)
    bm = bmesh.new()
    grid = bmesh.ops.create_grid(bm, x_segments=8, y_segments=8, size=0.5)
    for vert in grid["verts"]:
        vert.co.x *= width
        vert.co.y *= depth
        edge_falloff = 1.0 - min(abs(vert.co.x) / max(width * 0.5, 0.001), abs(vert.co.y) / max(depth * 0.5, 0.001))
        vert.co.z = math.sin(vert.co.x * 4.0) * 0.03 + math.cos(vert.co.y * 3.2) * 0.025
        vert.co.z *= max(edge_falloff, 0.2)
    solid = obj.modifiers.new("Solidify", "SOLIDIFY")
    solid.thickness = 0.03
    _finish_mesh(obj, bm)
    return obj


def create_berry_cluster(
    name: str,
    count: int = 8,
    radius: float = 0.08,
    spread: float = 0.22,
    collection: bpy.types.Collection | None = None,
) -> bpy.types.Object:
    parts = []
    for index in range(count):
        obj = _new_mesh_object(f"{name}_berry_{index:02d}", collection)
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=8, radius=radius * random.uniform(0.85, 1.1))
        for vert in bm.verts:
            vert.co.x *= random.uniform(0.92, 1.08)
            vert.co.y *= random.uniform(0.92, 1.08)
        _finish_mesh(obj, bm)
        obj.location = Vector(
            (
                random.uniform(-spread, spread),
                random.uniform(-spread, spread),
                random.uniform(0.0, spread * 0.8),
            )
        )
        parts.append(obj)
    return join_objects(parts, name)


def create_leaf_cluster(
    name: str,
    count: int = 9,
    size: float = 0.24,
    collection: bpy.types.Collection | None = None,
) -> bpy.types.Object:
    parts = []
    for index in range(count):
        obj = create_plank(f"{name}_leaf_{index:02d}", length=size, width=size * 0.55, thickness=size * 0.06, warp=size * 0.06, collection=collection)
        obj.rotation_euler = Euler(
            (
                random.uniform(-0.6, 0.6),
                random.uniform(-0.6, 0.6),
                random.uniform(-math.pi, math.pi),
            ),
            "XYZ",
        )
        obj.location = Vector(
            (
                random.uniform(-size * 0.9, size * 0.9),
                random.uniform(-size * 0.9, size * 0.9),
                random.uniform(-size * 0.4, size * 0.7),
            )
        )
        parts.append(obj)
    return join_objects(parts, name)


def create_grass_clump(
    name: str,
    blade_count: int = 12,
    height: float = 0.75,
    collection: bpy.types.Collection | None = None,
) -> bpy.types.Object:
    parts = []
    for index in range(blade_count):
        blade = create_plank(
            f"{name}_blade_{index:02d}",
            length=height * random.uniform(0.55, 1.0),
            width=0.06,
            thickness=0.015,
            warp=0.03,
            collection=collection,
        )
        blade.rotation_euler = Euler(
            (
                random.uniform(-0.2, 0.35),
                random.uniform(-0.8, 0.8),
                random.uniform(-0.35, 0.35),
            ),
            "XYZ",
        )
        blade.location = Vector((random.uniform(-0.1, 0.1), random.uniform(-0.1, 0.1), 0.0))
        parts.append(blade)
    return join_objects(parts, name)


def create_flame_mesh(
    name: str,
    height: float = 0.85,
    width: float = 0.34,
    collection: bpy.types.Collection | None = None,
) -> bpy.types.Object:
    obj = _new_mesh_object(name, collection)
    bm = bmesh.new()
    verts = [
        (-width, 0.0, 0.0),
        (-width * 0.35, -width * 0.22, height * 0.28),
        (0.0, 0.0, height),
        (width * 0.45, width * 0.08, height * 0.44),
        (width, 0.0, 0.0),
        (0.0, width * 0.18, height * 0.18),
    ]
    bm_verts = [bm.verts.new(coords) for coords in verts]
    bm.faces.new((bm_verts[0], bm_verts[1], bm_verts[2], bm_verts[3], bm_verts[4], bm_verts[5]))
    solid = bmesh.ops.extrude_face_region(bm, geom=list(bm.faces))
    extruded_verts = [elem for elem in solid["geom"] if isinstance(elem, bmesh.types.BMVert)]
    for vert in extruded_verts:
        vert.co.y += 0.05
    return _finish_mesh(obj, bm)


def apply_bevel(obj: bpy.types.Object, width: float = 0.02, segments: int = 2) -> bpy.types.Object:
    modifier = obj.modifiers.get("BushcraftBevel") or obj.modifiers.new("BushcraftBevel", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    modifier.limit_method = "ANGLE"
    modifier.angle_limit = math.radians(35.0)
    modifier.harden_normals = True
    return obj


def apply_subtle_deform(obj: bpy.types.Object, strength: float = 0.06) -> bpy.types.Object:
    mesh = obj.data
    if not isinstance(mesh, bpy.types.Mesh):
        return obj
    for vert in mesh.vertices:
        noise = Vector(
            (
                math.sin(vert.co.x * 3.1 + vert.co.y) * strength * 0.12,
                math.cos(vert.co.y * 2.6 + vert.co.z) * strength * 0.12,
                math.sin(vert.co.z * 2.1 + vert.co.x) * strength * 0.18,
            )
        )
        vert.co += noise
    mesh.update()
    return obj


def apply_flat_or_soft_normals(obj: bpy.types.Object, mode: str = "soft") -> bpy.types.Object:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth() if mode == "soft" else bpy.ops.object.shade_flat()
    if mode == "soft" and isinstance(obj.data, bpy.types.Mesh):
        if hasattr(obj.data, "use_auto_smooth"):
            obj.data.use_auto_smooth = True
        if hasattr(obj.data, "auto_smooth_angle"):
            obj.data.auto_smooth_angle = math.radians(50.0)
    obj.select_set(False)
    return obj


def auto_uv_unwrap(obj: bpy.types.Object) -> bpy.types.Object:
    if not isinstance(obj.data, bpy.types.Mesh):
        return obj
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(70.0), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)
    return obj


def set_origin_to_base_center(obj: bpy.types.Object) -> bpy.types.Object:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    min_z = min((obj.matrix_world @ Vector(corner)).z for corner in obj.bound_box)
    cursor = bpy.context.scene.cursor
    previous = cursor.location.copy()
    cursor.location = Vector((obj.location.x, obj.location.y, min_z))
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")
    cursor.location = previous
    obj.select_set(False)
    return obj


def join_objects(objs: Sequence[bpy.types.Object], name: str) -> bpy.types.Object:
    objs = [obj for obj in objs if obj]
    if not objs:
        raise ValueError("join_objects() requires at least one object")
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objs:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = name
    if isinstance(joined.data, bpy.types.Mesh):
        joined.data.name = f"{name}_mesh"
    return joined


def randomize_transform(
    obj: bpy.types.Object,
    location_jitter: tuple[float, float, float] = (0.0, 0.0, 0.0),
    rotation_jitter: tuple[float, float, float] = (0.0, 0.0, 0.0),
    scale_jitter: tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> bpy.types.Object:
    obj.location += Vector(random.uniform(-v, v) for v in location_jitter)
    obj.rotation_euler = Euler(
        tuple(a + random.uniform(-j, j) for a, j in zip(obj.rotation_euler, rotation_jitter)),
        "XYZ",
    )
    obj.scale = Vector(
        tuple(max(0.001, s * (1.0 + random.uniform(-j, j))) for s, j in zip(obj.scale, scale_jitter))
    )
    return obj


def assign_material(obj: bpy.types.Object, material_name: str) -> bpy.types.Object:
    material = bpy.data.materials.get(material_name)
    if not material:
        raise ValueError(f"Material '{material_name}' does not exist")
    if hasattr(obj.data, "materials"):
        if obj.data.materials.get(material.name) is None:
            obj.data.materials.append(material)
        if isinstance(obj.data, bpy.types.Mesh) and len(obj.data.materials) == 1:
            for poly in obj.data.polygons:
                poly.material_index = 0
    return obj


def create_bushcraft_materials() -> dict[str, bpy.types.Material]:
    palette = {
        "wood_bark_painted": ((0.23, 0.15, 0.08, 1.0), 0.92),
        "wood_cut_painted": ((0.59, 0.43, 0.23, 1.0), 0.88),
        "rope_fiber_painted": ((0.70, 0.60, 0.35, 1.0), 0.92),
        "stone_painted": ((0.44, 0.45, 0.48, 1.0), 0.94),
        "leaf_green_painted": ((0.29, 0.43, 0.18, 1.0), 0.95),
        "leaf_dry_painted": ((0.50, 0.41, 0.21, 1.0), 0.96),
        "berry_red_painted": ((0.58, 0.08, 0.10, 1.0), 0.84),
        "berry_dark_painted": ((0.22, 0.12, 0.18, 1.0), 0.84),
        "meat_painted": ((0.52, 0.16, 0.14, 1.0), 0.88),
        "fat_painted": ((0.85, 0.74, 0.60, 1.0), 0.90),
        "hide_painted": ((0.45, 0.30, 0.16, 1.0), 0.92),
        "bone_painted": ((0.88, 0.84, 0.70, 1.0), 0.92),
        "fire_painted": ((0.97, 0.52, 0.14, 1.0), 0.72),
        "ash_painted": ((0.28, 0.27, 0.25, 1.0), 0.96),
        "dirt_painted": ((0.36, 0.27, 0.16, 1.0), 0.98),
    }
    materials = {}
    for name, (color, roughness) in palette.items():
        material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        material.use_nodes = True
        bsdf = next((node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
        if not bsdf:
            bsdf = material.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = 0.18
        materials[name] = material
    return materials


def ensure_export_directories() -> None:
    for folder in (
        ASSET_ROOT / "Items" / "Models",
        ASSET_ROOT / "Items" / "Textures",
        ASSET_ROOT / "Resources" / "Models",
        ASSET_ROOT / "Resources" / "Textures",
        ASSET_ROOT / "Placeables" / "Models",
        ASSET_ROOT / "Placeables" / "Textures",
        ASSET_ROOT / "Materials",
        ASSET_ROOT / "Source" / "Blend",
        REPO_ROOT / "Docs" / "art",
    ):
        folder.mkdir(parents=True, exist_ok=True)
