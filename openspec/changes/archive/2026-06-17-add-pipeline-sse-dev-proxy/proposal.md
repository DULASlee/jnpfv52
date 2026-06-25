# Change: add-pipeline-sse-dev-proxy

## Why

流水线 SSE「后端有流、前端超时」的根因已定位为开发环境 EventSource 未走 Vite `/dev` 代理。需归档为 OpenSpec capability，供后续 AI/工程师共享，避免重复误诊为 `PipelineEngineService` 路由问题。

## What Changes

- 新增 capability `pipeline-sse-dev-proxy`（开发环境 SSE 代理前缀规范）
- 新增 ADR-002、Cursor 规则 `sse-dev-proxy.mdc`、CLAUDE.md R6 铁律 5
- 代码：`buildEventSourceUrl()` + useSSE 系列 composable 修复

## What

- 新增 capability `pipeline-sse-dev-proxy` spec 草稿
- 引用 ADR-002 作为决策锚点
- 修复已落地：`sseUrl.ts` + `useSSE.ts` 等 composable

## Scope

| 纳入 | 排除 |
|------|------|
| 开发环境 Vite 代理与 apiUrl 前缀规则 | 生产 Nginx 反向代理详细配置 |
| EventSource URL 构建规范 | SSE 事件 Schema 全量定义（见流水线设计文档） |
| `AIDevelopmentPipelineService` 路由表 | `PipelineEngineService` 内部引擎逻辑 |

## Status

- [x] 草稿创建（2026-06-18）
- [x] 代码修复已合并工作区
- [ ] 端到端 UI 复测通过后 `/opsx:archive`
