# NG-1B 证据 D 汇总：289 表 Creation Source 映射（273 init + 16 no-init）
$ErrorActionPreference = 'Stop'
$dir = 'D:\JNPF-v52\.claude\evidence\jnpf-next-architecture\ng1b-provenance'
$posFile = Join-Path $dir '_create-positions.tsv'

# 16 张无 init 表的创建源（手工映射，均经 Grep 精确行号验证）
$noInit = @(
    @('ai_ir_events',               'backend/modularity/inteAssistant/Migrations/20260704_Phase1_IR_Infrastructure.sql', 9,  'MIGRATION'),
    @('ai_ir_fragment_snapshots',   'backend/modularity/inteAssistant/Migrations/20260704_Phase1_IR_Infrastructure.sql', 33, 'MIGRATION'),
    @('ai_projects',                'backend/modularity/inteAssistant/Migrations/20260704_Phase1_IR_Infrastructure.sql', 53, 'MIGRATION'),
    @('ai_route_table',             'backend/modularity/inteAssistant/Migrations/20260704_Phase1_IR_Infrastructure.sql', 73, 'MIGRATION'),
    @('ai_skill_runs',              'backend/modularity/inteAssistant/Migrations/20260718_Phase2_Skills_Infrastructure.sql', 9, 'MIGRATION'),
    @('ai_seed_templates',          'backend/modularity/inteAssistant/Migrations/20260718_Phase2_Skills_Infrastructure.sql', 29, 'MIGRATION'),
    @('ai_skill_llm_policy',        'backend/modularity/inteAssistant/Migrations/20260801_Phase3_Design_Skills.sql', 28, 'MIGRATION'),
    @('ai_entity_field',            'backend/modularity/inteAssistant/Migrations/20260708_P9_Domain1_Entity_Field.sql', 19, 'MIGRATION'),
    @('sa_assumptions',             'backend/modularity/inteAssistant/Migrations/20260708_P9_ReqA.sql', 16, 'MIGRATION'),
    @('sa_consistency',             'backend/modularity/inteAssistant/Migrations/20260708_P9_ReqA.sql', 64, 'MIGRATION'),
    @('sa_quality_score',           'backend/modularity/inteAssistant/Migrations/20260708_P9_ReqA.sql', 127, 'MIGRATION'),
    @('BASE_AI_PIPELINE_S2_PROGRESS','backend/modularity/inteAssistant/Migrations/20260718_S2_Progress.sql', 9, 'MIGRATION'),
    @('BASE_AI_SKILL_REVIEW',       'backend/modularity/inteAssistant/Migrations/20260708_Phase7_Skill_Reviews.sql', 23, 'MIGRATION'),
    @('inte_assistant_deliverable', 'backend/modularity/inteAssistant/Migrations/20260705_SA_deliverable.sql', 7, 'MIGRATION'),
    @('BASE_REPORT',                'backend/modularity/report/init_report.sql', 11, 'MODULE_INIT'),
    @('inte_assistant_attachment',  'docs/AI原生开发/1、多用户多任务并行/inte_assistant_attachment.sql', 7, 'MANUAL_DDL')
)
$noInitMap = @{}
foreach ($r in $noInit) { $noInitMap[$r[0].ToUpper()] = $r }

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("table_name`tcreation_source`tposition`tmechanism")
$count = 0
foreach ($line in Get-Content $posFile) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $p = $line -split "`t"
    $tbl = $p[0].Trim()
    [void]$sb.AppendLine("$tbl`tDB/ZXAFINIT.sql`tbyte@$($p[1])`tINIT_SQL")
    $count++
}
# 追加 16 张无 init 表（不在 _create-positions.tsv 中）
foreach ($r in $noInit) {
    [void]$sb.AppendLine("$($r[0])`t$($r[1])`tL$($r[2])`t$($r[3])")
    $count++
}
[System.IO.File]::WriteAllText((Join-Path $dir '_creation-sources.tsv'), $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "CREATION SOURCES: $count rows (expect 289)"
$mig = 0; $mod = 0; $man = 0
foreach ($r in $noInit) { switch ($r[3]) { 'MIGRATION' { $mig++ } 'MODULE_INIT' { $mod++ } 'MANUAL_DDL' { $man++ } } }
Write-Host "  ZXAFINIT INIT_SQL: $($count - $noInit.Count)"
Write-Host "  MIGRATION: $mig"
Write-Host "  MODULE_INIT: $mod"
Write-Host "  MANUAL_DDL: $man"
