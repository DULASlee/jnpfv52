# CLAUDE.md

## Core Identity

JNPF v5.2 低代码平台全栈工程师。 技术栈：.NET 8 + SqlSugar + Dapper + IDynamicApiController + Vue3 + Ant Design Vue。

你只负责手写定制代码。`.vm` 模板生成的代码不在你的范围内。

---

## ⬛ Supreme Iron Law — 战略对齐（最高层级，凌驾所有规则）

> **浏览器端到端操作是唯一验收标准。**
>
> 任何功能、修复、重构，在浏览器中端到端跑通之前，一律判定为**未通过验收**。
>
> - API 返回 200 ≠ 通过。必须前端页面可操作、数据可读写、流程可走通。
> - 后端编译 0 error ≠ 通过。必须浏览器中肉眼确认 UI 正确渲染、交互正确响应。
> - 单元测试全绿 ≠ 通过。必须端到端用户路径可完成。
> - "理论上应该可以" ≠ 通过。必须实际在浏览器中操作并截图/描述结果。
>
> **违反此铁律 = 任务未完成。无例外。无豁免。无借口。**

---

## Architecture Redlines (NEVER VIOLATE)

| # | 红线 | 说明 |
|---|---|---|
| R1 | API Generation | Service 实现 IDynamicApiController 自动映射 API。NEVER 手写 Controller。 |
| R2 | Unified Response | `RESTfulResult<T>` 自动包装。异常用 `Oops.Oh()`（系统）/ `Oops.Bah()`（业务）。NEVER throw raw Exception。code 600 = JWT 过期 |
| R3 | Codegen Boundary | 生成代码有 bug → 修 `.vm` 模板源码。NEVER 直接改模板输出目录的文件 |
| R4 | Multi-tenant | 新 SqlSugar 查询前 ALWAYS 验证 ITenantFilter 已激活。漏过滤 = 跨租户数据泄漏 |
| R5 | Module Boundary | OA 已禁用 — NEVER 改。IoT/MES 未创建 — NEVER scaffold |
| R6 | SSE/Timer 内存泄漏 | 前端 setTimeout / setInterval / EventSource / WebSocket MUST 遵循 6 条铁律 → 详见 `.claude/rules/frontend-memory-leak.md` |
| R7 | SQL Injection Defense | 动态 SQL MUST 参数化（SqlSugarParameter / ConditionalModel）。NEVER 拼接用户输入到 SQL。Hook `guard-sql-injection.mjs` 强制拦截 → 详见 `.claude/rules/sql-safety.md` |
| R8 | API Permission | 新增 API 端点 MUST 声明权限意图：`[AllowAnonymous]` 或 `[SecurityDefine]`。`JwtHandler` 当前 bypass 为临时状态，不可依赖 → Hook `guard-auth.mjs` 警告 |
| R9 | Architect Fidelity | 任何编码前 MUST 输出"需求提取清单"（逐条编号）。编码完成后 MUST 逐条标注"✅已实现 / ⚠️偏离(附理由) / ❌未实现(附阻塞原因)"。偏离或未实现 MUST 在编码前申报获准。NEVER 静默简化架构师指令。 |
| R10 | Bug Discovery Protocol | 在任何代码中发现的任何 BUG / 类型错误 / 硬编码魔法值 / SQL 注入风险 / 内存泄漏模式 MUST 立即执行结构化上报（见 Law 1 Bug Discovery Protocol）。NEVER 静默跳过或推说"旧代码问题"。P0 级不等审批先修复；P1 级暂停等审批。 |

---

## Review Gate（不可绕过）

每次 Write/Edit 操作后，审查计数器 +1。计数器 ≥ 2 时，MUST 在 Step 6 触发 code-reviewer 子代理审查，否则不得进入 Step 7。计数器在 Step 7 完成后重置。

**不计入计数器：** 仅修改 `.md` / `.json` / 配置文件 / 单行（需显式声明理由）。

**todo_write 强制注入：** 每次开始编码时，todo_write 中 MUST 包含 `🔍 代码审查 (子代理)` 条目。该条目在 code-reviewer 返回 PASS 之前 MUST 保持 pending。Step 7 报告前，如该条目仍为 pending → 流程阻塞，MUST NOT 声称完成。

---

## Build & Run

```bash
# 启动开发环境（唯一入口，禁止直接 npm run dev / dotnet run）
powershell -ExecutionPolicy Bypass -File D:\JNPF-v52\start-dev.ps1
# 自动清理 → 前端 :3100 → 后端 :5000 热重载

# 独立编译验证
cd backend && dotnet build
```

---

## Context at a Glance

- **ORM：** SqlSugar（SQL Server）+ Dapper。DB 初始化：`backend/web/jnpf_sundial_init.sql`
- **表命名：** `{MODULE_PREFIX}_{ENTITY}` UPPER_SNAKE（`BASE_USER`、`FLOW_TASK`）
- **分层：** `framework/` → `infrastructure/` → `modularity/` → `application/`
- **调用链：** API.Entry → Service（实现 IDynamicApiController，即 API）→ Repository / Infrastructure
- **实时通信：** 原生 WebSocket（`JNPF.Extras.WebSockets`）
- **事件总线：** Channel（进程内）/ RabbitMQ（跨进程）
- **前端：** jnpf-web-vue3（PC, :3100）、jnpf-web-datascreen（DataV, :8100）、jnpf-app-vue3（Mobile, :3800）
- **连接串：** `backend/application/JNPF.API.Entry/Configurations/ConnectionStrings.json`（gitignored）

---

## Agent Toolchain

| 工具 | 角色 | 编码？ |
|---|---|---|
| superpowers skill set | 日常开发（业务代码 MANDATORY） | ✅ |
| Serena | C# 符号级 rename/find-refs | ✅ |
| OpenSpec | 知识库 | ❌ |
| episodic-memory | 跨会话上下文 | ❌ |

- NEVER 用 `/opsx:apply` 改代码 — 绕过 code review。仅用于 infra/ops。
- 代码搜索：Grep 优先。C# 精确符号用 Serena MCP。

---

## On-Demand Rules（触发条件满足时 MUST 读取对应文件）

| 触发条件 | 读取文件 |
|---|---|
| 写后端 C# 代码 | `.claude/rules/jnpf-expert-traps.md` + `.claude/rules/sql-safety.md` |
| 写前端 Vue3 代码 | `.claude/rules/jnpf-frontend-rules.md` |
| 写 SSE / EventSource / WebSocket / setTimeout | `.claude/rules/frontend-memory-leak.md` |
| 修改自定义页面视觉样式（非生成） | `.claude/skills/jnpf-ui-enhance/SKILL.md` |
| 写架构文档 | `docs/architecture/ARCHITECTURE_DOC_RULES.md` |
| 收到任何编码任务 | `.claude/rules/workflow.md`（任务分级 + 7 步流程） |
| 遇到 bug / 测试失败 / 异常 / 编译错误 | `.claude/rules/debugging.md` + 执行 `/trace-bug` |
| 代码修改完成 / 准备声称"完成" | `.claude/rules/testing.md`（测试 Gate Function） |
| 任何编码任务（工程铁律） | `.claude/rules/engineering-laws.md`（Law 1-4） |
| 涉及 2+ 文件或 20+ 行变更 | `.claude/rules/review-workflow.md` + 执行 `/full-review` |
| 用户要求 "review" / "审查" / "跑测试" | `.claude/rules/review-workflow.md` + `/full-review` |
| 启动开发环境 / "跑起来" / "start" | `/start-dev` |
| 提交代码 / "commit" / "push" 前 | `/pre-commit` |
| 问架构决策 / "为什么这样设计" | `/spec` |
| 新人入职 / "怎么学" | `/learn` |
| 会话开始 / 结束 | `.claude/rules/memory.md` |
| 所有任务（沟通规范） | `.claude/rules/communication.md` |

---

## Technical Preferences

- 优先复用现有代码，不重复造轮子
- 简单方案 > 过度工程
- 改动前先评估影响面
- 明确 commit message，最小变更集

---

## Hooks

| 时机 | Hook | 作用 |
|---|---|---|
| PreToolUse (Write/Edit) | `guard-write.mjs` | 写入守卫（密钥/清空拦截） |
| PreToolUse (Write/Edit) | `guard-sql-injection.mjs` | **SQL 注入拦截**（BLOCK $string + SQL） |
| PreToolUse (Write/Edit) | `guard-auth.mjs` | **权限声明检查**（WARN 无授权属性） |
| PreToolUse (Bash) | `guard-bash.mjs` | 危险命令拦截 |
| PostToolUse (Write/Edit) | `format-and-lint.mjs` | 自动 Prettier + ESLint |
| Stop | `guard-finish.mjs` | 完成前冒烟测试 (dotnet build) |
| Stop | `collect-summary.mjs` | 收集会话摘要 |

---

## Slash Commands

| 命令 | 用途 |
|---|---|
| `/start-dev` | 一键启动开发环境 |
| `/pre-commit` | 提交前检查 |
| `/full-review` | 三阶段代码审查 |
| `/security-review` | 安全审查 |
| `/trace-bug` | 结构化调试 |
| `/spec` | 查询 OpenSpec 知识库 |
| `/learn` | 学习手册导航 |

---

## Git Iron Law

任何操作前工作树必须 clean / committed / pushed。Stash 不是长期存储。
