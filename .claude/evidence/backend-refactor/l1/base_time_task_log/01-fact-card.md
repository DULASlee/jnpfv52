# 事实卡 - BASE_TIME_TASK_LOG

**批次**：L1 首批 #4（score=0）｜ **日期**：2026-08-26 ｜ **通道**：sqlcmd 实测 + 代码扫描

| 项 | 内容 |
|---|---|
| 列清单（16列实测） | 主键 f_id nvarchar(100)；业务列 4（f_task_id/f_run_time/f_run_result/f_description）；CLDSEntityBase（EnabledMark）；基类审计 9 列；租户 f_tenant_id；系统 f_zx_system_id。**全部小写规范**；无 text 遗留类型；无 nvarchar(max) |
| 索引现状 | 仅聚簇主键 PK__base_tim__2911CBED9958089B；零二级索引；**f_task_id 是主查询过滤列（GetList WHERE TaskId=id）但无索引** |
| 物理外键 | 无；逻辑关系：f_task_id→TimeTaskEntity（任务调度器实体） |
| 引用代码位置 | 实体 Entity/TimeTaskLogEntity.cs:13 `[SugarTable("BASE_TIME_TASK_LOG")]`；写入 TaskScheduler/TimeTaskService.cs:236 + Common.Core/Job/DbJobPersistence.cs:260-268（Job 完成时 Insertable）；读取 TaskScheduler/TimeTaskService.cs:99-109 `[HttpGet("{id}/TaskLog")]` 分页查询 |
| 读写方模块 | 写=taskscheduler（任务执行+Job完成双写）；读=taskscheduler（GET /{id}/TaskLog 分页）；前端无引用（日志仅后端消费） |
| 行数分布 | 22 行种子数据（ZXAFINIT.sql）；22/22 delete_mark=0；种子 f_task_id 值待验是否关联有效任务 |
| 事务边界/慢查询 | query-hotspots 无登记；单条 Insertable 无显式事务（不需要） |

## 本表特殊性（对比前3张）

前3张表均为 system 模块的日志表，本表是 **taskscheduler 模块内闭环**的运行日志：
- 有真实读写链路（GetList + Insertable 两端齐全）
- code_owner=read_consumers=taskscheduler（模块自产自销）
- **是首批5张中最"健康"的表**——无僵尸、无租户陷阱、无语义颠倒

## 初判（供台账分级）

- **f_task_id 索引**：主查询过滤列无索引，22行时无感知，任务量增长后会全表扫 → A级候选（加索引+回滚脚本），但需请示
- 种子数据孤儿引用验证：f_task_id 的值是否关联有效的 TimeTaskEntity → B级（先备份再验证+修复）
- 无其他结构性问题
