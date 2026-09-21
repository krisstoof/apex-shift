from __future__ import annotations

import ast
import json
from pathlib import Path

from apex_shift_profiles_v10 import load_profiles, validate_profile_coverage

ROOT = Path(__file__).resolve().parent


def main() -> None:
    profiles = load_profiles()
    issues = validate_profile_coverage(profiles)
    if issues:
        raise RuntimeError("; ".join(issues))

    generator_path = ROOT / "apex_shift_blender_generator_v10_1.py"
    module = ast.parse(generator_path.read_text(encoding="utf-8"))
    family_keys = []
    for node in ast.walk(module):
        if isinstance(node, ast.AnnAssign) and isinstance(node.target, ast.Name) and node.target.id == "FAMILY_GENERATORS" and isinstance(node.value, ast.Dict):
            family_keys = [key.value for key in node.value.keys if isinstance(key, ast.Constant)]
    spec_families = sorted({profile.family for profile in profiles})
    missing = sorted(set(spec_families) - set(family_keys))
    if missing:
        raise RuntimeError("Missing family generators: " + ", ".join(missing))

    qa_path = ROOT / "simulation_output_v10_1" / "apex_shift_v10_proxy_qa.json"
    qa = json.loads(qa_path.read_text(encoding="utf-8"))
    failed = [item["asset_id"] for item in qa if any(not check["pass"] for check in item["checks"])]
    if failed:
        raise RuntimeError("Proxy QA failures: " + ", ".join(failed))

    print(f"OK: profiles={len(profiles)}, families={len(spec_families)}, simulated={len(qa)}")


if __name__ == "__main__":
    main()
