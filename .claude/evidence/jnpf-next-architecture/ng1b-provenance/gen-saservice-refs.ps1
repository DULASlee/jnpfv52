# NG-1B 证据 E3：sa-service（Node 项目）对 289 表的引用扫描
$ErrorActionPreference = 'Stop'
$dir = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1b-provenance'
$root = 'D:\JNPF-v52\sa-service'

$tblSet = @{}
foreach ($line in Get-Content 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1-batch1\db-matrix-raw.tsv' -Encoding UTF8) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $t = ($line -split "`t")[0].Trim().ToUpper()
    if ($t -and $t -ne 'TABLE_NAME') { $tblSet[$t] = $true }
}

# 只扫源码与配置（排除 node_modules/.map/.min）
$files = Get-ChildItem $root -Recurse -File -Include *.ts,*.js,*.mjs,*.cjs |
    Where-Object { $_.FullName -notmatch '\\node_modules\\' -and $_.Extension -ne '.map' }
Write-Host "sa-service files: $($files.Count)"

$hits = @{}          # tbl -> file count
$fileTbl = @{}       # tbl -> hashset of relative file paths (cap 3)
foreach ($f in $files) {
    $text = $null
    try { $text = [System.IO.File]::ReadAllText($f.FullName).ToUpper() } catch { continue }
    foreach ($tbl in $tblSet.Keys) {
        if ($text.Contains($tbl)) {
            if (-not $hits.ContainsKey($tbl)) { $hits[$tbl] = 0; $fileTbl[$tbl] = @() }
            $hits[$tbl]++
            if ($fileTbl[$tbl].Count -lt 3) { $fileTbl[$tbl] += $f.FullName.Replace('D:\JNPF-v52\sa-service\', 'sa-service\') }
        }
    }
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("table_name`tsaservice_ref_files`tsaservice_sample")
foreach ($tbl in ($tblSet.Keys | Sort-Object)) {
    if ($hits.ContainsKey($tbl)) {
        $s = ($fileTbl[$tbl] -join ';')
        [void]$sb.AppendLine("$tbl`t$($hits[$tbl])`t$s")
    } else {
        [void]$sb.AppendLine("$tbl`t0`t")
    }
}
[System.IO.File]::WriteAllText((Join-Path $dir '_saservice-refs.tsv'), $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "sa-service referenced tables: $(($hits.Keys | Where-Object { $tblSet.ContainsKey($_) }).Count)"
Write-Host '--- sa_* 13 张 ---'
foreach ($tbl in ($tblSet.Keys | Where-Object { $_ -match '^SA_' } | Sort-Object)) {
    $c = if ($hits.ContainsKey($tbl)) { $hits[$tbl] } else { 0 }
    Write-Host ("  {0,-22} {1}" -f $tbl, $c)
}
