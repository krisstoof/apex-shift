$ErrorActionPreference = 'Stop'
$env:DCC_MCP_GATEWAY_PORT = '0'
$env:DCC_MCP_DISABLE_DEFAULT_SKILL_PATHS = 'true'
$env:DCC_MCP_REGISTRY_DIR = Join-Path $PSScriptRoot 'runtime'
$env:DCC_MCP_LOG_DIR = Join-Path $PSScriptRoot 'runtime/logs'
$adapter = Join-Path $PSScriptRoot '.venv/Scripts/dcc-mcp-speedtree.exe'
$modeler = 'C:/Program Files/SpeedTree/SpeedTree Modeler v10.2.0/win64/SpeedTree_Modeler.exe'
if (!(Test-Path -LiteralPath $adapter)) { throw 'Install dcc-mcp-speedtree==0.1.1 in Tools/SpeedTreeMCP/.venv first.' }
if (!(Test-Path -LiteralPath $modeler)) { throw "SpeedTree executable missing: $modeler" }
Start-Process -FilePath $adapter -ArgumentList @('--executable', ('"{0}"' -f $modeler), '--version', '10.2.0', '--port', '9877') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $PSScriptRoot 'stdout.log') -RedirectStandardError (Join-Path $PSScriptRoot 'stderr.log')
