# gen-l1-order.ps1 — L1 表级螺旋批次排序（可复算）
# 输入：ng1a 资产分类 / ng1b 来源矩阵 / db-fks.tsv / _access-map.tsv
# 输出：l1-batch-order.csv —— ELIGIBLE 按 score 升序在前，MULTI_WRITER 附后
# 规则：见《L1 表级螺旋执行手册》§1；排序分 = 入站FK*2 + 读方模块数 + API暴露*2 − 日志类*3

$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '..\..\jnpf-next-architecture'

$cls  = Import-Csv (Join-Path $root 'ng1a-product-boundary\platform-asset-classification.csv')
$prov = Import-Csv (Join-Path $root 'ng1b-provenance\provenance-matrix.csv')

# 入站外键计数（to_table 为被引用方）
$fkIn = @{}
Get-Content (Join-Path $root 'db-fks.tsv') |
    Where-Object { $_ -match '\S' } | ForEach-Object {
        $p = $_ -split "`t"
        if ($p.Count -ge 2) {
            $k = $p[1].Trim().ToUpper()
            if ($k) { $fkIn[$k] = 1 + ($fkIn[$k] ?? 0) }
        }
    }

# 访问映射（跳过表头行）
$map = @{}
Get-Content (Join-Path $root 'ng1b-provenance\_access-map.tsv') |
    Where-Object { $_ -match '\S' } | Select-Object -Skip 1 | ForEach-Object {
        $p = $_ -split "`t"
        if ($p.Count -ge 4) { $map[$p[0].Trim().ToUpper()] = [pscustomobject]@{ w = $p[2]; r = $p[3] } }
    }

$provH = @{}
foreach ($p in $prov) { $provH[$p.table_name.ToUpper()] = $p }

$rowsOut = @()
foreach ($c in $cls) {
    $name = $c.table_name
    if ($c.product_asset_class -notin @('P0_PLATFORM_CORE', 'P1_LOWCODE_RUNTIME')) { continue }
    if ($name -match '^sa_') { continue }
    if ($name -in @('SchemaVersions', 'undo_log')) { continue }

    $u  = $name.ToUpper()
    $pv = $provH[$u]
    $m  = $map[$u]

    $writers = @(); if ($m -and $m.w) { $writers = ($m.w -split ';') | Where-Object { $_ } }
    $readers = @(); if ($m -and $m.r) { $readers = ($m.r -split ';') | Where-Object { $_ } }

    $inFk  = $fkIn[$u] ?? 0
    $isLog = ($u -match 'LOG$') ? 1 : 0
    $api   = ($pv -and $pv.api_exposed -eq 'Y') ? 1 : 0
    $wCount = [Math]::Max($writers.Count, 1)

    $score  = 2 * $inFk + $readers.Count + 2 * $api - 3 * $isLog
    $status = ($wCount -gt 1) ? 'MULTI_WRITER' : 'ELIGIBLE'

    $rowsOut += [pscustomobject]@{
        table_name     = $name
        asset_class    = $c.product_asset_class
        db_rows        = $pv.db_rows
        writer_modules = ($writers -join '|')
        writer_count   = $wCount
        reader_count   = $readers.Count
        inbound_fks    = $inFk
        api_exposed    = $api
        log_class      = $isLog
        score          = $score
        status         = $status
    }
}

$out = Join-Path $PSScriptRoot 'l1-batch-order.csv'
$sorted = $rowsOut | Sort-Object status, score, @{ expression = 'db_rows'; Descending = $true }
$sorted | Export-Csv -NoTypeInformation -Encoding UTF8 $out

$elig = @($rowsOut | Where-Object status -eq 'ELIGIBLE')
$mw   = @($rowsOut | Where-Object status -eq 'MULTI_WRITER')
Write-Host "总入围: $($rowsOut.Count) ｜ ELIGIBLE: $($elig.Count) ｜ MULTI_WRITER(暂缓): $($mw.Count)"
Write-Host "--- ELIGIBLE 前 15（score 升序）---"
$elig | Sort-Object score, @{ expression = 'db_rows'; Descending = $true } |
    Select-Object -First 15 table_name, db_rows, writer_modules, reader_count, inbound_fks, api_exposed, log_class, score |
    Format-Table -AutoSize | Out-String | Write-Host
