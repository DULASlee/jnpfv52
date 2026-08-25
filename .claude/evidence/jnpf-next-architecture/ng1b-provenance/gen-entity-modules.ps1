# NG-1B 证据 E1：SugarTable 实体 → 模块归属映射（Code Owner 依据）
$ErrorActionPreference = 'Stop'
$dir = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1b-provenance'
$roots = @(
    'D:\JNPF-v52\backend\modularity',
    'D:\JNPF-v52\backend\application\JNPF.API.Entry'
)
$rx = [regex]'\[SugarTable\s*\(\s*"([^"]+)"\s*\)\]'
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("table_name`tmodule`tfile")
$rows = @{}
foreach ($root in $roots) {
    Get-ChildItem $root -Recurse -Filter *.cs -File | ForEach-Object {
        $f = $_
        $text = [System.IO.File]::ReadAllText($f.FullName)
        foreach ($m in $rx.Matches($text)) {
            $tbl = $m.Groups[1].Value.ToUpper()
            if (-not $rows.ContainsKey($tbl)) { $rows[$tbl] = $f.FullName }
        }
    }
}
foreach ($k in ($rows.Keys | Sort-Object)) {
    $rel = $rows[$k].Replace('D:\JNPF-v52\backend\', 'backend\')
    $mod = if ($rel -match '^backend\\modularity\\([^\\]+)') { $matches[1] } elseif ($rel -match '^backend\\application\\([^\\]+)') { $matches[1] } else { '?' }
    [void]$sb.AppendLine("$k`t$mod`t$rel")
}
[System.IO.File]::WriteAllText((Join-Path $dir '_entity-modules.tsv'), $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "ENTITY MODULES: $($rows.Count) tables"
$rows.Values | Group-Object { ($_ -replace '^D:\\JNPF-v52\\backend\\','') -replace '\\.*$','' } | Sort-Object Count -Descending | ForEach-Object { Write-Host ("  {0,-30} {1}" -f $_.Name, $_.Count) }
