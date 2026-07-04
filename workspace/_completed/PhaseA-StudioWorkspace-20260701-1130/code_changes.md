# Phase A — 代码变更记录

> **日期**：2026-07-01
> **方案**：B — 轻量静态工具类

---

## 提交列表

| # | Commit | 描述 |
|---|--------|------|
| 1 | `5db98e7` | feat(A1): add StudioWorkspaceRoot constant to KeyVariable |
| 2 | `b77f571` | feat(A1): add StudioWorkspaceHelper — path calculation, validation, zip, cleanup |
| 3 | `31e4a51` | feat(A2/A4/A5): wire workspace dirs, ai-dev-context, sandbox upload, delivery zip |
| 4 | `945feaa` | feat(A3-s2): cleanup workspace and ai-dev-context on pipeline abandon |
| 5 | `73fb8b1` | feat(A3-s1): add L4 AI workspace whitelist in guard-write hook |
| 6 | `ba2feda` | fix: guard-write L4 use normalizedPath consistently for regex test |

## 文件变更

| 文件 | 操作 | 行数 | 任务 |
|------|------|------|------|
| `KeyVariable.cs` | 修改 | +5 | A1 |
| `StudioWorkspaceHelper.cs` | **新建** | +200 | A1-A5 |
| `AIDevelopmentPipelineService.cs` | 修改 | +88 | A2/A4/A5 |
| `PipelineOrchestratorService.cs` | 修改 | +7 | A3-s2 |
| `guard-write.mjs` | 修改 | +41 | A3-s1 |

**总计：5 文件，+341 行。**

## 未修改（已验证）

- `CodeGenService.cs` — 无变更
- `SandboxManager.cs` — 无变更
- `ISandboxManager.cs` — 无变更

## 需求覆盖

| 需求 | 状态 | 实现位置 |
|------|------|----------|
| A1 常量 + 目录结构 | ✅ 已实现 | `KeyVariable.cs:46` + `StudioWorkspaceHelper.cs:GetPipelinePath/GetPipelineSubPaths/EnsureDirectories` |
| A2 输出切换 | ✅ 已实现 | `AIDevelopmentPipelineService.cs:CreateAsync` → `EnsureDirectories` |
| A3-s1 Hook 白名单 | ✅ 已实现 | `guard-write.mjs` L4 规则 |
| A3-s2 运行时校验 | ✅ 已实现 | `StudioWorkspaceHelper.cs:AssertWithinWorkspace` |
| A4 沙箱绑定 | ✅ 已实现 | `AIDevelopmentPipelineService.cs:StreamLlmResponseAsync` → sandbox upload |
| A5 交付打包 | ✅ 已实现 | `AIDevelopmentPipelineService.cs:GetDeliveryPackageAsync` → `CreateDeliveryZip` |
| 清理 | ✅ 已实现 | `PipelineOrchestratorService.cs:AbandonAsync` → `DeleteWorkspace` + `ClearAiDevContext` |
