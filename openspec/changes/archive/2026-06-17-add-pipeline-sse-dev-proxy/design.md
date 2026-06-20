# Design: add-pipeline-sse-dev-proxy

## 方案

开发环境 Vite 仅代理 `/dev` 前缀。axios 通过 `VITE_GLOB_API_URL` 自动加前缀；EventSource 需显式调用 `buildEventSourceUrl()`，规则与 axios `beforeRequestHook` 一致。

## 误诊排除

`PipelineEngineService` 非 HTTP API；流水线 HTTP 入口为 `AIDevelopmentPipelineService`（`api/founder/ai/pipeline`）。

## 关键文件

- `jnpf-web-vue3/src/utils/http/sseUrl.ts`
- `jnpf-web-vue3/src/views/studio/composables/useSSE.ts`
- `openspec/adr/ADR-002-sse-dev-proxy-prefix.md`
