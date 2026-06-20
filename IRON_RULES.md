# JNPF 项目铁律

> 所有开发者必读，违反铁律的代码不允许合并。

---

## R1: 多租户铁律

1. **禁止自行从 JWT 解析 TenantId** — 必须调用 `TenantResolver.Resolve()`
2. **禁止硬编码租户 ID** — 从 `appsettings.json` 的 `MultiTenancy` 配置读取
3. **新增数据库查询必须加 `ApplyTenantFilter`** — 漏加 = 安全漏洞
4. **admin 的 tenantId 由代码逻辑决定，数据库 `f_tenant_id` 保持 NULL**
5. **非管理员持有平台租户身份 = 越权** — `TenantResolver.Resolve()` 自动拦截

## R2: SSE 铁律

1. **SSE 事件类型统一用 `token`** — 不用 `chunk`、不用 `delta`
2. **SSE 接口必须校验 pipelineId 归属当前租户** — 禁止跨租户读取
3. **SSE fetch 请求必须带 Authorization header**

## R3: LLM 调用铁律

1. **LLM Gateway 调用必须带有效 TenantId** — 使用 `TenantResolver.ResolveForExternalService()`
2. **无效租户直接拒绝，不兜底为平台租户** — 避免"无主请求"获得上帝通道
3. **历史消息必须做滑动窗口截断** — 防止 token 超限

## R4: 代码质量铁律

1. **每个 Service 禁止自定义 `GetTenantId()` 方法** — 统一用 `TenantResolver`
2. **异常必须记录完整堆栈** — 禁止 `catch (Exception) { }` 空捕获
3. **Markdown 渲染必须 sanitize** — 防 XSS，使用 DOMPurify
4. **新功能必须有降级策略** — SA 失败 → 降级为 LLM 直接分析

## R5: 前端铁律

1. **所有 HTTP 请求必须带 Authorization header** — 包括 fetch、SSE
2. **401 响应必须跳转登录页** — 不显示通用错误消息
3. **AI 消息渲染必须 sanitize HTML** — `v-html` + DOMPurify

## R6: SA 流水线铁律

1. **SA 调用改为 SSE 流式转发** — 禁止一次性阻塞等待
2. **SA 失败时必须降级为纯 LLM 分析** — 不允许直接报错退出
3. **SA 的 9 步结果全部透传到前端** — 不允许截断为 6 步
4. **IR 数据必须随消息一起持久化** — 不允许只存在内存中
