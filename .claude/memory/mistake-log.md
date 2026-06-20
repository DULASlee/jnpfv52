# JNPF AI 错题本

> 每条错误记录格式：日期 | 类别 | 症状 | 根因 | 修复 | 关键词
> SessionStart 自动注入最近 30 天错误。新错误发现后立即追加。

---

## 2026-06-20

### M001 | 后端 | C# `volatile long` 编译错误 CS0677
- **症状**：`error CS0677: 可变字段的类型不能是"long"`
- **根因**：C# 不允许 `volatile` 修饰 64 位值类型（long），仅支持引用类型和 ≤32 位基元类型
- **修复**：改用 `Volatile.Read(ref _field)` / `Volatile.Write(ref _field, value)`，保留 `volatile bool` 用于双重检查锁
- **关键词**：`volatile`, `long`, `CS0677`, `线程安全`, `Volatile.Read`

### M002 | 后端 | `Oops.Bah()` 优于 `UnauthorizedAccessException`
- **症状**：直接 throw `new UnauthorizedAccessException(...)` 导致 HTTP 500，破坏 JNPF 统一响应 `{ code, data, msg }` 格式
- **根因**：JNPF/Furion 框架通过 `Oops.Bah()` 返回 HTTP 200 + 业务错误码；原生异常被转为 500
- **修复**：全部改用 `throw Oops.Bah("消息")`，需 `using JNPF.FriendlyException;`
- **关键词**：`Oops.Bah`, `异常处理`, `RESTfulResult`, `统一响应`

### M003 | 前端 | `fetch()` 不经过 Vite 代理导致请求未达后端
- **症状**：前端调用 `fetch(url, { method:'POST' })` 后，后端没有任何日志，`ExecuteStageAsync` 从未被调用
- **根因**：`fetch()` 直接在浏览器发起请求，不走 axios 拦截器链（baseURL、token 注入、Vite 代理）
- **修复**：业务 POST 请求改用 `defHttp.post()`（项目 axios 封装），`fetch()` 仅保留给 SSE ReadableStream
- **关键词**：`fetch`, `defHttp`, `Vite 代理`, `axios`, `POST`

### M004 | 前端 | `RESTfulResult` 包装导致 `res.pipelineId` 为 undefined
- **症状**：`pipelineId.value = res.pipelineId` → 值为 0，后续 `/execute/0/execute` 404
- **根因**：JNPF 框架将返回值包装为 `{ code:200, data: { pipelineId:33 } }`，真实数据在 `data` 下
- **修复**：`const data = res?.data || res; pipelineId.value = data?.pipelineId || data?.PipelineId`
- **关键词**：`RESTfulResult`, `data 包装`, `defHttp`, `响应解包`

### M005 | 后端 | Pipeline 实体落库漏写 `TenantId`
- **症状**：AiPipelineEntity 创建时未设置 `TenantId` 字段，下游 SA 调用携带错误的租户标识
- **根因**：代码只将 tenantId 传给 `_pipelineEngine.CreateAsync()`，但落库的 `new AiPipelineEntity { ... }` 未包含 `TenantId = tenantId.ToString()`
- **修复**：在 entity 初始化器中显式赋值 `TenantId = tenantId.ToString()`
- **关键词**：`TenantId`, `落库`, `AiPipelineEntity`, `多租户`

### M006 | 后端 | SSE `/events` 端点缺少租户归属校验
- **症状**：任何知道 pipelineId 的用户都可订阅 SSE 流，绕过租户隔离
- **根因**：`GetPipelineEvents` 直接从 `_sseChannels` 取 Channel，未校验 pipeline 是否属于当前租户
- **修复**：查询 pipeline 的 TenantId，与 `TenantResolver.Resolve()` 对比；平台租户（超级管理员）跳过校验
- **关键词**：`SSE`, `租户隔离`, `GetPipelineEvents`, `IRON_RULES.md R2.2`

### M007 | 前端 | `buildFetchSseUrl` + `fetch` 两步调用已存在但仍失败
- **症状**：代码看起来正确（先 POST /execute 再 GET /events），但后端日志只有 `/create` 没有 `/execute`
- **根因**：`fetch()` 虽然用了 `buildFetchSseUrl` 构建 URL，但仍不经过 axios 拦截器（token、代理）
- **修复**：Step 1 改用 `defHttp.post`，Step 2 保留 `fetch` 给 SSE 流
- **关键词**：`buildFetchSseUrl`, `defHttp`, `SSE 两步分离`

### M008 | 工具链 | 删除文件前必须先对比内容
- **症状**：直接删除用户级 4 个 Hook 文件，用户质疑"为什么不合并"
- **根因**：跳过对比步骤，假设同名文件 = 功能重叠。实际用户级文件未在 settings.json 注册（死文件），但应该先展示判断依据再操作
- **修复**：删除前必须：Read 内容 → 对比差异 → 输出分析 → 获确认后再删
- **关键词**：`文件删除`, `hook`, `对比`, `操作流程`
