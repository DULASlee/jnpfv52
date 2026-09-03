# MCP_COMPILATION_API_GAP — P7.4 BLOCKED 说明

**节点：** MCP-07-04 Compilation Gateway
**状态：** Adapter Contract READY / Implementation BLOCKED
**日期：** 2026-09-03

## 缺什么

`ICompilationProvider` 背后需要的上游能力：由 ResolvedWorkspace + Project 得到真实 Roslyn Compilation（含 CompilationIdentity：Project / AssemblyName / CompilationId / Timestamp）。

## 为什么 BLOCKED

1. `backend/modularity/Foundry.FSPM.Core/` 在本 worktree 不存在（无 SemanticResolver 可调）。
2. `backend/modularity/Foundry.FSPM.Compiler/` 仅有 csproj 壳 + 构建产物，无 `.cs` 源码（未读源码，仅目录级存在性检查）。
3. 按 V6.1-04 与铁律 3，MCP 禁止自建第二套 Compiler，也禁止 `Process.Start("dotnet")` 冒充构建。

## 已完成不受影响

P7.1 Workspace（McpWorkspaceResolver，P6-05）/ P7.2 Solution（SolutionResolver）/ P7.3 Project（ProjectResolver）全部 PASS（见 `.fspm/evidence/p7-workspace/`）。

## 解 Block 条件（V6.1 §20）

Compiler/Core 任一交付可用 Compilation 来源即：Implementation BLOCKED → READY → 接入 `Gateways/ICompilationProvider.cs`（届时新建）→ 跑 P7 Gate → 进 P8。
