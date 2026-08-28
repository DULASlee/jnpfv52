<#
.SYNOPSIS
    Analyze remaining P0 findings after J6/N1 suppression
#>
param()

$ErrorActionPreference = "Continue"
$findingsPath = Join-Path $PSScriptRoot "..\all-findings.json"
$outputPath = Join-Path $PSScriptRoot "..\p0-remaining-analysis.json"

$allFindings = Get-Content $findingsPath -Raw | ConvertFrom-Json

# Filter P0 only
$p0Findings = $allFindings | Where-Object { $_.Severity -eq "P0" }

# Exclude J6/N1 (now Informational)
$p0Remaining = $p0Findings | Where-Object { $_.RuleId -ne "J6" -and $_.RuleId -ne "N1" }

# Group by RuleId
$byRule = $p0Remaining | Group-Object RuleId | Sort-Object Count -Descending

# Group by Module
$byModule = $p0Remaining | Group-Object Module | Sort-Object Count -Descending

# Group by Dimension
$byDimension = $p0Remaining | Group-Object Dimension | Sort-Object Count -Descending

# Output
$analysis = @{
    totalP0Original = $p0Findings.Count
    j6n1Suppressed = ($p0Findings | Where-Object { $_.RuleId -eq "J6" -or $_.RuleId -eq "N1" }).Count
    p0Remaining = $p0Remaining.Count
    byRule = $byRule | ForEach-Object { @{ RuleId = $_.Name; Count = $_.Count; Description = ($_.Group | Select-Object -First 1).Description } }
    byModule = $byModule | ForEach-Object { @{ Module = $_.Name; Count = $_.Count } }
    byDimension = $byDimension | ForEach-Object { @{ Dimension = $_.Name; Count = $_.Count } }
}

$analysis | ConvertTo-Json -Depth 10 | Set-Content $outputPath

Write-Host "=== P0 Remaining Analysis ===" -ForegroundColor Green
Write-Host "Original P0: $($analysis.totalP0Original)" -ForegroundColor Yellow
Write-Host "J6/N1 Suppressed: $($analysis.j6n1Suppressed)" -ForegroundColor Cyan
Write-Host "Remaining P0: $($analysis.p0Remaining)" -ForegroundColor Red
Write-Host ""
Write-Host "By Rule:" -ForegroundColor White
$byRule | ForEach-Object {
    Write-Host "  $($_.Name): $($_.Count) - $(($_.Group | Select-Object -First 1).Description)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "By Module:" -ForegroundColor White
$byModule | ForEach-Object {
    Write-Host "  $($_.Name): $($_.Count)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "By Dimension:" -ForegroundColor White
$byDimension | ForEach-Object {
    Write-Host "  $($_.Name): $($_.Count)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "Saved to: $outputPath" -ForegroundColor Green