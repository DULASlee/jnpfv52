# UniApp H5 演示前自检（在 jnpf-app-vue3 目录执行）
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host '=== 1. 检查后端 API ===' -ForegroundColor Cyan
try {
    $r = Invoke-WebRequest -Uri 'http://localhost:5000/api/oauth/getLoginConfig' -UseBasicParsing -TimeoutSec 8
    $j = $r.Content | ConvertFrom-Json
    if ($j.code -ne 200) { throw "getLoginConfig code=$($j.code)" }
    Write-Host '  OK: 后端 5000 正常' -ForegroundColor Green
} catch {
    Write-Host '  FAIL: 请先启动 backend JNPF.API.Entry (端口 5000)' -ForegroundColor Red
    exit 1
}

Write-Host '=== 2. 验证登录接口 ===' -ForegroundColor Cyan
node scripts/verify-login-api.mjs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '=== 3. 释放 3800 端口 ===' -ForegroundColor Cyan
Get-NetTCPConnection -LocalPort 3800 -State Listen -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty OwningProcess -Unique |
    ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }

Write-Host ''
Write-Host '请在 HBuilderX 中：' -ForegroundColor Yellow
Write-Host '  运行 -> 运行到浏览器 -> Chrome'
Write-Host '  地址: http://localhost:3800/#/pages/login/index'
Write-Host '  账号: admin / 123456'
Write-Host ''
Write-Host '勿与 proxy_server.py 同时占用 3800。' -ForegroundColor Yellow
