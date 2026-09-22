from __future__ import annotations

"""
Apex Shift Bushcraft Asset Generator v6 - research grounded realism pass
=========================================================================

Purpose
-------
This file is a stronger follow-up to the earlier generator passes.
It uses internet-grounded morphological descriptions and common real-world
construction patterns as modelling targets, then patches the original
`bushcraft_asset_generator_v4_master.py`.

Design targets used in this pass
--------------------------------
1. Primitive / bushcraft hand tools should read as *hafted* objects:
   - stone head,
   - wood shaft with taper,
   - visible lashing / wedging,
   - asymmetry and handmade irregularity.
2. Shelter should read as a *real A-frame debris / hide shelter*:
   - two forked uprights,
   - ridgepole,
   - paired rafters,
   - overlapping bark / hide / thatch cover,
   - low triangular opening,
   - back closure,
   - floor bed / pallet.
3. Trap should read as a *figure-4 / deadfall inspired trap*:
   - heavy falling weight,
   - upright support,
   - diagonal trigger,
   - bait stick placed outside the drop zone.
4. Palisade wall should read as *irregular sharpened stakes* tied to rails.
5. Trees and shrubs should follow broad real-world morphology:
   - deciduous trees: trunk flare, branch scaffold, crown asymmetry,
   - conifers: tapering conical silhouette with radial branch tiers,
   - saplings: slim trunks and smaller, lighter crowns,
   - berry bushes: twig mass + leaves + attached berry clusters.
6. Creatures should have believable silhouettes, not toy-like blobs:
   - small prey -> hare / rabbit inspired,
   - grazer -> deer / goat inspired,
   - varnak -> wolf / hyena inspired fantasy predator.
7. Flower patches should have stems, petal discs, and mixed grasses.

Run
---
    blender --background --python bushcraft_asset_generator_v6_research_grounded.py

Notes
-----
- Keep this file in the same directory as `bushcraft_asset_generator_v4_master.py`.
- Output goes to `ApexShift_Bushcraft_Output_v6_ResearchGrounded`.
- You can temporarily limit generation by filling `B.ONLY_ASSETS` near the bottom.
"""

import importlib.util
import math
import os
import random
from pathlib import Path
from typing import Dict, List, Sequence, Tuple

import bpy
from mathutils import Euler, Vector

BASE_PATH = Path(__file__).with_name('bushcraft_asset_generator_v4_master.py')
spec = importlib.util.spec_from_file_location('apex_bushcraft_v4_base', str(BASE_PATH))
B = importlib.util.module_from_spec(spec)
import sys
sys.modules[spec.name] = B
spec.loader.exec_module(B)

def _fire_cluster(scale=1.0, location=(0, 0, 0)):
    a = B.primitive_uv_sphere('v6_flame_orange', radius=0.16 * scale, location=location, segments=12, rings=6, material=B.mat('flame_orange'))
    a.scale = (0.65, 0.65, 1.8)
    b = B.primitive_uv_sphere('v6_flame_yellow', radius=0.12 * scale, location=(location[0] + 0.02 * scale, location[1], location[2] + 0.10 * scale), segments=10, rings=5, material=B.mat('flame_yellow'))
    b.scale = (0.48, 0.48, 1.4)
    return [a, b]

B.fire_cluster = _fire_cluster

# -----------------------------------------------------------------------------
# Output paths
# -----------------------------------------------------------------------------

B.OUTPUT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), 'ApexShift_Bushcraft_Output_v6_ResearchGrounded'))
B.DOCS_ROOT = os.path.join(B.OUTPUT_ROOT, 'Docs')
B.SOURCE_BLEND_DIR = os.path.join(B.OUTPUT_ROOT, 'Source', 'Blend')
B.PREVIEW_DIR = os.path.join(B.OUTPUT_ROOT, 'Previews')
B.ITEM_MODEL_DIR = os.path.join(B.OUTPUT_ROOT, 'Items', 'Models')
B.PLACEABLE_MODEL_DIR = os.path.join(B.OUTPUT_ROOT, 'Placeables', 'Models')
B.RESOURCE_MODEL_DIR = os.path.join(B.OUTPUT_ROOT, 'Resources', 'Models')
B.CREATURE_MODEL_DIR = os.path.join(B.OUTPUT_ROOT, 'Creatures', 'Models')
B.LANDMARK_MODEL_DIR = os.path.join(B.OUTPUT_ROOT, 'Landmarks', 'Models')
B.MANIFEST_PATH = os.path.join(B.OUTPUT_ROOT, 'bushcraft_v6_manifest.json')
B.CATALOG_PATH = os.path.join(B.OUTPUT_ROOT, 'bushcraft_v6_catalog.md')

random.seed(B.SEED + 206)

# -----------------------------------------------------------------------------
# Reference design briefs - human-readable targets for the agent / future work
# -----------------------------------------------------------------------------

REFERENCE_BRIEFS: Dict[str, str] = {
    'tent': (
        'A real bushcraft A-frame shelter uses two forked uprights that hold a ridgepole. '
        'Rafters lean from the ridgepole to the ground on both sides. The roof is covered '
        'with overlapping slabs of bark, hides, or debris bundles. The entrance is low and '
        'triangular, the rear is mostly closed, and the floor often has a leaf / branch bed.'
    ),
    'trap': (
        'A primitive deadfall trap is read best as a heavy log or flat stone supported by a '
        'figure-4 trigger: upright, diagonal lever, and bait stick. The bait sits outside the '
        'weight so the mechanism remains visually understandable from an isometric view.'
    ),
    'wall': (
        'A bushcraft defensive wall is a simple palisade: irregular sharpened stakes planted '
        'vertically, backed by one or two horizontal rails, with visible lashings made of rope, '
        'fiber or rawhide.'
    ),
    'self_bow': (
        'A primitive self bow is a single wooden stave with a thicker rigid handle area, gently '
        'tapering limbs, narrower tips, and a light bowstring. It should not look like a straight '
        'stick; even undrawn it usually has mild arc and limb taper.'
    ),
    'stone_axe': (
        'A primitive stone axe has a hafted stone head fixed to a wooden handle with lashings, '
        'resin or wedges. The head should be asymmetrical and read as stone, not a metal block.'
    ),
    'trees': (
        'Deciduous trees need trunk flare, visible branch scaffolds, and asymmetric crowns. '
        'Conifers should taper to a point with branch tiers. Saplings are thinner, smaller, and '
        'less top-heavy than mature trees.'
    ),
    'berry_bush': (
        'Berry shrubs have a woody twig structure, leaf masses around the tips, and fruit forming '
        'in attached clusters along short pedicels or stems. Berries should not float separately.'
    ),
    'wildflowers': (
        'A small wildflower patch should read as fine grass blades plus individual stems carrying '
        'petaled flower heads with a central disc. Avoid spherical lollipop flowers.'
    ),
    'hare': 'Long ears, compact head, rounded body, powerful hind legs, small tail.',
    'deer': 'Deep but narrow chest, long slender legs, small cloven hooves, narrow muzzle, short tail.',
    'wolf': 'Deep chest, narrower waist, longer muzzle, triangular ears, sloped back, bushy tail.'
}

# -----------------------------------------------------------------------------
# Material extension
# -----------------------------------------------------------------------------

_orig_create_material_library = B.create_material_library

def create_material_library_v6():
    _orig_create_material_library()
    B.MATS['leaf_olive'] = B.make_material('leaf_olive', (0.41, 0.47, 0.22, 1.0))
    B.MATS['leaf_young'] = B.make_material('leaf_young', (0.60, 0.70, 0.35, 1.0))
    B.MATS['needle_dark'] = B.make_material('needle_dark', (0.13, 0.22, 0.10, 1.0))
    B.MATS['flower_center'] = B.make_material('flower_center', (0.83, 0.62, 0.16, 1.0), noise=False, bump=False, roughness=0.70)
    B.MATS['petal_white'] = B.make_material('petal_white', (0.96, 0.95, 0.90, 1.0), noise=False, bump=False, roughness=0.62)
    B.MATS['petal_yellow'] = B.make_material('petal_yellow', (0.92, 0.79, 0.25, 1.0), noise=False, bump=False, roughness=0.62)
    B.MATS['petal_purple'] = B.make_material('petal_purple', (0.68, 0.59, 0.86, 1.0), noise=False, bump=False, roughness=0.62)
    B.MATS['hide_dark'] = B.make_material('hide_dark', (0.36, 0.26, 0.18, 1.0), roughness=0.90, sheen=0.08)
    B.MATS['fur_gray'] = B.make_material('fur_gray', (0.42, 0.42, 0.40, 1.0), roughness=0.94, sheen=0.10)
    B.MATS['fur_tawny'] = B.make_material('fur_tawny', (0.60, 0.45, 0.26, 1.0), roughness=0.93, sheen=0.10)
    B.MATS['fur_red'] = B.make_material('fur_red', (0.61, 0.36, 0.20, 1.0), roughness=0.92, sheen=0.10)
    B.MATS['stone_light'] = B.make_material('stone_light', (0.60, 0.59, 0.55, 1.0), roughness=0.94)
    B.MATS['charcoal'] = B.make_material('charcoal', (0.12, 0.10, 0.08, 1.0), roughness=0.96)

B.create_material_library = create_material_library_v6

# -----------------------------------------------------------------------------
# General helpers
# -----------------------------------------------------------------------------

def organic_sphere(name: str, center=(0, 0, 0), radius=0.2, material_name='leaf_mid', scale=(1.0, 1.0, 1.0)):
    obj = B.primitive_uv_sphere(name, radius=radius, location=center, segments=16, rings=10, material=B.mat(material_name))
    obj.scale = scale
    B.add_displace(obj, strength=radius * 0.15, scale=2.1)
    B.apply_modifier(obj, 'Displace')
    B.smooth(obj, True)
    return obj


def thin_leaf(name: str, location=(0,0,0), rotation=(0,0,0), scale=(0.08, 0.18, 1.0), material_name='leaf_mid'):
    obj = B.primitive_plane(name, size=1.0, location=location, rotation=rotation, material=B.mat(material_name))
    obj.scale = scale
    return obj


def branch_between(name: str, start: Tuple[float, float, float], end: Tuple[float, float, float], r0: float, r1: float, material_name='bark_mid'):
    start_v = Vector(start)
    end_v = Vector(end)
    mid = (start_v + end_v) * 0.5
    delta = end_v - start_v
    depth = max(delta.length, 0.001)
    obj = B.tapered_cylinder(name, r0, r1, depth, location=mid, material=B.mat(material_name), vertices=10)
    obj.rotation_euler = delta.to_track_quat('Z', 'Y').to_euler()
    return obj


def lashing_bundle(prefix: str, location=(0,0,0), axis='Z', turns=4, radius=0.010, width=0.16) -> List[bpy.types.Object]:
    objs = []
    for i in range(turns):
        offset = (i - (turns - 1) / 2) * radius * 1.8
        loc = list(location)
        rot = [0.0, 0.0, 0.0]
        if axis == 'X':
            loc[0] += offset
            rot[1] = math.radians(90)
        elif axis == 'Y':
            loc[1] += offset
            rot[0] = math.radians(90)
        else:
            loc[2] += offset
        objs.append(B.primitive_cylinder(
            f'{prefix}_{i}', radius=radius, depth=width,
            location=tuple(loc), rotation=tuple(rot), vertices=8,
            material=B.mat('rope')
        ))
    return objs


def root_flare(prefix: str, count=4, radius=0.10, length=0.65, z=0.12, material_name='bark_dark'):
    objs = []
    for i in range(count):
        a = i / count * math.tau + random.uniform(-0.18, 0.18)
        target = (math.cos(a) * length, math.sin(a) * length, 0.02)
        objs.append(branch_between(f'{prefix}_root_{i}', (0, 0, z), target, radius, radius * 0.20, material_name))
    return objs


def leaf_mass(prefix: str, center=(0,0,0), radius=1.0, puff_count=18, card_count=10,
              palette=('leaf_young','leaf_light','leaf_mid','leaf_olive','leaf_dark')) -> List[bpy.types.Object]:
    objs = []
    for i in range(puff_count):
        a = random.uniform(0, math.tau)
        r = random.uniform(0.08, radius)
        z = random.uniform(-radius * 0.18, radius * 0.25)
        puff = organic_sphere(
            f'{prefix}_puff_{i}',
            center=(center[0] + math.cos(a) * r, center[1] + math.sin(a) * r, center[2] + z),
            radius=random.uniform(radius * 0.12, radius * 0.24),
            material_name=random.choice(palette),
            scale=(random.uniform(0.90, 1.25), random.uniform(0.90, 1.25), random.uniform(0.75, 1.08))
        )
        objs.append(puff)
    for i in range(card_count):
        a = random.uniform(0, math.tau)
        r = random.uniform(0.0, radius * 0.80)
        z = random.uniform(-radius * 0.18, radius * 0.18)
        objs.append(thin_leaf(
            f'{prefix}_leaf_{i}',
            location=(center[0] + math.cos(a) * r, center[1] + math.sin(a) * r, center[2] + z),
            rotation=(math.radians(random.uniform(45, 90)), math.radians(random.uniform(-18, 18)), random.uniform(0, math.tau)),
            scale=(random.uniform(0.03, 0.08), random.uniform(0.08, 0.18), 1.0),
            material_name=random.choice(palette)
        ))
    return objs


def berry_cluster(prefix: str, anchor=(0,0,0), count=5) -> List[bpy.types.Object]:
    objs = []
    for i in range(count):
        ang = i / count * math.tau + random.uniform(-0.20, 0.20)
        length = random.uniform(0.04, 0.07)
        end = (anchor[0] + math.cos(ang) * length, anchor[1] + math.sin(ang) * length, anchor[2] - random.uniform(0.01, 0.04))
        objs.append(branch_between(f'{prefix}_stem_{i}', anchor, end, 0.0045, 0.002, 'wood_dark'))
        berry = B.primitive_uv_sphere(f'{prefix}_berry_{i}', radius=random.uniform(0.027, 0.040), location=end, segments=10, rings=6, material=B.mat(random.choice(['berry_dark','berry_red'])))
        objs.append(berry)
    return objs


def flower_head(prefix: str, location=(0,0,0), stem_h=0.40, color='white') -> List[bpy.types.Object]:
    objs = []
    stem = B.primitive_cylinder(f'{prefix}_stem', radius=0.008, depth=stem_h, location=(location[0], location[1], location[2] + stem_h * 0.5), vertices=6, material=B.mat('grass'))
    objs.append(stem)
    top = Vector((location[0], location[1], location[2] + stem_h))
    center = B.primitive_uv_sphere(f'{prefix}_center', radius=0.019, location=tuple(top), segments=8, rings=5, material=B.mat('flower_center'))
    center.scale = (1.0, 1.0, 0.65)
    objs.append(center)
    petal_mat = {'white':'petal_white','yellow':'petal_yellow','purple':'petal_purple'}[color]
    petal_count = 6 if color != 'yellow' else 7
    for i in range(petal_count):
        ang = i / petal_count * math.tau
        petal = B.primitive_uv_sphere(f'{prefix}_petal_{i}', radius=0.013, location=(top.x + math.cos(ang) * 0.026, top.y + math.sin(ang) * 0.026, top.z), segments=8, rings=5, material=B.mat(petal_mat))
        petal.scale = (1.50, 0.86, 0.30)
        petal.rotation_euler = Euler((math.radians(72), 0, ang), 'XYZ')
        objs.append(petal)
    for sign in (-1, 1):
        objs.append(thin_leaf(
            f'{prefix}_stem_leaf_{sign}',
            location=(location[0] + sign * 0.015, location[1], location[2] + stem_h * random.uniform(0.35, 0.55)),
            rotation=(math.radians(55), 0, math.radians(35 * sign)),
            scale=(0.018, 0.045, 1.0),
            material_name='leaf_mid'
        ))
    return objs


def bark_panel(name: str, width=0.55, height=1.4, location=(0,0,0), rotation=(0,0,0), material_name='wood_dark'):
    obj = B.board(name, width, 0.03, height, location=location, rotation=rotation, material_name=material_name)
    B.add_displace(obj, strength=0.016, scale=3.0)
    B.apply_modifier(obj, 'Displace')
    return obj


def hide_panel(name: str, width=0.70, height=1.10, location=(0,0,0), rotation=(0,0,0), material_name='hide'):
    obj = B.primitive_plane(name, size=1.0, location=location, rotation=rotation, material=B.mat(material_name))
    obj.scale = (width, height, 1.0)
    solid = obj.modifiers.new('Solidify', 'SOLIDIFY')
    solid.thickness = 0.012
    subdiv = obj.modifiers.new('Subsurf', 'SUBSURF')
    subdiv.levels = 1
    disp = obj.modifiers.new('Displace', 'DISPLACE')
    tex = bpy.data.textures.new(f'{name}_tex', type='CLOUDS')
    tex.noise_scale = 0.55
    disp.texture = tex
    disp.strength = 0.03
    B.apply_modifier(obj, 'Subsurf')
    B.apply_modifier(obj, 'Displace')
    B.apply_modifier(obj, 'Solidify')
    return obj


def grass_patch(prefix: str, blade_count=36, radius=0.35, height=0.75, flower_count=0) -> List[bpy.types.Object]:
    objs = []
    for i in range(blade_count):
        a = random.uniform(0, math.tau)
        r = random.uniform(0.0, radius)
        blade_h = height * random.uniform(0.55, 1.0)
        blade = B.primitive_plane(
            f'{prefix}_blade_{i}', size=1.0,
            location=(math.cos(a)*r, math.sin(a)*r, blade_h * 0.5),
            rotation=(math.radians(random.uniform(58, 82)), math.radians(random.uniform(-10, 10)), a),
            material=B.mat('grass')
        )
        blade.scale = (random.uniform(0.008, 0.020), blade_h * 0.48, 1.0)
        objs.append(blade)
    for i in range(flower_count):
        a = random.uniform(0, math.tau)
        r = random.uniform(0.0, radius * 0.90)
        objs += flower_head(f'{prefix}_flower_{i}', (math.cos(a)*r, math.sin(a)*r, 0), stem_h=random.uniform(height*0.45, height*0.72), color=random.choice(['white','yellow','purple']))
    return objs


def log_stack(prefix: str, count=6, base=(0,0,0), length=0.80, radius=0.08) -> List[bpy.types.Object]:
    objs = []
    for i in range(count):
        x = base[0] + random.uniform(-0.10, 0.10)
        y = base[1] + random.uniform(-0.08, 0.08)
        z = base[2] + (i % 2) * radius * 0.55
        rot = (math.radians(90), 0, math.radians(random.uniform(-14, 14)))
        l = B.chopped_log(f'{prefix}_{i}', length=length * random.uniform(0.82, 1.08), radius=radius * random.uniform(0.88, 1.12), location=(x, y, z + radius), rotation=rot, material_name='wood_warm')
        objs.append(l)
    return objs

# -----------------------------------------------------------------------------
# Items - more faithful primitive / natural forms
# -----------------------------------------------------------------------------

def gen_wood_realistic():
    return log_stack('wood_piece', count=7, base=(0, 0, 0.0), length=0.78, radius=0.08)


def gen_stone_realistic():
    objs = []
    objs.append(B.irregular_rock('stone_main', radius=0.34, location=(0.0, 0.0, 0.17), scale=(1.15, 0.95, 0.92), material_name='stone'))
    for i in range(5):
        ang = i / 5 * math.tau + random.uniform(-0.20, 0.20)
        r = random.uniform(0.20, 0.32)
        objs.append(B.irregular_rock(f'stone_frag_{i}', radius=random.uniform(0.08, 0.14), location=(math.cos(ang)*r, math.sin(ang)*r, 0.05), scale=(1.0, random.uniform(0.8,1.1), random.uniform(0.8,1.0)), material_name='stone_light'))
    return objs


def gen_fiber_realistic():
    objs = []
    stem_count = 22
    for i in range(stem_count):
        a = random.uniform(-0.09, 0.09)
        y = random.uniform(-0.10, 0.10)
        stem = B.primitive_cylinder(f'fiber_stem_{i}', radius=0.012, depth=random.uniform(0.72, 0.90), location=(random.uniform(-0.04, 0.04), y, 0.36), rotation=(a, math.radians(random.uniform(-8,8)), 0), vertices=6, material=B.mat('rope'))
        objs.append(stem)
    objs += lashing_bundle('fiber_tie', location=(0, 0, 0.26), axis='Z', turns=4, radius=0.012, width=0.20)
    return objs


def gen_grass_realistic():
    return grass_patch('grass_pickup', blade_count=24, radius=0.22, height=0.55, flower_count=0)


def gen_hide_realistic():
    objs = []
    hide = hide_panel('hide_sheet', width=0.62, height=0.46, location=(0,0,0.03), rotation=(math.radians(90), 0, math.radians(12)), material_name='hide')
    objs.append(hide)
    for i in range(3):
        fold = hide_panel(f'hide_fold_{i}', width=0.24, height=0.18, location=(random.uniform(-0.12,0.10), random.uniform(-0.06,0.06), 0.05 + i*0.015), rotation=(math.radians(85 + random.uniform(-12,12)), 0, math.radians(random.uniform(-28,28))), material_name='hide_dark' if i == 2 else 'hide')
        objs.append(fold)
    return objs


def gen_meat_realistic():
    objs = []
    chunk = B.primitive_uv_sphere('meat_chunk', radius=0.24, location=(0,0,0.16), segments=16, rings=10, material=B.mat('meat'))
    chunk.scale = (1.28, 0.92, 0.70)
    B.add_displace(chunk, strength=0.05, scale=2.6)
    B.apply_modifier(chunk, 'Displace')
    objs.append(chunk)
    fat = B.primitive_uv_sphere('meat_fat', radius=0.10, location=(-0.12, -0.02, 0.17), segments=10, rings=6, material=B.mat('bone'))
    fat.scale = (1.2, 0.65, 0.55)
    objs.append(fat)
    return objs


def gen_bone_realistic():
    objs = []
    shaft = B.primitive_cylinder('bone_shaft', radius=0.048, depth=0.92, location=(0,0,0.16), rotation=(math.radians(90), math.radians(8), math.radians(22)), vertices=12, material=B.mat('bone'))
    objs.append(shaft)
    for idx, pos in enumerate([(-0.42, -0.03, 0.13), (-0.37, 0.04, 0.19), (0.41, -0.04, 0.13), (0.46, 0.05, 0.19)]):
        knob = B.primitive_uv_sphere(f'bone_knob_{idx}', radius=0.085, location=pos, segments=12, rings=8, material=B.mat('bone'))
        knob.scale = (1.0, 0.9, 1.0)
        objs.append(knob)
    return objs


def gen_berries_realistic():
    objs = []
    stem = branch_between('berries_stem', (-0.08, -0.02, 0.02), (0.10, 0.04, 0.11), 0.010, 0.005, 'wood_dark')
    objs.append(stem)
    objs += berry_cluster('berries_cluster', anchor=(0.10, 0.04, 0.11), count=6)
    objs.append(thin_leaf('berries_leaf_a', location=(-0.04, -0.02, 0.04), rotation=(math.radians(74), math.radians(8), math.radians(24)), scale=(0.08, 0.14, 1.0), material_name='leaf_mid'))
    objs.append(thin_leaf('berries_leaf_b', location=(0.03, 0.03, 0.05), rotation=(math.radians(70), math.radians(-8), math.radians(-18)), scale=(0.07, 0.12, 1.0), material_name='leaf_light'))
    return objs


def gen_torch_realistic():
    objs = []
    shaft = B.tapered_cylinder('torch_shaft', 0.028, 0.020, 1.05, location=(0,0,0.525), material=B.mat('wood_warm'), vertices=10)
    shaft.rotation_euler = Euler((math.radians(-2), math.radians(3), math.radians(2)), 'XYZ')
    objs.append(shaft)
    for i in range(7):
        rag = hide_panel(f'torch_wrap_{i}', width=0.07, height=0.12, location=(random.uniform(-0.02,0.02), random.uniform(-0.02,0.02), 0.88 + i*0.018), rotation=(math.radians(random.uniform(30, 80)), math.radians(random.uniform(-12, 12)), random.uniform(0, math.tau)), material_name='hide_dark')
        objs.append(rag)
    objs += lashing_bundle('torch_lashing', location=(0,0,0.82), axis='Z', turns=5, radius=0.011, width=0.18)
    flame_parts = B.fire_cluster(scale=0.55, location=(0, 0, 1.12))
    objs.extend(flame_parts)
    charcoal = B.primitive_cylinder('torch_char', radius=0.035, depth=0.16, location=(0,0,0.98), vertices=8, material=B.mat('charcoal'))
    objs.append(charcoal)
    return objs


def gen_spear_realistic():
    objs = []
    shaft = B.tapered_cylinder('spear_shaft', 0.026, 0.016, 1.46, location=(0,0,0.73), material=B.mat('wood_warm'), vertices=12)
    objs.append(shaft)
    head = B.irregular_rock('spear_head', radius=0.12, location=(0,0,1.45), scale=(0.42, 1.20, 0.25), material_name='stone_light')
    head.rotation_euler = Euler((0, math.radians(90), math.radians(90)), 'XYZ')
    objs.append(head)
    socket = B.board('spear_socket', 0.07, 0.03, 0.16, location=(0,0,1.34), rotation=(0,0,0), material_name='wood_dark')
    objs.append(socket)
    objs += lashing_bundle('spear_lashing', location=(0,0,1.28), axis='Z', turns=5, radius=0.010, width=0.16)
    return objs


def gen_bow_realistic():
    objs = []
    # Primitive self bow with thicker handle, tapered limbs, narrow tips.
    left_points = [(-0.02, 0, 0.06), (-0.13, 0, 0.38), (-0.18, 0, 0.74), (-0.16, 0, 1.10)]
    right_points = [(0.02, 0, 0.06), (0.13, 0, 0.38), (0.18, 0, 0.74), (0.16, 0, 1.10)]
    # build left and right limbs as segmented tapered branches
    radii_left = [0.030, 0.026, 0.020, 0.013]
    radii_right = [0.030, 0.026, 0.020, 0.013]
    for i in range(len(left_points) - 1):
        objs.append(branch_between(f'bow_left_{i}', left_points[i], left_points[i+1], radii_left[i], radii_left[i+1], 'wood_warm'))
    for i in range(len(right_points) - 1):
        objs.append(branch_between(f'bow_right_{i}', right_points[i], right_points[i+1], radii_right[i], radii_right[i+1], 'wood_warm'))
    handle = B.board('bow_handle', 0.06, 0.05, 0.18, location=(0,0,0.08), rotation=(0,0,0), material_name='wood_dark')
    objs.append(handle)
    grip_wrap = hide_panel('bow_grip', width=0.06, height=0.09, location=(0,0,0.08), rotation=(math.radians(90), 0, 0), material_name='hide_dark')
    objs.append(grip_wrap)
    # string
    string = branch_between('bow_string', (-0.16, 0.01, 1.10), (0.16, 0.01, 1.10), 0.004, 0.004, 'rope')
    # shift and rotate entire assembly into a standing bow orientation
    group = objs + [string]
    for obj in group:
        obj.rotation_euler.rotate(Euler((math.radians(90), 0, math.radians(90)), 'XYZ'))
    return group


def gen_axe_realistic():
    objs = []
    haft = B.tapered_cylinder('axe_haft', 0.034, 0.026, 0.92, location=(0,0,0.46), material=B.mat('wood_warm'), vertices=12)
    objs.append(haft)
    head = B.irregular_rock('axe_head', radius=0.17, location=(0.02, 0.0, 0.83), scale=(1.18, 0.55, 0.45), material_name='stone_light')
    head.rotation_euler = Euler((0, math.radians(88), math.radians(6)), 'XYZ')
    objs.append(head)
    cleft = B.board('axe_cleft', 0.06, 0.018, 0.12, location=(0.00, 0.0, 0.84), rotation=(0, 0, 0), material_name='wood_dark')
    objs.append(cleft)
    objs += lashing_bundle('axe_lashing', location=(0,0,0.77), axis='Z', turns=5, radius=0.014, width=0.16)
    return objs


def gen_pickaxe_realistic():
    objs = []
    haft = B.tapered_cylinder('pickaxe_haft', 0.035, 0.026, 1.03, location=(0,0,0.515), material=B.mat('wood_warm'), vertices=12)
    objs.append(haft)
    spike_a = branch_between('pick_cross', (-0.30, 0.0, 0.90), (0.30, 0.0, 0.92), 0.050, 0.040, 'stone')
    spike_b = branch_between('pick_pick', (0.10, 0.0, 0.90), (0.36, 0.0, 1.00), 0.030, 0.010, 'stone_dark')
    spike_c = branch_between('pick_adze', (-0.02, 0.0, 0.90), (-0.26, 0.0, 0.84), 0.030, 0.012, 'stone_light')
    objs += [spike_a, spike_b, spike_c]
    objs += lashing_bundle('pick_lashing', location=(0,0,0.86), axis='Z', turns=5, radius=0.013, width=0.18)
    return objs

# -----------------------------------------------------------------------------
# Placeables
# -----------------------------------------------------------------------------

def gen_campfire_realistic():
    objs = []
    ring_r = 0.52
    for i in range(12):
        ang = i / 12 * math.tau + random.uniform(-0.10, 0.10)
        r = ring_r + random.uniform(-0.05, 0.05)
        stone = B.irregular_rock(f'campfire_ring_{i}', radius=random.uniform(0.09, 0.13), location=(math.cos(ang)*r, math.sin(ang)*r, 0.06), scale=(1.0, random.uniform(0.8,1.2), random.uniform(0.7,1.0)), material_name='stone_light')
        objs.append(stone)
    # teepee/log-cabin hybrid using burned kindling
    for i in range(6):
        ang = i / 6 * math.tau
        stick = B.chopped_log(f'campfire_stick_{i}', length=0.58, radius=0.035, location=(math.cos(ang)*0.10, math.sin(ang)*0.10, 0.22), rotation=(math.radians(65), 0, ang), material_name='wood_dark')
        objs.append(stick)
    coal = B.primitive_uv_sphere('campfire_coal', radius=0.16, location=(0,0,0.08), segments=10, rings=6, material=B.mat('charcoal'))
    coal.scale = (1.4, 1.1, 0.5)
    objs.append(coal)
    flame_parts = B.fire_cluster(scale=0.75, location=(0,0,0.42))
    objs.extend(flame_parts)
    return objs


def gen_storage_box_realistic():
    objs = []
    base = B.board('storage_floor', 1.00, 0.08, 0.70, location=(0,0,0.04), material_name='wood_dark')
    objs.append(base)
    for i, x in enumerate([-0.46, 0.46]):
        objs.append(B.board(f'storage_side_{i}', 0.06, 0.46, 0.64, location=(x, 0, 0.36), material_name='wood_warm'))
    for i, y in enumerate([-0.31, 0.31]):
        objs.append(B.board(f'storage_end_{i}', 0.94, 0.06, 0.62, location=(0, y, 0.35), material_name='wood_warm'))
    # slats
    for i in range(4):
        y = -0.24 + i * 0.16
        objs.append(B.board(f'storage_slats_top_{i}', 0.88, 0.06, 0.02, location=(0, y, 0.66), material_name='wood_warm'))
    lid = B.board('storage_lid', 1.06, 0.12, 0.76, location=(0, 0.02, 0.75), rotation=(math.radians(-4), 0, 0), material_name='wood_dark')
    objs.append(lid)
    objs += lashing_bundle('storage_rope', location=(0.0, 0.34, 0.64), axis='X', turns=3, radius=0.010, width=0.18)
    return objs


def gen_tent_realistic():
    """Real A-frame shelter with forked uprights, ridgepole, rafters and covering."""
    objs = []
    # ground bed / pallet
    floor = B.board('tent_floor', 2.10, 0.05, 1.65, location=(0,0,0.025), material_name='dirt')
    objs.append(floor)
    # two forked front/back supports
    support_positions = [(-0.72, 0.0, 0.0), (0.72, 0.0, 0.0)]
    ridge_height = 1.58
    support_tops = []
    for s_idx, (x, y, z) in enumerate(support_positions):
        trunk_left = branch_between(f'tent_support_{s_idx}_left', (x, y, 0.0), (x, -0.10, ridge_height - 0.12), 0.070, 0.040, 'wood_dark')
        trunk_right = branch_between(f'tent_support_{s_idx}_right', (x, y, 0.0), (x, 0.10, ridge_height - 0.12), 0.070, 0.040, 'wood_dark')
        fork_cross = branch_between(f'tent_support_{s_idx}_fork', (x, -0.10, ridge_height - 0.12), (x, 0.10, ridge_height - 0.12), 0.018, 0.018, 'wood_dark')
        objs += [trunk_left, trunk_right, fork_cross]
        support_tops.append((x, 0.0, ridge_height))
    ridge = branch_between('tent_ridgepole', support_tops[0], support_tops[1], 0.052, 0.048, 'wood_dark')
    objs.append(ridge)
    # rafters
    rafter_xs = [-0.58, -0.32, -0.06, 0.20, 0.46]
    for i, x in enumerate(rafter_xs):
        ridge_point = (x, 0, ridge_height - 0.02)
        left_ground = (x, -0.72, 0.06)
        right_ground = (x, 0.72, 0.06)
        objs.append(branch_between(f'tent_rafter_left_{i}', ridge_point, left_ground, 0.030, 0.018, 'wood_warm'))
        objs.append(branch_between(f'tent_rafter_right_{i}', ridge_point, right_ground, 0.030, 0.018, 'wood_warm'))
    # roof cover: bark slabs on one side, hide panels on the other for visual variety
    panel_xs = [-0.50, -0.15, 0.20, 0.55]
    for i, x in enumerate(panel_xs):
        # left side bark
        objs.append(bark_panel(
            f'tent_bark_{i}', width=0.42, height=1.28,
            location=(x, -0.34, 0.88),
            rotation=(math.radians(0), math.radians(58), math.radians(90)),
            material_name='wood_dark' if i % 2 == 0 else 'wood_warm'
        ))
        # right side hides / thatch-like cover
        objs.append(hide_panel(
            f'tent_hide_{i}', width=0.38, height=0.62,
            location=(x, 0.38, 0.82),
            rotation=(math.radians(122), 0, math.radians(90)),
            material_name='hide' if i % 2 == 0 else 'hide_dark'
        ))
    # rear closure and front flap
    rear = hide_panel('tent_rear_closure', width=0.52, height=0.70, location=(0.72, 0.0, 0.54), rotation=(math.radians(90), 0, math.radians(90)), material_name='hide_dark')
    objs.append(rear)
    front_flap_left = hide_panel('tent_front_flap_left', width=0.26, height=0.56, location=(-0.72, -0.20, 0.46), rotation=(math.radians(106), 0, math.radians(76)), material_name='hide')
    front_flap_right = hide_panel('tent_front_flap_right', width=0.26, height=0.56, location=(-0.72, 0.20, 0.46), rotation=(math.radians(74), 0, math.radians(104)), material_name='hide')
    objs += [front_flap_left, front_flap_right]
    # lashings at ridge supports
    objs += lashing_bundle('tent_lash_a', location=(-0.72, 0, ridge_height-0.12), axis='X', turns=4, radius=0.012, width=0.18)
    objs += lashing_bundle('tent_lash_b', location=(0.72, 0, ridge_height-0.12), axis='X', turns=4, radius=0.012, width=0.18)
    # bedding inside
    objs += grass_patch('tent_bed', blade_count=18, radius=0.28, height=0.30, flower_count=0)
    for obj in objs[-18:]:
        obj.location.x += 0.08
        obj.location.z += 0.04
    return objs


def gen_wall_realistic():
    objs = []
    width = 2.8
    posts = 11
    spacing = width / (posts - 1)
    positions = []
    for i in range(posts):
        x = -width * 0.5 + i * spacing
        h = random.uniform(1.55, 1.95)
        post = B.stake(f'wall_post_{i}', height=h, radius=random.uniform(0.055, 0.078), location=(x, 0, h * 0.5), material_name='wood_dark')
        objs.append(post)
        positions.append((x, h))
    rail_low = B.chopped_log('wall_rail_low', length=2.64, radius=0.045, location=(0, -0.07, 0.74), rotation=(math.radians(90), 0, math.radians(90)), material_name='wood_warm')
    rail_high = B.chopped_log('wall_rail_high', length=2.60, radius=0.040, location=(0, -0.08, 1.20), rotation=(math.radians(90), 0, math.radians(90)), material_name='wood_warm')
    objs += [rail_low, rail_high]
    for idx in range(0, posts, 2):
        x, _ = positions[idx]
        objs += lashing_bundle(f'wall_lash_low_{idx}', location=(x, -0.03, 0.74), axis='X', turns=3, radius=0.009, width=0.10)
        objs += lashing_bundle(f'wall_lash_high_{idx}', location=(x, -0.03, 1.20), axis='X', turns=3, radius=0.009, width=0.10)
    return objs


def gen_trap_realistic():
    objs = []
    base = B.board('trap_ground', 1.32, 0.05, 0.96, location=(0,0,0.025), material_name='dirt')
    objs.append(base)
    deadfall = B.chopped_log('trap_deadfall', length=1.18, radius=0.15, location=(-0.05, 0.0, 0.26), rotation=(math.radians(90), 0, math.radians(88)), material_name='wood_dark')
    objs.append(deadfall)
    support = branch_between('trap_support', (0.02, 0.0, 0.05), (0.02, 0.0, 0.28), 0.018, 0.012, 'wood_warm')
    diagonal = branch_between('trap_diagonal', (0.02, 0.0, 0.28), (0.25, -0.10, 0.10), 0.014, 0.010, 'wood_warm')
    bait_stick = branch_between('trap_bait', (0.25, -0.10, 0.10), (0.42, -0.13, 0.14), 0.010, 0.006, 'wood_warm')
    objs += [support, diagonal, bait_stick]
    bait = gen_meat_realistic()
    for o in bait:
        o.location.x += 0.46
        o.location.y -= 0.13
        o.location.z = o.location.z * 0.35 + 0.04
        o.scale *= 0.48
    objs.extend(bait)
    return objs

# -----------------------------------------------------------------------------
# Vegetation / resources
# -----------------------------------------------------------------------------

def leafy_tree_variant(variant='a', stage='mature'):
    profile = {
        'a': dict(h=5.8, t=0.30, crown=1.75),
        'b': dict(h=5.4, t=0.28, crown=1.55),
        'c': dict(h=5.1, t=0.26, crown=1.42),
    }[variant]
    if stage == 'sapling':
        profile = dict(h=1.45 if variant != 'c' else 1.22, t=0.07, crown=0.42)
    objs = []
    trunk = B.tapered_cylinder(f'leafy_tree_{variant}_{stage}_trunk', profile['t'], profile['t'] * 0.56, profile['h'], location=(0,0,profile['h']*0.5), material=B.mat('bark_mid'), vertices=16)
    B.add_displace(trunk, strength=profile['t'] * 0.11, scale=2.6)
    B.apply_modifier(trunk, 'Displace')
    objs.append(trunk)
    if stage != 'sapling':
        objs += root_flare(f'leafy_tree_{variant}_{stage}', count=5, radius=profile['t']*0.40, length=0.82, z=0.16)
    branch_count = 4 if stage == 'sapling' else 8
    endpoints = []
    for i in range(branch_count):
        a = i / branch_count * math.tau + random.uniform(-0.35, 0.35)
        z = random.uniform(profile['h'] * (0.42 if stage == 'sapling' else 0.48), profile['h'] * 0.82)
        reach = random.uniform(profile['crown']*0.35, profile['crown']*0.85)
        end = (math.cos(a)*reach, math.sin(a)*reach, z + random.uniform(0.08, 0.35))
        start = (0, 0, z - random.uniform(0.12, 0.22))
        objs.append(branch_between(f'leafy_tree_{variant}_{stage}_branch_{i}', start, end, profile['t'] * 0.14, profile['t'] * 0.038, 'bark_mid'))
        endpoints.append(end)
    for i, end in enumerate(endpoints):
        r = profile['crown'] * (0.25 if stage == 'sapling' else 0.42)
        objs += leaf_mass(f'leafy_tree_{variant}_{stage}_cluster_{i}', center=end, radius=r, puff_count=8 if stage == 'sapling' else 16, card_count=4 if stage == 'sapling' else 10)
    return objs


def conifer_tree_variant(variant='a', stage='mature'):
    profile = {
        'a': dict(h=6.0, t=0.24, base=1.32),
        'b': dict(h=6.4, t=0.22, base=1.08),
        'c': dict(h=5.5, t=0.25, base=1.46),
    }[variant]
    if stage == 'sapling':
        profile = dict(h=1.58 if variant != 'b' else 1.72, t=0.06, base=0.34)
    objs = []
    trunk = B.tapered_cylinder(f'conifer_tree_{variant}_{stage}_trunk', profile['t'], profile['t'] * 0.42, profile['h'], location=(0,0,profile['h']*0.5), material=B.mat('bark_dark'), vertices=14)
    B.add_displace(trunk, strength=profile['t'] * 0.10, scale=2.8)
    B.apply_modifier(trunk, 'Displace')
    objs.append(trunk)
    if stage != 'sapling':
        objs += root_flare(f'conifer_tree_{variant}_{stage}', count=4, radius=profile['t'] * 0.25, length=0.52, z=0.12, material_name='bark_dark')
    levels = 4 if stage == 'sapling' else 9
    for level in range(levels):
        t = level / max(1, levels - 1)
        radius = profile['base'] * (1.0 - t * 0.80)
        z = profile['h'] * (0.22 + t * 0.68)
        branches = 8 if stage == 'sapling' else 14
        for i in range(branches):
            a = i / branches * math.tau + random.uniform(-0.08, 0.08)
            tip = (math.cos(a) * radius, math.sin(a) * radius, z)
            objs.append(branch_between(f'conifer_tree_{variant}_{stage}_branch_{level}_{i}', (0,0,z+0.03), tip, profile['t']*0.07, profile['t']*0.015, 'bark_mid'))
            needle = organic_sphere(f'conifer_tree_{variant}_{stage}_needle_{level}_{i}', center=(tip[0]*0.92, tip[1]*0.92, z - 0.03), radius=max(radius * 0.12, 0.07), material_name=random.choice(['needle','needle_dark']), scale=(1.28, 1.0, 0.48))
            objs.append(needle)
    objs.append(organic_sphere(f'conifer_tree_{variant}_{stage}_top', center=(0,0,profile['h']*0.98), radius=profile['base']*0.12, material_name='needle', scale=(0.85, 0.85, 1.5)))
    return objs


def dry_tree_variant(variant='a', stage='mature'):
    profile = {'a': (4.2, 0.18, 7), 'b': (4.7, 0.17, 8), 'c': (4.0, 0.16, 6)}[variant]
    if stage == 'sapling':
        profile = (1.20 if variant != 'c' else 1.08, 0.06, 2)
    h, t, bcount = profile
    objs = []
    trunk = B.tapered_cylinder(f'dry_tree_{variant}_{stage}_trunk', t, t*0.56, h, location=(0,0,h*0.5), material=B.mat('wood_dark'), vertices=12)
    B.add_displace(trunk, strength=t*0.18, scale=2.0)
    B.apply_modifier(trunk, 'Displace')
    objs.append(trunk)
    if stage != 'sapling':
        objs += root_flare(f'dry_tree_{variant}_{stage}', count=4, radius=t*0.28, length=0.56, z=0.10, material_name='wood_dark')
    for i in range(bcount):
        a = random.uniform(0, math.tau)
        start_z = random.uniform(h*0.36, h*0.90)
        reach = random.uniform(0.36, 1.06)
        rise = random.uniform(0.18, 0.66)
        end = (math.cos(a)*reach, math.sin(a)*reach, start_z + rise)
        objs.append(branch_between(f'dry_tree_{variant}_{stage}_branch_{i}', (0,0,start_z), end, t*0.16, t*0.03, 'wood_dark'))
    return objs


def green_bush_variant(variant='a'):
    profile = {'a': (0.84, 20), 'b': (0.72, 18), 'c': (0.95, 24)}[variant]
    radius, puffs = profile
    objs = []
    for i in range(12 if variant != 'c' else 15):
        a = random.uniform(0, math.tau)
        end = (math.cos(a) * random.uniform(radius * 0.30, radius * 0.70), math.sin(a) * random.uniform(radius * 0.30, radius * 0.70), random.uniform(0.20, 0.72))
        objs.append(branch_between(f'green_bush_{variant}_twig_{i}', (0,0,0.10), end, 0.020, 0.006, 'wood_dark'))
    objs += leaf_mass(f'green_bush_{variant}', center=(0,0,radius*0.55), radius=radius, puff_count=puffs, card_count=14)
    return objs


def berry_bush_variant(variant='a'):
    objs = green_bush_variant(variant)
    clusters = {'a': 6, 'b': 5, 'c': 7}[variant]
    spread = {'a': 0.62, 'b': 0.54, 'c': 0.68}[variant]
    for i in range(clusters):
        a = random.uniform(0, math.tau)
        r = random.uniform(0.18, spread)
        z = random.uniform(0.26, 0.78)
        anchor = (math.cos(a)*r, math.sin(a)*r, z)
        objs += berry_cluster(f'berry_bush_{variant}_{i}', anchor, count=random.randint(4,7))
    return objs


def dry_bush_variant(variant='a'):
    profile = {'a': (16,0.58,0.72), 'b': (14,0.46,0.88), 'c': (20,0.68,0.82)}[variant]
    bcount, radius, height = profile
    objs = []
    for i in range(bcount):
        a = random.uniform(0, math.tau)
        end = (math.cos(a) * random.uniform(radius*0.25, radius), math.sin(a) * random.uniform(radius*0.25, radius), random.uniform(height*0.25, height))
        objs.append(branch_between(f'dry_bush_{variant}_{i}', (0,0,0.08), end, 0.015, 0.002, 'dry_leaf'))
    return objs


def tall_grass_clump_variant(variant='a'):
    if variant == 'a':
        return grass_patch('tall_grass_a', blade_count=40, radius=0.38, height=1.02, flower_count=0)
    return grass_patch('tall_grass_b', blade_count=28, radius=0.28, height=0.82, flower_count=0)


def wildflower_patch_variant(variant='a'):
    if variant == 'a':
        return grass_patch('wildflowers_a', blade_count=24, radius=0.30, height=0.58, flower_count=7)
    return grass_patch('wildflowers_b', blade_count=16, radius=0.24, height=0.44, flower_count=4)

# -----------------------------------------------------------------------------
# Creatures
# -----------------------------------------------------------------------------

def gen_small_prey_realistic():
    objs = []
    body = B.primitive_uv_sphere('small_prey_body', radius=0.29, location=(0,0,0.28), segments=16, rings=10, material=B.mat('fur_tawny'))
    body.scale = (1.25, 0.80, 0.88)
    chest = B.primitive_uv_sphere('small_prey_chest', radius=0.14, location=(0.20,0,0.28), segments=12, rings=8, material=B.mat('fur_tawny'))
    rump = B.primitive_uv_sphere('small_prey_rump', radius=0.18, location=(-0.20,0,0.26), segments=12, rings=8, material=B.mat('fur_tawny'))
    head = B.primitive_uv_sphere('small_prey_head', radius=0.12, location=(0.36,0,0.36), segments=12, rings=8, material=B.mat('fur_tawny'))
    muzzle = B.primitive_uv_sphere('small_prey_muzzle', radius=0.06, location=(0.46,0,0.33), segments=8, rings=5, material=B.mat('bone'))
    objs += [body, chest, rump, head, muzzle]
    for sign in (-1, 1):
        ear = B.primitive_uv_sphere(f'small_prey_ear_{sign}', radius=0.05, location=(0.35, sign*0.03, 0.52), segments=8, rings=5, material=B.mat('fur_tawny'))
        ear.scale = (0.42, 0.22, 2.4)
        ear.rotation_euler = Euler((math.radians(10), math.radians(-18), math.radians(8*sign)), 'XYZ')
        objs.append(ear)
    leg_specs = [(-0.20,-0.09,0.12,0.24), (-0.20,0.09,0.12,0.24), (0.18,-0.07,0.10,0.18), (0.18,0.07,0.10,0.18)]
    for i, (x, y, z, h) in enumerate(leg_specs):
        leg = B.primitive_cylinder(f'small_prey_leg_{i}', radius=0.04 if i > 1 else 0.05, depth=h, location=(x,y,z), vertices=8, material=B.mat('fur_tawny'))
        objs.append(leg)
    tail = B.primitive_uv_sphere('small_prey_tail', radius=0.05, location=(-0.34,0,0.30), segments=8, rings=5, material=B.mat('bone'))
    objs.append(tail)
    return objs


def gen_grazer_realistic():
    objs = []
    body = B.primitive_uv_sphere('grazer_body', radius=0.58, location=(0,0,0.84), segments=20, rings=12, material=B.mat('fur_brown'))
    body.scale = (1.55, 0.80, 0.92)
    chest = B.primitive_uv_sphere('grazer_chest', radius=0.34, location=(0.58,0,0.86), segments=16, rings=10, material=B.mat('fur_brown'))
    hip = B.primitive_uv_sphere('grazer_hip', radius=0.32, location=(-0.55,0,0.82), segments=16, rings=10, material=B.mat('fur_brown'))
    neck = branch_between('grazer_neck', (0.56,0,1.02), (0.94,0,1.36), 0.12, 0.08, 'fur_brown')
    head = B.primitive_uv_sphere('grazer_head', radius=0.24, location=(1.10,0,1.42), segments=16, rings=10, material=B.mat('fur_tan'))
    muzzle = B.primitive_uv_sphere('grazer_muzzle', radius=0.12, location=(1.28,0,1.34), segments=10, rings=6, material=B.mat('fur_tan'))
    objs += [body, chest, hip, neck, head, muzzle]
    for sign in (-1,1):
        ear = B.primitive_uv_sphere(f'grazer_ear_{sign}', radius=0.06, location=(1.05, sign*0.08, 1.58), segments=8, rings=5, material=B.mat('fur_brown'))
        ear.scale = (0.5, 0.22, 1.3)
        horn = branch_between(f'grazer_horn_{sign}', (1.00, sign*0.07, 1.58), (1.12, sign*0.10, 1.86), 0.028, 0.010, 'bone')
        objs += [ear, horn]
    legs = [(-0.48,-0.20), (-0.48,0.20), (0.32,-0.18), (0.32,0.18)]
    for i,(x,y) in enumerate(legs):
        upper = branch_between(f'grazer_upper_{i}', (x,y,0.76), (x+0.02,y,0.34), 0.09, 0.06, 'fur_brown')
        lower = branch_between(f'grazer_lower_{i}', (x+0.02,y,0.34), (x+0.04,y,0.08), 0.06, 0.04, 'fur_tan')
        hoof = B.primitive_cube(f'grazer_hoof_{i}', size=(0.08,0.06,0.05), location=(x+0.04,y,0.03), material=B.mat('wood_dark'))
        objs += [upper, lower, hoof]
    tail = branch_between('grazer_tail', (-0.90,0,1.00), (-1.10,0,0.82), 0.03, 0.01, 'fur_brown')
    objs.append(tail)
    return objs


def gen_varnak_realistic():
    objs = []
    body = B.primitive_uv_sphere('varnak_body', radius=0.56, location=(0,0,0.78), segments=20, rings=12, material=B.mat('fur_gray'))
    body.scale = (1.55, 0.78, 0.84)
    shoulder = B.primitive_uv_sphere('varnak_shoulder', radius=0.34, location=(0.42,0,0.92), segments=16, rings=10, material=B.mat('fur_gray'))
    hip = B.primitive_uv_sphere('varnak_hip', radius=0.28, location=(-0.48,0,0.70), segments=14, rings=8, material=B.mat('fur_gray'))
    neck = branch_between('varnak_neck', (0.48,0,1.00), (0.88,0,1.04), 0.12, 0.07, 'fur_gray')
    head = B.primitive_uv_sphere('varnak_head', radius=0.24, location=(1.05,0,1.03), segments=16, rings=10, material=B.mat('fur_dark'))
    muzzle = B.primitive_uv_sphere('varnak_muzzle', radius=0.13, location=(1.28,0,0.98), segments=10, rings=6, material=B.mat('fur_red'))
    jaw = B.primitive_uv_sphere('varnak_jaw', radius=0.10, location=(1.26,0,0.90), segments=10, rings=6, material=B.mat('bone'))
    jaw.scale = (1.2, 0.7, 0.4)
    objs += [body, shoulder, hip, neck, head, muzzle, jaw]
    for sign in (-1,1):
        ear = B.primitive_uv_sphere(f'varnak_ear_{sign}', radius=0.07, location=(1.00, sign*0.08, 1.21), segments=8, rings=5, material=B.mat('fur_dark'))
        ear.scale = (0.4, 0.25, 1.4)
        fang = branch_between(f'varnak_fang_{sign}', (1.26, sign*0.04, 0.92), (1.31, sign*0.04, 0.82), 0.012, 0.004, 'bone')
        objs += [ear, fang]
    leg_positions = [(-0.40,-0.18), (-0.40,0.18), (0.42,-0.16), (0.42,0.16)]
    for i,(x,y) in enumerate(leg_positions):
        front = i >= 2
        top_z = 0.82 if front else 0.66
        mid_z = 0.38 if front else 0.34
        upper = branch_between(f'varnak_upper_{i}', (x,y,top_z), (x + (0.04 if front else -0.02), y, mid_z), 0.08, 0.05, 'fur_dark')
        lower = branch_between(f'varnak_lower_{i}', (x + (0.04 if front else -0.02), y, mid_z), (x + (0.08 if front else -0.04), y, 0.10), 0.05, 0.035, 'fur_gray')
        paw = B.primitive_cube(f'varnak_paw_{i}', size=(0.10,0.08,0.05), location=(x + (0.08 if front else -0.04), y, 0.03), material=B.mat('wood_dark'))
        objs += [upper, lower, paw]
    tail = branch_between('varnak_tail', (-0.94,0,0.92), (-1.28,0,1.12), 0.04, 0.012, 'fur_dark')
    objs.append(tail)
    # subtle fantasy spines
    for i, x in enumerate((-0.12, 0.08, 0.28)):
        objs.append(branch_between(f'varnak_spine_{i}', (x,0,1.10+i*0.02), (x+0.02,0,1.28+i*0.03), 0.018, 0.004, 'bone'))
    return objs

# -----------------------------------------------------------------------------
# Monkey patch base generators
# -----------------------------------------------------------------------------

B.gen_wood = gen_wood_realistic
B.gen_stone = gen_stone_realistic
B.gen_fiber = gen_fiber_realistic
B.gen_grass = gen_grass_realistic
B.gen_hide = gen_hide_realistic
B.gen_meat = gen_meat_realistic
B.gen_bone = gen_bone_realistic
B.gen_berries = gen_berries_realistic
B.gen_torch = gen_torch_realistic
B.gen_spear = gen_spear_realistic
B.gen_bow = gen_bow_realistic
B.gen_axe = gen_axe_realistic
B.gen_pickaxe = gen_pickaxe_realistic

B.gen_campfire = gen_campfire_realistic
B.gen_storage_box = gen_storage_box_realistic
B.gen_tent = gen_tent_realistic
B.gen_wall = gen_wall_realistic
B.gen_trap = gen_trap_realistic

B.leafy_tree_variant = leafy_tree_variant
B.conifer_tree_variant = conifer_tree_variant
B.dry_tree_variant = dry_tree_variant
B.green_bush_variant = green_bush_variant
B.berry_bush_variant = berry_bush_variant
B.dry_bush_variant = dry_bush_variant
B.tall_grass_clump_variant = tall_grass_clump_variant
B.wildflower_patch_variant = wildflower_patch_variant

# wrappers likely used by single-asset base ids
B.gen_green_bush = lambda: green_bush_variant('a')
B.gen_berry_bush = lambda: berry_bush_variant('a')
B.gen_dry_bush = lambda: dry_bush_variant('a')
B.gen_grass_or_flower = lambda: wildflower_patch_variant('a')
B.gen_leafy_tree = lambda: leafy_tree_variant('a', 'mature')
B.gen_conifer_tree = lambda: conifer_tree_variant('a', 'mature')
B.gen_dry_tree = lambda: dry_tree_variant('a', 'mature')

B.gen_small_prey = gen_small_prey_realistic
B.gen_grazer = gen_grazer_realistic
B.gen_varnak = gen_varnak_realistic

# -----------------------------------------------------------------------------
# Catalog notes / minimum sizes
# -----------------------------------------------------------------------------

_orig_build_catalog = B.build_catalog

def build_catalog_v6():
    catalog = _orig_build_catalog()
    notes = {
        'wood': ('bundle of cut logs with irregular diameters', (0.72, 0.36, 0.24)),
        'stone': ('cluster of irregular gathered stones', (0.58, 0.44, 0.26)),
        'fiber': ('tied sheaf of long plant fibers / reeds', (0.20, 0.18, 0.64)),
        'grass': ('harvestable grass tuft', (0.16, 0.16, 0.28)),
        'hide': ('folded animal hide / pelt', (0.42, 0.30, 0.05)),
        'meat': ('raw meat chunk with irregular silhouette', (0.24, 0.18, 0.12)),
        'bone': ('realistic long bone with bulbous ends', (0.48, 0.12, 0.12)),
        'berries': ('berry cluster attached to stem and leaves', (0.12, 0.10, 0.10)),
        'torch': ('wrapped torch with burning head', (0.10, 0.10, 0.90)),
        'spear': ('hafted stone spear with lashing', (0.10, 0.10, 1.30)),
        'bow': ('primitive self bow with tapered limbs', (0.14, 0.10, 1.10)),
        'axe': ('stone axe with wood haft and rope lashing', (0.16, 0.12, 0.80)),
        'pickaxe': ('primitive pick tool with cross-head and lashing', (0.30, 0.16, 0.90)),
        'campfire': ('stone ring campfire with kindling and flame', (0.90, 0.90, 0.66)),
        'storage_box': ('rough wooden storage crate / box', (0.92, 0.62, 0.66)),
        'tent': ('A-frame bushcraft shelter', (1.90, 1.45, 1.45)),
        'wall': ('irregular palisade with rails and lashings', (2.20, 0.18, 1.50)),
        'trap': ('figure-4 inspired deadfall trap', (1.10, 0.74, 0.30)),
        'grass_or_flower': ('mixed grass and wildflower patch', (0.22, 0.22, 0.22)),
        'small_prey': ('hare-like small prey', (0.62, 0.25, 0.40)),
        'grazer': ('deer/goat-like grazer', (1.45, 0.48, 1.46)),
        'varnak': ('wolf/hyena-like fantasy predator', (1.55, 0.46, 1.26)),
    }
    for asset_id, (note, bounds) in notes.items():
        if asset_id in catalog:
            catalog[asset_id].notes = note
            catalog[asset_id].min_bounds = bounds
    return catalog

B.build_catalog = build_catalog_v6

# Restrict generation during iteration if needed, e.g. ['tent', 'small_prey']
B.ONLY_ASSETS = []

if __name__ == '__main__':
    B.generate_all()
