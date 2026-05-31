<#
.SYNOPSIS
    T11: Performance Baseline Measurement - P99 latency impact of logging pipeline.
.DESCRIPTION
    Measures HTTP response latency for a configurable number of sequential requests,
    calculates P50/P95/P99/avg/max, and reports the impact of the logging pipeline.

    Since logging is always-on in the current architecture, this script measures the
    "with logging" state. To establish a true baseline, disable Serilog sinks and
    RequestActionFilter temporarily, re-run, and compare.

    For more accurate results, use the companion k6 script (t11_k6_benchmark.js).

    References:
    - RequestActionFilter: backend/modularity/common/JNPF.Common.Core/Filter/RequestActionFilter.cs
    - TraceIdMiddleware: backend/application/JNPF.API.Entry/Infrastructure/TraceIdMiddleware.cs
.PARAMETER BaseUrl
    API base URL (default: http://localhost:5000)
.PARAMETER RequestCount
    Number of requests to send (default: 500)
.PARAMETER P99ThresholdMs
    Maximum acceptable P99 latency in milliseconds (default: 100)
.PARAMETER Endpoint
    Endpoint to benchmark (default: /api/system/TechnicalLog/errors)
.EXAMPLE
    .\t11_performance_test.ps1 -BaseUrl http://localhost:5000 -RequestCount 500
    .\t11_performance_test.ps1 -BaseUrl http://localhost:5000 -RequestCount 1000 -P99ThresholdMs 50
#>

param(
    [string]$BaseUrl       = "http://localhost:5000",
    [int]   $RequestCount  = 500,
    [int]   $P99ThresholdMs = 100,
    [string]$Endpoint      = "/api/system/TechnicalLog/errors"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Banner ──────────────────────────────────────────────────────────────────
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "T11: Performance Baseline Measurement" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host ""
Write-Host "Prerequisites:" -ForegroundColor Yellow
Write-Host "  [1] API service is running at $BaseUrl"
Write-Host "  [2] Service is warmed up (not first request after restart)"
Write-Host "  [3] No other heavy load on the server"
Write-Host ""
Write-Host "Parameters:" -ForegroundColor Yellow
Write-Host "  RequestCount     = $RequestCount"
Write-Host "  P99 Threshold    = ${P99ThresholdMs}ms"
Write-Host "  Endpoint         = $Endpoint"
Write-Host ""

$confirm = Read-Host "All prerequisites met? (y/n)"
if ($confirm -ne 'y') { Write-Host "Aborted." -ForegroundColor Red; exit 0 }

# ── Warmup ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[Warmup] Sending 10 warmup requests ..." -ForegroundColor Cyan
$warmupUrl = "$BaseUrl$Endpoint"
for ($i = 0; $i -lt 10; $i++) {
    try {
        Invoke-WebRequest -Uri $warmupUrl -Method GET -UseBasicParsing -TimeoutSec 30 -ErrorAction SilentlyContinue | Out-Null
    } catch {}
}
Write-Host "  Warmup complete" -ForegroundColor Gray

# ── Measurement ─────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[Measurement] Sending $RequestCount sequential requests ..." -ForegroundColor Cyan

$latencies  = @()
$errors     = 0
$statusCodes = @{}
$totalSw    = [System.Diagnostics.Stopwatch]::StartNew()

for ($i = 0; $i -lt $RequestCount; $i++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $resp = Invoke-WebRequest -Uri $warmupUrl -Method GET -UseBasicParsing -TimeoutSec 30 -ErrorAction SilentlyContinue
        $sw.Stop()
        $latencies += $sw.ElapsedMilliseconds

        $code = [int]$resp.StatusCode
        if ($statusCodes.ContainsKey($code)) {
            $statusCodes[$code]++
        } else {
            $statusCodes[$code] = 1
        }
    }
    catch {
        $sw.Stop()
        $latencies += $sw.ElapsedMilliseconds
        $errors++
    }

    # Progress indicator every 100 requests
    if (($i + 1) % 100 -eq 0) {
        Write-Host "  $($i + 1) / $RequestCount ..." -ForegroundColor Gray
    }
}
$totalSw.Stop()

Write-Host "  Completed in $($totalSw.ElapsedMilliseconds) ms" -ForegroundColor Gray

# ── Calculate Statistics ────────────────────────────────────────────────────
Write-Host ""
Write-Host "[Analysis] Calculating latency distribution ..." -ForegroundColor Cyan

$sorted = $latencies | Sort-Object
$count  = $sorted.Count

function Get-Percentile {
    param([array]$SortedData, [double]$Percentile)
    $index = [math]::Floor($SortedData.Count * $Percentile / 100)
    $index = [math]::Min($index, $SortedData.Count - 1)
    return $SortedData[$index]
}

$p50  = Get-Percentile $sorted 50
$p75  = Get-Percentile $sorted 75
$p90  = Get-Percentile $sorted 90
$p95  = Get-Percentile $sorted 95
$p99  = Get-Percentile $sorted 99
$p999 = Get-Percentile $sorted 99.9
$avg  = [math]::Round(($sorted | Measure-Object -Average).Average, 2)
$min  = $sorted[0]
$max  = $sorted[-1]
$stddev = [math]::Round([math]::Sqrt(($sorted | ForEach-Object { [math]::Pow(($_ - $avg), 2) } | Measure-Object -Average).Average), 2)

# ── Report ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "VERIFICATION REPORT — Latency Distribution" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host ""

$distTable = @(
    [PSCustomObject]@{ Metric = "Min";      "Latency (ms)" = $min }
    [PSCustomObject]@{ Metric = "P50";      "Latency (ms)" = $p50 }
    [PSCustomObject]@{ Metric = "P75";      "Latency (ms)" = $p75 }
    [PSCustomObject]@{ Metric = "P90";      "Latency (ms)" = $p90 }
    [PSCustomObject]@{ Metric = "P95";      "Latency (ms)" = $p95 }
    [PSCustomObject]@{ Metric = "P99";      "Latency (ms)" = $p99 }
    [PSCustomObject]@{ Metric = "P99.9";    "Latency (ms)" = $p999 }
    [PSCustomObject]@{ Metric = "Max";      "Latency (ms)" = $max }
    [PSCustomObject]@{ Metric = "Avg";      "Latency (ms)" = $avg }
    [PSCustomObject]@{ Metric = "StdDev";   "Latency (ms)" = $stddev }
)

$distTable | Format-Table -AutoSize

Write-Host "Request Summary:" -ForegroundColor Cyan
Write-Host "  Total Requests:  $RequestCount"
Write-Host "  Successful:      $($RequestCount - $errors)"
Write-Host "  Errors:          $errors"
Write-Host "  Total Duration:  $($totalSw.ElapsedMilliseconds) ms"
Write-Host "  Throughput:      $([math]::Round($RequestCount / ($totalSw.ElapsedMilliseconds / 1000), 1)) req/s"
Write-Host ""

Write-Host "Status Code Distribution:" -ForegroundColor Cyan
foreach ($kv in $statusCodes.GetEnumerator() | Sort-Object Key) {
    Write-Host "  HTTP $($kv.Key): $($kv.Value) requests"
}
Write-Host ""

# ── Percentile Bar Chart ────────────────────────────────────────────────────
Write-Host "Latency Distribution (ASCII):" -ForegroundColor Cyan
$maxBar = 50
$scale  = if ($max -gt 0) { $maxBar / $max } else { 1 }

$bars = @(
    @{ Label = "P50";   Value = $p50 }
    @{ Label = "P75";   Value = $p75 }
    @{ Label = "P90";   Value = $p90 }
    @{ Label = "P95";   Value = $p95 }
    @{ Label = "P99";   Value = $p99 }
    @{ Label = "P99.9"; Value = $p999 }
    @{ Label = "Max";   Value = $max }
)

foreach ($bar in $bars) {
    $len = [math]::Max(1, [math]::Round($bar.Value * $scale))
    $barStr = "#" * $len
    Write-Host ("  {0,-6} [{1,-$maxBar}] {2} ms" -f $bar.Label, $barStr, $bar.Value)
}
Write-Host ""

# ── P99 Threshold Check ────────────────────────────────────────────────────
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "THRESHOLD CHECK" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host ""

$p99Check = if ($p99 -le $P99ThresholdMs) { "PASS" } else { "FAIL" }
$p99Color = if ($p99Check -eq "PASS") { "Green" } else { "Red" }

Write-Host "  P99 Latency:    ${p99}ms" -ForegroundColor $p99Color
Write-Host "  P99 Threshold:  ${P99ThresholdMs}ms" -ForegroundColor $p99Color
Write-Host "  Result:         $p99Check" -ForegroundColor $p99Color
Write-Host ""

# ── Comparison Note ─────────────────────────────────────────────────────────
Write-Host "NOTE: This measures the current state (logging enabled)." -ForegroundColor Yellow
Write-Host "To compare with logging disabled:" -ForegroundColor Yellow
Write-Host "  1. Stop the API" -ForegroundColor Yellow
Write-Host "  2. In SerilogBootstrap.cs, comment out .WriteTo.File(...) calls" -ForegroundColor Yellow
Write-Host "  3. Rebuild and restart" -ForegroundColor Yellow
Write-Host "  4. Re-run this script with the same parameters" -ForegroundColor Yellow
Write-Host "  5. Compare P99 delta" -ForegroundColor Yellow
Write-Host ""
Write-Host "For more accurate load testing, use k6:" -ForegroundColor Yellow
Write-Host "  k6 run t11_k6_benchmark.js" -ForegroundColor Yellow
Write-Host ""

# Overall verdict
if ($p99Check -eq "PASS") {
    Write-Host "OVERALL: PASS" -ForegroundColor Green
    exit 0
} else {
    Write-Host "OVERALL: FAIL — P99 exceeds threshold" -ForegroundColor Red
    exit 1
}
