# NG-1B 证据 E2：289 表 Write Owner / Read Consumers 全量扫描
# 方法：backend 全 .cs 扫描 SugarTable 实体泛型访问 + 字符串 SQL 表名访问 → per-table 读写模块集
$ErrorActionPreference = 'Stop'
$dir = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1b-provenance'
$backend = 'D:\JNPF-v52\backend'

# ---- 表名全集（289）----
$tblSet = @{}
foreach ($line in Get-Content 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1-batch1\db-matrix-raw.tsv' -Encoding UTF8) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $t = ($line -split "`t")[0].Trim().ToUpper()
    if ($t -and $t -ne 'TABLE_NAME') { $tblSet[$t] = $true }
}
Write-Host "table set: $($tblSet.Count)"

# ---- Pass 1: SugarTable → class 名（双向：tbl→cls 与 cls→tbl）----
$classOfTbl = @{}; $tblOfClass = @{}
$rxAttr = [regex]'\[SugarTable\s*\([^\]]*?"([^"]+)"[^\]]*?\)\][\s\S]{0,200}?(?:public\s+(?:sealed\s+|partial\s+)*class|class)\s+(\w+)'
$rxAttr2 = [regex]'\[SugarTable\s*\([^\]]*?Name\s*=\s*"([^"]+)"[^\]]*?\)\][\s\S]{0,200}?class\s+(\w+)'

# ---- Pass 2/3: 访问扫描 ----
$rxGeneric = [regex]'\b(Queryable|Insertable|Updateable|Deleteable|Storageable|GetList|GetById|GetSingle|FirstOrDefault|GetFirst|IsAny|Count|QueryableWithAttr|QueryableAsync)\s*<\s*(\w+)\s*>'
$rxRepo = [regex]'\b(?:ISqlSugarRepository|SqlSugarRepository|ISugarRepository|ISqlSugarClient|SqlSugarScope|ISugarQueryable)\s*<\s*(\w+)\s*>'
$rxSql = [regex]'(?i)\b(FROM|INTO|UPDATE|JOIN)\s+(?:\[dbo\]\.|dbo\.)?\[?([A-Za-z_][A-Za-z0-9_]*)\]?'
$readTbl = @{}; $writeTbl = @{}; $accTbl = @{}   # key=tbl -> hashset of module

$files = Get-ChildItem $backend -Recurse -Filter *.cs -File
Write-Host "cs files: $($files.Count)"

# ---- Loop 1: 建 SugarTable → class 双向映射（必须先于访问分析）----
$n = 0
foreach ($f in $files) {
    $n++
    if ($n % 1000 -eq 0) { Write-Host "  pass1 scanned $n files..." }
    $text = $null
    try { $text = [System.IO.File]::ReadAllText($f.FullName) } catch { continue }
    foreach ($m in $rxAttr.Matches($text)) {
        $tbl = $m.Groups[1].Value.ToUpper(); $cls = $m.Groups[2].Value
        if (-not $classOfTbl.ContainsKey($tbl)) { $classOfTbl[$tbl] = $cls }
        if (-not $tblOfClass.ContainsKey($cls.ToUpper())) { $tblOfClass[$cls.ToUpper()] = $tbl }
    }
    foreach ($m in $rxAttr2.Matches($text)) {
        $tbl = $m.Groups[1].Value.ToUpper(); $cls = $m.Groups[2].Value
        if (-not $classOfTbl.ContainsKey($tbl)) { $classOfTbl[$tbl] = $cls }
        if (-not $tblOfClass.ContainsKey($cls.ToUpper())) { $tblOfClass[$cls.ToUpper()] = $tbl }
    }
}
Write-Host "classOfTbl: $($classOfTbl.Count)"

# ---- Loop 2: 访问分析 ----
$n = 0
foreach ($f in $files) {
    $n++
    if ($n % 500 -eq 0) { Write-Host "  pass2 scanned $n files..." }
    $text = $null
    $rel = $f.FullName.Replace('D:\JNPF-v52\backend\', 'backend\')
    $mod = if ($rel -match '^backend\\modularity\\([^\\]+)') { $matches[1] } elseif ($rel -match '^backend\\application\\([^\\]+)') { $matches[1] } else { 'other' }
    try { $text = [System.IO.File]::ReadAllText($f.FullName) } catch { continue }

    # Pass 2a: 仓储声明 ISqlSugarRepository<T> 等 → access（读写未分）
    foreach ($m in $rxRepo.Matches($text)) {
        $cls = $m.Groups[1].Value
        $tbl = $tblOfClass[$cls.ToUpper()]
        if (-not $tbl -or -not $tblSet.ContainsKey($tbl)) { continue }
        if (-not $accTbl.ContainsKey($tbl)) { $accTbl[$tbl] = @{} }
        $accTbl[$tbl][$mod] = $true
    }

    # Pass 2b: 泛型访问（write: Insert/Update/Delete/Storage）
    foreach ($m in $rxGeneric.Matches($text)) {
        $kind = $m.Groups[1].Value; $cls = $m.Groups[2].Value
        $tbl = $tblOfClass[$cls.ToUpper()]
        if (-not $tbl -or -not $tblSet.ContainsKey($tbl)) { continue }
        $map = if ($kind -in @('Insertable','Updateable','Deleteable','Storageable')) { $writeTbl } else { $readTbl }
        if (-not $map.ContainsKey($tbl)) { $map[$tbl] = @{} }
        $map[$tbl][$mod] = $true
    }

    # Pass 3: 字符串 SQL 表名（FROM/INTO/UPDATE/JOIN）
    foreach ($m in $rxSql.Matches($text)) {
        $tbl = $m.Groups[2].Value.ToUpper()
        if (-not $tblSet.ContainsKey($tbl)) { continue }
        $verb = $m.Groups[1].Value.ToUpper()
        $map = if ($verb -in @('INTO','UPDATE')) { $writeTbl } else { $readTbl }
        if (-not $map.ContainsKey($tbl)) { $map[$tbl] = @{} }
        $map[$tbl][$mod] = $true
    }
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("table_name`taccess_modules`twrite_owner_modules`tread_consumer_modules")
foreach ($tbl in ($tblSet.Keys | Sort-Object)) {
    $a = if ($accTbl.ContainsKey($tbl)) { ($accTbl[$tbl].Keys | Sort-Object) -join ';' } else { '' }
    $w = if ($writeTbl.ContainsKey($tbl)) { ($writeTbl[$tbl].Keys | Sort-Object) -join ';' } else { '' }
    $r = if ($readTbl.ContainsKey($tbl)) { ($readTbl[$tbl].Keys | Sort-Object) -join ';' } else { '' }
    [void]$sb.AppendLine("$tbl`t$a`t$w`t$r")
}
[System.IO.File]::WriteAllText((Join-Path $dir '_access-map.tsv'), $sb.ToString(), [System.Text.Encoding]::UTF8)
$wCount = ($writeTbl.Keys | Where-Object { $tblSet.ContainsKey($_) }).Count
$rCount = ($readTbl.Keys | Where-Object { $tblSet.ContainsKey($_) }).Count
$aCount = ($accTbl.Keys | Where-Object { $tblSet.ContainsKey($_) }).Count
Write-Host "ACCESS MAP: access-evidence tables=$aCount write-evidence=$wCount read-evidence=$rCount"
