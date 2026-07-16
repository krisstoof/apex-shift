from __future__ import annotations

"""
Apex Shift Bushcraft Asset Generator v7 - detailed realism pass
================================================================

Cel
---
Ta wersja jest przygotowana jako "bardzo szczegółowy generator" dla projektu
Apex Shift. Nie próbuje iść w low-poly. Generator ma budować czytelne, bardziej
realistyczne, ręcznie-rzemieślnicze assety do świata bushcraft / survival.

Najważniejsze założenia artystyczne
-----------------------------------
1. Każdy model musi mieć WIARYGODNĄ MORFOLOGIĘ z realnego świata.
   - drewno = faktyczne polana / gałęzie z korą, słojami, nierówną średnicą,
   - kość = jedna kość długa, z trzonem i poszerzonymi nasadami,
   - jagody = owoce na łodyżce / gałązce, nie oderwane kulki,
   - krzaki = szkielet pędów + masy liści + owoce osadzone na pędach,
   - broń / narzędzia = kamienne głownie / ostrza z owijaniem i sensowną geometrią,
   - konstrukcje = logika konstrukcyjna (podpory, rygle, wiązania, ciężar, poszycie).

2. Styl ma być "hand-painted realism":
   - realistyczna sylwetka,
   - lekkie uproszczenie form pod grę,
   - ale absolutnie bez klockowatości placeholderów.

3. Asset musi czytać się w widoku izometrycznym.
   - wyraźna bryła główna,
   - czytelne proporcje,
   - zróżnicowane materiały,
   - brak przypadkowych oderwanych elementów.

4. Wszystkie ważne prefaby świata otrzymują override.
   Obejmuje to:
   - surowce / itemy,
   - narzędzia,
   - placeables,
   - roślinność,
   - landmarki,
   - stworzenia.

Uruchomienie
------------
    blender --background --python bushcraft_asset_generator_v7_detailed.py

Wymagania
---------
- Plik trzymaj obok:
  - bushcraft_asset_generator_v4_master.py
  - bushcraft_asset_generator_v6_research_grounded.py
- Wynik zostanie zapisany do:
  ApexShift_Bushcraft_Output_v7_Detailed

Uwagi
-----
To jest rozszerzenie v6, ale z mocniejszym naciskiem na:
- poprawę problematycznych assetów (wood, bone, berries, berry bushes, trap,
  axe, spear, tent),
- większą ilość warstwowej geometrii,
- bardziej wiarygodne relacje materiałowe,
- lepszy „real-world read”.
"""

import importlib.util
import math
import os
import random
from pathlib import Path
from typing import Dict, List, Sequence, Tuple

import bpy
from mathutils import Euler, Vector

# -----------------------------------------------------------------------------
# Load v6 as foundation
# -----------------------------------------------------------------------------

BASE_PATH = Path(__file__).with_name('bushcraft_asset_generator_v6_research_grounded.py')
spec = importlib.util.spec_from_file_location('apex_bushcraft_v6_base', str(BASE_PATH))
V = importlib.util.module_from_spec(spec)
import sys
sys.modules[spec.name] = V
spec.loader.exec_module(V)

# v6 internally uses module alias B for the v4 master; keep that as our base API.
B = V.B

# -----------------------------------------------------------------------------
# Output paths
# -----------------------------------------------------------------------------

B.OUTPUT_ROOT = r'C:/Users/kriss/apex-shift/Assets/_Project/Art/Bushcraft'
B.DOCS_ROOT = os.path.join(B.OUTPUT_ROOT, 'Docs')
B.SOURCE_BLEND_DIR = os.path.join(B.OUTPUT_ROOT, 'Source', 'Blend')
B.PREVIEW_DIR = os.path.join(B.OUTPUT_ROOT, 'Previews')
B.ITEM_MODEL_DIR = os.path.join(B.OUTPUT_ROOT, 'Items', 'Models')
B.PLACEABLE_MODEL_DIR = os.path.join(B.OUTPUT_ROOT, 'Placeables', 'Models')
B.RESOURCE_MODEL_DIR = os.path.join(B.OUTPUT_ROOT, 'Resources', 'Models')
B.CREATURE_MODEL_DIR = os.path.join(B.OUTPUT_ROOT, 'Creatures', 'Models')
B.LANDMARK_MODEL_DIR = os.path.join(B.OUTPUT_ROOT, 'Landmarks', 'Models')
B.MANIFEST_PATH = os.path.join(B.OUTPUT_ROOT, 'bushcraft_v7_manifest.json')
B.CATALOG_PATH = os.path.join(B.OUTPUT_ROOT, 'bushcraft_v7_catalog.md')

random.seed(B.SEED + 707)

# -----------------------------------------------------------------------------
# Design brief used directly by the generator
# -----------------------------------------------------------------------------

REFERENCE_SPEC: Dict[str, str] = {
    'wood': 'Bundle of short firewood logs with bark, end grain, knots, slightly different diameters, tied with fiber.',
    'bone': 'Single realistic long bone with widened epiphyses, narrow shaft, mild asymmetry, subtle wear and porous surface feel.',
    'berries_pickup': 'Harvest pickup should look like a real berry sprig: thin twig, attached berry cluster, leaves with shape and slight curl.',
    'berry_bush': 'Shrub must have many thin woody stems from base, layered leaf masses, fruit clusters attached on branch tips and internodes.',
    'green_bush': 'Dense bush with visible branch skeleton at core and layered leaf volumes on top and outer edges.',
    'dry_bush': 'Airy dry shrub with many twig branches emerging from root crown, varied angles, sparse seed pods, no random starburst.',
    'grass_flower': 'Patch of grass blades with mixed wildflowers: petals, stems, seed heads, height variation, natural clumping.',
    'axe': 'Stone axe with split-haft or side-haft read, asymmetrical chipped head, wedge/lashing, wood handle taper and hand grip.',
    'spear': 'Stone spear with flaked leaf-head or triangular head, wrapped lashing, straight shaft with mild organic taper.',
    'trap': 'Figure-4 / deadfall inspired trap: weight, support, diagonal trigger, bait peg. Mechanism should read clearly.',
    'wall': 'Palisade made of sharpened irregular stakes, rails lashed behind, slight tilt and height variation.',
    'tent': 'A-frame bushcraft shelter with forked uprights, ridgepole, rafters, bark/hide overlap, rear closure and leaf/fur bedding.',
    'storage': 'Handmade crate/chest from rough planks, corner reinforcement, lid logic, lashings or pegs.',
    'campfire': 'Stone ring with charcoal, staged wood pieces, ash, ember bed, readable flame shape.',
    'trees': 'Mature and juvenile trees need trunk flare, branch hierarchy, crown logic; conifers in tiered layers.',
    'creatures': 'Animals need real quadruped proportions and readable silhouettes, not blob proxies.'
}

# -----------------------------------------------------------------------------
# Materials - add a few more for painterly realism
# -----------------------------------------------------------------------------

_prev_create_material_library = B.create_material_library


def create_material_library_v7():
    _prev_create_material_library()
    extra = {
        'wood_bark_dark': ((0.24, 0.16, 0.10, 1.0), 0.90),
        'wood_bark_mid': ((0.34, 0.23, 0.14, 1.0), 0.88),
        'wood_fresh': ((0.70, 0.56, 0.36, 1.0), 0.80),
        'lichen': ((0.58, 0.63, 0.47, 1.0), 0.95),
        'twig_dark': ((0.22, 0.13, 0.08, 1.0), 0.90),
        'twig_light': ((0.38, 0.25, 0.14, 1.0), 0.88),
        'leaf_deep': ((0.26, 0.37, 0.18, 1.0), 0.78),
        'leaf_sun': ((0.56, 0.66, 0.28, 1.0), 0.76),
        'berry_blue': ((0.21, 0.24, 0.46, 1.0), 0.55),
        'berry_red_fresh': ((0.64, 0.16, 0.15, 1.0), 0.52),
        'berry_black': ((0.12, 0.10, 0.16, 1.0), 0.62),
        'bone_old': ((0.83, 0.78, 0.68, 1.0), 0.90),
        'stone_flint': ((0.41, 0.42, 0.44, 1.0), 0.92),
        'stone_basal': ((0.53, 0.53, 0.51, 1.0), 0.95),
    }
    for name, (color, rough) in extra.items():
        if name not in B.MATS:
            B.MATS[name] = B.make_material(name, color, roughness=rough)

B.create_material_library = create_material_library_v7

# -----------------------------------------------------------------------------
# Helper functions
# -----------------------------------------------------------------------------


def organic_rock(name: str, radius=0.2, location=(0, 0, 0), scale=(1, 1, 1), material_name='stone_flint'):
    obj = B.irregular_rock(name, radius=radius, location=location, scale=scale, material_name=material_name)
    return obj


def leaf_card(name: str, location=(0, 0, 0), rotation=(0, 0, 0), scale=(0.05, 0.12, 1.0), material_name='leaf_mid'):
    obj = B.primitive_plane(name, size=1.0, location=location, rotation=rotation, material=B.mat(material_name))
    obj.scale = scale
    return obj


def twig_between(name: str, start, end, r0=0.010, r1=0.003, material_name='twig_dark'):
    return V.branch_between(name, start, end, r0, r1, material_name)


def log_segment(name: str, length=0.7, radius=0.08, location=(0, 0, 0), rotation=(0, 0, 0), bark_material='wood_bark_mid'):
    """Short log with bark and visible cut ends."""
    obj = B.chopped_log(name, length=length, radius=radius, location=location, rotation=rotation, material_name=bark_material)
    return obj


def rope_wrap(prefix: str, location=(0, 0, 0), axis='Z', turns=5, width=0.16, radius=0.009):
    return V.lashing_bundle(prefix, location=location, axis=axis, turns=turns, width=width, radius=radius)


def layered_leaf_mass(prefix: str, center=(0, 0, 0), radius=0.45, puff_count=20, card_count=24,
                      palette=('leaf_sun', 'leaf_light', 'leaf_mid', 'leaf_olive', 'leaf_deep')):
    objs: List[bpy.types.Object] = []
    # Volumetric cores
    for i in range(puff_count):
        ang = random.uniform(0, math.tau)
        dist = random.uniform(0.0, radius)
        z = random.uniform(-radius * 0.22, radius * 0.30)
        puff = V.organic_sphere(
            f'{prefix}_puff_{i}',
            center=(center[0] + math.cos(ang) * dist, center[1] + math.sin(ang) * dist, center[2] + z),
            radius=random.uniform(radius * 0.10, radius * 0.21),
            material_name=random.choice(palette),
            scale=(random.uniform(0.9, 1.25), random.uniform(0.85, 1.2), random.uniform(0.75, 1.05)),
        )
        objs.append(puff)
    # Outer readable leaves
    for i in range(card_count):
        ang = random.uniform(0, math.tau)
        dist = random.uniform(radius * 0.25, radius * 0.95)
        z = random.uniform(-radius * 0.18, radius * 0.18)
        obj = leaf_card(
            f'{prefix}_leaf_{i}',
            location=(center[0] + math.cos(ang) * dist, center[1] + math.sin(ang) * dist, center[2] + z),
            rotation=(
                math.radians(random.uniform(55, 92)),
                math.radians(random.uniform(-20, 20)),
                ang + random.uniform(-0.35, 0.35),
            ),
            scale=(random.uniform(0.028, 0.06), random.uniform(0.055, 0.13), 1.0),
            material_name=random.choice(palette),
        )
        objs.append(obj)
    return objs


def berry_cluster_attached(prefix: str, anchor=(0, 0, 0), berry_count=6,
                           berry_palette=('berry_red_fresh', 'berry_blue', 'berry_black')):
    objs: List[bpy.types.Object] = []
    for i in range(berry_count):
        ang = i / max(berry_count, 1) * math.tau + random.uniform(-0.30, 0.30)
        length = random.uniform(0.025, 0.07)
        sag = random.uniform(0.015, 0.055)
        end = (
            anchor[0] + math.cos(ang) * length,
            anchor[1] + math.sin(ang) * length,
            anchor[2] - sag,
        )
        objs.append(twig_between(f'{prefix}_pedicel_{i}', anchor, end, 0.0038, 0.0018, 'twig_light'))
        berry = B.primitive_uv_sphere(
            f'{prefix}_berry_{i}', radius=random.uniform(0.020, 0.034),
            location=end, segments=12, rings=8, material=B.mat(random.choice(berry_palette))
        )
        berry.scale = (random.uniform(0.88, 1.05), random.uniform(0.90, 1.06), random.uniform(0.92, 1.10))
        objs.append(berry)
    return objs


def add_ground_cluster(prefix: str, rock_count=6, tuft_count=8, radius=0.5):
    objs: List[bpy.types.Object] = []
    for i in range(rock_count):
        a = random.uniform(0, math.tau)
        d = random.uniform(0.1, radius)
        objs.append(organic_rock(
            f'{prefix}_rock_{i}',
            radius=random.uniform(0.03, 0.08),
            location=(math.cos(a) * d, math.sin(a) * d, random.uniform(0.015, 0.045)),
            scale=(random.uniform(0.8, 1.3), random.uniform(0.8, 1.2), random.uniform(0.6, 1.0)),
            material_name='stone_basal',
        ))
    grass = V.grass_patch(f'{prefix}_grass', blade_count=tuft_count * 3, radius=radius * 0.72, height=0.20, flower_count=0)
    objs.extend(grass)
    return objs


def make_leafy_branch_system(prefix: str, branch_count=8, trunk_points: Sequence[Tuple[float, float, float]] = ((0, 0, 0.15), (0, 0, 0.55))):
    """Creates a base twig scaffold for bushes / saplings."""
    objs: List[bpy.types.Object] = []
    base_root = trunk_points[0]
    trunk_top = trunk_points[-1]
    objs.append(twig_between(f'{prefix}_stem_main', base_root, trunk_top, 0.045, 0.018, 'twig_dark'))
    anchors: List[Tuple[float, float, float]] = []
    for i in range(branch_count):
        t = random.uniform(0.30, 0.95)
        base = Vector(base_root).lerp(Vector(trunk_top), t)
        ang = random.uniform(0, math.tau)
        upward = random.uniform(0.15, 0.45)
        length = random.uniform(0.18, 0.40)
        end = (
            base.x + math.cos(ang) * length,
            base.y + math.sin(ang) * length,
            base.z + upward,
        )
        objs.append(twig_between(f'{prefix}_branch_{i}', tuple(base), end, random.uniform(0.015, 0.024), random.uniform(0.004, 0.008), 'twig_light'))
        anchors.append(end)
    return objs, anchors


# -----------------------------------------------------------------------------
# Resource / item generators - detailed overrides
# -----------------------------------------------------------------------------


def gen_wood_detailed():
    """Firewood bundle: short cut logs with bark and tied fiber, not smooth toy cylinders."""
    objs: List[bpy.types.Object] = []
    logs_data = [
        (-0.12, -0.05, 0.08, 0.76, 0.085, -6),
        (0.06, -0.02, 0.10, 0.70, 0.075, 5),
        (0.14, 0.06, 0.09, 0.68, 0.065, 12),
        (-0.04, 0.09, 0.07, 0.63, 0.060, -10),
        (0.00, 0.01, 0.17, 0.72, 0.072, 3),
        (-0.18, 0.02, 0.15, 0.58, 0.055, -14),
        (0.20, -0.08, 0.14, 0.56, 0.052, 18),
    ]
    for idx, (x, y, z, length, radius, rot) in enumerate(logs_data):
        objs.append(log_segment(
            f'wood_log_{idx}',
            length=length,
            radius=radius,
            location=(x, y, z),
            rotation=(math.radians(90), 0, math.radians(90 + rot)),
            bark_material='wood_bark_dark' if idx % 2 == 0 else 'wood_bark_mid'
        ))
    objs += rope_wrap('wood_bundle_rope_a', location=(-0.16, 0.02, 0.12), axis='X', turns=4, width=0.22, radius=0.012)
    objs += rope_wrap('wood_bundle_rope_b', location=(0.18, 0.00, 0.12), axis='X', turns=4, width=0.22, radius=0.012)
    return objs



def gen_bone_detailed():
    """Single long bone with widened ends and a believable shaft."""
    objs: List[bpy.types.Object] = []
    shaft = B.primitive_cylinder(
        'bone_shaft_v7', radius=0.045, depth=0.82,
        location=(0, 0, 0.15), rotation=(math.radians(90), math.radians(6), math.radians(18)),
        vertices=16, material=B.mat('bone_old')
    )
    shaft.scale = (1.0, 1.08, 1.0)
    B.add_displace(shaft, strength=0.014, scale=2.8)
    B.apply_modifier(shaft, 'Displace')
    objs.append(shaft)
    end_specs = [
        (-0.37, -0.02, 0.12, 0.10, (1.15, 0.85, 0.80)),
        (-0.42, 0.03, 0.16, 0.08, (0.92, 1.10, 0.90)),
        (0.37, -0.03, 0.13, 0.10, (1.10, 0.90, 0.82)),
        (0.43, 0.04, 0.17, 0.08, (0.88, 1.06, 0.92)),
    ]
    for idx, (x, y, z, r, scl) in enumerate(end_specs):
        knob = B.primitive_uv_sphere(f'bone_end_{idx}', radius=r, location=(x, y, z), segments=14, rings=10, material=B.mat('bone_old'))
        knob.scale = scl
        B.add_displace(knob, strength=0.010, scale=3.3)
        B.apply_modifier(knob, 'Displace')
        objs.append(knob)
    return objs



def gen_berries_pickup_detailed():
    """A real berry sprig / pickup, not berries floating near a plane."""
    objs: List[bpy.types.Object] = []
    main_stem = twig_between('berries_pickup_main', (-0.12, -0.03, 0.02), (0.10, 0.02, 0.10), 0.010, 0.0048, 'twig_dark')
    objs.append(main_stem)
    side_a = twig_between('berries_pickup_side_a', (0.01, 0.00, 0.07), (0.07, -0.05, 0.10), 0.005, 0.0025, 'twig_light')
    side_b = twig_between('berries_pickup_side_b', (0.03, 0.01, 0.08), (0.10, 0.05, 0.11), 0.0045, 0.0022, 'twig_light')
    objs += [side_a, side_b]
    objs += berry_cluster_attached('berries_pickup_cluster_a', anchor=(0.10, 0.05, 0.11), berry_count=4)
    objs += berry_cluster_attached('berries_pickup_cluster_b', anchor=(0.07, -0.05, 0.10), berry_count=4)
    leaf_specs = [
        ((-0.04, -0.03, 0.04), (75, 12, 20), (0.07, 0.13)),
        ((0.00, 0.05, 0.06), (70, -10, -24), (0.06, 0.11)),
        ((0.06, -0.01, 0.07), (80, 5, 12), (0.05, 0.10)),
    ]
    for idx, (loc, rot_deg, sc) in enumerate(leaf_specs):
        objs.append(leaf_card(
            f'berries_pickup_leaf_{idx}',
            location=loc,
            rotation=(math.radians(rot_deg[0]), math.radians(rot_deg[1]), math.radians(rot_deg[2])),
            scale=(sc[0], sc[1], 1.0),
            material_name='leaf_light' if idx != 2 else 'leaf_mid'
        ))
    return objs



def gen_stone_axe_detailed():
    """Split-haft stone axe with chip-like stone head and visible lashings."""
    objs: List[bpy.types.Object] = []
    haft = B.tapered_cylinder('axe_haft_v7', 0.038, 0.028, 0.96, location=(0, 0, 0.48), material=B.mat('wood_warm'), vertices=14)
    haft.rotation_euler = Euler((math.radians(0), math.radians(0), math.radians(-4)), 'XYZ')
    objs.append(haft)

    # Split section / top cleft of haft
    split_left = twig_between('axe_split_left', (0.0, -0.016, 0.74), (-0.02, -0.05, 0.90), 0.017, 0.008, 'wood_dark')
    split_right = twig_between('axe_split_right', (0.0, 0.016, 0.74), (0.02, 0.05, 0.90), 0.017, 0.008, 'wood_dark')
    objs += [split_left, split_right]

    head = organic_rock('axe_head_v7', radius=0.18, location=(0.01, 0.00, 0.86), scale=(1.35, 0.55, 0.45), material_name='stone_flint')
    head.rotation_euler = Euler((0, math.radians(88), math.radians(10)), 'XYZ')
    objs.append(head)

    wedge = B.board('axe_wedge_v7', 0.06, 0.02, 0.09, location=(0.0, 0.0, 0.92), rotation=(0, 0, math.radians(10)), material_name='wood_dark')
    objs.append(wedge)
    objs += rope_wrap('axe_wrap_v7_a', location=(0.0, 0.0, 0.80), axis='Z', turns=6, width=0.18, radius=0.011)
    objs += rope_wrap('axe_wrap_v7_b', location=(0.0, 0.0, 0.86), axis='Z', turns=4, width=0.12, radius=0.009)
    return objs



def gen_spear_detailed():
    objs: List[bpy.types.Object] = []
    shaft = B.tapered_cylinder('spear_shaft_v7', 0.028, 0.016, 1.58, location=(0, 0, 0.79), material=B.mat('wood_warm'), vertices=14)
    shaft.rotation_euler = Euler((0, math.radians(0), math.radians(1.5)), 'XYZ')
    objs.append(shaft)
    head = organic_rock('spear_head_v7', radius=0.16, location=(0, 0, 1.49), scale=(0.36, 1.38, 0.20), material_name='stone_flint')
    head.rotation_euler = Euler((0, math.radians(90), math.radians(90)), 'XYZ')
    objs.append(head)
    socket = B.board('spear_socket_v7', 0.065, 0.022, 0.14, location=(0, 0, 1.35), material_name='wood_dark')
    objs.append(socket)
    objs += rope_wrap('spear_wrap_v7', location=(0, 0, 1.29), axis='Z', turns=6, width=0.16, radius=0.009)
    return objs



def gen_trap_detailed():
    """Readable deadfall trap instead of abstract floating sticks."""
    objs: List[bpy.types.Object] = []
    # Ground elements
    objs += add_ground_cluster('trap_ground', rock_count=4, tuft_count=4, radius=0.42)

    # Deadfall log / weight
    weight = log_segment('trap_weight_log', length=1.08, radius=0.10, location=(-0.05, 0.0, 0.36), rotation=(math.radians(90), 0, math.radians(78)), bark_material='wood_bark_dark')
    objs.append(weight)
    wedge_rock = organic_rock('trap_back_rock', radius=0.18, location=(-0.46, -0.14, 0.08), scale=(1.2, 0.9, 0.7), material_name='stone_basal')
    objs.append(wedge_rock)

    upright = twig_between('trap_upright', (0.18, 0.0, 0.0), (0.20, 0.0, 0.42), 0.035, 0.018, 'twig_dark')
    diagonal = twig_between('trap_diagonal', (0.20, 0.0, 0.42), (-0.05, 0.0, 0.18), 0.020, 0.010, 'twig_light')
    bait_stick = twig_between('trap_bait', (0.20, 0.0, 0.10), (0.42, 0.02, 0.13), 0.008, 0.005, 'twig_light')
    trigger = twig_between('trap_trigger', (0.20, 0.0, 0.12), (0.06, 0.0, 0.24), 0.010, 0.006, 'twig_light')
    objs += [upright, diagonal, bait_stick, trigger]

    bait = B.primitive_uv_sphere('trap_bait_meat', radius=0.04, location=(0.45, 0.02, 0.14), segments=10, rings=8, material=B.mat('meat'))
    bait.scale = (1.2, 0.85, 0.70)
    objs.append(bait)
    return objs



def gen_wall_detailed():
    objs: List[bpy.types.Object] = []
    width = 2.9
    stakes = 12
    spacing = width / (stakes - 1)
    positions = []
    for i in range(stakes):
        x = -width * 0.5 + i * spacing
        h = random.uniform(1.50, 1.95)
        post = B.stake(f'wall_stake_v7_{i}', height=h, radius=random.uniform(0.058, 0.082), location=(x, random.uniform(-0.03, 0.03), h * 0.5), material_name='wood_dark')
        post.rotation_euler = Euler((0, math.radians(random.uniform(-3, 3)), math.radians(random.uniform(-4, 4))), 'XYZ')
        objs.append(post)
        positions.append((x, h))
    rail_low = log_segment('wall_rail_low_v7', length=2.70, radius=0.045, location=(0.02, -0.08, 0.78), rotation=(math.radians(90), 0, math.radians(90)), bark_material='wood_bark_mid')
    rail_high = log_segment('wall_rail_high_v7', length=2.64, radius=0.040, location=(0.00, -0.09, 1.20), rotation=(math.radians(90), 0, math.radians(90)), bark_material='wood_bark_mid')
    objs += [rail_low, rail_high]
    for i in range(0, stakes, 2):
        x = positions[i][0]
        objs += rope_wrap(f'wall_lash_{i}', location=(x, -0.06, 0.80), axis='X', turns=3, width=0.12, radius=0.008)
        objs += rope_wrap(f'wall_lash_hi_{i}', location=(x, -0.07, 1.20), axis='X', turns=3, width=0.12, radius=0.008)
    objs += add_ground_cluster('wall_ground', rock_count=8, tuft_count=8, radius=1.2)
    return objs



def gen_campfire_detailed():
    objs: List[bpy.types.Object] = []
    ring_r = 0.56
    for i in range(14):
        ang = i / 14 * math.tau + random.uniform(-0.06, 0.06)
        dist = ring_r + random.uniform(-0.05, 0.03)
        stone = organic_rock(
            f'campfire_ring_v7_{i}',
            radius=random.uniform(0.08, 0.14),
            location=(math.cos(ang) * dist, math.sin(ang) * dist, 0.05),
            scale=(random.uniform(0.8, 1.25), random.uniform(0.8, 1.20), random.uniform(0.60, 1.0)),
            material_name='stone_basal',
        )
        objs.append(stone)
    for i in range(6):
        ang = i / 6 * math.tau
        stick = log_segment(
            f'campfire_wood_v7_{i}',
            length=random.uniform(0.34, 0.58), radius=random.uniform(0.028, 0.045),
            location=(math.cos(ang) * 0.10, math.sin(ang) * 0.10, 0.18 + random.uniform(-0.02, 0.05)),
            rotation=(math.radians(random.uniform(45, 72)), 0, ang),
            bark_material='wood_bark_dark'
        )
        objs.append(stick)
    coal = B.primitive_uv_sphere('campfire_coal_v7', radius=0.18, location=(0, 0, 0.05), segments=12, rings=8, material=B.mat('charcoal'))
    coal.scale = (1.5, 1.2, 0.4)
    objs.append(coal)
    objs.extend(B.fire_cluster(scale=0.78, location=(0, 0, 0.40)))
    return objs



def gen_storage_box_detailed():
    objs: List[bpy.types.Object] = []
    # Base chest footprint and body
    body = B.board('storage_body_core_v7', 1.02, 0.52, 0.72, location=(0, 0, 0.26), material_name='wood_dark')
    objs.append(body)
    # External planks
    plank_y = [-0.28, -0.10, 0.10, 0.28]
    for i, y in enumerate(plank_y):
        objs.append(B.board(f'storage_side_left_plank_{i}', 0.08, 0.16, 0.72, location=(-0.47, y, 0.26), material_name='wood_warm'))
        objs.append(B.board(f'storage_side_right_plank_{i}', 0.08, 0.16, 0.72, location=(0.47, y, 0.26), material_name='wood_warm'))
    plank_x = [-0.38, -0.12, 0.12, 0.38]
    for i, x in enumerate(plank_x):
        objs.append(B.board(f'storage_front_plank_{i}', 0.20, 0.08, 0.72, location=(x, -0.31, 0.26), material_name='wood_warm'))
        objs.append(B.board(f'storage_back_plank_{i}', 0.20, 0.08, 0.72, location=(x, 0.31, 0.26), material_name='wood_warm'))
    lid = B.board('storage_lid_v7', 1.10, 0.12, 0.80, location=(0, 0.01, 0.64), material_name='wood_dark')
    objs.append(lid)
    for x in (-0.35, 0.35):
        brace = B.board(f'storage_lid_brace_{x:+.0f}', 0.10, 0.08, 0.78, location=(x, 0.0, 0.70), rotation=(0, 0, math.radians(2)), material_name='wood_warm')
        objs.append(brace)
    objs += rope_wrap('storage_lashing_left', location=(-0.47, 0.0, 0.54), axis='X', turns=3, width=0.16, radius=0.010)
    objs += rope_wrap('storage_lashing_right', location=(0.47, 0.0, 0.54), axis='X', turns=3, width=0.16, radius=0.010)
    return objs



def gen_tent_detailed():
    """A more believable debris / bark A-frame shelter."""
    objs: List[bpy.types.Object] = []
    floor = B.board('tent_floor_v7', 2.35, 0.06, 1.75, location=(0, 0, 0.03), material_name='dirt')
    objs.append(floor)

    front = (-0.82, 0.0, 0.0)
    rear = (0.82, 0.0, 0.0)
    ridge_h = 1.62

    # Forked uprights at front and rear
    for name, x in [('front', front[0]), ('rear', rear[0])]:
        left = twig_between(f'tent_{name}_upright_l', (x, -0.10, 0.0), (x, -0.04, ridge_h - 0.14), 0.070, 0.040, 'wood_bark_dark')
        right = twig_between(f'tent_{name}_upright_r', (x, 0.10, 0.0), (x, 0.04, ridge_h - 0.14), 0.070, 0.040, 'wood_bark_dark')
        fork = twig_between(f'tent_{name}_fork', (x, -0.05, ridge_h - 0.14), (x, 0.05, ridge_h - 0.14), 0.018, 0.018, 'twig_light')
        objs += [left, right, fork]
        objs += rope_wrap(f'tent_{name}_lashing', location=(x, 0.0, ridge_h - 0.14), axis='X', turns=4, width=0.18, radius=0.011)

    ridge = twig_between('tent_ridge_v7', (front[0], 0.0, ridge_h), (rear[0], 0.0, ridge_h), 0.050, 0.047, 'wood_bark_dark')
    objs.append(ridge)

    # Rafters down to both sides
    rafter_xs = [-0.64, -0.34, -0.02, 0.28, 0.58]
    for i, x in enumerate(rafter_xs):
        top = (x, 0.0, ridge_h - 0.02)
        left_base = (x, -0.78, 0.05)
        right_base = (x, 0.78, 0.05)
        objs.append(twig_between(f'tent_left_rafter_{i}', top, left_base, 0.028, 0.018, 'wood_bark_mid'))
        objs.append(twig_between(f'tent_right_rafter_{i}', top, right_base, 0.028, 0.018, 'wood_bark_mid'))

    # Left roof - bark shingles / slabs layered
    left_rows = [1.26, 1.08, 0.90]
    for row_idx, z in enumerate(left_rows):
        for i, x in enumerate([-0.50, -0.18, 0.14, 0.46]):
            panel = V.bark_panel(
                f'tent_bark_panel_{row_idx}_{i}',
                width=0.34, height=0.78,
                location=(x, -0.40 + row_idx * 0.02, z - row_idx * 0.18),
                rotation=(0, math.radians(57), math.radians(90 + random.uniform(-4, 4))),
                material_name='wood_bark_mid' if (i + row_idx) % 2 else 'wood_bark_dark'
            )
            objs.append(panel)

    # Right roof - hide / debris panels layered with overlap
    right_rows = [1.18, 1.00, 0.84]
    for row_idx, z in enumerate(right_rows):
        for i, x in enumerate([-0.48, -0.16, 0.16, 0.48]):
            panel = V.hide_panel(
                f'tent_hide_panel_{row_idx}_{i}',
                width=0.28, height=0.48,
                location=(x, 0.43 - row_idx * 0.02, z - row_idx * 0.14),
                rotation=(math.radians(120), 0, math.radians(90 + random.uniform(-5, 5))),
                material_name='hide_dark' if i % 2 else 'hide'
            )
            objs.append(panel)

    # Front opening flaps and rear closure
    front_flap_l = V.hide_panel('tent_front_flap_l_v7', width=0.22, height=0.52, location=(-0.82, -0.18, 0.45), rotation=(math.radians(108), 0, math.radians(76)), material_name='hide')
    front_flap_r = V.hide_panel('tent_front_flap_r_v7', width=0.22, height=0.52, location=(-0.82, 0.18, 0.45), rotation=(math.radians(72), 0, math.radians(104)), material_name='hide')
    rear_panel = V.hide_panel('tent_rear_panel_v7', width=0.50, height=0.60, location=(0.82, 0.0, 0.56), rotation=(math.radians(90), 0, math.radians(90)), material_name='hide_dark')
    objs += [front_flap_l, front_flap_r, rear_panel]

    # Bedding inside
    bed = V.grass_patch('tent_bed_v7', blade_count=28, radius=0.34, height=0.18, flower_count=0)
    for obj in bed:
        obj.location.x += 0.00
        obj.location.z += 0.05
    objs.extend(bed)
    # A simple pelt on top of the bedding
    pelt = V.hide_panel('tent_bed_pelt_v7', width=0.34, height=0.22, location=(0.08, 0.0, 0.07), rotation=(math.radians(90), 0, math.radians(4)), material_name='fur_tawny')
    objs.append(pelt)
    return objs


# -----------------------------------------------------------------------------
# Vegetation / world resource overrides
# -----------------------------------------------------------------------------


def berry_bush_variant_detailed(variant='a'):
    berry_palettes = {
        'a': ('berry_red_fresh', 'berry_blue'),
        'b': ('berry_blue', 'berry_black'),
        'c': ('berry_red_fresh', 'berry_black'),
        'd': ('berry_red_fresh',),
    }
    objs: List[bpy.types.Object] = []
    # Root crown and many canes
    anchors: List[Tuple[float, float, float]] = []
    stem_count = {'a': 12, 'b': 13, 'c': 10, 'd': 14}.get(variant, 12)
    for i in range(stem_count):
        ang = i / stem_count * math.tau + random.uniform(-0.30, 0.30)
        r = random.uniform(0.00, 0.16)
        base = (math.cos(ang) * r, math.sin(ang) * r, 0.0)
        h = random.uniform(0.55, 0.95)
        bend = random.uniform(0.10, 0.28)
        tip = (math.cos(ang) * (r + bend), math.sin(ang) * (r + bend), h)
        objs.append(twig_between(f'berry_bush_{variant}_cane_{i}', base, tip, random.uniform(0.020, 0.032), random.uniform(0.004, 0.009), 'twig_light'))
        anchors.append(tip)
        # a side branch or two on some canes
        if i % 2 == 0:
            branch_ang = ang + random.uniform(-0.6, 0.6)
            mid = Vector(base).lerp(Vector(tip), random.uniform(0.45, 0.75))
            side_tip = (mid.x + math.cos(branch_ang) * random.uniform(0.12, 0.22), mid.y + math.sin(branch_ang) * random.uniform(0.12, 0.22), mid.z + random.uniform(0.05, 0.16))
            objs.append(twig_between(f'berry_bush_{variant}_side_{i}', tuple(mid), side_tip, 0.010, 0.004, 'twig_light'))
            anchors.append(side_tip)

    # Leaf masses concentrated near tips / outer shell
    for i, anchor in enumerate(anchors[: min(len(anchors), 16)]):
        rad = random.uniform(0.12, 0.20)
        objs.extend(layered_leaf_mass(f'berry_bush_{variant}_mass_{i}', center=anchor, radius=rad, puff_count=8, card_count=10))

    # Berry clusters on a subset of branch ends
    chosen = random.sample(anchors, k=min(max(6, len(anchors) // 2), len(anchors)))
    for i, anchor in enumerate(chosen):
        objs.extend(berry_cluster_attached(f'berry_bush_{variant}_cluster_{i}', anchor=anchor, berry_count=random.randint(3, 6), berry_palette=berry_palettes.get(variant, ('berry_red_fresh', 'berry_blue'))))

    objs.extend(add_ground_cluster(f'berry_bush_{variant}_ground', rock_count=6, tuft_count=6, radius=0.55))
    return objs



def green_bush_variant_detailed(variant='a'):
    objs: List[bpy.types.Object] = []
    scaffold, anchors = make_leafy_branch_system(f'green_bush_{variant}', branch_count=10, trunk_points=((0, 0, 0.05), (0, 0, 0.42)))
    objs.extend(scaffold)
    for idx, anchor in enumerate(anchors):
        objs.extend(layered_leaf_mass(f'green_bush_{variant}_foliage_{idx}', center=anchor, radius=random.uniform(0.16, 0.24), puff_count=8, card_count=10,
                                      palette=('leaf_sun', 'leaf_light', 'leaf_mid', 'leaf_deep')))
    objs.extend(add_ground_cluster(f'green_bush_{variant}_ground', rock_count=5, tuft_count=5, radius=0.45))
    return objs



def dry_bush_variant_detailed(variant='a'):
    objs: List[bpy.types.Object] = []
    base_count = {'a': 16, 'b': 14, 'c': 18}.get(variant, 16)
    for i in range(base_count):
        ang = i / base_count * math.tau + random.uniform(-0.25, 0.25)
        base = (random.uniform(-0.06, 0.06), random.uniform(-0.06, 0.06), 0.0)
        spread = random.uniform(0.18, 0.42)
        h = random.uniform(0.42, 0.88)
        tip = (math.cos(ang) * spread, math.sin(ang) * spread, h)
        objs.append(twig_between(f'dry_bush_{variant}_stem_{i}', base, tip, random.uniform(0.014, 0.022), random.uniform(0.003, 0.006), 'twig_dark'))
        # secondary forks
        if i % 2 == 0:
            mid = Vector(base).lerp(Vector(tip), random.uniform(0.55, 0.75))
            for j in (-1, 1):
                fork_ang = ang + random.uniform(0.25, 0.55) * j
                fork_tip = (mid.x + math.cos(fork_ang) * random.uniform(0.10, 0.20), mid.y + math.sin(fork_ang) * random.uniform(0.10, 0.20), mid.z + random.uniform(0.06, 0.12))
                objs.append(twig_between(f'dry_bush_{variant}_fork_{i}_{j}', tuple(mid), fork_tip, 0.006, 0.0025, 'twig_light'))
                if random.random() < 0.35:
                    pod = B.primitive_uv_sphere(f'dry_bush_{variant}_pod_{i}_{j}', radius=0.014, location=fork_tip, segments=8, rings=5, material=B.mat('fiber'))
                    pod.scale = (0.85, 0.85, 1.1)
                    objs.append(pod)
    objs.extend(add_ground_cluster(f'dry_bush_{variant}_ground', rock_count=4, tuft_count=4, radius=0.42))
    return objs



def wildflower_patch_variant_detailed(variant='a'):
    objs = V.grass_patch(f'grass_patch_{variant}_core', blade_count=44, radius=0.34, height=0.52, flower_count=0)
    flower_colors = ['white', 'yellow', 'purple', 'white', 'purple']
    for i in range(9 if variant != 'c' else 12):
        ang = random.uniform(0, math.tau)
        dist = random.uniform(0.0, 0.28)
        loc = (math.cos(ang) * dist, math.sin(ang) * dist, 0.0)
        objs.extend(V.flower_head(
            f'grass_patch_{variant}_flower_{i}',
            location=loc,
            stem_h=random.uniform(0.18, 0.42),
            color=random.choice(flower_colors)
        ))
    objs.extend(add_ground_cluster(f'grass_patch_{variant}_ground', rock_count=2, tuft_count=4, radius=0.30))
    return objs



def sapling_tree_variant_detailed(variant='a', species='leafy'):
    objs: List[bpy.types.Object] = []
    trunk = twig_between(f'sapling_{species}_{variant}_trunk', (0, 0, 0.0), (0.0, 0.0, 1.45), 0.055, 0.020, 'wood_dark')
    objs.append(trunk)
    crown_anchors = []
    for i in range(8):
        z = random.uniform(0.65, 1.30)
        ang = random.uniform(0, math.tau)
        length = random.uniform(0.15, 0.36)
        base = (0, 0, z)
        end = (math.cos(ang) * length, math.sin(ang) * length, z + random.uniform(0.05, 0.18))
        objs.append(twig_between(f'sapling_{species}_{variant}_branch_{i}', base, end, 0.012, 0.004, 'twig_light'))
        crown_anchors.append(end)
    if species == 'leafy':
        for idx, anchor in enumerate(crown_anchors):
            objs.extend(layered_leaf_mass(f'sapling_leaf_mass_{variant}_{idx}', center=anchor, radius=random.uniform(0.12, 0.20), puff_count=6, card_count=8))
    else:
        # young conifer: tiered boughs
        for idx, z in enumerate([1.20, 1.00, 0.80, 0.62, 0.46]):
            tier_count = max(4, 8 - idx)
            for j in range(tier_count):
                ang = j / tier_count * math.tau
                start = (0, 0, z)
                end = (math.cos(ang) * (0.26 - idx * 0.03), math.sin(ang) * (0.26 - idx * 0.03), z - 0.10)
                objs.append(twig_between(f'sapling_conifer_bough_{variant}_{idx}_{j}', start, end, 0.010, 0.004, 'twig_light'))
                objs.extend(layered_leaf_mass(f'sapling_conifer_mass_{variant}_{idx}_{j}', center=end, radius=0.07, puff_count=3, card_count=4,
                                              palette=('needle_dark', 'leaf_deep', 'leaf_mid')))
    objs.extend(add_ground_cluster(f'sapling_{species}_{variant}_ground', rock_count=4, tuft_count=6, radius=0.42))
    return objs


# -----------------------------------------------------------------------------
# Creature passes - keep v6 base but ensure aliasing all entries
# -----------------------------------------------------------------------------
# v6 creatures are already better than placeholders; reuse them directly.

# -----------------------------------------------------------------------------
# Override generator entry points used by the base pipeline
# -----------------------------------------------------------------------------

# Items / raw materials
B.gen_wood = gen_wood_detailed
B.gen_stone = V.gen_stone_realistic
B.gen_fiber = V.gen_fiber_realistic
B.gen_grass = V.gen_grass_realistic
B.gen_hide = V.gen_hide_realistic
B.gen_meat = V.gen_meat_realistic
B.gen_bone = gen_bone_detailed
B.gen_berries = gen_berries_pickup_detailed
B.gen_torch = V.gen_torch_realistic
B.gen_spear = gen_spear_detailed
B.gen_bow = V.gen_bow_realistic
B.gen_axe = gen_stone_axe_detailed
B.gen_pickaxe = V.gen_pickaxe_realistic

# Placeables
B.gen_campfire = gen_campfire_detailed
B.gen_storage_box = gen_storage_box_detailed
B.gen_tent = gen_tent_detailed
B.gen_wall = gen_wall_detailed
B.gen_trap = gen_trap_detailed

# Vegetation / resources / world
B.gen_green_bush = lambda: green_bush_variant_detailed('a')
B.gen_berry_bush = lambda: berry_bush_variant_detailed('a')
B.gen_dry_bush = lambda: dry_bush_variant_detailed('a')
B.gen_grass_or_flower = lambda: wildflower_patch_variant_detailed('a')
B.gen_leafy_tree = lambda: V.leafy_tree_variant('a', 'mature')
B.gen_conifer_tree = lambda: V.conifer_tree_variant('a', 'mature')
B.gen_dry_tree = lambda: V.dry_tree_variant('a', 'mature')

# Extra variants exposed for manual generation / testing
B.berry_bush_variant = berry_bush_variant_detailed
B.green_bush_variant = green_bush_variant_detailed
B.dry_bush_variant = dry_bush_variant_detailed
B.wildflower_patch_variant = wildflower_patch_variant_detailed
B.sapling_tree_variant = sapling_tree_variant_detailed

# Creatures reuse v6 realism pass
B.gen_small_prey = V.gen_small_prey_realistic
B.gen_grazer = V.gen_grazer_realistic
B.gen_varnak = V.gen_varnak_realistic

# -----------------------------------------------------------------------------
# Catalog overrides so the generated docs describe intended look clearly
# -----------------------------------------------------------------------------

_prev_build_catalog = B.build_catalog


def build_catalog_v7():
    catalog = _prev_build_catalog()
    notes = {
        'wood': ('tied firewood bundle of short bark-covered logs with visible end grain', (0.78, 0.40, 0.26)),
        'stone': ('cluster of irregular gathered stones', (0.60, 0.46, 0.28)),
        'fiber': ('sheaf of long plant fibers / reeds bound together', (0.22, 0.18, 0.72)),
        'grass': ('harvestable tuft of field grass', (0.22, 0.22, 0.34)),
        'hide': ('folded animal hide / pelt sheets', (0.46, 0.34, 0.08)),
        'meat': ('raw meat chunk with natural asymmetry', (0.30, 0.22, 0.16)),
        'bone': ('single realistic long bone with rounded ends', (0.62, 0.16, 0.18)),
        'berries': ('berry sprig / pickup attached to twig and leaves', (0.24, 0.18, 0.14)),
        'torch': ('wrapped torch with burning head', (0.10, 0.10, 1.00)),
        'spear': ('hafted stone spear with flaked head and lashing', (0.12, 0.12, 1.46)),
        'bow': ('primitive self bow with tapered limbs and grip wrap', (0.16, 0.10, 1.12)),
        'axe': ('split-haft stone axe with visible lashings', (0.22, 0.16, 0.90)),
        'pickaxe': ('primitive cross-head pick tool', (0.34, 0.18, 0.96)),
        'campfire': ('stone ring campfire with fuel, embers, and flame', (1.00, 1.00, 0.70)),
        'storage_box': ('handmade wooden storage chest / crate', (1.02, 0.72, 0.78)),
        'tent': ('A-frame bushcraft shelter with bark / hide covering', (2.20, 1.65, 1.55)),
        'wall': ('irregular palisade wall with rails and lashings', (2.60, 0.25, 1.75)),
        'trap': ('readable primitive deadfall trap', (1.18, 0.90, 0.56)),
        'green_bush': ('dense branch-supported shrub', (0.96, 0.96, 0.96)),
        'berry_bush': ('fruiting shrub with attached berry clusters', (1.10, 1.10, 1.08)),
        'dry_bush': ('twiggy dry bush with airy silhouette', (0.82, 0.82, 0.96)),
        'grass_or_flower': ('mixed grass and wildflower patch', (0.42, 0.42, 0.42)),
        'small_prey': ('hare-like prey animal', (0.62, 0.28, 0.42)),
        'grazer': ('deer/goat-like grazer silhouette', (1.50, 0.50, 1.48)),
        'varnak': ('wolf/hyena-like predator silhouette', (1.60, 0.50, 1.30)),
    }
    for asset_id, (note, bounds) in notes.items():
        if asset_id in catalog:
            catalog[asset_id].notes = note
            catalog[asset_id].min_bounds = bounds
    return catalog

B.build_catalog = build_catalog_v7

# -----------------------------------------------------------------------------
# Iteration control
# -----------------------------------------------------------------------------

# Example: ['wood', 'bone', 'berries', 'berry_bush', 'tent']
B.ONLY_ASSETS = []

if __name__ == '__main__':
    B.RENDER_PREVIEWS = False
    B.generate_all()
