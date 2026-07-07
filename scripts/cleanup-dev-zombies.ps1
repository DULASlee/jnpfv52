# cleanup-dev-zombies.ps1 — 仅清理（逻辑内联于 start-dev.ps1）
# 用法：powershell -ExecutionPolicy Bypass -File scripts/cleanup-dev-zombies.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
& (Join-Path $root 'start-dev.ps1') -CleanupOnly
