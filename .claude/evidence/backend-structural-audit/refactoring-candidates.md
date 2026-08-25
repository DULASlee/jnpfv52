# Backend Structural Audit — Refactoring Candidates

**日期**：2026-08-25 ｜ 分级标准见审计规格 §7 ｜ 本登记表只识别不实施

## P0 — S2 前必须处理（3 项）

| ID | 对象 | 问题类型 | 证据 | S2 风险 |
|----|------|---------|------|---------|
| P0-1 | **ORM 直接依赖面**（modularity 403 文件 `using SqlSugar`，占 1549 业务文件 26%；84 文件调 `AsSugarClient`） | P6 数据访问混合（架构级） | 全仓 grep 实测 | S2 抽象边界必须先定（抽象范围/适配器策略/迁移波次），否则 S2 设计无锚点 |
| P0-2 | **数据权限双路径保护缺口**：`UserManager.GetCondition`（CC42，RunService 消费）已有 D1.5 特征；`GetConditionAsync`（CC60）+ `GetDataConditionAsync`（CC60，OrderService 消费，AppendTokenStrategy 独立路径）**零特征测试** | P5 隐式契约无保护 | complexity-inventory + D1 A0 记录 + grep 调用点（RunService.cs 唯一外部消费 GetCondition） | S2 抽象数据权限语义时，未保护路径的枚举数值/条件形态契约可能被破坏 |
| P0-3 | **租户隐式上下文**：ITenantFilter 挂靠仅 12 文件（EntityBase/TenantEntityBase 契约 + DataBaseManager/TenantManager + visualdata 8 实体），租户过滤依赖隐式上下文（AsSugarClient 会话） | P6 租户边界 | grep 实测 12 文件 | S2 抽象时租户语义绝不能丢；不变量清单未固化 |

## P1 — 强烈建议 S2 前处理（6 项）

| ID | 对象 | 问题类型 | 证据 |
|----|------|---------|------|
| P1-1 | `CodeGenFormControlDesignHelper.FormScriptDesign`（CC593/236-case/1669 LOC/789 calls/同文件 117 处 ToJsonString） | P4 巨型 switch + P5 隐式契约（D1 同类最极端样本） | complexity-inventory §2/§4 |
| P1-2 | `FormDataParsing.GetKeyData`（CC160/36-case/612 LOC/340 calls） | P4+P2 | complexity-inventory §2/§4 |
| P1-3 | `RunSqlCompiler.GetListQuerySql`（CC113/581 LOC/284 calls，S2 数据访问核心链路） | P6+P1 | complexity-inventory §2 |
| P1-4 | **巨型 switch 群**（≥8 case 共 20 方法，见 complexity-inventory §4；DataSyncService.GetDataTypeList 42-case、TimeTaskService.Update 40-case、SocialsUserService.GetAuthRequest 31-case 等） | P4 策略分派 | complexity-inventory §4 |
| P1-5 | **B 类 110 个**（CC 20-29，D1 同类候选池，无门禁保护） | P1/P2/P4 混合 | complexity-inventory §1 |
| P1-6 | **God Class 群**（8 个 >2000 行文件：CodeGenFormControlDesignHelper 3757 / RequirementAnalysisOrchestrator 3397 / RunService 2608 / CodeGenWay 2473 / FlowTaskManager 2251 / PmSkillService 2170 / AIDevelopmentPipelineService 2170 / VisualDevService 2121） | P2/P3 单类多职责 | dependency-hotspots.md |

## P2 — 后续技术债（不阻塞 S2）

- C 类 145 个（CC 15-19）观察层
- 台账内已降级 <30 的 8 条（如 GetSelector 258→20、GetSuperQueryInput 84→1、FieldBindDefaultValue 82→1）——可销账观察项（与 D1 相同五步协议，另立战役）
- ToJsonString 热点 10 文件（implicit-contracts.md）
- `UserManager.GetConditionalModel`（23-case switch，条件模型构造分派）
- `FlowTaskMsgUtil.GetMsgContent`（嵌套 13 层/CC19）

## P3 — 优化项（不进本战役）

- `ConfigController.cs`（JNPF.ZxDev）1 处 `$"SELECT` 字符串插值 SQL（参数化健康度整体 99.9%，hook L0 守护；zxdev 非核心模块）
- `ErrorCode.cs`（2142 行枚举文件，非 God Class）
- 其余 6600+ 方法不列

## 登记表字段说明

每项可追溯：文件/方法/CC/LOC/Calls/测试/依赖如上表；禁止「看起来复杂」式结论。
