# JNPF AI 错题本

> **加载方式：** 每次编码前 Grep 关键词；犯错误后立即追加。
> **编号规则：** M001-M999 连续，不可重用。重编号在本文件末尾记录映射。

---

## Before You Code（每次写代码前过一遍）

这些是从 31 条错误中提炼的**重复模式**。每条背后都有 ≥2 次实际犯错记录。

| # | 铁律 | 来源 |
|---|------|------|
| 1 | **验证三路径**：改了防御代码 → 正向/异常/缺失全测，不能只测修的那条 | M030 |
| 2 | **改 prompt = 改代码**：改完逐条对照原始 spec 审计，不能凭"感觉对了" | M031 |
| 3 | **先抓包再分析源码**：前端无响应 → Playwright `page.on('response')` → 看实际返回体 | M011 |
| 4 | **不跳过 brainstorming**：无论任务多小，MUST 走 S1。输入详尽≠豁免流程 | M009, M024 |
| 5 | **声称完成 = Gate Function 5 步**：IDENTIFY→RUN→READ→VERIFY→CLAIM，缺一不可 | M010 |

---

## 一、方法论（最贵——每条都导致 ≥1 小时浪费）

### M030 | 验证不完整：只测"修的那条路"

- **症状**：Q3-security 修复后只验了缺失路径，正向/漏洞路径被架构师追问才补
- **根因**：本能只验自己改过的那条路径，忽略防御代码影响多条路径
- **规则**：改了 if/switch/guard → 所有分支全测
- **日期**：2026-06-26 | **关键词**：`三路径`, `正向/异常/缺失`

### M032 | import type 导入运行时值 → ReferenceError

- **症状**：IrObservatoryPanel IR-3 Tab 不渲染，Console 报 `ReferenceError: IR3_RELEVANT_EVENT_TYPES is not defined`
- **根因**：`useIrObservatory.ts:11` 将 `IR3_RELEVANT_EVENT_TYPES` 和 `IR3_FRAGMENT_TYPES` 放在 `import type` 块中。`import type` 在编译时被擦除，运行时无法访问这些常量。而代码在 `new Set(IR3_RELEVANT_EVENT_TYPES)` 和 `IR3_FRAGMENT_TYPES.includes()` 中将它们作为运行时值使用
- **修复**：将两个常量从 `import type { ... }` 移到独立的 `import { ... }` 语句
- **日期**：2026-07-04 | **关键词**：`import type`, `ReferenceError`, `运行时值`, `类型擦除`, `Vite`

### M031 | Prompt 审计：凭感觉不逐条对照

- **症状**：论断纪律改版后用户亲自对照 spec 发现缺了两条核心规则
- **根因**：把 prompt 修改当"写文章"而非"改代码"，没有 diff 和回测
- **规则**：改完 MUST 逐条对照原始 spec，标注每条的"已覆盖/已删除/已修改"
- **日期**：2026-06-26 | **关键词**：`spec审计`, `逐条对照`, `prompt工程`

### M011 | 源码分析替代不了网络抓包

- **症状**：SSE 源码正确但仍无 AI 回复，花 4 小时反复改代码无效
- **根因**：一直看源码猜测，从未抓网络响应体。最终 Playwright `page.on('response')` 发现 `/events` 返回 `{"code":600,"msg":"登录过期"}`——HTTP 层就失败了
- **规则**：前端无响应 → 先抓包看实际返回，再分析源码
- **日期**：2026-06-21 | **关键词**：`网络抓包`, `Playwright`, `SSE`, `调试方法`

### M009 | 跳过 brainstorming 直接编码

- **症状**：多次小修复直接 Edit→Build→Claim，未走 S1
- **根因**："太简单不需要设计"的错觉，违反 S1 铁律
- **规则**：编码前 MUST `superpowers:brainstorming`，即使输出只有 3 行
- **日期**：2026-06-21 | **关键词**：`brainstorming`, `S1`, `流程`

### M010 | 声称完成但未执行 Gate Function

- **症状**：多次声称"✅ 完成"，但未执行 5 步验证
- **根因**：把"编译 0 error"当作完整验证，缺少 E2E 证据
- **规则**：声称完成前 MUST `superpowers:verification-before-completion`
- **日期**：2026-06-21 | **关键词**：`Gate Function`, `S2`, `E2E`

### M024 | 跳过 Phase 抬头声明

- **症状**：SA 门控施工全程未输出 Phase 抬头
- **根因**：施工手册极详尽 → 误判"设计已定直接执行"。手册是输入，流程是纪律，不冲突
- **规则**：无论输入多详细，逐 Phase 输出抬头声明
- **日期**：2026-06-23 | **关键词**：`Phase抬头`, `流程违规`, `七阶段流水线`

### M008 | 删除文件前未对比内容

- **症状**：直接删除 4 个用户级 Hook 文件，用户质疑
- **根因**：跳过对比步骤，假设同名 = 功能重叠
- **规则**：删除前 MUST Read → 对比 → 输出分析 → 获确认
- **日期**：2026-06-20 | **关键词**：`文件删除`, `对比`

---

## 二、C# 语言陷阱

### 模式：API 名记错 / 语法边界不清

| 编号 | 症状 | 根因 | 修复 | 日期 | 关键词 |
|------|------|------|------|------|--------|
| M001 | `volatile long` CS0677 | C# 不允许 volatile 修饰 64 位值类型 | `Volatile.Read/Write` | 06-20 | `volatile`, `CS0677` |
| M021 | `SingleProducer` 不存在 | .NET 8 属性名是 `SingleWriter` | 改用 `SingleWriter` | 06-22 | `BoundedChannelOptions`, `Channel` |
| M022 | using 写在方法体内 | C# 只允许文件级/namespace 级 using | 完全限定名替代 | 06-22 | `using directive`, `Program.cs` |
| M023 | `??` 类型不匹配 CS0019 | `ReadOnlyCollection<string>` vs `string[]` | 三元表达式 + 显式转型 | 06-22 | `??`, `类型不匹配` |
| M025 | `$"""` + JSON 大括号 CS9006 | `{{` 转义链超限 | `$$"""` 双美元 | 06-23 | `raw string`, `$$`, `CS9006` |
| M026 | `System.Text.Json` 不认字符串枚举 | 默认按数值反序列化枚举 | `JsonStringEnumConverter` | 06-23 | `enum`, `JsonException` |
| M028 | `List<T> = new()` 使 null 检查失效 | 反序列化用默认值而非 null | 额外检测 `Count == 0` | 06-23 | `record`, `init`, `default` |
| M029 | `new` 关键字不能替代 virtual | `new` 是隐藏不是重写，CLR 分派到基类 | 构造函数注入 Fake | 06-23 | `new vs virtual`, `vtable` |

---

## 三、JNPF 框架专属陷阱

### 模式：框架约定被 .NET 直觉覆盖

| 编号 | 症状 | 根因 | 修复 | 日期 | 关键词 |
|------|------|------|------|------|--------|
| M002 | throw `UnauthorizedAccessException` → HTTP 500 | JNPF 统一响应要求 `Oops.Bah()` | `throw Oops.Bah("msg")` | 06-20 | `Oops.Bah`, `RESTfulResult` |
| M004 | `res.pipelineId` 为 undefined | JNPF 包装为 `{ code, data: {...} }` | `const data = res?.data \|\| res` | 06-20 | `RESTfulResult`, `data 包装` |
| M020 | 更新后 CreateTime 被重置 | Mapster `Adapt()` 全量映射覆盖审计字段 | 先查原始实体再 Adapt | 06-21 | `Mapster`, `Adapt`, `Trap 2` |
| M005 | PipelineEntity 落库无 TenantId | 只传给 engine 未写入 entity 初始化器 | 显式赋值 `TenantId` | 06-20 | `TenantId`, `多租户` |
| M006 | SSE /events 无租户校验 | 直接从 `_sseChannels` 取 Channel | 校验 pipeline 归属当前租户 | 06-20 | `SSE`, `租户隔离` |
| M019 | FormData 上传 403 | axios 拦截器不处理 FormData，缺 `X-Tenant-Id` | 显式携带 `X-Tenant-Id` | 06-21 | `FormData`, `X-Tenant-Id` |

---

## 四、前端陷阱

### 模式：axios/Vite 代理链路断裂

| 编号 | 症状 | 根因 | 修复 | 日期 | 关键词 |
|------|------|------|------|------|--------|
| M003 | `fetch()` POST 未达后端 | fetch 不走 axios 拦截器链（baseURL/token/代理） | 业务 POST 改用 `defHttp.post()` | 06-20 | `fetch`, `defHttp`, `Vite` |
| M007 | buildFetchSseUrl + fetch 仍失败 | 虽然 URL 对了，但 fetch 仍不走 axios | Step1 用 defHttp, Step2 用 fetch | 06-20 | `buildFetchSseUrl`, `SSE 两步` |
| M012 | `Bearer Bearer` 双重前缀 | `getToken()` 自带 "Bearer "，代码又拼接一次 | `token.startsWith('Bearer ') ? token : \`Bearer ${token}\`` | 06-21 | `getToken`, `双重前缀` |
| M017 | 28 处 `as string` 类型断言 | `getToken()` 未标注返回类型 | 加 `string \| null` 返回类型 | 06-21 | `as string`, `TypeScript` |
| M018 | 纯附件消息被 handleSend 守卫拦截 | `if (!content) return` 早返回，附件代码不可达 | 有附件时即使无文字也继续 | 06-21 | `handleSend`, `附件`, `早返回` |

---

## 五、边界条件 / 配置

| 编号 | 症状 | 根因 | 修复 | 日期 | 关键词 |
|------|------|------|------|------|--------|
| M014 | AttachmentProcessor 处理音视频 | 未做格式过滤 | `IsAudioVideoFile()` 跳过 | 06-21 | `音视频`, `格式过滤` |
| M015 | 文件格式白名单过严 (28种) | 保守策略，未考虑文档分析场景 | 扩展到 58 种 | 06-21 | `AllowUploadFileType`, `白名单` |
| M016 | Markdown 被 D1800 拦截 | 白名单逐一列举遗漏 .md | 补充 md 扩展名 | 06-21 | `Markdown`, `白名单遗漏` |
| M013 | Pipeline 步骤间重复下载图片 | 步骤间无数据共享机制 | `ConcurrentDictionary` 缓存 | 06-21 | `重复下载`, `缓存` |

---

## 六、测试陷阱

| 编号 | 症状 | 根因 | 修复 | 日期 | 关键词 |
|------|------|------|------|------|--------|
| M027 | Moq mock 不命中，测试假绿 | `CancellationToken` + 重载 + Moq 匹配失效 | Fake 显式接口实现替代 Moq | 06-23 | `Moq`, `CancellationToken`, `Fake` |

---

## 七、架构 / 研究（无代码缺陷，仅记录决策上下文）

| 编号 | 内容 | 结论 | 日期 |
|------|------|------|------|
| M013-R | Open Code Review 对 JNPF 适用性评估 | OCR 不含 C# 规则，不能替代 code-reviewer 子代理 | 06-24 |
| M014-R | CodeGraph 部署 + 21 Hook 审计 | 发现 guard-finish 内存泄漏 + CodeGraph 无限递归 + 规则分层协议 | 06-25 |
| M015-R | V3.0 涅槃重构 | 自建状态机 → Claude Code 原生 Agent；7 soul + 3 脚本 + 7 Hook | 06-26 |

---

## 编号映射（旧→新）

```
旧 M013 (后端Pipeline)  → M013 (保留)
旧 M013 (研究OCR)      → M013-R
旧 M014 (后端格式过滤) → M014 (保留)
旧 M014 (基础设施审计)  → M014-R
旧 M015 (配置白名单)   → M015 (保留)
旧 M015 (V3.0架构)     → M015-R
其余编号未变。
```

---

## 错误类型分布

```
方法论:   7 ███████
C# 语法:  8 ████████
JNPF 框架: 6 ██████
前端:     5 █████
边界/配置: 4 ████
测试:     1 █
```

> **解读**：C# 语法错误虽然最多，但都是"查文档即可"的一次性错误。方法论错误只占 7/31（22%），但每条都导致 ≥1 小时浪费——**方法论是 ROI 最高的改进方向**。
