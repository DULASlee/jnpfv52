# NG-1B 证据 F1：IDynamicApiController 服务清单（API 暴露入口）
$ErrorActionPreference = 'Stop'
$dir = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1b-provenance'
$backend = 'D:\JNPF-v52\backend'

$rx = [regex]'(?:public\s+(?:sealed\s+|partial\s+)*class\s+(\w+)[\s\S]{0,400}?IDynamicApiController|class\s+(\w+)[^\{]{0,200}IDynamicApiController)'
$files = Get-ChildItem $backend -Recurse -Filter *.cs -File | Where-Object { $_.FullName -notmatch '\\tests?\\|\\bin\\|\\obj\\' }
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("service_class`tmodule`tfile")
$count = 0
foreach ($f in $files) {
    $text = $null
    try { $text = [System.IO.File]::ReadAllText($f.FullName) } catch { continue }
    if ($text -notmatch 'IDynamicApiController') { continue }
    foreach ($m in $rx.Matches($text)) {
        $cls = if ($m.Groups[1].Value) { $m.Groups[1].Value } else { $m.Groups[2].Value }
        $rel = $f.FullName.Replace('D:\JNPF-v52\backend\', 'backend\')
        $mod = if ($rel -match '^backend\\modularity\\([^\\]+)') { $matches[1] } elseif ($rel -match '^backend\\application\\([^\\]+)') { $matches[1] } else { 'other' }
        [void]$sb.AppendLine("$cls`t$mod`t$rel")
        $count++
    }
}
[System.IO.File]::WriteAllText((Join-Path $dir '_api-services.tsv'), $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "API SERVICES: $count classes"
Import-Csv (Join-Path $dir '_api-services.tsv') -Delimiter "`t" | Group-Object module | Sort-Object Count -Descending | ForEach-Object { Write-Host ("  {0,-20} {1}" -f $_.Name, $_.Count) }
