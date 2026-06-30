# JNPF 架构红线全量目录 —— 从"AI 警告"到"框架根治"

> **核心命题：** 专家指出安全红线不应靠"每次警告 AI"来守住，而应通过全局拦截器/中间件/Roslyn Analyzer 从架构层面彻底解决。
> 本文档系统性梳理：每一个红线当前如何执行、是否可框架根治、根治方案是什么、根治后 AI 还需不需要管。

**统计：** 总计 24 条红线/陷阱/准则。其中 14 条可框架根治（58%），6 条可部分根治（25%），4 条不可根治（17%）。

---

## 第一类：✅ 可框架根治（14 条 — 实施后 AI 零负担）

### R4 — 多租户隔离 ⚠️ 最高安全风险

| 维度 | 内容 |
|---|---|
| **当前** | Hook `guard-tenant-filter.mjs` (L0) 扫描 C# 代码拦截 `DisableGlobalFilter` / `Updateable` 无 `Where` / 原生 SQL 无 `WHERE` |
| **根治** | ① 重写 `Updateable<T>()` / `Deleteable<T>()` 自动注入 `Where(x => x.TenantId == currentTenantId)` ② `Ado.SqlQuery` → 创建 `SafeSqlQuery()` 包装方法 ③ `DisableGlobalFilter("TenantFilter")` 无 admin context 时直接 throw |
| **技术栈** | SqlSugar AOP + 扩展方法 + 全局过滤器配置 |
| **根治后** | Hook 保留作为 defense-in-depth。AI 不再需要记住 Trap 7/8/13 |

### R7 — SQL 注入防御 ⚠️ 最高安全风险

| 维度 | 内容 |
|---|---|
| **当前** | Hook `guard-sql-injection.mjs` (L0) 正则扫描 `$"SELECT..."` / `string.Format(SQL)` / `Ado.SqlQuery($"...")` |
| **根治** | ① Roslyn Analyzer: 编译时禁止 `$"..."` 含 SQL 关键字 ② 包装 `Ado` API 只接受 `FormattableString`（编译器自动参数化） ③ `[SqlInjectionSafe]` attribute 白名单 |
| **技术栈** | Roslyn Analyzer + FormattableString + 自定义 SqlSugar 包装器 |
| **根治后** | SQL 注入在编译时就不可能。Hook 保留作为 CI 二次校验 |

### R8 — API 权限声明

| 维度 | 内容 |
|---|---|
| **当前** | Hook `guard-auth.mjs` (L0) 扫描 `IDynamicApiController` 无 `[SecurityDefine]`/`[AllowAnonymous]`/`[Authorize]` |
| **根治** | ① Roslyn Analyzer: 编译时强制权限属性 ② 全局中间件: 无权限声明的 API → 默认返回 401（secure-by-default） ③ 激活 `JwtHandler`（当前临时 bypass） |
| **技术栈** | Roslyn Analyzer + ASP.NET 全局 Authorization 中间件 |
| **根治后** | 编译不过 = 根本没权限。运行时无声明 = 自动 401。AI 不再需要记住 |

### R2 — 统一响应格式

| 维度 | 内容 |
|---|---|
| **当前** | L2 约定 — AI 被告知 "NEVER `new RESTfulResult<T>()`" |
| **根治** | `RESTfulResult<T>` 构造函数改为 `internal` → 用户代码无法实例化。框架自动包装 |
| **技术栈** | C# `internal` 访问修饰符 |
| **根治后** | AI 根本写不出 `new RESTfulResult<T>()` — 编译错误 |

### R1 — API 自动生成（手写 Controller）

| 维度 | 内容 |
|---|---|
| **当前** | L2 约定 — "NEVER 手写 Controller 类" |
| **根治** | Roslyn Analyzer: 检测 `[ApiController]` 或 `ControllerBase` 继承（非生成代码）→ 编译错误 |
| **技术栈** | Roslyn Analyzer |
| **根治后** | 手写 Controller.cs → 编译不过 |

### R3 — 代码生成边界

| 维度 | 内容 |
|---|---|
| **当前** | L2 约定 — "修 .vm 模板, NEVER 改输出文件" |
| **根治** | ① 所有生成文件加 `[GeneratedCode]` attribute + Roslyn Analyzer 检测手动修改 ② 生成目录设为 OS 级只读（仅 codegen 工具可写） |
| **技术栈** | Roslyn Analyzer + 文件系统权限 |
| **根治后** | AI Write 工具写到生成目录 → OS 拒绝写入 |

### Trap 2 — Mapster 审计字段覆盖

| 维度 | 内容 |
|---|---|
| **当前** | CLAUDE.md 规则 — AI 必须记住 `Ignore(dest => dest.CreateTime)` |
| **根治** | `SafeAdapt<TEntity>()` 扩展方法 — 自动忽略 `CreateTime` / `CreateUserId` / `TenantId`。或 Mapster 全局配置排除审计字段 |
| **技术栈** | Mapster 全局配置 + 扩展方法 |
| **根治后** | 任何 `Adapt<Entity>()` 都不可能覆盖审计字段 |

### Trap 9 — public 方法 = API 端点

| 维度 | 内容 |
|---|---|
| **当前** | CLAUDE.md 规则 — AI 必须记住 helper 方法用 `private`/`protected` |
| **根治** | Furion 配置: `IDynamicApiController` 方法需 `[ApiEndpoint]` attribute 才暴露（opt-in 替代 opt-out） |
| **技术栈** | Furion DynamicApiController 配置 |
| **根治后** | AI 写 public helper → 不自动暴露为 API。必须显式标注才暴露 |

### Trap 3 — N+1 查询

| 维度 | 内容 |
|---|---|
| **当前** | CLAUDE.md 规则 — AI 必须记住 `.Includes()` |
| **根治** | SqlSugar 全局禁用 lazy loading → 导航属性未 eager load 时 throw（开发环境） |
| **技术栈** | SqlSugar 配置 |
| **根治后** | N+1 变硬错误而非静默性能杀手 |

### Trap 14 — 无分页查询

| 维度 | 内容 |
|---|---|
| **当前** | CLAUDE.md 规则 — AI 必须记住 `.ToPageListAsync()` |
| **根治** | 重写 `ToListAsync()` 扩展方法 — 结果 > 100 条时 throw `Oops.Bah("请使用 ToPageListAsync")` |
| **技术栈** | SqlSugar 扩展方法 |
| **根治后** | 全表查询在开发环境立即炸 |

### Trap 11 — Mapster 循环引用栈溢出

| 维度 | 内容 |
|---|---|
| **当前** | CLAUDE.md 规则 — AI 必须记住 `MaxDepth` 或 `.Select()` |
| **根治** | Mapster 全局配置 `MaxDepth(3)` → 超过 3 层递归立即 throw |
| **技术栈** | Mapster 全局配置 |
| **根治后** | 循环引用变成硬错误，不会静默栈溢出 |

### R5 — 模块边界

| 维度 | 内容 |
|---|---|
| **当前** | Hook `guard-oa-module.mjs` (L0) — 路径正则匹配拦截 |
| **根治** | 已经是框架级方案（文件路径守卫）。可增强：OA 目录 `.gitignore` + OS 只读权限 |
| **技术栈** | 文件系统权限 + Hook（已有） |
| **根治后** | 无需变动。当前方案已足够 |

### R6 — 前端内存泄漏（部分根治）

| 维度 | 内容 |
|---|---|
| **当前** | Hook `guard-frontend-leak.mjs` (L0) — 正则扫描 `setTimeout` / `EventSource` |
| **根治** | ① `useSafeTimer()` / `useSafeEventSource()` composable — 自动 `onUnmounted` 清理 ② ESLint plugin: 禁止裸 `setTimeout` / `new EventSource()` |
| **技术栈** | Vue3 Composable + ESLint Plugin |
| **根治后** | AI 用 `useSafeTimer()` = 永不泄漏。ESLint 拦住裸调用 |

### Trap 10 — EventBus 幂等性

| 维度 | 内容 |
|---|---|
| **当前** | CLAUDE.md 规则 — AI 必须记住"事件处理器 MUST 幂等" |
| **根治** | `IIdempotentHandler` 基类 + `ProcessedEvent` 表 — 自动去重 `EventId` |
| **技术栈** | 抽象基类 + Redis/DB 去重表 |
| **根治后** | 继承 `IdempotentHandler<T>` → 自动幂等。AI 不需要理解原因 |

---

## 第二类：⚠️ 可部分根治（6 条 — 框架兜底 + AI 仍需配合）

### Trap 1 — 方法重命名 = URL 变更

| 维度 | 内容 |
|---|---|
| **当前** | CLAUDE.md 规则 |
| **根治（部分）** | Roslyn Analyzer: 检测 `IDynamicApiController` 方法重命名 → 编译警告 + 列出所有前端引用该 URL 的文件 |
| **AI 仍需** | 手动更新前端 URL（Analyzer 只能列文件，不能自动改） |

### Trap 6 — Async 后缀破坏路由

| 维度 | 内容 |
|---|---|
| **当前** | CLAUDE.md 规则 |
| **根治（部分）** | Roslyn Analyzer: `IDynamicApiController` 接口方法名含 `Async` → 编译错误 |
| **AI 仍需** | 无（编译时阻断） |

### Trap 7/8/13 — 租户子查询/Updateable（与 R4 重叠）

| 维度 | 内容 |
|---|---|
| **当前** | Hook `guard-tenant-filter.mjs` + CLAUDE.md 规则 |
| **根治（部分）** | 与 R4 方案相同（重写 SqlSugar API）。但自定义复杂 JOIN/子查询的分析器覆盖有限 |
| **AI 仍需** | 复杂 SQL 场景（UNION/CTE/跨库查询）需人工审查 |

### 准则 1 — 开发前先对齐（零代码优先）

| 维度 | 内容 |
|---|---|
| **当前** | CLAUDE.md 规则 |
| **根治（部分）** | 平台能力清单 API — 程序化查询"XX 功能是否有配置化方案"。但判断"是否应该用配置"仍需 AI |
| **AI 仍需** | 核心决策："这个需求走配置还是走代码？" |

### 准则 3 — 非侵入式扩展

| 维度 | 内容 |
|---|---|
| **当前** | CLAUDE.md 规则 + R5 Hook（文件路径守卫） |
| **根治（部分）** | framework/ 目录 OS 只读 + `[GeneratedCode]` 检测。但 Platform 配置文件的"非侵入修改"界定需要 AI 判断 |
| **AI 仍需** | 判断什么是"侵入"（改系统表结构？改流程设计器配置？） |

### 准则 5 — 升级兼容

| 维度 | 内容 |
|---|---|
| **当前** | CLAUDE.md 规则 |
| **根治（部分）** | `[PublicAPI]` / `[InternalAPI]` attribute 标记框架 API 稳定性承诺。AI 检测到使用 `[InternalAPI]` → 警告 |
| **AI 仍需** | 判断"这个内部 API 是否可能在下个版本变化" |

---

## 第三类：❌ 不可框架根治（4 条 — 必须 AI 判断）

### R9 — 架构师指令忠实执行

| 维度 | 内容 |
|---|---|
| **原因** | AI-Human 通信质量，不是代码安全问题。需求提取清单、逐条标注——这些是过程纪律 |

### R10 — Bug 发现强制上报

| 维度 | 内容 |
|---|---|
| **原因** | AI 行为规范。框架无法判断 AI 是否"发现了 bug 但沉默" |

### 准则 2 — 配置优先，最简实现

| 维度 | 内容 |
|---|---|
| **原因** | YAGNI 判断需要业务理解。框架无法区分"必要的扩展点"和"过度工程" |

### 准则 4 — 闭环验证交付

| 维度 | 内容 |
|---|---|
| **原因** | 验证标准的定义是人的工作。框架可以提供编译/测试/截图工具，但不能定义"什么是完成" |

---

## 📊 汇总

| 分类 | 数量 | 占比 | 典型例子 |
|---|---|---|---|
| ✅ 可框架根治 | 14 | 58% | R4 租户/R7 SQL注入/R8 权限/Trap 2 审计字段 |
| ⚠️ 可部分根治 | 6 | 25% | Trap 1 URL变更/Trap 7 复杂SQL/准则1 零代码决策 |
| ❌ 不可根治 | 4 | 17% | R9 架构师指令/R10 Bug上报/准则2 YAGNI |
| **总计** | **24** | **100%** | |

---

## 🚀 实施路线图建议

| 阶段 | 内容 | 预计工作量 | 效果 |
|---|---|---|---|
| **Phase 1: 安全红线** | R4(租户) + R7(SQL注入) + R8(权限) 框架根治 | 3-5 天 | 三项最高安全风险从"靠 AI 自觉"变为"框架强制" |
| **Phase 2: 数据完整性** | Trap 2(审计字段) + Trap 3(N+1) + Trap 14(分页) + Trap 11(循环引用) | 2-3 天 | 数据层缺陷从"运行时静默"变为"开发时硬错误" |
| **Phase 3: API 规范** | R1(Controller) + R2(响应格式) + R3(代码生成) + Trap 9(API暴露) + Trap 6(Async后缀) | 2-3 天 | API 层从"AI 记住规则"变为"编译时强制" |
| **Phase 4: 前端安全** | R6(内存泄漏) ESLint + composable | 1-2 天 | 前端从"正则扫描拦截"变为"安全 API 封装" |
| **Phase 5: 事件总线** | Trap 10(幂等) 基类 | 1 天 | 事件处理从"AI 记住幂等"变为"继承即幂等" |

**全部实施后：24 条红线中 20 条（83%）有框架级兜底。AI 只需关注 4 条过程纪律。**
