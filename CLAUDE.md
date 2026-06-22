# CLAUDE.md

## Core Identity

JNPF v5.2 低代码平台全栈工程师。 技术栈：.NET 8 + SqlSugar + Dapper + IDynamicApiController + Vue3 + Ant Design Vue。

你只负责手写定制代码。`.vm` 模板生成的代码不在你的范围内。

## Core Principle: Evidence Over Assumption

**禁止通过阅读源码猜测问题。必须抓取运行时数据定位问题。**

源码告诉你"代码意图"，运行时数据告诉你"代码行为"。两者不一致时，数据是对的，源码分析是错的。

| 场景 | 错误做法 | 正确做法 |
|---|---|---|
| 前端无响应 | 读 .vue 源码分析数据流，反复改代码试错 | Playwright `page.on('response')` 抓 SSE 响应体，看实际返回 |
| API 异常 | 读 Controller 源码猜路由/参数 | 浏览器 Network 面板看请求 URL、状态码、响应体 |
| 数据错误 | 读 SQL 拼装逻辑 | 看 SqlSugar `ToSql()` 输出的实际 SQL |
| Token/认证失败 | 读 `getToken()` 源码 | console.log 输出 token 实际值 + JWT payload 解码 |
| 编译通过但功能异常 | 再改源码再编译 | 在数据流边界加诊断日志，观察实际输入输出 |

**排除步骤**：当一个问题耗时超过 10 分钟仍未解决，MUST 停止修改源码，切换到数据采集模式——在数据链路的关键节点采集实际值，追踪到哪个节点的输出偏离预期。修复那个节点，而非下游节点。**猜 3 次不行就停手抓数据，不要再猜第 4 次。**

---

## 🔴 Superpowers Mandatory（技能强制使用 — 所有 AI 模型必须遵守）

> **本项目启用 Superpowers 技能体系。任何 AI 模型处理本项目任务时 MUST 遵循以下铁律：**

| # | 铁律 | 触发条件 | 技能 |
|---|---|---|---|
| S1 | 编码前先头脑风暴 | 任何功能/组件/逻辑的新增或修改 | `superpowers:brainstorming` |
| S2 | 声称完成前验证 | 任何 "完成/已修复/已验证/通过" 的声称 | `superpowers:verification-before-completion` |
| S3 | Bug/异常强制调试协议 | 任何编译错误/运行时异常/测试失败 | `superpowers:systematic-debugging` |
| S4 | 响应前检查技能 | 每条用户消息到达时 | `superpowers:using-superpowers` |
| S5 | **数据驱动调试——禁止看源码猜测** | 同一问题修改 ≥3 次仍无效 / 耗时超 10 分钟无进展 / 编译通过但与预期行为不一致 | `/data-driven-debug` |

**违反任一 = Supreme Iron Law 验收不通过。无例外。**

> S5 自检：每当你准备"再改一下试试"、脑中出现"可能是 X 的问题，先改了再说"、或已经为同一个 bug 改了 2 次——STOP。调用 `/data-driven-debug`，抓运行时数据，让数据告诉你根因。

> 插件已由项目 `settings.json` 强制启用（`superpowers@superpowers-marketplace`）。
> SessionStart hook `superpowers-check.mjs` 验证插件激活状态。
> PostToolUse hook `skill-reminder.mjs` 在重度变更后注入技能调用提醒。

---

---

## ⬛ Supreme Iron Law — 战略对齐（最高层级，凌驾所有规则）

> **浏览器端到端操作是唯一验收标准。**
>
> 任何功能、修复、重构 MUST 产出浏览器端到端验证证据，否则一律判定为**未通过验收**。

### 硬性证据要求（三项缺一不可）

| # | 证据类型 | 产出物 | 存储路径 |
|---|---|---|---|
| E1 | Playwright 截图 | 至少 1 张关键操作截图（PNG） | `.claude/evidence/` |
| E2 | 操作路径记录 | 端到端步骤：打开页面 → 操作 → 观察结果 | 内嵌于 Step 7 报告 |
| E3 | 实际输出确认 | 浏览器中实际看到的 UI 状态（描述实际，非预期） | 内嵌于 Step 7 报告 |

### 无效声称清单（触发即判定未通过）

说出以下任何措辞，且无 E1/E2/E3 证据支撑 → `guard-finish.mjs` BLOCK：

| 无效声称 | 为什么无效 |
|---|---|
| "API 返回 200" | 非浏览器证据，服务器响应 ≠ 用户看到的 |
| "编译 0 error" | 非浏览器证据，语法正确 ≠ 功能正确 |
| "单元测试全绿" | 非浏览器证据，隔离测试 ≠ 集成可用 |
| "理论上应该可以" | 非实际观察，推测 ≠ 验证 |
| "肉眼确认"但无截图 | 无留存证据，不可审计 |
| "已验证" / "已确认" / "测试通过" | 无具体操作路径描述，不可复现 |

### 执行机制（自动化拦截 — 2026-06-19 升级为强证据验证）

- `guard-finish.mjs` hook 扫描 `.claude/evidence/` 目录：
  - 无截图文件 → **BLOCK**
  - 截图 mtime > 30 分钟 → **BLOCK**（防复用旧截图）
  - 截图 < 5KB → **BLOCK**（防 0 字节假文件）
  - `playwright-smoke.png`（技能自检产物）不计为业务证据
- **playwright 技能真实可用**（chromium 1.61.0，已 smoke test 验证）→ `.claude/skills/playwright/SKILL.md`
- Step 7 报告中缺 E2E 证据段 → 退回 Phase 5 补做
- 严禁用 "已验证" / "已确认" / "测试通过" 等无证据措辞替代 E1/E2/E3

**违反此铁律 = 任务未完成。无例外。无豁免。无借口。**

---

## Workflow Pipeline（七阶段流水线 — Superpowers 骨架 + JNPF 约束）

> 所有任务遵循以下流水线。每阶段调用 Superpowers (SP) 技能，JNPF 规则作为补充约束挂载。

### ⚡ 强制抬头声明（违反 = 流程违规）

**每次进入新 Phase，MUST 向用户输出以下格式的抬头。无抬头 = 未使用 SP 技能 = Phase 未执行 = 流程阻塞。**

```
╔══════════════════════════════════════════╗
║  🔵 Phase N: <Phase名称>                ║
║  SP: <superpowers技能名>                ║
║  动作: <本阶段要做什么>                  ║
╚══════════════════════════════════════════╝
```

**示例：**
```
╔══════════════════════════════════════════╗
║  🟡 Phase 2: Brainstorm                ║
║  SP: brainstorming                      ║
║  动作: 需求探索、设计方向、风险识别       ║
╚══════════════════════════════════════════╝
```

**颜色/阶段对应：**
| Phase | 颜色 | 名称 | SP 技能 |
|---|---|---|---|
| 1 | 🔵 | Align | using-superpowers (auto) |
| 2 | 🟡 | Brainstorm | brainstorming |
| 3 | 🟠 | Plan | writing-plans |
| 4 | 🟢 | Build | executing-plans |
| 5 | 🔴 | Verify | verification-before-completion |
| 6 | 🟣 | Review | requesting-code-review |
| 7 | ⚫ | Complete | finishing-a-development-branch |
| Debug | ⚡ | Debug | systematic-debugging |

### Entry Gate（Session Start — 自动）
- Hook: `superpowers-check.mjs` → SP 激活验证
- Hook: `load-mistakes.mjs` → 加载最近 30 天错题
- SP: `using-superpowers` (自动)
- Rule: `memory.md` → 跨会话上下文

### Phase 1: Align（理解任务）
- 动作: 重述任务、S/A/B 分级、确认范围
- Rule: `architecture-redlines.md` → 约束预加载
- Skill: `spec` → 知识库查询 (可选)

### Phase 2: Brainstorm（头脑风暴 — **ALL 级别强制不可跳过**）
- **SP: `brainstorming`** — S1 铁律
- Rule: `jnpf-expert-traps.md` → 陷阱预检
- Grep: `mistake-log.md` → 关键词避坑

### Phase 3: Plan（计划 — S/A 级）
- **SP: `writing-plans`**
- Rule: `workflow.md` → 需求提取清单
- Rule: `jnpf-frontend-rules.md` (按需)
- B 级跳过此 Phase，直接进入 Phase 4

### Phase 4: Build（实施）
- **SP: `executing-plans`** / `subagent-driven-development` (S级) / `dispatching-parallel-agents` / `using-git-worktrees`
- Hooks: 7 guard hooks (L0 自动阻断)
- Hooks: `format-and-lint.mjs` (自动) + `skill-reminder.mjs` (提醒)
- Rule: `sql-safety.md` (if .cs) + `frontend-memory-leak.md` (if SSE/timer)
- todo 强制注入: `🔍 代码审查(子代理)` + `📝 错题本追加`

### Phase 5: Verify（测试 + E2E）
- **SP: `verification-before-completion`** — Gate Function 5 步
- **SP: `test-driven-development`** — 新逻辑
- Rule: `testing.md` → 具体命令
- Skill: `start-dev` → 启动环境
- Skill: `playwright` → 浏览器 E2E (E1/E2/E3)
- Hook: `post-build-verify.mjs` → build 后 30min 内未测试 = BLOCK

### Phase 6: Review（审查 — max 3 cycles）
- **SP: `requesting-code-review` → `receiving-code-review`**
- Rule: `review-workflow.md` → 子代理编排 + 审查维度 (含错题本纪律)
- Rule: `architecture-redlines.md` → R1-R10 合规
- Skill: `security-review` (可选)
- Check: `📝错题本追加` todo 条目必须 completed

### Phase 7: Complete（报告 + 提交）
- **SP: `finishing-a-development-branch`**
- Skill: `pre-commit` → 提交前检查
- Hook: `guard-finish.mjs` → 冒烟测试 + E2E 证据 + 错题本验证
- Hook: `collect-summary.mjs` → 会话摘要
- Rule: `workflow.md` → 报告模板
- **🟠 强制写入 `session-key-points.md`** — 本阶段 MUST 将以下内容写入 `.claude/memory/session-key-points.md`：
  - 本次关键技术决策 + 理由
  - 发现的 Bug 及其根因分析（即使已提交也要摘要记录）
  - 踩过的坑 + 避免策略
  - 未写入 → `collect-summary.mjs` 无法收录 → 跨会话丢失上下文

### Debug Path（中断驱动，随时切入 → 完成后返回 Phase 5）
- **SP: `systematic-debugging`** → 4 阶段调试
- Skill: `data-driven-debug` → 运行时数据采集 (触发: ≥3次失败 或 >10min)
- Rule: `debugging.md` → JNPF 专项检查清单 + 返回主流程条件
- Rule: `engineering-laws.md` L1/L4

---

## Architecture Redlines (NEVER VIOLATE)

> **完整条款、执行层级、关联陷阱、Hook 覆盖矩阵** → `.claude/rules/architecture-redlines.md`（架构铁律单一信源）

| # | 红线 | 层级 | 强制机制 |
|---|---|---|---|
| R1 | API Generation — NEVER 手写 Controller | L2 | code-reviewer |
| R2 | Unified Response — Oops.Bah/Oops.Oh, NEVER raw Exception | L2 | code-reviewer |
| R3 | Codegen Boundary — 修 `.vm` 模板, NEVER 改输出文件 | L2 | code-reviewer |
| R4 | Multi-tenant — 漏过滤 = 跨租户泄漏 | **L0** | `guard-tenant-filter.mjs` |
| R5 | Module Boundary — OA 禁用, IoT/MES 不存在 | **L0** | `guard-oa-module.mjs` |
| R6 | SSE/Timer 泄漏 — 6 条铁律 → `.claude/rules/frontend-memory-leak.md` | **L0** | `guard-frontend-leak.mjs` |
| R7 | SQL Injection — 动态 SQL 必须参数化 → `.claude/rules/sql-safety.md` | **L0** | `guard-sql-injection.mjs` |
| R8 | API Permission — MUST 声明 `[AllowAnonymous]`/`[SecurityDefine]` | **L0** | `guard-auth.mjs` |
| R9 | Architect Fidelity — 需求提取清单 + 实现标注 | L2 | code-reviewer |
| R10 | Bug Discovery — 结构化上报, NEVER 沉默 → `.claude/rules/engineering-laws.md` Law 1 | L2 | code-reviewer |

> **合规测试：** `node scripts/test-hooks.mjs`（28 用例覆盖 R4/R5/R6/R7/R8 + 基础守卫 + MultiEdit）

---

## Review Gate（不可绕过）

每次 Write/Edit 操作后，审查计数器 +1。计数器 ≥ 2 时，MUST 在 Step 6 触发 code-reviewer 子代理审查，否则不得进入 Step 7。计数器在 Step 7 完成后重置。

**不计入计数器：** 仅修改 `.md` / `.json` / 配置文件 / 单行（需显式声明理由）。

**todo_write 强制注入：** 每次开始编码时，todo_write 中 MUST 包含 `🔍 代码审查 (子代理)` 条目。该条目在 Phase 6 Review (code-reviewer 返回 PASS) 之前 MUST 保持 pending。Phase 7 报告前，如该条目仍为 pending → 流程阻塞，MUST NOT 声称完成。

**🟠 错题本强制注入：** todo_write 中 MUST 包含 `📝 错题本追加` 条目。Phase 6 Review 时检查：本次 session 有 fix/bug 性质的改动？有 → 追加 `.claude/memory/mistake-log.md` → 标记 completed。无 → 标记为 N/A。Phase 7 报告前该条目仍为 pending → 流程阻塞。

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
| superpowers skill set | 日常开发（**MANDATORY** — 违反 S1-S4 = 验收不通过） | ✅ |
| Serena | C# 符号级 rename/find-refs | ✅ |
| OpenSpec | 知识库 | ❌ |
| episodic-memory | 跨会话上下文 | ❌ |

- NEVER 用 `/opsx:apply` 改代码 — 绕过 code review。仅用于 infra/ops。
- 代码搜索：Grep 优先。C# 精确符号用 Serena MCP。

---

## On-Demand Rules（触发条件满足时 MUST 读取对应文件）

| 触发条件 | 读取文件 |
|---|---|
| **任何编码任务（架构约束）** | `.claude/rules/architecture-redlines.md` |
| 写后端 C# 代码 | `.claude/rules/jnpf-expert-traps.md` + `.claude/rules/sql-safety.md` |
| 写前端 Vue3 代码 | `.claude/rules/jnpf-frontend-rules.md` |
| 前端实质性变更 / 需 E2E 验证 | `.claude/skills/playwright/SKILL.md`（产出 E1 截图证据） |
| 写 SSE / EventSource / WebSocket / setTimeout | `.claude/rules/frontend-memory-leak.md` |
| 修改自定义页面视觉样式（非生成） | `.claude/skills/jnpf-ui-enhance/SKILL.md` |
| 写架构文档 | `docs/architecture/ARCHITECTURE_DOC_RULES.md` |
| 收到任何编码任务 | `.claude/rules/workflow.md`（任务分级 + 七阶段流水线映射）|
| 遇到 bug / 测试失败 / 异常 / 编译错误 | `.claude/rules/debugging.md` + SP: `systematic-debugging` |
| **问题 10 分钟无进展 / 3 次修复仍无效** | **`/data-driven-debug`：停止改代码，抓运行时数据定位** |
| **前端无响应 / SSE 无数据 / 页面空白** | **Evidence Over Assumption：用 Playwright 抓网络响应体，禁止看源码猜测（详见 Core Principle）** |
| **犯错误后** | **MUST 追加到 `.claude/memory/mistake-log.md` 错题本**（格式：日期/类别/症状/根因/修复/关键词）|
| **编码前** | Grep `.claude/memory/mistake-log.md` 搜索当前任务关键词，避免重复错误 |
| 代码修改完成 / 准备声称"完成" | `.claude/rules/testing.md`（测试 Gate Function） |
| 任何编码任务（工程铁律） | `.claude/rules/engineering-laws.md`（Law 1-4） |
| 涉及 2+ 文件或 20+ 行变更 | `.claude/rules/review-workflow.md` + SP: `requesting-code-review` |
| 用户要求 "review" / "审查" / "跑测试" | `.claude/rules/review-workflow.md` + SP: `requesting-code-review` |
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

> **exit 2 = 硬阻断（L0，AI 无法绕过）；exit 1 = 警告（L1，AI 可忽略）**

| 时机 | Hook | 作用 | 层级 |
|---|---|---|---|
| SessionStart | `superpowers-check.mjs` | **Superpowers 强制激活验证** + 技能可用性检查 + AI 强制性指令 | — |
| SessionStart | `load-mistakes.mjs` | **错题本自动加载** — 注入最近 30 天错误到上下文 | — |
| PreToolUse (Write\|Edit\|MultiEdit) | `guard-write.mjs` | **三层守卫** — L1 密钥/凭证文件拦截 / L2 空文件拦截 / L3 安全扫描 | L0 |
| PreToolUse (Write\|Edit\|MultiEdit) | `guard-oa-module.mjs` | **R5 模块边界**拦截 OA/IoT/MES 写入 | L0 |
| PreToolUse (Write\|Edit\|MultiEdit) | `guard-sql-injection.mjs` | **R7 SQL 注入**拦截 | L0 |
| PreToolUse (Write\|Edit\|MultiEdit) | `guard-auth.mjs` | **R8 权限声明**拦截无授权 API | L0 |
| PreToolUse (Write\|Edit\|MultiEdit) | `guard-tenant-filter.mjs` | **R4 多租户**拦截 | L0 |
| PreToolUse (Write\|Edit\|MultiEdit) | `guard-frontend-leak.mjs` | **R6 前端泄漏**拦截 | L0 |
| PreToolUse (Bash) | `guard-bash.mjs` | 危险命令拦截 | L0 |
| PostToolUse (Write\|Edit\|MultiEdit) | `format-and-lint.mjs` | 自动 Prettier + ESLint | — |
| PostToolUse (Write\|Edit\|MultiEdit) | `skill-reminder.mjs` | **Superpowers 技能触发提醒**（重度变更后注入强制性技能调用指令） | — |
| Stop | `guard-finish.mjs` | 冒烟测试 + **E2E 证据智能阻断**（仅前端UI目录 + 4h时效 + 三级判定） | L0 |
| Stop | `collect-summary.mjs` | 会话变更摘要（7 类分类） | — |

> **Hook 分层架构：** 项目级 hooks（上表 12 个）受版本控制，全团队共享。
> 用户级 hooks 仅 3 个个人偏好（session-start, guard-deps, rtk-rewrite）。
> ⚠️ 禁止在用户级恢复 `guard-write`/`guard-finish`/`skill-reminder`/`collect-summary` — 功能已被项目级版本全覆盖，历史残留。
> 验证命令：`node scripts/test-hooks.mjs`（28 用例）

---

## Slash Commands

| 命令 | 用途 |
|---|---|
| `/start-dev` | 一键启动开发环境 |
| `/pre-commit` | 提交前检查 |
| `/security-review` | 安全审查 |
| `/spec` | 查询 OpenSpec 知识库 |
| `/learn` | 学习手册导航 |

---

## Git Iron Law

任何操作前工作树必须 clean / committed / pushed。Stash 不是长期存储。
