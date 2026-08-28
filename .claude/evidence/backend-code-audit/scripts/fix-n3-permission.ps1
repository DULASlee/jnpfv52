<#
.SYNOPSIS
    Fix N3 API Permission Missing Issues
#>

param(
    [string]$Module,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$OutputRoot = Join-Path $PSScriptRoot ".."
$LogPath = Join-Path $OutputRoot "fix-n3-log.json"

$log = @{
    startTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    filesModified = 0
    methodsFixed = 0
    errors = @()
}

function Fix-File {
    param([string]$FilePath)
    
    $content = Get-Content $FilePath -Raw -ErrorAction SilentlyContinue
    if (-not $content) { return 0 }
    
    if ($content -notmatch 'IDynamicApiController') { return 0 }
    
    $lines = Get-Content $FilePath
    $fixedCount = 0
    $newLines = @()
    $i = 0
    
    while ($i -lt $lines.Count) {
        $line = $lines[$i]
        
        # Check if this is a public method
        if ($line -match '^\s*public\s+(?:async\s+)?(?:Task|void|dynamic|IActionResult|ActionResult)' -and 
            $line -match '\w+\s*\(' -and
            $line -notmatch '^\s*//') {
            
            # Check if previous line has attribute
            $hasAttribute = $false
            if ($newLines.Count -gt 0) {
                $lastLine = $newLines[-1]
                if ($lastLine -match '\[SecurityDefine\]|\[AllowAnonymous\]|\[') {
                    $hasAttribute = $true
                }
            }
            
            if (-not $hasAttribute) {
                # Get indentation
                $indent = ""
                if ($line -match '^(\s+)') {
                    $indent = $Matches[1]
                }
                
                $newLines += "${indent}[SecurityDefine]"
                $fixedCount++
            }
        }
        
        $newLines += $line
        $i++
    }
    
    if ($fixedCount -gt 0 -and -not $DryRun) {
        Set-Content -Path $FilePath -Value ($newLines -join "`n")
    }
    
    return $fixedCount
}

Write-Host "=== N3 API Permission Fix Tool ===" -ForegroundColor Green
Write-Host "Mode: $(if ($DryRun) { 'Dry Run' } else { 'Actual Fix' })" -ForegroundColor Yellow
Write-Host ""

$modularityRoot = "D:\JNPF-v52\backend\modularity"

if ($Module) {
    $modules = @($Module)
} else {
    $modules = Get-ChildItem -Path $modularityRoot -Directory | Select-Object -ExpandProperty Name
}

foreach ($mod in $modules) {
    Write-Host "Processing module: $mod" -ForegroundColor Cyan
    
    $modulePath = Join-Path $modularityRoot $mod
    $files = Get-ChildItem -Path $modulePath -Recurse -Filter "*Service.cs" | 
        Where-Object { $_.FullName -notmatch "\\obj\\" }
    
    foreach ($file in $files) {
        $fixed = Fix-File -FilePath $file.FullName
        if ($fixed -gt 0) {
            $log.filesModified++
            $log.methodsFixed += $fixed
            Write-Host "  $($file.Name): fixed $fixed methods" -ForegroundColor Yellow
        }
    }
}

$log.endTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$log | ConvertTo-Json -Depth 10 | Set-Content $LogPath

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Green
Write-Host "Files modified: $($log.filesModified)" -ForegroundColor White
Write-Host "Methods fixed: $($log.methodsFixed)" -ForegroundColor White
Write-Host "Log saved to: $LogPath" -ForegroundColor Gray