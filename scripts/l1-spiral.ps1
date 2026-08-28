# L1 表级螺旋 - 极简执行器
# 用法:
#   .\l1-spiral.ps1 List              # 看首批队列(ELIGIBLE 头部)
#   .\l1-spiral.ps1 Init base_print_log   # 为某表建四件套目录+模板
#   .\l1-spiral.ps1 Check base_print_log  # 归档完整性检查(四件套齐没齐)
# 规则: 本脚本不碰数据库不改代码; 观察/行动/验证按手册 SOP 由人+AI 执行。
param(
  [Parameter(Position = 0)][string]$Cmd = 'List',
  [Parameter(Position = 1)][string]$Table,
  [int]$Top = 5
)
$ErrorActionPreference = 'Stop'
$root = '.claude/evidence/backend-refactor/l1'
$order = Join-Path $root 'l1-batch-order.csv'

if (-not (Test-Path $order)) { Write-Error "找不到排序清单: $order"; exit 1 }
$rows = Import-Csv $order

switch ($Cmd) {
  'List' {
    "=== ELIGIBLE 前 $Top (score 升序, 叶子优先):"
    $rows | Where-Object status -eq 'ELIGIBLE' | Select-Object -First $Top |
      Format-Table table_name, score, db_rows, writer_modules -AutoSize | Out-String -Width 200
    "暂缓 MULTI_WRITER: $(@($rows | Where-Object status -eq 'MULTI_WRITER').Count) 张; 剩余 ELIGIBLE: $(@($rows | Where-Object status -eq 'ELIGIBLE').Count)"
  }
  'Init' {
    if (-not $Table) { Write-Error '用法: .\l1-spiral.ps1 Init <table_name>'; exit 1 }
    $dir = Join-Path $root $Table
    if (Test-Path $dir) { "已存在: $dir"; exit 0 }
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    @(
      @{ n = '01-fact-card.md'; c = "# 事实卡 - $Table`n`n| 项 | 内容 |`n|---|---|`n| 列清单(INFORMATION_SCHEMA实测) | 待填 |`n| 索引现状(含碎片率) | 待填 |`n| 物理外键/逻辑关系 | 待填 |`n| 引用代码位置(Serena+文本双通道) | 待填 |`n| 读写方模块 | 待填 |`n| 行数与关键列分布 | 待填 |`n| 事务边界/已知慢查询 | 待填 |`n" },
      @{ n = '02-action-ledger.md'; c = "# 行动台账 - $Table`n`n| # | 动作 | 级别(A/B/C) | 状态 | 证据 |`n|---|---|---|---|---|`n| 1 | | A/B/C | 待办 | |`n" },
      @{ n = '03-validation.md'; c = "# 验证记录 - $Table`n`n- 考卷: 待跑(pnpm test:api)`n- CRUD快照比对: 待录(tests/characterization/fixtures/$Table/)`n- 性能前后对比(如有): 存档编号=待填`n" },
      @{ n = '04-open-items.md'; c = "# 遗留问题清单 - $Table`n`n| B/C未决项 | 级别 | 去向(下批/裁决会) |`n|---|---|---|`n" }
    ) | ForEach-Object { [System.IO.File]::WriteAllText((Join-Path $dir $_.n), $_.c, (New-Object System.Text.UTF8Encoding $false)) }
    "已初始化: $dir (四件套模板就位)"
  }
  'Check' {
    if (-not $Table) { Write-Error '用法: .\l1-spiral.ps1 Check <table_name>'; exit 1 }
    $dir = Join-Path $root $Table
    $need = '01-fact-card.md', '02-action-ledger.md', '03-validation.md', '04-open-items.md'
    $missing = @($need | Where-Object { -not (Test-Path (Join-Path $dir $_)) })
    if ($missing.Count -eq 0) { "$Table 四件套齐全 ✅" } else { "$Table 缺少: $($missing -join ', ') ❌"; exit 2 }
  }
  default { Write-Error "未知命令: $Cmd (可用: List / Init / Check)"; exit 1 }
}
