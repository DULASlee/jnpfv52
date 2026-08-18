# 构建交付预览沙箱镜像（30 号 W2）
# 用法（仓库根目录）：
#   powershell -ExecutionPolicy Bypass -File docker/jnpf-sandbox/build.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Test-Path (Join-Path $root "docker\jnpf-sandbox\Dockerfile"))) {
  $root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
}

Set-Location $root
Write-Host "Building jnpf-sandbox:latest from $root ..."
docker build -t jnpf-sandbox:latest -f docker/jnpf-sandbox/Dockerfile .
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "OK: jnpf-sandbox:latest"
docker images jnpf-sandbox:latest
