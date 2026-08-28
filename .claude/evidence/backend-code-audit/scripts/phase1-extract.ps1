<#
.SYNOPSIS
    Extract Critical Security Findings for Phase 1
#>
param()

$ErrorActionPreference = "Continue"
$findingsPath = Join-Path $PSScriptRoot "..\all-findings.json"
$outputPath = Join-Path $PSScriptRoot "..\phase1-critical-security.json"

$allFindings = Get-Content $findingsPath -Raw | ConvertFrom-Json

# Extract Critical Security Findings
$criticalRules = @("J1", "J2", "J5", "N2")
$criticalFindings = $allFindings | Where-Object { $_.RuleId -in $criticalRules }

# Group by RuleId
$byRule = $criticalFindings | Group-Object RuleId

Write-Host "=== Phase 1 Critical Security Findings ===" -ForegroundColor Green
Write-Host ""

foreach ($rule in $byRule) {
    Write-Host "$($rule.Name) ($($rule.Count) instances):" -ForegroundColor Yellow
    $rule.Group | Select-Object -First 3 | ForEach-Object {
        Write-Host "  File: $($_.File)" -ForegroundColor Gray
        Write-Host "  Line: $($_.Line)" -ForegroundColor Gray
        Write-Host "  Code: $($_.Code.Substring(0, [Math]::Min(80, $_.Code.Length)))" -ForegroundColor Gray
        Write-Host ""
    }
    if ($rule.Count -gt 3) {
        Write-Host "  ... and $($rule.Count - 3) more" -ForegroundColor Gray
    }
}

# Save to JSON
$criticalFindings | ConvertTo-Json -Depth 10 | Set-Content $outputPath

Write-Host ""
Write-Host "Total Critical Findings: $($criticalFindings.Count)" -ForegroundColor Cyan
Write-Host "Saved to: $outputPath" -ForegroundColor Green