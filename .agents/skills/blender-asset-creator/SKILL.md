---
name: blender-asset-creator
description: Create, refine, validate, render, and export Apex Shift bushcraft 3D assets through Blender MCP. Use for Blender, MCP, bpy, Unity model, item, resource, placeable, pivot, UV, material, FBX, OBJ, preview, or procedural asset-generation work. Do not use for unrelated Unity gameplay code.
---

# Blender Asset Creator

Use Blender MCP as the interaction layer and the repository's Python pipeline as the source of truth.

## Required project context

Read these files before changing asset behavior:

1. `Docs/art/BUSHCRAFT_GENERATION_SETUP.md`
2. `Docs/art/apex-shift-bushcraft-brief.md`
3. `Tools/Blender/bushcraft_asset_library.py`
4. `Tools/Blender/bushcraft_asset_generator.py`
5. `Tools/Blender/bushcraft_render_validation.py`
6. `Tools/Blender/blender_mcp_agent.py`

The current production target overrides older placeholder wording: assets are stylized realistic, hand-painted, organic and readable from an isometric camera. Mid-poly geometry is acceptable. Avoid primitive-looking low-poly substitutions.

## Preflight

1. Confirm the repository is a trusted checkout.
2. Confirm Blender is open and the Blender MCP add-on is connected on `localhost:9876`.
3. Inspect the scene before writing. Record Blender version, active scene, object count and current `.blend` path.
4. Save user-authored scene work before destructive operations.
5. Verify the requested asset IDs exist in `GENERATOR_MAP`. Never silently invent an ID.

Supported IDs currently include:

- Items: `wood`, `stone`, `fiber`, `grass`, `meat`, `hide`, `bone`, `berries`, `torch`, `spear`, `bow`
- Placeables: `campfire`, `storage_box`, `tent`, `wall`, `trap`
- Resources: `conifer_tree`, `leafy_tree`, `dry_tree`, `rock`, `green_bush`, `dry_bush`, `grass_or_flower`, `berry_bush`

## Preferred execution path

Use the Blender MCP Python execution tool only to import and call the checked-in orchestrator. Keep the executed snippet small and deterministic.

Example for the premium reference set:

```python
import sys
from pathlib import Path

repo = Path(r"C:\path\to\apex-shift")
tools = repo / "Tools" / "Blender"
if str(tools) not in sys.path:
    sys.path.insert(0, str(tools))

from blender_mcp_agent import run_asset_job
result = run_asset_job(["wood", "spear", "campfire"])
print(result)
```

For one asset, pass one ID. For the full set, call `run_asset_job(all_assets=True)` only after the three reference assets have been visually reviewed.

Do not paste the full generator implementation into MCP calls. Edit repository scripts when behavior must change, then invoke the checked-in code.

## Modeling rules

- Build from silhouette and gameplay function first.
- Use natural materials: bark, cut wood, stone, bone, hide, fiber, grass and leaves.
- Preserve handmade asymmetry, wear and imperfect bindings.
- Avoid plastic, nylon, polished factory hardware, modern equipment, fantasy runes and magical effects.
- Keep small details large enough to survive the isometric camera.
- Items and held tools need clear silhouettes distinct from their pickups and from one another.
- Prefer physical rope bindings where they explain construction.

## Scale, pivot and naming

- Work in Unity-compatible meters with Y-up export.
- Pickup origin: logical center or stable base as required by the existing pickup flow.
- Held tool origin: grip point when the runtime expects it; do not break existing held-item alignment.
- Placeable and resource origin: center of the base at ground level.
- Generated object name: `{asset_id}_stylized`.
- Keep exported names and game IDs stable.

## Output contract

The orchestrator writes to the existing directories:

- Models: `Assets/_Project/Art/Bushcraft/{Items|Placeables|Resources}/Models/`
- Textures: matching `Textures/` directory
- Blender sources: `Assets/_Project/Art/Bushcraft/Source/Blend/`
- Manifest: `Assets/_Project/Art/Bushcraft/bushcraft_model_manifest.json`
- Validation report: `Docs/art/bushcraft-validation-report.md`
- Last-run summary: `Docs/art/blender-mcp-agent-last-run.json`

Generate `.blend`, `.fbx`, `.obj` and an isometric preview PNG through the existing exporter.

## QA loop

For every asset:

1. Generate only that asset in a clean generated collection.
2. Validate it while it is still loaded in the scene.
3. Check UV presence, materials, origin, logical scale, mesh density, naming and preview output.
4. Inspect the rendered preview through Blender MCP.
5. Fix silhouette, clipping, material, scale or pivot issues in the repository generator.
6. Regenerate and revalidate until the automated status is `pass` and the preview is visually acceptable.

The orchestrator validates each asset immediately because the existing save/export flow removes previous generated objects from the active scene.

## Unity integration boundaries

- Buildings and resources remain integrated through `PrefabRegistry`, `BuildingPrefabEntry`, `ResourcePrefabEntry` and existing editor binders.
- Items should follow the existing pickup and held-item model resolver paths.
- Do not hardcode new runtime asset paths when an existing registry or resolver exists.
- Do not add Addressables as part of asset generation.
- Do not overwrite third-party packages or unrelated art folders.

## Safety

- Blender MCP can execute arbitrary Python. Run only repository code and short import/call snippets you authored for this task.
- Do not use shell, network, package installation or file deletion from inside Blender Python.
- Keep Blender MCP telemetry disabled through `.codex/config.toml`.
- External asset search and download features stay off unless explicitly requested.
- Do not modify files outside the repository root.

## Completion report

Report:

- requested and completed asset IDs
- generated file paths
- validation result for each asset
- visual issues corrected
- files changed in the repository
- Unity integration work still required
