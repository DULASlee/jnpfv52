# ADR-002：开发环境 SSE 必须带 apiUrl 代理前缀

| 字段 | 内容 |
|------|------|
| 状态 | 已接受（路由部分已由 ADR-003 取代） |
| 日期 | 2026-06-18 |
| 决策者 | 架构评审（流水线 SSE 连接超时排查） |

## 背景

2026-06-18，AI 原生开发平台「提交需求」页出现：**后端 LLM 流式完成（1226 chunks），前端却显示「连接超时」**。

### 误诊陷阱（禁止重复）

有人将根因归因为 **`PipelineEngineService` 路由与前端 URL 不匹配**（假设应为 `/api/studio/pipeline/executestage`）。经验证：

| 端点 | 状态 | 说明 |
|------|------|------|
| `POST /api/founder/ai/pipeline/{id}/execute` | 200 | 前端实际路由 ✅ |
| `GET  /api/founder/ai/pipeline/{id}/events` | 200 | SSE 端点 ✅ |
| `POST /api/studio/pipeline/executestage` | 404 | **不存在** ❌ |

**事实：** `PipelineEngineService` 实现 `IPipelineEngine, ISingleton`，**不是** `IDynamicApiController`，无 HTTP 路由。对外 API 在 **`AIDevelopmentPipelineService`**（`[Route("api/founder/ai/pipeline")]`）。

### 真实根因

开发环境 `.env.development`：

- `VITE_GLOB_API_URL=/dev`
- `VITE_PROXY = [["/dev","http://localhost:5000"]]`

| 通道 | 请求 URL | 是否到达 :5000 |
|------|----------|----------------|
| defHttp/axios | `/dev/api/founder/ai/pipeline/...` | ✅ Vite 代理 |
| EventSource（修复前） | `/api/founder/ai/pipeline/.../events` | ❌ 打到 :3100 前端 dev server |

execute 成功触发后端 LLM，SSE 连不到后端 → 收不到 chunk → 超时。

## 决策

1. **所有 EventSource / fetch 裸调用** MUST 通过 `buildEventSourceUrl()`（`jnpf-web-vue3/src/utils/http/sseUrl.ts`）或与 axios 相同的 `useGlobSetting().apiUrl` 前缀规则构建 URL。
2. **禁止** 将 `PipelineEngineService` 改为 HTTP Controller 以「对齐前端 URL」。
3. **排查 SSE 问题时**，优先检查浏览器 Network 中 EventSource 请求 URL 是否含 `/dev`（开发环境），而非假设后端 404。

## 后果

### 正面

- axios 与 SSE 走同一代理路径，开发/生产行为一致。
- 避免误改后端路由、误读 Furion 动态 API 生成规则。

### 负面

- 新增 SSE 入口须记得用 `buildEventSourceUrl`，不能直接 `new EventSource('/api/...')`。

## 相关文件

| 路径 | 说明 |
|------|------|
| `jnpf-web-vue3/src/utils/http/sseUrl.ts` | 统一 SSE URL 构建 |
| `jnpf-web-vue3/src/views/studio/composables/useSSE.ts` | AiChatPanel 使用的 SSE composable |
| `jnpf-web-vue3/src/utils/http/axios/index.ts` | axios apiUrl 拼接逻辑（须保持一致） |
| `jnpf-web-vue3/.env.development` | `VITE_GLOB_API_URL` / `VITE_PROXY` |
| `backend/.../AIDevelopmentPipelineService.cs` | 流水线 HTTP API（非 PipelineEngineService） |

## 前端 ↔ 后端 URL 映射（权威，2026-06-18 更新见 ADR-003）

| 操作 | 路由 |
|------|------|
| 创建流水线 | `POST /api/studio/pipeline/execute/create` |
| 执行阶段 | `POST /api/studio/pipeline/execute/{pipelineId}/execute` |
| SSE 事件流 | `GET /api/studio/pipeline/execute/{pipelineId}/events` |
| 流水线详情 | `GET /api/studio/pipeline/execute/{pipelineId}` |
| 列表 | `GET /api/studio/pipeline/execute/list` |
| 确认阶段 | `POST /api/studio/pipeline/execute/stage/{stageId}/confirm` |
| 回滚 | `POST /api/studio/pipeline/execute/{pipelineId}/rollback` |

> 废止：`api/founder/ai/pipeline/*`（见 ADR-003）

开发环境实际请求路径 = **`/dev` + 上表路径**。
