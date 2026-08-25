# Backend Structural Audit — Dependency Hotspots

**日期**：2026-08-25 ｜ 方法：complexity-inventory.csv（Calls=方法内调用数）+ 文件行数实测

## 1. 高扇出方法 Top 12（方法内 Invocation 数）

| Calls | CC | LOC | 方法 | 模块 |
|------:|----:|----:|------|------|
| 789 | 593 | 1669 | `CodeGenFormControlDesignHelper.FormScriptDesign` | engine（代码生成） |
| 491 | 180 | 695 | `VisualDevService.FuncToMenu` | visualdev |
| 373 | 323 | 735 | `CodeGenFormControlDesignHelper.FormControlDesign` | engine |
| 340 | 160 | 612 | `FormDataParsing.GetKeyData` | engine |
| 330 | 158 | 860 | `CodeGenService.TemplatesDataAggregation` | codegen |
| 318 | 81 | 424 | `VisualDevModelDataService.GetCDataList` | visualdev |
| 310 | 80 | 403 | `ExportImportDataHelper.GetCDataList` | common.codegen |
| 293 | 151 | 718 | `CodeGenWay.SingleTableFrontEnd` | engine |
| 284 | 113 | 581 | `RunSqlCompiler.GetListQuerySql` | visualdev（S2 核心） |
| 266 | 67 | 278 | `OAuthService.GetCurrentUser` | oauth |
| 215 | 61 | 328 | `FormDataParsing.GetVisualDevCaCheData` | engine |
| 197 | 62 | 285 | `UsersImportExportService.ImportUserData` | system |

> Fan-out 语义注：本表 Calls 为方法体内调用点计数（语法级实测）；Fan-in（被引用数）需语义级统计，标记 `[PARTIAL]`——以文件行数+调用点组合近似，精确 Fan-in 留给后续语义扫描。

## 2. God Class 候选（文件行数 Top 10，modularity）

| 行数 | 文件 | 职责维度 | 判断 |
|-----:|------|---------|------|
| 3757 | `engine/…/CodeGenFormControlDesignHelper.cs` | 控件设计/脚本生成/转换（117 ToJsonString） | **职责混杂（P1-1 关联）** |
| 3397 | `inteAssistant/…/RequirementAnalysisOrchestrator.cs` | 需求分析编排（91-case 方法在内） | 职责混杂（观察） |
| 2608 | `visualdev/…/RunService.cs` | 列表/详情/保存/数据权限/字段（多子链） | 职责混杂（观察；含 D1 已拆分调用点） |
| 2473 | `engine/…/CodeGenWay.cs` | 代码生成（多形态） | 职责混杂（观察） |
| 2251 | `workflow/…/FlowTaskManager.cs` | 流程任务管理（多方法高 CC） | 职责混杂（观察） |
| 2170 | `inteAssistant/…/PmSkillService.cs` | 技能服务 | 观察（CR 流程受保护） |
| 2170 | `inteAssistant/…/AIDevelopmentPipelineService.cs` | 管线服务 | 观察 |
| 2121 | `visualdev/…/VisualDevService.cs` | 模型服务（FuncToMenu CC180） | 职责混杂（观察） |
| 2074 | `visualdev/…/VisualDevModelDataService.cs` | 数据服务（ImportDataAssemble CC44） | 观察 |

> 原则：大类 ≠ 必须重构（规格 §十二）；仅登记职责混杂证据，重构决策由人工按行为保护/测试难度/S2 影响裁决。

## 3. 跨层/反向依赖

- ARCH01 系列测试已守护：framework→inteAssistant 禁边、Common.Core→inteAssistant 禁边、Common.Core 豁免清单——92/92 绿（Gate G 通过依据）
- 已知历史：Common.Core→模块 Entitys 反向依赖（登记于既有审计，不在本战役范围）

## 4. 层交叉标记

| 位置 | 类型 | 说明 |
|------|------|------|
| `RunSqlCompiler`（visualdev/Runtime） | Service 直接构造 SqlSugar 查询 | S2 核心目标（P1-3） |
| `DataBaseManager.GetTenantSqlSugarClient`（CC29） | Common.Core 会话工厂 | 租户过滤生效点（P0-3） |
