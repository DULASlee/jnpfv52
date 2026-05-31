# Change: add-logging-audit-spec

## Why

切面1（日志系统重构）开工前，须将现有日志链路沉淀为 OpenSpec capability，供 AI 与工程师共享「审计日志 / 操作日志 / 异常日志」的实现锚点。

## What

- 新增 capability `logging-audit` 的 spec 草稿（基于当前源码穿透验证）
- 切面1 施工完成后通过 `/opsx:archive` 归档至 `openspec/specs/logging-audit/spec.md`

## Scope

| 纳入 | 排除 |
|------|------|
| `LogEventSubscriber`、`RequestActionFilter`、`LogExceptionHandler` | Serilog 文件 sink 重构细节（切面1 再定） |
| **BASE_SYS_LOG** 表与 `SysLogService` | 前端日志 UI |

## Status

- [x] 草稿创建（2026-05-31，知识库基础设施阶段4）
- [ ] 切面1 施工完成后 archive
