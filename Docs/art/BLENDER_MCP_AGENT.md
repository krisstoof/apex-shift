# Blender MCP Asset Agent

The repository contains a Codex skill and a Blender-side orchestrator for creating Apex Shift bushcraft assets through Blender MCP.

## Included files

- `.codex/config.toml` - project-scoped Blender MCP server configuration for Windows.
- `AGENTS.md` - repository guidance that routes Blender work to the dedicated skill.
- `.agents/skills/blender-asset-creator/SKILL.md` - the agent workflow and safety rules.
- `.agents/skills/blender-asset-creator/references/asset-contracts.md` - visual and technical contracts.
- `Tools/Blender/blender_mcp_agent.py` - safe entry point around the existing generator and validation scripts.

## Local prerequisites

1. Install Blender and the current Blender MCP add-on.
2. Install `uv` with its official Windows installer so the `uvx` command exists.
3. Open the repository as a trusted project in Codex.
4. Restart Codex after reading the project `.codex/config.toml`.
5. Open Blender, enable the Blender MCP add-on, open the `BlenderMCP` sidebar tab and start the connection on port `9876`.
6. Use `/mcp` in Codex to confirm the `blender` server is active.

The project configuration pins Python 3.11, disables Blender MCP telemetry and asks for approval for write-capable tools.

## First run

Start with the reference set rather than generating every asset:

```text
Use the blender-asset-creator skill. Inspect the Blender scene, then generate and validate wood, spear and campfire through Tools/Blender/blender_mcp_agent.py. Show me the previews and report every failed check before changing more assets.
```

The MCP call should import the checked-in orchestrator and run:

```python
from blender_mcp_agent import run_asset_job
run_asset_job(["wood", "spear", "campfire"])
```

## Typical requests

```text
Use Blender MCP to regenerate spear. Improve the silhouette and fiber binding, keep the held-item pivot stable, render a preview and rerun validation.
```

```text
Generate tent and storage_box, then compare their scale and isometric readability. Do not modify Unity gameplay code.
```

```text
Generate all assets only after the wood, spear and campfire previews pass review. Write the combined manifest and validation report.
```

## Generated outputs

- `Assets/_Project/Art/Bushcraft/Items/Models/`
- `Assets/_Project/Art/Bushcraft/Placeables/Models/`
- `Assets/_Project/Art/Bushcraft/Resources/Models/`
- `Assets/_Project/Art/Bushcraft/Source/Blend/`
- `Assets/_Project/Art/Bushcraft/bushcraft_model_manifest.json`
- `Docs/art/bushcraft-validation-report.md`
- `Docs/art/blender-mcp-agent-last-run.json`

## Security boundaries

Blender MCP exposes arbitrary Python execution inside Blender. The agent therefore runs only checked-in repository modules and short import/call snippets. External model downloads, Poly Haven, Sketchfab, generated-model services and arbitrary web code remain disabled unless explicitly requested.
