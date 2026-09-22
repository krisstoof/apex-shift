# Blender MCP agent

This repository uses the v10.1 Blender pipeline as the only active production
asset workflow.

## Canonical files

- `Tools/BlenderV10/apex_shift_blender_generator_v10_1.py`
- `Tools/BlenderV10/apex_shift_profiles_v10.py`
- `Tools/BlenderV10/apex_shift_asset_visual_specs_v5.json`
- `Tools/BlenderV10/blender_mcp_agent.py`

The profile/spec set contains 98 asset profiles. Asset IDs must come from that
file; do not use a separate legacy ID list.

## MCP setup

Install `uv`/`uvx`, open Blender, enable the Blender MCP add-on and connect it
on `localhost:9876`. Codex uses `.codex/config.toml`; VS Code uses the same
command:

```json
{
  "command": "cmd",
  "args": ["/c", "uvx", "--python", "3.11", "blender-mcp"]
}
```

The configured environment is `BLENDER_HOST=localhost`, `BLENDER_PORT=9876`,
and `DISABLE_TELEMETRY=true`.

## MCP workflow

```python
import sys
from pathlib import Path

repo = Path(r"C:\path\to\apex-shift")
tools = repo / "Tools" / "BlenderV10"
sys.path.insert(0, str(tools))

from blender_mcp_agent import run_asset_job
result = run_asset_job(["tent"])
print(result)
```

Review previews before requesting `run_asset_job(all_assets=True)`. The
generator writes first to the ignored local directory
`Tools/BlenderV10/ApexShift_Assets_v10_Output/`. It does not write directly to
Unity production folders.

Unity import is a separate editor operation:

`Apex Shift -> Art -> Bushcraft -> Import Generated Assets And Bind PrefabRegistry`

That command uses `BushcraftGeneratedAssetBinder`. Production Unity models stay
committed under `Assets/_Project/Art/Bushcraft` and the relevant `Resources`
folders. No repository clone or git submodule of `blender-mcp` is required.

Legacy v4/v6/v7 and old-tree scripts are retained only in
`Tools/Archive/BushcraftLegacy/` for historical reference.
