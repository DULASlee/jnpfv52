# Install portable Superpowers + OpenSpec + episodic toolchain into a target repo.
# Usage:
#   .\scripts\install-toolchain.ps1 -TargetPath "d:\JNPF-v52" -EpisodicProjectId "D--JNPF-v52" -ProjectSlug "JNPF-v52"

param(
    [Parameter(Mandatory = $true)]
    [string]$TargetPath,
    [Parameter(Mandatory = $true)]
    [string]$EpisodicProjectId,
    [Parameter(Mandatory = $true)]
    [string]$ProjectSlug,
    [string]$DisplayName = "JNPF v5.2 workspace",
    [string]$SourcePath = ""
)

if (-not $SourcePath) {
    $scriptDir = $PSScriptRoot
    if (-not $scriptDir) { $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path }
    $SourcePath = Split-Path $scriptDir -Parent
}

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Copy-Tree($rel) {
    $src = Join-Path $SourcePath $rel
    $dst = Join-Path $TargetPath $rel
    if (-not (Test-Path $src)) { Write-Warning "Skip missing: $rel"; return }
    $parent = Split-Path $dst -Parent
    if (-not (Test-Path $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    if (Test-Path $dst) { Remove-Item $dst -Recurse -Force -ErrorAction SilentlyContinue }
    Copy-Item -Path $src -Destination $dst -Recurse -Force
    Write-Host "Copied: $rel" -ForegroundColor Green
}

Write-Host "`n=== Install toolchain to $TargetPath ===" -ForegroundColor Cyan

if (-not (Test-Path $TargetPath)) {
    New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
}

# .cursor (exclude logs / browser profile)
$cursorDst = Join-Path $TargetPath ".cursor"
if (Test-Path $cursorDst) { Remove-Item $cursorDst -Recurse -Force }
Copy-Item (Join-Path $SourcePath ".cursor") $cursorDst -Recurse -Force
@("logs", "edge-debug-profile") | ForEach-Object {
    $p = Join-Path $cursorDst $_
    if (Test-Path $p) { Remove-Item $p -Recurse -Force }
}
if (Test-Path (Join-Path $cursorDst "episodic\sync-status.json")) {
    Remove-Item (Join-Path $cursorDst "episodic\sync-status.json") -Force
}

# scripts
Copy-Tree "scripts\episodic-config.mjs"
Copy-Tree "scripts\episodic-sync.mjs"
Copy-Tree "scripts\toolchain-lib.mjs"
Copy-Tree "scripts\verify-toolchain.mjs"
Copy-Item (Join-Path $SourcePath "scripts\install-toolchain.ps1") (Join-Path $TargetPath "scripts\install-toolchain.ps1") -Force

# env example
Copy-Item (Join-Path $SourcePath ".env.toolchain.example") (Join-Path $TargetPath ".env.toolchain.example") -Force

# openspec skeleton
if (-not (Test-Path (Join-Path $TargetPath "openspec"))) {
    Copy-Tree "openspec"
} else {
    Copy-Tree "openspec\config.yaml"
    if (-not (Test-Path (Join-Path $TargetPath "openspec\changes"))) {
        New-Item -ItemType Directory -Path (Join-Path $TargetPath "openspec\changes\archive") -Force | Out-Null
    }
}

# docs toolchain guide
$toolchainDocSrc = Join-Path $SourcePath "docs\toolchain"
$toolchainDocDst = Join-Path $TargetPath "docs\toolchain"
if (Test-Path $toolchainDocSrc) {
    if (-not (Test-Path (Split-Path $toolchainDocDst -Parent))) {
        New-Item -ItemType Directory -Path (Split-Path $toolchainDocDst -Parent) -Force | Out-Null
    }
    Copy-Item $toolchainDocSrc $toolchainDocDst -Recurse -Force
}

# manifest
$manifest = @{
    version          = 1
    display_name     = $DisplayName
    project_slug     = $ProjectSlug
    episodic_project_id = $EpisodicProjectId
    workspace_path_hint = $TargetPath
    toolchain        = @{ superpowers = $true; openspec = $true; episodic_memory = $true }
    openspec         = @{ schema = "spec-driven"; specs_dir = "openspec/specs"; changes_dir = "openspec/changes" }
    docs             = @{
        playbook           = "docs/架构迭代/4、项目工作推进日程清单/episodic-memory-playbook.md"
        progress_registry  = "docs/架构迭代/4、项目工作推进日程清单/progress-registry.yaml"
    }
} | ConvertTo-Json -Depth 5
$manifestPath = Join-Path $TargetPath ".cursor\toolchain.manifest.json"
Set-Content -Path $manifestPath -Value $manifest -Encoding UTF8
Write-Host "Wrote manifest: episodic_project_id=$EpisodicProjectId" -ForegroundColor Green

# patch search-templates project_id
$templatesPath = Join-Path $TargetPath ".cursor\episodic\search-templates.yaml"
if (Test-Path $templatesPath) {
    $yaml = Get-Content $templatesPath -Raw -Encoding UTF8
    $yaml = $yaml -replace 'project_id:\s*\S+', "project_id: $EpisodicProjectId"
    $yaml = $yaml -replace '\["liu202505v2"', "[`"$ProjectSlug`""
    Set-Content -Path $templatesPath -Value $yaml -Encoding UTF8 -NoNewline
}

# .gitignore entries
$gi = Join-Path $TargetPath ".gitignore"
$lines = @(
    ".cursor/logs/",
    ".cursor/episodic/sync-status.json",
    ".env.toolchain",
    ".cursor/edge-debug-profile/"
)
if (Test-Path $gi) {
    $content = Get-Content $gi -Raw
    foreach ($line in $lines) {
        if ($content -notmatch [regex]::Escape($line)) { Add-Content $gi $line }
    }
} else {
    Set-Content $gi ($lines -join "`n")
}

# openspec init in target
Push-Location $TargetPath
try {
    $null = openspec init --tools cursor --force 2>&1
    Write-Host "openspec init (cursor) completed" -ForegroundColor Green
} catch {
    Write-Warning "openspec init failed (install openspec globally): $_"
}
Pop-Location

Write-Host "`nRun verification:" -ForegroundColor Cyan
Write-Host "  cd `"$TargetPath`"" -ForegroundColor White
Write-Host "  node scripts/verify-toolchain.mjs" -ForegroundColor White
Write-Host "  node scripts/episodic-sync.mjs --stats`n" -ForegroundColor White
