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

### ⬛ 全链条冲刺铁律（2026-07-11 立）

**按阶段推进验收；不得用全链冒烟顶替阶段正确性。**

| # | 铁律 | 要点 |
|---|------|------|
| **F1** | 分阶段 + 核心 xUnit | 每 SG 验收核心功能/产出物；确定性核心必须有单测验证业务准确性 |
| **F2** | 数据一致性 | IR Write Model；`ai_entity_field` 字段唯一源；投影契约无漂移 |
| **F3** | 排除旧实现干扰 | 切断旧 Analyst/九步/parse-IR 字段源/311 假绿 |
| **F4** | 全链冒烟置后 | SG0–SG7 + CONTRACT 全绿后才 W3 |

**顺序：** `W0 → SG-CONTRACT → SG0…SG7 → W3`。细则：`.claude/rules/fullchain-sprint-iron-law.md` · `.cursor/rules/iron-laws/fullchain-sprint-iron-law.mdc` · 30 号计划。

### ⬛ 需求分析子链铁律（2026-07-12 立）

**一切编码以阶段 A-B-C 为唯一施工依据（`1、阶段A.md` / `2、阶段B.md` / `3、阶段C.md`）；旧 25–33 号已废止归档。未经 CR 擅自修改关键业务方法 = 最严重越权。**

| # | 禁令 | 要点 | 机器检测 |
|---|------|------|----------|
| 一 | 禁止新增 .mjs 脚本 | 除 hooks 目录外；现有 mjs 冻结迁移 xUnit/Vitest .ts | guard-write L10c ✅ |
| 二 | 数据一致性 | IR=Write Model；ai_entity_field=字段唯一源；sa_*=投影禁手改 | L10d + xUnit ✅ |
| 三 | 逐阶段推进 | 门控→需求分析→架构→总体设计→开发→测试→debug→沙箱 | 审批 + L10 ✅ |
| 四 | 以阶段 A-B-C 为总纲 | 编码对照阶段 A/B/C；偏离先改文档再改代码 | 人审 📋 |
| 五 | 功能点验收 | 每功能点 = xUnit 绿 + 业务证据 + 用户审批；全验收后才联调 | xUnit + 审批 ✅ |
| 六 | CR 变更审批 | 关键业务方法修改前 MUST 提交 CR（保护清单见铁律文件） | guard-write L10a ✅ |
| 七 | 禁止复活废止模块 | ScannerValidator/cascadeUpdate/sa_ddd/Q1-Q9/编排器代问/普通SINGLE | L10b + JNPF007/008 ✅ |

**关键业务方法保护清单：** PmSkillService / RequirementAnalysisOrchestrator / AnalystSkillService / SkillsApiService / DesignSkillOrchestrator / Gates/* — 修改前写 `.claude/change-requests/CR-{日期}-{NN}.md`。

**完整条款：** `.claude/rules/req-analysis-iron-law.md` · `.cursor/rules/iron-laws/req-analysis-iron-law.mdc`

### ⬛ ADF 三先行（2026-07-12 立 · 全项目）

**S/A 级：架构先行 → 设计模式先行 → 接口契约先行 → 才允许实现；每阶段等用户「继续/通过」。**

| 阶段 | 要点 | 模板 |
|---|---|---|
| P0 | Business First Q1–Q3 | （复用业务优先铁律） |
| P1 | 层边界、唯一源、三元组、≥2 方案+failure_boundary、禁改清单 | `.cursor/templates/adf-architecture.md` |
| P2 | 1–2 模式映射 SkillHarness/Gate/IR/IDynamicApiController | `.cursor/templates/adf-patterns.md` |
| P3 | 签名/DTO/事件/错误契约，禁止方法体 | `.cursor/templates/adf-contracts.md` |
| P4 | 实现 + 节点审批 | 实现完整性铁律 |

B 级须声明 `ADF 豁免：B级 — …`。开 Chat 可粘贴 `.cursor/templates/task-kickoff.md`。

**零占位符硬失败：** `guard-write` L11 · Cursor `guard-placeholder` · `.githooks/pre-commit`；豁免 `// placeholder-ok: <理由>`。

**完整条款：** `.claude/rules/architecture-design-interface-first.md` · `.cursor/rules/iron-laws/architecture-design-interface-first.mdc`

### ⬛ 规则加载策略（2026-07-12 · 对抗约束衰减）

- **唯一 alwaysApply：** `.cursor/rules/00-constitution.mdc`；详规在 `iron-laws/` · `domain/` · `frontend/` · `toolchain/` · `docs/`（见 `.cursor/rules/README.md`）。
- **ADF 写入锁 L12：** `workflow-state.json` → `adfPhase=P0..P3`（或仅设 `currentSg`）时禁止写业务 `.cs/.vue`；`P4`/`exempt` 放行。
- **四支柱硬门：** 节点验收设 `awaitingNodeApproval`；填 `pillar-claim-current.json`；`pillar-claim-check.mjs --force`。

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
| R5 | Module Boundary — OA 独立入口（JNPF.OA.API.Entry）, IoT/MES 不存在 | **L0** |
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
| 前端类型检查 | `pnpm type-check`（禁止裸 `vue-tsc`）→ `.cursor/rules/frontend/frontend-typecheck.mdc` |
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

### Phase 8 表级重构 — Mandatory Skills（2026-08-30 强制规则）

> **R12 宪法级** — Phase 8 任务 MUST 主动加载下列 skill，不得跳过。

| 触发条件 | MUST 加载 | 备注 |
|---|---|---|
| **任何 Phase 8 表评估/重构/关闭** | `table-refactor-expert` | P1；Master Spec + Execution Manual 路由；不替代 Skill |
| **P8-B Controlled Production / 上线决策** | `production-audit` | P1；本地证据审计 |
| **Phase 7/8 context 缺失** | `jnpf-memory` (项目级替换 `unified-memory`) | P1；ECC Vault `ecc memory search "Phase 8"` recall |
| **声称完成 Phase 8 任一阶段前** | `verification-loop` | P2；Gate Function 验证 |
| **Phase 8 写/改 .cs/.vue/.ts** | `coder-mode` + `dotnet-patterns` | DI/async/EF Core/SqlSugar 规范 |
| **Phase 8 架构决策（如 schema 演进）** | `architect-mode` | Phase 1 Align 流程 |
| **Phase 8 跨 harness 知识传递** | `jnpf-memory`（写入；项目级替换幽灵 `unified-memory`） | `npx ecc memory save --kind context/decision/fact/handoff` |
| **Phase 8 复杂表 (base_user 68 列等)** | `rules-distill` | 跨表模式抽取 |

**Auto-load 机制**：SessionStart hook `session-skill-suggest.mjs` 会根据 Phase 上下文主动推荐上述 skill 列表到会话开头。LLM 应在 Phase 8 任务开始时立即加载 `table-refactor-expert`（不等待推荐）。

**Skill 路由质量保障**（2026-08-30 体检）：
- 23 项目 skills + 5 用户 skills + 7 OpenCode skills 全部 frontmatter 完整
- 95.7% 含 "Use when" 触发短语
- LoopX 10 个不稳定 skills 已删除（OpenCode 清理）
- `session-skill-suggest.mjs` SessionStart hook 主动推荐
- `session-summary-save.mjs` Stop hook 自动写 ECC Vault

---

## 参考层

### Build & Run

```bash
# 启动开发环境（唯一入口）
powershell -ExecutionPolicy Bypass -File D:\JNPF-v52\start-dev.ps1

# 独立编译验证
cd backend && dotnet build

# 后端质量门禁（2026-08 整改落地）
cd backend && dotnet build /p:CI_BUILD=true   # 含 JNPF009 复杂度硬门 + baseline
dotnet test --filter FullyQualifiedName~Architecture   # ARCH-01；Common.Core 硬失败
```

### Context at a Glance

- **ORM：** SqlSugar（SQL Server）+ Dapper | DB 初始化：`backend/web/jnpf_sundial_init.sql`
- **表命名：** `{MODULE_PREFIX}_{ENTITY}` UPPER_SNAKE | 分层：`framework/` → `infrastructure/` → `modularity/` → `application/`
- **调用链：** API.Entry → Service（IDynamicApiController）→ Repository / Infrastructure
- **前端：** jnpf-web-vue3（PC, :3100）、jnpf-web-datascreen（DataV, :3102）、jnpf-app-vue3（Mobile, :3800）
- **连接串：** `backend/application/JNPF.API.Entry/Configurations/ConnectionStrings.json`（gitignored）
- **Studio S2（ADR-004）：** compile 默认 → `SaNineViewCompiler`；confirm 后 C# `SaMaterializer` 写 `sa_*` 九表。见 `openspec/specs/studio-s2-compile/spec.md`
- **交互式澄清（ADR-005）：** 三阶段结构化选择题，IR 事件化。见 `openspec/specs/studio-clarification/spec.md`
- **Eval Pipeline（阶段七）：** L1-L3 确定性 + L4 LLM Judge 跨家族 mimo，月度 Cohen's kappa。见 `openspec/specs/studio-eval-pipeline/spec.md`
- **三元组铁律（R12）：** 一切数据/IR/路径 MUST 携带 `(tenantId, projectId, pipelineId)`。见 `.cursor/rules/iron-laws/triple-key-iron-law.mdc`

### Agent Toolchain

| 工具 | 用途 |
|---|---|
| superpowers skill set | 日常开发（MANDATORY） |
| jnpf-api-cli | 无浏览器登录 + API 自动测试 |
| jnpf-tester（子 agent） | Phase 5 Dev Loop 验证，产出 test-report-v1 |
| jnpf-debugger（子 agent） | 数据驱动根因诊断，产出 debug report |
| Serena MCP | C# 符号级 rename/find-refs/find-symbol/get-overview |
| Codebase-Memory MCP → **codegraph MCP** | 跨文件调用链(trace_path)/架构总览(get_architecture)/复杂度热点(query_graph Cypher)/BM25+向量搜索(search_graph)。**2026-08 升级：** Codebase-Memory v0.9.0 已被 codegraph v1.1.0 替代。 |
| Knowledge Graph MCP | 知识图谱搜索/实体查询/关系追溯（人工沉淀的领域知识） |

**代码/文件搜索规则（强制性）：**
- **针式搜索铁律** → `.cursor/rules/toolchain/needle-search.mdc` · `.claude/rules/needle-search.md`（先窄后宽 · 并行≤3 · 禁拖网 · >15s 收窄）
- **MCP 速查手册** → `.claude/rules/mcp-code-search.md`（三大 MCP 分工 + 参数速查 + 场景示例）
- C# 单符号搜索（找类/方法/接口/引用）→ **Serena MCP**（`mcp__serena__find_symbol` / `mcp__serena__find_referencing_symbols`）
- C# 文件结构概览 → **Serena MCP**（`mcp__serena__get_symbols_overview`）
- **跨文件调用链/影响分析** → **codegraph MCP**（`mcp__codegraph__trace_path` / `mcp__codegraph__search_graph`）✅ v1.1.0
- **项目架构/模块聚类/复杂度热点** → **codegraph MCP**（`mcp__codegraph__get_architecture` / `mcp__codegraph__query_graph`）✅ v1.1.0
- ~~**Codebase-Memory MCP**（`mcp__codebase-memory__*`）~~ ⚠️ **已废弃** — 2026-08 被 codegraph v1.1.0 替代；不再配置 `codebase-memory-mcp.exe`
- 领域知识/设计意图/历史决策查询 → **Knowledge Graph MCP**（`mcp__knowledge-graph__search_nodes`）
- 文本内容搜索 → Grep（必须带 path/glob）；已知路径直接 Read；文件名用窄 Glob
- **禁止** Shell 全仓搜索；**禁止**为找一个文件派 explore 子 Agent；**禁止**同轮 8+ 广域并行

### Hooks（自动拦截 · AI 无法绕过）

| Hook | 作用 | 层级 |
|---|---|---|
| `guard-write.mjs` | 十二层守卫（…/L10 需求铁律/L11 零占位符/L12 ADF 写入锁） | L0 |
| `guard-bash.mjs` | 危险命令拦截 | L0 |
| `guard-finish.mjs` | E2E 证据阻断（截图+时效校验） | L0 |

> 注册于 `.claude/settings.json`。验证：`node scripts/test-hooks.mjs`

### Git Iron Law

任何操作前工作树必须 clean / committed / pushed。Stash 不是长期存储。
