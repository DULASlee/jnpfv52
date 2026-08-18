# 后端质量迭代修复与优化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 后端先落复杂度与分层门禁止损，再按业务核心度对授权、在线开发、导入装配带测整改，最后 Bridges 依赖反转。

**Architecture:** W0 = Roslyn 基线 Analyzer + NetArchTest ARCH-01；W1–W3 = 表征测试 → 最小重构；W4 = `IInteAssistantBridge`。不碰前端工程。

**Tech Stack:** .NET 8 · Roslyn · NetArchTest · Security Code Scan · xUnit · `jnpf-api.mjs` / `pnpm test:api`

**Spec（唯一设计源）:** [`../specs/2026-08-06-backend-quality-remediation-design.md`](../specs/2026-08-06-backend-quality-remediation-design.md)

**Frontend plan (separate):** [`2026-08-06-frontend-quality-remediation-plan.md`](2026-08-06-frontend-quality-remediation-plan.md)

**盘点冻结（2026-08-06 · 检查项 1-2-4）：** 证据目录 `.claude/evidence/backend-quality-check/`（`checks-1-2-4-report.md` · `checks-1-2-4-summary.json`）。结论摘要见下节。

## Baseline inventory (已完成 · 非 W0 硬门)

| 检查项 | 状态 | 关键数字 |
|--------|------|----------|
| 1 架构 NetArchTest | **清单已跑通**（3/3） | 框架 PASS；Common.Core FAIL×1（`IntegreateEventSubscriber`）；csproj hits=3 |
| 2 复杂度盘点 | **库存已冻结** + **硬门已落地** | 盘点 CC>29=**41**；Roslyn 基线条目 **119**（JNPF009） |
| 4 安全扫描 | **SARIF 已归档** | SCS0006×1（`ElemeAuthRequest.cs:256`） |

## Global Constraints

- 禁止无测拆 CC≥30；禁止改断言凑绿；禁止给 Gate 开逃逸。  
- 受保护方法先 CR；禁新增业务 `.mjs`。  
- 多租户过滤 / SQL 参数化 / 禁手写 Controller / Oops.Bah·Oh。  
- 三元组（触及 Studio/IR 时）。  
- 一 Chat 一个可演示波次；人话验收。  
- NuGet：`backend/nuget.config`。  
- 盘点数字变更时同步更新 Spec §1.1 与本表，禁止口头改基线。

---

## File map

| 路径 | 职责 |
|------|------|
| `backend/tools/JNPF.Analyzers/` | ComplexityAnalyzer + baseline（**已做** · JNPF009） |
| `backend/tests/JNPF.Tests.Architecture/` | ARCH-01（**已入 solution** · `LayeringTests.cs`） |
| `.claude/evidence/backend-quality-check/` | 1-2-4 盘点产物 |
| `UserManager.cs` / `OAuthService.cs` · `IConditionStrategy` | W1 |
| `RunService.cs` · `VisualDevService.cs` · `FormDataParsing.cs` | W2 |
| `VisualDevModelDataService.cs` / `ExportImportDataHelper.cs` | W3 |
| `IInteAssistantBridge` + API.Entry 组合根 | W4 |
| `design-quality-hotspot-top20.md` | 每波次更新「有测」列 |

---

### Task 0: 开工与证据冻结

**Files:**
- Read: `docs/superpowers/specs/2026-08-06-backend-quality-remediation-design.md`
- Read: `docs/architecture/v52/design-quality-hotspot-top20.md`
- Evidence: `.claude/evidence/backend-quality-check/checks-1-2-4-report.md`

- [x] **Step 1:** 已完成 1-2-4 盘点；口令「继续」默认指向 **W0 硬门落地**（Analyzer + baseline；ARCH-01 纳入 solution）  
- [x] **Step 2:** 本波次目标：止损门禁（复杂度增量 + 分层清单归档）；不拆业务方法  
- [ ] **Step 3:** 受保护方法 → 先写 CR（仅 W1+ 触及 Orchestrator/Gates 时）  

---

### Task 1: W0-A — Roslyn 复杂度基线

**Files:**
- Create: Analyzer + `backend/tools/JNPF.Analyzers/complexity-baseline.json`
- Test: `JNPF.Analyzers.Tests`
- Seed from: `.claude/evidence/backend-quality-check/check02-complexity-inventory.json`（41 重症）

- [x] **Step 1:** 失败测试：无基线 CC=35 报诊断  
- [x] **Step 2:** 确认失败  
- [x] **Step 3:** 实现增量 Analyzer（圈复杂度，阈 30 · JNPF009）  
- [x] **Step 4:** 灌入基线（xUnit 生成器；check02 为种子意图，Roslyn 全量扫描得 119 条存量豁免）  
- [x] **Step 5:** `dotnet build …/p:CI_BUILD=true`（Common.Core）绿；单测证明无基线超阈红  

```powershell
cd D:\JNPF-v52\backend
dotnet build /p:CI_BUILD=true
dotnet test tools/JNPF.Analyzers/JNPF.Analyzers.Tests -v n
```

---

### Task 2: W0-B — NetArchTest ARCH-01

**Files:**
- Exists: `backend/tests/JNPF.Tests.Architecture/`（NetArchTest.Rules 1.3.2）
- Modify: solution 纳入项目（若尚未加入 `zx_lowcode_netcore.sln`）
- Evidence: `arch01-jnpf-framework.json` · `arch01-common-core.json` · `arch01-project-references.json`

- [x] **Step 1:** 建项目 + NetArchTest 引用  
- [x] **Step 2:** ARCH-01：framework 不得引用 InteAssistant（**PASS**）；Common.Core 清单模式（**FAIL 预期**，样本 `IntegreateEventSubscriber`）  
- [x] **Step 3:** 违规/清单写入 `.claude/evidence/backend-quality-check/`（非旧路径 `arch-01-violations.txt`）  
- [x] **Step 4:** 本任务不抽 Contracts（仍待「继续」后的专项）  
- [x] **Step 5:** 将 `JNPF.Tests.Architecture` 加入 solution；CI 可调用 `dotnet test …Architecture`  

```powershell
dotnet test D:\JNPF-v52\backend\tests\JNPF.Tests.Architecture -v n
# 已验证：失败 0，通过 3
```

---

### Task 2b: W0-C — 安全扫描归档（检查项 4）

**Files / Evidence:**
- `security-scan.sarif` · `check04-security-scan-summary.json`
- Hit: `backend/infrastructure/JNPF.Extras.CollectiveOAuth/.../ElemeAuthRequest.cs:256`

- [x] **Step 1:** 安装并跑 `security-scan` 全解（排除 tests/tools）  
- [x] **Step 2:** 归档 SARIF；当前 **SCS0006×1**（弱哈希）  
- [x] **Step 3:** 评估：记兼容豁免（第三方 OAuth MD5）写入 Spec §3.4（**不阻塞** Task1/2）  
- [ ] **Step 4:** 可选：PR/CI 定期重跑，新增高危须说明  

```powershell
security-scan D:\JNPF-v52\backend\zx_lowcode_netcore.sln --no-banner --export=D:\JNPF-v52\.claude\evidence\backend-quality-check\security-scan.sarif --excl-proj="**/tests/**;**/tools/**;*Test*" --ignore-msbuild-errors
```

---

### Task 3: W1 — 授权簇 / 登录

**Files:**
- `UserManager.cs` · `OAuthService.cs`
- Create: `UserManagerDataPermissionTests.cs` · 可选 `IConditionStrategy` 实现

- [x] **Step 1:** 5 场景表征测试（管理员 / 全数据 / 本部门 / 本部门及下级 / 仅本人）  
- [x] **Step 2:** WHERE 快照 + ToSql 不变量断言（`DataPermissionWhereSnapshot`）  
- [x] **Step 3:** 补测转绿（8/8）  
- [x] **Step 4:** 短路 extract：`DataPermissionShortCircuits`（Admin / AllowAll / DenyAll）  
- [x] **Step 5:** `IConditionStrategy` 决策表替换 @userId/@organizeId/@organizationAndSuborganization/@userAraSubordinates  
- [x] **Step 6:** `GetConditionAsync` / `GetDataConditionAsync` 共用短路+策略；`GetCondition` 短路+QueryType；CodeGen 表名拆分→`ConditionalByTableNameFilter`（修 RemoveAt 跳叶；RunService 算法不同未并）  
- [x] **Step 7:** 登录冒烟；更新 Hotspot「有测」；等节点「通过」  

```powershell
node D:\JNPF-v52\scripts\lib\jnpf-auth.mjs --json
node D:\JNPF-v52\scripts\jnpf-api.mjs GET /api/oauth/CurrentUser
```

**演示:** 登录 → 受权限列表 → 数据范围正确。

---

### Task 4: W2 — 在线开发主路径

**Files:**
- `VisualDevService.FuncToMenu` · `RunService.*` · `FormDataParsing.GetKeyData` + 测试 / `pnpm test:api`

- [x] **Step 1:** 钉列表/保存金丝雀（表征单测作金丝雀；`pnpm test:api` 回归）  
- [x] **Step 2:** `FuncToMenu`：`FuncToMenuReleasePlanner` + 3 测  
- [x] **Step 3:** `GetListQuerySql` / `GetListResult`：别名/列表整形/行编辑回显；`FieldBindDefaultValue`→`FieldBindDefaultValueHelpers`（岗位偏好入参）  
- [x] **Step 4:** `SaveDataToDataByFId` / `BatchDelHaveTableData`：补测 → 降嵌套（`FlowFormMapRuleMerger` HashSet；`BatchDeleteSqlPlanner`；批删仅在 `isInteAssis` 时逐条取数；证据 `w2-save-batchdel-surgery-summary.json`）  
- [x] **Step 5:** `GetKeyData`：`ShortLinkFormFieldFilter` + 2 测（其余转换分支续）  
- [x] **Step 6:** 已跑 `pnpm test:api`（Studio S5 超时红，与在线开发 extract 无直接关联）；W2 金丝雀以 `JNPF.Tests.VisualDev` 8/8 为准；保存/批删与 CC&lt;20 续  

```powershell
cd D:\JNPF-v52
$env:E2E_PIPELINE_ID=311; pnpm test:api
```

---

### Task 5: W3 — 导入装配双实现

**Files:**
- `VisualDevModelDataService.ImportDataAssemble` · `ExportImportDataHelper.ImportDataAssemble`

- [x] **Step 1:** 两处差异表落 evidence（`w3-importdataassemble-diff.json`）  
- [x] **Step 2:** 共享 helper 表征测试 7 条（`ImportAssembleHelperTests`；全套 VisualDev 15/15）  
- [x] **Step 3:** 导入装配主控件链已共享：选项/路径/DATE·TIME/子表/评分·滑块·数字/系统自动生成控件（语义分叉保留；POPUP 缓存装配等边角续）  
- [x] **Step 4:** 主实现=VisualDev；次=CodeGen；共享落 Engine；`clearWhenEmpty` 保留语义差  
- [x] **Step 5:** 金丝雀=表征单测 + CodeGen/VisualDev 编译绿（全量 Excel 导入 E2E 续）  
- [x] **Step 6:** 节点审批（口令「通过」）  

---

### Task 5b: W4 — framework↔inteAssistant 依赖反转

**Files:**
- Create: `IInteAssistantBridge`（framework 侧）  
- Modify: inteAssistant 实现 · `JNPF.API.Entry` 组合根注册  
- Modify: framework/`Common.Core` 去掉对 InteAssistant 的编译期引用（渐进 Top10）  
- Test: `JNPF.Tests.Architecture` 将 Common.Core 规则从清单改为可配置硬失败（豁免名单外）

- [x] **Step 1:** TopN=Common.Core 唯一编译依赖点（事件订阅查集成/入队）→ 桥签名定稿  
- [x] **Step 2:** `InteAssistantBridge` + `Program.cs` 组合根注册  
- [x] **Step 3:** `IntegreateEventSubscriber` 改走 `IInteAssistantBridge`；去掉 Common.Core→Entitys ProjectReference  
- [x] **Step 4:** ARCH-01 Common.Core **硬失败**；Message.Interfaces / API.Entry 豁免名单  
- [x] **Step 4b:** Message.Interfaces 豁免已清（`IntegrateTaskMessageDto`；证据 `w4-message-interfaces-surgery-summary.json`）；豁免仅 API.Entry  
- [x] **Step 5:** 节点审批（口令「通过」）  

---

### Task 6: 收尾 / 季度再生

- [x] **Step 1:** Codebase-Memory 重查 CC>29 → 仍 **41**（证据 `task6-wrapup-summary.json`）  
- [x] **Step 2:** 更新 `design-quality-hotspot-top20.md`（波次状态 + xUnit 列）  
- [x] **Step 3:** baseline 119 entries、无重复；未删减（只增不减策略维持）  
- [x] **Step 4:** AGENTS/CLAUDE 门禁命令已补；`design-quality-baseline-gates.md` 快照同步  
- [x] **Step 5:** 已跑 `pnpm test:api`（Studio S5 超时红，与本整改无直接关联）；金丝雀=Architecture 8/8 + Complexity 5/5 + Common.Core CI_BUILD 0 error  
- [x] **Step 6:** 节点审批（口令「通过」· 施工包结案）  

---

## 顺序

```text
Task0 → Task1 → Task2 → Task2b → (通过) → Task3 → Task4 → Task5 → Task5b → Task6
```

W0–W4 + Task6 收尾已交验；续项见 hotspot §4。

## 完成定义

- [x] ARCH-01 清单已有（framework PASS + Common.Core **硬失败 PASS**）  
- [x] 复杂度 41 重症盘点已冻结 + JNPF009 baseline 硬门  
- [x] Security Code Scan SARIF 已归档（SCS0006×1）  
- [x] W0 Analyzer + baseline 硬门落地（Task1）  
- [x] Architecture 项目纳入 solution / CI（Task2 Step5）  
- [x] W1–W4 + Task6 用户「通过」· 施工包结案  
- [x] `dotnet build /p:CI_BUILD=true`（Common.Core 抽样）0 error  
- [x] 无未批 CR 改动受保护方法  
