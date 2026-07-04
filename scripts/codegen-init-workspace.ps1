# P4-B06 — 预还原 codegen 宿主 + sandbox 共享 NuGet 缓存
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$nugetPackages = Join-Path $RepoRoot "workspace\codegen-sandbox\.nuget\packages"
$sandboxTemplate = Join-Path $RepoRoot "workspace\codegen-sandbox\template\JNPF.Codegen.Sandbox.csproj"
$hostCsproj = Join-Path $RepoRoot "workspace\codegen-host-demo\JNPF.Codegen.HostDemo.csproj"
$hostMarker = Join-Path $RepoRoot "workspace\codegen-host-demo\.restore-complete"
$sandboxMarker = Join-Path $RepoRoot "workspace\codegen-sandbox\.restore-complete"

New-Item -ItemType Directory -Force -Path $nugetPackages | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path $sandboxTemplate) | Out-Null

if (-not (Test-Path $sandboxTemplate)) {
    Write-Host "[codegen-init] writing sandbox template csproj..."
    Push-Location (Join-Path $RepoRoot "backend\tests\JNPF.Tests.PhaseB")
    dotnet run --no-build -- sandbox-gate 2>$null | Out-Null
    Pop-Location
}

Write-Host "[codegen-init] dotnet restore sandbox template..."
$env:NUGET_PACKAGES = $nugetPackages
dotnet restore $sandboxTemplate --packages $nugetPackages
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Set-Content -Path $sandboxMarker -Value (Get-Date -Format o)

Write-Host "[codegen-init] dotnet restore host demo..."
dotnet restore $hostCsproj --packages $nugetPackages
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Set-Content -Path $hostMarker -Value (Get-Date -Format o)

Write-Host "[codegen-init] PASS — NuGet cache: $nugetPackages"
Write-Host "[codegen-init] next: node scripts/codegen-inject-host.mjs --ensure-generated"
