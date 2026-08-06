# Change: backend-arch-code-quality

## Why

后端代码全量扫描（7682 个 C# 方法）显示：**96.9% 的方法健康（CC<10），但 41 个重症方法（CC≥30，认知复杂度最高 834）撑起了整个系统的可维护性风险**。问题不是「代码烂」，而是「局部重症 + 结构性耦合」：

- **41 个上帝方法**：`ImportDataAssemble`(CC 138/认知 834)、`UserManager.GetConditionAsync`(CC 45/认知 279，授权核心)、`FuncToMenu`(CC 84/认知 432)——每改一处需求都要踩雷，且**全部无针对性 xUnit 覆盖**。
- **分层违规**：`framework(JNPF) ↔ inteAssistant` 双向调用 1427 次——核心层反向依赖业务模块，改 framework 动全身。
- **无架构约束测试**：`JNPF.Analyzers` 只拦行为红线（租户/SQL/权限），**没有复杂度/分层门禁**，重症方法仍在持续产生。

这导致的结果：业务核心（授权 `UserManager`、在线开发 `RunService`、可视化装配 `VisualDevService`）成为「改一处崩三处」的雷区，迭代速度被技术债拖累。

## What

分四个阶段，从「防止新增」到「清除存量」：

1. **静态门禁**（防止新增重症）：扩展 `JNPF.Analyzers`，新增复杂度门禁（CC>30 编译失败）+ ArchUnit.NET 架构测试（禁止跨层双向依赖）；接入 CI（`dotnet build /p:CI_BUILD=true`）。
2. **授权簇加固**（最高业务风险优先）：为 `UserManager` 4 个数据权限方法（`GetConditionAsync`/`GetDataConditionAsync`/`GetCondition`/`GetCodeGenAuthorizeModuleResource`，CC 37-45）**先补 xUnit → 再 extract method 拆解**，目标 CC<15。这是多租户数据隔离的核心，零测试拆分 = 数据越权事故。
3. **低代码主路径重构**：`VisualDevService.FuncToMenu`(CC84)、`RunService.SaveDataToDataByFId`(嵌套38)/`GetListResult`(CC53)/`BatchDelHaveTableData`(嵌套37)、`VisualDevModelDataService.ImportDataAssemble`(CC138)——在线开发列表/保存/导入主路径，逐个补测+拆解。
4. **分层解耦**：`framework↔inteAssistant` 反转依赖——framework 定义 `IInteAssistantBridge` 接口，inteAssistant 实现，切断 framework 反向依赖。

## Scope

| 纳入 | 排除 |
|------|------|
| `JNPF.Analyzers` 新增 ComplexityAnalyzer + LayerBoundaryAnalyzer | 重写 `JNPF.Analyzers` 既有行为红线 |
| `UserManager` 4 方法补测 + 拆解 | `UserManager` 数据权限算法逻辑变更（行为不变，仅结构拆分） |
| `RunService`/`VisualDevService`/`VisualDevModelDataService` Top 方法补测+拆解 | 这些 Service 的 API 契约变更（零改动） |
| ArchUnit.NET 架构测试（framework 不依赖业务模块） | 重构 framework 核心机制（DI/SqlSugar/JWT） |
| `framework↔inteAssistant` 依赖反转（抽 `IInteAssistantBridge`） | inteAssistant 业务逻辑变更 |
| 全量回归：`dotnet build` + `dotnet test` | 性能基准改造 |

## 数据锚定（诊断证据）

| 维度 | 数值 | 来源 |
|------|------|------|
| 重症方法（CC≥30） | **41 个**（其中 CC≥50 的 15 个） | [`design-quality-hotspot-top20.md`](../../docs/architecture/v52/design-quality-hotspot-top20.md) |
| 最高认知复杂度 | **834**（`ImportDataAssemble`） | 同上 |
| framework↔inteAssistant 双向调用 | **1427 次**（801+626） | Codebase-Memory `boundaries` |
| 重症方法 xUnit 覆盖 | **0/41**（全部无针对性测试） | hotspot 报告「有针对性 xUnit?」列 |
| 健康方法占比 | 96.9%（7441/7682 CC<10） | Codebase-Memory 复杂度分布 |

## Status

- [x] 诊断完成（[`design-quality-hotspot-top20.md`](../../docs/architecture/v52/design-quality-hotspot-top20.md) + [`design-quality-diagnostics.md`](../../docs/architecture/v52/design-quality-diagnostics.md)）
- [ ] spec 草稿（本 proposal + `design.md` + `tasks.md`）
- [ ] 用户审批
- [ ] 实施

## 关联

- 诊断报告：[`docs/architecture/v52/design-quality-hotspot-top20.md`](../../docs/architecture/v52/design-quality-hotspot-top20.md)
- 方法体系：[`docs/architecture/v52/design-quality-diagnostics.md`](../../docs/architecture/v52/design-quality-diagnostics.md)
- 前端对照：[`../frontend-arch-code-quality/proposal.md`](../frontend-arch-code-quality/proposal.md)
- 铁律：实现完整性铁律（无测试禁拆 CC≥30）· 全链条冲刺铁律 F1-F4
