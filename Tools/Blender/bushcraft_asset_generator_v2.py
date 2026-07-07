"""
Apex Shift Bushcraft Asset Generator v2
======================================

This v2 generator is intentionally silhouette-first. It creates recognizable,
stylized-realistic bushcraft assets for Blender/Unity instead of debug primitives.

Run:
    blender --background --python bushcraft_asset_generator_v2.py

Output:
    ./ApexShift_Bushcraft_Output_v2/

Core rule:
    hero silhouette -> construction -> painted material -> detail

The script focuses on real-world object structure: trees have trunks, branches and
large crowns; tents have A-frame volume and entrance; boxes are made from planks;
campfires have stone rings, logs, ash and flame. It is still procedural and should
be used as a strong base for manual polish, not as a substitute for final art.
"""

from __future__ import annotations

import json
import math
import os
import random
from dataclasses import dataclass, asdict
from typing import Callable, Dict, List, Tuple

import bpy
from mathutils import Vector

SEED = 73
random.seed(SEED)

REPO_ROOT = None
if REPO_ROOT:
    OUTPUT_ROOT = os.path.join(REPO_ROOT, "Assets", "_Project", "Art", "Bushcraft")
    DOCS_ROOT = os.path.join(REPO_ROOT, "Docs", "art")
else:
    OUTPUT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "ApexShift_Bushcraft_Output_v2"))
    DOCS_ROOT = os.path.join(OUTPUT_ROOT, "Docs")

SOURCE_BLEND_DIR = os.path.join(OUTPUT_ROOT, "Source", "Blend")
PREVIEW_DIR = os.path.join(OUTPUT_ROOT, "Previews")
ITEM_MODEL_DIR = os.path.join(OUTPUT_ROOT, "Items", "Models")
PLACEABLE_MODEL_DIR = os.path.join(OUTPUT_ROOT, "Placeables", "Models")
RESOURCE_MODEL_DIR = os.path.join(OUTPUT_ROOT, "Resources", "Models")

EXPORT_FBX = True
EXPORT_OBJ = True
SAVE_BLEND = True
RENDER_PREVIEWS = True
GENERATE_ALL_ON_RUN = True

ONLY_ASSETS: List[str] = ["tent"]

MATS: Dict[str, bpy.types.Material] = {}
MANIFEST = []


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


def ensure_dirs() -> None:
    for p in [OUTPUT_ROOT, DOCS_ROOT, SOURCE_BLEND_DIR, PREVIEW_DIR, ITEM_MODEL_DIR, PLACEABLE_MODEL_DIR, RESOURCE_MODEL_DIR]:
        os.makedirs(p, exist_ok=True)


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    for block in list(bpy.data.materials):
        if block.users == 0:
            bpy.data.materials.remove(block)


def look_at(obj: bpy.types.Object, target: Vector) -> None:
    direction = target - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_scene() -> None:
    bpy.context.scene.render.engine = "CYCLES"
    bpy.context.scene.cycles.samples = 80
    bpy.context.scene.render.resolution_x = 1400
    bpy.context.scene.render.resolution_y = 1000
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.world.color = (0.77, 0.70, 0.58)

    bpy.ops.object.light_add(type="AREA", location=(-4.5, -5.5, 8.0))
    key = bpy.context.object
    key.name = "warm_large_key_light"
    key.data.energy = 520
    key.data.size = 6.5

    bpy.ops.object.light_add(type="AREA", location=(4, 3, 5))
    fill = bpy.context.object
    fill.name = "soft_fill_light"
    fill.data.energy = 85
    fill.data.size = 7

    bpy.ops.object.camera_add(location=(6, -8, 6), rotation=(math.radians(60), 0, math.radians(38)))
    cam = bpy.context.object
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 5.5
    cam.data.lens = 55
    bpy.context.scene.camera = cam
    look_at(cam, Vector((0, 0, 1.4)))

    bpy.ops.mesh.primitive_plane_add(size=9, location=(0, 0, -0.025))
    ground = bpy.context.object
    ground.name = "preview_parchment_ground_NOT_EXPORT"
    ground.data.materials.append(make_material("preview_parchment", (0.73, 0.64, 0.47, 1), noise=True, bump=False))


def make_material(name: str, base: Tuple[float, float, float, float], *, noise: bool = True, bump: bool = True, roughness: float = 0.88) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if not bsdf:
        return mat
    bsdf.inputs["Base Color"].default_value = base
    bsdf.inputs["Metallic"].default_value = 0
    bsdf.inputs["Roughness"].default_value = roughness
    if noise:
        noise_node = nodes.new(type="ShaderNodeTexNoise")
        noise_node.inputs["Scale"].default_value = 20
        noise_node.inputs["Detail"].default_value = 10
        noise_node.inputs["Roughness"].default_value = 0.58
        ramp = nodes.new(type="ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.22
        ramp.color_ramp.elements[1].position = 1.0
        ramp.color_ramp.elements[0].color = (base[0] * 0.55, base[1] * 0.55, base[2] * 0.55, base[3])
        ramp.color_ramp.elements[1].color = (min(base[0] * 1.28, 1), min(base[1] * 1.28, 1), min(base[2] * 1.28, 1), base[3])
        mat.node_tree.links.new(noise_node.outputs["Fac"], ramp.inputs["Fac"])
        mat.node_tree.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    if bump:
        n = nodes.new(type="ShaderNodeTexNoise")
        n.inputs["Scale"].default_value = 48
        n.inputs["Detail"].default_value = 8
        b = nodes.new(type="ShaderNodeBump")
        b.inputs["Strength"].default_value = 0.035
        b.inputs["Distance"].default_value = 0.06
        mat.node_tree.links.new(n.outputs["Fac"], b.inputs["Height"])
        mat.node_tree.links.new(b.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def create_materials() -> None:
    global MATS
    MATS = {
        "bark": make_material("painted_bark_dark", (0.25, 0.13, 0.055, 1)),
        "wood": make_material("painted_wood_warm", (0.50, 0.30, 0.12, 1)),
        "wood_light": make_material("painted_cut_wood_light", (0.82, 0.61, 0.34, 1)),
        "wood_dark": make_material("painted_charred_darkwood", (0.14, 0.08, 0.04, 1)),
        "rope": make_material("painted_rope_fiber", (0.68, 0.57, 0.30, 1)),
        "rope_dark": make_material("painted_rope_shadow", (0.34, 0.27, 0.13, 1)),
        "stone": make_material("painted_stone_gray", (0.45, 0.44, 0.40, 1)),
        "stone_dark": make_material("painted_stone_shadow", (0.27, 0.27, 0.25, 1)),
        "leaf": make_material("painted_leaf_mid", (0.35, 0.45, 0.18, 1)),
        "leaf_light": make_material("painted_leaf_light", (0.56, 0.66, 0.26, 1)),
        "leaf_dark": make_material("painted_leaf_shadow", (0.18, 0.28, 0.11, 1)),
        "leaf_dry": make_material("painted_dry_leaf", (0.56, 0.42, 0.20, 1)),
        "hide": make_material("painted_hide", (0.48, 0.27, 0.12, 1)),
        "meat": make_material("painted_meat", (0.62, 0.16, 0.12, 1)),
        "fat": make_material("painted_fat", (0.91, 0.78, 0.60, 1)),
        "bone": make_material("painted_bone", (0.84, 0.74, 0.55, 1)),
        "berry_red": make_material("painted_berry_red", (0.62, 0.07, 0.05, 1), roughness=0.62),
        "berry_dark": make_material("painted_berry_dark", (0.12, 0.10, 0.24, 1), roughness=0.62),
        "fire_yellow": make_material("painted_fire_yellow", (1.0, 0.78, 0.12, 1), noise=False, bump=False, roughness=0.35),
        "fire_orange": make_material("painted_fire_orange", (1.0, 0.32, 0.04, 1), noise=False, bump=False, roughness=0.35),
        "ash": make_material("painted_ash", (0.11, 0.10, 0.09, 1)),
        "dirt": make_material("painted_dirt", (0.38, 0.29, 0.17, 1)),
        "entrance": make_material("painted_dark_entrance", (0.035, 0.028, 0.022, 1), noise=False, bump=False),
        "flower_white": make_material("painted_flower_white", (0.92, 0.88, 0.72, 1), noise=False, bump=False),
        "flower_yellow": make_material("painted_flower_yellow", (0.95, 0.73, 0.12, 1), noise=False, bump=False),
        "flower_purple": make_material("painted_flower_purple", (0.46, 0.36, 0.70, 1), noise=False, bump=False),
    }


def mat(name: str) -> bpy.types.Material:
    return MATS[name]


def shade(obj: bpy.types.Object, smooth=True) -> bpy.types.Object:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    if smooth:
        try:
            bpy.ops.object.shade_smooth()
        except Exception:
            pass
    obj.select_set(False)
    return obj


def bevel(obj: bpy.types.Object, width: float, segments: int = 2) -> None:
    if width <= 0:
        return
    mod = obj.modifiers.new("handmade_soft_bevel", "BEVEL")
    mod.width = width
    mod.segments = segments
    mod.profile = 0.55
    obj.modifiers.new("painted_weighted_normals", "WEIGHTED_NORMAL")


def displace(obj: bpy.types.Object, strength: float, scale: float = 1.0) -> None:
    if strength <= 0:
        return
    tex = bpy.data.textures.new(obj.name + "_organic_noise", "VORONOI")
    tex.noise_scale = scale
    tex.intensity = 0.4
    mod = obj.modifiers.new("subtle_organic_shape", "DISPLACE")
    mod.strength = strength
    mod.texture = tex


def assign(obj: bpy.types.Object, material: bpy.types.Material) -> bpy.types.Object:
    obj.data.materials.append(material)
    return obj


def apply_scale(obj: bpy.types.Object) -> None:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.select_set(False)


def cylinder_between(name: str, start: Vector, end: Vector, radius: float, material: bpy.types.Material, *,
                     radius2: float | None = None, vertices: int = 16, bevel_width: float = 0.01, deform: float = 0.0) -> bpy.types.Object:
    vec = end - start
    length = vec.length
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=max(length, 0.001), location=(start + end) / 2)
    obj = bpy.context.object
    obj.name = name
    if length > 0.0001:
        obj.rotation_euler = vec.to_track_quat("Z", "Y").to_euler()
    if radius2 is not None:
        mesh = obj.data
        for v in mesh.vertices:
            if v.co.z > 0:
                v.co.x *= radius2 / radius
                v.co.y *= radius2 / radius
    assign(obj, material)
    if bevel_width > 0:
        bevel(obj, bevel_width, segments=2)
    if deform > 0:
        displace(obj, deform, scale=max(length, 0.6))
    shade(obj, True)
    return obj


def handmade_branch(name: str, start: Vector, end: Vector, radius: float, tip_radius: float, material: bpy.types.Material, *,
                    segments: int = 4, side_twigs: int = 0) -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    objs.append(cylinder_between(name, start, end, radius, material, radius2=tip_radius, vertices=segments * 4, bevel_width=0.008, deform=0.012))
    direction = end - start
    length = direction.length
    if length > 0.001 and side_twigs > 0:
        base_dir = direction.normalized()
        for i in range(side_twigs):
            t = (i + 1) / (side_twigs + 1)
            twig_base = start.lerp(end, t)
            offset = Vector((random.uniform(-0.12, 0.12), random.uniform(-0.12, 0.12), random.uniform(0.0, 0.12)))
            twig_end = twig_base + base_dir.cross(Vector((0, 0, 1))).normalized() * random.uniform(0.14, 0.34) + offset
            objs.append(cylinder_between(f"{name}_twig_{i}", twig_base, twig_end, radius * 0.35, material, radius2=tip_radius * 0.7, vertices=8, bevel_width=0.004, deform=0.0))
    return objs


def irregular_rock(name: str, location: Tuple[float, float, float], scale: Tuple[float, float, float], material: bpy.types.Material) -> bpy.types.Object:
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    displace(obj, 0.08, scale=1.6)
    bevel(obj, 0.02, segments=2)
    assign(obj, material)
    shade(obj, True)
    return obj


def leaf_mass(name: str, location: Tuple[float, float, float], scale: Tuple[float, float, float], materials: List[bpy.types.Material], cards: int = 5) -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    for i in range(cards):
        bpy.ops.mesh.primitive_cube_add(location=(location[0] + random.uniform(-0.12, 0.12), location[1] + random.uniform(-0.12, 0.12), location[2] + random.uniform(-0.08, 0.08)))
        leaf = bpy.context.object
        leaf.name = f"{name}_card_{i}"
        leaf.scale = (scale[0] * random.uniform(0.35, 0.9), scale[1] * random.uniform(0.35, 0.9), scale[2] * random.uniform(0.35, 0.9))
        leaf.rotation_euler = (
            math.radians(random.uniform(0, 180)),
            math.radians(random.uniform(0, 180)),
            math.radians(random.uniform(0, 180)),
        )
        displace(leaf, 0.03, scale=1.9)
        assign(leaf, random.choice(materials))
        shade(leaf, True)
        objs.append(leaf)
    return objs


def grass_clump(name: str, count: int, radius: float, height: float, flowers: int = 0) -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    for i in range(count):
        a = i / max(count, 1) * math.tau + random.uniform(-0.2, 0.2)
        base = Vector((0, 0, 0.0))
        end = Vector((math.cos(a) * random.uniform(0.12, radius), math.sin(a) * random.uniform(0.12, radius), random.uniform(height * 0.6, height)))
        objs += handmade_branch(f"{name}_blade_{i}", base, end, random.uniform(0.01, 0.016), 0.002, random.choice([mat("leaf"), mat("leaf_light"), mat("leaf_dark")]), segments=2)
    for i in range(flowers):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=0.04, location=(random.uniform(-0.14, 0.14), random.uniform(-0.14, 0.14), random.uniform(height * 0.55, height * 0.95)))
        flower = bpy.context.object
        flower.name = f"{name}_flower_{i}"
        assign(flower, random.choice([mat("flower_white"), mat("flower_yellow"), mat("flower_purple")]))
        shade(flower, True)
        objs.append(flower)
    return objs


def gen_wood() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    log = cylinder_between(
        "wood_log",
        Vector((-0.95, 0, 0.12)),
        Vector((0.95, 0, 0.12)),
        0.18,
        mat("wood"),
        radius2=0.17,
        vertices=14,
        bevel_width=0.02,
        deform=0.01,
    )
    log.rotation_euler = (math.radians(90), 0, math.radians(2))
    objs.append(log)
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.06, depth=0.10, location=(-0.95, 0, 0.12))
    left_cap = bpy.context.object
    assign(left_cap, mat("wood_light"))
    shade(left_cap, True)
    objs.append(left_cap)
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.06, depth=0.10, location=(0.95, 0, 0.12))
    right_cap = bpy.context.object
    assign(right_cap, mat("wood_light"))
    shade(right_cap, True)
    objs.append(right_cap)
    return objs


def gen_stone() -> List[bpy.types.Object]:
    objs = [irregular_rock("stone_main", (0.0, 0.0, 0.24), (0.7, 0.62, 0.48), mat("stone"))]
    for i in range(4):
        a = i / 4 * math.tau + random.uniform(-0.35, 0.35)
        r = random.uniform(0.18, 0.55)
        objs.append(irregular_rock(f"stone_support_{i}", (math.cos(a) * r, math.sin(a) * r, 0.06), (random.uniform(0.10, 0.22), random.uniform(0.10, 0.24), random.uniform(0.08, 0.18)), random.choice([mat("stone"), mat("stone_dark")])))
    return objs


def gen_fiber() -> List[bpy.types.Object]:
    return grass_clump("fiber_bundle", count=22, radius=0.34, height=0.68, flowers=0)


def gen_grass() -> List[bpy.types.Object]:
    return grass_clump("grass_tuft", count=36, radius=0.42, height=0.82, flowers=0)


def gen_meat() -> List[bpy.types.Object]:
    objs = [irregular_rock("meat_chunk", (0, 0, 0.18), (0.62, 0.42, 0.28), mat("meat"))]
    objs.append(irregular_rock("meat_fat", (0.12, -0.06, 0.23), (0.24, 0.14, 0.08), mat("fat")))
    return objs


def gen_hide() -> List[bpy.types.Object]:
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, 0, 0.14))
    hide = bpy.context.object
    hide.name = "hide_sheet"
    hide.scale = (1.38, 1.08, 1.0)
    hide.rotation_euler = (math.radians(-8), math.radians(1), math.radians(10))
    displace(hide, 0.06, scale=1.2)
    bevel(hide, 0.008, segments=2)
    assign(hide, mat("hide"))
    shade(hide, True)
    return [hide]


def gen_bone() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    objs.append(cylinder_between("bone_main", Vector((-0.48, 0, 0.18)), Vector((0.48, 0, 0.18)), 0.075, mat("bone"), radius2=0.055, vertices=12, bevel_width=0.012, deform=0.0))
    for end_x in (-0.65, 0.65):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=0.17, location=(end_x, 0, 0.18))
        cap = bpy.context.object
        cap.scale = (1.15, 0.95, 0.85)
        assign(cap, mat("bone"))
        shade(cap, True)
        objs.append(cap)
    return objs


def gen_berries() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    for i in range(18):
        a = random.uniform(0, math.tau)
        r = random.uniform(0.08, 0.26)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=random.uniform(0.05, 0.075), location=(math.cos(a) * r, math.sin(a) * r, random.uniform(0.0, 0.28)))
        berry = bpy.context.object
        berry.name = f"berries_{i}"
        assign(berry, random.choice([mat("berry_red"), mat("berry_dark")]))
        shade(berry, True)
        objs.append(berry)
    return objs


def gen_torch() -> List[bpy.types.Object]:
    objs = []
    objs += handmade_branch("torch_shaft", Vector((0, 0, 0)), Vector((0.0, 0.0, 1.55)), 0.07, 0.05, mat("wood"), segments=4)
    objs += handmade_branch("torch_wrap", Vector((0.0, 0.0, 1.05)), Vector((0.12, 0.0, 1.10)), 0.06, 0.04, mat("rope"), segments=2)
    bpy.ops.mesh.primitive_cone_add(vertices=10, radius1=0.22, radius2=0.02, depth=0.65, location=(0, 0, 1.78))
    flame = bpy.context.object
    assign(flame, mat("fire_orange"))
    shade(flame, True)
    objs.append(flame)
    return objs


def gen_spear() -> List[bpy.types.Object]:
    objs = []
    objs += handmade_branch("spear_shaft", Vector((0, 0, 0)), Vector((0.0, 0.0, 2.55)), 0.06, 0.045, mat("wood"), segments=4)
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.09, radius2=0.01, depth=0.38, location=(0, 0, 2.77))
    tip = bpy.context.object
    assign(tip, mat("wood_light"))
    shade(tip, True)
    objs.append(tip)
    objs += handmade_branch("spear_wrap", Vector((0.0, 0.0, 0.55)), Vector((0.14, 0.0, 0.60)), 0.04, 0.03, mat("rope"), segments=2)
    return objs


def gen_bow() -> List[bpy.types.Object]:
    objs = []
    arc_points = [
        (0.00, -0.42, 0.00),
        (0.10, -0.28, 0.68),
        (0.18, -0.10, 1.28),
        (0.10, 0.08, 1.88),
        (0.00, 0.24, 2.48),
    ]
    for i in range(len(arc_points) - 1):
        s = Vector(arc_points[i])
        e = Vector(arc_points[i + 1])
        objs += handmade_branch(f"bow_arc_{i}", s, e, 0.055, 0.042, mat("wood"), segments=4)
    objs += handmade_branch("bow_string", Vector((0.02, -0.42, 0.02)), Vector((0.02, 0.24, 2.46)), 0.010, 0.006, mat("rope_dark"), segments=2)
    return objs


def gen_campfire() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    for i in range(12):
        a = i / 12 * math.tau
        objs.append(irregular_rock(f"campfire_ring_{i}", (math.cos(a) * 0.75, math.sin(a) * 0.62, 0.02), (0.14, 0.11, 0.08), random.choice([mat("stone"), mat("stone_dark")])))
    objs += handmade_branch("campfire_log_a", Vector((-0.42, -0.08, 0.09)), Vector((0.46, 0.06, 0.13)), 0.06, 0.04, mat("wood_dark"), segments=4)
    objs += handmade_branch("campfire_log_b", Vector((-0.18, 0.34, 0.08)), Vector((0.24, -0.24, 0.18)), 0.06, 0.04, mat("wood_dark"), segments=4)
    objs += handmade_branch("campfire_log_c", Vector((-0.08, -0.28, 0.11)), Vector((0.08, 0.30, 0.17)), 0.06, 0.04, mat("wood_dark"), segments=4)
    objs.append(irregular_rock("campfire_ash", (0.0, 0.0, 0.02), (0.48, 0.36, 0.06), mat("ash")))
    bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=0.25, radius2=0.02, depth=0.75, location=(0, 0, 0.55))
    flame = bpy.context.object
    assign(flame, mat("fire_yellow"))
    shade(flame, True)
    objs.append(flame)
    return objs


def gen_storage_box() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    bpy.ops.mesh.primitive_cube_add(location=(0, 0, 0.34))
    body = bpy.context.object
    body.name = "box_body"
    body.scale = (0.72, 0.48, 0.34)
    displace(body, 0.035, scale=1.15)
    bevel(body, 0.03, segments=3)
    assign(body, mat("wood"))
    shade(body, True)
    objs.append(body)

    bpy.ops.mesh.primitive_cube_add(location=(0, 0, 0.72))
    lid = bpy.context.object
    lid.name = "box_lid"
    lid.scale = (0.78, 0.54, 0.12)
    displace(lid, 0.018, scale=1.1)
    bevel(lid, 0.022, segments=2)
    assign(lid, mat("wood_light"))
    shade(lid, True)
    objs.append(lid)

    for x in (-0.42, 0.42):
        objs += handmade_branch(f"box_band_{x}", Vector((x, -0.49, 0.30)), Vector((x, 0.49, 0.30)), 0.035, 0.024, mat("rope"), segments=2)
    for y in (-0.36, 0.36):
        objs += handmade_branch(f"box_strap_{y}", Vector((-0.70, y, 0.44)), Vector((0.70, y, 0.44)), 0.028, 0.02, mat("rope_dark"), segments=2)
    bpy.ops.mesh.primitive_cube_add(location=(0, 0.54, 0.34))
    front_trim = bpy.context.object
    front_trim.scale = (0.70, 0.04, 0.22)
    assign(front_trim, mat("wood_dark"))
    shade(front_trim, True)
    objs.append(front_trim)
    return objs


def gen_tent() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    bpy.ops.mesh.primitive_cube_add(location=(0, 0.00, 0.08))
    base = bpy.context.object
    base.name = "tent_base"
    base.scale = (1.28, 0.90, 0.06)
    displace(base, 0.015, scale=1.0)
    bevel(base, 0.015, segments=2)
    assign(base, mat("dirt"))
    shade(base, True)
    objs.append(base)

    front_y, back_y = -0.98, 1.12
    opening_y = -0.20
    ridge_z = 1.72
    foot_x = 1.02

    # Exposed frame: this should read clearly as an A-frame shelter.
    for y, label in [(front_y, "front"), (back_y, "back")]:
        objs.append(cylinder_between(f"tent_{label}_left_pole", Vector((-foot_x, y, 0.03)), Vector((-0.08, y, ridge_z)), 0.060, mat("bark"), radius2=0.042, vertices=10, bevel_width=0.010))
        objs.append(cylinder_between(f"tent_{label}_right_pole", Vector((foot_x, y, 0.03)), Vector((0.08, y, ridge_z)), 0.060, mat("bark"), radius2=0.042, vertices=10, bevel_width=0.010))
        objs.append(cylinder_between(f"tent_{label}_cross_sill", Vector((-0.92, y, 0.06)), Vector((0.92, y, 0.06)), 0.026, mat("rope_dark"), radius2=0.022, vertices=8, bevel_width=0.006))

    objs.append(cylinder_between("tent_ridge_pole", Vector((0.0, front_y, ridge_z)), Vector((0.0, back_y, ridge_z)), 0.040, mat("wood_dark"), radius2=0.032, vertices=10, bevel_width=0.008))
    objs.append(cylinder_between("tent_front_tie", Vector((-0.22, front_y + 0.02, 1.22)), Vector((0.22, front_y + 0.02, 1.22)), 0.018, mat("rope"), radius2=0.016, vertices=8, bevel_width=0.004))
    objs.append(cylinder_between("tent_back_tie", Vector((-0.18, back_y - 0.03, 1.18)), Vector((0.18, back_y - 0.03, 1.18)), 0.018, mat("rope"), radius2=0.016, vertices=8, bevel_width=0.004))

    def cloth_quad(name: str, pts: List[Vector], material_name: str) -> bpy.types.Object:
        mesh = bpy.data.meshes.new(name + "Mesh")
        mesh.from_pydata([tuple(p) for p in pts], [], [(0, 1, 2, 3)])
        mesh.update()
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.collection.objects.link(obj)
        assign(obj, mat(material_name))
        bevel(obj, 0.010, segments=1)
        displace(obj, 0.014, scale=0.9)
        shade(obj, True)
        return obj

    # Open entrance: the cloth starts behind the doorway, not over it.
    left_roof = cloth_quad(
        "tent_left_roof",
        [
            Vector((-foot_x, opening_y, 0.08)),
            Vector((-foot_x, back_y - 0.02, 0.08)),
            Vector((0.0, back_y - 0.02, ridge_z)),
            Vector((0.0, opening_y + 0.10, ridge_z)),
        ],
        "hide",
    )
    right_roof = cloth_quad(
        "tent_right_roof",
        [
            Vector((foot_x, opening_y, 0.08)),
            Vector((0.0, opening_y + 0.10, ridge_z)),
            Vector((0.0, back_y - 0.02, ridge_z)),
            Vector((foot_x, back_y - 0.02, 0.08)),
        ],
        "hide",
    )
    objs.extend([left_roof, right_roof])

    # Small front side pieces that frame the entrance and make it look like a shelter.
    for side in (-1, 1):
        flap = cloth_quad(
            f"tent_front_flap_{'left' if side < 0 else 'right'}",
            [
                Vector((side * 0.90, front_y + 0.02, 0.10)),
                Vector((side * 0.56, opening_y + 0.02, 0.18)),
                Vector((side * 0.30, opening_y + 0.05, 1.02)),
                Vector((side * 0.68, front_y + 0.00, 1.24)),
            ],
            "hide",
        )
        objs.append(flap)

    # Keep the opening dark and clearly visible.
    entrance_pts = [(-0.58, front_y + 0.01, 0.10), (0.58, front_y + 0.01, 0.10), (0.0, front_y + 0.01, 1.28)]
    mesh = bpy.data.meshes.new("tent_entranceMesh")
    mesh.from_pydata(entrance_pts, [], [(0, 1, 2)])
    mesh.update()
    entrance = bpy.data.objects.new("tent_entrance", mesh)
    bpy.context.collection.objects.link(entrance)
    assign(entrance, mat("entrance"))
    shade(entrance, True)
    objs.append(entrance)

    # Dark floor shadow for depth.
    bpy.ops.mesh.primitive_cube_add(location=(0.0, front_y + 0.36, 0.045))
    interior_shadow = bpy.context.object
    interior_shadow.name = "tent_interior_shadow"
    interior_shadow.scale = (0.52, 0.34, 0.02)
    assign(interior_shadow, mat("entrance"))
    shade(interior_shadow, True)
    objs.append(interior_shadow)

    # Small tether stones, like the reference has at the base.
    for x in (-0.82, 0.82):
        for y in (front_y + 0.06, back_y - 0.06):
            objs.append(irregular_rock(f"tent_anchor_stone_{x:.2f}_{y:.2f}", (x, y, 0.05), (0.12, 0.10, 0.06), mat("stone")))

    # Rope lashings at the apex and the front posts.
    for y in (front_y, back_y):
        objs += handmade_branch(
            f"tent_apex_lashing_{y:.2f}",
            Vector((-0.12, y, ridge_z - 0.01)),
            Vector((0.12, y, ridge_z - 0.01)),
            0.018,
            0.012,
            mat("rope"),
            segments=2,
        )
    for x in (-0.72, 0.72):
        objs += handmade_branch(
            f"tent_front_leg_lashing_{x:.2f}",
            Vector((x, front_y + 0.04, 0.18)),
            Vector((x, front_y + 0.10, 0.48)),
            0.014,
            0.010,
            mat("rope_dark"),
            segments=2,
        )

    # A few thin thatch strips so the roof reads as layered rather than as one boxy card.
    for side in (-1, 1):
        for i, y in enumerate([opening_y + 0.06, 0.18, 0.42, 0.68, 0.92]):
            start = Vector((side * (0.96 - i * 0.04), y, 0.18 + i * 0.12))
            end = Vector((side * 0.14, y + 0.03, ridge_z - 0.06 + i * 0.01))
            objs += handmade_branch(f"tent_{'left' if side < 0 else 'right'}_thatch_{i}", start, end, 0.020, 0.012, random.choice([mat("leaf_dry"), mat("rope"), mat("wood_dark")]), segments=3)
    return objs


def gen_wall() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    for i in range(9):
        x = -0.96 + i * 0.24
        objs += handmade_branch(f"wall_pale_{i}", Vector((x, 0, 0)), Vector((x, 0, random.uniform(1.55, 2.15))), 0.065, 0.04, mat("wood"), segments=4)
    objs += handmade_branch("wall_rail_low", Vector((-1.02, 0, 0.36)), Vector((1.02, 0, 0.36)), 0.045, 0.03, mat("rope"), segments=2)
    objs += handmade_branch("wall_rail_high", Vector((-1.02, 0, 1.15)), Vector((1.02, 0, 1.15)), 0.045, 0.03, mat("rope"), segments=2)
    return objs


def gen_trap() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    objs += handmade_branch("trap_frame_a", Vector((-0.66, -0.42, 0.10)), Vector((0.66, -0.42, 0.30)), 0.045, 0.03, mat("wood"), segments=3)
    objs += handmade_branch("trap_frame_b", Vector((-0.66, 0.42, 0.10)), Vector((0.66, 0.42, 0.30)), 0.045, 0.03, mat("wood"), segments=3)
    objs += handmade_branch("trap_cross_a", Vector((-0.42, -0.26, 0.20)), Vector((0.42, 0.26, 0.20)), 0.04, 0.025, mat("rope_dark"), segments=2)
    objs += handmade_branch("trap_cross_b", Vector((-0.42, 0.26, 0.20)), Vector((0.42, -0.26, 0.20)), 0.04, 0.025, mat("rope_dark"), segments=2)
    for i in range(8):
        a = i / 8 * math.tau
        spike_base = Vector((math.cos(a) * 0.48, math.sin(a) * 0.30, 0.08))
        spike_tip = Vector((math.cos(a) * 0.58, math.sin(a) * 0.38, 0.62))
        objs += handmade_branch(f"trap_spike_{i}", spike_base, spike_tip, 0.03, 0.008, mat("wood_light"), segments=3)
    return objs


def gen_leafy_tree() -> List[bpy.types.Object]:
    objs: List[bpy.types.Object] = []
    objs += handmade_branch("leafy_tree_trunk", Vector((0, 0, 0)), Vector((0.08, -0.04, 3.65)), 0.24, 0.10, mat("bark"), segments=5, side_twigs=5)
    for i in range(8):
        a = i / 8 * math.tau + random.uniform(-0.25, 0.25)
        base = Vector((0.05, 0.02, random.uniform(1.75, 2.7)))
        end = Vector((math.cos(a) * random.uniform(0.9, 1.7), math.sin(a) * random.uniform(0.9, 1.7), random.uniform(2.7, 3.8)))
        objs += handmade_branch(f"leafy_tree_main_branch_{i}", base, end, random.uniform(0.10, 0.16), 0.035, mat("bark"), segments=4, side_twigs=1)
    positions = [
        (0,0,3.65), (-0.9,-0.35,3.35), (0.9,-0.25,3.35), (-0.25,0.95,3.35), (0.45,0.85,3.45),
        (-0.7,0.45,3.95), (0.75,0.35,3.95), (0.05,-0.85,3.85), (0.0,0.1,4.35), (-0.35,0.0,4.05)
    ]
    for idx, p in enumerate(positions):
        objs += leaf_mass(f"leafy_tree_canopy_{idx}", p, (random.uniform(0.55,0.88), random.uniform(0.48,0.80), random.uniform(0.42,0.68)), [mat("leaf"), mat("leaf_light"), mat("leaf_dark")], cards=7)
    return objs


def gen_conifer_tree() -> List[bpy.types.Object]:
    objs = []
    objs.append(cylinder_between("conifer_visible_trunk", Vector((0,0,0)), Vector((0.02,0.02,4.9)), 0.25, mat("bark"), radius2=0.12, vertices=18, bevel_width=0.026, deform=0.025))
    layers = [(0.80,1.60,0.52),(1.35,1.46,0.50),(1.90,1.28,0.46),(2.42,1.10,0.42),(2.95,0.90,0.38),(3.46,0.70,0.34),(3.95,0.50,0.30),(4.32,0.34,0.26),(4.68,0.22,0.24)]
    for i,(z,r,h) in enumerate(layers):
        objs.append(conifer_layer_mesh(f"conifer_needle_skirt_{i}", z, r, h, random.choice([mat("leaf_dark"), mat("leaf"), mat("leaf_light")])) )
        for j in range(3):
            a=random.uniform(0,math.tau)
            objs += leaf_mass(f"conifer_side_mass_{i}_{j}", (math.cos(a)*r*0.35, math.sin(a)*r*0.35, z+0.05), (r*0.22, r*0.16, 0.16), [mat("leaf_dark"),mat("leaf")], cards=2)
    objs += leaf_mass("conifer_top_cap", (0.0, 0.0, 5.15), (0.22, 0.18, 0.20), [mat("leaf_dark"), mat("leaf")], cards=4)
    return objs


def conifer_layer_mesh(name: str, z: float, radius: float, height: float, material: bpy.types.Material) -> bpy.types.Object:
    verts = [(0,0,z+height*0.45)]
    n = 20
    for i in range(n):
        a = i/n*math.tau
        r = radius*random.uniform(0.75,1.12)
        verts.append((math.cos(a)*r, math.sin(a)*r, z + random.uniform(-0.08,0.06)))
    faces=[]
    for i in range(1,n+1):
        faces.append((0,i,1 if i==n else i+1))
    mesh=bpy.data.meshes.new(name+"Mesh"); mesh.from_pydata(verts,[],faces); mesh.update()
    obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj)
    assign(obj, material); shade(obj, True); displace(obj,0.018,0.8)
    return obj


def gen_dry_tree() -> List[bpy.types.Object]:
    objs = []
    objs.append(cylinder_between("dry_tree_twisted_trunk", Vector((0,0,0)), Vector((0.12,-0.06,4.2)), 0.23, mat("wood_dark"), radius2=0.10, vertices=14, bevel_width=0.022, deform=0.045))
    for i in range(15):
        a = i/15*math.tau+random.uniform(-0.25,0.25)
        base = Vector((0.08,-0.03,random.uniform(1.2,3.9)))
        length = random.uniform(0.55,1.45)
        end = Vector((math.cos(a)*length, math.sin(a)*length, base.z+random.uniform(-0.15,0.75)))
        objs += handmade_branch(f"dry_tree_branch_{i}", base, end, random.uniform(0.035,0.075), 0.006, mat("wood_dark"), segments=3, side_twigs=1)
    return objs


def gen_resource_rock() -> List[bpy.types.Object]:
    objs = [irregular_rock("resource_boulder_main", (0,0,0.40), (0.88,0.70,0.48), mat("stone"))]
    for i in range(7):
        a=random.uniform(0,math.tau); r=random.uniform(0.42,0.85); s=random.uniform(0.11,0.28)
        objs.append(irregular_rock(f"resource_boulder_support_{i}", (math.cos(a)*r, math.sin(a)*r, 0.07), (s,s*random.uniform(0.7,1.2),s*0.55), random.choice([mat("stone"),mat("stone_dark")])) )
    return objs


def gen_green_bush() -> List[bpy.types.Object]:
    objs=[]
    for i in range(8):
        a=i/8*math.tau; r=random.uniform(0.08,0.43)
        objs += leaf_mass(f"green_bush_mass_{i}", (math.cos(a)*r, math.sin(a)*r, random.uniform(0.30,0.65)), (random.uniform(0.34,0.58),random.uniform(0.30,0.52),random.uniform(0.28,0.46)), [mat("leaf"),mat("leaf_light"),mat("leaf_dark")], cards=5)
    for i in range(5):
        a=random.uniform(0,math.tau)
        objs += handmade_branch(f"green_bush_inner_stem_{i}", Vector((0,0,0.05)), Vector((math.cos(a)*0.3,math.sin(a)*0.3,0.45)), 0.025,0.010,mat("bark"),segments=2)
    return objs


def gen_berry_bush() -> List[bpy.types.Object]:
    objs = gen_green_bush()
    for i in range(28):
        a=random.uniform(0,math.tau); r=random.uniform(0.20,0.60); z=random.uniform(0.35,0.92)
        bpy.ops.mesh.primitive_uv_sphere_add(segments=14, ring_count=7, radius=random.uniform(0.045,0.075), location=(math.cos(a)*r,math.sin(a)*r,z))
        berry=bpy.context.object; berry.name=f"berry_bush_visible_fruit_{i}"; assign(berry, random.choice([mat("berry_red"), mat("berry_dark")])); shade(berry); objs.append(berry)
    return objs


def gen_dry_bush() -> List[bpy.types.Object]:
    objs=[]
    for i in range(30):
        a=random.uniform(0,math.tau); length=random.uniform(0.35,0.95)
        start=Vector((0,0,0.04)); end=Vector((math.cos(a)*length*random.uniform(0.35,0.85), math.sin(a)*length*random.uniform(0.35,0.85), random.uniform(0.30,0.90)))
        objs += handmade_branch(f"dry_bush_twig_{i}", start, end, random.uniform(0.010,0.024),0.002,mat("leaf_dry"),segments=2)
    return objs


def gen_grass_or_flower() -> List[bpy.types.Object]:
    return grass_clump("grass_or_flower", count=44, radius=0.48, height=0.88, flowers=9)


GENERATORS: Dict[str, Tuple[str, Callable[[], List[bpy.types.Object]], str]] = {
    "wood": ("item", gen_wood, "bundle of real sticks/logs"),
    "stone": ("item", gen_stone, "small pile of irregular stones"),
    "fiber": ("item", gen_fiber, "tied plant fiber bundle"),
    "grass": ("item", gen_grass, "grass tuft pickup"),
    "meat": ("item", gen_meat, "stylized raw meat cut"),
    "hide": ("item", gen_hide, "irregular animal hide"),
    "bone": ("item", gen_bone, "bone pickup"),
    "berries": ("item", gen_berries, "berries and leaves"),
    "torch": ("item", gen_torch, "primitive torch with wrapped head"),
    "spear": ("item", gen_spear, "wooden spear with stone tip"),
    "bow": ("item", gen_bow, "primitive wooden bow"),
    "campfire": ("placeable", gen_campfire, "stone ring, logs, ash and flame"),
    "storage_box": ("placeable", gen_storage_box, "handmade plank storage chest"),
    "tent": ("placeable", gen_tent, "A-frame bushcraft shelter with entrance"),
    "wall": ("placeable", gen_wall, "sharpened wooden palisade"),
    "trap": ("placeable", gen_trap, "handmade snare trap"),
    "leafy_tree": ("resource", gen_leafy_tree, "deciduous tree: trunk, branches, large crown"),
    "conifer_tree": ("resource", gen_conifer_tree, "conifer with trunk and layered needle skirts"),
    "dry_tree": ("resource", gen_dry_tree, "dead tree with dry branches"),
    "rock": ("resource", gen_resource_rock, "large harvestable boulder"),
    "green_bush": ("resource", gen_green_bush, "dense green bush"),
    "dry_bush": ("resource", gen_dry_bush, "dry twig bush"),
    "grass_or_flower": ("resource", gen_grass_or_flower, "grass and flowers"),
    "berry_bush": ("resource", gen_berry_bush, "fruiting berry bush"),
}

MIN_BOUNDS = {
    "leafy_tree": (3.0, 3.0, 5.0),
    "conifer_tree": (2.5, 2.5, 5.0),
    "dry_tree": (1.8, 1.8, 3.8),
    "campfire": (1.1, 1.1, 0.7),
    "storage_box": (1.1, 0.65, 0.55),
    "tent": (1.6, 2.0, 1.4),
    "wall": (2.0, 0.15, 1.5),
    "trap": (1.1, 0.75, 0.45),
}


def category_dir(category: str) -> str:
    return {"item": ITEM_MODEL_DIR, "placeable": PLACEABLE_MODEL_DIR, "resource": RESOURCE_MODEL_DIR}[category]


def bounds(obj: bpy.types.Object) -> Tuple[float, float, float]:
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    return tuple(max(c[i] for c in corners) - min(c[i] for c in corners) for i in range(3))


def join_meshes(name: str, meshes: List[bpy.types.Object]) -> bpy.types.Object:
    meshes = [o for o in meshes if o and o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    return obj


def set_origin_base(obj: bpy.types.Object) -> None:
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    x = sum(c.x for c in corners)/8
    y = sum(c.y for c in corners)/8
    z = min(c.z for c in corners)
    bpy.context.scene.cursor.location = (x,y,z)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR", center="MEDIAN")
    obj.select_set(False)


def validate_bounds(asset_id: str, obj: bpy.types.Object) -> str:
    b = bounds(obj)
    req = MIN_BOUNDS.get(asset_id)
    if not req:
        return "OK"
    if b[0] < req[0] or b[1] < req[1] or b[2] < req[2]:
        return f"FAILED_BOUNDS required={req} actual={tuple(round(x,2) for x in b)}"
    return "OK"


def generate_asset(asset_id: str) -> AssetRecord:
    category, fn, notes = GENERATORS[asset_id]
    clear_scene()
    create_materials()
    setup_scene()
    objs = fn()
    mesh_objs = [o for o in objs if o.type == "MESH"]
    asset = join_meshes(asset_id + "_stylized", mesh_objs)
    set_origin_base(asset)

    b = bounds(asset)
    max_dim = max(b)
    bpy.context.scene.camera.data.ortho_scale = max(2.4, max_dim * 1.55)
    look_at(bpy.context.scene.camera, Vector((0,0,max(0.6,b[2]*0.42))))

    validation = validate_bounds(asset_id, asset)
    if validation != "OK":
        notes += " | " + validation
        print("WARNING", asset_id, validation)

    base = asset_id + "_stylized"
    model_dir = category_dir(category)
    blend_path = os.path.join(SOURCE_BLEND_DIR, base + ".blend")
    fbx_path = os.path.join(model_dir, base + ".fbx")
    obj_path = os.path.join(model_dir, base + ".obj")
    preview_path = os.path.join(PREVIEW_DIR, base + "_preview.png")

    if SAVE_BLEND:
        bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    bpy.ops.object.select_all(action="DESELECT")
    asset.select_set(True)
    bpy.context.view_layer.objects.active = asset
    if EXPORT_FBX:
        bpy.ops.export_scene.fbx(filepath=fbx_path, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False)
    if EXPORT_OBJ:
        bpy.ops.wm.obj_export(filepath=obj_path, export_selected_objects=True, export_materials=True)
    if RENDER_PREVIEWS:
        asset.select_set(False)
        bpy.context.scene.render.filepath = preview_path
        bpy.ops.render.render(write_still=True)

    rec = AssetRecord(asset_id, category, blend_path, fbx_path, obj_path, preview_path, len(mesh_objs), len(asset.data.materials), b, notes)
    MANIFEST.append(rec)
    return rec


def write_manifests(records: List[AssetRecord]) -> None:
    with open(os.path.join(OUTPUT_ROOT, "bushcraft_model_manifest_v2.json"), "w", encoding="utf-8") as f:
        json.dump([asdict(r) for r in records], f, indent=2, ensure_ascii=False)
    lines = [
        "# Apex Shift — Bushcraft Stylized Model Manifest v2", "",
        "Generator is silhouette-first and uses real-world object structure as reference.", "",
        "| Asset | Category | Source meshes | Materials | Bounds XYZ | Preview | Notes |",
        "|---|---:|---:|---:|---:|---|---|",
    ]
    for r in records:
        lines.append(f"| `{r.asset_id}` | {r.category} | {r.source_object_count} | {r.joined_material_count} | `{tuple(round(x,2) for x in r.bounds)}` | `{r.preview_path}` | {r.notes} |")
    with open(os.path.join(DOCS_ROOT, "bushcraft-models-manifest-v2.md"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines))


def generate_all() -> List[AssetRecord]:
    ensure_dirs()
    ids = ONLY_ASSETS if ONLY_ASSETS else list(GENERATORS.keys())
    records = []
    for asset_id in ids:
        print("\n=== generating", asset_id, "===")
        records.append(generate_asset(asset_id))
    write_manifests(records)
    return records


if __name__ == "__main__":
    generate_all()
