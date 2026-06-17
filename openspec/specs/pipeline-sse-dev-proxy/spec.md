# pipeline-sse-dev-proxy Specification

## Purpose
TBD - created by archiving change add-pipeline-sse-dev-proxy. Update Purpose after archive.
## Requirements
### Requirement: EventSource URL MUST 与 axios 共享 apiUrl 前缀

开发环境下，JNPF PC 前端（jnpf-web-vue3）的 EventSource URL 构建 MUST 与 defHttp/axios 使用相同的 `VITE_GLOB_API_URL` 前缀规则。实现 MUST 通过 `buildEventSourceUrl()`（`src/utils/http/sseUrl.ts`），逻辑 MUST 与 `src/utils/http/axios/index.ts` 中 `beforeRequestHook` 的 `apiUrl` 拼接一致。

#### Scenario: 开发环境 SSE 经 Vite 代理到达后端

- **WHEN** 前端订阅 `GET /api/studio/pipeline/execute/{pipelineId}/events` 且 `VITE_GLOB_API_URL=/dev`
- **THEN** 浏览器实际请求 URL MUST 为 `/dev/api/studio/pipeline/execute/{pipelineId}/events`
- **AND** Vite 代理 MUST 将请求转发至 `http://localhost:5000`

#### Scenario: 禁止裸写 EventSource 路径

- **WHEN** 开发者新建 SSE 连接
- **THEN** MUST NOT 使用 `new EventSource('/api/...')` 而不加 apiUrl 前缀
- **AND** MUST 使用 `buildEventSourceUrl('/api/...')`

### Requirement: 流水线 HTTP API 入口 MUST 为 AIDevelopmentPipelineService

排查 SSE 或流水线 API 路由时，MUST 以 `AIDevelopmentPipelineService`（`[Route("api/studio/pipeline/execute")]`）为 HTTP 入口。MUST NOT 将 `PipelineEngineService`（`IPipelineEngine, ISingleton`，非 `IDynamicApiController`）当作 HTTP 路由排查对象。路由迁移决策见 ADR-003。

#### Scenario: 后端 LLM 完成但前端连接超时

- **WHEN** 后端日志显示 LLM 流式完成，前端显示「连接超时」
- **THEN** 排查 MUST 优先检查浏览器 Network 面板中 EventSource 请求 URL 是否含 apiUrl 前缀
- **AND** MUST NOT 优先修改 `PipelineEngineService` 或假设错误的 Furion 动态路由

> **路由更新（ADR-003）**：流水线 API 已从 `api/founder/ai/pipeline` 迁移至 `api/studio/pipeline/execute`。

