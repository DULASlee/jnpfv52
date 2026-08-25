# Backend Structural Audit — Complexity Inventory

**日期**：2026-08-25 ｜ **扫描面**：2276 文件 / 2457 类 / 6627 方法 ｜ **原始数据**：`complexity-inventory.csv`

## 1. 分层统计（CyclomaticComplexityWalker 同源口径）

| 分层 | 阈值 | 数量 | 说明 |
|------|------|-----:|------|
| A 类 | CC≥30 | **111** | 全部受 JNPF009 台账保护（见 §3） |
| B 类 | 20-29 | **110** | D1 同类候选池（无门禁保护，结构观察） |
| C 类 | 15-19 | **145** | 观察层 |
| 全仓均值 | — | 4.79 | 整体健康；尾部集中在少数核心链路 |

## 2. A 类 Top 20（高复杂度热点）

| CC | LOC | Calls | 方法 | 台账 |
|----|-----|-------|------|------|
| 593 | 1669 | 789 | `CodeGenFormControlDesignHelper.FormScriptDesign` | 冻结 |
| 323 | 735 | 373 | `CodeGenFormControlDesignHelper.FormControlDesign` | 冻结 |
| 180 | 695 | 491 | `VisualDevService.FuncToMenu` | 冻结 |
| 160 | 612 | 340 | `FormDataParsing.GetKeyData` | 冻结 |
| 158 | 860 | 330 | `CodeGenService.TemplatesDataAggregation` | 冻结 |
| 151 | 718 | 293 | `CodeGenWay.SingleTableFrontEnd` | 冻结 |
| 113 | 581 | 284 | `RunSqlCompiler.GetListQuerySql` | 冻结 |
| 112 | 402 | 169 | `FormDataParsing.TemplateControlsDataConversion` | 冻结 |
| 109 | 790 | 177 | `InteAssistantRun.GetIntegrateNodeList` | 冻结 |
| 94 | 332 | 191 | `UserManager.GetCodeGenAuthorizeModuleResource` | 冻结 |
| 91 | 455 | 111 | `RequirementAnalysisOrchestrator.RunPmPipelineAsync` | 冻结 |
| 88 | 249 | 172 | `CodeGenControlsAttributeHelper.GetItemRule` | 冻结 |
| 85 | 148 | 169 | `OrganizeAdministratorService.Save` | 冻结 |
| 85 | 403 | 107 | `CodeGenFormControlDesignHelper.FormControlProps` | 冻结 |
| 82 | 209 | 122 | `SuperQueryHelper.GetSuperQueryInput` | 冻结 |
| 81 | 424 | 318 | `VisualDevModelDataService.GetCDataList` | 冻结 |
| 80 | 403 | 310 | `ExportImportDataHelper.GetCDataList` | 冻结 |
| 74 | 279 | 183 | `ModuleService.ImportData` | 冻结 |
| 72 | 304 | 118 | `CodeGenFormControlDesignHelper.FormScriptDesign`（重载） | 冻结 |
| 72 | 543 | 157 | `RunSqlCompiler.GetQueryJson` | 冻结 |

> 111 个 A 类全部在台账（maxComplexity 冻结），**GAP=0**；与 D1 同源的巨型分派/超大方法/隐式契约特征显著（见 refactoring-candidates.md）。

## 3. 台账交叉核对（门禁健康度）

| 项 | 结果 |
|----|------|
| 台账条目 | 119（456e2d6b 初始，D1 5 条已销账） |
| A 类在台账 | **111/111（GAP=0）** |
| 台账孤儿（无对应方法） | **0** |
| 台账内已降级 <30 | 8 条（含重载噪音，如 GetSelector 258→20、GetSuperQueryInput 84→1、FieldBindDefaultValue 82→1）——**可销账观察项（P2，不实施）** |

**结论：JNPF009 门禁覆盖完整、无缺口、无陈旧条目。**（2026-08-25 曾出现 2 个疑似缺口（PortalService.GetList/ScheduleUIMiddleware.InvokeAsync），经探针实测为扫描器假阳性，门禁行为正确。）

## 4. 巨型 switch（≥8 case，Top 20）

| cases | CC | 方法 |
|------:|----:|------|
| 236 | 593 | `CodeGenFormControlDesignHelper.FormScriptDesign` |
| 62 | 112 | `FormDataParsing.TemplateControlsDataConversion` |
| 50 | 109 | `InteAssistantRun.GetIntegrateNodeList` |
| 45 | 85 | `CodeGenFormControlDesignHelper.FormControlProps` |
| 42 | 43 | `DataSyncService.GetDataTypeList` |
| 40 | 48 | `TimeTaskService.Update` |
| 36 | 160 | `FormDataParsing.GetKeyData` |
| 36 | 39 | `CodeGenControlsAttributeHelper.GetWhetherToConvertAllModeControlsIntoLists` |
| 31 | 35 | `SocialsUserService.GetAuthRequest` |
| 29 | 30 | `CodeGenHelper.ConvertDataType` |
| 24 | 65 | `TemplateParsingBase.GetItemRule` |
| 24 | 94 | `UserManager.GetCodeGenAuthorizeModuleResource` |
| 23 | 24 | `CodeGenControlsAttributeHelper.JudgeContainsChildTableControlIsDataConversion` |
| 23 | 24 | `UserManager.GetConditionalModel` |
| 22 | 44 | `VisualDevModelDataService.ImportDataAssemble` |
| 22 | 39 | `ExportImportDataHelper.ImportDataAssemble` |
| 22 | 34 | `CodeGenHelper.CodeGenTemplate` |
| 22 | 37 | `VisualDevModelDataService.TemplateDownload` |
| 22 | 56 | `CodeGenWay.CodeGenFrontEndEngine` |

## 5. 高嵌套（NestingDepth≥7，Top 10）

| nest | CC | 方法 |
|-----:|----:|------|
| 13 | 19 | `FlowTaskMsgUtil.GetMsgContent` |
| 12 | 160 | `FormDataParsing.GetKeyData` |
| 11 | 53 | `RunService.GenerateFeilds` |
| 11 | 113 | `RunSqlCompiler.GetListQuerySql` |
| 10 | 48 | `RunService.GetUpdateSqlByTemplate` |
| 10 | 29 | `RunService.GetChildTableData` |
| 10 | 60 | `UserManager.GetConditionAsync` |
| 10 | 60 | `UserManager.GetDataConditionAsync` |
| 9 | 81 | `VisualDevModelDataService.GetCDataList` |
| 9 | 42 | `UserManager.GetCondition` |
