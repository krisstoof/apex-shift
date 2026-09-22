"""
Apex Shift Bushcraft Asset Generator v4 Master
==============================================

Purpose
-------
Generate a broad, style-consistent set of bushcraft-inspired 3D assets for
Apex Shift directly inside Blender. This generator is much more descriptive and
art-directed than the earlier prototype generators. It is designed around the
visual goal established in the conversation:

- hand-painted realism rather than low-poly abstraction,
- believable real-world bushcraft construction,
- readable silhouettes for isometric gameplay,
- reusable asset families with controlled variation,
- all major known Apex Shift prefab families: items, placeables, resources,
  vegetation variants, creatures, and landmarks.

It is still procedural, so it should be treated as a strong authored base for
further hand polish, UV/detail paint, rigging and gameplay integration.

Run
---
    blender --background --python bushcraft_asset_generator_v4_master.py

Optional configuration
----------------------
Adjust REPO_ROOT to export directly into the Unity repository. Leave it as
None to export beside this file.

Notes
-----
1. The script avoids debug-like primitives by layering many handcrafted forms.
2. Materials are painterly and earthy: bark, rough timber, weathered stone,
   canvas/hide, fiber rope, berries, ash, embers, etc.
3. Asset naming follows the established `*_stylized` naming convention.
4. The asset catalog covers the currently known gameplay-visible prefab set and
   additional variant prefabs useful for the runtime world builder.
"""

from __future__ import annotations

import json
import math
import os
import random
from dataclasses import dataclass, asdict
from typing import Callable, Dict, List, Optional, Sequence, Tuple

import bpy
from mathutils import Euler, Vector

# -----------------------------------------------------------------------------
# Global configuration
# -----------------------------------------------------------------------------

SEED = 1729
random.seed(SEED)

REPO_ROOT = None  # e.g. r"C:/Projects/apex-shift"
if REPO_ROOT:
    OUTPUT_ROOT = os.path.join(REPO_ROOT, "Assets", "_Project", "Art", "Bushcraft")
    DOCS_ROOT = os.path.join(REPO_ROOT, "Docs", "art")
else:
    OUTPUT_ROOT = os.path.abspath(
        os.path.join(os.path.dirname(__file__), "ApexShift_Bushcraft_Output_v4_Master")
    )
    DOCS_ROOT = os.path.join(OUTPUT_ROOT, "Docs")

SOURCE_BLEND_DIR = os.path.join(OUTPUT_ROOT, "Source", "Blend")
PREVIEW_DIR = os.path.join(OUTPUT_ROOT, "Previews")
ITEM_MODEL_DIR = os.path.join(OUTPUT_ROOT, "Items", "Models")
PLACEABLE_MODEL_DIR = os.path.join(OUTPUT_ROOT, "Placeables", "Models")
RESOURCE_MODEL_DIR = os.path.join(OUTPUT_ROOT, "Resources", "Models")
CREATURE_MODEL_DIR = os.path.join(OUTPUT_ROOT, "Creatures", "Models")
LANDMARK_MODEL_DIR = os.path.join(OUTPUT_ROOT, "Landmarks", "Models")
MANIFEST_PATH = os.path.join(OUTPUT_ROOT, "bushcraft_v4_manifest.json")
CATALOG_PATH = os.path.join(OUTPUT_ROOT, "bushcraft_v4_catalog.md")

EXPORT_FBX = True
EXPORT_OBJ = True
SAVE_BLEND = True
RENDER_PREVIEWS = True
GENERATE_ALL_ON_RUN = True

# Useful for partial generation during iteration.
ONLY_ASSETS: List[str] = []

MATS: Dict[str, bpy.types.Material] = {}


# -----------------------------------------------------------------------------
# Data structures
# -----------------------------------------------------------------------------

@dataclass
class AssetRecord:
    asset_id: str
    category: str
    blend_path: str
    fbx_path: str
    obj_path: str
    preview_path: str
    source_object_count: int
    joined_material_count: int
    bounds: Tuple[float, float, float]
    notes: str


@dataclass
class AssetSpec:
    asset_id: str
    category: str
    notes: str
    generator: Callable[[], List[bpy.types.Object]]
    min_bounds: Tuple[float, float, float] = (0.0, 0.0, 0.0)


# -----------------------------------------------------------------------------
# File system helpers
# -----------------------------------------------------------------------------


def ensure_dirs() -> None:
    for path in [
        OUTPUT_ROOT,
        DOCS_ROOT,
        SOURCE_BLEND_DIR,
        PREVIEW_DIR,
        ITEM_MODEL_DIR,
        PLACEABLE_MODEL_DIR,
        RESOURCE_MODEL_DIR,
        CREATURE_MODEL_DIR,
        LANDMARK_MODEL_DIR,
    ]:
        os.makedirs(path, exist_ok=True)


# -----------------------------------------------------------------------------
# Scene helpers
# -----------------------------------------------------------------------------


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.users == 0:
            bpy.data.collections.remove(collection)
    for material in list(bpy.data.materials):
        if material.users == 0:
            bpy.data.materials.remove(material)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)



def look_at(obj: bpy.types.Object, target: Vector) -> None:
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()



def setup_scene() -> None:
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 96
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1200
    scene.view_settings.view_transform = "Filmic"
    scene.view_settings.look = "Medium High Contrast"
    scene.render.film_transparent = False
    scene.world.color = (0.80, 0.73, 0.62)

    bpy.ops.object.camera_add(location=(7.8, -8.6, 6.4))
    cam = bpy.context.object
    cam.name = "preview_camera"
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 6.4
    scene.camera = cam
    look_at(cam, Vector((0.0, 0.0, 1.4)))

    bpy.ops.object.light_add(type="AREA", location=(-4.8, -5.8, 8.6))
    key = bpy.context.object
    key.name = "key_area"
    key.data.energy = 640
    key.data.size = 7.0

    bpy.ops.object.light_add(type="AREA", location=(5.5, 4.2, 5.5))
    fill = bpy.context.object
    fill.name = "fill_area"
    fill.data.energy = 120
    fill.data.size = 7.5

    bpy.ops.object.light_add(type="SUN", location=(0.0, 0.0, 10.0))
    sun = bpy.context.object
    sun.name = "sun_rim"
    sun.data.energy = 0.8
    sun.rotation_euler = Euler((math.radians(42), math.radians(0), math.radians(34)), "XYZ")

    bpy.ops.mesh.primitive_plane_add(size=12, location=(0, 0, -0.03))
    ground = bpy.context.object
    ground.name = "preview_ground_DO_NOT_EXPORT"
    assign_material(ground, make_material("preview_ground", (0.71, 0.63, 0.49, 1.0), noise=True, bump=True, roughness=0.92))


# -----------------------------------------------------------------------------
# Materials
# -----------------------------------------------------------------------------


def make_material(name: str, base: Tuple[float, float, float, float], *, noise: bool = True, bump: bool = True, roughness: float = 0.85, sheen: float = 0.0) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        return mat

    bsdf.inputs["Base Color"].default_value = base
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Sheen Weight"].default_value = sheen
    bsdf.inputs["Specular IOR Level"].default_value = 0.32

    if noise:
        noise_node = nodes.new(type="ShaderNodeTexNoise")
        noise_node.inputs["Scale"].default_value = 14.0
        noise_node.inputs["Detail"].default_value = 8.0
        noise_node.inputs["Roughness"].default_value = 0.6
        ramp = nodes.new(type="ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.28
        ramp.color_ramp.elements[1].position = 0.88
        dark = (base[0] * 0.72, base[1] * 0.72, base[2] * 0.72, base[3])
        bright = (min(base[0] * 1.18, 1.0), min(base[1] * 1.18, 1.0), min(base[2] * 1.18, 1.0), base[3])
        ramp.color_ramp.elements[0].color = dark
        ramp.color_ramp.elements[1].color = bright
        links.new(noise_node.outputs["Fac"], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])

    if bump:
        bump_noise = nodes.new(type="ShaderNodeTexNoise")
        bump_noise.inputs["Scale"].default_value = 36.0
        bump_noise.inputs["Detail"].default_value = 9.0
        bump_node = nodes.new(type="ShaderNodeBump")
        bump_node.inputs["Strength"].default_value = 0.05
        bump_node.inputs["Distance"].default_value = 0.05
        links.new(bump_noise.outputs["Fac"], bump_node.inputs["Height"])
        links.new(bump_node.outputs["Normal"], bsdf.inputs["Normal"])

    return mat



def create_material_library() -> None:
    global MATS
    MATS = {
        # wood and bark
        "bark_dark": make_material("bark_dark", (0.24, 0.13, 0.06, 1.0)),
        "bark_mid": make_material("bark_mid", (0.33, 0.18, 0.08, 1.0)),
        "wood_warm": make_material("wood_warm", (0.52, 0.31, 0.14, 1.0)),
        "wood_light": make_material("wood_light", (0.78, 0.59, 0.33, 1.0)),
        "wood_dark": make_material("wood_dark", (0.16, 0.09, 0.05, 1.0)),
        "charred": make_material("charred", (0.09, 0.07, 0.06, 1.0), roughness=0.95),
        # vegetation
        "leaf_light": make_material("leaf_light", (0.58, 0.67, 0.30, 1.0)),
        "leaf_mid": make_material("leaf_mid", (0.38, 0.48, 0.20, 1.0)),
        "leaf_dark": make_material("leaf_dark", (0.20, 0.29, 0.13, 1.0)),
        "needle": make_material("needle", (0.24, 0.35, 0.16, 1.0)),
        "dry_leaf": make_material("dry_leaf", (0.58, 0.44, 0.19, 1.0)),
        "grass": make_material("grass", (0.50, 0.57, 0.24, 1.0)),
        "flower_white": make_material("flower_white", (0.93, 0.89, 0.78, 1.0), noise=False, bump=False, roughness=0.62),
        "flower_yellow": make_material("flower_yellow", (0.94, 0.74, 0.15, 1.0), noise=False, bump=False, roughness=0.60),
        "flower_purple": make_material("flower_purple", (0.49, 0.40, 0.72, 1.0), noise=False, bump=False, roughness=0.60),
        "berry_red": make_material("berry_red", (0.63, 0.10, 0.07, 1.0), noise=False, bump=False, roughness=0.48),
        "berry_dark": make_material("berry_dark", (0.16, 0.10, 0.28, 1.0), noise=False, bump=False, roughness=0.48),
        # stone and soil
        "stone": make_material("stone", (0.47, 0.46, 0.42, 1.0)),
        "stone_dark": make_material("stone_dark", (0.31, 0.31, 0.28, 1.0)),
        "moss": make_material("moss", (0.32, 0.40, 0.20, 1.0)),
        "dirt": make_material("dirt", (0.39, 0.29, 0.17, 1.0)),
        "ash": make_material("ash", (0.12, 0.11, 0.10, 1.0)),
        "water": make_material("water", (0.16, 0.37, 0.53, 1.0), noise=False, bump=False, roughness=0.18),
        # fibers / fabrics / organic
        "rope": make_material("rope", (0.70, 0.58, 0.31, 1.0), roughness=0.92, sheen=0.14),
        "canvas": make_material("canvas", (0.60, 0.47, 0.24, 1.0), roughness=0.96, sheen=0.18),
        "canvas_dark": make_material("canvas_dark", (0.42, 0.31, 0.17, 1.0), roughness=0.96, sheen=0.16),
        "hide": make_material("hide", (0.50, 0.30, 0.14, 1.0), roughness=0.88, sheen=0.12),
        "bone": make_material("bone", (0.84, 0.74, 0.57, 1.0), roughness=0.72),
        "meat": make_material("meat", (0.61, 0.17, 0.13, 1.0), roughness=0.68),
        "fat": make_material("fat", (0.90, 0.78, 0.63, 1.0), roughness=0.56),
        # fire
        "flame_yellow": make_material("flame_yellow", (1.0, 0.80, 0.14, 1.0), noise=False, bump=False, roughness=0.28),
        "flame_orange": make_material("flame_orange", (1.0, 0.34, 0.06, 1.0), noise=False, bump=False, roughness=0.28),
        "ember": make_material("ember", (0.88, 0.19, 0.03, 1.0), noise=False, bump=False, roughness=0.25),
        # fur/creatures
        "fur_brown": make_material("fur_brown", (0.40, 0.24, 0.13, 1.0), roughness=0.90, sheen=0.10),
        "fur_dark": make_material("fur_dark", (0.18, 0.14, 0.11, 1.0), roughness=0.92, sheen=0.10),
        "fur_tan": make_material("fur_tan", (0.63, 0.51, 0.33, 1.0), roughness=0.90, sheen=0.10),
        "eye_dark": make_material("eye_dark", (0.04, 0.03, 0.03, 1.0), noise=False, bump=False, roughness=0.16),
    }



def mat(name: str) -> bpy.types.Material:
    return MATS[name]


# -----------------------------------------------------------------------------
# Mesh utilities
# -----------------------------------------------------------------------------


def assign_material(obj: bpy.types.Object, material: bpy.types.Material) -> bpy.types.Object:
    if obj.data.materials:
        obj.data.materials[0] = material
    else:
        obj.data.materials.append(material)
    return obj



def smooth(obj: bpy.types.Object, enabled: bool = True) -> bpy.types.Object:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    if enabled:
        try:
            bpy.ops.object.shade_smooth()
        except Exception:
            pass
    obj.select_set(False)
    return obj



def apply_modifier(obj: bpy.types.Object, modifier_name: str) -> None:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    try:
        bpy.ops.object.modifier_apply(modifier=modifier_name)
    except Exception:
        pass
    obj.select_set(False)



def add_subsurf(obj: bpy.types.Object, levels: int = 1, render_levels: int = 1) -> bpy.types.Object:
    mod = obj.modifiers.new(name="Subsurf", type="SUBSURF")
    mod.levels = levels
    mod.render_levels = render_levels
    return obj



def add_bevel(obj: bpy.types.Object, width: float = 0.02, segments: int = 2) -> bpy.types.Object:
    mod = obj.modifiers.new(name="Bevel", type="BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = 'ANGLE'
    return obj



def add_displace(obj: bpy.types.Object, strength: float = 0.02, scale: float = 5.0) -> bpy.types.Object:
    tex = bpy.data.textures.new(name=f"{obj.name}_noise", type='CLOUDS')
    tex.noise_scale = scale
    mod = obj.modifiers.new(name="Displace", type='DISPLACE')
    mod.texture = tex
    mod.strength = strength
    return obj



def rename(obj: bpy.types.Object, name: str) -> bpy.types.Object:
    obj.name = name
    return obj



def primitive_cube(name: str, size=(1.0, 1.0, 1.0), location=(0, 0, 0), rotation=(0, 0, 0), material: Optional[bpy.types.Material] = None) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (size[0] * 0.5, size[1] * 0.5, size[2] * 0.5)
    if material:
        assign_material(obj, material)
    smooth(obj, False)
    return obj



def primitive_cylinder(name: str, radius=0.5, depth=1.0, location=(0, 0, 0), rotation=(0, 0, 0), vertices: int = 12, material: Optional[bpy.types.Material] = None) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    if material:
        assign_material(obj, material)
    smooth(obj, True)
    return obj



def primitive_uv_sphere(name: str, radius=0.5, location=(0, 0, 0), segments: int = 16, rings: int = 8, material: Optional[bpy.types.Material] = None) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius, location=location)
    obj = bpy.context.object
    obj.name = name
    if material:
        assign_material(obj, material)
    smooth(obj, True)
    return obj



def primitive_ico_sphere(name: str, radius=0.5, location=(0, 0, 0), subdivisions: int = 2, material: Optional[bpy.types.Material] = None) -> bpy.types.Object:
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=radius, location=location)
    obj = bpy.context.object
    obj.name = name
    if material:
        assign_material(obj, material)
    smooth(obj, True)
    return obj



def primitive_plane(name: str, size=1.0, location=(0, 0, 0), rotation=(0, 0, 0), material: Optional[bpy.types.Material] = None) -> bpy.types.Object:
    bpy.ops.mesh.primitive_plane_add(size=size, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    if material:
        assign_material(obj, material)
    return obj



def tapered_cylinder(name: str, radius_bottom: float, radius_top: float, depth: float, location=(0, 0, 0), rotation=(0, 0, 0), material: Optional[bpy.types.Material] = None, vertices: int = 12) -> bpy.types.Object:
    obj = primitive_cylinder(name, radius=1.0, depth=depth, location=location, rotation=rotation, vertices=vertices, material=material)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='DESELECT')
    bpy.ops.mesh.select_mode(type='VERT')
    bpy.ops.mesh.select_non_manifold()
    bpy.ops.object.mode_set(mode='OBJECT')
    # Simpler and more predictable: scale object as a whole first, then lattice-like taper with mesh transform.
    obj.scale = (radius_bottom, radius_bottom, 1.0)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_mode(type='VERT')
    bpy.ops.mesh.select_all(action='DESELECT')
    bpy.ops.object.mode_set(mode='OBJECT')
    top_z = max(v.co.z for v in obj.data.vertices)
    for v in obj.data.vertices:
        if abs(v.co.z - top_z) < 1e-4:
            v.select = True
    bpy.ops.object.mode_set(mode='EDIT')
    if radius_bottom > 0:
        bpy.ops.transform.resize(value=(radius_top / radius_bottom, radius_top / radius_bottom, 1.0))
    bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)
    return obj



def join_objects(name: str, objects: Sequence[bpy.types.Object]) -> bpy.types.Object:
    meshes = [o for o in objects if o and o.type == 'MESH']
    if not meshes:
        raise ValueError(f"No mesh objects to join for {name}")
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    return obj



def set_origin_to_bottom(obj: bpy.types.Object) -> None:
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    x = sum(c.x for c in corners) / 8.0
    y = sum(c.y for c in corners) / 8.0
    z = min(c.z for c in corners)
    bpy.context.scene.cursor.location = (x, y, z)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR', center='MEDIAN')
    obj.select_set(False)



def object_bounds(obj: bpy.types.Object) -> Tuple[float, float, float]:
    corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return (
        max(c.x for c in corners) - min(c.x for c in corners),
        max(c.y for c in corners) - min(c.y for c in corners),
        max(c.z for c in corners) - min(c.z for c in corners),
    )


# -----------------------------------------------------------------------------
# Shape language / reusable modeling helpers
# -----------------------------------------------------------------------------


def irregular_rock(name: str, radius: float, location=(0, 0, 0), scale=(1.0, 1.0, 1.0), material_name: str = "stone") -> bpy.types.Object:
    rock = primitive_ico_sphere(name, radius=radius, location=location, subdivisions=2, material=mat(material_name))
    rock.scale = scale
    add_displace(rock, strength=radius * 0.18, scale=1.8)
    add_bevel(rock, width=radius * 0.06, segments=2)
    apply_modifier(rock, "Displace")
    try:
        apply_modifier(rock, "Bevel")
    except Exception:
        pass
    smooth(rock, True)
    return rock



def chopped_log(name: str, length: float, radius: float, location=(0, 0, 0), rotation=(0, 0, 0), material_name: str = "wood_warm") -> bpy.types.Object:
    log = tapered_cylinder(name, radius_bottom=radius, radius_top=radius * random.uniform(0.92, 1.05), depth=length, location=location, rotation=rotation, material=mat(material_name), vertices=14)
    add_displace(log, strength=radius * 0.08, scale=3.0)
    apply_modifier(log, "Displace")
    smooth(log, True)
    return log



def stake(name: str, height: float, radius: float, location=(0, 0, 0), rotation=(0, 0, 0), material_name: str = "wood_dark") -> bpy.types.Object:
    obj = primitive_cylinder(name, radius=radius, depth=height, location=location, rotation=rotation, vertices=10, material=mat(material_name))
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='DESELECT')
    bpy.ops.object.mode_set(mode='OBJECT')
    top_z = max(v.co.z for v in obj.data.vertices)
    for v in obj.data.vertices:
        if abs(v.co.z - top_z) < 1e-4:
            v.select = True
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.transform.resize(value=(0.12, 0.12, 1.0))
    bpy.ops.transform.translate(value=(0, 0, radius * 0.6))
    bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)
    add_displace(obj, strength=radius * 0.05, scale=3.0)
    apply_modifier(obj, "Displace")
    smooth(obj, True)
    return obj



def board(name: str, length: float, width: float, thickness: float, location=(0, 0, 0), rotation=(0, 0, 0), material_name: str = "wood_warm") -> bpy.types.Object:
    obj = primitive_cube(name, size=(length, width, thickness), location=location, rotation=rotation, material=mat(material_name))
    add_bevel(obj, width=min(width, thickness) * 0.18, segments=2)
    apply_modifier(obj, "Bevel")
    smooth(obj, False)
    return obj



def rope_bundle(name: str, segments: int = 5, length: float = 0.8, location=(0, 0, 0)) -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    for i in range(segments):
        x = (i - (segments - 1) / 2) * 0.05
        y = random.uniform(-0.02, 0.02)
        z = random.uniform(-0.01, 0.01)
        piece = primitive_cylinder(f"{name}_fiber_{i}", radius=0.018, depth=length * random.uniform(0.85, 1.05), location=(location[0] + x, location[1] + y, location[2] + z), rotation=(math.radians(90), 0, random.uniform(-0.35, 0.35)), vertices=8, material=mat("rope"))
        objs.append(piece)
    tie = primitive_cylinder(f"{name}_tie", radius=0.028, depth=0.14, location=(location[0], location[1], location[2]), rotation=(math.radians(90), 0, 0), vertices=8, material=mat("rope"))
    objs.append(tie)
    return objs



def leaf_cluster(name: str, center=(0, 0, 0), radius: float = 0.8, count: int = 18, palette: Sequence[str] = ("leaf_light", "leaf_mid", "leaf_dark"), stretch=(1.0, 1.0, 1.0)) -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    for i in range(count):
        a = random.uniform(0, math.tau)
        r = random.uniform(0.0, radius)
        z = random.uniform(-radius * 0.25, radius * 0.45)
        puff = primitive_uv_sphere(
            f"{name}_{i}",
            radius=random.uniform(radius * 0.14, radius * 0.28),
            location=(center[0] + math.cos(a) * r * stretch[0], center[1] + math.sin(a) * r * stretch[1], center[2] + z * stretch[2]),
            segments=12,
            rings=6,
            material=mat(random.choice(tuple(palette))),
        )
        puff.scale = (
            random.uniform(0.8, 1.25),
            random.uniform(0.8, 1.25),
            random.uniform(0.8, 1.25),
        )
        objs.append(puff)
    return objs



def grass_tuft(name: str, location=(0, 0, 0), blade_count: int = 20, height: float = 0.45, radius: float = 0.20, add_flowers: bool = False) -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    for i in range(blade_count):
        angle = random.uniform(0, math.tau)
        dist = random.uniform(0.0, radius)
        blade = primitive_plane(
            f"{name}_blade_{i}",
            size=random.uniform(0.06, 0.09),
            location=(location[0] + math.cos(angle) * dist, location[1] + math.sin(angle) * dist, location[2] + height * 0.5),
            rotation=(math.radians(random.uniform(68, 88)), 0, angle),
            material=mat("grass"),
        )
        blade.scale = (0.20, 1.0, 1.0)
        objs.append(blade)
    if add_flowers:
        for i in range(random.randint(4, 8)):
            angle = random.uniform(0, math.tau)
            dist = random.uniform(0.0, radius * 0.8)
            stem = primitive_cylinder(
                f"{name}_stem_{i}",
                radius=0.008,
                depth=height * random.uniform(0.45, 0.7),
                location=(location[0] + math.cos(angle) * dist, location[1] + math.sin(angle) * dist, location[2] + height * 0.28),
                vertices=6,
                material=mat("grass"),
            )
            objs.append(stem)
            flower = primitive_uv_sphere(
                f"{name}_flower_{i}",
                radius=0.03,
                location=(stem.location.x, stem.location.y, stem.location.z + stem.dimensions.z * 0.52),
                segments=10,
                rings=5,
                material=mat(random.choice(["flower_white", "flower_yellow", "flower_purple"])),
            )
            objs.append(flower)
    return objs



def cloth_panel(name: str, size=(1.6, 1.2), location=(0, 0, 0), rotation=(0, 0, 0), material_name: str = "canvas", bend: float = 0.08) -> bpy.types.Object:
    panel = primitive_plane(name, size=1.0, location=location, rotation=rotation, material=mat(material_name))
    panel.scale = (size[0] * 0.5, size[1] * 0.5, 1.0)
    bpy.context.view_layer.objects.active = panel
    panel.select_set(True)
    try:
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.subdivide(number_cuts=8)
        bpy.ops.object.mode_set(mode='OBJECT')
    finally:
        panel.select_set(False)
    add_displace(panel, strength=bend, scale=2.2)
    apply_modifier(panel, "Displace")
    smooth(panel, True)
    return panel



def antler_branch(name: str, base=(0, 0, 0), height: float = 1.8, lean=(0.0, 0.0), branches: int = 4, dry: bool = False) -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    trunk = tapered_cylinder(name + "_trunk", 0.14, 0.05, height, location=(base[0] + lean[0], base[1] + lean[1], base[2] + height * 0.5), material=mat("bark_mid" if not dry else "wood_dark"), vertices=10)
    objs.append(trunk)
    for i in range(branches):
        angle = random.uniform(0, math.tau)
        elev = random.uniform(height * 0.45, height * 0.92)
        length = random.uniform(0.35, 0.85)
        branch = tapered_cylinder(
            f"{name}_branch_{i}",
            0.035,
            0.012,
            length,
            location=(base[0] + math.cos(angle) * length * 0.15, base[1] + math.sin(angle) * length * 0.15, base[2] + elev),
            rotation=(math.radians(65), 0, angle),
            material=mat("bark_mid" if not dry else "wood_dark"),
            vertices=8,
        )
        objs.append(branch)
    return objs


# -----------------------------------------------------------------------------
# Items
# -----------------------------------------------------------------------------


def gen_wood() -> List[bpy.types.Object]:
    objs = []
    for i in range(4):
        angle = random.uniform(-0.35, 0.35)
        objs.append(chopped_log(f"wood_log_{i}", length=random.uniform(0.8, 1.15), radius=random.uniform(0.08, 0.13), location=(random.uniform(-0.08, 0.08), random.uniform(-0.12, 0.12), 0.10 + i * 0.02), rotation=(math.radians(90), 0, angle), material_name="wood_warm"))
    objs += rope_bundle("wood_rope", segments=4, length=0.42, location=(0, 0, 0.18))
    return objs



def gen_stone() -> List[bpy.types.Object]:
    return [
        irregular_rock("stone_pickup_a", 0.18, location=(-0.12, 0.04, 0.10), scale=(1.0, 0.9, 0.8)),
        irregular_rock("stone_pickup_b", 0.15, location=(0.09, -0.08, 0.08), scale=(0.8, 1.1, 0.9)),
        irregular_rock("stone_pickup_c", 0.12, location=(0.05, 0.12, 0.07), scale=(1.0, 0.8, 0.7)),
    ]



def gen_fiber() -> List[bpy.types.Object]:
    objs = rope_bundle("fiber_bundle", segments=9, length=0.95, location=(0, 0, 0.16))
    extra = primitive_plane("fiber_spread", size=0.75, location=(0, 0, 0.05), rotation=(math.radians(90), 0, random.uniform(0, math.tau)), material=mat("rope"))
    extra.scale = (0.45, 0.12, 1.0)
    objs.append(extra)
    return objs



def gen_grass() -> List[bpy.types.Object]:
    return grass_tuft("grass_pickup", height=0.40, blade_count=16, radius=0.18, add_flowers=False)



def gen_meat() -> List[bpy.types.Object]:
    slab = primitive_uv_sphere("meat_slab", radius=0.32, location=(0, 0, 0.18), segments=18, rings=10, material=mat("meat"))
    slab.scale = (1.15, 0.72, 0.55)
    fat = primitive_uv_sphere("meat_fat", radius=0.16, location=(0.04, 0.02, 0.27), segments=12, rings=8, material=mat("fat"))
    fat.scale = (1.3, 0.65, 0.32)
    smooth(slab, True)
    smooth(fat, True)
    return [slab, fat]



def gen_hide() -> List[bpy.types.Object]:
    hide = cloth_panel("hide_skin", size=(1.05, 0.85), location=(0, 0, 0.04), rotation=(0, 0, math.radians(18)), material_name="hide", bend=0.06)
    hide.scale = (1.0, 0.85, 1.0)
    return [hide]



def gen_bone() -> List[bpy.types.Object]:
    shaft = primitive_cylinder("bone_shaft", radius=0.055, depth=0.82, location=(0, 0, 0.16), rotation=(math.radians(90), math.radians(8), math.radians(24)), vertices=10, material=mat("bone"))
    knob_a = primitive_uv_sphere("bone_knob_a", radius=0.10, location=(-0.38, 0.0, 0.16), segments=12, rings=8, material=mat("bone"))
    knob_b = primitive_uv_sphere("bone_knob_b", radius=0.10, location=(0.38, 0.0, 0.16), segments=12, rings=8, material=mat("bone"))
    return [shaft, knob_a, knob_b]



def gen_berries() -> List[bpy.types.Object]:
    objs = []
    for i in range(9):
        a = i / 9 * math.tau
        r = random.uniform(0.05, 0.16)
        objs.append(primitive_uv_sphere(f"berries_{i}", radius=random.uniform(0.05, 0.07), location=(math.cos(a) * r, math.sin(a) * r, random.uniform(0.07, 0.14)), segments=10, rings=6, material=mat(random.choice(["berry_red", "berry_dark"]))))
    leaf = primitive_plane("berries_leaf", size=0.35, location=(0.12, 0.02, 0.06), rotation=(math.radians(70), 0, math.radians(20)), material=mat("leaf_mid"))
    objs.append(leaf)
    return objs



def gen_torch() -> List[bpy.types.Object]:
    handle = chopped_log("torch_handle", length=1.35, radius=0.06, location=(0, 0, 0.65), rotation=(0, 0, 0), material_name="wood_dark")
    head = primitive_cylinder("torch_head_wrap", radius=0.10, depth=0.28, location=(0, 0, 1.28), vertices=10, material=mat("canvas_dark"))
    ember = primitive_uv_sphere("torch_ember", radius=0.08, location=(0, 0, 1.42), segments=12, rings=8, material=mat("ember"))
    flame_a = primitive_uv_sphere("torch_flame_a", radius=0.10, location=(0, 0.0, 1.57), segments=12, rings=8, material=mat("flame_orange"))
    flame_a.scale = (0.55, 0.55, 1.55)
    flame_b = primitive_uv_sphere("torch_flame_b", radius=0.07, location=(0.02, -0.01, 1.63), segments=10, rings=6, material=mat("flame_yellow"))
    flame_b.scale = (0.42, 0.42, 1.25)
    return [handle, head, ember, flame_a, flame_b]



def gen_spear() -> List[bpy.types.Object]:
    shaft = tapered_cylinder("spear_shaft", 0.04, 0.026, 2.15, location=(0, 0, 1.05), material=mat("wood_warm"), vertices=12)
    tip = primitive_ico_sphere("spear_tip_stone", radius=0.12, location=(0, 0, 2.18), subdivisions=2, material=mat("stone"))
    tip.scale = (0.32, 0.16, 0.45)
    tip.rotation_euler = Euler((0, math.radians(90), 0), 'XYZ')
    lash_a = primitive_cylinder("spear_lash_a", radius=0.045, depth=0.10, location=(0, 0, 2.02), rotation=(0, math.radians(90), 0), vertices=8, material=mat("rope"))
    lash_b = primitive_cylinder("spear_lash_b", radius=0.048, depth=0.10, location=(0, 0, 2.08), rotation=(0, math.radians(90), 0), vertices=8, material=mat("rope"))
    return [shaft, tip, lash_a, lash_b]



def gen_bow() -> List[bpy.types.Object]:
    upper = primitive_cylinder("bow_limb_upper", radius=0.03, depth=1.05, location=(0, 0.04, 1.12), rotation=(math.radians(90), 0, math.radians(12)), vertices=10, material=mat("wood_warm"))
    upper.scale = (1.0, 1.0, 1.0)
    lower = primitive_cylinder("bow_limb_lower", radius=0.03, depth=1.05, location=(0, -0.04, 0.98), rotation=(math.radians(90), 0, math.radians(-12)), vertices=10, material=mat("wood_warm"))
    grip = primitive_cylinder("bow_grip", radius=0.04, depth=0.25, location=(0, 0, 1.05), rotation=(math.radians(90), 0, 0), vertices=10, material=mat("wood_dark"))
    string = primitive_plane("bow_string", size=1.8, location=(0.0, -0.02, 1.05), rotation=(math.radians(90), 0, math.radians(90)), material=mat("rope"))
    string.scale = (0.01, 0.55, 1.0)
    return [upper, lower, grip, string]



def gen_axe() -> List[bpy.types.Object]:
    haft = tapered_cylinder("axe_haft", 0.045, 0.03, 1.05, location=(0, 0, 0.55), material=mat("wood_warm"), vertices=12)
    head = primitive_cube("axe_head", size=(0.22, 0.10, 0.16), location=(0, 0, 1.05), material=mat("stone"))
    blade = primitive_cube("axe_blade", size=(0.10, 0.05, 0.18), location=(0.12, 0, 1.05), rotation=(0, 0, math.radians(14)), material=mat("stone_light") if "stone_light" in MATS else mat("stone"))
    lash = primitive_cylinder("axe_lash", radius=0.05, depth=0.12, location=(0, 0, 0.98), rotation=(math.radians(90), 0, 0), vertices=8, material=mat("rope"))
    return [haft, head, blade, lash]



def gen_pickaxe() -> List[bpy.types.Object]:
    haft = tapered_cylinder("pickaxe_haft", 0.05, 0.032, 1.15, location=(0, 0, 0.58), material=mat("wood_warm"), vertices=12)
    core = primitive_cube("pickaxe_head_core", size=(0.12, 0.10, 0.12), location=(0, 0, 1.08), material=mat("stone_dark"))
    spike_a = primitive_cube("pickaxe_spike_a", size=(0.34, 0.05, 0.08), location=(0.18, 0, 1.08), rotation=(0, 0, math.radians(10)), material=mat("stone"))
    spike_b = primitive_cube("pickaxe_spike_b", size=(0.34, 0.05, 0.08), location=(-0.18, 0, 1.08), rotation=(0, 0, math.radians(-10)), material=mat("stone"))
    lash = primitive_cylinder("pickaxe_lash", radius=0.05, depth=0.12, location=(0, 0, 1.00), rotation=(math.radians(90), 0, 0), vertices=8, material=mat("rope"))
    return [haft, core, spike_a, spike_b, lash]


# -----------------------------------------------------------------------------
# Placeables
# -----------------------------------------------------------------------------


def gen_campfire() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    for i in range(12):
        a = i / 12 * math.tau + random.uniform(-0.12, 0.12)
        rock = irregular_rock(f"campfire_rock_{i}", radius=random.uniform(0.09, 0.14), location=(math.cos(a) * 0.45, math.sin(a) * 0.45, 0.06), scale=(random.uniform(0.8, 1.2), random.uniform(0.7, 1.2), random.uniform(0.7, 1.0)))
        objs.append(rock)
    objs.append(primitive_cylinder("campfire_ash_bed", radius=0.24, depth=0.06, location=(0, 0, 0.03), vertices=18, material=mat("ash")))
    for i, ang in enumerate([0.0, math.radians(60), math.radians(120)]):
        log = chopped_log(f"campfire_log_{i}", length=0.75, radius=0.07, location=(0, 0, 0.10 + i * 0.02), rotation=(math.radians(90), 0, ang), material_name="charred")
        objs.append(log)
    ember = primitive_uv_sphere("campfire_ember", radius=0.12, location=(0.01, -0.02, 0.16), segments=12, rings=6, material=mat("ember"))
    ember.scale = (1.35, 1.0, 0.55)
    flame_a = primitive_uv_sphere("campfire_flame_orange", radius=0.16, location=(0.0, 0.0, 0.35), segments=12, rings=6, material=mat("flame_orange"))
    flame_a.scale = (0.65, 0.65, 1.8)
    flame_b = primitive_uv_sphere("campfire_flame_yellow", radius=0.12, location=(0.02, 0.0, 0.46), segments=10, rings=5, material=mat("flame_yellow"))
    flame_b.scale = (0.48, 0.48, 1.4)
    objs.extend([ember, flame_a, flame_b])
    return objs



def gen_storage_box() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    base_length, base_width, base_height = 1.18, 0.72, 0.62
    plank_t = 0.045
    # base frame
    objs.append(board("box_floor", base_length, base_width, plank_t, location=(0, 0, plank_t * 0.5), material_name="wood_dark"))
    side_z = base_height * 0.5
    for side, y in [("front", -base_width * 0.5 + plank_t * 0.5), ("back", base_width * 0.5 - plank_t * 0.5)]:
        objs.append(board(f"box_{side}", base_length, plank_t, base_height, location=(0, y, side_z), material_name="wood_warm"))
    for side, x in [("left", -base_length * 0.5 + plank_t * 0.5), ("right", base_length * 0.5 - plank_t * 0.5)]:
        objs.append(board(f"box_{side}", plank_t, base_width - plank_t * 2, base_height, location=(x, 0, side_z), material_name="wood_warm"))
    # lid
    objs.append(board("box_lid", base_length + 0.04, base_width + 0.04, plank_t, location=(0, 0, base_height + plank_t * 0.5), rotation=(math.radians(4), 0, 0), material_name="wood_light"))
    # braces
    for x in (-0.38, 0.38):
        objs.append(board(f"box_brace_{'L' if x<0 else 'R'}", 0.08, base_width + 0.05, 0.12, location=(x, 0, base_height + 0.02), material_name="wood_dark"))
    objs += rope_bundle("box_rope", segments=2, length=0.22, location=(0, -0.38, 0.34))
    return objs



def gen_tent() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    # central ridge and frame
    ridge_height = 1.65
    ridge_length = 2.55
    objs.append(chopped_log("tent_ridge", length=ridge_length, radius=0.055, location=(0, 0, ridge_height), rotation=(math.radians(90), 0, math.radians(90)), material_name="wood_dark"))
    for i, x in enumerate((-0.95, 0.95)):
        objs.append(stake(f"tent_pole_{i}", height=1.82, radius=0.055, location=(x, 0, 0.91), material_name="wood_dark"))
    # front and back A-frame supports
    for i, x in enumerate((-1.0, 1.0)):
        for side, y in [(-0.62, -0.62), (0.62, 0.62)]:
            objs.append(chopped_log(f"tent_support_{i}_{'L' if y<0 else 'R'}", length=1.95, radius=0.038, location=(x, y * 0.5, 0.95), rotation=(math.radians(60), 0, math.radians(90 if x<0 else -90) + (0.12 if y>0 else -0.12)), material_name="wood_warm"))
    # tent skin – two large cloth panels and rear closure
    left_panel = cloth_panel("tent_canvas_left", size=(2.65, 1.72), location=(0.0, -0.58, 0.92), rotation=(math.radians(66), 0, 0), material_name="canvas", bend=0.03)
    right_panel = cloth_panel("tent_canvas_right", size=(2.65, 1.72), location=(0.0, 0.58, 0.92), rotation=(math.radians(-66), 0, 0), material_name="canvas", bend=0.03)
    rear_panel = cloth_panel("tent_rear", size=(1.25, 1.35), location=(-1.18, 0, 0.78), rotation=(0, math.radians(90), 0), material_name="canvas_dark", bend=0.03)
    objs.extend([left_panel, right_panel, rear_panel])
    # open flap entrance
    flap_left = cloth_panel("tent_flap_left", size=(0.92, 1.14), location=(1.09, -0.18, 0.72), rotation=(math.radians(18), math.radians(90), math.radians(18)), material_name="canvas_dark", bend=0.02)
    flap_right = cloth_panel("tent_flap_right", size=(0.92, 1.14), location=(1.09, 0.18, 0.72), rotation=(math.radians(-18), math.radians(90), math.radians(-18)), material_name="canvas_dark", bend=0.02)
    objs.extend([flap_left, flap_right])
    # groundsheet
    objs.append(primitive_plane("tent_groundsheet", size=1.9, location=(0.05, 0, 0.03), rotation=(0, 0, 0), material=mat("hide")))
    # tied ropes and pegs
    for x in (-1.10, 1.15):
        for y in (-0.82, 0.82):
            peg = stake(f"tent_peg_{x:.1f}_{y:.1f}", height=0.28, radius=0.018, location=(x, y, 0.14), rotation=(math.radians(16), 0, 0), material_name="wood_dark")
            rope = primitive_cylinder(f"tent_guyline_{x:.1f}_{y:.1f}", radius=0.01, depth=0.75, location=((x * 0.62), (y * 0.62), 0.92), rotation=(math.radians(63), 0, math.atan2(y, x)), vertices=6, material=mat("rope"))
            objs.extend([peg, rope])
    return objs



def gen_wall() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    count = 11
    width = 2.65
    step = width / (count - 1)
    for i in range(count):
        x = -width * 0.5 + i * step
        height = random.uniform(1.55, 1.88)
        radius = random.uniform(0.055, 0.075)
        objs.append(stake(f"wall_stake_{i}", height=height, radius=radius, location=(x, 0, height * 0.5), material_name="wood_dark"))
    # cross braces
    objs.append(chopped_log("wall_brace_lower", length=2.6, radius=0.045, location=(0, 0.06, 0.72), rotation=(math.radians(90), 0, math.radians(90)), material_name="wood_warm"))
    objs.append(chopped_log("wall_brace_upper", length=2.5, radius=0.038, location=(0, -0.05, 1.24), rotation=(math.radians(90), 0, math.radians(90)), material_name="wood_warm"))
    return objs



def gen_trap() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    # deadfall snare / primitive trap mix to remain readable in game
    objs.append(board("trap_base", 1.05, 0.72, 0.05, location=(0, 0, 0.03), material_name="wood_dark"))
    objs.append(chopped_log("trap_fall_log", length=0.88, radius=0.07, location=(0.05, 0, 0.30), rotation=(math.radians(90), 0, math.radians(20)), material_name="wood_warm"))
    objs.append(stake("trap_stake_left", height=0.48, radius=0.03, location=(-0.28, -0.18, 0.24), material_name="wood_dark"))
    objs.append(stake("trap_stake_right", height=0.42, radius=0.03, location=(-0.14, 0.18, 0.21), material_name="wood_dark"))
    objs.append(primitive_cylinder("trap_snare_loop", radius=0.16, depth=0.02, location=(0.22, 0, 0.18), rotation=(math.radians(90), 0, 0), vertices=16, material=mat("rope")))
    objs += rope_bundle("trap_rope_tie", segments=3, length=0.22, location=(-0.18, 0.0, 0.24))
    return objs


# -----------------------------------------------------------------------------
# Vegetation / resources
# -----------------------------------------------------------------------------


def leafy_tree_variant(variant: str = "a", stage: str = "mature") -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    profile = {
        "a": dict(height=5.4, trunk=0.30, crown=1.95, asym=0.0),
        "b": dict(height=5.7, trunk=0.28, crown=1.75, asym=0.24),
        "c": dict(height=5.1, trunk=0.26, crown=1.58, asym=-0.18),
    }[variant]
    if stage == "sapling":
        profile = dict(height=1.5 if variant != "c" else 1.35, trunk=0.07, crown=0.42, asym=profile["asym"])
    trunk = tapered_cylinder(
        f"leafy_tree_{variant}_{stage}_trunk",
        profile["trunk"],
        profile["trunk"] * 0.62,
        profile["height"],
        location=(0, 0, profile["height"] * 0.5),
        material=mat("bark_mid"),
        vertices=14,
    )
    add_displace(trunk, strength=profile["trunk"] * 0.16, scale=2.6)
    apply_modifier(trunk, "Displace")
    objs.append(trunk)
    branch_count = 3 if stage == "sapling" else 7
    for i in range(branch_count):
        angle = (i / max(1, branch_count)) * math.tau + random.uniform(-0.4, 0.4)
        z = random.uniform(profile["height"] * 0.45, profile["height"] * 0.82)
        length = random.uniform(profile["crown"] * 0.45, profile["crown"] * 0.72)
        branch = tapered_cylinder(
            f"leafy_tree_{variant}_{stage}_branch_{i}",
            profile["trunk"] * 0.18,
            profile["trunk"] * 0.05,
            length,
            location=(math.cos(angle) * length * 0.20, math.sin(angle) * length * 0.20, z),
            rotation=(math.radians(random.uniform(52, 78)), 0, angle),
            material=mat("bark_mid"),
            vertices=8,
        )
        objs.append(branch)
    cluster_count = 5 if stage == "sapling" else 11
    for i in range(cluster_count):
        a = i / cluster_count * math.tau + random.uniform(-0.35, 0.35)
        r = random.uniform(0.15, profile["crown"] * 0.58)
        z = random.uniform(profile["height"] * 0.58, profile["height"] * 0.92)
        center = (math.cos(a) * r + profile["asym"], math.sin(a) * r, z)
        objs += leaf_cluster(f"leafy_tree_{variant}_{stage}_cluster_{i}", center=center, radius=profile["crown"] * (0.48 if stage == "sapling" else 0.62), count=8 if stage == "sapling" else 16, palette=("leaf_light", "leaf_mid", "leaf_dark"), stretch=(1.0, 1.0, 1.0))
    return objs



def conifer_tree_variant(variant: str = "a", stage: str = "mature") -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    profile = {
        "a": dict(height=5.8, trunk=0.25, skirt=1.35),
        "b": dict(height=6.2, trunk=0.22, skirt=1.08),
        "c": dict(height=5.4, trunk=0.28, skirt=1.55),
    }[variant]
    if stage == "sapling":
        profile = dict(height=1.45 if variant != "b" else 1.65, trunk=0.06, skirt=0.32)
    trunk = tapered_cylinder(f"conifer_tree_{variant}_{stage}_trunk", profile["trunk"], profile["trunk"] * 0.45, profile["height"], location=(0, 0, profile["height"] * 0.5), material=mat("bark_dark"), vertices=12)
    add_displace(trunk, strength=profile["trunk"] * 0.12, scale=3.4)
    apply_modifier(trunk, "Displace")
    objs.append(trunk)
    layers = 4 if stage == "sapling" else 8
    for i in range(layers):
        frac = i / max(1, layers - 1)
        radius = profile["skirt"] * (1.0 - frac * 0.72)
        z = profile["height"] * (0.25 + frac * 0.58)
        layer = primitive_cylinder(f"conifer_tree_{variant}_{stage}_layer_{i}", radius=max(radius, profile["skirt"] * 0.18), depth=0.18 if stage == "sapling" else 0.24, location=(0, 0, z), vertices=8, material=mat("needle"))
        layer.scale = (1.0, 1.0, random.uniform(0.55, 0.95))
        add_displace(layer, strength=0.08 if stage == "sapling" else 0.12, scale=2.0)
        apply_modifier(layer, "Displace")
        objs.append(layer)
    top = primitive_uv_sphere(f"conifer_tree_{variant}_{stage}_top", radius=profile["skirt"] * 0.20, location=(0, 0, profile["height"] * 0.96), segments=12, rings=8, material=mat("needle"))
    top.scale = (0.8, 0.8, 1.4)
    objs.append(top)
    return objs



def dry_tree_variant(variant: str = "a", stage: str = "mature") -> List[bpy.types.Object]:
    profile = {
        "a": dict(height=4.2, trunk=0.20, branches=5),
        "b": dict(height=4.8, trunk=0.18, branches=7),
        "c": dict(height=4.0, trunk=0.17, branches=6),
    }[variant]
    if stage == "sapling":
        profile = dict(height=1.15 if variant == "a" else 1.25, trunk=0.06, branches=2)
    return antler_branch(f"dry_tree_{variant}_{stage}", base=(0, 0, 0), height=profile["height"], lean=(random.uniform(-0.08, 0.08), random.uniform(-0.08, 0.08)), branches=profile["branches"], dry=True)



def gen_resource_rock() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    main = irregular_rock("resource_rock_main", radius=0.72, location=(0, 0, 0.48), scale=(1.45, 1.20, 1.18), material_name="stone")
    objs.append(main)
    sub = irregular_rock("resource_rock_sub", radius=0.34, location=(0.36, -0.20, 0.26), scale=(1.0, 0.9, 0.8), material_name="stone_dark")
    objs.append(sub)
    moss = primitive_uv_sphere("resource_rock_moss", radius=0.20, location=(-0.10, 0.18, 0.72), segments=12, rings=8, material=mat("moss"))
    moss.scale = (1.7, 1.2, 0.4)
    objs.append(moss)
    return objs



def green_bush_variant(variant: str = "a") -> List[bpy.types.Object]:
    profile = {
        "a": dict(radius=0.75, height=0.78, count=18),
        "b": dict(radius=0.58, height=0.98, count=16),
        "c": dict(radius=0.84, height=0.68, count=21),
    }[variant]
    objs: List[bpy.types.Object] = []
    # twig skeleton
    for i in range(10):
        ang = random.uniform(0, math.tau)
        twig = tapered_cylinder(f"green_bush_{variant}_twig_{i}", 0.02, 0.007, random.uniform(0.32, 0.55), location=(0, 0, random.uniform(0.10, 0.20)), rotation=(math.radians(random.uniform(45, 78)), 0, ang), material=mat("wood_dark"), vertices=6)
        objs.append(twig)
    objs += leaf_cluster(f"green_bush_{variant}_foliage", center=(0, 0, profile["height"] * 0.5), radius=profile["radius"], count=profile["count"], palette=("leaf_light", "leaf_mid", "leaf_dark"), stretch=(1.1, 1.1, 0.9 if variant != "b" else 1.2))
    return objs



def berry_bush_variant(variant: str = "a") -> List[bpy.types.Object]:
    objs = green_bush_variant(variant)
    fruit_count = {"a": 18, "b": 14, "c": 22}[variant]
    spread = {"a": 0.50, "b": 0.45, "c": 0.58}[variant]
    for i in range(fruit_count):
        a = random.uniform(0, math.tau)
        r = random.uniform(0.16, spread)
        z = random.uniform(0.28, 0.92)
        berry = primitive_uv_sphere(f"berry_bush_{variant}_fruit_{i}", radius=random.uniform(0.035, 0.055), location=(math.cos(a) * r, math.sin(a) * r, z), segments=10, rings=6, material=mat(random.choice(["berry_red", "berry_dark"])))
        objs.append(berry)
    return objs



def dry_bush_variant(variant: str = "a") -> List[bpy.types.Object]:
    profile = {"a": dict(branches=18, radius=0.55, height=0.72), "b": dict(branches=16, radius=0.46, height=0.92), "c": dict(branches=24, radius=0.66, height=0.80)}[variant]
    objs: List[bpy.types.Object] = []
    for i in range(profile["branches"]):
        a = random.uniform(0, math.tau)
        elev = random.uniform(math.radians(25), math.radians(70))
        length = random.uniform(profile["radius"] * 0.45, profile["radius"])
        twig = tapered_cylinder(f"dry_bush_{variant}_twig_{i}", 0.015, 0.0035, length, location=(0, 0, 0.12), rotation=(elev, 0, a), material=mat("dry_leaf"), vertices=6)
        objs.append(twig)
    return objs



def forest_shrub_variant(variant: str = "a") -> List[bpy.types.Object]:
    return green_bush_variant("a" if variant == "a" else "c")



def tall_grass_clump_variant(variant: str = "a") -> List[bpy.types.Object]:
    return grass_tuft(f"tall_grass_{variant}", blade_count=36 if variant == "a" else 26, height=1.02 if variant == "a" else 0.82, radius=0.36 if variant == "a" else 0.28, add_flowers=False)



def wildflower_patch_variant(variant: str = "a") -> List[bpy.types.Object]:
    return grass_tuft(f"wildflower_patch_{variant}", blade_count=26 if variant == "a" else 18, height=0.48 if variant == "a" else 0.38, radius=0.34 if variant == "a" else 0.24, add_flowers=True)


# -----------------------------------------------------------------------------
# Creatures
# -----------------------------------------------------------------------------


def quadruped_creature(name: str, body_scale=(1.2, 0.7, 0.7), head_scale=(0.45, 0.32, 0.32), leg_height=0.55, tail_length=0.42, horned=False, hunched=False, fur_material="fur_brown") -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    body = primitive_uv_sphere(f"{name}_body", radius=0.6, location=(0, 0, leg_height + body_scale[2] * 0.28), segments=18, rings=10, material=mat(fur_material))
    body.scale = body_scale
    objs.append(body)
    chest = primitive_uv_sphere(f"{name}_chest", radius=0.36, location=(0.42, 0, leg_height + body_scale[2] * 0.32), segments=14, rings=8, material=mat(fur_material))
    chest.scale = (0.9, 0.85, 0.95)
    objs.append(chest)
    hip = primitive_uv_sphere(f"{name}_hip", radius=0.34, location=(-0.42, 0, leg_height + body_scale[2] * 0.26), segments=14, rings=8, material=mat(fur_material))
    hip.scale = (0.95, 0.9, 0.9)
    objs.append(hip)
    head = primitive_uv_sphere(f"{name}_head", radius=0.28, location=(0.88, 0, leg_height + body_scale[2] * 0.45), segments=14, rings=8, material=mat(fur_material))
    head.scale = head_scale
    objs.append(head)
    muzzle = primitive_uv_sphere(f"{name}_muzzle", radius=0.15, location=(1.08, 0, leg_height + body_scale[2] * 0.40), segments=12, rings=6, material=mat("fur_tan" if fur_material != "fur_tan" else "fur_brown"))
    muzzle.scale = (1.35, 0.85, 0.7)
    objs.append(muzzle)
    for sign in (-1, 1):
        eye = primitive_uv_sphere(f"{name}_eye_{'L' if sign<0 else 'R'}", radius=0.03, location=(0.97, sign * 0.10, leg_height + body_scale[2] * 0.50), segments=8, rings=5, material=mat("eye_dark"))
        objs.append(eye)
    leg_xs = (-0.42, -0.08, 0.22, 0.56)
    for i, lx in enumerate(leg_xs):
        for sign in (-1, 1):
            leg = primitive_cylinder(f"{name}_leg_{i}_{'L' if sign<0 else 'R'}", radius=0.07 if not hunched else 0.08, depth=leg_height, location=(lx, sign * 0.22, leg_height * 0.5), vertices=10, material=mat(fur_material))
            objs.append(leg)
            hoof = primitive_cube(f"{name}_hoof_{i}_{'L' if sign<0 else 'R'}", size=(0.11, 0.09, 0.07), location=(lx, sign * 0.22, 0.035), material=mat("wood_dark"))
            objs.append(hoof)
    tail = tapered_cylinder(f"{name}_tail", 0.06, 0.015, tail_length, location=(-0.94, 0, leg_height + body_scale[2] * 0.34), rotation=(math.radians(72 if not hunched else 95), 0, math.radians(180)), material=mat(fur_material), vertices=8)
    objs.append(tail)
    if horned:
        horn_left = tapered_cylinder(f"{name}_horn_left", 0.06, 0.015, 0.35, location=(0.88, -0.12, leg_height + body_scale[2] * 0.62), rotation=(math.radians(32), math.radians(-15), math.radians(12)), material=mat("bone"), vertices=8)
        horn_right = tapered_cylinder(f"{name}_horn_right", 0.06, 0.015, 0.35, location=(0.88, 0.12, leg_height + body_scale[2] * 0.62), rotation=(math.radians(32), math.radians(15), math.radians(-12)), material=mat("bone"), vertices=8)
        objs.extend([horn_left, horn_right])
    if hunched:
        spine = primitive_uv_sphere(f"{name}_spine", radius=0.18, location=(0.05, 0, leg_height + body_scale[2] * 0.72), segments=10, rings=6, material=mat("bone"))
        spine.scale = (1.8, 0.35, 0.45)
        objs.append(spine)
    return objs



def gen_small_prey() -> List[bpy.types.Object]:
    return quadruped_creature("small_prey", body_scale=(0.78, 0.46, 0.40), head_scale=(0.75, 0.75, 0.72), leg_height=0.30, tail_length=0.22, horned=False, hunched=False, fur_material="fur_tan")



def gen_grazer() -> List[bpy.types.Object]:
    return quadruped_creature("grazer", body_scale=(1.32, 0.76, 0.78), head_scale=(1.0, 0.82, 0.82), leg_height=0.62, tail_length=0.36, horned=True, hunched=False, fur_material="fur_brown")



def gen_varnak() -> List[bpy.types.Object]:
    objs = quadruped_creature("varnak", body_scale=(1.25, 0.82, 0.74), head_scale=(1.08, 0.80, 0.78), leg_height=0.60, tail_length=0.54, horned=False, hunched=True, fur_material="fur_dark")
    jaw = primitive_cube("varnak_jaw", size=(0.28, 0.14, 0.08), location=(1.18, 0, 0.92), rotation=(0, 0, math.radians(2)), material=mat("bone"))
    fang_l = tapered_cylinder("varnak_fang_l", 0.02, 0.005, 0.14, location=(1.21, -0.05, 0.87), rotation=(math.radians(170), 0, 0), material=mat("bone"), vertices=6)
    fang_r = tapered_cylinder("varnak_fang_r", 0.02, 0.005, 0.14, location=(1.21, 0.05, 0.87), rotation=(math.radians(170), 0, 0), material=mat("bone"), vertices=6)
    spine_a = tapered_cylinder("varnak_spine_a", 0.04, 0.01, 0.25, location=(-0.15, 0, 1.18), rotation=(math.radians(18), 0, math.radians(90)), material=mat("bone"), vertices=6)
    spine_b = tapered_cylinder("varnak_spine_b", 0.04, 0.01, 0.25, location=(0.15, 0, 1.22), rotation=(math.radians(10), 0, math.radians(90)), material=mat("bone"), vertices=6)
    objs.extend([jaw, fang_l, fang_r, spine_a, spine_b])
    return objs


# -----------------------------------------------------------------------------
# Landmarks
# -----------------------------------------------------------------------------


def gen_landmark_old_tree() -> List[bpy.types.Object]:
    objs = leafy_tree_variant("a", "mature")
    roots = []
    for i in range(6):
        ang = i / 6 * math.tau
        root = tapered_cylinder(f"old_tree_root_{i}", 0.18, 0.06, 1.4, location=(math.cos(ang) * 0.45, math.sin(ang) * 0.45, 0.18), rotation=(math.radians(90), 0, ang), material=mat("bark_dark"), vertices=8)
        roots.append(root)
    objs.extend(roots)
    return objs



def gen_landmark_ruins() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    objs.append(board("ruins_ground", 3.6, 2.8, 0.12, location=(0, 0, 0.06), material_name="stone_dark"))
    for pos, size in [((-1.1, -0.4, 0.55), (0.55, 0.55, 1.1)), ((-0.2, 0.9, 0.45), (0.85, 0.36, 0.90)), ((1.0, -0.2, 0.70), (0.42, 0.42, 1.4)), ((0.35, -0.85, 0.36), (0.95, 0.28, 0.72))]:
        pillar = primitive_cube(f"ruins_block_{len(objs)}", size=size, location=pos, rotation=(0, 0, math.radians(random.uniform(-12, 12))), material=mat("stone"))
        add_bevel(pillar, width=0.04, segments=2)
        apply_modifier(pillar, "Bevel")
        objs.append(pillar)
    for i in range(5):
        objs.append(irregular_rock(f"ruins_rubble_{i}", radius=random.uniform(0.16, 0.24), location=(random.uniform(-1.4, 1.4), random.uniform(-1.1, 1.1), 0.10), scale=(random.uniform(0.8, 1.2), random.uniform(0.8, 1.2), random.uniform(0.7, 1.0)), material_name="stone_dark"))
    objs += grass_tuft("ruins_grass", location=(0, 0, 0.08), blade_count=16, height=0.45, radius=1.1, add_flowers=True)
    return objs



def gen_landmark_pond() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    bank = primitive_cylinder("pond_bank", radius=2.1, depth=0.18, location=(0, 0, 0.09), vertices=24, material=mat("dirt"))
    water = primitive_cylinder("pond_water", radius=1.65, depth=0.04, location=(0, 0, 0.12), vertices=24, material=mat("water"))
    objs.extend([bank, water])
    for i in range(8):
        a = i / 8 * math.tau
        objs.append(irregular_rock(f"pond_rock_{i}", radius=random.uniform(0.10, 0.22), location=(math.cos(a) * random.uniform(1.4, 1.9), math.sin(a) * random.uniform(1.4, 1.9), 0.12), scale=(1.0, 0.9, 0.7), material_name="stone"))
    objs += grass_tuft("pond_reeds", location=(0.95, -1.0, 0.12), blade_count=14, height=0.78, radius=0.32, add_flowers=False)
    return objs



def gen_landmark_camp() -> List[bpy.types.Object]:
    objs = gen_campfire()
    objs += gen_tent()
    # shift sub-campfire and tent apart
    for obj in objs:
        if obj.name.startswith("tent_"):
            obj.location.x += 1.5
        else:
            obj.location.x -= 0.8
    return objs



def gen_landmark_cave() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    main = irregular_rock("cave_main", radius=1.4, location=(0, 0, 1.05), scale=(1.7, 1.2, 1.25), material_name="stone_dark")
    objs.append(main)
    lip = irregular_rock("cave_lip", radius=0.72, location=(0.82, 0, 0.62), scale=(0.9, 1.2, 0.7), material_name="stone")
    objs.append(lip)
    dark = primitive_cube("cave_entrance_fill", size=(1.0, 1.0, 1.1), location=(1.05, 0, 0.58), material=mat("charred"))
    objs.append(dark)
    for i in range(6):
        objs.append(irregular_rock(f"cave_rubble_{i}", radius=random.uniform(0.12, 0.22), location=(random.uniform(0.7, 1.7), random.uniform(-0.65, 0.65), 0.10), scale=(random.uniform(0.8, 1.2), random.uniform(0.8, 1.2), 0.8), material_name="stone"))
    return objs


# -----------------------------------------------------------------------------
# Asset catalog
# -----------------------------------------------------------------------------


def build_catalog() -> Dict[str, AssetSpec]:
    catalog: Dict[str, AssetSpec] = {}

    def add(asset_id: str, category: str, notes: str, generator: Callable[[], List[bpy.types.Object]], min_bounds=(0.0, 0.0, 0.0)) -> None:
        catalog[asset_id] = AssetSpec(asset_id, category, notes, generator, min_bounds)

    # items
    add("wood", "item", "bundle of gathered wood sticks/logs", gen_wood, (0.55, 0.35, 0.20))
    add("stone", "item", "small irregular harvestable stones", gen_stone, (0.28, 0.22, 0.14))
    add("fiber", "item", "tied plant fiber bundle", gen_fiber, (0.55, 0.18, 0.14))
    add("grass", "item", "grass tuft pickup", gen_grass, (0.22, 0.22, 0.25))
    add("meat", "item", "raw meat chunk", gen_meat, (0.35, 0.18, 0.12))
    add("hide", "item", "irregular animal hide", gen_hide, (0.55, 0.45, 0.04))
    add("bone", "item", "animal bone pickup", gen_bone, (0.55, 0.14, 0.12))
    add("berries", "item", "berry cluster pickup", gen_berries, (0.22, 0.22, 0.12))
    add("torch", "item", "primitive handheld torch", gen_torch, (0.14, 0.14, 1.2))
    add("spear", "item", "primitive spear with stone tip", gen_spear, (0.12, 0.12, 2.0))
    add("bow", "item", "primitive wooden bow", gen_bow, (0.08, 0.08, 1.6))
    add("axe", "item", "stone axe for chopping", gen_axe, (0.22, 0.12, 0.9))
    add("pickaxe", "item", "stone pickaxe for mining", gen_pickaxe, (0.36, 0.12, 1.0))

    # placeables
    add("campfire", "placeable", "stone ring campfire", gen_campfire, (0.85, 0.85, 0.45))
    add("storage_box", "placeable", "handmade storage chest from planks", gen_storage_box, (1.0, 0.6, 0.55))
    add("tent", "placeable", "A-frame bushcraft shelter with open flaps", gen_tent, (2.2, 1.5, 1.35))
    add("wall", "placeable", "primitive sharpened palisade wall segment", gen_wall, (2.2, 0.25, 1.55))
    add("trap", "placeable", "primitive snare/deadfall trap", gen_trap, (0.85, 0.55, 0.32))

    # base resource ids
    add("leafy_tree", "resource", "legacy leafy tree id mapped to variant A", lambda: leafy_tree_variant("a", "mature"), (2.9, 2.9, 5.0))
    add("conifer_tree", "resource", "legacy conifer tree id mapped to variant A", lambda: conifer_tree_variant("a", "mature"), (2.3, 2.3, 5.4))
    add("dry_tree", "resource", "legacy dry tree id mapped to variant A", lambda: dry_tree_variant("a", "mature"), (1.6, 1.6, 3.8))
    add("rock", "resource", "harvestable boulder", gen_resource_rock, (1.1, 0.9, 0.9))
    add("green_bush", "resource", "legacy green bush id mapped to variant A", lambda: green_bush_variant("a"), (0.8, 0.8, 0.6))
    add("dry_bush", "resource", "legacy dry bush id mapped to variant A", lambda: dry_bush_variant("a"), (0.5, 0.5, 0.4))
    add("berry_bush", "resource", "legacy berry bush id mapped to variant A", lambda: berry_bush_variant("a"), (0.8, 0.8, 0.7))
    add("grass_or_flower", "resource", "legacy grass/flower id mapped to wildflower patch A", lambda: wildflower_patch_variant("a"), (0.24, 0.24, 0.18))

    # tree variants / saplings
    for variant in ("a", "b", "c"):
        add(f"leafy_tree_{variant}", "resource", f"leafy tree variant {variant.upper()}", lambda v=variant: leafy_tree_variant(v, "mature"), (2.5, 2.5, 5.0))
        add(f"conifer_tree_{variant}", "resource", f"conifer tree variant {variant.upper()}", lambda v=variant: conifer_tree_variant(v, "mature"), (2.0, 2.0, 5.0))
        add(f"dry_tree_{variant}", "resource", f"dry tree variant {variant.upper()}", lambda v=variant: dry_tree_variant(v, "mature"), (1.4, 1.4, 3.8))
        add(f"leafy_sapling_{variant}", "resource", f"young leafy sapling variant {variant.upper()}", lambda v=variant: leafy_tree_variant(v, "sapling"), (0.5, 0.5, 1.1))
        add(f"conifer_sapling_{variant}", "resource", f"young conifer sapling variant {variant.upper()}", lambda v=variant: conifer_tree_variant(v, "sapling"), (0.4, 0.4, 1.0))
        add(f"green_bush_{variant}", "resource", f"green bush variant {variant.upper()}", lambda v=variant: green_bush_variant(v), (0.6, 0.6, 0.5))
        add(f"berry_bush_{variant}", "resource", f"berry bush variant {variant.upper()}", lambda v=variant: berry_bush_variant(v), (0.6, 0.6, 0.5))
        add(f"dry_bush_{variant}", "resource", f"dry bush variant {variant.upper()}", lambda v=variant: dry_bush_variant(v), (0.5, 0.5, 0.4))
    for variant in ("a", "b"):
        add(f"dry_sapling_{variant}", "resource", f"dry sapling variant {variant.upper()}", lambda v=variant: dry_tree_variant("a" if v == "a" else "b", "sapling"), (0.18, 0.18, 0.8))
        add(f"forest_shrub_{variant}", "resource", f"forest shrub variant {variant.upper()}", lambda v=variant: forest_shrub_variant(v), (0.5, 0.5, 0.35))
        add(f"tall_grass_clump_{variant}", "resource", f"tall grass clump variant {variant.upper()}", lambda v=variant: tall_grass_clump_variant(v), (0.3, 0.3, 0.55))
        add(f"wildflower_patch_{variant}", "resource", f"wildflower patch variant {variant.upper()}", lambda v=variant: wildflower_patch_variant(v), (0.22, 0.22, 0.20))

    # creatures
    add("small_prey", "creature", "small prey creature prototype mesh", gen_small_prey, (0.9, 0.4, 0.55))
    add("grazer", "creature", "grazer creature prototype mesh", gen_grazer, (1.6, 0.7, 1.1))
    add("varnak", "creature", "Varnak predator creature prototype mesh", gen_varnak, (1.7, 0.8, 1.2))

    # landmarks
    add("old_tree_landmark", "landmark", "great old tree landmark", gen_landmark_old_tree, (3.0, 3.0, 5.0))
    add("ruins_landmark", "landmark", "overgrown ruins landmark", gen_landmark_ruins, (3.0, 2.0, 1.0))
    add("pond_landmark", "landmark", "freshwater pond landmark", gen_landmark_pond, (3.0, 3.0, 0.3))
    add("camp_landmark", "landmark", "abandoned camp landmark", gen_landmark_camp, (4.0, 2.0, 1.4))
    add("cave_landmark", "landmark", "sealed cave landmark", gen_landmark_cave, (3.0, 2.0, 1.6))

    return catalog


# -----------------------------------------------------------------------------
# Export helpers
# -----------------------------------------------------------------------------


def category_dir(category: str) -> str:
    return {
        "item": ITEM_MODEL_DIR,
        "placeable": PLACEABLE_MODEL_DIR,
        "resource": RESOURCE_MODEL_DIR,
        "creature": CREATURE_MODEL_DIR,
        "landmark": LANDMARK_MODEL_DIR,
    }[category]



def validate_bounds(spec: AssetSpec, obj: bpy.types.Object) -> str:
    b = object_bounds(obj)
    req = spec.min_bounds
    if req == (0.0, 0.0, 0.0):
        return "OK"
    if b[0] < req[0] or b[1] < req[1] or b[2] < req[2]:
        return f"FAILED_BOUNDS required={req} actual={tuple(round(x, 2) for x in b)}"
    return "OK"



def export_asset(spec: AssetSpec, obj: bpy.types.Object) -> AssetRecord:
    base_name = spec.asset_id + "_stylized"
    model_dir = category_dir(spec.category)
    blend_path = os.path.join(SOURCE_BLEND_DIR, base_name + ".blend")
    fbx_path = os.path.join(model_dir, base_name + ".fbx")
    obj_path = os.path.join(model_dir, base_name + ".obj")
    preview_path = os.path.join(PREVIEW_DIR, base_name + "_preview.png")

    validation = validate_bounds(spec, obj)
    notes = spec.notes if validation == "OK" else spec.notes + " | " + validation

    # Frame camera around model.
    b = object_bounds(obj)
    max_dim = max(b)
    cam = bpy.context.scene.camera
    cam.data.ortho_scale = max(2.5, max_dim * 1.65)
    look_at(cam, Vector((0.0, 0.0, max(0.6, b[2] * 0.45))))

    if RENDER_PREVIEWS:
        bpy.context.scene.render.filepath = preview_path
        try:
            bpy.ops.render.render(write_still=True)
        except Exception as exc:
            print(f"Preview render failed for {spec.asset_id}: {exc}")

    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    if EXPORT_OBJ:
        try:
            bpy.ops.wm.obj_export(filepath=obj_path, export_selected_objects=True, export_materials=True)
        except Exception as exc:
            print(f"OBJ export failed for {spec.asset_id}: {exc}")
    if EXPORT_FBX:
        try:
            bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=True, apply_unit_scale=True, bake_space_transform=False, object_types={'MESH'})
        except Exception as exc:
            print(f"FBX export failed for {spec.asset_id}: {exc}")
    if SAVE_BLEND:
        try:
            bpy.ops.wm.save_as_mainfile(filepath=blend_path, copy=True)
        except Exception as exc:
            print(f"BLEND save failed for {spec.asset_id}: {exc}")

    return AssetRecord(
        asset_id=spec.asset_id,
        category=spec.category,
        blend_path=blend_path,
        fbx_path=fbx_path,
        obj_path=obj_path,
        preview_path=preview_path,
        source_object_count=sum(1 for o in bpy.context.scene.objects if o.type == 'MESH'),
        joined_material_count=len(obj.data.materials),
        bounds=tuple(round(x, 4) for x in b),
        notes=notes,
    )


# -----------------------------------------------------------------------------
# Generation flow
# -----------------------------------------------------------------------------


def generate_asset(spec: AssetSpec) -> AssetRecord:
    print(f"\n=== generating {spec.asset_id} ===")
    clear_scene()
    create_material_library()
    setup_scene()
    generated = spec.generator()
    mesh_objs = [o for o in generated if o and o.type == 'MESH']
    asset = join_objects(spec.asset_id + "_stylized", mesh_objs)
    set_origin_to_bottom(asset)
    return export_asset(spec, asset)



def write_outputs(records: List[AssetRecord], catalog: Dict[str, AssetSpec]) -> None:
    with open(MANIFEST_PATH, "w", encoding="utf-8") as fh:
        json.dump([asdict(record) for record in records], fh, indent=2, ensure_ascii=False)

    lines: List[str] = []
    lines.append("# Apex Shift Bushcraft Generator v4 Catalog")
    lines.append("")
    lines.append("This catalog was generated by `bushcraft_asset_generator_v4_master.py`.")
    lines.append("")
    lines.append("## Visual direction")
    lines.append("")
    lines.append("- hand-painted realism")
    lines.append("- bushcraft / survival handmade construction")
    lines.append("- readable silhouettes for isometric view")
    lines.append("- grounded materials: bark, weathered wood, rope, canvas, hide, stone, berries, ash")
    lines.append("")
    lines.append("## Generated assets")
    lines.append("")
    lines.append("| Asset ID | Category | Bounds | Preview | Notes |")
    lines.append("| --- | --- | --- | --- | --- |")
    for rec in records:
        lines.append(f"| `{rec.asset_id}` | `{rec.category}` | `{tuple(round(x, 2) for x in rec.bounds)}` | `{os.path.basename(rec.preview_path)}` | {rec.notes} |")

    lines.append("")
    lines.append("## Known prefab families covered")
    lines.append("")
    families = {
        "items": [k for k, v in catalog.items() if v.category == "item"],
        "placeables": [k for k, v in catalog.items() if v.category == "placeable"],
        "resources": [k for k, v in catalog.items() if v.category == "resource"],
        "creatures": [k for k, v in catalog.items() if v.category == "creature"],
        "landmarks": [k for k, v in catalog.items() if v.category == "landmark"],
    }
    for family, ids in families.items():
        lines.append(f"- **{family}**: {', '.join(f'`{i}`' for i in ids)}")

    lines.append("")
    lines.append("## Integration notes")
    lines.append("")
    lines.append("- Exported objects follow the `*_stylized` naming convention.")
    lines.append("- Resource variants are intended to feed PrefabRegistry / world-builder variant selection.")
    lines.append("- Creature meshes are static art bases and still need gameplay rigging/animation polish.")
    lines.append("- Landmark meshes are intended as hero world props and can be split into sub-prefabs later if needed.")
    lines.append("- This script prioritizes strong readable silhouettes and painterly realism over exact high-poly realism.")

    with open(CATALOG_PATH, "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines))



def generate_all() -> List[AssetRecord]:
    ensure_dirs()
    catalog = build_catalog()
    ids = ONLY_ASSETS if ONLY_ASSETS else list(catalog.keys())
    records = [generate_asset(catalog[asset_id]) for asset_id in ids]
    write_outputs(records, catalog)
    return records


if __name__ == "__main__":
    if GENERATE_ALL_ON_RUN:
        generate_all()
