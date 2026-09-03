# MCP_CORE_API_GAP — MCP 侧上游 API 缺口总表

> **维护方：** MCP 工作面（V6.1 §16 / §20）。Compiler 每交付一行，MCP 立即按 §20 接入，无需重新设计。
> **更新：** 2026-09-03（P7-04 检查）。状态以本表为准，旧 `.fspm/evidence/baseline/MCP_UPSTREAM_GAP.md` 仍保留为历史快照。

| Capability | Required API | Input | Output | Current State | Owner | Blocking Phase |
|---|---|---|---|---|---|---|
| Compilation | ICompilationProvider（MCP 网关定义，见计划 P7-4） | ResolvedWorkspace + Project | Compilation + CompilationIdentity | MISSING（Core 目录不存在；Compiler 工程仅 csproj 壳） | Compiler/Core | P7（P7.4） |
| Resolve Type | ISemanticResolver.ResolveTypeAsync | SemanticQuery | SemanticRef + INamedTypeSymbol | MISSING | Compiler/Core | P8（MCP-08-03） |
| Resolve Property | ISemanticResolver.ResolvePropertyAsync | SemanticQuery | SemanticRef + IPropertySymbol | MISSING | Compiler/Core | P8（MCP-08-03） |
| Resolve Method | ISemanticResolver.ResolveMethodAsync | SemanticQuery | SemanticRef + IMethodSymbol | MISSING | Compiler/Core | P8（MCP-08-03） |
| Construct | IConstructionService.PlanAsync | SemanticRef + intent | construction plan | MISSING | Core | P9（MCP-09-04） |
| Mutation | ISourceMutationEngine.MutateAsync | plan | changedFiles/diff/txId | MISSING | Core | P10（MCP-10-03） |
| Verify 编排 | IVerificationOrchestrator.ExecuteAsync | VerifyRequest | 分阶段结果 | MISSING | Core | P11（MCP-11-02） |
| Analyzer | 4 Analyzer + FspmRuleIds | Compilation | Diagnostics + Evidence | MISSING | Core/Analyzer | P11 |
| Evidence | IEvidenceStore / IFspmEvidenceCollector | 各阶段产物 | Authoritative Evidence | MISSING（仅 LOCKDOWN 冻结了形状） | Core | P12 |

检查方法（存在性级，未读 Compiler 源码，严守 V3.0 §九）：
`backend/modularity/Foundry.FSPM.Core/` 不存在；`Foundry.FSPM.Compiler/` 仅含 csproj + bin/obj，无 `.cs` 源码（`Get-ChildItem -Recurse -File -Name` 实测）。
