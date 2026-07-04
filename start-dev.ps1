# start-dev.ps1 v3.1 — JNPF v5.2 全栈开发环境一键启动
# Encoding: UTF-8 with BOM (PowerShell 5.1 requirement)
# 启动：PC 前端 :3100 | 数字大屏 :3102 | UniApp H5 :3800 | 后端 :5000 | SA :3001
param(
    [switch]$CleanupOnly
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

# 端口规范见 docs/conventions/ports.md
$script:DevPorts = @(3100, 3102, 3800, 5000, 3001)

function Invoke-BackendBuild {
    param([string]$ProjectPath)
    # -v q + RunAnalyzers=false：开发启动只关心 error，避免 1 万条 CA/SA 警告淹没 MSB4166/MSB3027
    dotnet build $ProjectPath `
        -v q /nologo `
        /nodeReuse:false `
        -m:1 `
        -p:BuildInParallel=false `
        -p:UseSharedCompilation=false `
        -p:RunAnalyzers=false
    return $LASTEXITCODE
}

function Write-Step([string]$Num, [string]$Total, [string]$Msg) {
    Write-Host "[$Num/$Total] $Msg" -ForegroundColor Yellow
}

function Clear-DevEnvironment {
    $prevErr = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'

    $freedPorts = 0
    $zombies = 0
    $freedMB = 0

    foreach ($port in $script:DevPorts) {
        $listeners = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty OwningProcess -Unique
        foreach ($procId in $listeners) {
            $name = (Get-Process -Id $procId -ErrorAction SilentlyContinue).ProcessName
            Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
            Write-Host "  Port $port freed (PID $procId $name)"
            $freedPorts++
        }
    }

    $dotnetProcs = Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match 'MSBuild|VBCSCompiler|dotnet\.dll run|JNPF\.API\.Entry' }
    foreach ($item in $dotnetProcs) {
        $p = Get-Process -Id $item.ProcessId -ErrorAction SilentlyContinue
        if ($null -eq $p) { continue }
        $freedMB += [math]::Round($p.WorkingSet64 / 1MB, 0)
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  Killed dotnet PID $($p.Id)"
        $zombies++
    }

    $nodeProcs = Get-CimInstance Win32_Process -Filter "Name = 'node.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match 'jnpf|JNPF|vite|tsx|sa-service|uni-app|3800|3100|3102' }
    foreach ($item in $nodeProcs) {
        $p = Get-Process -Id $item.ProcessId -ErrorAction SilentlyContinue
        if ($null -eq $p) { continue }
        $freedMB += [math]::Round($p.WorkingSet64 / 1MB, 0)
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  Killed node PID $($p.Id)"
        $zombies++
    }

    $pyFilter = "Name = 'python.exe' OR Name = 'python3.exe'"
    $pyProcs = Get-CimInstance Win32_Process -Filter $pyFilter -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -match 'proxy_server\.py' }
    foreach ($item in $pyProcs) {
        $p = Get-Process -Id $item.ProcessId -ErrorAction SilentlyContinue
        if ($null -eq $p) { continue }
        $freedMB += [math]::Round($p.WorkingSet64 / 1MB, 0)
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  Killed python PID $($p.Id)"
        $zombies++
    }

    $browsers = Get-Process -Name 'chrome', 'msedge', 'chromium' -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowTitle -eq '' -or $_.Path -match 'playwright|chrome-win' }
    foreach ($p in $browsers) {
        $freedMB += [math]::Round($p.WorkingSet64 / 1MB, 0)
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        $zombies++
    }

    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
    Start-Sleep -Seconds 2

    $stillBusy = @()
    foreach ($port in $script:DevPorts) {
        if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
            $stillBusy += $port
        }
    }
    if ($stillBusy.Count -gt 0) {
        $busyList = $stillBusy -join ', '
        Write-Host "  WARN: ports still in use: $busyList - retrying..." -ForegroundColor DarkYellow
        foreach ($port in $stillBusy) {
            $pids = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
                Select-Object -ExpandProperty OwningProcess -Unique
            foreach ($procId in $pids) {
                Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
            }
        }
        Start-Sleep -Seconds 1
    }

    Write-Host "  Cleanup done: ports=$freedPorts processes=$zombies approx ${freedMB}MB" -ForegroundColor Green
    $ErrorActionPreference = $prevErr
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

Write-Host "=== JNPF v5.2 Dev Startup (v3.1) ===" -ForegroundColor Cyan

# ================================================================
# Step 1/7: 清理端口与僵尸进程（内联，确保编译前无 DLL 锁）
# ================================================================
Write-Step '1' '7' 'Cleaning ports and zombie processes...'
Clear-DevEnvironment

if ($CleanupOnly) {
    Write-Host 'Cleanup-only mode - exiting.' -ForegroundColor Cyan
    exit 0
}

# ================================================================
# Step 2/7: 编译后端（单线程避免 MSB4166）
# ================================================================
Write-Host ''
Write-Step '2' '7' 'Building backend API Entry...'

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

Write-Host '  dotnet build (quiet, single-thread, analyzers off)...'
$buildExit = Invoke-BackendBuild $backendProjectPath
if ($buildExit -ne 0) {
    Write-Host '  First build failed - cleaning zombies and retrying once...' -ForegroundColor DarkYellow
    Clear-DevEnvironment
    $buildExit = Invoke-BackendBuild $backendProjectPath
}
if ($buildExit -ne 0) {
    Write-Host '  Backend build failed - re-run with verbose log:' -ForegroundColor Red
    Write-Host "    cd backend; dotnet build $backendProject -v minimal /nodeReuse:false -m:1" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $backendDll)) {
    Write-Host "  Backend build incomplete: missing $backendDll" -ForegroundColor Red
    exit 1
}
Write-Host "  Build OK: $(Split-Path $backendDll -Leaf)" -ForegroundColor Green

# ================================================================
# Step 3/7: 启动后端
# ================================================================
Write-Host ''
Write-Step '3' '7' 'Starting backend API...'

Start-DevWindow -Title 'JNPF Backend :5000' -WorkDir $backendDir -Command `
    "dotnet run --project '$backendProject' --no-build --urls 'http://0.0.0.0:5000'"

$backendReady = Wait-HttpReady -Url 'http://127.0.0.1:5000' -Label 'Backend :5000'
if (-not $backendReady) {
    Write-Host '  Check the Backend window for startup errors' -ForegroundColor Red
    exit 1
}

# ================================================================
# Step 4/7: 启动 SA Service
# ================================================================
Write-Host ''
Write-Step '4' '7' 'Starting SA Service...'

$saDir = Join-Path $root 'sa-service'
Start-DevWindow -Title 'JNPF SA :3001' -WorkDir $saDir -Command `
    "`$env:SA_SERVICE_PORT='3001'; `$env:SA_DB_BACKEND='inmemory'; `$env:LLM_GATEWAY_URL='http://127.0.0.1:5000/api/LlmGateway/ChatAsync'; Write-Host 'SA Service starting...'; npx tsx src/server.ts"
Write-Host '  SA Service launched' -ForegroundColor Green

# ================================================================
# Step 5/7: 启动 PC 前端
# ================================================================
Write-Host ''
Write-Step '5' '7' 'Starting PC frontend (Vite)...'

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
# Step 6/7: 启动数字大屏
# ================================================================
Write-Host ''
Write-Step '6' '7' 'Starting DataScreen (DataV)...'

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
# Step 7/7: 启动 UniApp H5（小程序 Web 预览）
# ================================================================
Write-Host ''
Write-Step '7' '7' 'Starting UniApp H5...'

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
