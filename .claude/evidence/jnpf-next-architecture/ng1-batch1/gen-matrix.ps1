# NG-1 第一批：289 表六维矩阵 CSV 生成脚本（v1：DB 实测维度 + 前缀域映射）
$ErrorActionPreference = 'Stop'
$src = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1-batch1\db-matrix-raw.tsv'
$out = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1-batch1\ownership-matrix-v1.csv'

function Get-Domain([string]$tbl) {
    $t = $tbl.ToLower()
    # 精确细分（base_ 小写族）
    if ($t -match '^base_user') { return 'D1-Identity' }
    if ($t -match '^base_organize|^base_role$|^base_position|^base_group|^base_socials') { return 'D1-Identity' }
    if ($t -match '^base_module|^base_authorize|^base_permission_group|^base_columns_purview') { return 'D3-Permission' }
    if ($t -match '^base_advanced_query_scheme') { return 'D3-Permission' }
    if ($t -match '^base_visual') { return 'D5-FormLowCode' }
    if ($t -match '^base_dictionary|^base_portal|^base_province') { return 'D6-Dictionary' }
    if ($t -match '^base_file|^base_sign') { return 'D7-File' }
    if ($t -match '^base_message|^base_msg|^base_notice|^base_im') { return 'D8-Message' }
    if ($t -match '^base_sys_log|^base_api_log') { return 'D9-Log' }
    if ($t -match '^base_sys_config|^base_system$') { return 'D2-Tenant' }
    if ($t -match '^base_bill_rule') { return 'TBD-BillRule' }
    if ($t -match '^base_schedule|^base_time_task') { return 'TBD-Job' }
    if ($t -match '^base_db_link|^base_data_interface|^base_integrate') { return 'TBD-Integration' }
    if ($t -match '^base_common_|^base_print|^base_app_data|^base_cache') { return 'TBD-Platform' }
    if ($t -match '^base_') { return 'TBD-Base' }
    # 大写 BASE_ 族
    if ($tbl -like 'BASE_AI_*' -or $tbl -like 'BASE_IR_*' -or $tbl -like 'BASE_KNOWLEDGE_*' -or $tbl -like 'BASE_STUDIO_*' -or $tbl -like 'BASE_SANDBOX*') { return 'D10-AI' }
    if ($tbl -like 'BASE_TENANT_*') { return 'D2-Tenant' }
    if ($tbl -like 'BASE_REPORT*') { return 'D11-Report' }
    if ($tbl -like 'BASE_FOUNDER_*') { return 'D9-Log' }
    if ($tbl -like 'BASE_MENU_*') { return 'TBD-Platform' }
    if ($tbl -like 'BASE_*') { return 'TBD-BaseUpper' }
    switch -Regex ($t) {
        '^ai_|^sa_|^inte_|^kg_|^eval_' { return 'D10-AI' }
        '^flow_|^wform_' { return 'D4-Workflow' }
        '^ext_' { return 'D12-OrderExt' }
        '^wm_' { return 'D12-WM-ORPHAN' }
        '^wh_' { return 'D12-WH-ORPHAN' }
        '^demo_' { return 'D12-Demo-ORPHAN' }
        '^blade_|^data_report|^report_' { return 'D11-Report' }
        '^zx_' { return 'D2-Tenant' }
        '^mt' { return 'D5-DynamicTable' }
        '^sys_' { return 'D8-Outbox' }
        '^processed_' { return 'D8-Event' }
        '^undo_log' { return 'INFRA-Seata' }
        '^schemaversions' { return 'INFRA-Migration' }
        '^domain_model' { return 'TBD-DomainModel' }
        '^student' { return 'TBD-Demo' }
        default { return 'UNKNOWN' }
    }
}

function Get-Class([string]$domain, [string]$tbl) {
    if ($domain -eq 'UNKNOWN' -or $domain -like 'TBD-*') { return 'UNKNOWN' }
    if ($domain -like '*ORPHAN*') { return 'UNKNOWN' }
    if ($domain -eq 'D5-DynamicTable') { return 'SHARED' }
    if ($tbl -eq 'flow_form_authorize') { return 'OWNERSHIP-CONFLICT' }
    if ($tbl -eq 'base_signature' -or $tbl -eq 'base_signature_user') { return 'UNKNOWN' }
    if ($tbl -match '^base_user$|^base_module$|^base_authorize|^base_dictionary') { return 'SHARED' }
    return 'OWNED-PENDING'
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('table_name,prefix,rows,cols,tenant_style,pk_type,domain,domain_source,classification,write_owner,read_consumers,transaction,evidence')
foreach ($line in Get-Content $src) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $f = $line -split "`t"
    $tbl = $f[0]; $pfx = $f[1]; $rows = $f[2]; $cols = $f[3]; $tn = $f[4]; $pk = $f[5]
    $dom = Get-Domain $tbl
    $cls = Get-Class $dom $tbl
    $srcTag = if ($dom -like 'D12-*ORPHAN*') { 'NG1-grep-zero-refs' } else { 'NG0-prefix-cluster+NG1-db' }
    $wo = ''; $rc = ''; $tx = ''; $ev = 'ng1-batch1/db-matrix-raw.tsv'
    if ($tbl -like 'ext_order*') {
        $wo = 'OrderService.Save/Delete(OrderService.cs L227-238,L253-259)'
        $rc = 'OrderService.GetList 3-join(OrderService.cs L84-106)'
        $tx = 'NONE(0 BeginTran in extend module)'
        $ev = 'OrderService.cs L199-269 + BeginTran scan'
    }
    if ($tbl -like 'WM_*' -or $tbl -like 'WH_*' -or $tbl -like 'Demo_*') {
        $ev = 'grep SugarTable("WM_/WH_/Demo_ = 0 matches; code orphan'
    }
    [void]$sb.AppendLine("$tbl,$pfx,$rows,$cols,$tn,$pk,$dom,$srcTag,$cls,$wo,$rc,$tx,$ev")
}
[System.IO.File]::WriteAllText($out, $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "rows: $((Get-Content $src | Where-Object { $_.Trim() -ne '' }).Count) -> $out"
