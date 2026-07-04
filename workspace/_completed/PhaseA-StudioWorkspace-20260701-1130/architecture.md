# Phase A — StudioWorkspace 多用户隔离架构

> **设计文档**：`docs/superpowers/specs/2026-07-01-studio-workspace-isolation-design.md`
> **方案**：B — 轻量静态工具类

---

## 架构决策

### ADR：路径隔离层选用静态工具类而非 DI 服务

**决策**：新建 `StudioWorkspaceHelper` 静态类（零状态、零依赖），而非 `IStudioWorkspaceManager` DI 接口。

**理由**：
- 所有路径计算是纯函数，输入 `(tenantId, pipelineId)` → 输出 `string`，不需要 DI
- 后续如果需要配额管理/数据库跟踪，可低成本升级为 DI 方案 C
- 静态类避免在 `AIDevelopmentPipelineService`（~1500 行）中追加新依赖

### ADR：guard-write.mjs 上下文传递采用文件桥接而非环境变量

**决策**：`AIDevelopmentPipelineService` 写入 `.claude/ai-dev-context.json`，`guard-write.mjs` 读取该文件获取 `pipelineId`。

**理由**：
- Claude Code hook 运行在独立子进程，C# 运行时无法直接注入 env var
- 文件桥接是同一文件系统内的可靠共享机制
- 文件不存在 → 保守回退到默认 Hook 行为（安全优先）

---

## 分层

```
表示层 (API)
  AIDevelopmentPipelineService    ← 调用 StudioWorkspaceHelper
  PipelineOrchestratorService     ← 调用 StudioWorkspaceHelper

业务层 (Logic)
  StudioWorkspaceHelper           ← [NEW] 静态工具类，路径计算+校验+打包

基础设施层 (Infra)
  SandboxManager                  ← 不变，接口零改动
  CodeGenService                  ← 不变，路径零改动

Hook 层 (Guard)
  guard-write.mjs                 ← L4 新增 AI workspace 白名单
```

---

## 模块边界

| 模块 | 可改动 | 说明 |
|------|--------|------|
| `JNPF.InteAssistant` | ✅ | AI 开发模块，新增 `StudioWorkspaceHelper.cs` |
| `JNPF.CodeGen` | ❌ | 传统代码生成，路径不可变 |
| `JNPF.Common.Configuration` | ✅ (+1 行) | `KeyVariable.cs` 加常量 |
| `.claude/hooks` | ✅ | `guard-write.mjs` 加 L4 |

---

## 红线遵守

| 红线 | 状态 | 说明 |
|------|------|------|
| R1 (API Gen) | N/A | 无新 Controller |
| R2 (Unified Response) | N/A | 复用框架自动包装 |
| R3 (Codegen Boundary) | ✅ | 传统 CodeGen 不动 |
| R4 (Multi-Tenant) | ✅ | `tenantId` 来自 `TenantResolver.Resolve()` |
| R5 (Module Boundary) | ✅ | 不改 OA/IoT/MES |
| R6 (Frontend Leak) | N/A | 无前端改动 |
| R7 (SQL Injection) | N/A | 无 SQL |
| R8 (API Permission) | N/A | 无新 API 端点 |
| R9 (Architect Fidelity) | ✅ | 方案 B 与施工包一致 |
| R10 (Bug Discovery) | — | 实施中遵守 |
