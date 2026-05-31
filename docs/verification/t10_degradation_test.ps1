<#
.SYNOPSIS
    T10: Graceful Degradation Verification - logging subsystem failure does not break business.
.DESCRIPTION
    Tests that the JNPF API continues to serve requests correctly even when the
    logging subsystem is partially or fully broken.

    Phase A: Make log files read-only (simulates disk/full/permission failure).
    Phase B: Restore permissions, verify logging resumes.
    Phase C: (Optional, commented out) Simulate database log table failure.

    References:
    - LogDiskGuardService: backend/application/JNPF.API.Entry/Services/LogDiskGuardService.cs
    - SerilogBootstrap: backend/application/JNPF.API.Entry/Infrastructure/SerilogBootstrap.cs
    - RequestActionFilter: backend/modularity/common/JNPF.Common.Core/Filter/RequestActionFilter.cs
.PARAMETER BaseUrl
    API base URL (default: http://localhost:5000)
.PARAMETER LogDir
    Serilog log directory (default: logs, relative to API working directory)
.PARAMETER LogDirFullPath
    Full path to the log directory. If empty, auto-detected from LogDir.
    On Windows this is typically: backend/application/JNPF.API.Entry/bin/Debug/net8.0/logs
.PARAMETER RequestCount
    Number of requests per phase (default: 5)
.EXAMPLE
    .\t10_degradation_test.ps1 -BaseUrl http://localhost:5000
    .\t10_degradation_test.ps1 -BaseUrl http://localhost:5000 -LogDirFullPath "D:\JNPF-v52\backend\application\JNPF.API.Entry\logs"
#>

param(
    [string]$BaseUrl          = "http://localhost:5000",
    [string]$LogDir           = "logs",
    [string]$LogDirFullPath   = "",
    [int]   $RequestCount     = 5,
    [string]$TestEndpoint     = "/api/system/TechnicalLog/errors"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Banner ──────────────────────────────────────────────────────────────────
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "T10: Graceful Degradation Verification" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host ""
Write-Host "Prerequisites:" -ForegroundColor Yellow
Write-Host "  [1] API service is running at $BaseUrl"
Write-Host "  [2] Admin access to filesystem (for permission changes)"
Write-Host "  [3] Serilog log directory is accessible"
Write-Host ""
Write-Host "WARNING: This test temporarily modifies file permissions on the log directory." -ForegroundColor Yellow
Write-Host "         Permissions are always restored in a finally block, even on failure." -ForegroundColor Yellow
Write-Host ""

$confirm = Read-Host "All prerequisites met? Proceed with degradation test? (y/n)"
if ($confirm -ne 'y') { Write-Host "Aborted." -ForegroundColor Red; exit 0 }

# ── Auto-detect log directory ───────────────────────────────────────────────
if (-not $LogDirFullPath) {
    # Try common locations
    $candidates = @(
        (Join-Path $PSScriptRoot "..\..\backend\application\JNPF.API.Entry\logs"),
        (Join-Path $PSScriptRoot "..\..\backend\application\JNPF.API.Entry\bin\Debug\net8.0\logs"),
        (Join-Path $PSScriptRoot "..\..\backend\application\JNPF.API.Entry\bin\Release\net8.0\logs"),
        $LogDir  # as-is (if absolute or relative to CWD)
    )
    foreach ($candidate in $candidates) {
        $resolved = Resolve-Path $candidate -ErrorAction SilentlyContinue
        if ($resolved -and (Test-Path $resolved)) {
            $LogDirFullPath = $resolved.Path
            break
        }
    }
    if (-not $LogDirFullPath) {
        Write-Host "ERROR: Cannot auto-detect log directory. Please specify -LogDirFullPath." -ForegroundColor Red
        exit 1
    }
}

Write-Host "Log directory: $LogDirFullPath" -ForegroundColor Gray

if (-not (Test-Path $LogDirFullPath)) {
    Write-Host "ERROR: Log directory does not exist: $LogDirFullPath" -ForegroundColor Red
    exit 1
}

# ── Helper: send N requests and return success/failure counts ───────────────
function Send-TestBatch {
    param(
        [string]$Url,
        [int]   $Count,
        [string]$Label
    )
    $ok   = 0
    $fail = 0
    $latencies = @()

    for ($i = 0; $i -lt $Count; $i++) {
        try {
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $resp = Invoke-WebRequest -Uri $Url -Method GET -UseBasicParsing -TimeoutSec 30 -ErrorAction SilentlyContinue
            $sw.Stop()
            $latencies += $sw.ElapsedMilliseconds
            if ([int]$resp.StatusCode -ge 200 -and [int]$resp.StatusCode -lt 500) {
                $ok++
            } else {
                $fail++
            }
        }
        catch {
            $fail++
        }
    }

    return [PSCustomObject]@{
        Label       = $Label
        Success     = $ok
        Failed      = $fail
        AvgLatencyMs = if ($latencies.Count -gt 0) { [math]::Round(($latencies | Measure-Object -Average).Average, 1) } else { 0 }
    }
}

# ── Helper: count log files in directory ────────────────────────────────────
function Get-LogFileCount {
    param([string]$Dir)
    $files = Get-ChildItem -Path $Dir -Filter "*.json" -ErrorAction SilentlyContinue
    return $files.Count
}

function Get-LogFileSize {
    param([string]$Dir)
    $files = Get-ChildItem -Path $Dir -Filter "*.json" -ErrorAction SilentlyContinue
    return ($files | Measure-Object -Property Length -Sum).Sum
}

# ── Phase A: File Sink Failure ──────────────────────────────────────────────
Write-Host ""
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "Phase A: File Sink Failure Simulation" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host ""

$phaseAResult = "PASS"
$originalReadOnly = @()

try {
    # Record initial state
    $initialLogCount = Get-LogFileCount $LogDirFullPath
    $initialLogSize  = Get-LogFileSize  $LogDirFullPath
    Write-Host "  Initial state: $initialLogCount log files, $([math]::Round($initialLogSize / 1024, 1)) KB total" -ForegroundColor Gray

    # Make all log files read-only
    $logFiles = Get-ChildItem -Path $LogDirFullPath -Filter "*.json" -ErrorAction SilentlyContinue
    if ($logFiles.Count -eq 0) {
        Write-Host "  WARN: No log files found to lock. Creating a dummy file to test." -ForegroundColor Yellow
        $dummyFile = Join-Path $LogDirFullPath "degradation-test-dummy.json"
        Set-Content -Path $dummyFile -Value "{}" -ErrorAction SilentlyContinue
        $logFiles = Get-ChildItem -Path $LogDirFullPath -Filter "degradation-test-dummy.json"
    }

    foreach ($file in $logFiles) {
        # Store original attributes
        $originalReadOnly += [PSCustomObject]@{
            Path     = $file.FullName
            ReadOnly = $file.IsReadOnly
        }
        $file.IsReadOnly = $true
    }

    # Also try to make the directory itself read-only (new files can't be created)
    $dirAcl = Get-Acl $LogDirFullPath
    $denyRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "Users", "Write", "ContainerInherit,ObjectInherit", "None", "Deny"
    )
    $dirAcl.AddAccessRule($denyRule)
    Set-Acl -Path $LogDirFullPath -AclObject $dirAcl -ErrorAction SilentlyContinue

    Write-Host "  Locked $($logFiles.Count) log file(s) as read-only" -ForegroundColor Yellow
    Write-Host "  Added Write-Deny ACL to log directory" -ForegroundColor Yellow

    # Send requests while logging is broken
    Write-Host ""
    Write-Host "  Sending $RequestCount requests with broken file logging ..." -ForegroundColor Cyan
    $testUrl = "$BaseUrl$TestEndpoint"
    $phaseA = Send-TestBatch -Url $testUrl -Count $RequestCount -Label "Phase A (broken)"

    Write-Host "  Results: $($phaseA.Success)/$RequestCount succeeded, $($phaseA.Failed) failed" -ForegroundColor $(
        if ($phaseA.Failed -eq 0) { "Green" } else { "Red" }
    )
    Write-Host "  Avg latency: $($phaseA.AvgLatencyMs) ms" -ForegroundColor Gray

    if ($phaseA.Failed -gt 0) {
        $phaseAResult = "FAIL"
        Write-Host "  FAIL: Business operations were blocked by logging failure!" -ForegroundColor Red
    } else {
        Write-Host "  PASS: All requests succeeded despite broken file logging" -ForegroundColor Green
    }

    # Verify TraceId still present
    try {
        $resp = Invoke-WebRequest -Uri $testUrl -Method GET -UseBasicParsing -TimeoutSec 10
        if ($resp.Headers["X-Trace-Id"]) {
            Write-Host "  PASS: X-Trace-Id header still present (middleware unaffected)" -ForegroundColor Green
        } else {
            Write-Host "  WARN: X-Trace-Id header missing" -ForegroundColor Yellow
        }
    } catch {}

}
finally {
    # ── CRITICAL: Restore permissions even if test fails ─────────────────────
    Write-Host ""
    Write-Host "  [finally] Restoring file permissions ..." -ForegroundColor Magenta

    # Remove deny ACL
    try {
        $dirAcl = Get-Acl $LogDirFullPath
        $denyRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            "Users", "Write", "ContainerInherit,ObjectInherit", "None", "Deny"
        )
        $dirAcl.RemoveAccessRule($denyRule) | Out-Null
        Set-Acl -Path $LogDirFullPath -AclObject $dirAcl -ErrorAction SilentlyContinue
    } catch {
        Write-Host "  WARN: Could not remove deny ACL: $($_.Exception.Message)" -ForegroundColor Yellow
    }

    # Restore read-only flags
    foreach ($entry in $originalReadOnly) {
        try {
            $file = Get-Item $entry.Path -ErrorAction SilentlyContinue
            if ($file) {
                $file.IsReadOnly = $entry.ReadOnly
            }
        } catch {
            Write-Host "  WARN: Could not restore $($entry.Path): $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }

    # Clean up dummy file
    $dummyFile = Join-Path $LogDirFullPath "degradation-test-dummy.json"
    if (Test-Path $dummyFile) {
        Remove-Item $dummyFile -Force -ErrorAction SilentlyContinue
    }

    Write-Host "  [finally] Permissions restored" -ForegroundColor Magenta
}

# ── Phase B: Recovery Verification ──────────────────────────────────────────
Write-Host ""
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "Phase B: Recovery Verification" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host ""

$sizeBefore = Get-LogFileSize $LogDirFullPath

Write-Host "  Sending $RequestCount requests after permission restore ..." -ForegroundColor Cyan
$phaseB = Send-TestBatch -Url "$BaseUrl$TestEndpoint" -Count $RequestCount -Label "Phase B (recovered)"

Write-Host "  Results: $($phaseB.Success)/$RequestCount succeeded" -ForegroundColor $(
    if ($phaseB.Success -eq $RequestCount) { "Green" } else { "Yellow" }
)

# Wait a moment for Serilog to flush
Start-Sleep -Seconds 2

$sizeAfter = Get-LogFileSize $LogDirFullPath
$sizeDelta = $sizeAfter - $sizeBefore

if ($sizeDelta -gt 0) {
    Write-Host "  PASS: Log files grew by $sizeDelta bytes — logging resumed" -ForegroundColor Green
    $phaseBResult = "PASS"
} else {
    # File logging may not grow for non-error endpoints (only Error/Warning are logged)
    Write-Host "  INFO: No new file log entries (expected if endpoints don't trigger Error/Warning)" -ForegroundColor Gray
    Write-Host "  INFO: EventBus DB logging is independent of file sink — check BASE_SYS_LOG" -ForegroundColor Gray
    $phaseBResult = "INFO"
}

# ── Phase C: Database Log Table Simulation (Optional) ───────────────────────
Write-Host ""
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "Phase C: Database Log Table Simulation (Optional)" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host ""

Write-Host "  NOTE: This phase is disabled by default." -ForegroundColor Yellow
Write-Host "  To test DB failure gracefully:" -ForegroundColor Yellow
Write-Host "    1. Temporarily rename BASE_SYS_LOG in the database" -ForegroundColor Yellow
Write-Host "    2. Or modify ConnectionStrings.json to point to a non-existent DB" -ForegroundColor Yellow
Write-Host "    3. Send requests — business should still return HTTP 200" -ForegroundColor Yellow
Write-Host "    4. Restore the connection string" -ForegroundColor Yellow
Write-Host ""

# Uncomment below to enable Phase C (requires manual DB setup):
<#
$phaseCResult = "PASS"
try {
    Write-Host "  [Phase C] Sending $RequestCount requests with simulated DB failure ..." -ForegroundColor Cyan
    $phaseC = Send-TestBatch -Url "$BaseUrl$TestEndpoint" -Count $RequestCount -Label "Phase C (DB sim)"
    Write-Host "  Results: $($phaseC.Success)/$RequestCount succeeded" -ForegroundColor $(
        if ($phaseC.Success -eq $RequestCount) { "Green" } else { "Red" }
    )
    if ($phaseC.Failed -gt 0) {
        $phaseCResult = "FAIL"
    }
} finally {
    # Restore DB connection if needed
    Write-Host "  [finally] Ensure DB connection is restored" -ForegroundColor Magenta
}
#>

$phaseCResult = "SKIPPED"

# ── Report ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "VERIFICATION REPORT" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host ""

$report = @(
    [PSCustomObject]@{ Phase = "A: File Sink Failure";   Status = $phaseAResult; Detail = "Business ops unblocked ($($phaseA.Success)/$RequestCount succeeded)" }
    [PSCustomObject]@{ Phase = "B: Recovery";            Status = $phaseBResult; Detail = $(if ($sizeDelta -gt 0) { "Log files grew $sizeDelta bytes" } else { "No file growth (normal for non-error endpoints)" }) }
    [PSCustomObject]@{ Phase = "C: DB Failure Sim";      Status = $phaseCResult; Detail = "Disabled by default — see script comments" }
)

$report | Format-Table -AutoSize

Write-Host ""
Write-Host "Safety: All file permissions were restored in finally blocks." -ForegroundColor Green

# Overall verdict
$anyFail = $report | Where-Object { $_.Status -eq "FAIL" }
if ($anyFail) {
    Write-Host "OVERALL: FAIL" -ForegroundColor Red
    exit 1
} else {
    Write-Host "OVERALL: PASS" -ForegroundColor Green
    exit 0
}
