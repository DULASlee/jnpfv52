# 单实例重启 Serena MCP（先清孤儿，再杀旧 daemon，再起一个）
# 用法: powershell -ExecutionPolicy Bypass -File scripts/serena-restart-single.ps1

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path $PSScriptRoot -Parent
$Port = 9900

& "$RepoRoot/scripts/serena-cleanup-orphans.ps1"

Write-Host "`nStopping existing Serena on port $Port..."
Get-CimInstance Win32_Process -Filter "CommandLine LIKE '%start-mcp-server%'" |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Start-Sleep -Seconds 2

& "$RepoRoot/scripts/serena-cleanup-orphans.ps1"

$listener = netstat -ano | findstr "LISTENING" | findstr ":$Port "
if ($listener) {
    Write-Error "Port $Port still in use. Manual kill required."
    exit 1
}

Write-Host "Starting single Serena instance..."
Start-Process -FilePath "serena" -ArgumentList @(
    'start-mcp-server',
    '--transport', 'sse',
    '--host', '127.0.0.1',
    '--port', "$Port",
    '--project', 'JNPF-v52',
    '--context', 'ide',
    '--open-web-dashboard', 'false',
    '--enable-gui-log-window', 'false'
) -WindowStyle Hidden

Start-Sleep -Seconds 5
Write-Host "Done. serena.exe=$((Get-Process serena -ErrorAction SilentlyContinue).Count) OmniSharp=$((Get-Process OmniSharp -ErrorAction SilentlyContinue).Count)"
