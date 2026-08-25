# NG-1B 产出物 1：provenance-matrix.csv（289 表 × 14 维 + 三态）
# 证据驱动：全部维度来自脚本扫描 + DB 实测 + file:line 验证，无主观判断
$ErrorActionPreference = 'Stop'
$dir = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1b-provenance'
$batch1 = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1-batch1'
$ng1a = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1a-product-boundary'

# ---- 加载证据源 ----
$raw = @{}; foreach ($l in Get-Content (Join-Path $batch1 'db-matrix-raw.tsv') -Encoding UTF8) {
    if ([string]::IsNullOrWhiteSpace($l)) { continue }
    $p = $l -split "`t"; $t = $p[0].Trim().ToUpper()
    if ($t -ne 'TABLE_NAME') { $raw[$t] = $p }
}
$cls = @{}; foreach ($l in Import-Csv (Join-Path $ng1a 'platform-asset-classification.csv')) { $cls[$l.table_name.ToUpper()] = $l }
$cre = @{}; foreach ($l in Import-Csv (Join-Path $dir '_creation-sources.tsv') -Delimiter "`t") { $cre[$l.table_name.ToUpper()] = $l }
$ent = @{}; foreach ($l in Import-Csv (Join-Path $dir '_entity-modules.tsv') -Delimiter "`t") { $ent[$l.table_name.ToUpper()] = $l }
$acc = @{}; foreach ($l in Import-Csv (Join-Path $dir '_access-map.tsv') -Delimiter "`t") { $acc[$l.table_name.ToUpper()] = $l }
$sas = @{}; foreach ($l in Import-Csv (Join-Path $dir '_saservice-refs.tsv') -Delimiter "`t") { $sas[$l.table_name.ToUpper()] = $l }
$seed = @{}; foreach ($l in Import-Csv (Join-Path $dir '_seed-map.tsv') -Delimiter "`t") { $seed[$l.table_name.ToUpper()] = [int]$l.seed_inserts }
$apiMods = @{}; foreach ($l in Import-Csv (Join-Path $dir '_api-services.tsv') -Delimiter "`t") { $apiMods[$l.module] = $true }
$fw = @{}; foreach ($l in Import-Csv (Join-Path $dir '_framework-evidence.tsv') -Delimiter "`t") { $fw[$l.table_name.ToUpper()] = $l }

# ---- 判定辅助 ----
function Has-ApiModule($row) {
    $mods = @()
    foreach ($k in @('access_modules','write_owner_modules','read_consumer_modules')) {
        if ($row.$k) { $mods += ($row.$k -split ';') }
    }
    $mods = $mods | Where-Object { $_ } | Select-Object -Unique
    return ($mods | Where-Object { $apiMods.ContainsKey($_) }).Count -gt 0
}
# UI 判定（菜单/前端实测驱动）
function Get-UiMenu($tbl) {
    if ($tbl -match '^EXT_') { return 'Y' }                      # extend.* 菜单 62 条实测（含 extend.order）
    if ($tbl -match '^WFORM_') { return 'Y' }                    # generator.flowForm 流程表单设计器入口
    if ($tbl -match '^WM_' -or $tbl -match '^WH_') { return 'LEGACY' }  # 仅 InStorage/OutStorage 静态页（弱）
    if ($tbl -match '^BASE_MODULE') { return 'Y' }               # 菜单/功能配置表自身（UI 元数据）
    return ''
}
function Get-Demo($tbl, $row) {
    if ($tbl -match '^EXT_') { return 'Y' }                      # Demo 菜单群 + Seed 测试数据
    if ($tbl -match '^DEMO') { return 'Y' }                      # DEMO_ 前缀表
    if ($tbl -match '^MT') { return 'N' }
    return ''
}
function Get-Template($tbl) {
    if ($tbl -match '^WFORM_') { return 'Y' }                    # 空表模板族（51 张预置）
    return ''
}

# ---- 三态判定（计划 §3 规则，脚本可复算）----
function Get-Provenance($tbl, $row, $apiY, $ui, $owner) {
    if ($fw[$tbl]) { return @(6, 'PROVEN') }   # 框架运行时表：源码级创建证据 + 框架角色确认（_framework-evidence.tsv）
    $c = $cre[$tbl]
    $creation = if ($c -and $c.creation_source) { 1 } else { 0 }
    $entity = if ($row.entity_mapped -eq 'Y') { 1 } else { 0 }
    $refs = 0
    if ($acc[$tbl]) { if ($acc[$tbl].access_modules -or $acc[$tbl].write_owner_modules -or $acc[$tbl].read_consumer_modules) { $refs = 1 } }
    if ($sas[$tbl] -and [int]$sas[$tbl].saservice_ref_files -gt 0) { $refs = 1 }
    if ($row.code_refs -and [int]$row.code_refs -gt 0) { $refs = 1 }
    $api = if ($apiY) { 1 } else { 0 }
    $uiscore = if ($ui -eq 'Y') { 1 } else { 0 }
    $ow = if ($owner) { 1 } else { 0 }
    $score = $creation + $entity + $refs + $api + $uiscore + $ow
    $hasWriteRead = $refs -eq 1
    if ($score -ge 5 -and $creation -eq 1 -and $ow -eq 1 -and $hasWriteRead) { return @($score, 'PROVEN') }
    if ($score -ge 2) { return @($score, 'PARTIAL') }
    return @($score, 'UNKNOWN')
}

# ---- 无实体表的前缀族 owner（模块归属映射，文件系统证据）----
function Get-PrefixOwner($tbl) {
    if ($tbl -match '^BASE_') { return 'system' }
    if ($tbl -match '^FLOW_' -or $tbl -match '^WFORM_') { return 'workflow' }
    if ($tbl -match '^EXT_') { return 'extend' }
    if ($tbl -match '^SA_' -or $tbl -match '^AI_' -or $tbl -match '^INTE_' -or $tbl -match '^EVAL_') { return 'inteAssistant' }
    if ($tbl -match '^PROCESSED_') { return 'infrastructure' }
    if ($tbl -eq 'SCHEMAVERSIONS' -or $tbl -eq 'UNDO_LOG') { return 'infrastructure' }
    if ($tbl -match '^BLADE_') { return 'visualdata' }
    if ($tbl -match '^KG_') { return 'inteAssistant' }
    if ($tbl -match '^SYS_') { return 'system' }
    if ($tbl -match '^WM_' -or $tbl -match '^WH_') { return '' }   # 孤儿：无代码 owner（42 张实测）
    return ''
}

$sb = New-Object System.Text.StringBuilder
$hdr = 'table_name,prefix,db_rows,db_cols,pk_type,creation_source,creation_pos,mechanism,code_owner,entity_mapped,write_owner,read_consumers,saservice_refs,api_exposed,ui_menu,template,seed_inserts,demo,runtime_required,startup_impact,product_deliverable,asset_class,platform_role,asset_lifecycle,score,provenance'
[void]$sb.AppendLine($hdr)
$stats = @{ PROVEN = 0; PARTIAL = 0; UNKNOWN = 0 }
$rows = 0
foreach ($tbl in ($raw.Keys | Sort-Object)) {
    $p = $raw[$tbl]; $row = $cls[$tbl]
    if (-not $row) { Write-Host "MISSING CLASS: $tbl"; continue }
    $c = $cre[$tbl]; $e = $ent[$tbl]; $a = $acc[$tbl]; $s = $sas[$tbl]
    $owner = if ($e) { $e.module } else { Get-PrefixOwner $tbl }
    $apiY = if ($a) { Has-ApiModule $a } else { $false }
    $ui = Get-UiMenu $tbl
    $demo = Get-Demo $tbl $row
    $tpl = Get-Template $tbl
    $sd = if ($seed.ContainsKey($tbl)) { $seed[$tbl] } else { 0 }
    $sdFlag = if ($sd -gt 0) { 'Y' } else { '' }
    $rt = if ($row.product_asset_class -match '^P[01]_') { 'Y' } else { 'N' }
    $su = if ($rt -eq 'Y') { 'REQUIRED' } else { 'REMOVABLE*' }
    $pd = if ($row.product_asset_class -match '^P[012]_') { 'Y' } else { 'N' }
    $prov = Get-Provenance $tbl $row $apiY $ui $owner
    $stats[$prov[1]]++
    $w = if ($a) { $a.write_owner_modules } else { '' }
    $r = if ($a) { $a.read_consumer_modules } else { '' }
    $sr = if ($s) { $s.saservice_ref_files } else { '0' }
    $cs = if ($c) { $c.creation_source } else { '' }
    $cp = if ($c) { $c.position } else { '' }
    $cm = if ($c) { $c.mechanism } else { '' }
    $vals = @($tbl, $p[1], $p[2], $p[3], $p[5], $cs, $cp, $cm, $owner, $row.entity_mapped, $w, $r, $sr,
              $(if ($apiY) { 'Y' } else { '' }), $ui, $tpl, $sd, $demo, $rt, $su, $pd,
              $row.product_asset_class, $row.platform_role, $row.asset_lifecycle, $prov[0], $prov[1])
    # CSV 转义（含逗号/引号的字段）
    $escaped = $vals | ForEach-Object { $s0 = [string]$_; if ($s0 -match '[",]') { '"' + $s0.Replace('"','""') + '"' } else { $s0 } }
    [void]$sb.AppendLine(($escaped -join ','))
    $rows++
}
$out = Join-Path $dir 'provenance-matrix.csv'
[System.IO.File]::WriteAllText($out, $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "ROWS: $rows (expect 289)"
Write-Host ("PROVEN={0} PARTIAL={1} UNKNOWN={2}" -f $stats['PROVEN'], $stats['PARTIAL'], $stats['UNKNOWN'])
