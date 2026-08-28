<#
.SYNOPSIS
    Fix N3 API Permission Missing - V3 (string-based, no Array.Insert)
#>
param(
    [string]$Module,
    [switch]$DryRun
)

$ErrorActionPreference = "Continue"
$modularityRoot = "D:\JNPF-v52\backend\modularity"

function Fix-File {
    param([string]$Path)
    
    $raw = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    if ($raw -notmatch 'IDynamicApiController') { return 0 }
    
    $lines = $raw -split "`r?`n"
    $newLines = [System.Collections.Generic.List[string]]::new($lines.Count)
    $fixed = 0
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $newLines.Add($line)
        
        $trimmed = $line.TrimStart()
        $isPublic = $trimmed -match '^public\s+'
        $isMethod = $isPublic -and $trimmed -match '\(' -and $trimmed -notmatch '^\s*public\s+(class|interface|struct|enum|delegate|static\s+class|abstract\s+class|sealed\s+class|partial\s+class)'
        $notComment = -not ($trimmed.StartsWith('//') -or $trimmed.StartsWith('/*') -or $trimmed.StartsWith('*'))
        
        if ($isMethod -and $notComment) {
            $hasPerm = $false
            for ($j = $newLines.Count - 2; $j -ge [Math]::Max(0, $newLines.Count - 8); $j--) {
                $p = $newLines[$j].Trim()
                if ($p -match '^\[(SecurityDefine|AllowAnonymous|HttpGet|HttpPost|HttpPut|HttpDelete|Route|LogPolicy|ApiDescription|MapToApiVersion)' -or $p -eq '') {
                    if ($p -match '^\[(SecurityDefine|AllowAnonymous)') { $hasPerm = $true }
                } else { break }
            }
            
            if (-not $hasPerm) {
                $indent = ""
                if ($line -match '^(\s+)') { $indent = $Matches[1] }
                $newLines.Insert($newLines.Count - 1, "${indent}[SecurityDefine]")
                $fixed++
            }
        }
    }
    
    if ($fixed -gt 0 -and -not $DryRun) {
        $result = $newLines -join "`r`n"
        [System.IO.File]::WriteAllText($Path, $result, [System.Text.Encoding]::UTF8)
    }
    return $fixed
}

Write-Host "=== N3 Fix V3 ===" -ForegroundColor Green
$files = if ($Module) {
    Get-ChildItem -Path (Join-Path $modularityRoot $Module) -Recurse -Filter "*.cs" |
        Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\Interfaces\\" -and $_.FullName -notmatch "\\Entitys\\" }
} else {
    Get-ChildItem -Path $modularityRoot -Recurse -Filter "*.cs" |
        Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\Interfaces\\" -and $_.FullName -notmatch "\\Entitys\\" }
}

$totalFixed = 0
$filesModified = 0
foreach ($f in $files) {
    $n = Fix-File -Path $f.FullName
    if ($n -gt 0) {
        $filesModified++
        $totalFixed += $n
        Write-Host "  $($f.Directory.Parent.Name)/$($f.Name): $n" -ForegroundColor Yellow
    }
}

Write-Host "`nFiles: $($files.Count) scanned, $filesModified modified" -ForegroundColor Cyan
Write-Host "Methods fixed: $totalFixed" -ForegroundColor Green