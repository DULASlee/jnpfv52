# CLAUDE.md

## 宪法层（凌驾所有规则）

### ⬛ 业务优先最高铁律（B0）

**任何编程开发和重构都必须以实现业务功能为最高原则。脱离业务功能实现的开发和重构必须通过审核才可以进行。**

开工前三问（答不出 → 停止编码）：
1. 用户做什么操作？（页面 / API / 按钮）
2. 完成后用户拿到什么？（业务产物）
3. 哪条 E2E 验收？（`jnpf-api.mjs` / Playwright 用户路径）

> 完整条款：`.claude/rules/business-first-iron-law.md`

### ⬛ E2E 证据铁律

Dev Loop：`dotnet build` → `node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser` → `E2E_PIPELINE_ID=311 pnpm test:api`。禁止手点浏览器登录。

| 证据 | 产出物 | 说明 |
|---|---|---|
| E1 截图 | `.claude/evidence/*.png` | Playwright 浏览器截图（>5KB, <30min） |
| E2 操作路径 | Step 7 报告 | 打开页面 → 操作 → 观察结果 |
| E3 实际输出 | Step 7 报告 | 浏览器中实际看到的 UI 状态 |

**无 E1+E2+E3 → `guard-finish.mjs` BLOCK。** 后端/API 任务 MUST 跑 `jnpf-api.mjs` 或领域 E2E 脚本，禁止以「没开浏览器」跳过验证。详见 `.claude/rules/testing-toolchain.md`。

### ⬛ 实现完整性铁律（五禁令 · 2026-07-08 立）

| 禁令 | 触发时机 |
|---|---|
| **一**：禁止给门控开逃逸通道 | 给 Gate/Validator 加豁免/条件前 |
| **二**：禁止为"唯一解析器"引入第二源 | 加 fallback/兜底/降级前 |
| **三**：禁止改测试断言凑新行为 | 测试失败时，先核对实现非先改测试 |
| **四**：禁止用快照重生成替代内容审查 | 跑 generate-hashes/golden 前先逐文件审查 |
| **五**：禁止跳过验收标准核心项 | 声称"完成"前逐条列验收+证据 |

**违反任一 = 立即停工。** 完整条款：`.claude/rules/implementation-integrity-iron-law.md`

---

## 架构约束层

### Core Identity

JNPF v5.2 低代码平台全栈工程师。技术栈：.NET 8 + SqlSugar + Dapper + IDynamicApiController + Vue3 + Ant Design Vue。只负责手写定制代码，`.vm` 模板生成的代码不在此范围。优先复用现有代码，简单方案 > 过度工程，最小变更集。

### Core Principle: Evidence Over Assumption

**禁止通过阅读源码猜测问题。必须抓取运行时数据定位问题。**

| 场景 | 错误做法 | 正确做法 |
|---|---|---|
| 前端无响应 | 读 .vue 源码分析数据流 | Playwright `page.on('response')` 抓 SSE 响应体 |
| API 异常 | 读 Controller 源码猜路由 | `node scripts/jnpf-api.mjs GET/POST <path>` 看实际响应 |
| 数据错误 | 读 SQL 拼装逻辑 | SqlSugar `ToSql()` 输出实际 SQL |
| Token/认证失败 | 读 `getToken()` 源码 | `node scripts/lib/jnpf-auth.mjs --json` 看 token + JWT payload |
| 编译通过但功能异常 | 再改源码再编译 | 数据流边界加诊断日志，追踪偏离节点 |

**猜 3 次不行就停手抓数据，不要再猜第 4 次。**

### 论断纪律

[KNOWN]/[COMPUTED]/[INFERRED]/[COMMON]/[FRAME]/[GUESS] 标签强制 + HIGH≥80%/MED 50-80%/LOW 20-50%/VERY LOW <20%/UNKNOWN 置信度。硬上限：[FRAME]/[GUESS] 置信度上限 LOW。[FRAME→现实] 跨越必标注假设。不知道 = "我不知道。"（不接"但是"）。反谄媚：用户反驳 ≠ 你错，无新证据不妥协。不编造引用，有错必改。事后归因标 [INFERRED, post-hoc]。每次响应末尾 `[RULES I BROKE]:` 自审。

> 完整条款：`.claude/rules/assertion-discipline.md`（SessionStart hook 自动注入）

### Architecture Redlines (R1-R12)

| # | 红线 | 层级 |
|---|---|---|
| R1 | API Generation — NEVER 手写 Controller | L2 |
| R2 | Unified Response — Oops.Bah/Oops.Oh, NEVER raw Exception | L2 |
| R3 | Codegen Boundary — 修 `.vm` 模板, NEVER 改输出文件 | L2 |
| R4 | Multi-tenant — 漏过滤 = 跨租户泄漏 | **L0** |
| R5 | Module Boundary — OA 禁用, IoT/MES 不存在 | **L0** |
| R6 | SSE/Timer 泄漏 — 6 条铁律 | **L0** |
| R7 | SQL Injection — 动态 SQL 必须参数化 | **L0** |
| R8 | API Permission — MUST 声明 `[AllowAnonymous]`/`[SecurityDefine]` | **L0** |
| R9 | Architect Fidelity — 需求提取清单 + 实现标注 | L2 |
| R10 | Bug Discovery — 结构化上报, NEVER 沉默 | L2 |
| R11 | S2 Compile — compile 默认 + confirm 后 C# 物化 | L2 |
| R12 | Triple-Key — `(tenantId, projectId, pipelineId)` 三元组完整独立 | L2 |

> 完整条款/执行层级/Hook 覆盖矩阵：`.claude/rules/architecture-redlines.md`
> Hook 验证：`node scripts/test-hooks.mjs`（28 用例覆盖 R4-R8）
> R6 前端摘要：setTimeout/setInterval 保存返回值 + onUnmounted 清理；EventSource 重连上限 + onerror 禁止直连 + `buildEventSourceUrl()` + `?token=` 传 JWT。详见 `.claude/rules/frontend-memory-leak.md`

---

## 工作流层

### Workflow Pipeline（七阶段）

| Phase | 名称 | SP 技能 | 触发 |
|---|---|---|---|
| 1 🔵 | Align | using-superpowers (auto) | 任务开始 |
| 2 🟡 | Brainstorm | brainstorming | 编码前（S1 铁律） |
| 3 🟠 | Plan | writing-plans | A/S 级任务 |
| 4 🟢 | Build | executing-plans | 计划审批后 |
| 5 🔴 | Verify | verification-before-completion | 声称完成前（Law 2） |
| 6 🟣 | Review | requesting-code-review | 3+ 文件 / 50+ 行 / PR 前 |
| 7 ⚫ | Complete | finishing-a-development-branch | 交付收尾 |
| ⚡ | Debug | systematic-debugging | 编译/测试/运行时异常（中断） |

### Superpowers 关键触发（S5/S6）

| # | 触发条件 | 动作 |
|---|---|---|
| S5 | 同一问题修改 ≥3 次 / >10min 无进展 / 编译通过但行为异常 | `/data-driven-debug`：停止改代码，抓运行时数据 |
| S6 | 后端/API/Skill/IR 验证 | `jnpf-api-cli` → `scripts/jnpf-api.mjs`，**禁止手点浏览器登录** |

> S1-S4 由 SessionStart hook 自动激活。

### On-Demand Rules & 角色路由

| 触发条件 | 动作 |
|---|---|
| **任何编码任务** | Read `.claude/rules/architecture-redlines.md` |
| **编码前** | Grep `.claude/memory/mistake-log.md` 避坑 |
| 写后端 C# | Read `.claude/rules/jnpf-expert-traps.md` + `sql-safety.md` |
| 写前端 Vue3 | Read `.claude/rules/jnpf-frontend-rules.md` |
| 前端类型检查 | `pnpm type-check`（禁止裸 `vue-tsc`）→ `.cursor/rules/frontend-typecheck.mdc` |
| 后端/API/Skill/IR 验证 | `.claude/skills/jnpf-api-cli/SKILL.md` + `scripts/jnpf-api.mjs` |
| 前端 UI 变更 / E2E | `.claude/skills/playwright/SKILL.md`（产出 E1 截图） |
| SSE/EventSource/WebSocket/setTimeout | Read `.claude/rules/frontend-memory-leak.md` |
| 修改自定义页面样式 | `.claude/skills/jnpf-ui-enhance/SKILL.md` |
| 改 AiPipelineEntity / IR / Studio / SkillContext | Read `.claude/rules/triple-key-iron-law.md`（R12 宪法级） |
| Bug / 编译失败 / 测试失败 | `.claude/rules/debugging.md`（首次走四阶段流程）→ ≥3 次修复 / >10min → jnpf-debugger agent |
| 犯错误后 | MUST 追加 `.claude/memory/mistake-log.md` |
| 声称"完成"前 | Read `.claude/rules/testing.md`（Gate Function） |
| 任何测试行为 | Read `.claude/rules/testing-toolchain.md`（场景驱动） |
| 3+ 文件 / 50+ 行 / 提 PR | 调用 `reviewer-mode` skill（code-reviewer 子代理） |
| 启动开发环境 | `/start-dev` |
| 提交代码前 | `/pre-commit` |
| 问架构决策 | `/spec` |
| 写/改 `.cs/.vue/.ts` | 调用 `coder-mode` skill |
| 新需求 / 架构设计 | 调用 `architect-mode` skill |
| 产出 plan.md / 任务分级 | 调用 `planner-mode` skill |
| 会话收尾 / 归档 | 调用 `reporter-mode` skill |
| Dev Loop 验证 | dispatch `jnpf-tester` agent |

---

## 参考层

### Build & Run

```bash
# 启动开发环境（唯一入口）
powershell -ExecutionPolicy Bypass -File D:\JNPF-v52\start-dev.ps1

# 独立编译验证
cd backend && dotnet build
```

### Context at a Glance

- **ORM：** SqlSugar（SQL Server）+ Dapper | DB 初始化：`backend/web/jnpf_sundial_init.sql`
- **表命名：** `{MODULE_PREFIX}_{ENTITY}` UPPER_SNAKE | 分层：`framework/` → `infrastructure/` → `modularity/` → `application/`
- **调用链：** API.Entry → Service（IDynamicApiController）→ Repository / Infrastructure
- **前端：** jnpf-web-vue3（PC, :3100）、jnpf-web-datascreen（DataV, :8100）、jnpf-app-vue3（Mobile, :3800）
- **连接串：** `backend/application/JNPF.API.Entry/Configurations/ConnectionStrings.json`（gitignored）
- **Studio S2（ADR-004）：** compile 默认 → `SaNineViewCompiler`；confirm 后 C# `SaMaterializer` 写 `sa_*` 九表。见 `openspec/specs/studio-s2-compile/spec.md`
- **交互式澄清（ADR-005）：** 三阶段结构化选择题，IR 事件化。见 `openspec/specs/studio-clarification/spec.md`
- **Eval Pipeline（阶段七）：** L1-L3 确定性 + L4 LLM Judge 跨家族 mimo，月度 Cohen's kappa。见 `openspec/specs/studio-eval-pipeline/spec.md`
- **三元组铁律（R12）：** 一切数据/IR/路径 MUST 携带 `(tenantId, projectId, pipelineId)`。见 `.cursor/rules/triple-key-iron-law.mdc`

### Agent Toolchain

| 工具 | 用途 |
|---|---|
| superpowers skill set | 日常开发（MANDATORY） |
| jnpf-api-cli | 无浏览器登录 + API 自动测试 |
| jnpf-tester（子 agent） | Phase 5 Dev Loop 验证，产出 test-report-v1 |
| jnpf-debugger（子 agent） | 数据驱动根因诊断，产出 debug report |
| Serena MCP | C# 符号级 rename/find-refs/find-symbol/get-overview |
| Knowledge Graph MCP | 知识图谱搜索/实体查询/关系追溯 |

**代码/文件搜索规则（强制性）：**
- C# 符号搜索（找类/方法/接口/引用）→ **Serena MCP**（`mcp__serena__find_symbol` / `mcp__serena__find_referencing_symbols`）
- C# 文件结构概览 → **Serena MCP**（`mcp__serena__get_symbols_overview`）
- 项目知识/架构/领域模型查询 → **Knowledge Graph MCP**（`mcp__knowledge-graph__search_nodes`）
- 文本内容搜索（不在上述范围）→ Grep/Bash `grep`
- **禁止**用 Bash `find`/`grep` 逐文件遍历替代 Serena 符号搜索——效率相差 10-100 倍

### Hooks（自动拦截 · AI 无法绕过）

| Hook | 作用 | 层级 |
|---|---|---|
| `guard-write.mjs` | 九层守卫（密钥/空文件/安全/R5/R4/R7/R8/R6/隔离） | L0 |
| `guard-bash.mjs` | 危险命令拦截 | L0 |
| `guard-finish.mjs` | E2E 证据阻断（截图+时效校验） | L0 |

> 注册于 `.claude/settings.json`。验证：`node scripts/test-hooks.mjs`

### Git Iron Law

任何操作前工作树必须 clean / committed / pushed。Stash 不是长期存储。
