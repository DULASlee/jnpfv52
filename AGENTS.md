# AGENTS.md

Compact instruction file for automated coding agents working in this repository.

---

## 🚦 启动规则（每次响应前必读 — 凌驾一切默认行为）

> **来源：** `.agents/skills/using-superpowers/SKILL.md` + `.claude/rules/workflow-iron-law.md` (WORKFLOW-IRON-01)。本段是该元规则的项目级精简版。
> **优先级：** 用户明确指令 > 本启动规则 > 默认行为。

### 铁律 0：调用 superpowers 技能 = 第一动作（强制、不可跳过）

**BEFORE any response, including clarifications:**

1. **必调 `using-superpowers`**（meta-skill，告诉 agent 如何按场景路由 superpowers 技能集合）
2. **必调场景对应的 superpowers skill**（按下方路由表选 1 个或几个）— 不是判断"要不要调"，是"必调，由路由表决定调哪个"
3. `skill({ name: "..." })` 逐个调用
4. Announce: `🔧 Using [skill] to [purpose]`
5. Follow skill exactly — don't adapt away discipline

**核心原则**：

```
每次任务/响应：
  → 必调 superpowers 技能集合（不是 0 个，不是固定的 N 个）
  → 调哪个 = 按场景路由表
  → "判断要不要调" 本身 = 违规
```

**禁止措辞**（任一出现 = 违规）：
- ❌ "这次简单，跳过 skill"
- ❌ "skill 是 overkill"
- ❌ "我记得这个 skill"
- ❌ "上次调过这次不用"
- ❌ "等下再调"
- ❌ "先看代码再说"

### 场景 → superpowers 技能路由表（项目级映射）

| 触发场景 | 必调 superpowers skill | 配套项目 skill |
|---|---|---|
| **任何消息**（必做第 1 步） | `using-superpowers` | — |
| 模糊/新需求 / "帮我设计 XX" / 重构决策 | `brainstorming` → `writing-plans` | `.claude/skills/architect-mode` |
| 编码/实现（.cs / .vue / .ts） | `coder-mode`（TDD 例外见下） | — |
| Bug / 测试失败 / 异常 | `systematic-debugging` + `data-driven-debug`（10min 强制） | — |
| 派子 agent（多任务并行） | `dispatching-parallel-agents` / `subagent-driven-development` | — |
| 收到代码审查反馈 / 用户批评当前工作 | `receiving-code-review` | — |
| 写跨阶段 plan | `writing-plans` → `executing-plans` | `.claude/skills/planner-mode` |
| **声称完成**任何任务前 | `verification-before-completion`（5 步 Gate Function）| `.claude/rules/testing.md` |
| 任务结束 / 合并前 | `requesting-code-review` + `finishing-a-development-branch` | `.claude/skills/reporter-mode` |
| **跨会话记忆/保存到知识库/搜之前决策** | `unified-memory`（运行 `ecc memory search/save/handoff`） | — |
| 之前会话引用（"上次我们..."） | **必须**先 `ecc memory search "<keyword>"` 才回复 | — |

**使用模式**：

```
if 用户消息是新需求/设计:
    invoke("using-superpowers")
    invoke("brainstorming")
    invoke("writing-plans")

elif 用户消息是改代码:
    invoke("using-superpowers")
    invoke("coder-mode")

elif 用户消息是修 bug:
    invoke("using-superpowers")
    invoke("systematic-debugging")

elif 用户消息是批评/反馈:
    invoke("using-superpowers")
    invoke("receiving-code-review")

elif 用户消息说"上次我们..."或涉及之前决策:
    invoke("using-superpowers")
    invoke("unified-memory")
    run("ecc memory search <keyword>")

elif 用户消息声称完成:
    invoke("using-superpowers")
    invoke("verification-before-completion")

# ... etc
```

**核心**：调是必做的，调哪个由路由表决定——不调 = 违规。

### ⚠️ TDD 例外（本项目禁用纯 TDD 流程）

`test-driven-development` superpowers 强制 "test first"，但**本项目策略不同**：
- **禁用：** 先写失败测试 → 看红 → 写最小实现 → 看绿（流程性 TDD）
- **采用：** 先实现 → xUnit/Vitest 覆盖核心产出物 → Phase 6 Review 强制覆盖（见 `.claude/rules/implementation-integrity-iron-law.md`）
- **理由：** 项目 30 份铁律 + Phase 流水线已有强制测试环节；流程性 TDD 会与现有 ADF、节点审批、Req-Analysis 等冲突
- **仍可用：** 测试作为"先验证需求"手段（`verification-before-completion`）

### 双层控制（避免"打架"误解）

| 层 | 控制 | 文件 |
|---|---|---|
| **节奏层**（细粒度不打断） | HIP-01：默认连续，仅 4 类情况停 | `.claude/rules/agent-runtime-iron-laws.md` |
| **节点层**（关键阶段必停） | B0 业务优先 / ADF 三先行 / 实现完整性 / Req-Analysis / CR 变更 | `.claude/rules/*-iron-law.md` |

**关键节点（不可被 HIP-01 绕过）：** 业务锚定 Q1-Q3 未答出 / 架构方案未「继续」/ 实现完整性五禁令命中 / CR 保护方法变更 / **未调 superpowers 必调 skill**。

---

## Multi-Agent Environment

This repo is used by **multiple AI coding agents**. Each has its own instruction system — they must coexist without conflict:

| Agent | Instructions | Auto-loaded by |
|-------|-------------|----------------|
| **Claude Code** | `.claude/rules/*.md`, `.claude/skills/*/SKILL.md`, [CLAUDE.md](./CLAUDE.md) | Claude Code session start |
| **Cursor** | `.cursor/rules/*.mdc` (alwaysApply), `.cursor/skills/*/SKILL.md` | Cursor IDE |
| **Any agent** | This file (`AGENTS.md`) | OpenCode / other agents |

**Rules of coexistence:**
- This file is a **subset** of [CLAUDE.md](./CLAUDE.md) — it repeats only what an agent would guess wrong. CLAUDE.md remains the single source of truth.
- **Never delete or alter** `.claude/`, `.cursor/`, `.agents/` content from this agent — those are managed by their respective environments.
- Root `.cursorrules` is an IDE short-cache pointer only — never pile new rules into it; authoritative Cursor entry is `.cursor/rules/00-constitution.mdc`.
- **On-Demand Rules** (`.claude/rules/`) and **Cursor rules** (`.cursor/rules/`) contain deeper context. When available, prefer reading them over this summary.

## Project

JNPF v5.2 low-code platform — .NET 8 backend + Vue 3 frontends. Full architecture/rules in [CLAUDE.md](./CLAUDE.md).

## Dev Environment Startup

**Only** use the unified script — never `npm run dev` or `dotnet run` directly:

```powershell
powershell -ExecutionPolicy Bypass -File D:\JNPF-v52\start-dev.ps1
```

**Single-stack lock (v4.2):** if any of 3100/3102/3800/5000/3001 is already listening, a second `start-dev` **exits 2** (does not kill/steal). Stop with `-CleanupOnly`; restart with `-Force`. Concurrent `start-dev` processes are also blocked by a named Mutex.

Otherwise: kills stale processes on those ports, then launches frontend (`:3100`), datascreen (`:3102`), mobile (`:3800`), and backend (`:5000` with hot-reload).

## Key Commands

| What | Command | Working Dir |
|------|---------|-------------|
| Backend build | `dotnet build` | `backend/` |
| Backend Release build | `dotnet build -c Release` | `backend/` |
| Backend CI build (with analyzers) | `dotnet build /p:CI_BUILD=true` | `backend/` |
| Complexity gate (JNPF009) | baseline: `backend/tools/JNPF.Analyzers/complexity-baseline.json`；测：`dotnet test tools/JNPF.Analyzers/JNPF.Analyzers.Tests` | `backend/` |
| ARCH-01 layering | `dotnet test --filter FullyQualifiedName~Architecture`（Common.Core 硬失败；Message.Interfaces 豁免） | `backend/` |
| Backend tests | `dotnet test backend/zx_lowcode_netcore.sln` | repo root |
| Frontend lint | `pnpm lint` | `jnpf-web-vue3/` |
| Frontend type-check (Studio, default) | `pnpm type-check` | `jnpf-web-vue3/` |
| Frontend type-check (full / legacy) | `pnpm type-check:full` | `jnpf-web-vue3/` |
| Frontend unit tests | `pnpm test:unit` | `jnpf-web-vue3/` |
| Frontend build | `pnpm build` | `jnpf-web-vue3/` |
| Toolchain verify | `node scripts/verify-toolchain.mjs` | repo root |
| API 快测（Vitest） | `E2E_PIPELINE_ID=311 pnpm test:api` | repo root |
| S0→S2 长链（非默认） | `node scripts/_run-s2-longchain.mjs --from verify`（steps: `status/gate/pm/confirm/analyst/materialize/verify`） | repo root |
| Git hooks enable (after clone) | `git config core.hooksPath .githooks` | repo root |
| Hook 合规（含 L11 占位符） | `node scripts/test-hooks.mjs` | repo root |

**CI gate order:** `lint → type-check → test:unit → build`（`.github/workflows/ci.yml`）

- **Backend CI:** Release build with `/p:CI_BUILD=true` — any `error JNPF*` analyzer finding hard-fails; then `dotnet test`.
- **Module dependency gate:** CI runs `pwsh -File scripts/arch-module-dependency-scan.ps1 -Gate` — cross-module violations block merge.
- **PR-only, non-blocking:** promptfoo LLM eval (`promptfoo/`), k6 smoke (`scripts/load/studio-pipeline.js`).

**Frontend type-check:** Never run bare `npx vue-tsc --noEmit` (OOM on full `src`). Use `pnpm type-check` (Studio scoped, `tsconfig.typecheck.json`); use `pnpm type-check:full` when editing legacy modules. See `.cursor/rules/frontend/frontend-typecheck.mdc`.

## Code Search Rules（强制性 — 所有 Agent 必遵）

> **针式搜索铁律（防卡死）：** `.cursor/rules/toolchain/needle-search.mdc` · `.claude/rules/needle-search.md`  
> 先窄后宽 · 并行≤3 · 禁全仓拖网 · 禁为找文件派 explore · 大文件局部 Read · >15s 收窄不盲重试。

| 搜索目标 | 工具 | 说明 |
|---------|------|------|
| 已知路径 | 直接 Read（大文件带 offset/limit） | 禁止先全仓扫 |
| C# 单符号（类/方法/接口/引用） | **Serena MCP** `find_symbol` / `find_referencing_symbols` | 精确到调用点 |
| C# 文件结构概览 | **Serena MCP** `get_symbols_overview` | 一眼看清文件内容 |
| **跨文件调用链/影响分析** | **Codebase-Memory MCP** `trace_path` / `search_graph` | 多跳 callers/callees、BM25+向量搜索 |
| **项目架构/模块聚类/复杂度热点** | **Codebase-Memory MCP** `get_architecture` / `query_graph`（Cypher） | Leiden 社区检测、O(n²) 隐患 |
| 领域知识/设计意图/历史决策 | **Knowledge Graph MCP** `search_nodes` | 人工沉淀的领域知识 |
| 文本关键词 | Grep（**必须**带 path 和/或 glob） | 禁止无范围全仓扫 |
| 文件名模式 | 窄 Glob（如 `**/PmSkill*.cs`） | 禁止 `**/*` 拖网 |

> **三大 MCP 分工：** Serena=单符号精确查；Codebase-Memory=跨文件调用链/架构/复杂度（自动索引）；Knowledge Graph=领域知识/设计意图（人工沉淀）。三者互补，非替代。详见 `.claude/rules/mcp-code-search.md`。

> ❌ Shell `find`/`dir /s` 全仓搜索 · ❌ 为「找一个文件」派 explore 子 Agent。详见 [CLAUDE.md](./CLAUDE.md) §Agent Toolchain。

## Auto Test-Fix Loop（无浏览器 — 所有 Agent 必遵）

**禁止手点浏览器登录。** 后端/API/Skill/IR 验证 MUST 用脚本 + Token：

```powershell
node scripts/lib/jnpf-auth.mjs --json                              # 登录，Token 缓存
E2E_PIPELINE_ID=311 pnpm test:api                                  # Vitest 快断言
node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser               # 冒烟
node scripts/_run-s2-longchain.mjs --from verify                   # S0→S2 长链（非默认）
python scripts/jnpf_auth.py GET /api/studio/ir/42/events          # Python 等价
```

**闭环：** 编码 → `dotnet build` → **`pnpm test:api`（首选）** → FAIL 则修复 → 重跑（≤3 轮）。

| 场景 | 工具 |
|------|------|
| **日常 Dev Loop（后端/API）** | **`E2E_PIPELINE_ID=311 pnpm test:api`** + `jnpf-api.mjs` |
| 长链 Skill watch / evidence | `_run-s2-longchain.mjs` 分步（**非默认**） |
| 手工调 API | `pnpm sync:http-env` + `api-tests/http/*.http` |
| 前端 UI 交付 | Playwright：`pnpm e2e:login` / `pnpm e2e:studio` → `.claude/evidence/` |

**E2E 知识库：** `openspec/specs/studio-e2e-toolchain/spec.md` · **规则：** `.cursor/rules/toolchain/testing-toolchain.mdc`

登录：`POST /api/oauth/Login`（form-urlencoded，MD5+AES）· **不是** `/api/auth/login`

## Repo Layout

```
backend/              .NET 8 solution (zx_lowcode_netcore.sln)
  framework/          Core: DynamicApiController, DI, SqlSugar, JWT, Serilog
  infrastructure/     Cross-cutting: event bus, OAuth, WebSockets
  modularity/         16 business modules (app, codegen, inteAssistant, system, visualdata, visualdev, workflow, etc.)
  application/        Hosts: JNPF.API.Entry (main), JNPF.OA.API.Entry (OA — separate entry point)
  tests/              Integration test projects (Gate, Phase6, Stage5, ADR012)
  tools/              JNPF.Analyzers (custom Roslyn analyzer); DB init SQL: backend/web/jnpf_sundial_init.sql
jnpf-web-vue3/        PC admin frontend → :3100 (pnpm, Vite, Ant Design Vue, WindiCSS)
jnpf-web-datascreen/  Data screen frontend → :3102/DataV/ (pnpm, Element Plus)
jnpf-app-vue3/        UniApp mobile H5 → :3800 (requires proxy_server.py)
sa-service/           RETIRED (2026-07-15) — compile mode doesn't need or start it; kept for agent-mode regression only
openspec/             Spec knowledge base (specs/, changes/, ADRs) — query via /spec skill
```

## Architecture Rules (violation = broken system)

- **Business First (B0):** 任何开发以实现业务功能为最高原则。开工前三问：用户做什么操作？完成后拿到什么业务产物？哪条 E2E 验收？答不出 → 停止编码。
- **Assertion discipline:** Tag claims with `[KNOWN]`/`[COMPUTED]`/`[INFERRED]`/`[GUESS]` + confidence (HIGH≥80% / MED 50-80% / LOW 20-50%). Don't guess past 3 failures — capture runtime data. See `.claude/rules/assertion-discipline.md`.
- **Never write Controllers.** All APIs auto-map from Service classes implementing `IDynamicApiController`.
- **Unified response:** `RESTfulResult<T>` wraps automatically. Throw `Oops.Oh()` (system) / `Oops.Bah()` (business) — never raw `Exception`. HTTP code 600 = JWT expired.
- **Codegen boundary:** Bugs in generated code → fix `.vm` template source. Never edit template output files directly.
- **Multi-tenant:** Every SqlSugar query MUST verify `ITenantFilter` is active. Missing filter = cross-tenant data leak.
- **SQL injection:** Dynamic SQL MUST be parameterized. Never `$"SELECT * FROM {table}"` — use `SqlSugarFilterable<T>` or `SqlParameter[]`. Hook-enforced (L0).
- **API permission:** Every `IDynamicApiController` method MUST declare `[AllowAnonymous]` or `[SecurityDefine]`. Missing = hook-blocked (L0).
- **OA module** has separate entry point (`JNPF.OA.API.Entry`) — do not modify from main entry. IoT/MES modules don't exist — never scaffold.
- **Database:** SqlSugar (SQL Server) + Dapper. Table names: `UPPER_SNAKE_CASE` with module prefix (`BASE_USER`, `FLOW_TASK`). C# code: PascalCase.

## 业务优先最高铁律 B0（宪法级）

**任何编程开发和重构都必须以实现业务功能为最高原则；脱离业务的开发/重构必须先过审。** 完整条款：`.claude/rules/business-first-iron-law.md`

**E2E 证据铁律：** Dev Loop = `dotnet build` → `node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser` → `E2E_PIPELINE_ID=311 pnpm test:api`。无 E1 截图+E2 操作路径+E3 实际输出 → `guard-finish.mjs` BLOCK。

## 实现完整性铁律（宪法级, 永远生效, 2026-07-08 立）

**实现驱动测试，不是测试驱动实现。为通过测试而降低业务实现质量 = 系统性作弊。**

**主文件：** `.claude/rules/implementation-integrity-iron-law.md` · `CLAUDE.md` §实现完整性铁律

**五禁令（违反任一 = 立即停工）：**
1. **禁止给门控开逃逸通道** — Gate/Validator 的设计意图不可被实现层豁免绕过
2. **禁止为唯一解析器引入第二源** — 计划写"唯一源"处，不得加 fallback/兜底
3. **禁止改测试断言凑新行为** — 测试失败先核对实现，非先改测试
4. **禁止用快照重生成替代内容审查** — 重生成 hash/golden 前必须逐文件审查内容
5. **禁止跳过验收标准核心项** — 声称"完成"前逐条列验收+证据，弱项不替代强项

**节点审批门禁：** 从第一个小功能起，每个功能节点完成后 MUST 暂停，提交"业务实现+质量自检+功能证据+验收对照"，**未经用户审批不得进入下一节点。** 沉默 ≠ 审批。

## 全链条冲刺铁律（宪法级, 永远生效, 2026-07-11 立）

**按阶段推进；核心功能/产出物做 xUnit；保证数据一致性；排除旧实现干扰；全链条冒烟置后。**

| # | 铁律 |
|---|------|
| F1 | 分阶段推进 + 核心产出物可单测（验证业务准确性） |
| F2 | 数据一致性（IR / ai_entity_field / 投影契约） |
| F3 | 排除并修订旧实现干扰 |
| F4 | 全链条冒烟测试置后（SG 全绿 → W3） |

**主文件：** `.cursor/rules/iron-laws/fullchain-sprint-iron-law.mdc` · `.claude/rules/fullchain-sprint-iron-law.md` · 30 号计划 §0.6/§5/§16

## 需求分析子链铁律（宪法级, 永远生效, 2026-07-12 立）

**一切编码以阶段 A-B-C 为唯一施工依据（`docs/AI原生开发/1、多用户多任务并行/1、阶段A.md` + `2、阶段B.md` + `3、阶段C.md`）；旧 25–33 已废止归档。未经 CR 擅自修改关键业务方法 = 最严重越权。**

| # | 禁令 |
|---|------|
| 一 | 禁止新增 .mjs 脚本（除 hooks）；现有冻结迁移 xUnit/Vitest .ts — guard-write L10c ✅ |
| 二 | 数据一致性：IR=Write Model；ai_entity_field=字段唯一源；sa_*=投影禁手改 — L10d + xUnit ✅ |
| 三 | 逐阶段推进：门控→需求分析→架构→总体设计→开发→测试→debug→沙箱 — 审批 + L10 ✅ |
| 四 | 以阶段 A-B-C 为总纲，编码对照阶段 A/B/C；偏离先改文档再改代码 — 人审 📋 |
| 五 | 功能点验收：每功能点 = xUnit 绿 + 业务证据 + 用户审批；全验收后才联调 — xUnit + 审批 ✅ |
| 六 | CR 变更审批：关键业务方法（PmSkillService/RequirementAnalysisOrchestrator/AnalystSkillService/SkillsApiService/DesignSkillOrchestrator/Gates/*）修改前 MUST 提交 CR — guard-write L10a ✅ |
| 七 | 禁止复活废止模块（ScannerValidator/cascadeUpdate/sa_ddd/Q1-Q9/编排器代问/普通SINGLE）— L10b + JNPF007/008 ✅ |

**CR 流程：** `.claude/change-requests/CR-{日期}-{NN}.md` → 用户审批 → `workflow-state.json` 标 `cr-approved` → L10a 放行。

**主文件：** `.claude/rules/req-analysis-iron-law.md` · `.cursor/rules/iron-laws/req-analysis-iron-law.mdc`

## 自主工程闭环铁律 WORKFLOW-IRON-01（2026-08-30 立, 永远生效）

**Superpowers 从“工具调用规范”升级为“工程闭环基础设施”；任何工作轮次必须通过 4 环节闭环（Self Evaluation / Self Test / Self Repair / Reviewer Review）才能汇报完成。**

**适用所有 AI 大模型：** Claude Code · Cursor · Qoder · 其他 Agent。

**4 环节强制闭环：**

```
Implementation → Self Evaluation → Self Test → Self Repair → Reviewer Review → Final Report
```

**仅允许人工介入的 3 类情况：**

1. **不可逆架构选择**（单体 vs 微服务 / 数据模型重大调整 / Public Contract 变化）
2. **业务价值判断**（删除功能 / 改变用户流程 / 接受成本/性能权衡）
3. **与既定 Governance 冲突**（唯一修复方案违反 Governance 时）

**主文件：** `.claude/rules/workflow-iron-law.md` · `.cursor/rules/iron-laws/workflow-iron-law.mdc` · [`docs/构建AI软件工程agent闭环体系/类级重构专家Agent封装实现要求.md`](./docs/构建AI软件工程agent闭环体系/类级重构专家Agent封装实现要求.md)

**配套铁律：** `.claude/rules/agent-runtime-iron-laws.md`（HIP-01 Human Interrupt Policy，与本铁律互补：HIP-01 控制节奏，本铁律保证执行质量）

**违反后果：** 跳过 4 环节任何一项 = 任务未完成，状态不得标记为完成。

## ADF 三先行（全项目, 2026-07-12 立）

**S/A：架构 → 设计模式 → 接口契约 → 实现；每阶段等用户「继续/通过」。** B 级须 `ADF 豁免：B级 — …`。

| 阶段 | 要点 |
|------|------|
| P0 | Business First Q1–Q3（复用业务优先铁律） |
| P1 | 层边界、唯一源、三元组、≥2 方案+failure_boundary |
| P2 | 模式映射 SkillHarness / Gate / IR / IDynamicApiController |
| P3 | 签名/DTO/事件契约，禁止方法体 |
| P4 | 实现 + 节点审批 |

模板：`.cursor/templates/adf-*.md` · 启动：`.cursor/templates/task-kickoff.md`  
零占位符硬拦：`guard-write` L11 · Cursor hooks · `.githooks/pre-commit`

**主文件：** `.cursor/rules/iron-laws/architecture-design-interface-first.mdc` · `.claude/rules/architecture-design-interface-first.md`

### 规则加载与硬门（2026-07-12）

- **唯一 alwaysApply：** `.cursor/rules/00-constitution.mdc`（分层：`iron-laws/` `domain/` `frontend/` `toolchain/` `docs/`，见 README）
- **ADF L12：** `adfPhase=P0..P3` 锁业务源码；`P4`/`exempt` 放行
- **四支柱：** `awaitingNodeApproval` + `pillar-claim-current.json` + `pillar-claim-check.mjs --force`

## Triple-Key Iron Law (R12 — 宪法级, 永远生效)

**AI 原生开发一切数据/IR/路径/SkillContext MUST 携带三元组 `(tenantId, projectId, pipelineId)`，三者完整、独立、可分离。**

- 关系：`1 tenant → N projects → M pipelines`（WorkMode: greenfield / bugfix / enhancement）
- 支持 fork（二次开发）/ freeze（冻结）/ resume（拉起）— 走标准 API，禁止手改 DB
- 路径公式：`{SystemPath}/StudioWorkspace/{tenantId}/{projectId}/{pipelineId}/`（四层）
- IR 投影 WHERE MUST 含三元组 + FragmentId（缺 PipelineId = 撞唯一键）
- `ResolveProjectAsync` MUST 返回真实 ProjectId（非 pipelineId）
- 创建 pipeline MUST 写 `F_CREATOR_USER_ID`（同租户用户隔离）

**主文件：** `.cursor/rules/iron-laws/triple-key-iron-law.mdc` · `.claude/rules/triple-key-iron-law.md` · `architecture-redlines.md` §R12

**违反后果：** IR 投影覆盖数据 / fork 无法继承代码 / 三元组血缘断裂 / 同租户越权 — "多用户多项目多对话"形同虚设。

## Studio S2 架构（2026-07-06 — ADR-004）

**两大变更：** ① SA 九步 Agent 从生产主链分离，默认 **compile** → `SaNineViewCompiler`；② **`sa_*` 九表物化迁至 C# `SaMaterializer`**（用户 confirm 后），不再经 sa-service 写主库。

**sa-service 已退役（2026-07-15）：** compile 模式不启动它；仅 agent 模式回归需要（:3001，Node 服务）。禁止为 compile 模式排障去启动 sa-service。

| 模式 | 需要 sa-service | S2 写九表 |
|------|-----------------|-----------|
| compile（默认） | **否** | **否**（confirm 后 C# 物化） |
| agent（回归） | 是 | legacy（禁止主链） |

**文档：** `openspec/specs/studio-s2-compile/spec.md` · `.cursor/rules/domain/studio-s2-compile.mdc`  
**验收（快测优先）：** `E2E_PIPELINE_ID=311 pnpm test:api` · mjs verify 仅 evidence/长链

## 交互式澄清问答（2026-07-06 — ADR-005）

需求分析/架构设计/总体设计三阶段，LLM 产出**结构化选择题**（单选/多选/文本，每轮 3-5 题，末项恒为"其他"+文本框）让用户逐条细化需求，而非直接输出 markdown 待确认事项。

| 阶段 | 提问入口 | 暂停/恢复 | 答案注入 |
|------|---------|-----------|---------|
| 需求分析 | `RequirementGateService` 复用成熟度评估 LLM | sa-gate 对话流 | 对话历史→下一轮 maturity |
| 架构设计 | `ArchitectSkillService` 阶段一 BudgetGuard LLM | 两阶段 Skill 重跑 | answersText→ToT userPrompt |
| 总体设计 | `SystemDesignClarificationSkill` 阶段一 BudgetGuard LLM | 两阶段 Skill 重跑 | answersText→SystemDesignLocked.assumptions |

**关键题硬门控**：`ClarificationQuestion.Required=true` 必答才推进（`Oops.Bah` 拒绝）。**逃生口**："全部跳过直接分析"始终可见。**IR 事件**：`ClarificationRequested`(in-progress) / `ClarificationAnswered`(stable)，可审计回放。

**文档：** `openspec/specs/studio-clarification/spec.md` · `.cursor/rules/domain/studio-clarification.mdc`

## Eval Pipeline 四层评估（2026-07-08 — 阶段七）

Skill 质量评估管线：L1 组件/L2 轨迹/L3 任务**确定性**（无 LLM，fail-fast 跳过 L4）→ L4 `LlmJudgeService` 经 `SkillLlmBudgetGuard` fast tier 路由**跨家族 mimo**（生成 deepseek / Judge mimo，避免自偏好），**pass/fail 二元**（非 1-5 分制）；`JudgeCalibrationService` 月度 Cohen's kappa 校准（<0.6 降级 advisory）；人工抽检双写表+IR事件；失败 trace 回写 GoldenSet。

| 层 | LLM | 实现 |
|----|-----|------|
| L1 组件 / L2 轨迹 / L3 任务 | 否（确定性） | `EvalPipelineRunner` |
| L4 业务 | 是 fast（跨家族 mimo） | `LlmJudgeService` |

**验收（快测优先）：** `node scripts/phase7-eval-verify.mjs`（23 项 DoD）· `dotnet build` InteAssistant 0 错误

**文档：** `openspec/specs/studio-eval-pipeline/spec.md` · `.cursor/rules/domain/studio-eval-pipeline.mdc`

## E2E 分层工具链（2026-07-06）

**禁止**日常仅依赖慢速 `.mjs`。见 `openspec/specs/studio-e2e-toolchain/spec.md` · `.cursor/rules/toolchain/testing-toolchain.mdc`。

## Hooks (自动拦截 · AI 无法绕过)

L0 hooks registered in `.claude/settings.json` block dangerous writes, commands, and unverified completions:

| Hook | Guards |
|------|--------|
| `guard-write.mjs` | Secrets, empty files, R4–R8, L10 req-analysis, **L11 zero-placeholder** |
| `guard-bash.mjs` | Dangerous shell commands |
| `guard-finish.mjs` | E2E evidence — blocks if no screenshot/api-smoke output |

Verify hooks: `node scripts/test-hooks.mjs` (28 用例). If a write/command is blocked, check the hook output — don't retry blindly.

## Git

- **Git Iron Law:** any git operation requires a clean / committed / pushed worktree first. Stash is not long-term storage.
- Only commit when explicitly asked; hooks run via `.githooks/pre-commit` (enable once after clone, see Key Commands).

## Evidence Over Assumption（禁止猜源码, 必须抓运行时数据）

**猜 3 次不行就停手抓数据，不要再猜第 4 次。**

| 场景 | 错误 | 正确 |
|------|------|------|
| 前端无响应 | 读 .vue 源码猜 | Playwright `page.on('response')` 抓 SSE |
| API 异常 | 读 Controller 猜路由 | `scripts/jnpf-api.mjs GET/POST <path>` |
| 数据错误 | 读 SQL 拼装逻辑 | SqlSugar `ToSql()` 输出实际 SQL |
| Token 失败 | 读 `getToken()` 源码 | `scripts/lib/jnpf-auth.mjs --json` |

## Frontend SSE/Timer Rules (memory leak prevention)

Every `setTimeout`/`setInterval`/`EventSource`/`WebSocket` must follow these or leaks result:

1. **Save** timer return values to variables — never fire-and-forget.
2. **Clear** all timers in `onUnmounted`.
3. **EventSource reconnect must have a retry cap** (e.g., `MAX_RETRIES = 5`), not infinite.
4. **Never call `connect()` directly in `onerror`** — always via `setTimeout` + counter (synchronous error → busy loop).
5. **SSE URL must use `buildEventSourceUrl()`** from `/@/utils/http/sseUrl` — dev proxy requires `/dev` prefix, not raw `/api`.
6. **EventSource must pass JWT via `?token=`** — cannot set Authorization header. `buildEventSourceUrl()` handles this.

## Secrets / Config (gitignored)

- `backend/application/JNPF.API.Entry/Configurations/ConnectionStrings.json` — must create locally.
- `backend/application/JNPF.API.Entry/Configurations/JWT.json`
- `.env.local`, `.env.*.local`, `.env.toolchain` — never commit.

## Package Manager / Registry

- **Frontends:** pnpm (8.x). Registry pre-configured in root `.npmrc` → `registry.npmmirror.com`.
- **Backend:** NuGet with Huawei Cloud mirror (`backend/nuget.config`).
- Node.js 18+, .NET SDK 8.0 (pinned in `backend/global.json`).

## Default Credentials

admin / 123456 (seed data). Backend API docs: `http://localhost:5000/newapi`.

## Docker

```bash
# Production
docker compose -f docker-compose.production.yml --env-file .env.production up -d

# Backend image only
docker build -f backend/application/JNPF.API.Entry/Dockerfile -t jnpf-api backend/
```


