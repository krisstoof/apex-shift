"""Safe Blender MCP orchestration for the canonical v10.1 asset generator."""

from __future__ import annotations

import json
import random
from dataclasses import asdict
from pathlib import Path
from typing import Iterable, Sequence

import bpy

from apex_shift_blender_generator_v10_1 import (
    DEFAULT_OUTPUT_ROOT,
    generate_one,
    write_reports,
)
from apex_shift_profiles_v10 import load_profiles, validate_profile_coverage

ROOT = Path(__file__).resolve().parent
LAST_RUN_PATH = ROOT.parent.parent / "Docs" / "art" / "blender-mcp-agent-last-run.json"


def available_assets() -> list[str]:
    """Return the explicit asset IDs supported by the v10 profile set."""
    profiles = load_profiles(ROOT / "apex_shift_asset_visual_specs_v5.json")
    return [profile.asset_id for profile in profiles]


def inspect_scene() -> dict[str, object]:
    scene = bpy.context.scene
    return {
        "blender_version": bpy.app.version_string,
        "scene": scene.name if scene else None,
        "object_count": len(bpy.data.objects),
        "active_object": bpy.context.active_object.name if bpy.context.active_object else None,
        "blend_filepath": bpy.data.filepath or None,
        "is_dirty": bool(getattr(bpy.data, "is_dirty", False)),
    }


def run_asset_job(
    asset_ids: Iterable[str] | None = None,
    *,
    all_assets: bool = False,
    output_root: Path | None = None,
    export_enabled: bool = True,
    preview_enabled: bool = True,
    seed: int = 27071992,
) -> dict[str, object]:
    """Generate and audit explicitly requested v10 assets in Blender."""
    profiles = load_profiles(ROOT / "apex_shift_asset_visual_specs_v5.json")
    issues = validate_profile_coverage(profiles)
    if issues:
        raise RuntimeError("Profile coverage failed: " + "; ".join(issues))

    requested = list(asset_ids) if asset_ids is not None else []
    selected_ids = {profile.asset_id for profile in profiles} if all_assets else set(requested)
    if not selected_ids:
        raise ValueError("Pass explicit asset IDs or all_assets=True.")
    unknown = sorted(selected_ids - {profile.asset_id for profile in profiles})
    if unknown:
        raise ValueError("Unknown asset IDs: " + ", ".join(unknown))

    selected = [profile for profile in profiles if profile.asset_id in selected_ids]
    output = (output_root or DEFAULT_OUTPUT_ROOT).resolve()
    module = __import__("apex_shift_blender_generator_v10_1")
    module.RNG = random.Random(seed)
    results = [generate_one(profile, output, export_enabled, preview_enabled) for profile in selected]
    write_reports(output, selected, results)
    payload = {
        "status": "pass" if all(not any(not check["pass"] for check in result["checks"]) for result in results) else "needs_polish",
        "requested_assets": [profile.asset_id for profile in selected],
        "preflight": inspect_scene(),
        "results": results,
        "output_root": output.as_posix(),
    }
    LAST_RUN_PATH.parent.mkdir(parents=True, exist_ok=True)
    LAST_RUN_PATH.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    return payload


def main(argv: Sequence[str] | None = None) -> int:
    import argparse

    parser = argparse.ArgumentParser(description="Generate Apex Shift v10.1 assets through Blender MCP.")
    parser.add_argument("asset_ids", nargs="*", help="Explicit IDs from the v10 profile set.")
    parser.add_argument("--all", action="store_true", help="Generate the full v10 profile set.")
    args = parser.parse_args(argv)
    result = run_asset_job(args.asset_ids or None, all_assets=args.all)
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if result["status"] == "pass" else 2


if __name__ == "__main__":
    raise SystemExit(main())
