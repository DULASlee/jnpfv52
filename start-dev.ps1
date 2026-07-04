# start-dev.ps1 v2.3 — JNPF v5.2 dev environment launcher
# Encoding: UTF-8 with BOM (PowerShell 5.1 requirement)
# v2.3: build API Entry 项目（非全 solution），避免 MSB3030 *.xml 并行复制竞态
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== JNPF v5.2 Dev Startup ===" -ForegroundColor Cyan

# ================================================================
# Step 1/5: Free ports (kill only processes on our ports)
# ================================================================
Write-Host "[1/5] Freeing ports..." -ForegroundColor Yellow

$ports = @(
    @{Port=3100; Name='Vite frontend'},
    @{Port=5000; Name='Backend API'},
    @{Port=3001; Name='SA Service'}
)

foreach ($p in $ports) {
    $conn = Get-NetTCPConnection -LocalPort $p.Port -State Listen -ErrorAction SilentlyContinue
    if ($conn) {
        $conn | ForEach-Object {
            $proc = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
            if ($proc) {
                Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
                Write-Host "  Port $($p.Port) freed (was $($p.Name) PID $($_.OwningProcess))"
            }
        }
    } else {
        Write-Host "  Port $($p.Port) is free"
    }
}
Start-Sleep -Seconds 1

# ================================================================
# Step 2/5: Clean zombie processes (MSBuild nodes, old Chrome)
# ================================================================
Write-Host "[2/5] Cleaning zombie processes..." -ForegroundColor Yellow

$zombies = 0
$zombieFreedMB = 0

# MSBuild worker nodes only — do NOT kill all dotnet.exe (breaks IDE / dotnet run)
Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -match 'MSBuild|VBCSCompiler' } |
    ForEach-Object {
        $proc = Get-Process -Id $_.ProcessId -ErrorAction SilentlyContinue
        if ($proc) {
            $mb = [math]::Round($proc.WorkingSet64 / 1MB, 0)
            $zombieFreedMB += $mb
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            $zombies++
        }
    }

# Old Chrome instances (Playwright test leftovers)
Get-Process -Name 'chrome' -ErrorAction SilentlyContinue | ForEach-Object {
    $mb = [math]::Round($_.WorkingSet64 / 1MB, 0)
    $zombieFreedMB += $mb
    Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    $zombies++
}

# Force GC
[System.GC]::Collect()
[System.GC]::WaitForPendingFinalizers()

if ($zombies -gt 0) {
    Write-Host "  Cleaned $zombies zombies, freed ~$zombieFreedMB MB" -ForegroundColor Green
} else {
    Write-Host "  System is clean" -ForegroundColor Green
}

# ================================================================
# Step 3/5: Start backend API
# ================================================================
Write-Host ""
Write-Host "[3/5] Starting backend API..." -ForegroundColor Yellow

$backendDir = Join-Path $root 'backend'
$backendProject = 'application/JNPF.API.Entry/JNPF.API.Entry.csproj'
$backendProjectPath = Join-Path $backendDir $backendProject

# Restore packages only if obj directory is missing
$objDir = Join-Path $backendDir 'application/JNPF.API.Entry/obj'
if (-not (Test-Path $objDir)) {
    Write-Host "  First run: restoring NuGet packages..."
    dotnet restore $backendProjectPath --verbosity quiet
}

Write-Host "  Building backend API Entry (not full solution)..."
dotnet build $backendProjectPath -v q /nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "  Backend build failed — see errors above" -ForegroundColor Red
    exit 1
}

Start-Process powershell -ArgumentList (
    '-NoExit', '-Command',
    "cd '$backendDir'; dotnet run --project '$backendProject' --no-build --urls 'http://0.0.0.0:5000'"
) -PassThru | Out-Null

# Wait for backend to become ready
Write-Host "  Waiting for backend :5000..."
$backendReady = $false
$timeout = 120
for ($i = 0; $i -lt $timeout; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri 'http://localhost:5000' -UseBasicParsing -TimeoutSec 1 -ErrorAction Stop
        if ($resp.StatusCode -eq 200 -or $resp.StatusCode -eq 403) {
            Write-Host "  Backend ready (${i}s)" -ForegroundColor Green
            $backendReady = $true
            break
        }
    } catch {
        $statusCode = $null
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        if ($statusCode -eq 403) {
            Write-Host "  Backend ready (${i}s, 403 = JWT middleware OK)" -ForegroundColor Green
            $backendReady = $true
            break
        }
    }
    Start-Sleep -Seconds 1
}
if (-not $backendReady) {
    Write-Host "  Backend startup timed out (${timeout}s) — check the backend window for errors" -ForegroundColor Red
}

# ================================================================
# Step 4/5: Start SA Service
# ================================================================
Write-Host ""
Write-Host "[4/5] Starting SA Service..." -ForegroundColor Yellow

$saDir = Join-Path $root 'sa-service'
Start-Process powershell -ArgumentList (
    '-NoExit', '-Command',
    "cd '$saDir'; `$env:SA_SERVICE_PORT='3001'; `$env:SA_DB_BACKEND='inmemory'; `$env:LLM_GATEWAY_URL='http://localhost:5000/api/LlmGateway/ChatAsync'; Write-Host 'SA Service starting...'; npx tsx src/server.ts"
)
Write-Host "  SA Service launched in new window" -ForegroundColor Green

# ================================================================
# Step 5/5: Start frontend Vite
# ================================================================
Write-Host ""
Write-Host "[5/5] Starting frontend Vite..." -ForegroundColor Yellow

$frontendDir = Join-Path $root 'jnpf-web-vue3'

# Clear stale Vite dep cache (fixes 504 Outdated Optimize Dep / dynamic import failures)
$viteCache = Join-Path $frontendDir 'node_modules\.vite'
if (Test-Path $viteCache) {
    Write-Host "  Clearing Vite optimize cache..."
    Remove-Item -Recurse -Force $viteCache -ErrorAction SilentlyContinue
}

Start-Process powershell -ArgumentList (
    '-NoExit', '-Command',
    "cd '$frontendDir'; Write-Host 'Vite starting (fresh deps)...'; pnpm dev -- --force"
)

# 注：已移除 Vite-MemGuard 后台 Job——它在不清理 .vite 缓存的情况下重启 Vite，
# 会导致浏览器仍请求旧 dep hash 而报 504 Outdated Optimize Dep。

# Wait for frontend to become ready
Write-Host "  Waiting for frontend :3100..."
$frontendReady = $false
for ($i = 0; $i -lt 60; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri 'http://localhost:3100' -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($resp.StatusCode -eq 200) {
            Write-Host "  Frontend ready (${i}s)" -ForegroundColor Green
            $frontendReady = $true
            break
        }
    } catch {}
    Start-Sleep -Seconds 1
}
if (-not $frontendReady) {
    Write-Host "  Frontend startup timed out (60s) — check the Vite window (first start may take longer)" -ForegroundColor Yellow
}

# ================================================================
# Done
# ================================================================
Write-Host ""
Write-Host "=== Dev environment ready ===" -ForegroundColor Green
Write-Host "  Backend:  http://localhost:5000" -ForegroundColor Cyan
Write-Host "  Frontend: http://localhost:3100" -ForegroundColor Cyan
Write-Host "  SA:       http://localhost:3001" -ForegroundColor Cyan
Write-Host "  Login:    admin / 123456" -ForegroundColor White
Write-Host ""
Write-Host "Open http://localhost:3100 to start" -ForegroundColor White
