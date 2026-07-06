"""
Validation helpers for generated bushcraft assets.

This script focuses on technical checks that can be automated from Blender.
"""

from __future__ import annotations

import json
from pathlib import Path

import bpy


REPO_ROOT = Path(__file__).resolve().parents[2]
ASSET_ROOT = REPO_ROOT / "Assets" / "_Project" / "Art" / "Bushcraft"
REPORT_PATH = REPO_ROOT / "Docs" / "art" / "bushcraft-validation-report.md"
MANIFEST_PATH = ASSET_ROOT / "bushcraft_model_manifest.json"


def _load_manifest() -> dict:
    if not MANIFEST_PATH.exists():
        return {"assets": []}
    return json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))


def _logical_scale(obj: bpy.types.Object) -> bool:
    dims = obj.dimensions
    return all(0.01 < value < 20.0 for value in dims)


def _origin_is_reasonable(obj: bpy.types.Object) -> bool:
    z_values = [(obj.matrix_world @ v.co).z for v in obj.data.vertices] if obj.data.vertices else [0.0]
    if not z_values:
        return True
    base = min(z_values)
    return abs(obj.location.z - base) < 0.5


def _has_uv(obj: bpy.types.Object) -> bool:
    return isinstance(obj.data, bpy.types.Mesh) and len(obj.data.uv_layers) > 0


def _material_count_ok(obj: bpy.types.Object) -> bool:
    return len([slot for slot in obj.data.materials if slot]) >= 1


def _part_count_ok(obj: bpy.types.Object) -> bool:
    verts = len(obj.data.vertices)
    polys = len(obj.data.polygons)
    return verts >= 24 and polys >= 12


def _preview_exists(path: str) -> bool:
    return Path(path).exists()


def validate_asset_entry(entry: dict) -> dict:
    obj = bpy.data.objects.get(f"{entry['asset_id']}_stylized")
    if not obj or obj.type != "MESH":
        return {
            "asset_id": entry["asset_id"],
            "status": "missing_in_scene",
            "checks": ["Object not loaded in current Blender scene."],
        }
    checks = {
        "has_uv": _has_uv(obj),
        "has_material": _material_count_ok(obj),
        "origin_ok": _origin_is_reasonable(obj),
        "scale_ok": _logical_scale(obj),
        "part_density_ok": _part_count_ok(obj),
        "preview_exists": _preview_exists(entry.get("preview_path", "")),
        "obj_name_ok": obj.name == f"{entry['asset_id']}_stylized",
    }
    status = "pass" if all(checks.values()) else "needs_polish"
    return {"asset_id": entry["asset_id"], "status": status, "checks": checks}


def generate_validation_report() -> str:
    manifest = _load_manifest()
    results = [validate_asset_entry(entry) for entry in manifest.get("assets", [])]
    lines = [
        "# Bushcraft Validation Report",
        "",
        "Automated validation for stylized bushcraft assets generated from Blender.",
        "",
        "## Summary",
        f"- Total assets in manifest: {len(results)}",
        f"- Passing assets: {sum(1 for item in results if item['status'] == 'pass')}",
        f"- Assets needing polish: {sum(1 for item in results if item['status'] != 'pass')}",
        "",
        "## Results",
    ]
    for result in results:
        lines.append(f"### {result['asset_id']}")
        lines.append(f"- Status: `{result['status']}`")
        checks = result.get("checks")
        if isinstance(checks, dict):
            for key, value in checks.items():
                lines.append(f"- {key}: `{value}`")
        else:
            for item in checks or []:
                lines.append(f"- {item}")
        lines.append("")
    REPORT_PATH.write_text("\n".join(lines), encoding="utf-8")
    return str(REPORT_PATH.as_posix())


if __name__ == "__main__":
    print(generate_validation_report())
