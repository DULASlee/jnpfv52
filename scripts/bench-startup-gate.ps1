# ═══════════════════════════════════════════════════════════════
# bench-startup-gate.ps1 — PR 防退坡门控（战役 0 交付物）
#
# 功能：运行 JNPF.Startup.Benchmarks --mode inproc，
#       对比 baseline.json 的 DI 描述符数，增幅超阈值即失败。
# 用法：powershell -File scripts\bench-startup-gate.ps1 [-SkipBuild]
# 说明：冷启动全量测量耗时较长，不进 PR gate，走每日/发布前（见 docs/benchmark-baseline.md §6）。
# ═══════════════════════════════════════════════════════════════
param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$backend = Join-Path $repo 'backend'
$proj = Join-Path $backend 'tools\JNPF.Startup.Benchmarks\JNPF.Startup.Benchmarks.csproj'
$harnessDll = Join-Path $backend 'tools\JNPF.Startup.Benchmarks\bin\Debug\net8.0\JNPF.Startup.Benchmarks.dll'
$entryDir = Join-Path $backend 'application\JNPF.API.Entry\bin\Debug\net8.0'
$baselineFile = Join-Path $backend 'tools\JNPF.Startup.Benchmarks\baseline.json'

if (-not $SkipBuild) {
    Write-Host "[gate] 构建基准工程..."
    dotnet build $proj --nologo -v q | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Error "[gate] 基准工程构建失败"; exit 1 }
}
if (-not (Test-Path $harnessDll)) { Write-Error "[gate] harness 不存在: $harnessDll"; exit 1 }
if (-not (Test-Path (Join-Path $entryDir 'JNPF.API.Entry.dll'))) {
    Write-Error "[gate] 入口未构建，先 dotnet build JNPF.API.Entry"; exit 1
}

$baseline = Get-Content $baselineFile -Raw | ConvertFrom-Json
$thresholdPct = $baseline.thresholds.diDescriptorGrowthPct

Write-Host "[gate] 运行 inproc 测量..."
$pushed = Get-Location
try {
    Set-Location $entryDir
    $output = dotnet $harnessDll --mode inproc 2>&1 | Out-String
} finally {
    Set-Location $pushed
}

$match = [regex]::Match($output, '\[METRIC\] descriptor_count=(\d+)')
if (-not $match.Success) {
    Write-Host $output
    Write-Error "[gate] 无法解析描述符数（inproc 运行失败？）"
    exit 1
}
$current = [int]$match.Groups[1].Value
$baselineCount = [int]$baseline.inproc.descriptorCount
$growthPct = [math]::Round(($current - $baselineCount) * 100.0 / $baselineCount, 1)

Write-Host "[gate] DI 描述符数: baseline=$baselineCount current=$current growth=${growthPct}% threshold=${thresholdPct}%"

if ($growthPct -gt $thresholdPct) {
    Write-Error "[gate] FAIL — DI 注册数增幅 ${growthPct}% 超过阈值 ${thresholdPct}%。如属预期，请更新 baseline.json 并在 PR 说明理由。"
    exit 1
}

Write-Host "[gate] PASS"
exit 0
