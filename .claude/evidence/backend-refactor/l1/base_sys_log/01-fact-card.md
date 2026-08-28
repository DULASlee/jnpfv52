# 事实卡 - BASE_SYS_LOG

**批次**：L1 首批 #5（score=0）｜ **日期**：2026-08-26 ｜ **通道**：sqlcmd 实测 + 代码扫描

| 项 | 内容 |
|---|---|
| 列清单（32列实测） | 主键 f_id nvarchar(100)；业务列 20（user_id/user_name/type/level/ip_address/ip_address_name/request_url/request_method/request_duration/json/plat_form/module_id/module_name/object_id/description/browser/request_param/request_target/login_mark/login_type）；基类审计 9 列；租户 f_tenant_id；系统 f_zx_system_id；**F_TRACE_ID nvarchar(128)**（大写命名，疑似后补列——链路追踪ID）。**无 text 遗留类型**；无 nvarchar(max)。唯一大写列 F_TRACE_ID |
| 索引现状 | 聚簇主键 PK__base_sys__2911CBED3C589CD7；**二级索引 IX_SYS_LOG_TRACE_ID（非唯一）** — 首批5张中唯一有二级索引的表 |
| 物理外键 | 无；逻辑关系：f_user_id→BASE_USER、f_module_id→BASE_MODULE（弱引用，日志语义） |
| 引用代码位置 | 实体 Entity/System/SysLogEntity.cs；写入：4处事件总线 PublishAsync（LogEventSource）——Common.Core/Filter/RequestActionFilter.cs L127(CreateReLog)/L154(CreateOpLog) + LogExceptionHandler.cs L51(CreateExLog) + OAuthService.cs L1358(CreateVisLog)；读取：System/SysLogService.cs L58-81 GetList 分页查询 |
| 读写方模块 | 写=common模块（事件总线异步写入，4种日志类型：请求/操作/异常/访问）；读=system模块（SysLogService.GetList）+前端 api/system/log.ts 调用 `/api/system/Log` |
| 行数分布 | 12615 行全部存活（delete_mark=0）；**全部为种子数据**（seed_inserts=12615）；query-hotspots 登记"日志写密集无归档策略" |
| 事务边界/慢查询 | query-hotspots 登记"审计日志双写：base_sys_log + base_api_log + BASE_AI_CALL_LOG 三族写放大"；事件总线写入无显式事务 |

## 本表特殊性（对比前4张）

| 维度 | base_sys_log | 前4张表 |
|------|-------------|--------|
| 行数 | 12615（唯一大表） | 0-39行 |
| 索引 | 有二级索引（IX_SYS_LOG_TRACE_ID） | 零二级索引 |
| 写入模式 | 事件总线异步（LogEventSource） | 直接 Insertable |
| 读取消费者 | 后端 SysLogService + 前端 api/system/log.ts | 0-1个（仅后端） |
| 无归档策略 | 12615行持续增长无清理 | 0-39行不增长 |

## 初判（供台账分级）

- **归档策略缺失**：12615行持续增长无清理机制 → A级候选（需设计归档/清理策略+执行）
- **F_TRACE_ID 大写命名**：疑似后补列，与其他列命名不一致 → C级（改名需 ALTER COLUMN）
- **事件总线写入可审计**：4种日志类型分离清晰，但"审计日志双写"模式需评估是否冗余
