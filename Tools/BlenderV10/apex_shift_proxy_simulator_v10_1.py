from __future__ import annotations

import json
import math
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable, Dict, Iterable, List, Sequence, Tuple

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
import trimesh
from mpl_toolkits.mplot3d.art3d import Poly3DCollection

from apex_shift_profiles_v10 import (
    AssetProfile,
    SIMULATION_ASSET_IDS,
    load_profiles,
    profiles_by_id,
    validate_profile_coverage,
)

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "simulation_output_v10_1"
OUT.mkdir(parents=True, exist_ok=True)


@dataclass
class ProxyPart:
    name: str
    mesh: trimesh.Trimesh
    color: Tuple[float, float, float]
    tags: Tuple[str, ...] = ()


@dataclass
class ProxyAsset:
    asset_id: str
    parts: List[ProxyPart] = field(default_factory=list)

    def add(self, part: ProxyPart) -> None:
        self.parts.append(part)

    @property
    def bounds(self) -> np.ndarray:
        mins = np.min([part.mesh.bounds[0] for part in self.parts], axis=0)
        maxs = np.max([part.mesh.bounds[1] for part in self.parts], axis=0)
        return np.stack([mins, maxs], axis=0)

    @property
    def extents(self) -> np.ndarray:
        return self.bounds[1] - self.bounds[0]


COLORS = {
    "bark": (0.30, 0.18, 0.09),
    "wood": (0.48, 0.30, 0.14),
    "cut": (0.74, 0.57, 0.34),
    "rope": (0.64, 0.48, 0.24),
    "stone": (0.44, 0.46, 0.48),
    "flint": (0.25, 0.28, 0.30),
    "leaf": (0.28, 0.48, 0.20),
    "leaf_light": (0.48, 0.65, 0.30),
    "needle": (0.16, 0.34, 0.18),
    "berry_red": (0.65, 0.11, 0.10),
    "berry_blue": (0.18, 0.20, 0.48),
    "hide": (0.48, 0.30, 0.16),
    "bone": (0.84, 0.80, 0.68),
    "fur": (0.46, 0.31, 0.20),
    "fur_light": (0.69, 0.55, 0.37),
    "charcoal": (0.10, 0.09, 0.08),
    "fire": (0.95, 0.35, 0.04),
    "fire_yellow": (1.00, 0.75, 0.08),
    "earth": (0.38, 0.29, 0.19),
}


def _transform(mesh: trimesh.Trimesh, translation=(0, 0, 0), scale=(1, 1, 1), rotation=(0, 0, 0)) -> trimesh.Trimesh:
    mesh = mesh.copy()
    mesh.apply_scale(scale)
    rx, ry, rz = rotation
    matrix = trimesh.transformations.euler_matrix(rx, ry, rz, axes="sxyz")
    mesh.apply_transform(matrix)
    mesh.apply_translation(translation)
    return mesh


def ellipsoid(name: str, center, radii, color, tags=()) -> ProxyPart:
    mesh = trimesh.creation.icosphere(subdivisions=1, radius=1.0)
    mesh = _transform(mesh, translation=center, scale=radii)
    return ProxyPart(name, mesh, color, tuple(tags))


def box(name: str, center, size, color, rotation=(0, 0, 0), tags=()) -> ProxyPart:
    mesh = trimesh.creation.box(extents=size)
    mesh = _transform(mesh, translation=center, rotation=rotation)
    return ProxyPart(name, mesh, color, tuple(tags))


def cylinder_between(name: str, a, b, radius, color, sections=10, tags=()) -> ProxyPart:
    a = np.asarray(a, dtype=float)
    b = np.asarray(b, dtype=float)
    vec = b - a
    length = float(np.linalg.norm(vec))
    if length < 1e-6:
        length = 1e-6
        vec = np.array([0, 0, 1], dtype=float)
    mesh = trimesh.creation.cylinder(radius=radius, height=length, sections=sections)
    align = trimesh.geometry.align_vectors([0, 0, 1], vec / length)
    mesh.apply_transform(align)
    mesh.apply_translation((a + b) * 0.5)
    return ProxyPart(name, mesh, color, tuple(tags))


def cone_between(name: str, a, b, radius, color, sections=10, tags=()) -> ProxyPart:
    a = np.asarray(a, dtype=float)
    b = np.asarray(b, dtype=float)
    vec = b - a
    length = float(np.linalg.norm(vec))
    mesh = trimesh.creation.cone(radius=radius, height=max(length, 1e-5), sections=sections)
    align = trimesh.geometry.align_vectors([0, 0, 1], vec / max(length, 1e-5))
    mesh.apply_transform(align)
    mesh.apply_translation((a + b) * 0.5)
    return ProxyPart(name, mesh, color, tuple(tags))


def wedge(name: str, center, size, color, rotation=(0, 0, 0), tags=()) -> ProxyPart:
    sx, sy, sz = size
    vertices = np.array([
        [-sx / 2, -sy / 2, -sz / 2], [sx / 2, -sy / 2, -sz / 2],
        [-sx / 2, sy / 2, -sz / 2], [sx / 2, sy / 2, -sz / 2],
        [-sx * 0.12, -sy / 2, sz / 2], [sx * 0.12, -sy / 2, sz / 2],
        [-sx * 0.12, sy / 2, sz / 2], [sx * 0.12, sy / 2, sz / 2],
    ])
    faces = np.array([
        [0, 1, 3], [0, 3, 2], [4, 6, 7], [4, 7, 5],
        [0, 4, 5], [0, 5, 1], [2, 3, 7], [2, 7, 6],
        [0, 2, 6], [0, 6, 4], [1, 5, 7], [1, 7, 3],
    ])
    mesh = trimesh.Trimesh(vertices=vertices, faces=faces, process=False)
    mesh = _transform(mesh, translation=center, rotation=rotation)
    return ProxyPart(name, mesh, color, tuple(tags))


def leaf(name: str, center, size, color, rotation=(0, 0, 0), tags=()) -> ProxyPart:
    sx, sy, sz = size
    vertices = np.array([
        [0, -sy / 2, 0], [sx / 2, 0, sz * 0.2], [0, sy / 2, 0], [-sx / 2, 0, -sz * 0.1],
        [0, 0, sz],
    ])
    faces = np.array([[0, 1, 4], [1, 2, 4], [2, 3, 4], [3, 0, 4], [0, 3, 2], [0, 2, 1]])
    mesh = trimesh.Trimesh(vertices=vertices, faces=faces, process=False)
    mesh = _transform(mesh, translation=center, rotation=rotation)
    return ProxyPart(name, mesh, color, tuple(tags))


def ring_parts(prefix: str, center, radius, tube_radius, count, color, axis="z") -> List[ProxyPart]:
    parts: List[ProxyPart] = []
    for i in range(count):
        a0 = i / count * math.tau
        a1 = (i + 1) / count * math.tau
        if axis == "z":
            p0 = (center[0] + math.cos(a0) * radius, center[1] + math.sin(a0) * radius, center[2])
            p1 = (center[0] + math.cos(a1) * radius, center[1] + math.sin(a1) * radius, center[2])
        else:
            p0 = (center[0], center[1] + math.cos(a0) * radius, center[2] + math.sin(a0) * radius)
            p1 = (center[0], center[1] + math.cos(a1) * radius, center[2] + math.sin(a1) * radius)
        parts.append(cylinder_between(f"{prefix}_{i}", p0, p1, tube_radius, color, sections=6, tags=("rope",)))
    return parts


def _seed(asset_id: str) -> np.random.Generator:
    value = sum((i + 1) * ord(char) for i, char in enumerate(asset_id)) % (2**32 - 1)
    return np.random.default_rng(value)


# -----------------------------------------------------------------------------
# Representative proxy builders
# -----------------------------------------------------------------------------


def build_wood(profile: AssetProfile) -> ProxyAsset:
    rng = _seed(profile.asset_id)
    asset = ProxyAsset(profile.asset_id)
    for i in range(7):
        y = (i - 3) * 0.045 + rng.uniform(-0.012, 0.012)
        z = 0.07 + abs(i - 3) * 0.010
        length = rng.uniform(0.62, 0.82)
        radius = rng.uniform(0.035, 0.060)
        angle = rng.uniform(-0.10, 0.10)
        a = (-length / 2, y, z)
        b = (length / 2, y + math.sin(angle) * 0.03, z + rng.uniform(-0.015, 0.015))
        asset.add(cylinder_between(f"log_{i}", a, b, radius, COLORS["bark"], tags=("wood", "ground")))
        asset.add(ellipsoid(f"cut_{i}_a", a, (radius * 0.92, radius * 0.92, radius * 0.18), COLORS["cut"], tags=("cut_face",)))
        asset.add(ellipsoid(f"cut_{i}_b", b, (radius * 0.92, radius * 0.92, radius * 0.18), COLORS["cut"], tags=("cut_face",)))
    for x in (-0.22, 0.22):
        asset.parts.extend(ring_parts(f"binding_{x}", (x, 0, 0.09), 0.13, 0.008, 12, COLORS["rope"], axis="x"))
    return asset


def build_berries(profile: AssetProfile) -> ProxyAsset:
    rng = _seed(profile.asset_id)
    asset = ProxyAsset(profile.asset_id)
    main_a, main_b = (-0.14, 0.0, 0.03), (0.12, 0.0, 0.14)
    asset.add(cylinder_between("main_twig", main_a, main_b, 0.009, COLORS["bark"], tags=("stem", "ground")))
    anchors = [(0.04, -0.04, 0.12), (0.08, 0.04, 0.14)]
    for j, anchor in enumerate(anchors):
        asset.add(cylinder_between(f"side_stem_{j}", (0.0, 0, 0.10), anchor, 0.004, COLORS["wood"], tags=("stem",)))
        for i in range(4):
            ang = i / 4 * math.tau + rng.uniform(-0.25, 0.25)
            berry_center = (anchor[0] + math.cos(ang) * 0.035, anchor[1] + math.sin(ang) * 0.035, anchor[2] - 0.025 - i * 0.003)
            asset.add(cylinder_between(f"pedicel_{j}_{i}", anchor, berry_center, 0.0025, COLORS["leaf"], tags=("pedicel",)))
            asset.add(ellipsoid(f"berry_{j}_{i}", berry_center, (0.025, 0.025, 0.026), COLORS["berry_red"] if (i + j) % 2 == 0 else COLORS["berry_blue"], tags=("fruit",)))
    leaf_centers = [(-0.04, -0.045, 0.09), (0.0, 0.05, 0.11), (0.07, -0.02, 0.13)]
    for i, center in enumerate(leaf_centers):
        asset.add(leaf(f"leaf_{i}", center, (0.07, 0.12, 0.012), COLORS["leaf_light"] if i == 0 else COLORS["leaf"], rotation=(0.8, 0.1 * i, -0.4 + i * 0.35), tags=("leaf",)))
    return asset


def build_spear(profile: AssetProfile) -> ProxyAsset:
    asset = ProxyAsset(profile.asset_id)
    asset.add(cylinder_between("shaft", (0, 0, 0.0), (0, 0, 1.66), 0.025, COLORS["wood"], tags=("shaft", "ground")))
    asset.add(wedge("flaked_head", (0, 0, 1.78), (0.22, 0.075, 0.34), COLORS["flint"], tags=("head",)))
    asset.parts.extend(ring_parts("lashing", (0, 0, 1.57), 0.033, 0.005, 12, COLORS["rope"], axis="z"))
    return asset


def build_bow(profile: AssetProfile) -> ProxyAsset:
    asset = ProxyAsset(profile.asset_id)
    # One continuous C-shaped stave. The previous draft accidentally formed a closed loop.
    points = [
        (-0.03, 0.0, 0.03), (0.04, 0.0, 0.18), (0.12, 0.0, 0.38),
        (0.17, 0.0, 0.56), (0.12, 0.0, 0.76), (0.04, 0.0, 0.96), (-0.03, 0.0, 1.12),
    ]
    for i in range(len(points) - 1):
        center_bias = 1.0 - abs(i - 2.5) / 2.5
        radius = 0.014 + 0.010 * max(center_bias, 0.0)
        asset.add(cylinder_between(f"stave_{i}", points[i], points[i + 1], radius, COLORS["wood"], tags=("stave", "ground" if i == 0 else "")))
    asset.add(cylinder_between("string", (-0.03, 0.012, 0.04), (-0.03, 0.012, 1.11), 0.0025, COLORS["rope"], sections=6, tags=("string",)))
    asset.add(box("grip", (0.17, 0, 0.56), (0.06, 0.05, 0.15), COLORS["hide"], tags=("grip",)))
    return asset


def build_axe(profile: AssetProfile) -> ProxyAsset:
    asset = ProxyAsset(profile.asset_id)
    asset.add(cylinder_between("haft", (0, 0, 0), (0, 0, 0.88), 0.035, COLORS["wood"], tags=("shaft", "ground")))
    asset.add(wedge("stone_head", (0.04, 0.0, 0.80), (0.34, 0.14, 0.20), COLORS["flint"], rotation=(0, 0.15, 0.05), tags=("head",)))
    asset.parts.extend(ring_parts("lashing", (0, 0, 0.74), 0.045, 0.006, 12, COLORS["rope"], axis="z"))
    return asset


def build_campfire(profile: AssetProfile) -> ProxyAsset:
    asset = ProxyAsset(profile.asset_id)
    for i in range(14):
        ang = i / 14 * math.tau
        asset.add(ellipsoid(f"stone_{i}", (math.cos(ang) * 0.48, math.sin(ang) * 0.48, 0.08), (0.13, 0.11, 0.08), COLORS["stone"], tags=("ring_stone", "ground")))
    for i in range(6):
        ang = i / 6 * math.tau
        asset.add(cylinder_between(f"fuel_{i}", (math.cos(ang) * 0.20, math.sin(ang) * 0.20, 0.08), (0, 0, 0.50), 0.035, COLORS["bark"], tags=("fuel", "ground")))
    asset.add(ellipsoid("coal_bed", (0, 0, 0.05), (0.27, 0.23, 0.05), COLORS["charcoal"], tags=("ember_bed", "ground")))
    asset.add(cone_between("flame_outer", (0, 0, 0.18), (0, 0, 0.82), 0.18, COLORS["fire"], tags=("flame",)))
    asset.add(cone_between("flame_inner", (0.02, 0, 0.24), (0.02, 0, 0.68), 0.10, COLORS["fire_yellow"], tags=("flame",)))
    return asset


def build_tent(profile: AssetProfile) -> ProxyAsset:
    asset = ProxyAsset(profile.asset_id)
    front_x, rear_x = -0.88, 0.88
    ridge_z = 1.46
    eave_y, eave_z = 0.82, 0.055
    roof_angle = math.atan2(eave_y, ridge_z - eave_z)

    # Real A-frame supports with wide feet, not almost vertical posts.
    for side_name, x in (("front", front_x), ("rear", rear_x)):
        frame_x = x - 0.055 if side_name == "front" else x + 0.055
        asset.add(cylinder_between(f"{side_name}_support_l", (frame_x, -eave_y, 0), (frame_x, -0.035, ridge_z - 0.055), 0.060, COLORS["bark"], tags=("frame", "ground")))
        asset.add(cylinder_between(f"{side_name}_support_r", (frame_x, eave_y, 0), (frame_x, 0.035, ridge_z - 0.055), 0.060, COLORS["bark"], tags=("frame", "ground")))

    asset.add(cylinder_between("ridgepole", (front_x - 0.10, 0, ridge_z), (rear_x + 0.10, 0, ridge_z), 0.048, COLORS["bark"], tags=("ridgepole",)))
    for side_sign in (-1, 1):
        asset.add(cylinder_between(f"eave_rail_{side_sign}", (front_x - 0.04, side_sign * eave_y, eave_z + 0.03), (rear_x + 0.04, side_sign * eave_y, eave_z + 0.03), 0.032, COLORS["wood"], tags=("eave_rail", "ground")))

    rafter_xs = (-0.80, -0.48, -0.16, 0.16, 0.48, 0.80)
    for i, x in enumerate(rafter_xs):
        asset.add(cylinder_between(f"rafter_l_{i}", (x, 0, ridge_z - 0.015), (x, -eave_y, eave_z), 0.026, COLORS["wood"], tags=("rafter", "ground")))
        asset.add(cylinder_between(f"rafter_r_{i}", (x, 0, ridge_z - 0.015), (x, eave_y, eave_z), 0.026, COLORS["wood"], tags=("rafter", "ground")))

    for side_sign in (-1, 1):
        for row_index, t in enumerate((0.40, 0.70)):
            y = side_sign * eave_y * (1.0 - t) * 0.93
            z = eave_z + (ridge_z - eave_z) * t - 0.045
            asset.add(cylinder_between(f"purlin_{side_sign}_{row_index}", (front_x - 0.03, y, z), (rear_x + 0.03, y, z), 0.021, COLORS["wood"], tags=("purlin",)))

    row_t_values = (0.15, 0.39, 0.63, 0.86)
    column_x_values = (-0.78, -0.47, -0.16, 0.15, 0.46, 0.77)
    for side_sign in (-1, 1):
        for row, t in enumerate(row_t_values):
            y = side_sign * eave_y * (1.0 - t)
            z = eave_z + (ridge_z - eave_z) * t
            for col, x in enumerate(column_x_values):
                color = COLORS["hide"] if side_sign > 0 and (row, col) in {(1, 4), (2, 1), (3, 3)} else COLORS["bark"]
                asset.add(box(
                    f"roof_cover_{side_sign}_{row}_{col}",
                    (x, y, z),
                    (0.38, 0.020, 0.54),
                    color,
                    rotation=(side_sign * roof_angle, 0.0, math.radians(((row + col) % 3 - 1) * 1.5)),
                    tags=("cover",),
                ))

    # Closed rear gable, open front entrance.
    vertices = np.array([
        [rear_x - 0.015, -0.72, 0.04], [rear_x - 0.015, 0.72, 0.04], [rear_x - 0.015, 0.0, ridge_z - 0.08],
        [rear_x + 0.015, -0.72, 0.04], [rear_x + 0.015, 0.72, 0.04], [rear_x + 0.015, 0.0, ridge_z - 0.08],
    ], dtype=float)
    faces = np.array([
        [0, 1, 2], [5, 4, 3], [0, 3, 4], [0, 4, 1], [1, 4, 5], [1, 5, 2], [2, 5, 3], [2, 3, 0]
    ], dtype=int)
    rear_mesh = trimesh.Trimesh(vertices=vertices, faces=faces, process=False)
    asset.add(ProxyPart("rear_gable", rear_mesh, COLORS["hide"], ("rear_closure",)))

    asset.add(box("bed", (0.12, 0.0, 0.055), (1.15, 0.72, 0.075), COLORS["earth"], tags=("bed", "ground")))
    asset.add(cylinder_between("front_threshold", (front_x + 0.12, -0.52, 0.055), (front_x + 0.12, 0.52, 0.055), 0.030, COLORS["wood"], tags=("threshold", "ground")))
    return asset


def build_trap(profile: AssetProfile) -> ProxyAsset:
    asset = ProxyAsset(profile.asset_id)
    asset.add(cylinder_between("deadfall_weight", (-0.58, -0.02, 0.18), (0.46, 0.02, 0.42), 0.11, COLORS["bark"], tags=("weight",)))
    asset.add(ellipsoid("back_stop", (-0.52, -0.15, 0.10), (0.18, 0.15, 0.10), COLORS["stone"], tags=("ground",)))
    asset.add(cylinder_between("upright", (0.18, 0, 0.0), (0.18, 0, 0.34), 0.020, COLORS["wood"], tags=("trigger", "ground")))
    asset.add(cylinder_between("diagonal", (0.18, 0, 0.34), (-0.02, 0, 0.16), 0.014, COLORS["wood"], tags=("trigger",)))
    asset.add(cylinder_between("bait_stick", (0.18, 0, 0.10), (0.38, 0, 0.10), 0.009, COLORS["wood"], tags=("bait_stick",)))
    asset.add(ellipsoid("bait", (0.42, 0, 0.11), (0.045, 0.035, 0.030), COLORS["berry_red"], tags=("bait",)))
    return asset


def build_berry_bush(profile: AssetProfile) -> ProxyAsset:
    rng = _seed(profile.asset_id)
    asset = ProxyAsset(profile.asset_id)
    anchors: List[Tuple[float, float, float]] = []
    for i in range(11):
        ang = i / 11 * math.tau + rng.uniform(-0.20, 0.20)
        base = (rng.uniform(-0.05, 0.05), rng.uniform(-0.05, 0.05), 0.0)
        tip = (math.cos(ang) * rng.uniform(0.34, 0.58), math.sin(ang) * rng.uniform(0.34, 0.58), rng.uniform(0.56, 1.02))
        asset.add(cylinder_between(f"cane_{i}", base, tip, rng.uniform(0.012, 0.020), COLORS["wood"], tags=("stem", "ground")))
        anchors.append(tip)
        mid = tuple(np.asarray(base) * 0.35 + np.asarray(tip) * 0.65)
        side = (mid[0] + math.cos(ang + 0.6) * 0.18, mid[1] + math.sin(ang + 0.6) * 0.18, mid[2] + 0.08)
        asset.add(cylinder_between(f"side_{i}", mid, side, 0.008, COLORS["wood"], tags=("stem",)))
        anchors.append(side)
    for i, anchor in enumerate(anchors):
        for j in range(5):
            ang = j / 5 * math.tau + rng.uniform(-0.25, 0.25)
            center = (anchor[0] + math.cos(ang) * 0.08, anchor[1] + math.sin(ang) * 0.08, anchor[2] + rng.uniform(-0.04, 0.04))
            asset.add(leaf(f"leaf_{i}_{j}", center, (0.07, 0.13, 0.012), COLORS["leaf_light"] if j % 2 == 0 else COLORS["leaf"], rotation=(rng.uniform(0.4, 1.1), rng.uniform(-0.3, 0.3), ang), tags=("leaf",)))
        if i % 2 == 0:
            for j in range(3):
                berry_center = (anchor[0] + (j - 1) * 0.025, anchor[1] + 0.02 * j, anchor[2] - 0.06 - j * 0.012)
                asset.add(cylinder_between(f"pedicel_{i}_{j}", anchor, berry_center, 0.0025, COLORS["leaf"], tags=("pedicel",)))
                asset.add(ellipsoid(f"berry_{i}_{j}", berry_center, (0.025, 0.025, 0.026), COLORS["berry_red"] if j % 2 == 0 else COLORS["berry_blue"], tags=("fruit",)))
    return asset


def build_conifer(profile: AssetProfile) -> ProxyAsset:
    rng = _seed(profile.asset_id)
    asset = ProxyAsset(profile.asset_id)
    h = 5.0
    asset.add(cylinder_between("trunk", (0, 0, 0), (0, 0, h), 0.16, COLORS["bark"], tags=("trunk", "ground")))
    for tier in range(8):
        z = 4.35 - tier * 0.50
        radius = 0.42 + tier * 0.12
        count = 7 + tier // 2
        for i in range(count):
            ang = i / count * math.tau + (0.23 if tier % 2 else 0)
            end = (math.cos(ang) * radius, math.sin(ang) * radius, z - rng.uniform(0.08, 0.20))
            asset.add(cylinder_between(f"bough_{tier}_{i}", (0, 0, z), end, 0.024, COLORS["wood"], tags=("branch",)))
            for frac in (0.50, 0.78, 1.0):
                p = np.asarray((0, 0, z)) * (1 - frac) + np.asarray(end) * frac
                asset.add(ellipsoid(f"needle_{tier}_{i}_{frac}", p, (0.20, 0.11, 0.10), COLORS["needle"], tags=("foliage",)))
    asset.add(cone_between("leader", (0, 0, 4.80), (0, 0, 5.35), 0.10, COLORS["needle"], tags=("leader",)))
    return asset


def build_small_prey(profile: AssetProfile) -> ProxyAsset:
    asset = ProxyAsset(profile.asset_id)
    asset.add(ellipsoid("body", (0, 0, 0.30), (0.34, 0.18, 0.20), COLORS["fur"], tags=("body",)))
    asset.add(ellipsoid("rump", (-0.23, 0, 0.30), (0.19, 0.18, 0.18), COLORS["fur"], tags=("body",)))
    asset.add(ellipsoid("chest", (0.22, 0, 0.31), (0.16, 0.15, 0.17), COLORS["fur"], tags=("body",)))
    asset.add(ellipsoid("head", (0.42, 0, 0.42), (0.13, 0.11, 0.13), COLORS["fur"], tags=("head",)))
    asset.add(ellipsoid("muzzle", (0.53, 0, 0.39), (0.07, 0.07, 0.055), COLORS["fur_light"], tags=("muzzle",)))
    for sign in (-1, 1):
        asset.add(ellipsoid(f"ear_{sign}", (0.39, sign * 0.035, 0.59), (0.040, 0.024, 0.17), COLORS["fur"], tags=("ear",)))
    for idx, y in enumerate((-0.09, 0.09)):
        asset.add(cylinder_between(f"hind_thigh_{idx}", (-0.16, y, 0.25), (-0.20, y, 0.13), 0.055, COLORS["fur"], tags=("hind_leg",)))
        asset.add(cylinder_between(f"hind_shin_{idx}", (-0.20, y, 0.13), (-0.06, y, 0.035), 0.034, COLORS["fur"], tags=("hind_leg", "ground")))
    for idx, y in enumerate((-0.06, 0.06)):
        asset.add(cylinder_between(f"front_leg_{idx}", (0.20, y, 0.25), (0.23, y, 0.03), 0.022, COLORS["fur"], tags=("front_leg", "ground")))
    asset.add(ellipsoid("tail", (-0.38, 0, 0.33), (0.055, 0.055, 0.055), COLORS["fur_light"], tags=("tail",)))
    return asset


def build_old_tree(profile: AssetProfile) -> ProxyAsset:
    rng = _seed(profile.asset_id)
    asset = ProxyAsset(profile.asset_id)
    asset.add(cylinder_between("massive_trunk", (0, 0, 0), (0, 0, 5.6), 0.55, COLORS["bark"], tags=("trunk", "ground")))
    for i in range(8):
        ang = i / 8 * math.tau
        asset.add(cylinder_between(f"root_{i}", (0, 0, 0.18), (math.cos(ang) * 1.55, math.sin(ang) * 1.55, 0.02), 0.13, COLORS["bark"], tags=("root", "ground")))
    anchors: List[Tuple[float, float, float]] = []
    for level, z in enumerate((1.7, 2.3, 2.9, 3.5, 4.1, 4.7)):
        count = 3 + (level % 2)
        for i in range(count):
            ang = i / count * math.tau + rng.uniform(-0.22, 0.22)
            end = (math.cos(ang) * rng.uniform(1.35, 2.35), math.sin(ang) * rng.uniform(1.35, 2.35), z + rng.uniform(0.2, 0.6))
            asset.add(cylinder_between(f"branch_{level}_{i}", (0, 0, z), end, 0.10 - level * 0.01, COLORS["bark"], tags=("branch",)))
            anchors.append(end)
    for i, anchor in enumerate(anchors):
        asset.add(ellipsoid(f"crown_{i}", anchor, (0.58, 0.54, 0.46), COLORS["leaf"] if i % 2 else COLORS["leaf_light"], tags=("foliage",)))
    return asset


BUILDERS: Dict[str, Callable[[AssetProfile], ProxyAsset]] = {
    "wood": build_wood,
    "berries": build_berries,
    "spear": build_spear,
    "bow": build_bow,
    "axe": build_axe,
    "campfire": build_campfire,
    "tent": build_tent,
    "trap": build_trap,
    "berry_bush_a": build_berry_bush,
    "conifer_tree_b": build_conifer,
    "small_prey": build_small_prey,
    "old_tree_landmark": build_old_tree,
}


# -----------------------------------------------------------------------------
# QA
# -----------------------------------------------------------------------------


def tags(asset: ProxyAsset) -> Dict[str, int]:
    counts: Dict[str, int] = {}
    for part in asset.parts:
        for tag in part.tags:
            if tag:
                counts[tag] = counts.get(tag, 0) + 1
    return counts


def qa_asset(asset: ProxyAsset, profile: AssetProfile) -> Dict:
    ext = asset.extents
    target = np.asarray(profile.target_size_m)
    ratio = ext / np.maximum(target, 1e-6)
    min_z = float(asset.bounds[0, 2])
    t = tags(asset)
    checks: List[Tuple[str, bool, str]] = []
    checks.append(("ground_contact", min_z <= 0.05, f"min_z={min_z:.3f}"))
    checks.append(("scale_envelope", bool(np.all((ratio > 0.45) & (ratio < 1.75))), f"ratio={ratio.round(2).tolist()}"))
    checks.append(("part_count", len(asset.parts) >= 3, f"parts={len(asset.parts)}"))

    required_by_asset = {
        "wood": {"wood": 5, "rope": 2, "cut_face": 4},
        "berries": {"stem": 2, "pedicel": 4, "fruit": 6, "leaf": 2},
        "spear": {"shaft": 1, "head": 1, "rope": 4},
        "bow": {"stave": 6, "string": 1, "grip": 1},
        "axe": {"shaft": 1, "head": 1, "rope": 4},
        "campfire": {"ring_stone": 10, "fuel": 4, "ember_bed": 1, "flame": 1},
        "tent": {"frame": 4, "ridgepole": 1, "rafter": 12, "cover": 48, "purlin": 4, "eave_rail": 2, "rear_closure": 1, "bed": 1},
        "trap": {"weight": 1, "trigger": 2, "bait": 1},
        "berry_bush_a": {"stem": 12, "leaf": 30, "pedicel": 6, "fruit": 6},
        "conifer_tree_b": {"trunk": 1, "branch": 35, "foliage": 70, "leader": 1},
        "small_prey": {"body": 3, "head": 1, "ear": 2, "hind_leg": 4, "front_leg": 2},
        "old_tree_landmark": {"trunk": 1, "root": 6, "branch": 12, "foliage": 12},
    }
    for tag_name, minimum in required_by_asset.get(asset.asset_id, {}).items():
        actual = t.get(tag_name, 0)
        checks.append((f"required_{tag_name}", actual >= minimum, f"actual={actual}, required={minimum}"))

    passed = sum(1 for _, ok, _ in checks if ok)
    score = round(passed / len(checks) * 10.0, 2)
    return {
        "asset_id": asset.asset_id,
        "parts": len(asset.parts),
        "extents": ext.round(4).tolist(),
        "target_size": target.round(4).tolist(),
        "min_z": round(min_z, 4),
        "tags": t,
        "checks": [{"name": name, "pass": ok, "detail": detail} for name, ok, detail in checks],
        "score_10": score,
    }


# -----------------------------------------------------------------------------
# Rendering
# -----------------------------------------------------------------------------


def _render_asset(ax, asset: ProxyAsset, title: str) -> None:
    for part in asset.parts:
        mesh = part.mesh
        faces = mesh.faces
        if len(faces) > 600:
            step = max(1, len(faces) // 600)
            faces = faces[::step]
        triangles = mesh.vertices[faces]
        coll = Poly3DCollection(triangles, linewidths=0.08, edgecolors=(0.08, 0.07, 0.05, 0.20))
        coll.set_facecolor((*part.color, 0.96))
        ax.add_collection3d(coll)

    bounds = asset.bounds
    center = bounds.mean(axis=0)
    ext = bounds[1] - bounds[0]
    radius = max(ext.max() * 0.62, 0.25)
    ax.set_xlim(center[0] - radius, center[0] + radius)
    ax.set_ylim(center[1] - radius, center[1] + radius)
    ax.set_zlim(max(-0.03, center[2] - radius), center[2] + radius)
    ax.view_init(elev=26, azim=-48)
    ax.set_proj_type("ortho")
    ax.set_axis_off()
    ax.set_title(title, fontsize=9, pad=2)


def render_contact_sheet(assets: Sequence[ProxyAsset], profiles: Dict[str, AssetProfile], path: Path) -> None:
    cols = 4
    rows = math.ceil(len(assets) / cols)
    fig = plt.figure(figsize=(16, rows * 4.0), dpi=150)
    fig.patch.set_facecolor((0.92, 0.90, 0.85))
    for i, asset in enumerate(assets):
        ax = fig.add_subplot(rows, cols, i + 1, projection="3d")
        ax.set_facecolor((0.92, 0.90, 0.85))
        _render_asset(ax, asset, f"{asset.asset_id}\n{profiles[asset.asset_id].name}")
    fig.suptitle("Apex Shift Generator v10.1 — tent geometry correction", fontsize=16, y=0.995)
    plt.subplots_adjust(left=0.01, right=0.99, bottom=0.01, top=0.96, wspace=0.02, hspace=0.08)
    fig.savefig(path, bbox_inches="tight", facecolor=fig.get_facecolor())
    plt.close(fig)


def render_individual(asset: ProxyAsset, profile: AssetProfile, path: Path) -> None:
    fig = plt.figure(figsize=(6, 6), dpi=170)
    fig.patch.set_facecolor((0.92, 0.90, 0.85))
    ax = fig.add_subplot(1, 1, 1, projection="3d")
    ax.set_facecolor((0.92, 0.90, 0.85))
    _render_asset(ax, asset, f"{asset.asset_id} — {profile.name}")
    fig.savefig(path, bbox_inches="tight", facecolor=fig.get_facecolor())
    plt.close(fig)


def write_markdown_report(results: Sequence[Dict], path: Path) -> None:
    lines = [
        "# Apex Shift Generator v10.1 — raport symulacji proxy",
        "",
        "Symulacja używa lekkiego renderera zastępczego opartego o te same proporcje i reguły kompozycyjne co generator Blendera. Nie jest renderem z `bpy`, ale pozwala sprawdzić sylwetkę, skalę, liczbę części i obecność elementów wymaganych przez biblię wizualną.",
        "",
        "| Asset | Wynik /10 | Części | Rozmiar proxy | Rozmiar docelowy |",
        "|---|---:|---:|---|---|",
    ]
    for result in results:
        lines.append(f"| `{result['asset_id']}` | {result['score_10']:.2f} | {result['parts']} | `{tuple(result['extents'])}` | `{tuple(result['target_size'])}` |")
    lines += ["", "## Kontrole szczegółowe", ""]
    for result in results:
        lines.append(f"### `{result['asset_id']}` — {result['score_10']:.2f}/10")
        lines.append("")
        for check in result["checks"]:
            marker = "PASS" if check["pass"] else "FAIL"
            lines.append(f"- **{marker}** `{check['name']}` — {check['detail']}")
        lines.append("")
    path.write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    profiles_list = load_profiles()
    coverage_issues = validate_profile_coverage(profiles_list)
    if coverage_issues:
        raise RuntimeError("; ".join(coverage_issues))
    profiles = {p.asset_id: p for p in profiles_list}

    assets: List[ProxyAsset] = []
    results: List[Dict] = []
    for asset_id in SIMULATION_ASSET_IDS:
        builder = BUILDERS[asset_id]
        asset = builder(profiles[asset_id])
        assets.append(asset)
        results.append(qa_asset(asset, profiles[asset_id]))
        render_individual(asset, profiles[asset_id], OUT / f"{asset_id}_proxy.png")

    render_contact_sheet(assets, profiles, OUT / "apex_shift_v10_proxy_contact_sheet.png")
    (OUT / "apex_shift_v10_proxy_qa.json").write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
    write_markdown_report(results, OUT / "apex_shift_v10_proxy_qa.md")

    avg = sum(item["score_10"] for item in results) / len(results)
    print(f"Rendered {len(assets)} proxy assets")
    print(f"Average QA score: {avg:.2f}/10")
    for item in results:
        failed = [check["name"] for check in item["checks"] if not check["pass"]]
        print(f"{item['asset_id']:<22} {item['score_10']:>5.2f} failures={failed}")


if __name__ == "__main__":
    main()
