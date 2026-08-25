# NG-0 证据 3/11 — 查询热点分析（DB-3）

**来源**：DB 实测（表行数/动态表）+ 代码侧审计（S1-Final：CC 清单/ORM 模式/条件注入链）——本阶段为**静态热点**（无 SQL Profiler 追踪；运行期采样列入 NG-1）

## 1. 数据量热点（大表）

| 表 | 行数 | 热点特征 |
|----|-----:|---------|
| base_province | 47512 | 省市区字典（只读高频、全量缓存候选） |
| base_sys_log | 12615 | 日志写密集（无归档策略观察） |
| ai_ir_events | 3780 | AI 事件流（追加写 + 事件消费） |
| base_authorize | 2553 | **权限校验高频读**（每个数据权限入口先查） |
| WM_BillDetail/WM_CheckBillDetail | 1629+1613 | 演示业务明细 |
| BASE_AI_CALL_LOG | 1502 | AI 调用日志（追加写） |
| base_message | 1229 | 站内信 |
| ai_entity_field | 824 | **字段元数据唯一源**（AI 链路热读） |

## 2. 代码侧热点（S1-Final 审计数据）

| 热点 | 证据 | 说明 |
|------|------|------|
| **动态 SQL 巨型构建** | `RunSqlCompiler.GetListQuerySql` CC=113 | 列表查询 SQL 拼装（排序/过滤/分页/权限注入合一）——S2 核心目标之一 |
| **数据聚合查询** | `GetKeyData` CC=160 | 表单数据聚合（表单+子表+权限） |
| **条件注入链** | DataBaseManager L563-566：`SqlQueryable.Where(dataRuleJson).Where(querJson).Where(superQueryJson,true).Where(dataPermissions)` | 四段条件叠加：数据规则/普通查询/超级查询/数据权限——**权限过滤+租户过滤的汇聚点** |
| **权限入口** | `GetCondition`（路径 A）/`GetConditionAsync`（路径 B） | 每列表查询都执行：IsAdministrator → 分级管理 → authorize 查库 → scheme 查库（**base_authorize+base_module+scheme 三连查**） |
| **会话直查面** | 403 ORM 文件/118 Service 含 AsSugarClient/Queryable 173 文件 | 查询分布全业务面，无集中仓储 |
| **Join 链** | OrderService 三表 Join（OrderEntity+UserEntity+FlowTaskEntity） | 业务表 Join 用户/流程——**N+1 风险模式**（列表+详情每行再查） |
| **租户过滤挂靠** | ITenantFilter 12 文件（DataBaseManager/TenantManager/EntityBase/visualdata 8 实体） | 租户过滤在仓储/实体层隐式注入——**查询均隐式携带租户约束**（P0-C 要点） |
| **动态表** | mt* 5 张（低代码运行时建表） | 运行时 DDL + 查询（元数据驱动的表结构） |

## 3. 分页/排序/聚合模式（代码约定）

- 分页：`ToPagedListAsync/ToDataTablePageAsync` + `Pagination` 模型（PageResult）——统一分页契约
- 排序：`OrderBy/OrderByIF` 链式（多列：sortCode 升序 + creatorTime 降序 等）
- 聚合：`SqlFunc` 内联（MergeString/Sum 等）在 Select 投影中
- 动态条件：`ConditionalModel/IConditionalModel`（JsonToConditionalModels 反序列化）——**权限条件走序列化契约**（D1.5/P0-B 已锁定）

## 4. 慢查询/索引风险（静态推断）

| 风险 | 依据 |
|------|------|
| base_authorize 全表过滤（ObjectId/ItemType） | 权限链每次查询过滤 + 无 FK；索引 598 个中 Unique 460——普通列索引覆盖待验 |
| f_creator_user_id 反查 | 无 FK/无索引推断（列级索引未逐列验证——NG-1 用 missing index DMV） |
| nvarchar(max) JSON 字段过滤 | f_property_json/f_form_data 等 161 表大字段——若被 WHERE 引用则无法索引 |
| 动态表 mt* | 运行时建表无预建索引 |
| 审计日志双写 | base_sys_log + base_api_log + BASE_AI_CALL_LOG 三个日志族——写放大 |

## 5. Next 决策要点

1. **权限三连查（authorize→module→scheme）必须缓存化或预计算**（权限快照——跨域解耦关键）；
2. **条件注入链四段叠加**是 Next 数据访问架构的核心抽象点（P0-B 规格已是契约输入）；
3. **审计日志族分离**（业务日志/AI 日志/API 日志 → 独立存储候选——Aspire 评估关联）；
4. **运行期热点采样**（DMV/慢查询日志）列入 NG-1 数据库原型阶段，本阶段不臆断索引方案；
5. **Join 用户表反规范化**：Next 列表查询以「审计快照」替代 Join（与 DB-2 §5 一致）。
