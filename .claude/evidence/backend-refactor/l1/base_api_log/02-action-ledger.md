# 行动台账 - BASE_API_LOG

| # | 动作 | 级别 | 状态 | 证据 |
|---|---|---|---|---|
| 1 | 二级索引设计：**暂缓**——读消费者为 0，无可优化查询；待消费方落地后按查询模式设计 | A(改判) | 关闭-不动作 | 01-fact-card.md §读写方模块 |
| 2 | text×4 列(F_REQUEST_Body/Headers/Result/F_Msg) → nvarchar(max) 迁移+回滚脚本 | C | 裁决队列 | 事实卡列清单；registry.v1 §3「text/ntext REMOVE」 |
| 3 | 列名大小写统一(6 列大写混排) | C | 裁决队列 | INFORMATION_SCHEMA 实测 |
| 4 | DataInterfaceService 双插写放大 → 异步化/合并评估 | A候选 | 待批——需先确认消费方存在性 | query-hotspots.md 审计日志双写条目 |
| 5 | 僵尸表定性上报：只写不读+全种子数据，建议纳入退役评估或补齐消费方 | C | 裁决队列 | 溯源矩阵 read_consumers 空 + seed_inserts=39 |

> 本表首批结论：**零结构变更落地**。全部实质项为 C 级进裁决队列；A 级唯一候选(#4)挂起等消费方确认。
> 验证义务相应免除：无结构变更 → 无需快照比对/性能对比（03-validation.md 记录此豁免依据）。
