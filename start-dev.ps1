# start-dev.ps1 v4.1 — JNPF v5.2 全栈开发环境一键启动
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

# --- Console encoding helpers (ASCII-only comments: PS 5.1 needs UTF-8 BOM) ---
# Do NOT call chcp mid-script: it redraws/clears the console and wipes prior step output.
# Set DOTNET_CLI_UI_LANGUAGE=en for clean English MSBuild; set encodings without chcp.
$script:CliOutputPrepared = $false

function Set-SafeConsoleEncoding {
    param($Encoding)
    if ($null -eq $Encoding) { return }
    try { [Console]::OutputEncoding = $Encoding } catch { }
}

function Set-SafeConsoleInputEncoding {
    param($Encoding)
    if ($null -eq $Encoding) { return }
    try { [Console]::InputEncoding = $Encoding } catch { }
}

function Enable-CleanDotnetCliOutput {
    if ($script:CliOutputPrepared) { return }
    # Prefer UTF-8 OutputEncoding only (no chcp) to keep scrollback of steps 1-2.
    Set-SafeConsoleEncoding ([System.Text.Encoding]::UTF8)
    Set-SafeConsoleInputEncoding ([System.Text.Encoding]::UTF8)
    $env:DOTNET_CLI_UI_LANGUAGE = 'en'
    $script:CliOutputPrepared = $true
}

# Prepare CLI output once at start (before any Write-Step), so build does not clear history.
Enable-CleanDotnetCliOutput

function Invoke-BackendBuild {
    param([string]$ProjectPath)
    # Run from backend/ so global.json resolves SDK version correctly
    $backendDir = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $ProjectPath))
    try {
        Enable-CleanDotnetCliOutput
        Push-Location $backendDir
        dotnet build $ProjectPath `
            -v q /nologo `
            -p:RunAnalyzers=false `
            -p:CI_BUILD=false `
            -p:IsPackable=false
    } finally {
        Pop-Location -ErrorAction SilentlyContinue
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

function Stop-ProcessTree {
    param(
        [int]$ProcessId,
        [string]$Reason = ''
    )
    if ($ProcessId -le 4) { return $false }
    if ($ProcessId -eq $PID) { return $false }  # never kill ourselves

    $proc = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($null -eq $proc) { return $false }

    $name = $proc.ProcessName
    $mb = [math]::Round($proc.WorkingSet64 / 1MB, 0)

    # /T = kill child tree (node→esbuild, dotnet→host, powershell→child)
    $null = & taskkill.exe /F /T /PID $ProcessId 2>$null
    if ($LASTEXITCODE -ne 0) {
        Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
    }

    $label = if ($Reason) { $Reason } else { 'kill' }
    Write-Host "  $label`: $name (PID $ProcessId, ~${mb}MB)" -ForegroundColor DarkGray
    return $true
}

function Test-IsJnpfDevCommandLine {
    param([string]$CommandLine, [string]$ProcessName)

    if ([string]::IsNullOrWhiteSpace($CommandLine)) { return $false }

    $rootEsc = [regex]::Escape($root)
    $rootAlt = [regex]::Escape(($root -replace '\\', '/'))

    # Strongest: command line references this repo path
    if ($CommandLine -match $rootEsc -or $CommandLine -match $rootAlt) { return $true }

    # Backend host / project (cwd may be relative — Path alone is useless)
    if ($ProcessName -match '^(dotnet|JNPF\.API\.Entry)$') {
        if ($CommandLine -match 'JNPF\.API\.Entry|zx_lowcode_netcore|InteAssistant|SaPipeline') {
            return $true
        }
    }

    # Frontend / SA / datascreen / H5 tooling
    if ($ProcessName -match '^(node|nodejs)$') {
        if ($CommandLine -match 'jnpf-web-vue3|jnpf-web-datascreen|jnpf-app-vue3|sa-service|\\vite\\|/vite/|tsx\s+src[/\\]server\.ts') {
            return $true
        }
    }

    # UniApp H5 proxy
    if ($ProcessName -match '^python') {
        if ($CommandLine -match 'proxy_server\.py') { return $true }
    }

    # MSBuild locking JNPF outputs
    if ($ProcessName -match '^(MSBuild|msbuild)$') {
        if ($CommandLine -match 'JNPF|zx_lowcode') { return $true }
    }

    return $false
}

function Clear-DevEnvironment {
    $freedPorts = 0
    $zombies = 0
    $freedMB = 0
    $killedPids = @{}  # dedupe across layers

    $modeLabel = if ($script:IsAdmin) { 'Admin' } else { 'User' }
    Write-Host "  Mode: $modeLabel | Repo: $root" -ForegroundColor DarkGray

    # --- Layer 1: Kill by listening port (single netstat pass) ---
    $portMap = Get-PortProcessMap
    foreach ($port in $script:DevPorts) {
        foreach ($procId in ($portMap[$port] | Select-Object -Unique)) {
            $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
            $mb = if ($proc) { [math]::Round($proc.WorkingSet64 / 1MB, 0) } else { 0 }
            if (Stop-ProcessTree -ProcessId $procId -Reason "Port $port") {
                $killedPids[$procId] = $true
                $freedPorts++
                $freedMB += $mb
            }
        }
    }

    # --- Layer 2: CIM CommandLine scan (real zombie detection) ---
    # Process.Path is always ...\dotnet.exe / ...\node.exe — useless for repo matching.
    # Win32_Process.CommandLine carries the project args / absolute module paths.
    $targetNames = @(
        'dotnet.exe', 'node.exe', 'nodejs.exe',
        'JNPF.API.Entry.exe',
        'python.exe', 'python3.exe', 'py.exe',
        'MSBuild.exe',
        'VBCSCompiler.exe'
    )

    try {
        $cimProcs = Get-CimInstance Win32_Process -ErrorAction Stop |
            Where-Object { $targetNames -contains $_.Name }
    } catch {
        Write-Host "  WARN: CIM scan failed ($($_.Exception.Message)) - name-only fallback" -ForegroundColor DarkYellow
        $cimProcs = @()
        # Minimal fallback without CommandLine: kill API host by name only
        foreach ($n in @('JNPF.API.Entry', 'VBCSCompiler')) {
            foreach ($p in @(Get-Process -Name $n -ErrorAction SilentlyContinue)) {
                if ($killedPids.ContainsKey($p.Id)) { continue }
                $mb = [math]::Round($p.WorkingSet64 / 1MB, 0)
                if (Stop-ProcessTree -ProcessId $p.Id -Reason "Fallback $n") {
                    $killedPids[$p.Id] = $true
                    $zombies++
                    $freedMB += $mb
                }
            }
        }
    }

    foreach ($cim in $cimProcs) {
        $procId = [int]$cim.ProcessId
        if ($killedPids.ContainsKey($procId)) { continue }
        if ($procId -eq $PID) { continue }

        $procName = [System.IO.Path]::GetFileNameWithoutExtension($cim.Name)
        $cmd = $cim.CommandLine

        # VBCSCompiler: shared Roslyn server; restarting it unlocks stale DLL handles (MSB3027)
        $isVbcs = ($procName -eq 'VBCSCompiler')
        $isApiEntry = ($procName -eq 'JNPF.API.Entry')
        $isJnpf = $isApiEntry -or $isVbcs -or (Test-IsJnpfDevCommandLine -CommandLine $cmd -ProcessName $procName)

        if (-not $isJnpf) { continue }

        $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
        $mb = if ($proc) { [math]::Round($proc.WorkingSet64 / 1MB, 0) } else { 0 }
        $reason = if ($isVbcs) { 'VBCSCompiler' } elseif ($isApiEntry) { 'API.Entry' } else { "Zombie $procName" }
        if (Stop-ProcessTree -ProcessId $procId -Reason $reason) {
            $killedPids[$procId] = $true
            $zombies++
            $freedMB += $mb
        }
    }

    # --- Layer 3: Orphan PowerShell windows left by previous start-dev ---
    $shellProcs = @(Get-Process -Name 'powershell', 'pwsh' -ErrorAction SilentlyContinue)
    foreach ($shell in $shellProcs) {
        if ($shell.Id -eq $PID) { continue }
        if ($killedPids.ContainsKey($shell.Id)) { continue }
        $title = ''
        try { $title = $shell.MainWindowTitle } catch { }
        if ($title -match '^JNPF (Backend|PC|SA|DataV|H5)\b') {
            $mb = [math]::Round($shell.WorkingSet64 / 1MB, 0)
            if (Stop-ProcessTree -ProcessId $shell.Id -Reason "Shell '$title'") {
                $killedPids[$shell.Id] = $true
                $zombies++
                $freedMB += $mb
            }
        }
    }

    # --- Layer 4: Playwright / bundled Chromium orphans only (never touch user Chrome) ---
    $browserProcs = @(Get-Process -Name 'chrome', 'msedge', 'chromium' -ErrorAction SilentlyContinue |
        Where-Object {
            -not $killedPids.ContainsKey($_.Id) -and
            $_.Path -and
            $_.Path -match 'playwright|chrome-win|chromium-browser'
        })
    foreach ($browser in $browserProcs) {
        $mb = [math]::Round($browser.WorkingSet64 / 1MB, 0)
        if (Stop-ProcessTree -ProcessId $browser.Id -Reason 'Playwright orphan') {
            $killedPids[$browser.Id] = $true
            $zombies++
            $freedMB += $mb
        }
    }

    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
    Start-Sleep -Seconds 2

    # --- Layer 5: Verify ports are free; force-kill leftovers ---
    $portMap2 = Get-PortProcessMap
    $stillBusy = @($script:DevPorts | Where-Object { $portMap2[$_].Count -gt 0 })
    if ($stillBusy.Count -gt 0) {
        $busyList = $stillBusy -join ', '
        Write-Host "  WARN: ports still in use: $busyList - force killing..." -ForegroundColor DarkYellow
        foreach ($port in $stillBusy) {
            foreach ($procId in ($portMap2[$port] | Select-Object -Unique)) {
                if ($procId -and $procId -gt 4 -and -not $killedPids.ContainsKey($procId)) {
                    if (Stop-ProcessTree -ProcessId $procId -Reason "Force port $port") {
                        $killedPids[$procId] = $true
                        $freedPorts++
                    }
                }
            }
        }
        Start-Sleep -Seconds 1
    }

    $totalKilled = $killedPids.Count
    Write-Host "  Cleanup done: ports=$freedPorts zombies=$zombies totalPids=$totalKilled approx ${freedMB}MB" -ForegroundColor Green
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

Write-Host "=== JNPF v5.2 Dev Startup (v4.1) ===" -ForegroundColor Cyan

# ================================================================
# Step 1/8: 清理端口 + node/.NET 僵尸进程（CIM CommandLine，确保编译前无 DLL 锁）
# ================================================================
Write-Step '1' '8' 'Cleaning ports and zombie processes (node / .NET host)...'
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
# UTF-8 + English CLI：避免中文 MSBuild 在 CP936 控制台乱码（不恢复编码，避免 PS5.1 null 异常）
Enable-CleanDotnetCliOutput
Push-Location $backendDir
try {
    $buildOutput = & {
        dotnet build $backendProjectPath `
            -v q /nologo `
            -p:RunAnalyzers=false `
            -p:CI_BUILD=false 2>&1
    }
} finally {
    Pop-Location -ErrorAction SilentlyContinue
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
        Enable-CleanDotnetCliOutput
        Push-Location $backendDir
        try {
            dotnet build $backendProjectPath `
                -v q /nologo `
                -m:1 `
                -p:BuildInParallel=false `
                -p:RunAnalyzers=false `
                -p:CI_BUILD=false 2>&1 | Out-Null
        } finally {
            Pop-Location -ErrorAction SilentlyContinue
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
    "Write-Host 'Vite PC starting...'; pnpm dev"

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
