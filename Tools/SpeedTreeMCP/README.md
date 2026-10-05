# SpeedTree MCP for Apex Shift

Local adapter: `dcc-mcp-speedtree==0.1.1`, with Core/Server `0.20.41`.
Source: https://github.com/dcc-mcp/dcc-mcp-speedtree (MIT).

The adapter uses the installed SpeedTree Modeler 10.2.0 executable. It is experimental and supports native file operations and licensed command-line export; full live GUI control is unavailable. Modeler licensing and successful exports must be verified per asset.

Run `Tools/SpeedTreeMCP/Start-SpeedTreeMCP.ps1` when the local service is stopped, then reload Codex. Project configuration in `.codex/config.toml` connects to `http://127.0.0.1:9877/mcp`. Start only one service on that port. No automatic Windows startup is configured.

The Python environment, cache, runtime data, and logs stay local and are ignored by Git. No models, textures, or vendor binaries were downloaded.
