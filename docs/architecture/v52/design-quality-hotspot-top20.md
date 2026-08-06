# Hotspot Top20 — 复杂度 × 变更频率

> **父文档**：[`design-quality-diagnostics.md`](design-quality-diagnostics.md)  
> **生成日期**：2026-08-06  
> **数据源**：Codebase-Memory project=`jnpf-v52`（Method.`complexity` / `cognitive`）× `git log --since=2024-01-01` 文件级 commits  
> **公式**：`score = 业务核心度(1..5) × max(commits,1) × 认知复杂度`

---

## 1. Top20（按 score 降序）

| # | score | biz | commits | CC | 认知 | 方法 | 文件（相对 `backend/`） | 有针对性 xUnit? |
|---|------:|----:|--------:|---:|-----:|------|-------------------------|-----------------|
| 1 | 10512 | 2 | 24 | 48 | 219 | `StreamLlmResponseAsync` | `modularity/inteAssistant/JNPF.InteAssistant/AIDevelopmentPipelineService.cs` | 否（仓库 tests 无方法名命中） |
| 2 | 10008 | 3 | 4 | 138 | 834 | `ImportDataAssemble` | `modularity/visualdev/JNPF.VisualDev/VisualDevModelDataService.cs` | 否 |
| 3 | 8370 | 5 | 6 | 45 | 279 | `GetConditionAsync` | `modularity/common/JNPF.Common.Core/Manager/User/UserManager.cs` | 否 |
| 4 | 8370 | 5 | 6 | 45 | 279 | `GetDataConditionAsync` | 同上 | 否 |
| 5 | 7002 | 3 | 3 | 130 | 778 | `ImportDataAssemble` | `modularity/common/JNPF.Common.CodeGen/ExportImport/ExportImportDataHelper.cs` | 否 |
| 6 | 6270 | 5 | 6 | 38 | 209 | `GetCodeGenAuthorizeModuleResource` | `UserManager.cs` | 否 |
| 7 | 6000 | 5 | 8 | 43 | 150 | `Login` | `modularity/oauth/JNPF.OAuth/OAuthService.cs` | 否 |
| 8 | 5790 | 5 | 6 | 37 | 193 | `GetCondition` | `UserManager.cs` | 否 |
| 9 | 5220 | 4 | 3 | 94 | 435 | `GetListQuerySql` | `modularity/visualdev/JNPF.VisualDev/RunService.cs` | 否 |
| 10 | 5184 | 3 | 4 | 84 | 432 | `FuncToMenu` | `modularity/visualdev/JNPF.VisualDev/VisualDevService.cs` | 否 |
| 11 | 4260 | 3 | 2 | 97 | 710 | `GetKeyData` | `modularity/engine/JNPF.VisualDev.Engine/Core/FormDataParsing.cs` | 否 |
| 12 | 3612 | 3 | 4 | 65 | 301 | `GetCDataList` | `VisualDevModelDataService.cs` | 否 |
| 13 | 3588 | 4 | 3 | 45 | 299 | `GenerateFeilds` | `RunService.cs` | 否 |
| 14 | 3108 | 3 | 4 | 63 | 259 | `ImportData` | `modularity/system/JNPF.Systems/System/ModuleService.cs` | 否 |
| 15 | 3060 | 4 | 3 | 80 | 255 | `GetSelector` | `modularity/system/JNPF.Systems/Permission/OrganizeAdministratorService.cs` | 否 |
| 16 | 2052 | 3 | 4 | 40 | 171 | `ImportFirstVerify` | `VisualDevModelDataService.cs` | 否 |
| 17 | 1992 | 4 | 3 | 37 | 166 | `FieldBindDefaultValue` | `RunService.cs` | 否 |
| 18 | 1896 | 4 | 3 | 54 | 158 | `SaveDataToDataByFId` | `RunService.cs` | 否 |
| 19 | 1602 | 3 | 2 | 91 | 267 | `TemplatesDataAggregation` | `modularity/codegen/JNPF.CodeGen/CodeGenService.cs` | 否 |
| 20 | 1512 | 4 | 3 | 53 | 126 | `GetListResult` | `RunService.cs` | 否 |

**读表要点：**

- `#1 StreamLlmResponseAsync`：**变更极频繁**抬高 score，不一定是「最难读」；整改前先确认是否仍在主链热路径（Studio/LLM）。
- `#3–8` 授权/登录簇：业务核心度最高，**应优先补测再动刀**。
- `#9/#18/#20` `RunService`：在线开发列表/保存主路径，与业务验收直接相关。
- 全表「有针对性 xUnit」均为否 → **任何拆分前必须先补测试**（实现完整性铁律）。

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
| 40 | 171 | 3 | `ImportFirstVerify` | VisualDevModelDataService.cs |
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
| 30 | 150 | 3 | `GetSuperQueryInput` | RunService.cs |
| 30 | 146 | 3 | `GetSuperQueryInput` | SuperQueryHelper.cs |
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

**硬伤**：`JNPF → inteAssistant` 双向边（626）= framework 被业务污染。处置设计见 [`design-quality-baseline-gates.md`](design-quality-baseline-gates.md)。

---

## 4. 建议整改波次（仍须用户排期，本文不编码）

| 波次 | 目标 | 入口方法 |
|------|------|----------|
| W1 | 授权/登录带测 | `GetConditionAsync` / `GetDataConditionAsync` / `Login` |
| W2 | 在线开发主路径带测 | `GetListResult` / `SaveDataToDataByFId` / `GetListQuerySql` |
| W3 | 导入装配拆分 | 两处 `ImportDataAssemble`（先对齐重复实现） |
| W4 | 可视化菜单 | `FuncToMenu` |
| W5 | Studio 热变更 | `StreamLlmResponseAsync`（先确认主链职责边界） |

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
