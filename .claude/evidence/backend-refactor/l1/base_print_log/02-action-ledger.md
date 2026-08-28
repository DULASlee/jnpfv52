# 行动台账 - BASE_PRINT_LOG

| # | 动作 | 级别 | 状态 | 证据 |
|---|---|---|---|---|
| 1 | 种子数据租户补齐：UPDATE f_tenant_id NULL→'0'（21行，先备份快照） | B | **待请示**——数据修正，用户逐项审批制 | 01-fact-card.md 怪异#1 |
| 2 | PrintLogService.Delete 方法重命名为 Create/Save（语义颠倒纠正） | A(代码) | **待请示**——业务代码修改 | 01-fact-card.md 怪异#2 |
| 3 | f_print_id 二级索引：关闭-不动作（体量小增长受限，收益趋零） | A(改判) | 关闭 | 01-fact-card.md §索引现状 |
| 4 | 怪异#1/#2 登记入 legacy-compatibility-registry（下版升格时补录 §5 不复制清单） | 文档 | 待办-随registry v1.1 | 快照 printlog-list-01 |

> 与 base_api_log 的对照结论：两表同分(-1)同族(日志)，但生命周期完全不同——base_print_log 有真实读写链路值得保留，base_api_log 是只写不读僵尸。**日志类不能一刀切退役**。
