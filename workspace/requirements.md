# Phase B — AI 前端预览与沙箱增强

> **分支**：`frontend-architecture-refactor`
> **前置依赖**：Phase A (StudioWorkspace) + S1 (JwtHandler) 已完成
> **创建日期**：2026-07-01

---

## 任务摘要

Phase A 实现了代码生成隔离。Phase B 让用户能在沙箱中**实时预览** AI 生成的前端页面，并补齐沙箱并发调度的排队机制。

**原则**：只加层不拆房。不修改 `jnpf-web-vue3` 主前端工程，一切预览发生在独立壳工程 + Docker 沙箱内。

---

## 任务分级：S 级

**理由**：新建独立前端工程 + 修改 SandboxManager + 修改 AIDevelopmentPipelineService + 新增 SSE 事件类型，涉及前后端协同。

---

## 子任务

| # | 任务 | 描述 |
|---|------|------|
| B1 | AI 前端产物独立预览工程 | 新建 `studio-preview/` 壳工程 + 文件注入 + 沙箱构建 + SSE 推送预览 URL |
| B2 | 沙箱并发队列与排队 UI | ConcurrentQueue + 后台调度循环 + `sandbox_queued`/`queue_position` 事件 |
| B3 | Git Worktree 支持（可选） | `InitGitRepo()` + `CreateWorktree()` |

---

## 约束

1. `studio-preview/` 不进主仓库构建链路
2. `SandboxManager` 已有接口不改签名
3. 所有预览流量走 Docker 容器端口，不污染主前端开发服务器
4. B3 可选，B1/B2 必须

---

## 验收标准

- [ ] `studio-preview/` 独立 `npm install && npm run dev` 可启动
- [ ] AI 生成的 `.vue` 文件注入壳工程后可预览
- [ ] 沙箱 npm 执行不超时
- [ ] 预览 URL 通过 SSE 推送到前端
- [ ] 超并发时排队而非报错
- [ ] `dotnet build` 零错误
