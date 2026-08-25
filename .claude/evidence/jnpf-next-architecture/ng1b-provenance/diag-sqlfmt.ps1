$ErrorActionPreference = 'Stop'
$fs = [System.IO.File]::OpenRead('D:\JNPF-v52\DB\ZXAFINIT.sql')
$buf = New-Object byte[] 400000
$n = $fs.Read($buf, 0, 400000)
$fs.Close()
$s = [System.Text.Encoding]::Unicode.GetString($buf, 0, $n)
$i = $s.IndexOf('CREATE TABLE')
Write-Host "first 'CREATE TABLE' idx=$i"
if ($i -ge 0) { Write-Host ("CTX: [" + $s.Substring($i, 120) + "]") }
$regex = [regex]'(?i)CREATE\s+TABLE\s+(?:[A-Za-z0-9_$#]+\.)?[\["]?([A-Za-z_$#][A-Za-z0-9_$#]*)[\]"]?'
$ms = $regex.Matches($s)
Write-Host "regex matches in first 400KB: $($ms.Count)"
foreach ($m in $ms | Select-Object -First 5) { Write-Host ("MATCH: g1=[{0}] full=[{1}]" -f $m.Groups[1].Value, $m.Value.Substring(0, [Math]::Min(60, $m.Value.Length))) }
# 统计字面 CREATE TABLE 出现次数
$c2 = ([regex]::Matches($s, '(?i)CREATE\s+TABLE')).Count
Write-Host "literal CREATE TABLE count (first 400KB): $c2"
