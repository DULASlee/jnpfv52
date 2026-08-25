# NG-1B 证据 G2：ZXAFINIT.sql 中优先集合表的 INSERT 位置扫描
$ErrorActionPreference = 'Stop'
$dir = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1b-provenance'
$targets = @('ext_order','ext_order_entry','ext_customer','ext_product','wform_leaveapply','wform_salesorder','wm_billdetail','wh_basicdata')

$fs = [System.IO.File]::OpenRead('D:\JNPF-v52\DB\ZXAFINIT.sql')
$total = $fs.Length
Write-Host "file bytes: $total"
$blockSize = 8MB
$overlap = 512
$carry = ''
$pos = 0
$found = @{}
$insertCount = 0
while ($pos -lt $total) {
    $read = [Math]::Min($blockSize, $total - $pos)
    $buf = New-Object byte[] $read
    $n = $fs.Read($buf, 0, $read)
    $chunk = $carry + [System.Text.Encoding]::Unicode.GetString($buf, 0, $n)
    $start = $pos - $carry.Length * 2
    # 统计 INSERT 总数 + 定位目标表 INSERT
    foreach ($m in [regex]::Matches($chunk, '(?i)INSERT\s+(?:INTO\s+)?(?:\[dbo\]\.|dbo\.)?\[?([A-Za-z_][A-Za-z0-9_]*)\]?')) {
        $insertCount++
        $t = $m.Groups[1].Value.ToLower()
        if ($targets -contains $t -and -not $found.ContainsKey($t)) {
            $off = $start + $m.Index * 2
            $found[$t] = $off
        }
    }
    # carry 保留末尾 overlap 字符（UTF-16）
    if ($chunk.Length -gt $overlap) {
        $carry = $chunk.Substring($chunk.Length - $overlap)
        $pos += ($chunk.Length - $carry.Length) * 2
    } else {
        $carry = ''
        $pos = $total
    }
}
$fs.Close()
Write-Host "total INSERT statements: $insertCount"
foreach ($t in $targets) {
    if ($found.ContainsKey($t)) { Write-Host ("  INSERT {0,-24} byte@{1}" -f $t, $found[$t]) }
    else { Write-Host ("  INSERT {0,-24} NOT FOUND" -f $t) }
}
