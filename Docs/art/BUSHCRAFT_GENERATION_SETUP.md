# Apex Shift Bushcraft generation setup

This document describes the active v10.1, 98-profile Blender pipeline. The
older 25-asset `Tools/Blender` workflow is historical and is not production.

## Source of truth

Use these files:

- `Tools/BlenderV10/apex_shift_blender_generator_v10_1.py`
- `Tools/BlenderV10/apex_shift_profiles_v10.py`
- `Tools/BlenderV10/apex_shift_asset_visual_specs_v5.json`
- `Tools/BlenderV10/apex_shift_biblia_wizualna_v5.md`
- `Tools/BlenderV10/blender_mcp_agent.py`

The profile file defines the complete 98-asset pack and is the source of valid
asset IDs. The v10.1 generator validates profile coverage before generation.

## Blender MCP

Install `uv`/`uvx` and run the Blender MCP add-on in Blender on
`localhost:9876`. Codex and VS Code use:

```text
cmd /c uvx --python 3.11 blender-mcp
```

The configured environment is `BLENDER_HOST=localhost`, `BLENDER_PORT=9876`,
and `DISABLE_TELEMETRY=true`. A repository clone or git submodule of
`blender-mcp` is not required.

## Generate assets

One asset:

```powershell
blender --background --python Tools/BlenderV10/apex_shift_blender_generator_v10_1.py -- --only tent
```

Several assets:

```powershell
blender --background --python Tools/BlenderV10/apex_shift_blender_generator_v10_1.py -- --only tent,campfire,storage_box
```

Full pack: omit `--only`. For MCP, import `run_asset_job` from
`Tools/BlenderV10/blender_mcp_agent.py` and pass explicit IDs, or use
`run_asset_job(all_assets=True)` after reviewing reference previews.

## Output and Unity import

Generation first writes locally to:

`Tools/BlenderV10/ApexShift_Assets_v10_Output/`

This directory is ignored and is not source controlled. It may contain FBX,
GLB, Blend, previews and audit reports for local QA. MCP does not write
directly into Unity production folders.

Import generated FBX through:

`Apex Shift -> Art -> Bushcraft -> Import Generated Assets And Bind PrefabRegistry`

The command uses `BushcraftGeneratedAssetBinder`, whose local source root is
`Tools/BlenderV10/ApexShift_Assets_v10_Output`. Final Unity models remain
committed under `Assets/_Project/Art/Bushcraft` and the relevant
`Assets/_Project/Resources` folders. Fresh checkout gameplay does not require
running Blender.

## Legacy policy

Old v4/v6/v7 generators and the old-tree experiment sources are retained only
under `Tools/Archive/BushcraftLegacy/` for historical reference. They are not
used by Unity, Blender MCP, Codex or VS Code active workflows.
