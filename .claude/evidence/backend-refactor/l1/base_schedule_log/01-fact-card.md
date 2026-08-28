# 事实卡 - BASE_SCHEDULE_LOG

**批次**：L1 首批 #3（score=-1）｜ **日期**：2026-08-26 ｜ **通道**：sqlcmd 实测 + Serena/代码全文扫描

| 项 | 内容 |
|---|---|
| 列清单（35列实测） | 主键 f_id nvarchar(100)；业务列 22（category/urgent/title/content/all_day/start_day/end_day/duration/color/reminder_time/reminder_type/send_config/repetition/push_time/group_id/user_id/schedule_id/operation_type/description + CLDSEntityBase.EnabledMark）；审计列 9（基类 CLDEntityBase：Id/creator/last_modify/delete_mark/sort_code）；租户 f_tenant_id；系统 f_zx_system_id。**f_user_id 是 nvarchar(max)**——存逗号分隔的用户ID列表，无法高效过滤。无 text 遗留类型，全部小写规范 |
| 索引现状 | 仅聚簇主键 PK__base_sch__2911CBED67069D3C；零二级索引 |
| 物理外键 | 无；逻辑关系：f_schedule_id→ScheduleEntity、f_user_id→BASE_USER（弱引用，逗号字符串） |
| 引用代码位置 | 实体 Entity/System/ScheduleLogEntity.cs:9 `[SugarTable("BASE_SCHEDULE_LOG")]`；写入 System/ScheduleService.cs L293-858（`AddScheduleLog()` 出现 20+ 处，覆盖创建/更新/重发/状态变更四种操作类型）；读路径：**全后端零处 Queryable**、前端零引用 |
| 读写方模块 | 写=system 模块（ScheduleService 20+ 处 Insertable）；**读=无任何消费者**（read_consumers 空 + api_exposed=Y 实为虚标） |
| 行数分布 | 0 行（空表，无种子数据）；运行时用户创建日程后会自动生成 |
| 事务边界/慢查询 | query-hotspots 无登记；Insertable 无显式事务；批量创建时（repeatEntity）在同一方法内多次调用 Insertable，无事务包装 |

## 已锁定怪异（考卷级发现）

| # | 怪异 | 实证 |
|---|---|---|
| 1 | **只写不读审计表**：ScheduleService 有 20+ 处写入调用，但全后端+前端无任何查询/展示 ScheduleLog 的代码。日志只存不看，写放大无收益 | 代码扫描 39 匹配均为写入或类型定义 |
| 2 | **f_user_id nvarchar(max) 存逗号分隔列表**：`string.Join(",", input.toUserIds)` — 每次操作存一份用户ID快照。无法建索引，无法高效查询（违反1NF） | ScheduleService.cs L304/456 |
| 3 | **批量写入无事务**：repeatEntity 循环内多次 Insertable 无事务包装，异常中断将导致日志不一致 | ScheduleService.cs L322-377 |

## 初判（供台账分级）

- 零行空表+零读消费者 → **退役候选**（C 级裁决）；
- 如果保留：f_user_id 设计需重新考虑（JSON 列或关联表），但属 C 级架构变更；
- 如果退役：需评估是否有外部系统（如审计模块）依赖此表——目前代码未发现；
- 空表演练习：本表的价值在于**暴露工具链盲区**——对空表的 SOQL/索引设计无实际收益，工具链应输出"跳过"或"归档"提示而非等待数据填充。
