# ADR-003：流水线执行 API 从 founder 迁移至 studio

| 字段 | 内容 |
|------|------|
| 状态 | 已接受 |
| 日期 | 2026-06-18 |
| 决策者 | 架构评审 |
| 关联 | ADR-002（SSE 代理前缀） |

## 背景

流水线执行 API 原路径为 `api/founder/ai/pipeline/*`。「提交需求」页面对**所有角色**开放（普通用户、业务专家、开发者、租户/平台管理员、创始人），与 `founder` 命名语义冲突。

### 隐患

1. **权限误解**：`FounderGuardMiddleware` 拦截 `/api/founder/*`；pipeline 曾作为匿名白名单例外，非 founder 用户依赖此 hack 才能访问。
2. **Studio 路由不一致**：其他 Studio 服务均在 `api/studio/` 下，仅 TOTP 认证保留 `api/studio/founder/`。

## 决策

将 `AIDevelopmentPipelineService` 路由迁移至：

```
[Route("api/studio/pipeline/execute")]
```

与 `PipelineStageConfigService`（`api/studio/pipeline/stages`）、`ModelRoutingService` 等配置类 API 平级。

**废止** `api/founder/ai/pipeline/*`（不保留兼容路由，避免双轨技术债）。

## 路由映射

| 操作 | 新路由 |
|------|--------|
| 创建 | `POST /api/studio/pipeline/execute/create` |
| 执行 | `POST /api/studio/pipeline/execute/{pipelineId}/execute` |
| SSE | `GET /api/studio/pipeline/execute/{pipelineId}/events` |
| 详情 | `GET /api/studio/pipeline/execute/{pipelineId}` |
| 列表 | `GET /api/studio/pipeline/execute/list` |
| 确认阶段 | `POST /api/studio/pipeline/execute/stage/{stageId}/confirm` |
| 回滚 | `POST /api/studio/pipeline/execute/{pipelineId}/rollback` |
| 启动 | `POST /api/studio/pipeline/execute/{pipelineId}/start` |

鉴权：普通 JWT（与 Studio 其他 API 一致），**不再**依赖 `X-Founder-Token`。

## 后果

### 正面

- 路径语义与权限模型一致
- 从 `FounderGuardMiddleware` 白名单移除 pipeline，减少安全例外
- Studio API 命名空间统一

### 负面

- 前端/文档中所有旧 URL 须同步更新（一次性迁移）

## 相关文件

- `backend/.../AIDevelopmentPipelineService.cs`
- `backend/.../FounderGuardMiddleware.cs`
- `jnpf-web-vue3/src/api/founder/pipeline.ts`（baseUrl）
- `jnpf-web-vue3/src/views/studio/components/AiChatPanel.vue`
