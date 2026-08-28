<#
.SYNOPSIS
    J6/N1 Finding Inventory - Dedup + Classification
#>
param()

$ErrorActionPreference = "Continue"
$findingsPath = Join-Path $PSScriptRoot "..\all-findings.json"
$outputPath = Join-Path $PSScriptRoot "..\j6n1-inventory.json"

$allFindings = Get-Content $findingsPath -Raw | ConvertFrom-Json
$j6n1Findings = $allFindings | Where-Object { $_.RuleId -eq "J6" -or $_.RuleId -eq "N1" }

$uniqueFindings = @{}
foreach ($f in $j6n1Findings) {
    $key = "$($f.File):$($f.Line)"
    if (-not $uniqueFindings.ContainsKey($key)) {
        $uniqueFindings[$key] = @{
            FindingId = "J6N1-$($uniqueFindings.Count + 1)"
            Module = $f.Module
            File = $f.File
            Line = $f.Line
            Code = $f.Code
            HasJ6 = $false
            HasN1 = $false
            Entity = ""
            Operation = "Query"
            Classification = "G-Undetermined"
        }
    }
    if ($f.RuleId -eq "J6") { $uniqueFindings[$key].HasJ6 = $true }
    if ($f.RuleId -eq "N1") { $uniqueFindings[$key].HasN1 = $true }
}

foreach ($key in $uniqueFindings.Keys) {
    $f = $uniqueFindings[$key]
    if ($f.Code -match 'Queryable<(\w+)') {
        $f.Entity = $Matches[1]
    }
}

$inventory = @()
foreach ($key in $uniqueFindings.Keys) {
    $inventory += [PSCustomObject]$uniqueFindings[$key]
}
$inventory | ConvertTo-Json -Depth 10 | Set-Content $outputPath

Write-Host "=== J6/N1 Finding Inventory ===" -ForegroundColor Green
Write-Host "J6 count: $(($j6n1Findings | Where-Object { $_.RuleId -eq 'J6' }).Count)" -ForegroundColor Yellow
Write-Host "N1 count: $(($j6n1Findings | Where-Object { $_.RuleId -eq 'N1' }).Count)" -ForegroundColor Yellow
Write-Host "Unique: $($inventory.Count)" -ForegroundColor Cyan
Write-Host ""
Write-Host "By Module:" -ForegroundColor White
$inventory | Group-Object Module | Sort-Object Count -Descending | ForEach-Object {
    Write-Host "  $($_.Name): $($_.Count)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "Top Entities:" -ForegroundColor White
$inventory | Where-Object { $_.Entity -ne "" } | Group-Object Entity | Sort-Object Count -Descending | Select-Object -First 15 | ForEach-Object {
    Write-Host "  $($_.Name): $($_.Count)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "Saved: $outputPath" -ForegroundColor Green