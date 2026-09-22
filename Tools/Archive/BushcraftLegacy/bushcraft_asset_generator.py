"""
Main generator entry-point for stylized bushcraft assets.

The script can be executed from Blender MCP or directly from Blender.
"""

from __future__ import annotations

import json
import math
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

import bpy
from mathutils import Euler, Vector

from bushcraft_asset_library import (
    ASSET_ROOT,
    apply_bevel,
    apply_flat_or_soft_normals,
    apply_subtle_deform,
    assign_material,
    auto_uv_unwrap,
    cleanup_generated_collection,
    create_berry_cluster,
    create_bone_piece,
    create_branch_segment,
    create_bushcraft_materials,
    create_fiber_bundle,
    create_flame_mesh,
    create_grass_clump,
    create_hide_sheet,
    create_leaf_cluster,
    create_plank,
    create_rock_chunk,
    create_rope_binding,
    create_split_log,
    ensure_collection,
    ensure_export_directories,
    join_objects,
    randomize_transform,
    set_origin_to_base_center,
)


REPO_ROOT = Path(__file__).resolve().parents[2]
DOC_ROOT = REPO_ROOT / "Docs" / "art"
BLEND_ROOT = ASSET_ROOT / "Source" / "Blend"


@dataclass
class AssetRecord:
    asset_id: str
    category: str
    blend_path: str
    fbx_path: str
    obj_path: str
    preview_path: str
    texture_paths: list[str]
    materials: list[str]
    polycount: int
    notes: str
    unity_usage: str


def _asset_name(asset_id: str) -> str:
    return f"{asset_id}_stylized"


def _category_dir(category: str) -> Path:
    mapping = {
        "item": ASSET_ROOT / "Items",
        "resource": ASSET_ROOT / "Resources",
        "placeable": ASSET_ROOT / "Placeables",
    }
    return mapping[category]


def _clear_scene_for_asset() -> None:
    bpy.ops.object.select_all(action="DESELECT")


def _finalize_asset(obj: bpy.types.Object, material_names: list[str], origin_mode: str = "base") -> bpy.types.Object:
    for material_name in material_names:
        assign_material(obj, material_name)
    apply_bevel(obj, width=0.018, segments=2)
    apply_subtle_deform(obj, strength=0.04)
    auto_uv_unwrap(obj)
    apply_flat_or_soft_normals(obj, mode="soft")
    if origin_mode == "base":
        set_origin_to_base_center(obj)
    return obj


def _polycount(obj: bpy.types.Object) -> int:
    if not isinstance(obj.data, bpy.types.Mesh):
        return 0
    return sum(len(poly.vertices) - 2 for poly in obj.data.polygons)


def _record(asset_id: str, category: str, obj: bpy.types.Object, notes: str, unity_usage: str) -> AssetRecord:
    stem = _asset_name(asset_id)
    base_dir = _category_dir(category)
    model_dir = base_dir / "Models"
    texture_dir = base_dir / "Textures"
    return AssetRecord(
        asset_id=asset_id,
        category=category,
        blend_path=str((BLEND_ROOT / f"{stem}.blend").as_posix()),
        fbx_path=str((model_dir / f"{stem}.fbx").as_posix()),
        obj_path=str((model_dir / f"{stem}.obj").as_posix()),
        preview_path=str((model_dir / f"{stem}_preview.png").as_posix()),
        texture_paths=[
            str((texture_dir / f"{stem}_albedo.png").as_posix()),
            str((texture_dir / f"{stem}_roughness.png").as_posix()),
        ],
        materials=[slot.name for slot in obj.data.materials if slot],
        polycount=_polycount(obj),
        notes=notes,
        unity_usage=unity_usage,
    )


def _scatter_logs(name: str, count: int = 6) -> bpy.types.Object:
    parts = []
    for index in range(count):
        log = create_split_log(f"{name}_log_{index:02d}", length=1.05 + index * 0.05, radius=0.12 - index * 0.005)
        log.location = Vector((index * 0.12 - 0.32, (index % 2) * 0.16 - 0.08, 0.05 * (index % 3)))
        log.rotation_euler = Euler((0.0, random_angle(index, 0.08), random_angle(index + 2, 0.55)), "XYZ")
        parts.append(log)
    bundle = join_objects(parts, name)
    return bundle


def random_angle(seed: int, scale: float) -> float:
    return math.sin(seed * 1.91) * scale


def generate_item_wood() -> bpy.types.Object:
    parts = []
    for index in range(7):
        log = create_split_log(f"wood_log_{index:02d}", length=1.05 + 0.11 * (index % 4), radius=0.105 + 0.012 * (index % 3), flatness=0.76)
        assign_material(log, "wood_bark_painted")
        log.location = Vector((0.08 * (index - 3), 0.18 * ((index % 3) - 1), 0.10 + 0.12 * (index // 4)))
        log.rotation_euler = Euler((random_angle(index, 0.10), random_angle(index + 4, 0.08), math.radians(-18 + index * 7)), "XYZ")
        parts.append(log)
    for index, x in enumerate((-0.18, 0.18)):
        tie = create_rope_binding(f"wood_tie_{index}", turns=3, radius=0.25, width=0.12)
        assign_material(tie, "rope_fiber_painted")
        tie.rotation_euler = Euler((0.0, math.radians(90), 0.0), "XYZ")
        tie.location.x = x
        parts.append(tie)
    obj = join_objects(parts, "wood_stylized")
    return _finalize_asset(obj, ["wood_bark_painted", "wood_cut_painted"], origin_mode="base")


def generate_item_stone() -> bpy.types.Object:
    parts = []
    for index, radius in enumerate((0.32, 0.26, 0.22, 0.18)):
        rock = create_rock_chunk(f"stone_stylized_{index:02d}", radius=radius)
        rock.location = Vector((index * 0.18 - 0.22, ((index % 2) * 0.18) - 0.06, 0.0))
        parts.append(rock)
    obj = join_objects(parts, "stone_stylized")
    return _finalize_asset(obj, ["stone_painted"], origin_mode="base")


def generate_item_fiber() -> bpy.types.Object:
    bundle = create_fiber_bundle("fiber_stylized", strand_count=18)
    binding = create_rope_binding("fiber_binding", turns=2, radius=0.06, width=0.22)
    binding.rotation_euler = Euler((0.0, math.radians(90), 0.0), "XYZ")
    binding.location = Vector((0.04, 0.0, 0.0))
    obj = join_objects([bundle, binding], "fiber_stylized")
    return _finalize_asset(obj, ["rope_fiber_painted"], origin_mode="base")


def generate_item_grass() -> bpy.types.Object:
    grass = create_grass_clump("grass_stylized", blade_count=16, height=0.72)
    pebbles = [create_rock_chunk(f"grass_pebble_{index}", radius=0.05) for index in range(2)]
    for index, pebble in enumerate(pebbles):
        pebble.location = Vector((index * 0.12 - 0.05, 0.1 * index, 0.0))
    obj = join_objects([grass, *pebbles], "grass_stylized")
    return _finalize_asset(obj, ["leaf_green_painted", "stone_painted"], origin_mode="base")


def generate_item_meat() -> bpy.types.Object:
    meat = create_rock_chunk("meat_core", radius=0.36, roughness=0.08)
    meat.scale = Vector((1.25, 0.88, 0.55))
    fat = create_hide_sheet("meat_fat", width=0.58, depth=0.42)
    fat.location = Vector((0.03, -0.04, 0.08))
    obj = join_objects([meat, fat], "meat_stylized")
    return _finalize_asset(obj, ["meat_painted", "fat_painted"], origin_mode="base")


def generate_item_hide() -> bpy.types.Object:
    obj = create_hide_sheet("hide_stylized", width=1.2, depth=1.0)
    return _finalize_asset(obj, ["hide_painted"], origin_mode="base")


def generate_item_bone() -> bpy.types.Object:
    obj = create_bone_piece("bone_stylized")
    return _finalize_asset(obj, ["bone_painted"], origin_mode="base")


def generate_item_berries() -> bpy.types.Object:
    berries = create_berry_cluster("berries_core", count=9)
    leaves = create_leaf_cluster("berries_leaves", count=5, size=0.18)
    obj = join_objects([berries, leaves], "berries_stylized")
    return _finalize_asset(obj, ["berry_dark_painted", "leaf_green_painted"], origin_mode="base")


def generate_item_torch() -> bpy.types.Object:
    shaft = create_branch_segment("torch_shaft", length=1.8, radius_start=0.08, radius_end=0.05, bend=0.06)
    wrap = create_rope_binding("torch_wrap", turns=5, radius=0.09, width=0.28)
    wrap.rotation_euler = Euler((0.0, math.radians(90), 0.0), "XYZ")
    wrap.location = Vector((0.68, 0.0, 0.0))
    flame = create_flame_mesh("torch_flame", height=0.72, width=0.25)
    flame.location = Vector((0.92, 0.0, 0.16))
    obj = join_objects([shaft, wrap, flame], "torch_stylized")
    obj.rotation_euler = Euler((0.0, math.radians(90), math.radians(-18)), "XYZ")
    return _finalize_asset(obj, ["wood_bark_painted", "rope_fiber_painted", "fire_painted"], origin_mode="base")


def generate_item_spear() -> bpy.types.Object:
    shaft = create_branch_segment("spear_shaft", length=2.55, radius_start=0.075, radius_end=0.052, bend=0.045, cut_bias=0.0)
    assign_material(shaft, "wood_bark_painted")
    tip = create_branch_segment("spear_carved_tip", length=0.34, radius_start=0.046, radius_end=0.002, bend=0.0, cut_bias=0.0)
    tip.location = Vector((1.445, 0.0, 0.0))
    assign_material(tip, "wood_cut_painted")
    binding = create_rope_binding("spear_grip", turns=6, radius=0.078, width=0.30)
    binding.rotation_euler = Euler((0.0, math.radians(90), 0.0), "XYZ")
    binding.location = Vector((-0.32, 0.0, 0.045))
    assign_material(binding, "rope_fiber_painted")
    obj = join_objects([shaft, tip, binding], "spear_stylized")
    obj.rotation_euler = Euler((math.radians(4), math.radians(-7), math.radians(-18)), "XYZ")
    return _finalize_asset(obj, ["wood_bark_painted", "wood_cut_painted", "rope_fiber_painted"], origin_mode="base")


def generate_item_bow() -> bpy.types.Object:
    bow = create_branch_segment("bow_body", length=1.8, radius_start=0.06, radius_end=0.05, bend=0.32)
    for vert in bow.data.vertices:
        if vert.co.x < 0:
            vert.co.y -= 0.14
        else:
            vert.co.y += 0.14
    string = create_rope_binding("bow_string", turns=1, radius=0.01, width=1.65)
    string.rotation_euler = Euler((0.0, 0.0, math.radians(90)), "XYZ")
    grip = create_rope_binding("bow_grip", turns=4, radius=0.05, width=0.18)
    grip.rotation_euler = Euler((0.0, math.radians(90), 0.0), "XYZ")
    obj = join_objects([bow, string, grip], "bow_stylized")
    obj.rotation_euler = Euler((0.0, math.radians(90), math.radians(20)), "XYZ")
    return _finalize_asset(obj, ["wood_bark_painted", "rope_fiber_painted"], origin_mode="base")


def generate_placeable_campfire() -> bpy.types.Object:
    rocks = [create_rock_chunk(f"campfire_rock_{index}", radius=0.18 + (index % 3) * 0.03, roughness=0.08) for index in range(10)]
    for index, rock in enumerate(rocks):
        angle = (index / len(rocks)) * math.tau
        rock.location = Vector((math.cos(angle) * 0.82, math.sin(angle) * 0.72, 0.0))
        rock.scale *= 0.88 + 0.05 * (index % 4)
        assign_material(rock, "stone_painted")
    ash = create_rock_chunk("campfire_ash", radius=0.62, roughness=0.05)
    ash.scale.z = 0.08
    ash.location.z = -0.05
    assign_material(ash, "ash_painted")
    logs = [create_split_log(f"campfire_log_{index}", length=1.0, radius=0.09) for index in range(4)]
    for index, log in enumerate(logs):
        log.rotation_euler = Euler((0.0, math.radians(60 * index), math.radians(15 * index)), "XYZ")
        log.location = Vector((0.0, 0.0, 0.06 + index * 0.02))
        assign_material(log, "wood_bark_painted")
    flames = []
    for index, (height, width) in enumerate(((1.05, 0.34), (0.72, 0.24), (0.48, 0.16))):
        flame = create_flame_mesh(f"campfire_flame_{index}", height=height, width=width)
        flame.location = Vector((0.08 * (index - 1), 0.03 * index, 0.18))
        flame.rotation_euler.z = math.radians(index * 55)
        assign_material(flame, "fire_painted")
        flames.append(flame)
    obj = join_objects([ash, *rocks, *logs, *flames], "campfire_stylized")
    return _finalize_asset(obj, ["stone_painted", "wood_bark_painted", "fire_painted"], origin_mode="base")


def generate_placeable_storage_box() -> bpy.types.Object:
    parts = [
        create_plank("crate_floor", length=1.5, width=0.92, thickness=0.12),
        create_plank("crate_side_a", length=1.5, width=0.18, thickness=0.08),
        create_plank("crate_side_b", length=1.5, width=0.18, thickness=0.08),
        create_plank("crate_end_a", length=0.92, width=0.18, thickness=0.08),
        create_plank("crate_end_b", length=0.92, width=0.18, thickness=0.08),
    ]
    parts[1].location = Vector((0.0, -0.38, 0.28))
    parts[2].location = Vector((0.0, 0.38, 0.34))
    parts[3].rotation_euler = Euler((0.0, 0.0, math.radians(90)), "XYZ")
    parts[4].rotation_euler = Euler((0.0, 0.0, math.radians(90)), "XYZ")
    parts[3].location = Vector((-0.68, 0.0, 0.28))
    parts[4].location = Vector((0.68, 0.0, 0.28))
    braces = [create_branch_segment(f"crate_brace_{i}", length=0.86, radius_start=0.05, radius_end=0.04, bend=0.02) for i in range(2)]
    for index, brace in enumerate(braces):
        brace.rotation_euler = Euler((0.0, math.radians(90), 0.0), "XYZ")
        brace.location = Vector(((-0.62 if index == 0 else 0.62), 0.0, 0.44))
    obj = join_objects([*parts, *braces], "storage_box_stylized")
    return _finalize_asset(obj, ["wood_cut_painted", "wood_bark_painted"], origin_mode="base")


def generate_placeable_tent() -> bpy.types.Object:
    poles = [create_branch_segment(f"tent_pole_{i}", length=2.0, radius_start=0.06, radius_end=0.04, bend=0.03) for i in range(3)]
    poles[0].rotation_euler = Euler((0.0, math.radians(25), math.radians(90)), "XYZ")
    poles[1].rotation_euler = Euler((0.0, math.radians(-25), math.radians(90)), "XYZ")
    poles[2].rotation_euler = Euler((math.radians(90), 0.0, 0.0), "XYZ")
    poles[0].location = Vector((-0.55, 0.0, 0.72))
    poles[1].location = Vector((0.55, 0.0, 0.72))
    poles[2].location = Vector((0.0, -0.88, 1.28))
    cover = create_hide_sheet("tent_cover", width=2.4, depth=2.0)
    cover.rotation_euler = Euler((math.radians(68), 0.0, 0.0), "XYZ")
    cover.location = Vector((0.0, -0.12, 0.82))
    binding_a = create_rope_binding("tent_binding_a", turns=3, radius=0.05, width=0.18)
    binding_b = create_rope_binding("tent_binding_b", turns=3, radius=0.05, width=0.18)
    binding_a.location = Vector((-0.55, 0.0, 1.34))
    binding_b.location = Vector((0.55, 0.0, 1.34))
    obj = join_objects([*poles, cover, binding_a, binding_b], "tent_stylized")
    return _finalize_asset(obj, ["wood_bark_painted", "hide_painted", "rope_fiber_painted"], origin_mode="base")


def generate_placeable_wall() -> bpy.types.Object:
    pales = []
    for index in range(8):
        pale = create_branch_segment(
            f"wall_pale_{index}",
            length=1.9 + (index % 3) * 0.18,
            radius_start=0.09,
            radius_end=0.035,
            bend=0.02,
        )
        pale.rotation_euler = Euler((0.0, math.radians(90), 0.0), "XYZ")
        pale.location = Vector((index * 0.28 - 0.98, 0.0, 0.9))
        pales.append(pale)
    rails = [create_branch_segment(f"wall_rail_{i}", length=2.6, radius_start=0.05, radius_end=0.04, bend=0.02) for i in range(2)]
    rails[0].location = Vector((0.0, -0.14, 0.72))
    rails[1].location = Vector((0.0, 0.14, 1.18))
    bindings = [create_rope_binding(f"wall_binding_{i}", turns=3, radius=0.04, width=0.12) for i in range(4)]
    for index, binding in enumerate(bindings):
        binding.location = Vector((index * 0.56 - 0.84, 0.0, 0.84 + (index % 2) * 0.38))
    obj = join_objects([*pales, *rails, *bindings], "wall_stylized")
    return _finalize_asset(obj, ["wood_bark_painted", "rope_fiber_painted"], origin_mode="base")


def generate_placeable_trap() -> bpy.types.Object:
    frame = [create_branch_segment(f"trap_frame_{i}", length=1.8, radius_start=0.06, radius_end=0.04, bend=0.02) for i in range(4)]
    frame[0].rotation_euler = Euler((0.0, math.radians(90), math.radians(12)), "XYZ")
    frame[1].rotation_euler = Euler((0.0, math.radians(90), math.radians(-12)), "XYZ")
    frame[2].rotation_euler = Euler((0.0, math.radians(90), 0.0), "XYZ")
    frame[3].rotation_euler = Euler((0.0, math.radians(90), 0.0), "XYZ")
    frame[0].location = Vector((0.0, -0.26, 0.2))
    frame[1].location = Vector((0.0, 0.26, 0.2))
    frame[2].location = Vector((-0.72, 0.0, 0.1))
    frame[3].location = Vector((0.72, 0.0, 0.1))
    spikes = [create_branch_segment(f"trap_spike_{i}", length=0.78, radius_start=0.05, radius_end=0.018, bend=0.01) for i in range(6)]
    for index, spike in enumerate(spikes):
        spike.rotation_euler = Euler((0.0, math.radians(70), math.radians(index * 12 - 26)), "XYZ")
        spike.location = Vector((index * 0.26 - 0.62, -0.08 + (index % 2) * 0.16, 0.28))
    trigger = create_rope_binding("trap_trigger", turns=1, radius=0.18, width=0.04)
    trigger.location = Vector((0.0, 0.0, 0.58))
    obj = join_objects([*frame, *spikes, trigger], "trap_stylized")
    return _finalize_asset(obj, ["wood_bark_painted", "rope_fiber_painted"], origin_mode="base")


def generate_resource_conifer_tree() -> bpy.types.Object:
    """Tall conifer tree - stożkowy, z dużymi warstwami igliwia"""
    # 1. GŁÓWNY PIEŃ (pionowo stojący) - WIDOCZNY
    trunk = create_branch_segment("conifer_trunk", length=6.0, radius_start=0.40, radius_end=0.18, bend=0.03)
    trunk.location = Vector((0.0, 0.0, 0.0))
    trunk.rotation_euler = Euler((0, math.radians(90), 0), "XYZ")  # Pionowy!
    
    parts = [trunk]
    
    # 2. WARSTWY IGLIWIA (5 warstw z wieloma małymi rock chunks zamiast dużych brył)
    for layer_idx in range(5):
        layer_height = 2.5 + layer_idx * 0.9
        layer_width = 3.8 - layer_idx * 0.5
        
        # Wiele małych chunks w warstwie tworzy masę
        chunks_per_layer = 5 + layer_idx * 2  # Więcej w dolnych warstwach
        for chunk_idx in range(chunks_per_layer):
            angle = (chunk_idx / chunks_per_layer) * 6.28 + random_angle(layer_idx + chunk_idx, 0.4)
            radius = layer_width * (0.4 + random_angle(chunk_idx, 0.2))
            
            chunk = create_rock_chunk(f"conifer_layer{layer_idx}_{chunk_idx}", radius=0.5, roughness=0.3)
            chunk.location = Vector((
                math.cos(angle) * radius,
                math.sin(angle) * radius,
                layer_height + random_angle(chunk_idx + layer_idx, 0.3)
            ))
            chunk.scale = Vector((0.9 + random_angle(chunk_idx + 1, 0.15), 0.9 + random_angle(chunk_idx + 2, 0.15), 0.85))
            parts.append(chunk)
    
    # 3. CZUBEK (mały stożkowaty rock chunk na górze)
    top_cluster = create_rock_chunk(f"conifer_top", radius=0.6, roughness=0.3)
    top_cluster.location = Vector((0.0, 0.0, 7.0))
    parts.append(top_cluster)
    
    obj = join_objects(parts, "conifer_tree_stylized")
    return _finalize_asset(obj, ["wood_bark_painted", "leaf_green_painted"], origin_mode="base")


def generate_resource_leafy_tree() -> bpy.types.Object:
    """Broad deciduous tree - liściaste, z grubym pniem i naturalnymi gałęziami"""
    # 1. GŁÓWNY PIEŃ (grubszy, brązowy) - WIDOCZNY I SOLIDNY - PIONOWY
    trunk = create_branch_segment("leafy_trunk", length=3.8, radius_start=0.45, radius_end=0.25, bend=0.06)
    trunk.location = Vector((0.0, 0.0, 0.0))
    trunk.rotation_euler = Euler((0, math.radians(90), 0), "XYZ")  # Obrót aby pień był pionowy!
    
    parts = [trunk]
    
    # 2. KORZENIE (3-5 krótkich korzeni przy podstawie) - naturalnie osadzone
    for root_idx in range(4):
        root = create_branch_segment(f"leafy_root_{root_idx}", length=0.9, radius_start=0.12, radius_end=0.05, bend=0.15)
        angle = (root_idx / 4) * 6.28
        root.location = Vector((math.cos(angle) * 0.25, math.sin(angle) * 0.25, 0.0))
        # Korzenie wychodzą na boki od pnia
        root.rotation_euler = Euler((
            random_angle(root_idx, 0.3),
            angle,
            random_angle(root_idx + 2, 0.3)
        ), "XYZ")
        parts.append(root)
    
    # 3. GŁÓWNE KONARY (5-7 naturalnych gałęzi wychodz. z pnia)
    # Każdy konar to grubsza gałąź, która podpiera liście
    branches = []
    for branch_idx in range(6):
        branch = create_branch_segment(
            f"leafy_branch_{branch_idx}",
            length=1.8,
            radius_start=0.20,
            radius_end=0.09,
            bend=0.14
        )
        
        branch_height = 2.1 + (branch_idx % 3) * 0.5
        angle = (branch_idx / 6) * 6.28 + random_angle(branch_idx, 0.3)
        
        branch.location = Vector((0.0, 0.0, branch_height))
        branch.rotation_euler = Euler((
            random_angle(branch_idx, 0.7),
            angle,
            branch_idx * 1.0
        ), "XYZ")
        parts.append(branch)
        branches.append((branch_idx, angle, branch_height))
    
    # 4. LIŚCIE NA KONARACH - dla każdego konaru: large + medium + small clusters
    # Tworzy naturalną koronę rozproszoną wokół konarów
    
    # Duże kępy liści (bezpośrednio na końcach konarów)
    for branch_idx, branch_angle, branch_height in branches:
        # Koniec każdego konaru = gdzie leżą liście
        branch_end_x = math.cos(branch_angle) * 1.8
        branch_end_y = math.sin(branch_angle) * 1.8
        branch_end_z = branch_height + 1.8
        
        # Duża główna kępa liści na końcu konaru
        main_cluster = create_leaf_cluster(f"leafy_main_{branch_idx}", count=16, size=1.2)
        main_cluster.location = Vector((branch_end_x, branch_end_y, branch_end_z))
        main_cluster.scale = Vector((1.3, 1.3, 1.0))
        parts.append(main_cluster)
        
        # 2-3 mniejsze kępy wokół głównej (tworzą objętość)
        for side_idx in range(2):
            angle_offset = branch_angle + (side_idx - 0.5) * 1.57
            offset_x = math.cos(angle_offset) * 0.6
            offset_y = math.sin(angle_offset) * 0.6
            
            side_cluster = create_leaf_cluster(f"leafy_side_{branch_idx}_{side_idx}", count=12, size=0.9)
            side_cluster.location = Vector((branch_end_x + offset_x, branch_end_y + offset_y, branch_end_z + random_angle(branch_idx + side_idx, 0.4)))
            parts.append(side_cluster)
    
    # Centralna masa korony (dla objętości) - dodatkowo wypełnia środek
    for crown_idx in range(5):
        central_cluster = create_leaf_cluster(f"leafy_crown_center_{crown_idx}", count=14, size=1.0)
        central_cluster.location = Vector((
            random_angle(crown_idx, 0.8),
            random_angle(crown_idx + 1, 0.8),
            3.5 + random_angle(crown_idx + 2, 0.5)
        ))
        parts.append(central_cluster)
    
    obj = join_objects(parts, "leafy_tree_stylized")
    return _finalize_asset(obj, ["wood_bark_painted", "leaf_green_painted"], origin_mode="base")


def generate_resource_dry_tree() -> bpy.types.Object:
    """Dead/dying tree - martwe, suche gałęzie bez liści"""
    # 1. GŁÓWNY PIEŃ (cieńszy, suchy, szaro-brązowy) - PIONOWY
    trunk = create_branch_segment("dry_trunk", length=5.2, radius_start=0.28, radius_end=0.10, bend=0.10)
    trunk.location = Vector((0.0, 0.0, 0.0))
    trunk.rotation_euler = Euler((0, math.radians(90), 0), "XYZ")  # Pionowy!
    
    parts = [trunk]
    
    # 2. GŁÓWNE SUCHE GAŁĘZIE (8-12 dużych gałęzi wychodzących z różnych wysokości)
    for branch_idx in range(10):
        branch = create_branch_segment(
            f"dry_main_branch_{branch_idx}",
            length=2.0 + random_angle(branch_idx, 0.4),
            radius_start=0.12,
            radius_end=0.03,
            bend=0.18
        )
        
        branch_height = 1.5 + (branch_idx % 5) * 0.8
        angle = (branch_idx / 10) * 6.28 + random_angle(branch_idx + 1, 0.6)
        
        branch.location = Vector((0.0, 0.0, branch_height))
        branch.rotation_euler = Euler((
            random_angle(branch_idx, 1.0),
            angle + random_angle(branch_idx + 2, 0.8),
            branch_idx * 0.7 + random_angle(branch_idx + 3, 0.5)
        ), "XYZ")
        parts.append(branch)
    
    # 3. GAŁĘZIE DRUGIEGO RZĘDU (rozwidlenia)
    for secondary_idx in range(14):
        twig = create_branch_segment(
            f"dry_secondary_{secondary_idx}",
            length=1.2 + random_angle(secondary_idx, 0.3),
            radius_start=0.06,
            radius_end=0.015,
            bend=0.22
        )
        
        # Małe gałęzie wysoko, rozproszone asymetrycznie
        height = 2.5 + random_angle(secondary_idx + 1, 1.5)
        offset_x = random_angle(secondary_idx, 1.0)
        offset_y = random_angle(secondary_idx + 2, 0.8)
        
        twig.location = Vector((offset_x, offset_y, height))
        twig.rotation_euler = Euler((
            random_angle(secondary_idx + 3, 1.2),
            random_angle(secondary_idx + 4, 1.0),
            secondary_idx * 0.4 + random_angle(secondary_idx + 5, 0.7)
        ), "XYZ")
        parts.append(twig)
    
    obj = join_objects(parts, "dry_tree_stylized")
    return _finalize_asset(obj, ["wood_bark_painted", "leaf_dry_painted"], origin_mode="base")


def generate_resource_rock() -> bpy.types.Object:
    main = create_rock_chunk("resource_rock_main", radius=0.9, roughness=0.12)
    supports = [create_rock_chunk(f"resource_rock_support_{i}", radius=0.24 + i * 0.03, roughness=0.08) for i in range(4)]
    for index, rock in enumerate(supports):
        rock.location = Vector((random_angle(index, 0.65), random_angle(index + 1, 0.55), 0.0))
    obj = join_objects([main, *supports], "rock_stylized")
    return _finalize_asset(obj, ["stone_painted"], origin_mode="base")


def generate_resource_green_bush() -> bpy.types.Object:
    clusters = [create_leaf_cluster(f"green_bush_cluster_{i}", count=10, size=0.42) for i in range(6)]
    for index, cluster in enumerate(clusters):
        cluster.location = Vector((random_angle(index, 0.38), random_angle(index + 2, 0.3), 0.28 + (index % 3) * 0.16))
    obj = join_objects(clusters, "green_bush_stylized")
    return _finalize_asset(obj, ["leaf_green_painted"], origin_mode="base")


def generate_resource_dry_bush() -> bpy.types.Object:
    twigs = [create_branch_segment(f"dry_bush_twig_{i}", length=0.95, radius_start=0.03, radius_end=0.015, bend=0.08) for i in range(10)]
    for index, twig in enumerate(twigs):
        twig.rotation_euler = Euler((random_angle(index, 1.0), random_angle(index + 1, 0.7), index * 0.55), "XYZ")
        twig.location = Vector((random_angle(index, 0.18), random_angle(index + 4, 0.18), 0.12))
    obj = join_objects(twigs, "dry_bush_stylized")
    return _finalize_asset(obj, ["leaf_dry_painted"], origin_mode="base")


def generate_resource_grass_or_flower() -> bpy.types.Object:
    grass = create_grass_clump("flower_grass", blade_count=18, height=0.82)
    flowers = [create_berry_cluster(f"flower_cluster_{i}", count=1, radius=0.06, spread=0.0) for i in range(3)]
    for index, flower in enumerate(flowers):
        flower.location = Vector((index * 0.16 - 0.14, 0.0, 0.62 + index * 0.06))
    obj = join_objects([grass, *flowers], "grass_or_flower_stylized")
    return _finalize_asset(obj, ["leaf_green_painted", "berry_red_painted"], origin_mode="base")


def generate_resource_berry_bush() -> bpy.types.Object:
    bush = generate_resource_green_bush()
    berries = create_berry_cluster("berry_bush_fruit", count=12, radius=0.07, spread=0.46)
    berries.location = Vector((0.0, 0.0, 0.34))
    obj = join_objects([bush, berries], "berry_bush_stylized")
    return _finalize_asset(obj, ["leaf_green_painted", "berry_red_painted"], origin_mode="base")


GENERATOR_MAP: dict[str, tuple[str, Callable[[], bpy.types.Object], str, str]] = {
    "wood": ("item", generate_item_wood, "Stylized wood bundle with rope tie.", "Inventory pickup / world item."),
    "stone": ("item", generate_item_stone, "Stone pickup cluster.", "Inventory pickup / crafting ingredient."),
    "fiber": ("item", generate_item_fiber, "Fiber bundle with visible strands.", "Inventory pickup / crafting ingredient."),
    "grass": ("item", generate_item_grass, "Readable grass pickup.", "Inventory pickup / crafting ingredient."),
    "meat": ("item", generate_item_meat, "Stylized raw meat chunk.", "Loot drop / consumable pickup."),
    "hide": ("item", generate_item_hide, "Folded hide sheet.", "Loot drop / crafting ingredient."),
    "bone": ("item", generate_item_bone, "Stylized bone pickup.", "Loot drop / crafting ingredient."),
    "berries": ("item", generate_item_berries, "Berry cluster pickup.", "Food pickup / forage item."),
    "torch": ("item", generate_item_torch, "Torch with painted flame preview.", "Held item / placement preview."),
    "spear": ("item", generate_item_spear, "Bushcraft spear with stone head.", "Weapon / held item."),
    "bow": ("item", generate_item_bow, "Handmade bow with wrapped grip.", "Weapon / held item."),
    "campfire": ("placeable", generate_placeable_campfire, "Campfire with stone ring and stylized flame.", "BuildingPrefabEntry / base camp."),
    "storage_box": ("placeable", generate_placeable_storage_box, "Handmade open crate.", "BuildingPrefabEntry / storage."),
    "tent": ("placeable", generate_placeable_tent, "Bushcraft shelter with hide cover.", "BuildingPrefabEntry / shelter."),
    "wall": ("placeable", generate_placeable_wall, "Uneven palisade wall with bindings.", "BuildingPrefabEntry / defense."),
    "trap": ("placeable", generate_placeable_trap, "Handmade trap with visible trigger ring.", "BuildingPrefabEntry / defense / hunting."),
    "conifer_tree": ("resource", generate_resource_conifer_tree, "Layered conifer silhouette.", "ResourcePrefabEntry / world resource."),
    "leafy_tree": ("resource", generate_resource_leafy_tree, "Broadleaf tree with crown breakup.", "ResourcePrefabEntry / world resource."),
    "dry_tree": ("resource", generate_resource_dry_tree, "Dead tree silhouette.", "ResourcePrefabEntry / world resource."),
    "rock": ("resource", generate_resource_rock, "Large stylized resource rock.", "ResourcePrefabEntry / world resource."),
    "green_bush": ("resource", generate_resource_green_bush, "Dense green bush.", "ResourcePrefabEntry / gatherable foliage."),
    "dry_bush": ("resource", generate_resource_dry_bush, "Dry brittle bush.", "ResourcePrefabEntry / gatherable foliage."),
    "grass_or_flower": ("resource", generate_resource_grass_or_flower, "Grass tuft with flowers.", "ResourcePrefabEntry / world dressing."),
    "berry_bush": ("resource", generate_resource_berry_bush, "Bush with visible fruit clusters.", "ResourcePrefabEntry / gatherable food."),
}


def export_asset(asset_id: str, category: str) -> dict[str, str]:
    ensure_export_directories()
    name = _asset_name(asset_id)
    base_dir = _category_dir(category) / "Models"
    obj_path = base_dir / f"{name}.obj"
    fbx_path = base_dir / f"{name}.fbx"
    bpy.ops.object.select_all(action="DESELECT")
    for obj in bpy.context.selected_objects:
        obj.select_set(False)
    for obj in bpy.data.objects:
        if obj.select_get() is False and obj.name == f"{name}":
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            break
    obj_export = getattr(bpy.ops.wm, "obj_export", None)
    if obj_export is not None:
        obj_export(filepath=str(obj_path), export_selected_objects=True, forward_axis="NEGATIVE_Z", up_axis="Y")
    fbx_export = getattr(bpy.ops.export_scene, "fbx", None)
    if fbx_export is not None:
        fbx_export(filepath=str(fbx_path), use_selection=True, axis_forward="-Z", axis_up="Y", bake_space_transform=False)
    return {"obj": str(obj_path.as_posix()), "fbx": str(fbx_path.as_posix())}


def save_blend_copy(asset_id: str) -> str:
    ensure_export_directories()
    path = BLEND_ROOT / f"{_asset_name(asset_id)}.blend"
    
    # Clean scene before saving - remove everything except the asset (keep camera, light)
    asset_name = _asset_name(asset_id)
    
    # Select all objects
    bpy.ops.object.select_all(action='SELECT')
    
    # Deselect the asset we want to keep + camera + light
    asset_obj = bpy.data.objects.get(asset_name)
    if asset_obj:
        asset_obj.select_set(False)
    
    # Keep camera and lights
    for obj in bpy.data.objects:
        if obj.type in ('CAMERA', 'LIGHT'):
            obj.select_set(False)
    
    # Delete all other selected objects (junk like default cube, etc)
    bpy.ops.object.delete(use_global=False)
    
    # Select and focus on the asset
    if asset_obj:
        asset_obj.select_set(True)
        bpy.context.view_layer.objects.active = asset_obj
    
    # Save
    bpy.ops.wm.save_as_mainfile(filepath=str(path), copy=True)
    return str(path.as_posix())


def render_preview(asset_id: str) -> str:
    ensure_export_directories()
    category = GENERATOR_MAP[asset_id][0]
    path = _category_dir(category) / "Models" / f"{_asset_name(asset_id)}_preview.png"
    scene = bpy.context.scene
    asset = bpy.data.objects.get(_asset_name(asset_id))
    if asset is None:
        raise ValueError(f"Missing preview object: {asset_id}")
    scene.render.engine = "BLENDER_EEVEE"
    scene.world.color = (0.78, 0.70, 0.56)
    center = asset.location + Vector((0.0, 0.0, asset.dimensions.z * 0.42))
    camera = bpy.data.objects.get("Camera")
    if camera is None:
        camera_data = bpy.data.cameras.new("Camera")
        camera = bpy.data.objects.new("Camera", camera_data)
        scene.collection.objects.link(camera)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = max(asset.dimensions.x, asset.dimensions.y, asset.dimensions.z) * 1.55
    camera.location = center + Vector((4.8, -6.2, 5.2))
    camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = camera
    light = bpy.data.objects.get("BushcraftKey")
    if light is None:
        light_data = bpy.data.lights.new("BushcraftKey", "AREA")
        light = bpy.data.objects.new("BushcraftKey", light_data)
        scene.collection.objects.link(light)
    light.data.energy = 1100
    light.data.color = (1.0, 0.78, 0.55)
    light.data.shape = "DISK"
    light.data.size = 5.0
    light.location = center + Vector((-3.0, -4.0, 6.0))
    light.rotation_euler = (center - light.location).to_track_quat("-Z", "Y").to_euler()
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.render.filepath = str(path)
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.film_transparent = False
    bpy.ops.render.render(write_still=True)
    return str(path.as_posix())


def generate_manifest(records: list[AssetRecord] | None = None) -> dict:
    manifest = {
        "project": "Apex Shift",
        "style": "hand-painted bushcraft stylized",
        "generator": "Tools/Blender/bushcraft_asset_generator.py",
        "assets": [record.__dict__ for record in (records or [])],
    }
    path = ASSET_ROOT / "bushcraft_model_manifest.json"
    path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return manifest


def generate_all_assets(asset_ids: list[str] | None = None) -> list[AssetRecord]:
    ensure_export_directories()
    create_bushcraft_materials()
    records: list[AssetRecord] = []
    for asset_id in asset_ids or list(GENERATOR_MAP.keys()):
        category, generator, notes, unity_usage = GENERATOR_MAP[asset_id]
        collection = ensure_collection("BushcraftGenerated")
        bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children[collection.name]
        _clear_scene_for_asset()
        obj = generator()
        obj.name = _asset_name(asset_id)
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        export_asset(asset_id, category)
        save_blend_copy(asset_id)
        preview_path = render_preview(asset_id)
        record = _record(asset_id, category, obj, notes, unity_usage)
        record.preview_path = preview_path
        records.append(record)
    generate_manifest(records)
    return records


def _premium_reference_assets() -> list[str]:
    return ["wood", "spear", "campfire"]


if __name__ == "__main__":
    cleanup_generated_collection("BushcraftGenerated")
    generate_all_assets(_premium_reference_assets())
