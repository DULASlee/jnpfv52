# NG-1B 证据 D1：ZXAFINIT.sql CREATE TABLE 精确字节位置（块扫描；UTF-16LE 解码 + 边界重叠防切分）
$ErrorActionPreference = 'Stop'
$dir = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1b-provenance'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$sqlFile = 'D:\JNPF-v52\DB\ZXAFINIT.sql'
$outFile = Join-Path $dir '_create-positions.tsv'
$fs = [System.IO.File]::OpenRead($sqlFile)
$sb = New-Object System.Text.StringBuilder
$blockSize = 8MB
$overlap = 256   # 字符（UTF-16 下 = 512 字节）
$buffer = New-Object byte[] $blockSize
$pos = [long]0
$carry = ''
$count = 0
$seen = @{}
# CREATE TABLE [schema.]name ｜ [name] ｜ "name"（不区分大小写；schema 允许 [dbo]. 或 dbo.）
$regex = [regex]'(?i)CREATE\s+TABLE\s+(?:\[[A-Za-z0-9_$#]+\]\.|[A-Za-z0-9_$#]+\.)?[\["]?([A-Za-z_$#][A-Za-z0-9_$#]*)[\]"]?'
while ($true) {
    $n = $fs.Read($buffer, 0, $blockSize)
    if ($n -le 0) { break }
    # UTF-16LE：每字符 2 字节；$n 为偶数（8MB 块），安全
    $chunk = $carry + [System.Text.Encoding]::Unicode.GetString($buffer, 0, $n)
    $start = $pos - $carry.Length * 2   # chunk 起始字节偏移
    foreach ($m in $regex.Matches($chunk)) {
        $tbl = $m.Groups[1].Value.ToUpper()
        $off = $start + $m.Index * 2    # 字符索引 → 字节偏移
        if (-not $seen.ContainsKey($tbl)) { $seen[$tbl] = $off; $count++ }
    }
    if ($chunk.Length -gt $overlap) { $carry = $chunk.Substring($chunk.Length - $overlap) } else { $carry = $chunk }
    $pos += $n
}
$fs.Close()
$lines = $seen.Keys | Sort-Object | ForEach-Object { "$_`t$($seen[$_])" }
[System.IO.File]::WriteAllText($outFile, ($lines -join "`r`n") + "`r`n", [System.Text.Encoding]::ASCII)
Write-Host "CREATE TABLE positions: $count"
