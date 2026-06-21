# JNPF AI 错题本

> 每条错误记录格式：日期 | 类别 | 症状 | 根因 | 修复 | 关键词
> SessionStart 自动注入最近 30 天错误。新错误发现后立即追加。

---

## 2026-06-21

### M011 | 方法论 | 源码分析替代不了网络包抓取
- **症状**：SSE 源码看起来正确（`data.data \|\| data.content`），编译通过，但前端始终无 AI 回复。花了 4 小时反复改代码、清缓存、重启服务，全无效
- **根因**：一直在看源码猜测，从未抓取网络响应体。最终用 Playwright `page.on('response')` 抓包，发现 `/events` 返回 `{"code":600,"msg":"登录过期"}`——HTTP 层认证就失败了，后面所有 SSE 解析代码再正确也无用
- **修复**：**前端调试铁律——先抓包看网络响应体，再分析源码**。Playwright: `page.on('response', async r => { if (r.url().includes('/events')) console.log(await r.text()); })`
- **关键词**：`网络抓包`, `page.on('response')`, `Playwright`, `SSE`, `调试方法`, `600`

### M012 | 前端 | `getToken()` 自带 "Bearer " 前缀，不能重复拼接
- **症状**：SSE `/events` 请求返回 code 600（JWT 过期），但 token 刚登录是新的
- **根因**：JNPF `getToken()` 返回 `"Bearer eyJ..."`（已含 Bearer 前缀），代码又拼接 `` `Bearer ${token}` `` → 实际发送 `"Bearer Bearer eyJ..."` → JWT 中间件解析失败
- **修复**：`token.startsWith('Bearer ') ? token : \`Bearer ${token}\``
- **关键词**：`getToken`, `Bearer`, `Authorization`, `双重前缀`, `600`, `JWT`

## 2026-06-21 (earlier)

### M009 | 流程 | 跳过 brainstorming 直接编码
- **症状**：pipelineId 修复、stageName 修复均未走 S1 头脑风暴，直接 Edit→Build→Claim 完成
- **根因**：多次小修复产生"太简单不需要设计"的错觉，违反 Superpowers S1 铁律（任何功能/组件/逻辑的新增或修改 MUST brainstorming）
- **修复**：无论任务多小，编码前 MUST 调用 `superpowers:brainstorming`（即使输出只有 3 行也算）
- **关键词**：`brainstorming`, `S1`, `superpowers`, `流程`, `跳过`

### M010 | 流程 | 声称完成但未执行 Gate Function 验证
- **症状**：多次声称"✅ 完成"/"✅ 验证通过"，但未执行 5 步 Gate Function（IDENTIFY→RUN→READ→VERIFY→CLAIM）
- **根因**：把"编译 0 error"和"API 返回 200"当作完整验证，但缺少端到端浏览器截图 + 操作路径 + 实际输出确认（E1/E2/E3）
- **修复**：声称完成前 MUST 调用 `superpowers:verification-before-completion`，执行 Gate Function 全部 5 步
- **关键词**：`verification-before-completion`, `Gate Function`, `S2`, `E2E`, `验证`

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

### M013 | 后端 | Pipeline 步骤间重复下载同一图片
- **症状**：SA 门控链路中同一张图片在步骤2（分析）和步骤7（生成方案）各下载一次，浪费带宽和延迟
- **根因**：Pipeline 步骤间无数据共享机制，每个步骤独立获取所需数据，`DownloadFileBytesAsync` 无缓存层
- **修复**：步骤2 下载后将 `byte[]` 缓存到 `ConcurrentDictionary<string, byte[]>`（key=attachmentId），步骤7 优先从缓存取，命中则跳过下载
- **关键词**：`重复下载`, `缓存`, `ConcurrentDictionary`, `Pipeline`, `附件`

### M014 | 后端 | AttachmentProcessor 尝试处理音视频格式
- **症状**：用户上传 mp3/mp4 等音视频文件后，AttachmentProcessor 尝试对其执行文档分析（OCR/文本提取），无意义且浪费资源
- **根因**：AttachmentProcessor 未做格式过滤，对所有附件类型一视同仁
- **修复**：App.json 配置排除音视频格式，AttachmentProcessor 检查 `IsAudioVideoFile()` 后直接跳过
- **关键词**：`音视频`, `AttachmentProcessor`, `格式过滤`, `边界条件`

### M015 | 配置 | AllowUploadFileType 白名单过严（28→58）
- **症状**：文档分析系统只允许 28 种文件格式上传，用户无法上传常见文档格式（如 .csv/.log/.xml/.rtf 等）
- **根因**：文件类型白名单基于保守策略（仅常见 Office 格式），未考虑文档分析场景需要处理多种数据源
- **修复**：扩展到 58 种全格式覆盖
- **关键词**：`AllowUploadFileType`, `白名单`, `文件格式`, `配置`

### M016 | 配置 | AllowUploadFileType 白名单遗漏 Markdown
- **症状**：用户上传 .md 文件被 D1800 校验拦截
- **根因**：白名单逐一列举格式时遗漏了 Markdown（.md），每种新格式都是潜在遗漏点
- **修复**：白名单补充 md 扩展名
- **关键词**：`Markdown`, `D1800`, `白名单遗漏`, `文件上传`

### M017 | 前端 | `getToken()` 返回类型不明确导致 28 处 `as string` 断言
- **症状**：全项目 28 处调用 `getToken() as string` 类型断言，类型安全性丧失
- **根因**：`getToken()` 声明时未标注返回类型，TypeScript 推断不精确，调用方被迫手动断言
- **修复**：在 `getToken()` 声明处加返回类型标注 `string | null`，28 处断言自动消除
- **关键词**：`getToken`, `as string`, `类型标注`, `TypeScript`

### M018 | 前端 | 纯附件消息发送被 handleSend 守卫拦截
- **症状**：用户只上传文件不打字，点击发送无任何反应，附件从未离开前端
- **根因**：`handleSend` 第 476 行 `if (!content || loading) return`，content 为空字符串时直接 return，附件上传代码不可达
- **修复**：handleSend 改为有附件时即使无文字也继续；sendMessage 同样放过有 uploadedFiles 的情况
- **关键词**：`handleSend`, `附件`, `空文本`, `守卫逻辑`, `早返回`

### M019 | 前端 | FormData 文件上传不带 `X-Tenant-Id` 导致 403
- **症状**：SA 门控文件上传返回 403
- **根因**：`defHttp` FormData POST 不经过 axios 拦截器链（拦截器只能拦截 JSON 请求），`X-Tenant-Id` header 缺失。FileService 标了 `[AllowAnonymous]` 跳过 JWT 校验，但租户守卫仍然拦截无 TenantId 的请求
- **修复**：authToken 新增 `getTenantId()` 从 JWT payload 解码 TenantId；上传请求显式携带 `X-Tenant-Id` header
- **关键词**：`FormData`, `X-Tenant-Id`, `403`, `多租户`, `axios 拦截器`

### M020 | 后端 | Mapster Adapt 覆盖审计字段
- **症状**：表更新操作后 CreateTime/CreateUserId 被重置为默认值
- **根因**：`input.Adapt<Entity>()` 全量映射，未排除审计字段。直接用 Adapt 结果做 Updateable 导致原始审计数据丢失
- **修复**：先查询原始实体 → `input.Adapt(entity)`（保留已有审计字段）→ 再更新，或使用 `.Ignore(dest => dest.CreateTime)` 排除
- **关键词**：`Mapster`, `Adapt`, `审计字段`, `CreateTime`, `Trap 2`
