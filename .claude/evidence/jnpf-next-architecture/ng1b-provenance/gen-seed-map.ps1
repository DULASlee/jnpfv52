# NG-1B 证据 G3：ZXAFINIT.sql 全量 INSERT → 289 表 Seed 映射
$ErrorActionPreference = 'Stop'
$dir = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1b-provenance'

$tblSet = @{}
foreach ($line in Get-Content 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1-batch1\db-matrix-raw.tsv' -Encoding UTF8) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $t = ($line -split "`t")[0].Trim().ToUpper()
    if ($t -and $t -ne 'TABLE_NAME') { $tblSet[$t] = $true }
}

$fs = [System.IO.File]::OpenRead('D:\JNPF-v52\DB\ZXAFINIT.sql')
$total = $fs.Length
$blockSize = 8MB
$overlap = 512
$carry = ''
$pos = 0
$seed = @{}
$insertTotal = 0
while ($pos -lt $total) {
    $read = [Math]::Min($blockSize, $total - $pos)
    $buf = New-Object byte[] $read
    $n = $fs.Read($buf, 0, $read)
    $chunk = $carry + [System.Text.Encoding]::Unicode.GetString($buf, 0, $n)
    foreach ($m in [regex]::Matches($chunk, '(?i)INSERT\s+(?:INTO\s+)?(?:\[dbo\]\.|dbo\.)?\[?([A-Za-z_][A-Za-z0-9_]*)\]?')) {
        $insertTotal++
        $t = $m.Groups[1].Value.ToUpper()
        if ($tblSet.ContainsKey($t)) {
            if (-not $seed.ContainsKey($t)) { $seed[$t] = 0 }
            $seed[$t]++
        }
    }
    if ($chunk.Length -gt $overlap) {
        $carry = $chunk.Substring($chunk.Length - $overlap)
        $pos += ($chunk.Length - $carry.Length) * 2
    } else {
        $carry = ''
        $pos = $total
    }
}
$fs.Close()
Write-Host "total INSERT: $insertTotal ; seeded tables (of 289): $($seed.Count)"

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("table_name`tseed_inserts")
foreach ($tbl in ($tblSet.Keys | Sort-Object)) {
    $c = if ($seed.ContainsKey($tbl)) { $seed[$tbl] } else { 0 }
    [void]$sb.AppendLine("$tbl`t$c")
}
[System.IO.File]::WriteAllText((Join-Path $dir '_seed-map.tsv'), $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host '--- 优先集合 seed 统计 ---'
foreach ($pfx in @('EXT_','WFORM_','WM_','WH_','SA_')) {
    $grp = $tblSet.Keys | Where-Object { $_ -match "^$pfx" }
    $seeded = ($grp | Where-Object { $seed.ContainsKey($_) -and $seed[$_] -gt 0 }).Count
    Write-Host ("  {0,-8} total={1,-3} seeded={2}" -f $pfx, $grp.Count, $seeded)
}
