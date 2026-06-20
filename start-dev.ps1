# start-dev.ps1 v2.1 — JNPF v5.2 dev environment launcher
# Encoding: UTF-8 with BOM (PowerShell 5.1 requirement)
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

# Zombie MSBuild nodes: dotnet processes not listening on any port
Get-Process -Name 'dotnet' -ErrorAction SilentlyContinue | ForEach-Object {
    $pid = $_.Id
    $port = Get-NetTCPConnection -OwningProcess $pid -State Listen -ErrorAction SilentlyContinue
    if (-not $port) {
        $mb = [math]::Round($_.WorkingSet64 / 1MB, 0)
        $zombieFreedMB += $mb
        Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
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

# Restore packages only if obj directory is missing
$objDir = Join-Path $backendDir 'application/JNPF.API.Entry/obj'
if (-not (Test-Path $objDir)) {
    Write-Host "  First run: restoring NuGet packages..."
    dotnet restore (Join-Path $backendDir $backendProject) --verbosity quiet
}

Start-Process powershell -ArgumentList (
    '-NoExit', '-Command',
    "cd '$backendDir'; dotnet run --project '$backendProject' --urls 'http://0.0.0.0:5000'"
) -PassThru | Out-Null

# Wait for backend to become ready
Write-Host "  Waiting for backend :5000..."
$timeout = 120
for ($i = 0; $i -lt $timeout; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri 'http://localhost:5000' -UseBasicParsing -TimeoutSec 1 -ErrorAction Stop
        if ($resp.StatusCode -eq 200 -or $resp.StatusCode -eq 403) {
            Write-Host "  Backend ready (${i}s)" -ForegroundColor Green
            break
        }
    } catch {
        if ($_.Exception.Response.StatusCode -eq 403) {
            Write-Host "  Backend ready (${i}s, 403 = JWT middleware OK)" -ForegroundColor Green
            break
        }
    }
    if ($i -eq ($timeout - 1)) {
        Write-Host "  Backend startup timed out (${timeout}s)" -ForegroundColor Red
    }
    Start-Sleep -Seconds 1
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
Start-Process powershell -ArgumentList (
    '-NoExit', '-Command',
    "cd '$frontendDir'; Write-Host 'Vite starting...'; `$env:NODE_OPTIONS='--max-old-space-size=2048'; npx vite --mode development"
)

# Memory guard: restart Vite if node exceeds 2GB
$memGuard = {
    $limit = 2GB
    $frontendDir = $using:frontendDir
    while ($true) {
        Start-Sleep -Seconds 30
        $conn = Get-NetTCPConnection -LocalPort 3100 -State Listen -ErrorAction SilentlyContinue
        if (-not $conn) { continue }
        $proc = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
        if ($proc -and $proc.WorkingSet64 -gt $limit) {
            $gb = [math]::Round($proc.WorkingSet64 / 1GB, 1)
            Write-Host "$(Get-Date -Format 'HH:mm:ss') node PID $($proc.Id) memory ${gb}GB > 2GB, restarting Vite..." -ForegroundColor Red
            Stop-Process -Id $proc.Id -Force
            Start-Sleep -Seconds 2
            Set-Location $frontendDir
            $env:NODE_OPTIONS = '--max-old-space-size=2048'
            Start-Process npx -ArgumentList 'vite','--mode','development' -WindowStyle Minimized
        }
    }
}
Start-Job -ScriptBlock $memGuard -Name 'Vite-MemGuard' | Out-Null

# Wait for frontend to become ready
Write-Host "  Waiting for frontend :3100..."
for ($i = 0; $i -lt 30; $i++) {
    try {
        $resp = Invoke-WebRequest -Uri 'http://localhost:3100' -UseBasicParsing -TimeoutSec 1 -ErrorAction Stop
        if ($resp.StatusCode -eq 200) {
            Write-Host "  Frontend ready (${i}s)" -ForegroundColor Green
            break
        }
    } catch {}
    if ($i -eq 29) {
        Write-Host "  Frontend startup timed out (30s)" -ForegroundColor Red
    }
    Start-Sleep -Seconds 1
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
