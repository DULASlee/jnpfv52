# NG-1A：ProductAssetClass P0-PX 判定脚本 v2（证据驱动 + §0A.6 升级裁决：P0-PX 十类 + PlatformRole×AssetLifecycle 二维）
$ErrorActionPreference = 'Stop'
$dir = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1a-product-boundary'
$raw = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1-batch1\db-matrix-raw.tsv'
$out = Join-Path $dir 'platform-asset-classification.csv'

# 证据加载
$entity = @{}; Get-Content (Join-Path $dir '_entity-tables.txt') | ForEach-Object { $t = $_.Trim(); if ($t) { $entity[$t.ToUpper()] = $true } }
$init = @{};  Get-Content (Join-Path $dir '_init-sql-tables.txt')  | ForEach-Object { $t = $_.Trim(); if ($t) { $init[$t.ToUpper()] = $true } }
$refs = @{};  Get-Content (Join-Path $dir '_no-entity-refs.tsv')  | ForEach-Object { if ($_ -match '^(.+)\t(\d+)$') { $refs[$matches[1].ToUpper()] = [int]$matches[2] } }

function Get-AssetClass([string]$tbl) {
    $t = $tbl.ToUpper()
    $hasEntity = $entity.ContainsKey($t)
    $hasInit = $init.ContainsKey($t)
    $refCount = if ($refs.ContainsKey($t)) { $refs[$t] } else { -1 }  # -1 = 有实体表未在 no-entity 扫描

    # P5 测试表
    if ($t -match '^TEST') { return 'P5_TEST_FIXTURE' }
    # P4 用户通过低代码平台创建的动态业务表（MT 数字后缀）
    if ($t -match '^MT\d') { return 'P4_CUSTOMER_APPLICATION' }
    # P3 官方演示应用（ext_ 12 子域演示代码 + demo_ + 3 张演示流程表）；ext_ 登记 P2 模板候选
    if ($t -match '^EXT_') { return 'P3_DEMO_APPLICATION' }
    if ($t -match '^DEMO_') { return 'P3_DEMO_APPLICATION' }
    if ($t -in @('WFORM_LEAVEAPPLY','WFORM_SALESORDER','WFORM_SALESORDERENTRY')) { return 'P3_DEMO_APPLICATION' }
    # P2 产品模板（官方示例表单数据：OA 模板请假/报销/合同/差旅等，无业务代码仅备份清单引用）
    if ($t -match '^WFORM_') { return 'P2_PRODUCT_TEMPLATE' }
    # P6 历史遗留（init 打包 + 代码零业务引用）
    if ($t -match '^WM_|^WH_') { return 'P6_LEGACY' }
    if ($t -in @('BASE_STUDIO_MENU_BAK_20260617','STUDENT','DOMAIN_MODEL','KG_PATTERN','KG_PATTERN_USAGE','BASE_FILE')) { return 'P6_LEGACY' }
    # P7 彻底孤儿（无 init 无实体 0 引用）
    if ($t -eq 'BASE_VISUAL_FILTER') { return 'P7_ORPHAN' }
    # PX UNKNOWN（证据不足：独立报表前端 / 零引用零实体）
    if ($t -in @('DATA_REPORT','REPORT_CHARTS','REPORT_USER','REPORT_DEPARTMENT','BASE_TENANT_GLOSSARY','BASE_TENANT_INDUSTRY')) { return 'PX_UNKNOWN' }
    # P1 低代码运行时元数据（在线开发/数据大屏在线配置）
    if ($t -in @('BASE_VISUAL_DEV','BASE_VISUAL_LINK','BASE_VISUAL_RELEASE')) { return 'P1_LOWCODE_RUNTIME' }
    if ($t -match '^BLADE_VISUAL') { return 'P1_LOWCODE_RUNTIME' }
    # P0 平台核心（含基础设施：UNDO_LOG/SCHEMAVERSIONS/PROCESSED_EVENT/OUTBOX；有实体映射或代码引用的平台功能表）
    return 'P0_PLATFORM_CORE'
}

# 二维分类（PlatformRole × AssetLifecycle，§0A.6.2）
function Get-PlatformRole([string]$cls) {
    switch ($cls) {
        'P0_PLATFORM_CORE'      { return 'CORE' }
        'P1_LOWCODE_RUNTIME'    { return 'RUNTIME' }
        'P2_PRODUCT_TEMPLATE'   { return 'PRODUCT_CONTENT' }
        'P3_DEMO_APPLICATION'   { return 'PRODUCT_CONTENT' }
        'P4_CUSTOMER_APPLICATION'{ return 'EXTERNAL' }
        'P5_TEST_FIXTURE'       { return 'PRODUCT_CONTENT' }
        'P6_LEGACY'             { return 'LEGACY' }
        'P7_ORPHAN'             { return 'LEGACY' }
        'P8_EXTERNAL'           { return 'EXTERNAL' }
        default                 { return 'UNKNOWN' }
    }
}
function Get-AssetLifecycle([string]$cls) {
    switch ($cls) {
        'P0_PLATFORM_CORE'      { return 'MANDATORY' }
        'P1_LOWCODE_RUNTIME'    { return 'MANDATORY' }
        'P2_PRODUCT_TEMPLATE'   { return 'TEMPLATE' }
        'P3_DEMO_APPLICATION'   { return 'DEMO' }
        'P4_CUSTOMER_APPLICATION'{ return 'CUSTOMER_GENERATED' }
        'P5_TEST_FIXTURE'       { return 'TEST' }
        'P6_LEGACY'             { return 'LEGACY' }
        'P7_ORPHAN'             { return 'ORPHAN' }
        'P8_EXTERNAL'           { return 'UNKNOWN' }
        default                 { return 'UNKNOWN' }
    }
}

function Get-Evidence([string]$tbl, [string]$cls) {
    $t = $tbl.ToUpper()
    $hasEntity = $entity.ContainsKey($t)
    $hasInit = $init.ContainsKey($t)
    $refCount = if ($refs.ContainsKey($t)) { $refs[$t] } else { -1 }
    $ev = @()
    if ($hasEntity) { $ev += 'entity:SugarTable' } else { $ev += 'no-entity' }
    if ($hasInit) { $ev += 'init:ZXAFINIT.sql' } else { $ev += 'no-init(runtime-created)' }
    if ($refCount -ge 0) { $ev += "code-refs:$refCount" }
    if ($cls -eq 'P3_DEMO_APPLICATION' -and $t -match '^EXT_') { $ev += '12-services:JNPF.Extend;P2-template-candidate' }
    if ($cls -eq 'P2_PRODUCT_TEMPLATE' -and $refCount -ge 0 -and $refCount -le 2) { $ev += 'refs=DataBaseService backup-list' }
    if ($cls -in @('P6_LEGACY','P7_ORPHAN')) { $ev += 'zero-business-refs' }
    return ($ev -join '; ')
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('table_name,rows,cols,tenant_style,pk_type,entity_mapped,in_init_script,code_refs,product_asset_class,platform_role,asset_lifecycle,evidence')
foreach ($line in Get-Content $raw) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $f = $line -split "`t"
    $tbl = $f[0].Trim(); $pfx = $f[1]; $rows = $f[2]; $cols = $f[3]; $tn = $f[4]; $pk = $f[5]
    $t = $tbl.ToUpper()
    $cls = Get-AssetClass $t
    $role = Get-PlatformRole $cls
    $life = Get-AssetLifecycle $cls
    $hasEntity = if ($entity.ContainsKey($t)) { 'Y' } else { 'N' }
    $hasInit = if ($init.ContainsKey($t)) { 'Y' } else { 'N' }
    $refCount = if ($refs.ContainsKey($t)) { $refs[$t] } else { '' }
    $ev = Get-Evidence $tbl $cls
    [void]$sb.AppendLine("$tbl,$rows,$cols,$tn,$pk,$hasEntity,$hasInit,$refCount,$cls,$role,$life,$ev")
}
[System.IO.File]::WriteAllText($out, $sb.ToString(), [System.Text.Encoding]::UTF8)

# 统计
$rows = Get-Content $out | Select-Object -Skip 1
$total = $rows.Count
$stats = $rows | ForEach-Object { ($_ -split ',')[8] } | Group-Object | Sort-Object Count -Descending
Write-Host "TOTAL: $total"
$stats | ForEach-Object { Write-Host ("{0,-24} {1,4}  ({2:P1})" -f $_.Name, $_.Count, ($_.Count / $total)) }
