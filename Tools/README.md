# Apex Shift tools

## Canonical Blender pipeline

The production generator is:

`Tools/BlenderV10/apex_shift_blender_generator_v10_1.py`

Its source of truth is the v10 profile/spec set in `Tools/BlenderV10/`.
The Blender MCP wrapper is `Tools/BlenderV10/blender_mcp_agent.py`.

## Blender MCP setup

The supported dependency model is `uvx`; no repository clone or git submodule
of `blender-mcp` is required. Codex uses `.codex/config.toml`. VS Code uses the
same Windows command:

```json
{
  "command": "cmd",
  "args": ["/c", "uvx", "--python", "3.11", "blender-mcp"]
}
```

The Blender MCP add-on must be running in Blender on `localhost:9876`.
Environment values are `BLENDER_HOST=localhost`, `BLENDER_PORT=9876`, and
`DISABLE_TELEMETRY=true`.

## Generation

Generate one asset on Windows:

```powershell
blender --background --python Tools/BlenderV10/apex_shift_blender_generator_v10_1.py -- --only tent
```

Generate several assets with `--only tent,campfire,storage_box`, or generate
the complete profile pack by omitting `--only`.

Generated output is written to:

`Tools/BlenderV10/ApexShift_Assets_v10_Output/`

This directory is local, ignored, and not source controlled. The generator
exports FBX for Unity and may also produce GLB, Blend and previews for local
QA; those generated files are not Unity production source.

Unity import is performed through `Apex Shift -> Art -> Bushcraft -> Import
Generated Assets And Bind PrefabRegistry` using
`BushcraftGeneratedAssetBinder`. Final Unity-consumable models remain committed
under `Assets/_Project/Art/Bushcraft` and the relevant `Resources` folders.

Legacy v4/v6/v7 and old-tree scripts are retained only under
`Tools/Archive/BushcraftLegacy/` for historical reference. They are not used by
Unity or active agent workflows.
