# NG-1B 证据 D2：无 init 表清单（289 - 273 = 16 张，CodeFirst/运行时自建候选）+ CodeFirst 证据
$ErrorActionPreference = 'Stop'
$dir = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1b-provenance'
$raw = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1-batch1\db-matrix-raw.tsv'
$initFile = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1a-product-boundary\_init-sql-tables.txt'
$entityFile = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1a-product-boundary\_entity-tables.txt'
$refsFile = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1a-product-boundary\_no-entity-refs.tsv'

$init = @{}; Get-Content $initFile | ForEach-Object { $t = $_.Trim(); if ($t) { $init[$t.ToUpper()] = $true } }
$entity = @{}; Get-Content $entityFile | ForEach-Object { $t = $_.Trim(); if ($t) { $entity[$t.ToUpper()] = $true } }
$refs = @{}; Get-Content $refsFile | ForEach-Object { if ($_ -match '^(.+)\t(\d+)$') { $refs[$matches[1].ToUpper()] = [int]$matches[2] } }

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("table_name`tentity_mapped`tcode_refs")
$noInit = @()
foreach ($line in Get-Content $raw) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $tbl = ($line -split "`t")[0].Trim()
    $t = $tbl.ToUpper()
    if (-not $init.ContainsKey($t)) {
        $hasE = if ($entity.ContainsKey($t)) { 'Y' } else { 'N' }
        $rc = if ($refs.ContainsKey($t)) { $refs[$t] } else { '' }
        $noInit += $tbl
        [void]$sb.AppendLine("$tbl`t$hasE`t$rc")
    }
}
[System.IO.File]::WriteAllText((Join-Path $dir '_no-init-tables.tsv'), $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "NO-INIT TABLES: $($noInit.Count)"
$noInit | ForEach-Object { Write-Host "  $_" }
