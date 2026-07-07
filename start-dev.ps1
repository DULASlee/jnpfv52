# start-dev.ps1 v3.1 — JNPF v5.2 全栈开发环境一键启动
# Encoding: UTF-8 with BOM (PowerShell 5.1 requirement)
# 启动：PC 前端 :3100 | 数字大屏 :3102 | UniApp H5 :3800 | 后端 :5000 | SA :3001
param(
    [switch]$CleanupOnly
)

$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$script:IsAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

# 端口规范见 docs/conventions/ports.md
$script:DevPorts = @(3100, 3102, 3800, 5000, 3001)

function Invoke-BackendBuild {
    param([string]$ProjectPath)
    # Run from backend/ so global.json resolves SDK version correctly
    $backendDir = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $ProjectPath))
    $prev = [Console]::OutputEncoding
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    try {
        Push-Location $backendDir
        dotnet build $ProjectPath `
            -v q /nologo `
            -p:RunAnalyzers=false `
            -p:CI_BUILD=false
    } finally {
        Pop-Location
        [Console]::OutputEncoding = $prev
    }
    return $LASTEXITCODE
}

function Write-Step([string]$Num, [string]$Total, [string]$Msg) {
    Write-Host "[$Num/$Total] $Msg" -ForegroundColor Yellow
}

function Get-PortProcessMap {
    # Returns hashtable: port -> @(pid1, pid2, ...)
    # Runs netstat ONCE and parses all ports in a single pass.
    $map = @{}
    foreach ($port in $script:DevPorts) { $map[$port] = @() }

    # netstat -ano: fast, works non-admin
    $rawLines = netstat -ano 2>$null
    foreach ($line in $rawLines) {
        if ($line -notmatch 'LISTENING') { continue }
        foreach ($port in $script:DevPorts) {
            if ($line -match ":$port\s") {
                $parts = $line -split '\s+' | Where-Object { $_ -ne '' }
                $pidStr = $parts[-1]
                if ($pidStr -match '^\d+$' -and [int]$pidStr -gt 4) {
                    $map[$port] += [int]$pidStr
                }
                break
            }
        }
    }
    return $map
}

function Clear-DevEnvironment {
    $freedPorts = 0
    $zombies = 0
    $freedMB = 0

    $modeLabel = if ($script:IsAdmin) { 'Admin' } else { 'User (netstat fallback)' }
    Write-Host "  Mode: $modeLabel" -ForegroundColor DarkGray

    # --- Layer 1: Kill by port (single netstat pass for all ports) ---
    $portMap = Get-PortProcessMap
    foreach ($port in $script:DevPorts) {
        foreach ($procId in $portMap[$port]) {
            $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
            if ($null -eq $proc) { continue }
            $freedMB += [math]::Round($proc.WorkingSet64 / 1MB, 0)
            Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
            Write-Host "  Port $($port): killed $($proc.ProcessName) (PID $procId)" -ForegroundColor DarkGray
            $freedPorts++
        }
    }

    # --- Layer 2: Kill by process name (catches DLL-lock zombies without port) ---
    # Uses Get-Process only (fast kernel API, no WMI). Path matching guards against killing
    # unrelated dotnet/node processes on the dev machine.

    # 2a: JNPF.API.Entry — the backend itself
    Get-Process -Name 'JNPF.API.Entry' -ErrorAction SilentlyContinue | ForEach-Object {
        $freedMB += [math]::Round($_.WorkingSet64 / 1MB, 0)
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  Name kill: JNPF.API.Entry PID $($_.Id)" -ForegroundColor DarkGray
        $zombies++
    }

    # 2b: dotnet.exe processes under the JNPF backend directory
    Get-Process -Name 'dotnet' -ErrorAction SilentlyContinue | ForEach-Object {
        $kill = $false
        try { $procPath = $_.Path } catch { $procPath = '' }
        # dotnet.exe from JNPF solution or SDK build tools
        if ($procPath -and ($procPath -match 'JNPF|dotnet\\sdk')) { $kill = $true }
        # Also kill if working directory is under JNPF (dotnet run)
        if (-not $kill) {
            try { $kill = $_.MainWindowTitle -match 'JNPF' } catch {}
        }
        if ($kill) {
            $freedMB += [math]::Round($_.WorkingSet64 / 1MB, 0)
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
            Write-Host "  Killed dotnet PID $($_.Id)" -ForegroundColor DarkGray
            $zombies++
        }
    }

    # 2c: node.exe processes — check if path is within jnpf-web-vue3 or sa-service
    Get-Process -Name 'node' -ErrorAction SilentlyContinue | ForEach-Object {
        $kill = $false
        try { $procPath = $_.Path } catch { $procPath = '' }
        if ($procPath -and ($procPath -match 'jnpf-web-vue3|sa-service|jnpf-app-vue3|jnpf-web-datascreen')) { $kill = $true }
        # Fallback: window title match
        if (-not $kill) {
            try { $kill = $_.MainWindowTitle -match 'JNPF|Vite|3100|3102|3800|3001' } catch {}
        }
        if ($kill) {
            $freedMB += [math]::Round($_.WorkingSet64 / 1MB, 0)
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
            Write-Host "  Killed node PID $($_.Id)" -ForegroundColor DarkGray
            $zombies++
        }
    }

    # --- Layer 3: Python proxy (match by path, no WMI) ---
    Get-Process -Name 'python', 'python3' -ErrorAction SilentlyContinue | ForEach-Object {
        $isProxy = $false
        try { $isProxy = $_.Path -match 'proxy_server\.py' -or $_.MainWindowTitle -match 'proxy' } catch {}
        if ($isProxy) {
            $freedMB += [math]::Round($_.WorkingSet64 / 1MB, 0)
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
            Write-Host "  Killed python proxy PID $($_.Id)" -ForegroundColor DarkGray
            $zombies++
        }
    }

    # --- Layer 4: Headless browser orphans ---
    Get-Process -Name 'chrome', 'msedge', 'chromium' -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowTitle -eq '' -or $_.Path -match 'playwright|chrome-win' } |
        ForEach-Object {
            $freedMB += [math]::Round($_.WorkingSet64 / 1MB, 0)
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
            $zombies++
        }

    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
    Start-Sleep -Seconds 2

    # --- Verify ports are free ---
    $portMap2 = Get-PortProcessMap
    $stillBusy = @($script:DevPorts | Where-Object { $portMap2[$_].Count -gt 0 })
    if ($stillBusy.Count -gt 0) {
        $busyList = $stillBusy -join ', '
        Write-Host "  WARN: ports still in use: $busyList - force killing..." -ForegroundColor DarkYellow
        foreach ($port in $stillBusy) {
            foreach ($procId in $portMap2[$port]) {
                if ($procId -and $procId -gt 4) {
                    Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
                    taskkill /F /PID $procId 2>$null
                }
            }
        }
        Start-Sleep -Seconds 1
    }

    Write-Host "  Cleanup done: ports=$freedPorts zombies=$zombies approx ${freedMB}MB" -ForegroundColor Green
}

function Wait-HttpReady {
    param(
        [string]$Url,
        [int]$TimeoutSec = 120,
        [int[]]$OkStatus = @(200, 403),
        [string]$Label = 'Service'
    )
    for ($i = 0; $i -lt $TimeoutSec; $i++) {
        try {
            $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
            if ($OkStatus -contains $resp.StatusCode) {
                Write-Host "  $Label ready (${i}s)" -ForegroundColor Green
                return $true
            }
        } catch {
            $code = $null
            if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
            if ($code -and ($OkStatus -contains $code)) {
                Write-Host "  $Label ready (${i}s, HTTP $code)" -ForegroundColor Green
                return $true
            }
        }
        Start-Sleep -Seconds 1
    }
    Write-Host "  $Label startup timed out (${TimeoutSec}s)" -ForegroundColor Red
    return $false
}

function Ensure-EnvFile {
    param([string]$Dir, [string]$ExampleName = '.env.example', [string]$TargetName = '.env')
    $example = Join-Path $Dir $ExampleName
    $target = Join-Path $Dir $TargetName
    if (-not (Test-Path $target) -and (Test-Path $example)) {
        Copy-Item $example $target
        Write-Host "  Created $TargetName from $ExampleName" -ForegroundColor DarkYellow
    }
}

function Start-DevWindow {
    param([string]$Title, [string]$WorkDir, [string]$Command)
    $cmd = "cd '$WorkDir'; `$Host.UI.RawUI.WindowTitle = '$Title'; $Command"
    Start-Process powershell -ArgumentList @('-NoExit', '-Command', $cmd) | Out-Null
}

function Invoke-PreflightChecks {
    $allOk = $true

    # --- Check 1: .NET SDK version vs global.json ---
    $globalJson = Join-Path $root 'backend\global.json'
    if (Test-Path $globalJson) {
        $json = Get-Content $globalJson -Raw | ConvertFrom-Json
        $required = $json.sdk.version
        $rollFwd = $json.sdk.rollForward
        # Must run from backend/ so global.json takes effect for SDK resolution
        $backendDir = Join-Path $root 'backend'
        $resolved = & { Push-Location $backendDir; try { dotnet --version 2>$null } finally { Pop-Location } }
        if ($resolved) {
            $reqMajor = [int]($required.Split('.')[0])
            $resMajor = [int]($resolved.Split('.')[0])
            $reqMinor = [int]($required.Split('.')[1])
            $resMinor = [int]($resolved.Split('.')[1])

            $ok = $false
            if ($reqMajor -eq $resMajor) {
                if ($rollFwd -eq 'latestPatch' -and $reqMinor -eq $resMinor) { $ok = $true }
                elseif ($rollFwd -eq 'latestMinor') { $ok = $true }
                elseif ($rollFwd -eq 'latestMajor') { $ok = $true }
            }
            if ($rollFwd -eq 'latestMajor') { $ok = $true }

            if ($ok) {
                Write-Host "  SDK OK: required $required ($rollFwd), resolved to $resolved" -ForegroundColor DarkGray
            } else {
                Write-Host "  SDK MISMATCH: global.json requires $required ($rollFwd), resolved $resolved" -ForegroundColor Red
                Write-Host "    Install .NET $required SDK or update backend/global.json rollForward" -ForegroundColor Yellow
                $allOk = $false
            }
        }
    }

    # --- Check 2: SQL Server reachable (fast TCP socket, no ping) ---
    $connFile = Join-Path $root 'backend\application\JNPF.API.Entry\Configurations\ConnectionStrings.json'
    if (Test-Path $connFile) {
        try {
            $connJson = Get-Content $connFile -Raw | ConvertFrom-Json
            $configs = $connJson.ConnectionStrings.ConnectionConfigs
            if ($configs -and $configs.Count -gt 0) {
                $dbHost = $configs[0].Host
                $dbPort = if ($configs[0].Port) { [int]$configs[0].Port } else { 1433 }
                # Normalize: (local)/localhost -> 127.0.0.1, strip instance name for TCP check
                $dbHost = $dbHost -replace '^\(local\)', '127.0.0.1'
                $dbHost = $dbHost -replace '^localhost', '127.0.0.1'
                $dbHost = $dbHost -replace '\\.*$', ''  # strip \SQLEXPRESS etc.
                # .NET TcpClient with 3s timeout — no ICMP ping, instant result
                $reachable = $false
                try {
                    $client = New-Object System.Net.Sockets.TcpClient
                    $ar = $client.BeginConnect($dbHost, $dbPort, $null, $null)
                    $reachable = $ar.AsyncWaitHandle.WaitOne(3000)
                    if ($reachable) { $client.EndConnect($ar) }
                    $client.Close()
                } catch { $reachable = $false }
                if ($reachable) {
                    Write-Host "  SQL OK: $dbHost`:$dbPort reachable" -ForegroundColor DarkGray
                } else {
                    Write-Host "  SQL UNREACHABLE: $dbHost`:$dbPort — make sure SQL Server is running" -ForegroundColor DarkYellow
                }
            }
        } catch {
            Write-Host "  SQL check skipped (cannot parse config)" -ForegroundColor DarkGray
        }
    }

    # --- Check 3: frontend node_modules ---
    $frontendDir = Join-Path $root 'jnpf-web-vue3'
    if (-not (Test-Path (Join-Path $frontendDir 'node_modules'))) {
        Write-Host "  FRONTEND DEPS MISSING: jnpf-web-vue3/node_modules not found" -ForegroundColor DarkYellow
        Write-Host "    Run: cd jnpf-web-vue3 && pnpm install" -ForegroundColor Yellow
    } else {
        Write-Host "  node_modules OK: jnpf-web-vue3" -ForegroundColor DarkGray
    }

    if (-not $allOk) {
        Write-Host "  Preflight: some checks failed (see above) — continuing anyway..." -ForegroundColor DarkYellow
    } else {
        Write-Host "  Preflight: all checks passed" -ForegroundColor Green
    }
}

Write-Host "=== JNPF v5.2 Dev Startup (v4.0) ===" -ForegroundColor Cyan

# ================================================================
# Step 1/8: 清理端口与僵尸进程（内联，确保编译前无 DLL 锁）
# ================================================================
Write-Step '1' '8' 'Cleaning ports and zombie processes...'
Clear-DevEnvironment

if ($CleanupOnly) {
    Write-Host 'Cleanup-only mode - exiting.' -ForegroundColor Cyan
    exit 0
}

# ================================================================
# Step 2/8: 环境预检（SDK / SQL / node_modules）
# ================================================================
Write-Host ''
Write-Step '2' '8' 'Running preflight checks...'
Invoke-PreflightChecks

# ================================================================
# Step 3/8: 编译后端（单线程避免 MSB4166）
# ================================================================
Write-Host ''
Write-Step '3' '8' 'Building backend API Entry...'

$backendDir = Join-Path $root 'backend'
$backendProject = 'application/JNPF.API.Entry/JNPF.API.Entry.csproj'
$backendProjectPath = Join-Path $backendDir $backendProject
$backendDll = Join-Path $backendDir 'application/JNPF.API.Entry/bin/Debug/net8.0/JNPF.API.Entry.dll'

$objDir = Join-Path $backendDir 'application/JNPF.API.Entry/obj'
if (-not (Test-Path $objDir)) {
    Write-Host '  First run: restoring NuGet packages...'
    dotnet restore $backendProjectPath --verbosity quiet
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

Write-Host '  dotnet build...'
# Set UTF-8 so MSBuild Chinese errors display correctly; run from backend/ for global.json
$prevEncoding = [Console]::OutputEncoding
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Push-Location $backendDir
try {
    $buildOutput = & {
        dotnet build $backendProjectPath `
            -v q /nologo `
            -p:RunAnalyzers=false `
            -p:CI_BUILD=false 2>&1
    }
} finally {
    Pop-Location
    [Console]::OutputEncoding = $prevEncoding
}
$buildExit = $LASTEXITCODE

if ($buildExit -ne 0) {
    # Detect MSB3027/MSB3021 (DLL file lock — from parallel build race or stale process)
    $lockErrors = ($buildOutput | Select-String 'MSB3027|MSB3021' | ForEach-Object { $_.Line })
    if ($lockErrors.Count -gt 0) {
        Write-Host "  DLL file lock detected (MSB3027/MSB3021) - killing residual processes..." -ForegroundColor DarkYellow
        Write-Host "  Locked files:" -ForegroundColor DarkGray
        $lockErrors | Select-Object -First 8 | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
        Clear-DevEnvironment
        Start-Sleep -Seconds 3
        Write-Host "  Retrying with single-thread (-m:1) to avoid parallel DLL race..."
        [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
        Push-Location $backendDir
        try {
            dotnet build $backendProjectPath `
                -v q /nologo `
                -m:1 `
                -p:BuildInParallel=false `
                -p:RunAnalyzers=false `
                -p:CI_BUILD=false 2>&1 | Out-Null
        } finally {
            Pop-Location
            [Console]::OutputEncoding = $prevEncoding
        }
        $buildExit = $LASTEXITCODE
    } else {
        Write-Host '  First build failed - cleaning zombies and retrying once...' -ForegroundColor DarkYellow
        Clear-DevEnvironment
        $buildExit = Invoke-BackendBuild $backendProjectPath
    }
}
if ($buildExit -ne 0) {
    Write-Host '  Backend build failed - re-run with verbose log:' -ForegroundColor Red
    Write-Host "    cd backend; dotnet build $backendProject -v minimal" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $backendDll)) {
    Write-Host "  Backend build incomplete: missing $backendDll" -ForegroundColor Red
    exit 1
}
Write-Host "  Build OK: $(Split-Path $backendDll -Leaf)" -ForegroundColor Green

# ================================================================
# Step 4/8: 启动后端
# ================================================================
Write-Host ''
Write-Step '4' '8' 'Starting backend API...'

Start-DevWindow -Title 'JNPF Backend :5000' -WorkDir $backendDir -Command `
    "dotnet run --project '$backendProject' --no-build --urls 'http://0.0.0.0:5000'"

$backendReady = Wait-HttpReady -Url 'http://127.0.0.1:5000' -Label 'Backend :5000'
if (-not $backendReady) {
    Write-Host '  Check the Backend window for startup errors' -ForegroundColor Red
    exit 1
}

# ================================================================
# Step 5/8: 启动 SA Service
# ================================================================
Write-Host ''
Write-Step '5' '8' 'Starting SA Service...'

$saDir = Join-Path $root 'sa-service'
$saDbBackend = 'inmemory'
$saDbConnArg = ''
$connPath = Join-Path $root 'backend\application\JNPF.API.Entry\Configurations\ConnectionStrings.json'
if (Test-Path $connPath) {
    try {
        $connJson = Get-Content $connPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $defaultConn = $connJson.ConnectionStrings.DefaultConnection
        if ($defaultConn) {
            $saDbBackend = 'sqlserver'
            $escapedConn = $defaultConn -replace "'", "''"
            $saDbConnArg = "; `$env:SA_DB_CONNECTION_STRING='$escapedConn'"
            Write-Host '  SA DB: sqlserver (from ConnectionStrings.json)' -ForegroundColor DarkGray
        }
    } catch {
        Write-Host '  SA DB: inmemory (ConnectionStrings.json parse failed)' -ForegroundColor DarkYellow
    }
}
Start-DevWindow -Title 'JNPF SA :3001' -WorkDir $saDir -Command `
    "`$env:SA_SERVICE_PORT='3001'; `$env:SA_DB_BACKEND='$saDbBackend'$saDbConnArg; `$env:LLM_GATEWAY_URL='http://127.0.0.1:5000/api/LlmGateway/ChatAsync'; Write-Host 'SA Service starting...'; npx tsx src/server.ts"
Write-Host '  SA Service launched' -ForegroundColor Green

# ================================================================
# Step 6/8: 启动 PC 前端
# ================================================================
Write-Host ''
Write-Step '6' '8' 'Starting PC frontend (Vite)...'

$frontendDir = Join-Path $root 'jnpf-web-vue3'
$viteCache = Join-Path $frontendDir 'node_modules\.vite'
if (Test-Path $viteCache) {
    Write-Host '  Clearing Vite optimize cache...'
    Remove-Item -Recurse -Force $viteCache -ErrorAction SilentlyContinue
}

Start-DevWindow -Title 'JNPF PC :3100' -WorkDir $frontendDir -Command `
    "Write-Host 'Vite PC starting...'; pnpm dev -- --force"

$null = Wait-HttpReady -Url 'http://127.0.0.1:3100' -TimeoutSec 90 -Label 'PC frontend :3100'

# ================================================================
# Step 7/8: 启动数字大屏
# ================================================================
Write-Host ''
Write-Step '7' '8' 'Starting DataScreen (DataV)...'

$datascreenDir = Join-Path $root 'jnpf-web-datascreen'
Ensure-EnvFile -Dir $datascreenDir

if (-not (Test-Path (Join-Path $datascreenDir 'node_modules'))) {
    Write-Host '  Installing datascreen dependencies (first run)...'
    Push-Location $datascreenDir
    pnpm install
    Pop-Location
    if ($LASTEXITCODE -ne 0) {
        Write-Host '  datascreen pnpm install failed' -ForegroundColor Red
    }
}

Start-DevWindow -Title 'JNPF DataV :3102' -WorkDir $datascreenDir -Command `
    "Write-Host 'DataScreen starting on :3102...'; pnpm dev"

$null = Wait-HttpReady -Url 'http://127.0.0.1:3102/DataV/' -TimeoutSec 90 -Label 'DataScreen :3102'

# ================================================================
# Step 8/8: 启动 UniApp H5（小程序 Web 预览）
# ================================================================
Write-Host ''
Write-Step '8' '8' 'Starting UniApp H5...'

$appDir = Join-Path $root 'jnpf-app-vue3'
$h5Root = Join-Path $appDir 'unpackage/dist/build/web'
$proxyScript = Join-Path $appDir 'scripts/proxy_server.py'

if (Test-Path (Join-Path $h5Root 'index.html')) {
    $python = $null
    foreach ($cmd in @('python', 'python3', 'py')) {
        if (Get-Command $cmd -ErrorAction SilentlyContinue) {
            $python = $cmd
            break
        }
    }
    if ($python) {
        Start-DevWindow -Title 'JNPF H5 :3800' -WorkDir $appDir -Command `
            "Write-Host 'UniApp H5 proxy on :3800...'; & '$python' scripts/proxy_server.py"
        $null = Wait-HttpReady -Url 'http://127.0.0.1:3800' -TimeoutSec 30 -Label 'UniApp H5 :3800'
    } else {
        Write-Host '  Python not found - skip H5 proxy (install Python 3 or use HBuilderX)' -ForegroundColor Yellow
    }
} else {
    Write-Host '  H5 build not found: unpackage/dist/build/web' -ForegroundColor Yellow
    Write-Host '  Build in HBuilderX: 发行 -> 网站-H5, then rerun start-dev.ps1' -ForegroundColor Yellow
}

# ================================================================
# Done
# ================================================================
Write-Host ''
Write-Host '=== Dev environment ready ===' -ForegroundColor Green
Write-Host '  Backend:    http://127.0.0.1:5000' -ForegroundColor Cyan
Write-Host '  PC:         http://127.0.0.1:3100' -ForegroundColor Cyan
Write-Host '  DataScreen: http://127.0.0.1:3102/DataV/' -ForegroundColor Cyan
Write-Host '  UniApp H5:  http://127.0.0.1:3800' -ForegroundColor Cyan
Write-Host '  SA:         http://127.0.0.1:3001' -ForegroundColor Cyan
Write-Host '  Login:      admin / 123456' -ForegroundColor White
Write-Host ''
Write-Host 'Open http://127.0.0.1:3100 to start' -ForegroundColor White
