# Phase A — AI 工作区多用户多任务并行隔离

> **施工包**：Phase A 施工包清单
> **方案**：B — 轻量静态工具类 `StudioWorkspaceHelper`
> **设计文档**：`docs/superpowers/specs/2026-07-01-studio-workspace-isolation-design.md`
> **创建日期**：2026-07-01

---

## 任务摘要

在不动主仓库核心结构的前提下，补齐 AI 原生开发平台的多用户、多任务并行隔离能力。所有 AI 开发产物落入 `{SystemPath}/StudioWorkspace/{tenantId}/{pipelineId}/`，沙箱挂接完整生命周期，交付时打包 zip 供手动集成。

---

## 任务分级：S 级

**理由**：5+ 文件改动，涉及 Hook 层/C# 静态类/Service 层三层，有安全相关改动（路径穿越防御、Hook 白名单），需 E2E 沙箱验证。

---

## 子任务

| # | 任务 | 文件 | 动作 |
|---|------|------|------|
| A1 | 定义 AI 工作区根路径常量与目录结构 | `KeyVariable.cs` + `StudioWorkspaceHelper.cs`（新建） | +1 常量 + ~120 行新文件 |
| A2 | AI 流水线代码生成输出切换到工作区 | `AIDevelopmentPipelineService.cs` | ~30 行改动 |
| A3-s1 | 设计时 Hook 白名单（guard-write.mjs L4） | `.claude/hooks/guard-write.mjs` | ~30 行新增规则 |
| A3-s2 | 运行时路径校验（AssertWithinWorkspace） | `StudioWorkspaceHelper.cs` + `AIDevelopmentPipelineService.cs` | 同 A1/A2 |
| A4 | 沙箱生命周期与流水线阶段绑定 | `AIDevelopmentPipelineService.cs` | 调适配层 → SandboxManager |
| A5 | 交付闸门：工作区打包 zip | `StudioWorkspaceHelper.cs` + `AIDevelopmentPipelineService.cs` | 同 A1/A2 |
| Cleanup | 放弃/异常时清理工作区 | `PipelineOrchestratorService.cs` | ~10 行 |

---

## 约束

1. 传统 `CodeGenerate/` 路径零改动
2. `SandboxManager` 接口零改动
3. `StudioWorkspaceHelper` 保持纯函数、零状态、零 DI
4. `AssertWithinWorkspace` 用 `Path.GetFullPath` 防路径穿越
5. 清理逻辑异常安全：删除失败不阻塞放弃流程

---

## 验收标准

- [ ] `dotnet build` 零错误
- [ ] 创建流水线 → development → `generated/` 目录生成 → 沙箱上传成功
- [ ] 交付：`generated/` 打包 zip → 前端下载
- [ ] 放弃：沙箱销毁 + workspace 目录清理
- [ ] 传统 VisualDev 代码生成路径不受影响
- [ ] `guard-write.mjs` L4 规则在有/无 ai-dev-context.json 时行为正确
