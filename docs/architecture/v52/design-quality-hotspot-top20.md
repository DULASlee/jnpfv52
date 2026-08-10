# Hotspot Top20 — 复杂度 × 变更频率

> **父文档**：[`design-quality-diagnostics.md`](design-quality-diagnostics.md)  
> **后端整改**：[`../../superpowers/specs/2026-08-06-backend-quality-remediation-design.md`](../../superpowers/specs/2026-08-06-backend-quality-remediation-design.md) · [`../../superpowers/plans/2026-08-06-backend-quality-remediation-plan.md`](../../superpowers/plans/2026-08-06-backend-quality-remediation-plan.md)  
> **生成日期**：2026-08-06 · **再生复核**：2026-08-07（Task6）  
> **数据源**：Codebase-Memory project=`jnpf-v52`（Method.`complexity` / `cognitive`）× `git log --since=2024-01-01` 文件级 commits  
> **公式**：`score = 业务核心度(1..5) × max(commits,1) × 认知复杂度`  
> **2026-08-07 复核**：CC&gt;29 仍 **41**（图索引未因 W1–W4 extract 重算方法体 CC）；Top 文件 commits 未变；W1–W4 补测/桥已落地见下表「有针对性 xUnit」列

---

## 1. Top20（按 score 降序）

| # | score | biz | commits | CC | 认知 | 方法 | 文件（相对 `backend/`） | 有针对性 xUnit? |
|---|------:|----:|--------:|---:|-----:|------|-------------------------|-----------------|
| 1 | 10512 | 2 | 24 | 48 | 219 | `StreamLlmResponseAsync` | `modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs` | 部分（续 · FlowHelpers + LegacyGatePlanner） |
| 2 | 10008 | 3 | 4 | 138 | 834 | `ImportDataAssemble` | `modularity/visualdev/JNPF.VisualDev/VisualDevModelDataService.cs` | 部分（W3 · 映射 helper 表征测） |
| 3 | 8370 | 5 | 6 | 45 | 279 | `GetConditionAsync` | `modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs` | 是（W1 · `JNPF.Tests.Common`） |
| 4 | 8370 | 5 | 6 | 45 | 279 | `GetDataConditionAsync` | 同上 | 是（W1 · 同测） |
| 5 | 7002 | 3 | 3 | 130 | 778 | `ImportDataAssemble` | `modularity/common/JNPF.Common.CodeGen/ExportImport/ExportImportDataHelper.cs` | 部分（W3 · 同 helper） |
| 6 | 6270 | 5 | 6 | 38 | 209 | `GetCodeGenAuthorizeModuleResource` | `UserManager.cs` | 部分（W1 · 表名拆分测） |
| 7 | 6000 | 5 | 8 | 43 | 150 | `Login` | `modularity/oauth/JNPF.OAuth/OAuthService.cs` | 部分（W-oauth · LoginFlowHelpers） |
| 8 | 5790 | 5 | 6 | 37 | 193 | `GetCondition` | `UserManager.cs` | 部分（W1 · 短路+QueryType 子句测） |
| 9 | 5220 | 4 | 3 | 94 | 435 | `GetListQuerySql` | `modularity/visualdev/JNPF.VisualDev/RunService.cs` | 部分（W2+ · FieldAlias + Fragment/Projection helpers） |
| 10 | 5184 | 3 | 4 | 84 | 432 | `FuncToMenu` | `modularity/visualdev/JNPF.VisualDev/VisualDevService.cs` | 是（W2 · 发布目标组装） |
| 11 | 4260 | 3 | 2 | 97 | 710 | `GetKeyData` | `modularity/engine/JNPF.VisualDev.Engine/Core/FormDataParsing.cs` | 部分（W2 · 短链字段过滤） |
| 12 | 3612 | 3 | 4 | 65 | 301 | `GetCDataList` | `VisualDevModelDataService.cs` | 部分（W3+ · 地址/组织/IdEncode 缓存） |
| 13 | 3588 | 4 | 3 | 45 | 299 | `GenerateFeilds` | `RunService.cs` | 部分（W2 · SystemFieldGenerateHelpers） |
| 14 | 3108 | 3 | 4 | 63 | 259 | `ImportData` | `modularity/system/JNPF.Systems/System/ModuleService.cs` | 部分（W-systems · ModuleImportHelpers） |
| 15 | 3060 | 4 | 3 | 80 | 255 | `GetSelector` | `modularity/system/JNPF.Systems/Permission/OrganizeAdministratorService.cs` | 部分（W-systems · OrganizeAdminSelectorHelpers） |
| 16 | 2052 | 3 | 4 | 40 | 171 | `ImportFirstVerify` | `VisualDevModelDataService.cs` | 部分（W3+ · 内存必填/批次唯一） |
| 17 | 1992 | 4 | 3 | 37 | 166 | `FieldBindDefaultValue` | `RunService.cs` | 是（W2 · FieldBindDefaultValueHelpers） |
| 18 | 1896 | 4 | 3 | 54 | 158 | `SaveDataToDataByFId` | `RunService.cs` | 部分（W2+ · FlowFormDataMapper 内存映射） |
| 19 | 1602 | 3 | 2 | 91 | 267 | `TemplatesDataAggregation` | `modularity/codegen/JNPF.CodeGen/CodeGenService.cs` | 部分（W-codegen · TemplatesDataAggregationHelpers） |
| 20 | 1512 | 4 | 3 | 53 | 126 | `GetListResult` | `RunService.cs` | 部分（W2+ · Shape + QueryInput helpers） |

**读表要点：**

- `#1 StreamLlmResponseAsync`：**变更极频繁**抬高 score，不一定是「最难读」；整改前先确认是否仍在主链热路径（Studio/LLM）。
- `#3–4` 授权簇：W1 已补测并短路/策略拆分；`GetCondition` 短路+QueryType；CodeGen 表名拆分已抽并修跳叶；RunService 同名私有方法语义不同续。
- `#9/#18/#20` `RunService`：W2 已抽部分；保存/批删 + 列表后处理 + 行编辑回显 + 默认值绑定 + **系统字段生成**（`SystemFieldGenerateHelpers`）已落地。
- `#2/#5` 导入装配：主控件链（映射/日期/路径/子表/数值/系统自动生成）已共享抽离；方法体 CC 待重索引后复测；POPUP 缓存装配等边角续。
- 未标「是」的入口：**拆分前必须先补测试**（实现完整性铁律）。

---

## 2. 重症方法全集（CC > 29，41 个）

来自 Codebase-Memory（查询：`WHERE m.complexity > 29`，project=`jnpf-v52`，2026-08-06）：

| CC | 认知 | loop_depth | 方法 | 文件 |
|---:|-----:|-----------:|------|------|
| 138 | 834 | 3 | `ImportDataAssemble` | VisualDevModelDataService.cs |
| 130 | 778 | 3 | `ImportDataAssemble` | ExportImportDataHelper.cs |
| 97 | 710 | 4 | `GetKeyData` | FormDataParsing.cs |
| 94 | 435 | 4 | `GetListQuerySql` | RunService.cs |
| 91 | 267 | 2 | `TemplatesDataAggregation` | CodeGenService.cs |
| 84 | 432 | 3 | `FuncToMenu` | VisualDevService.cs |
| 80 | 255 | 1 | `GetSelector` | OrganizeAdministratorService.cs |
| 72 | 320 | 3 | `GetIntegrateNodeList` | InteAssistantRun.cs |
| 65 | 301 | 2 | `GetCDataList` | VisualDevModelDataService.cs |
| 64 | 302 | 2 | `GetCDataList` | ExportImportDataHelper.cs |
| 63 | 259 | 2 | `ImportData` | ModuleService.cs |
| 61 | 237 | 0 | `TemplateControlsDataConversion` | FormDataParsing.cs |
| 54 | 158 | 2 | `SaveDataToDataByFId` | RunService.cs |
| 53 | 191 | 2 | `SingleTableFrontEnd` | CodeGenWay.cs |
| 53 | 126 | 1 | `GetListResult` | RunService.cs |
| 48 | 219 | 1 | `StreamLlmResponseAsync` | AIDevelopmentPipelineService.cs |
| 45 | 299 | 3 | `GenerateFeilds` | RunService.cs |
| 45 | 279 | 5 | `GetConditionAsync` | UserManager.cs |
| 45 | 279 | 5 | `GetDataConditionAsync` | UserManager.cs |
| 44 | 170 | 2 | `GetVisualDevCaCheData` | FormDataParsing.cs |
| 43 | 150 | 1 | `Login` | OAuthService.cs |
| 41 | 149 | 2 | `ExportMemoryStream` | ExcelExportHelper.cs |
| 40 | 171 | 3 | `ImportFirstVerify` | VisualDevModelDataService.cs（W3+ 内存初验已抽） |
| 38 | 209 | 4 | `GetCodeGenAuthorizeModuleResource` | UserManager.cs |
| 37 | 193 | 5 | `GetCondition` | UserManager.cs |
| 37 | 166 | 2 | `FieldBindDefaultValue` | RunService.cs |
| 37 | 159 | 3 | `ImportFirstVerify` | ExportImportDataHelper.cs |
| 36 | 131 | 2 | `GetImportPreviewData` | ExportImportDataHelper.cs |
| 36 | 130 | 2 | `ImportPreview` | VisualDevModelDataService.cs |
| 36 | 103 | 1 | `GetCurrentUser` | OAuthService.cs |
| 35 | 131 | 2 | `GetItemRule` | CodeGenControlsAttributeHelper.cs |
| 34 | 136 | 3 | `GetCreateFirstColumnsHeader` | VisualDevModelDataService.cs |
| 34 | 132 | 3 | `GetTargetForm` | InteAssistantRun.cs |
| 33 | 177 | 3 | `GetParsDataByList` | ControlParsing.cs |
| 32 | 78 | 3 | `GetListChildTable` | RunService.cs |
| 31 | 72 | 2 | `SyncPortal` | PortalService.cs |
| 31 | 54 | 2 | `ImportUserData` | UsersService.cs |
| 30 | 150 | 3 | `GetSuperQueryInput` | RunService.cs（W2+ ListSuperQueryInputRewriter；勿并 SuperQueryHelper） |
| 30 | 146 | 3 | `GetSuperQueryInput` | SuperQueryHelper.cs（CodeGen 分叉保留） |
| 30 | 104 | 3 | `GetCreateFirstColumnsHeader` | ExportImportDataHelper.cs |
| 30 | 61 | 3 | `GetCreateSqlByTemplate` | RunService.cs |

---

## 3. 分层违例快照（配套结构债）

`get_architecture(aspects=['boundaries'])` on `jnpf-v52`（同日）：

| From | To | call_count |
|------|-----|------------:|
| inteAssistant | JNPF | 801 |
| system | common | 717 |
| system | JNPF | 678 |
| **JNPF** | **inteAssistant** | **626** |
| workflow | common | 370 |
| inteAssistant | common | 319 |
| system | inteAssistant | 222 |
| workflow | JNPF | 218 |
| inteAssistant | workflow | 199 |

**硬伤**：图关系 `framework ↔ inteAssistant` 仍在；**编译期** `Common.Core → InteAssistant.Entitys` 已由 W4 桥切断（ARCH-01 硬失败）。`Message.Interfaces` 已清（`IntegrateTaskMessageDto`）；豁免仅剩 API.Entry 组合根。处置见 [`design-quality-baseline-gates.md`](design-quality-baseline-gates.md)。

---

## 4. 整改波次状态（2026-08-07）

| 波次 | 目标 | 状态 |
|------|------|------|
| W0 | JNPF009 + baseline + ARCH-01 入 solution | **已通过** |
| W1 | 授权簇短路/策略 + 表征测 | **已通过**；CodeGen 表名过滤 + RunService 独立过滤；列表高级查询改写 2026-08-07（证据 w2-superquery-input-rewriter-surgery-summary.json） |
| W2 | 在线开发主路径 extract + 测 | **已通过**；ChildTable + SuperQuery + FlowFormDataMapper + GetListResult QueryInput 2026-08-07（证据 w2-list-query-input-helpers-surgery-summary.json） |
| W3 | 导入装配映射共享 | **已通过**；主控件链 + POPUP + ImportFirstVerify + GetCDataList 地址/组织缓存 2026-08-07（证据 w3-import-address-cache-surgery-summary.json） |
| W4 | `IInteAssistantBridge` + Common.Core ARCH-01 硬失败 | **已通过**；Message.Interfaces 豁免已清（2026-08-07） |
| R | 季度再生本表 + 基线只增不减审计 | **已通过** |
| W-oauth/W-systems/W-codegen | Top20 剩余否清零 + GetListQuerySql 加深 | **已通过**（2026-08-08；证据 w-oauth-login / w2-list-query-sql-fragments / w-systems-* / w-codegen-templates-aggregation） |
| 续 | Studio 热路径 `StreamLlmResponseAsync` | **切片交验**：FlowHelpers + GatePlanner + Attachment + post-gate（视觉决策/默认流请求/预算·沙箱门）；下载 HTTP/DB/解析器仍在调用点；哈希·缓存·Bearer·超时·状态载荷已抽（证据 w-continue-streamllm-attachment-status-summary.json） |

---

## 5. 再生步骤（禁止新增 .mjs；用本机命令）

在仓库根执行 PowerShell（候选列表可按第 2 节扩展）：

```powershell
# 1) 从 Codebase-Memory 刷新 CC/认知（Cursor MCP query_graph）
#    MATCH (m:Method) WHERE m.complexity > 29 RETURN ...

# 2) 对每个候选文件统计 commits
git log --since='2024-01-01' --oneline -- backend/path/ToFile.cs | Measure-Object

# 3) score = biz * max(commits,1) * cognitive
# 4) 覆写本文件 Top20 表；更新日期
```

业务核心度字典见父文档 §3。

---

## 6. 本节关键代码路径索引

- `backend/modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs`
- `backend/modularity/visualdev/JNPF.VisualDev/RunService.cs`
- `backend/modularity/visualdev/JNPF.VisualDev/VisualDevService.cs`
- `backend/modularity/oauth/JNPF.OAuth/OAuthService.cs`
- `backend/modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs`
