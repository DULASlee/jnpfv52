# 事实卡 - BASE_PRINT_LOG

**批次**：L1 首批 #1（score=-1）｜ **日期**：2026-08-26 ｜ **通道**：sqlcmd 实测 + 代码全文扫描 + API 实调验证

| 项 | 内容 |
|---|---|
| 列清单（14列实测） | 主键 f_id nvarchar(100)；业务列 4（f_print_num int / f_print_title nvarchar(510) / f_print_id nvarchar(100)）；基类审计 9 列；租户 f_tenant_id；系统 f_zx_system_id。**全部小写规范**（与 base_api_log 大小写混排形成对照，两表建表规范不一致）；无 text 遗留类型 |
| 索引现状 | 仅聚簇主键 PK__base_pri__2911CBED0B82DC7D；零二级索引；GetList 唯一过滤列 f_print_id 无索引 |
| 物理外键 | 无；逻辑关系：f_print_id→打印方案、f_creator_user_id→BASE_USER |
| 引用代码位置 | 实体 Entity/System/PrintLogEntity.cs:14；服务 System/PrintLogService.cs:27(IDynamicApiController→/api/System/PrintLog) ：57 [HttpGet("{id}")]GetList 读 ：85-95 [HttpPost("save")] 写(AsInsertable) |
| 读写方模块 | system 模块闭环（写 save / 读 GetList）；前端打印功能消费 |
| 行数分布 | 21 行全种子(ZXAFINIT.sql)、F_DELETE_MARK 全 0；**f_tenant_id 21/21 全 NULL**；f_creator_user_id 20/21 NULL |
| 事务边界/慢查询 | query-hotspots 无登记；单条 Insertable 无显式事务 |

## 已锁定怪异（考卷级发现）

| # | 怪异 | 实证 |
|---|---|---|
| 1 | **R4 租户过滤陷阱实证**：种子数据 f_tenant_id 全 NULL，运行时 ITenantFilter（admin=租户0）将其全部滤除 → GET /api/system/PrintLog/{id} 恒返回 total=0（HTTP 200 静默失效） | 快照 printlog-list-01 已录制锁定现状；sqlcmd GROUP BY 证实 NULL 21/21 |
| 2 | **方法名语义颠倒**：[HttpPost("save")] 的方法名竟为 `Delete`，实现是 AsInsertable 插入（L92）——纯命名缺陷，误导维护者 | PrintLogService.cs:86-95 |
| 3 | 种子数据质量：creator_user_id 缺失率 95%（20/21 NULL），GetList 的 JOIN 用户信息将大面积空 | sqlcmd TOP 3 实测 |

## 初判（供台账分级）

- 表体量小且有真实读写链路 → 非僵尸表，保留价值明确；
- 核心问题是**种子数据租户缺失**导致功能静默失效——修复动作是数据 UPDATE（B 级，需备份快照后执行），非结构变更；
- f_print_id 索引：日志量级小且增长受限（人工触发打印），索引收益趋零 → 关闭。
