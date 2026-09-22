"""MCP-friendly orchestration for the Apex Shift Blender asset pipeline.

This module is intentionally small. Blender MCP should execute short snippets that
import and call these functions instead of receiving large arbitrary bpy scripts.
"""

from __future__ import annotations

import argparse
import json
import sys
from dataclasses import asdict
from pathlib import Path
from typing import Iterable, Sequence

import bpy

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parents[1]
LAST_RUN_PATH = REPO_ROOT / "Docs" / "art" / "blender-mcp-agent-last-run.json"
VALIDATION_REPORT_PATH = REPO_ROOT / "Docs" / "art" / "bushcraft-validation-report.md"

if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

from bushcraft_asset_generator import (  # noqa: E402
    GENERATOR_MAP,
    cleanup_generated_collection,
    generate_all_assets,
    generate_manifest,
)
from bushcraft_render_validation import validate_asset_entry  # noqa: E402

PREMIUM_REFERENCE_ASSETS: tuple[str, ...] = ("wood", "spear", "campfire")


def available_assets() -> dict[str, list[str]]:
    """Return supported asset IDs grouped by category."""
    grouped: dict[str, list[str]] = {"item": [], "placeable": [], "resource": []}
    for asset_id, (category, *_rest) in GENERATOR_MAP.items():
        grouped.setdefault(category, []).append(asset_id)
    return grouped


def inspect_scene() -> dict[str, object]:
    """Return a compact Blender preflight snapshot suitable for MCP output."""
    scene = bpy.context.scene
    filepath = bpy.data.filepath or None
    return {
        "blender_version": bpy.app.version_string,
        "scene": scene.name if scene else None,
        "object_count": len(bpy.data.objects),
        "active_object": bpy.context.active_object.name if bpy.context.active_object else None,
        "blend_filepath": filepath,
        "is_saved": bool(filepath),
        "is_dirty": bool(getattr(bpy.data, "is_dirty", False)),
    }


def _normalize_asset_ids(asset_ids: Iterable[str] | None, all_assets: bool) -> list[str]:
    if all_assets:
        selected = list(GENERATOR_MAP.keys())
    elif asset_ids is None:
        selected = list(PREMIUM_REFERENCE_ASSETS)
    else:
        selected = list(dict.fromkeys(asset_ids))

    if not selected:
        raise ValueError("At least one asset ID is required.")

    unknown = [asset_id for asset_id in selected if asset_id not in GENERATOR_MAP]
    if unknown:
        allowed = ", ".join(GENERATOR_MAP.keys())
        raise ValueError(f"Unknown asset IDs: {', '.join(unknown)}. Allowed: {allowed}")
    return selected


def _write_validation_report(results: Sequence[dict[str, object]]) -> str:
    passing = sum(1 for result in results if result.get("status") == "pass")
    lines = [
        "# Bushcraft Validation Report",
        "",
        "Validation captured by the Blender MCP asset agent while each asset was active in the scene.",
        "",
        "## Summary",
        f"- Total assets: {len(results)}",
        f"- Passing assets: {passing}",
        f"- Assets needing polish: {len(results) - passing}",
        "",
        "## Results",
    ]
    for result in results:
        lines.extend(
            [
                f"### {result['asset_id']}",
                f"- Status: `{result['status']}`",
            ]
        )
        checks = result.get("checks", {})
        if isinstance(checks, dict):
            for key, value in checks.items():
                lines.append(f"- {key}: `{value}`")
        elif isinstance(checks, list):
            lines.extend(f"- {item}" for item in checks)
        lines.append("")

    VALIDATION_REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    VALIDATION_REPORT_PATH.write_text("\n".join(lines), encoding="utf-8")
    return str(VALIDATION_REPORT_PATH.as_posix())


def run_asset_job(
    asset_ids: Iterable[str] | None = None,
    *,
    all_assets: bool = False,
    clean_between_assets: bool = True,
) -> dict[str, object]:
    """Generate, export and validate requested assets inside Blender.

    The existing save flow removes previously generated scene objects. Therefore
    each asset is generated and validated separately, then the combined manifest
    and validation report are written at the end.
    """
    selected = _normalize_asset_ids(asset_ids, all_assets)
    preflight = inspect_scene()
    records = []
    validation_results: list[dict[str, object]] = []

    for asset_id in selected:
        if clean_between_assets:
            cleanup_generated_collection("BushcraftGenerated")
        generated = generate_all_assets([asset_id])
        if len(generated) != 1:
            raise RuntimeError(f"Expected one generated record for {asset_id}, got {len(generated)}.")
        record = generated[0]
        records.append(record)
        validation_results.append(validate_asset_entry(asdict(record)))

    generate_manifest(records)
    validation_report = _write_validation_report(validation_results)
    payload: dict[str, object] = {
        "status": "pass" if all(item.get("status") == "pass" for item in validation_results) else "needs_polish",
        "requested_assets": selected,
        "preflight": preflight,
        "records": [asdict(record) for record in records],
        "validation": validation_results,
        "validation_report": validation_report,
    }
    LAST_RUN_PATH.parent.mkdir(parents=True, exist_ok=True)
    LAST_RUN_PATH.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    return payload


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Generate Apex Shift assets from Blender.")
    parser.add_argument("asset_ids", nargs="*", help="Asset IDs from GENERATOR_MAP.")
    parser.add_argument("--all", action="store_true", help="Generate all supported assets.")
    parser.add_argument("--list", action="store_true", help="List supported assets and exit.")
    parser.add_argument(
        "--keep-generated",
        action="store_true",
        help="Do not clear the BushcraftGenerated collection between assets.",
    )
    return parser


def _blender_script_args(argv: Sequence[str] | None) -> list[str]:
    if argv is not None:
        return list(argv)
    raw = list(sys.argv)
    if "--" in raw:
        return raw[raw.index("--") + 1 :]
    return []


def main(argv: Sequence[str] | None = None) -> int:
    parser = _build_parser()
    args = parser.parse_args(_blender_script_args(argv))
    if args.list:
        print(json.dumps(available_assets(), indent=2))
        return 0

    result = run_asset_job(
        args.asset_ids or None,
        all_assets=args.all,
        clean_between_assets=not args.keep_generated,
    )
    print(json.dumps(result, indent=2))
    return 0 if result["status"] == "pass" else 2


if __name__ == "__main__":
    raise SystemExit(main())
