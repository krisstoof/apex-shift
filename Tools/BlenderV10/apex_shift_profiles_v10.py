from __future__ import annotations

import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Tuple

ROOT = Path(__file__).resolve().parent
DEFAULT_SPEC_PATH = ROOT / "apex_shift_asset_visual_specs_v5.json"


@dataclass(frozen=True)
class AssetProfile:
    asset_id: str
    name: str
    category: str
    family: str
    role: str
    scale_text: str
    variant_text: str
    states_text: str
    technical_text: str
    reference_title: str
    reference_analysis: str
    family_structure: str
    family_materials: str
    family_forbidden: str
    reference_source_page: str
    reference_image_url: str
    variant_index: int
    target_size_m: Tuple[float, float, float]


CATEGORY_DIRS: Dict[str, str] = {
    "Przedmiot": "Items",
    "Narzędzie": "Tools",
    "Broń": "Weapons",
    "Amunicja": "Ammo",
    "Konstrukcja": "Placeables",
    "Roślinność": "Vegetation",
    "Zasób świata": "WorldResources",
    "Stworzenie": "Creatures",
    "Landmark": "Landmarks",
}


# Family-specific target dimensions. These are gameplay-oriented display sizes,
# not exact botanical or zoological measurements. Variant multipliers are
# applied later and are intentionally conservative.
FAMILY_BASE_SIZE: Dict[str, Tuple[float, float, float]] = {
    "wood": (0.80, 0.42, 0.28),
    "stone_pickup": (0.46, 0.34, 0.22),
    "fiber": (0.24, 0.20, 0.72),
    "grass_pickup": (0.34, 0.32, 0.42),
    "meat": (0.36, 0.24, 0.18),
    "hide": (0.60, 0.46, 0.10),
    "bone": (0.66, 0.18, 0.16),
    "berries_pickup": (0.30, 0.24, 0.20),
    "kindling": (0.62, 0.30, 0.22),
    "flint": (0.28, 0.20, 0.14),
    "reed_bundle": (0.24, 0.22, 0.90),
    "torch": (0.20, 0.20, 1.28),
    "spear": (0.18, 0.16, 1.90),
    "bow": (0.28, 0.10, 1.34),
    "arrow": (0.10, 0.08, 1.00),
    "arrow_bundle": (0.22, 0.18, 1.02),
    "axe": (0.34, 0.18, 0.98),
    "pickaxe": (0.68, 0.20, 1.08),
    "campfire": (1.12, 1.12, 0.86),
    "campfire_burned": (1.08, 1.08, 0.28),
    "storage_box": (1.10, 0.72, 0.78),
    "tent": (2.35, 1.85, 1.62),
    "lean_to": (2.30, 1.55, 1.45),
    "wall": (2.90, 0.34, 1.88),
    "spike_barrier": (2.25, 0.72, 1.18),
    "deadfall": (1.30, 0.82, 0.62),
    "snare": (0.62, 0.52, 0.64),
    "tanning_rack": (1.26, 0.34, 1.56),
    "drying_rack": (1.26, 0.34, 1.46),
    "leafy_tree": (4.20, 4.00, 5.40),
    "conifer_tree": (3.60, 3.60, 5.80),
    "dry_tree": (2.50, 2.30, 4.40),
    "leafy_sapling": (1.10, 1.00, 1.65),
    "conifer_sapling": (0.92, 0.92, 1.55),
    "dry_sapling": (0.72, 0.70, 1.30),
    "rock_outcrop": (1.55, 1.32, 1.08),
    "rock_cluster": (1.05, 0.92, 0.72),
    "green_bush": (1.20, 1.15, 1.08),
    "forest_shrub": (1.02, 0.98, 0.88),
    "dry_bush": (1.00, 0.96, 0.92),
    "berry_bush": (1.30, 1.24, 1.16),
    "ground_patch": (0.62, 0.58, 0.48),
    "tall_grass": (0.72, 0.68, 1.06),
    "wildflowers": (0.74, 0.70, 0.64),
    "reeds": (0.82, 0.76, 1.18),
    "mushrooms": (0.46, 0.42, 0.24),
    "herbs": (0.52, 0.48, 0.30),
    "small_prey": (0.86, 0.40, 0.62),
    "grazer": (2.10, 0.82, 1.75),
    "varnak": (2.15, 0.86, 1.56),
    "old_tree": (5.20, 5.00, 7.00),
    "ruins": (4.20, 3.40, 2.10),
    "pond": (4.60, 4.10, 1.20),
    "camp": (5.20, 4.40, 2.10),
    "cave": (4.40, 3.60, 2.70),
}

VARIANT_SCALE: Dict[int, Tuple[float, float, float]] = {
    0: (1.00, 1.00, 1.00),
    1: (0.94, 0.96, 0.96),
    2: (1.04, 1.00, 1.06),
    3: (0.92, 1.08, 0.92),
    4: (1.10, 1.06, 1.10),
}


SIMULATION_ASSET_IDS: Tuple[str, ...] = (
    "wood",
    "berries",
    "spear",
    "bow",
    "axe",
    "campfire",
    "tent",
    "trap",
    "berry_bush_a",
    "conifer_tree_b",
    "small_prey",
    "old_tree_landmark",
)


def load_raw_specs(path: Optional[Path] = None) -> Dict:
    spec_path = Path(path) if path else DEFAULT_SPEC_PATH
    with spec_path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def infer_variant_index(asset_id: str) -> int:
    suffix = re.search(r"_([a-d])$", asset_id)
    if suffix:
        return ord(suffix.group(1)) - ord("a") + 1
    if asset_id.endswith("_heavy") or "large" in asset_id:
        return 4
    if asset_id.endswith("_short") or "small" in asset_id:
        return 1
    if asset_id.endswith("_burned") or asset_id.endswith("_unlit"):
        return 2
    return 0


def infer_target_size(family: str, asset_id: str, variant_index: int) -> Tuple[float, float, float]:
    base = FAMILY_BASE_SIZE.get(family, (1.0, 1.0, 1.0))
    mult = VARIANT_SCALE.get(variant_index, (1.0, 1.0, 1.0))

    # Semantic overrides are stronger than suffix multipliers.
    if asset_id.endswith("_heavy"):
        mult = (1.10, 1.08, 1.08)
    elif asset_id.endswith("_short"):
        mult = (0.84, 0.92, 0.84)
    elif "cluster_large" in asset_id:
        mult = (1.35, 1.30, 1.28)
    elif "cluster_small" in asset_id:
        mult = (0.72, 0.74, 0.70)

    return tuple(round(base[i] * mult[i], 4) for i in range(3))


def load_profiles(path: Optional[Path] = None) -> List[AssetProfile]:
    raw = load_raw_specs(path)
    result: List[AssetProfile] = []
    for item in raw["assets"]:
        idx = infer_variant_index(item["asset_id"])
        ref = item.get("reference") or {}
        result.append(
            AssetProfile(
                asset_id=item["asset_id"],
                name=item.get("name", item["asset_id"]),
                category=item.get("category", "Unknown"),
                family=item.get("family", "unknown"),
                role=item.get("role", ""),
                scale_text=item.get("scale", ""),
                variant_text=item.get("variant", ""),
                states_text=item.get("states", ""),
                technical_text=item.get("technical", ""),
                reference_title=ref.get("title", ""),
                reference_analysis=ref.get("analysis", ""),
                family_structure=item.get("family_structure", ""),
                family_materials=item.get("family_materials", ""),
                family_forbidden=item.get("family_forbidden", ""),
                reference_source_page=item.get("reference_source_page", ""),
                reference_image_url=item.get("reference_image_url", ""),
                variant_index=idx,
                target_size_m=infer_target_size(item.get("family", "unknown"), item["asset_id"], idx),
            )
        )
    return result


def profiles_by_id(path: Optional[Path] = None) -> Dict[str, AssetProfile]:
    return {profile.asset_id: profile for profile in load_profiles(path)}


def validate_profile_coverage(profiles: Iterable[AssetProfile]) -> List[str]:
    problems: List[str] = []
    profiles = list(profiles)
    if len(profiles) != 98:
        problems.append(f"Expected 98 profiles, found {len(profiles)}")
    ids = [p.asset_id for p in profiles]
    duplicates = sorted({asset_id for asset_id in ids if ids.count(asset_id) > 1})
    if duplicates:
        problems.append(f"Duplicate asset ids: {', '.join(duplicates)}")
    missing_sizes = sorted(p.asset_id for p in profiles if p.family not in FAMILY_BASE_SIZE)
    if missing_sizes:
        problems.append("Missing family size profiles: " + ", ".join(missing_sizes))
    unknown_categories = sorted({p.category for p in profiles if p.category not in CATEGORY_DIRS})
    if unknown_categories:
        problems.append("Unknown categories: " + ", ".join(unknown_categories))
    return problems


if __name__ == "__main__":
    profiles = load_profiles()
    issues = validate_profile_coverage(profiles)
    print(f"Loaded profiles: {len(profiles)}")
    if issues:
        for issue in issues:
            print("ERROR:", issue)
        raise SystemExit(1)
    print("Profile validation OK")
