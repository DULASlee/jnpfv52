# Tasks: backend-arch-code-quality

> **实施计划**（对照 [`design.md`](./design.md)）· **铁律**：S2/S3 每个方法「先补测(红→绿) → 再 extract(绿保绿)」  
> **验收**：每阶段 `dotnet build` + `dotnet test` 全绿，重症方法 CC 下降至阈值

---

## S1 — 静态门禁（防止新增重症）

- [ ] `backend/tools/JNPF.Analyzers/Analyzers/ComplexityAnalyzer.cs`：CC>30 error、CC>20 warning（基于 Roslyn CyclomaticComplexity）
- [ ] 引入 baseline：现有 41 个 CC>30 方法加 `[Suppress("JNPF_Complexity_30")] + TODO`，仅拦新增
- [ ] 新建测试项目 `backend/tests/JNPF.Architecture.Tests/`，引入 ArchUnit.NET
- [ ] `LayerBoundaryTests.cs`：断言 `framework(JNPF)` 不依赖 `inteAssistant`（S4 完成后启用）
- [ ] CI 接入：`dotnet build /p:CI_BUILD=true` 触发 ComplexityAnalyzer；架构测试纳入 `dotnet test`
- [ ] **门禁验证**：故意写一个 CC=31 的测试方法，确认编译失败

## S2 — UserManager 授权簇加固（最高业务风险）

> 源码：`modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs` · 4 方法 CC 37-45

- [ ] **先补测**：`tests/.../UserManagerDataPermissionTests.cs`，5 场景（管理员全权限/全数据/本部门/本部门及下级/仅本人）→ 红
- [ ] **不变量断言**：`ToSql()` 输出 WHERE 子句快照，拆解前后逐字符等价
- [ ] 补测转绿（基于现有实现）
- [ ] `GetConditionAsync` extract 4 方法：`BuildAdminShortCircuit` / `ResolveDataScope` / `AggregateRolePermissions` / `ComposeSqlSugarConditions`（设计 §2.3）
- [ ] 引入 `IConditionStrategy` 决策表，替换 ItemType 的 if/switch 链
- [ ] `GetDataConditionAsync` / `GetCondition` 同模式拆解（三者逻辑高度相似，抽共享私有方法）
- [ ] `GetCodeGenAuthorizeModuleResource` extract method
- [ ] **回归**：5 场景测试 + ToSql 等价断言全绿；CC 全部 < 15
- [ ] 移除 `[Suppress]` 标记

## S3 — 低代码主路径重构

> `RunService` / `VisualDevService` / `VisualDevModelDataService`

- [ ] `VisualDevService.FuncToMenu` (CC84)：补测 → 按菜单组装步骤 extract
- [ ] `VisualDevModelDataService.ImportDataAssemble` (CC138/认知834)：补测 → 按「解析→校验→映射→写入」4 阶段拆
- [ ] `RunService.GetListQuerySql` (CC94/认知435)：补测 → SQL 装配 extract
- [ ] `RunService.SaveDataToDataByFId` (嵌套38)：补测 → 拆嵌套（linear_scan_in_loop=11 隐患）
- [ ] `RunService.GetListResult` (CC53)：补测 → extract
- [ ] `RunService.BatchDelHaveTableData` (嵌套37)：补测 → 拆嵌套
- [ ] `FormDataParsing.GetKeyData` (CC97)：补测 → extract
- [ ] **回归**：`dotnet test` 全绿；所有拆解方法 CC < 20

## S4 — framework↔inteAssistant 依赖反转

- [ ] 据图 `framework→inteAssistant` 调用 Top10，定义 `IInteAssistantBridge` 接口（framework 侧）
- [ ] inteAssistant 实现 `IInteAssistantBridge`
- [ ] `application` 组合根（`JNPF.API.Entry/Startup`）注册桥实现
- [ ] framework 内 `using JNPF.InteAssistant` → `using JNPF.Bridges`
- [ ] 启用 ArchUnit `LayerBoundaryTests`（S1 预备的），确认 `framework` 无 inteAssistant 依赖
- [ ] **回归**：`dotnet build` + 全量 `dotnet test` + 图 `boundaries` 复扫确认双向边下降

## S5 — 收尾

- [ ] 更新 `CLAUDE.md` / `AGENTS.md`：记录 ComplexityAnalyzer 门禁 + ArchUnit 约束
- [ ] 更新 [`design-quality-hotspot-top20.md`](../../docs/architecture/v52/design-quality-hotspot-top20.md) 复扫数据
- [ ] Codebase-Memory 重新索引，确认 41 重症方法 CC 全部下降
- [ ] 全链冒烟：`E2E_PIPELINE_ID=311 pnpm test:api`
