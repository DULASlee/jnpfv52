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
| API 异常 | 读 Controller 源码猜路由/参数 | `node scripts/jnpf-api.mjs GET/POST <path>` 看实际 URL、状态码、响应体 |
| 数据错误 | 读 SQL 拼装逻辑 | 看 SqlSugar `ToSql()` 输出的实际 SQL |
| Token/认证失败 | 读 `getToken()` 源码 | `node scripts/lib/jnpf-auth.mjs --json` 看 token + JWT payload |
| 编译通过但功能异常 | 再改源码再编译 | 在数据流边界加诊断日志，观察实际输入输出 |

**排除步骤**：当一个问题耗时超过 10 分钟仍未解决，MUST 停止修改源码，切换到数据采集模式——在数据链路的关键节点采集实际值，追踪到哪个节点的输出偏离预期。修复那个节点，而非下游节点。**猜 3 次不行就停手抓数据，不要再猜第 4 次。**

### Data-Driven Debug 工具链 → souls/debugger/soul.md

四件套（full-fidelity-debug / visual-debug / agent-probe / netcoredbg-mcp）+ mistake-rag + 采集优先级 + 错题本查询详见 `.claude/souls/debugger/soul.md` §10。

---

## 🔄 自动测试闭环 → souls/tester/soul.md

Dev-Deploy-Debug Loop（`dotnet build` → `node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser` → `E2E_PIPELINE_ID=311 pnpm test:api`）+ 登录协议（MD5+AES → `/api/oauth/Login`）+ 工具链 + 禁止清单详见 `.claude/souls/tester/soul.md` §7-8。编码侧引用见 `.claude/souls/coder/soul.md` §9。

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
| S6 | **无浏览器 API 自动测试** | 后端/API/Skill/IR 验证、需 Token 调接口、Dev-Deploy-Debug 循环 | `jnpf-api-cli` → `scripts/jnpf-api.mjs` |

**违反任一 = Supreme Iron Law 验收不通过。无例外。**

> S6：**禁止手点浏览器登录。** 用 `node scripts/lib/jnpf-auth.mjs` 或 `python scripts/jnpf_auth.py` 拿 Token，再 `node scripts/jnpf-api.mjs` 调接口。详见下方「自动测试·自动修复闭环」。

> S5 自检：每当你准备"再改一下试试"、脑中出现"可能是 X 的问题，先改了再说"、或已经为同一个 bug 改了 2 次——STOP。调用 `/data-driven-debug`，抓运行时数据，让数据告诉你根因。

> 插件已由项目 `settings.json` 强制启用（`superpowers@superpowers-marketplace`）。
> SessionStart hook `superpowers-check.mjs` 验证插件激活状态。
> 共享约束 `souls/_shared/` 在每次 Soul 加载时自动注入论断纪律 + 错题本避坑 + 调试纪律。

---

---

## ⬛ Supreme Iron Law — 战略对齐（最高层级，凌驾所有规则）

> **双轨验收：** 日常 Dev Loop 用 **无浏览器 API 脚本**；**前端 UI 变更 / 阶段交付** 用 Playwright 浏览器证据。
>
> 后端/API/Skill 任务：**禁止**以「没开浏览器」跳过验证——MUST 跑 `jnpf-api.mjs` 或领域 E2E 脚本。

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
| "API 返回 200"（无脚本输出/无 Playwright） | 口头声称不可审计；后端任务须附 `jnpf-api.mjs` 或 E2E 脚本 exit 0 输出 |
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

## 论断纪律（宪法级 — 速记卡）

> **完整条款（自动加载）：** `.claude/rules/assertion-discipline.md`
> **souls 加载指针：** `.claude/souls/_shared/assertion-discipline.md`

**9 条铁律一句话：**
1. 打标签 — API/库/类/版本 MUST 前置 [KNOWN]/[COMPUTED]/[INFERRED]/[COMMON]/[FRAME]/[GUESS]
2. 定置信度 — HIGH≥80% / MED 50-80% / LOW 20-50% / VERY LOW <20% / UNKNOWN
3. 硬上限 — [FRAME] 和 [GUESS] 置信度上限 LOW
4. [FRAME→现实] 跨越必标注假设
5. 不知道 = "我不知道。"（不接"但是"）
6. 反谄媚 — 用户反驳 ≠ 你错，无新证据不妥协
7. 不编造引用 / 有错必改（公开修正，悄悄改口 = 编造）
8. 事后归因标 [INFERRED, post-hoc]
9. 每次响应末尾 `[RULES I BROKE]:` 自审

---

## 角色体系入口（souls + skills 活性注入）

工作流角色定义存放于 `.claude/souls/{role}/soul.md`（8 角色：orchestrator/architect/planner/coder/tester/reviewer/reporter/debugger）。souls 不自动加载 — 按下表触发条件 **主动调用对应 skill 或 dispatch agent**。

### 活性注入路由表

| 触发条件 | 角色 | 注入方式 |
|---|---|---|
| 写/改 `.cs` `.vue` `.ts` 代码 | Coder | 调用 `coder-mode` skill |
| 收到新需求 / 设计架构 / 产出 architecture.md | Architect | 调用 `architect-mode` skill |
| 产出 plan.md / 任务分级 / 需求提取清单 | Planner | 调用 `planner-mode` skill |
| 主 Claude 自审代码 / 3+ 文件变更 / 提 PR | Reviewer | 调用 `reviewer-mode` skill（隔离审查走 dispatch `code-reviewer`，prompt 含 Read soul）|
| 产出 delivery_report.md / 会话收尾 / 归档 | Reporter | 调用 `reporter-mode` skill |
| 后端/API/Skill/IR 验证 · Dev Loop | Tester | dispatch `jnpf-tester` agent |
| 编译失败/测试失败/≥3次修复无效/>10min 无进展 | Debugger | dispatch `jnpf-debugger` agent 或 `/data-driven-debug` |
| 任务流转判定（缺哪个产出物）| Orchestrator | Read `souls/orchestrator/soul.md`（主 Claude 默认扮演）|

### workspace/ 产出物流转

```
requirements.md → architecture.md → plan.md → code_changes.md → test_report.md → review_report.md → delivery_report.md → _completed/{任务名}-{时间戳}/
```

任一阶段编译失败/测试失败/运行时异常 → 切 Debugger（产出 debug_report.md）→ 返回断点。

> 角色切换状态机详情（workspace/ 结构 / 角色判定表 / 隔离 / 收尾 / 自动流转 / 人工介入）：`.claude/souls/orchestrator/soul.md` §7-8

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
| Debug | ⚡ | Debugger（中断） | —（自动切入，不占 Phase） |

### Phase 明细 → 各角色 soul

| Phase | 颜色 | SP 技能 | 明细位置 |
|---|---|---|---|
| Entry | — | using-superpowers (auto) | `souls/architect/soul.md` §7 |
| 1 | 🔵 | Align — using-superpowers | `souls/architect/soul.md` §8 |
| 2 | 🟡 | Brainstorm — brainstorming（S1 铁律） | `souls/architect/soul.md` §9 |
| 3 | 🟠 | Plan — writing-plans | `souls/planner/soul.md` §7 |
| 4 | 🟢 | Build — executing-plans | `souls/coder/soul.md` §7 |
| 5 | 🔴 | Verify — verification-before-completion | `souls/tester/soul.md` §8 |
| 6 | 🟣 | Review — requesting-code-review | `souls/reviewer/soul.md` §8 |
| 7 | ⚫ | Complete — finishing-a-development-branch | `souls/reporter/soul.md` §7 |
| Debug | ⚡ | systematic-debugging / data-driven-debug | `souls/debugger/soul.md` §10-11 |

> 每 Phase 的 Rule / Skill / Hook 明细见对应 soul。Phase 抬头声明模板见各角色 soul 顶部。
> 本节上方的「强制抬头声明」+「颜色/阶段对应」总表保留为全局骨架；明细全部下沉各 soul。

---

## Architecture Redlines (NEVER VIOLATE)

> **完整条款、执行层级、关联陷阱、Hook 覆盖矩阵** → `.claude/rules/architecture-redlines.md`（架构铁律单一信源）

| # | 红线 | 层级 | 强制机制 |
|---|---|---|---|
| R1 | API Generation — NEVER 手写 Controller | L2 | code-reviewer |
| R2 | Unified Response — Oops.Bah/Oops.Oh, NEVER raw Exception | L2 | code-reviewer |
| R3 | Codegen Boundary — 修 `.vm` 模板, NEVER 改输出文件 | L2 | code-reviewer |
| R4 | Multi-tenant — 漏过滤 = 跨租户泄漏 | **L0** | `guard-write.mjs` L5 |
| R5 | Module Boundary — OA 禁用, IoT/MES 不存在 | **L0** | `guard-write.mjs` L4 |
| R6 | SSE/Timer 泄漏 — 6 条铁律 → `.claude/rules/frontend-memory-leak.md` | **L0** | `guard-write.mjs` L8 |
| R7 | SQL Injection — 动态 SQL 必须参数化 → `.claude/rules/sql-safety.md` | **L0** | `guard-write.mjs` L6 |
| R8 | API Permission — MUST 声明 `[AllowAnonymous]`/`[SecurityDefine]` | **L0** | `guard-write.mjs` L7 |
| R9 | Architect Fidelity — 需求提取清单 + 实现标注 | L2 | code-reviewer |
| R10 | Bug Discovery — 结构化上报, NEVER 沉默 → `.claude/rules/engineering-laws.md` Law 1 | L2 | code-reviewer |
| R11 | S2 Compile — compile 默认 + confirm 后 C# 物化；禁止 S2 写九表 / sa-service 写主库 | L2 | code-reviewer · ADR-004 |
| R12 | Triple-Key — `(tenantId, projectId, pipelineId)` 三元组在所有层 MUST 完整独立可分离；多用户/多项目/多对话/fork/冻结拉起的存在前提 | L2 | code-reviewer · 宪法级 |

> **合规测试：** `node scripts/test-hooks.mjs`（28 用例覆盖 R4/R5/R6/R7/R8 + 基础守卫 + MultiEdit）

---

## Review Gate → souls/orchestrator/soul.md + souls/reviewer/soul.md

审查计数器（Write/Edit ≥2 触发 code-reviewer）+ 子代理 dispatch 路由（Phase 5 → `jnpf-tester`，Debug → `jnpf-debugger`）+ todo 强制注入（🔍 代码审查 + 📝 错题本追加）详见 `.claude/souls/orchestrator/soul.md` §8 + `.claude/souls/reviewer/soul.md` §9。

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
- **Studio S2（ADR-004）：** compile 默认 → `SaNineViewCompiler` 九视图；confirm 后 C# `SaMaterializer` 写 `sa_*` 九表；**compile 主链不需 sa-service**。见 `openspec/specs/studio-s2-compile/spec.md` · `.cursor/rules/studio-s2-compile.mdc`
- **交互式澄清问答（ADR-005）：** 需求分析/架构设计/总体设计三阶段，LLM 产出结构化选择题（单选/多选/文本，每轮 3-5 题，末项恒为"其他"+文本框）让用户逐条细化需求；关键题（required）硬门控推进；完整 IR 事件化（`ClarificationRequested`/`ClarificationAnswered`）；默认 3-7 轮（`Clarification:MaxRounds`），可"全部跳过"。见 `openspec/specs/studio-clarification/spec.md` · `.cursor/rules/studio-clarification.mdc`
- **Eval Pipeline 四层评估（阶段七）：** L1 组件/L2 轨迹/L3 任务 确定性（无 LLM，fail-fast 跳过 L4）；L4 `LlmJudgeService` 经 `SkillLlmBudgetGuard` fast tier 路由**跨家族 mimo**（生成走 deepseek），pass/fail 二元；`JudgeCalibrationService` 月度 Cohen's kappa 校准（<0.6 降级 advisory）；人工抽检双写 `BASE_AI_SKILL_REVIEW` + `SkillReviewRecorded` IR 事件；质量榜 SQL 聚合 grade A/B/C/D；失败 trace 回写 GoldenSet（生产 trace→eval 闭环）。见 `openspec/specs/studio-eval-pipeline/spec.md` · `.cursor/rules/studio-eval-pipeline.mdc`
- **三元组铁律（R12 · 宪法级）：** AI 原生开发一切数据/IR/路径/SkillContext MUST 携带 `(tenantId, projectId, pipelineId)`，三者完整、独立、可分离。1 tenant → N projects → M pipelines（greenfield/bugfix/enhancement，可 fork/freeze/resume）。违反 = 多用户多项目多对话功能形同虚设。见 `.cursor/rules/triple-key-iron-law.mdc` · `.claude/rules/triple-key-iron-law.md` · `architecture-redlines.md` §R12

---

## Agent Toolchain

| 工具 | 角色 | 编码？ |
|---|---|---|
| superpowers skill set | 日常开发（**MANDATORY** — 违反 S1-S6 = 验收不通过） | ✅ |
| **jnpf-api-cli** | 无浏览器登录 + API 自动测试闭环 | ✅（Shell） |
| **jnpf-tester**（子 agent） | Phase 5 Verify — Dev Loop 验证，产出 test-report-v1 JSON | ❌（只验证） |
| **jnpf-debugger**（子 agent） | Debug Path — 数据驱动根因诊断，产出 debug report | ❌（只诊断） |
| Serena | C# 符号级 rename/find-refs | ✅ |
| OpenSpec | 知识库 | ❌ |
| episodic-memory | 跨会话上下文 | ❌ |

- NEVER 用 `/opsx:apply` 改代码 — 绕过 code review。仅用于 infra/ops。
- 代码搜索：Grep 优先。C# 精确符号用 Serena MCP。

---

## On-Demand Rules（触发条件满足时 MUST 读取对应文件）

> 🆕 **角色 soul 活性注入路由**：见上方「角色体系入口」节 — coder/architect/planner/reviewer/reporter 调 `*-mode` skill；tester/debugger dispatch agent。下表触发条件与之联动。

| 触发条件 | 读取文件 |
|---|---|
| **任何编码任务（架构约束）** | `.claude/rules/architecture-redlines.md` |
| **改 AiPipelineEntity / IR 投影 / Studio 路径 / Skill 入口 / SkillContext** | `.claude/rules/triple-key-iron-law.md`（R12 宪法级，三元组铁律） |
| 写后端 C# 代码 | `.claude/rules/jnpf-expert-traps.md` + `.claude/rules/sql-safety.md` |
| 写前端 Vue3 代码 | `.claude/rules/jnpf-frontend-rules.md` |
| **前端类型检查 / Dev Loop 验证** | `.cursor/rules/frontend-typecheck.mdc`（`pnpm type-check`；禁止裸 `vue-tsc`） |
| **后端/API/Skill/IR 验证 · Dev Loop** | `.claude/skills/jnpf-api-cli/SKILL.md` + `scripts/jnpf-api.mjs`（**禁止手点浏览器登录**） |
| 前端实质性变更 / 需 E2E 验证 | `.claude/skills/playwright/SKILL.md`（产出 E1 截图证据） |
| 写 SSE / EventSource / WebSocket / setTimeout | `.claude/rules/frontend-memory-leak.md` |
| 修改自定义页面视觉样式（非生成） | `.claude/skills/jnpf-ui-enhance/SKILL.md` |
| 写架构文档 | `docs/architecture/ARCHITECTURE_DOC_RULES.md` |
| 收到任何编码任务 | `.claude/rules/workflow.md`（任务分级 + 七阶段流水线映射）|
| 遇到 bug / 测试失败 / 异常 / 编译错误 | **自动切 Debugger**（`souls/debugger/soul.md`）+ `.claude/rules/debugging.md` |
| **问题 10 分钟无进展 / 3 次修复仍无效** | **`/data-driven-debug`：停止改代码，抓运行时数据定位** |
| **前端无响应 / SSE 无数据 / 页面空白** | **Evidence Over Assumption：用 Playwright 抓网络响应体，禁止看源码猜测（详见 Core Principle）** |
| **犯错误后** | **MUST 追加到 `.claude/memory/mistake-log.md` 错题本**（格式：日期/类别/症状/根因/修复/关键词）|
| **编码前** | Grep `.claude/memory/mistake-log.md` 搜索当前任务关键词，避免重复错误 |
| 代码修改完成 / 准备声称"完成" | `.claude/rules/testing.md`（测试 Gate Function） |
| **任何测试行为（用什么工具、跑什么命令）** | **`.claude/rules/testing-toolchain.md`（AI 模型执行手册，场景驱动）** |
| 遇到 Bug / 测试失败 / 不知用什么工具排查 | `.claude/rules/testing-toolchain.md` §场景 D（Bug 诊断）+ §场景 B（前端） |
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
| SessionStart | `session-scheduler.mjs` | 智能调度入口（superpowers 激活验证 + 技能可用性 + 错题本注入 + AI 强制指令） | — |
| PreToolUse (Write\|Edit\|MultiEdit) | `guard-write.mjs` | **统一九层守卫** — L1密钥 / L2空文件 / L3安全扫描(eval/命令注入/XSS/弱加密) / L4模块边界R5 / L5多租户R4 / L6注入R7 / L7权限R8 / L8前端泄漏R6 / L9工作区隔离 | L0 |
| PreToolUse (Bash) | `guard-bash.mjs` | 危险命令拦截（rm -rf / DROP / git push --force 等） | L0 |
| PreToolUse (Skill) | `guard-skill-load.mjs` | Skill 限速，防 Skill 风暴 | — |
| PostToolUse (Write\|Edit\|MultiEdit) | `guard-reviewer.mjs` | Reviewer L0 预筛选（生成 `.claude/review/flags/` 标志供 Reviewer L1 读取） | — |
| Stop | `guard-finish.mjs` | 冒烟测试 + **E2E 证据智能阻断**（仅前端UI目录 + 4h时效 + 三级判定） | L0 |

> **Hook 注册：** 项目 hooks 注册在 `.claude/settings.json`（版本控制、团队共享），命令用 `$CLAUDE_PROJECT_DIR` 定位。用户级 `~/.claude/settings.json` 仅 3 个个人偏好（session-start, guard-deps, rtk-rewrite），与项目 hooks 合并运行（不冲突）。
> **guard-write 九层：** L1-L3 通用防护 + L4-L8 五条 L0 红线（R5/R4/R7/R8/R6）+ L9 AI 开发态工作区隔离。独立 guard（oa/sql/auth/tenant/leak）已合并为 L4-L8（2026-07-07 完成，原 cf5ac57d 删除后未迁移的缺口已补）。
> 验证命令：`node scripts/test-hooks.mjs`（28 用例：R5(4)+R8(3)+R7(3)+R4(5)+R6(4)+GW(6)+GB(3)）

---

## Slash Commands

| 命令 | 用途 |
|---|---|
| `/start-dev` | 一键启动开发环境 |
| `node scripts/jnpf-api.mjs` | 无浏览器 Token 调任意 API |
| `node scripts/phase2-skills-e2e.mjs` | 阶段二 HTTP E2E |
| `/pre-commit` | 提交前检查 |
| `/security-review` | 安全审查 |
| `/spec` | 查询 OpenSpec 知识库 |
| `/learn` | 学习手册导航 |

---

## Git Iron Law

任何操作前工作树必须 clean / committed / pushed。Stash 不是长期存储。
