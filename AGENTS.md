# AGENTS.md

Compact instruction file for automated coding agents working in this repository.

## Multi-Agent Environment

This repo is used by **multiple AI coding agents**. Each has its own instruction system — they must coexist without conflict:

| Agent | Instructions | Auto-loaded by |
|-------|-------------|----------------|
| **Claude Code** | `.claude/rules/*.md`, `.claude/skills/*/SKILL.md`, [CLAUDE.md](./CLAUDE.md) | Claude Code session start |
| **Cursor** | `.cursor/rules/*.mdc` (alwaysApply), `.cursor/skills/*/SKILL.md` | Cursor IDE |
| **Any agent** | This file (`AGENTS.md`) | OpenCode / other agents |

**Rules of coexistence:**
- This file is a **subset** of [CLAUDE.md](./CLAUDE.md) — it repeats only what an agent would guess wrong. CLAUDE.md remains the single source of truth.
- **Never delete or alter** `.claude/` or `.cursor/` content from this agent — those are managed by their respective environments.
- **On-Demand Rules** (`.claude/rules/`) and **Cursor rules** (`.cursor/rules/`) contain deeper context. When available, prefer reading them over this summary.

## Project

JNPF v5.2 low-code platform — .NET 8 backend + Vue 3 frontends. Full architecture/rules in [CLAUDE.md](./CLAUDE.md).

## Dev Environment Startup

**Only** use the unified script — never `npm run dev` or `dotnet run` directly:

```powershell
powershell -ExecutionPolicy Bypass -File D:\JNPF-v52\start-dev.ps1
```

Kills stale dotnet/node processes, frees ports 3100+5000, then launches frontend (`:3100`) and backend (`:5000` with hot-reload).

## Key Commands

| What | Command | Working Dir |
|------|---------|-------------|
| Backend build | `dotnet build` | `backend/` |
| Backend Release build | `dotnet build -c Release` | `backend/` |
| Backend CI build (with analyzers) | `dotnet build /p:CI_BUILD=true` | `backend/` |
| Backend tests | `dotnet test backend/zx_lowcode_netcore.sln` | repo root |
| Frontend lint | `pnpm lint` | `jnpf-web-vue3/` |
| Frontend type-check (Studio, default) | `pnpm type-check` | `jnpf-web-vue3/` |
| Frontend type-check (full / legacy) | `pnpm type-check:full` | `jnpf-web-vue3/` |
| Frontend unit tests | `pnpm test:unit` | `jnpf-web-vue3/` |
| Frontend build | `pnpm build` | `jnpf-web-vue3/` |
| Toolchain verify | `node scripts/verify-toolchain.mjs` | repo root |
| API 快测（Vitest） | `E2E_PIPELINE_ID=311 pnpm test:api` | repo root |
| S0→S2 长链 | `node scripts/phase-sup-s2-e2e.mjs verify` | repo root |
| Git hooks enable (after clone) | `git config core.hooksPath .githooks` | repo root |

**CI gate order:** `lint → type-check → test:unit → build`

**Frontend type-check:** Never run bare `npx vue-tsc --noEmit` (OOM on full `src`). Use `pnpm type-check` (Studio scoped, `tsconfig.typecheck.json`); use `pnpm type-check:full` when editing legacy modules. See `.cursor/rules/frontend-typecheck.mdc`.

## Code Search Rules（强制性 — 所有 Agent 必遵）

**使用 MCP 工具做代码搜索，禁止逐文件 grep/find 遍历：**

| 搜索目标 | 工具 | 说明 |
|---------|------|------|
| C# 符号（类/方法/接口/引用） | **Serena MCP** `mcp__serena__find_symbol` / `find_referencing_symbols` | 语义级精确搜索 |
| C# 文件结构概览 | **Serena MCP** `mcp__serena__get_symbols_overview` | 一眼看清文件内容 |
| 项目架构/领域知识查询 | **Knowledge Graph MCP** `mcp__knowledge-graph__search_nodes` | 知识图谱语义查询 |
| 文本内容搜索 | Grep / `git grep` | 仅限上述工具不适用时 |

> Serena 和 Knowledge Graph 已配置于 `.zcode/config.json`。详见 [CLAUDE.md](./CLAUDE.md) §Agent Toolchain。

## Auto Test-Fix Loop（无浏览器 — 所有 Agent 必遵）

**禁止手点浏览器登录。** 后端/API/Skill/IR 验证 MUST 用脚本 + Token：

```powershell
node scripts/lib/jnpf-auth.mjs --json                              # 登录，Token 缓存
E2E_PIPELINE_ID=311 pnpm test:api                                  # Vitest 快断言
node scripts/jnpf-api.mjs GET /api/oauth/CurrentUser               # 冒烟
node scripts/phase-sup-s2-e2e.mjs verify                             # S0→S2 长链
python scripts/jnpf_auth.py GET /api/studio/ir/42/events          # Python 等价
```

**闭环：** 编码 → `dotnet build` → **`pnpm test:api`（首选）** → FAIL 则修复 → 重跑（≤3 轮）。

| 场景 | 工具 |
|------|------|
| **日常 Dev Loop（后端/API）** | **`E2E_PIPELINE_ID=311 pnpm test:api`** + `jnpf-api.mjs` |
| 长链 Skill watch / evidence | `phase-sup-s2-e2e.mjs` 分步（**非默认**） |
| 手工调 API | `pnpm sync:http-env` + `api-tests/http/*.http` |
| 前端 UI 交付 | Playwright → `.claude/evidence/` |

**E2E 知识库：** `openspec/specs/studio-e2e-toolchain/spec.md` · **规则：** `.cursor/rules/testing-toolchain.mdc`

登录：`POST /api/oauth/Login`（form-urlencoded，MD5+AES）· **不是** `/api/auth/login`


```
backend/              .NET 8 solution (zx_lowcode_netcore.sln)
  framework/          Core: DynamicApiController, DI, SqlSugar, JWT, Serilog
  infrastructure/     Cross-cutting: event bus, OAuth, WebSockets
  modularity/         15 business modules (system, workflow, visualdev, etc.)
  application/        Hosts: JNPF.API.Entry (main), JNPF.OA.API.Entry (OA — disabled)
  tests/              Integration test projects (Gate, Phase6, Stage5, ADR012)
  tools/              JNPF.Analyzers (custom Roslyn analyzer)
jnpf-web-vue3/        PC admin frontend → :3100 (pnpm, Vite, Ant Design Vue, WindiCSS)
jnpf-web-datascreen/  Data screen frontend → :8100/DataV/ (pnpm, Element Plus)
jnpf-app-vue3/        UniApp mobile H5 → :3800 (requires proxy_server.py)
```

## Architecture Rules (violation = broken system)

- **Never write Controllers.** All APIs auto-map from Service classes implementing `IDynamicApiController`.
- **Unified response:** `RESTfulResult<T>` wraps automatically. Throw `Oops.Oh()` (system) / `Oops.Bah()` (business) — never raw `Exception`. HTTP code 600 = JWT expired.
- **Codegen boundary:** Bugs in generated code → fix `.vm` template source. Never edit template output files directly.
- **Multi-tenant:** Every SqlSugar query MUST verify `ITenantFilter` is active. Missing filter = cross-tenant data leak.
- **OA module is disabled** — never modify. IoT/MES modules don't exist — never scaffold.
- **Database:** SqlSugar (SQL Server) + Dapper. Table names: `UPPER_SNAKE_CASE` with module prefix (`BASE_USER`, `FLOW_TASK`). C# code: PascalCase.

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

## Triple-Key Iron Law (R12 — 宪法级, 永远生效)

**AI 原生开发一切数据/IR/路径/SkillContext MUST 携带三元组 `(tenantId, projectId, pipelineId)`，三者完整、独立、可分离。**

- 关系：`1 tenant → N projects → M pipelines`（WorkMode: greenfield / bugfix / enhancement）
- 支持 fork（二次开发）/ freeze（冻结）/ resume（拉起）— 走标准 API，禁止手改 DB
- 路径公式：`{SystemPath}/StudioWorkspace/{tenantId}/{projectId}/{pipelineId}/`（四层）
- IR 投影 WHERE MUST 含三元组 + FragmentId（缺 PipelineId = 撞唯一键）
- `ResolveProjectAsync` MUST 返回真实 ProjectId（非 pipelineId）
- 创建 pipeline MUST 写 `F_CREATOR_USER_ID`（同租户用户隔离）

**主文件：** `.cursor/rules/triple-key-iron-law.mdc` · `.claude/rules/triple-key-iron-law.md` · `architecture-redlines.md` §R12

**违反后果：** IR 投影覆盖数据 / fork 无法继承代码 / 三元组血缘断裂 / 同租户越权 — "多用户多项目多对话"形同虚设。

## Studio S2 架构（2026-07-06 — ADR-004）

**两大变更：** ① SA 九步 Agent 从生产主链分离，默认 **compile** → `SaNineViewCompiler`；② **`sa_*` 九表物化迁至 C# `SaMaterializer`**（用户 confirm 后），不再经 sa-service 写主库。

| 模式 | 需要 sa-service | S2 写九表 |
|------|-----------------|-----------|
| compile（默认） | **否** | **否**（confirm 后 C# 物化） |
| agent（回归） | 是 | legacy（禁止主链） |

**文档：** `openspec/specs/studio-s2-compile/spec.md` · `.cursor/rules/studio-s2-compile.mdc`  
**验收（快测优先）：** `E2E_PIPELINE_ID=311 pnpm test:api` · mjs verify 仅 evidence/长链

## 交互式澄清问答（2026-07-06 — ADR-005）

需求分析/架构设计/总体设计三阶段，LLM 产出**结构化选择题**（单选/多选/文本，每轮 3-5 题，末项恒为"其他"+文本框）让用户逐条细化需求，而非直接输出 markdown 待确认事项。

| 阶段 | 提问入口 | 暂停/恢复 | 答案注入 |
|------|---------|-----------|---------|
| 需求分析 | `RequirementGateService` 复用成熟度评估 LLM | sa-gate 对话流 | 对话历史→下一轮 maturity |
| 架构设计 | `ArchitectSkillService` 阶段一 BudgetGuard LLM | 两阶段 Skill 重跑 | answersText→ToT userPrompt |
| 总体设计 | `SystemDesignClarificationSkill` 阶段一 BudgetGuard LLM | 两阶段 Skill 重跑 | answersText→SystemDesignLocked.assumptions |

**关键题硬门控**：`ClarificationQuestion.Required=true` 必答才推进（`Oops.Bah` 拒绝）。**逃生口**："全部跳过直接分析"始终可见。**IR 事件**：`ClarificationRequested`(in-progress) / `ClarificationAnswered`(stable)，可审计回放。

**文档：** `openspec/specs/studio-clarification/spec.md` · `.cursor/rules/studio-clarification.mdc`

## Eval Pipeline 四层评估（2026-07-08 — 阶段七）

Skill 质量评估管线：L1 组件/L2 轨迹/L3 任务**确定性**（无 LLM，fail-fast 跳过 L4）→ L4 `LlmJudgeService` 经 `SkillLlmBudgetGuard` fast tier 路由**跨家族 mimo**（生成 deepseek / Judge mimo，避免自偏好），**pass/fail 二元**（非 1-5 分制）；`JudgeCalibrationService` 月度 Cohen's kappa 校准（<0.6 降级 advisory）；人工抽检双写表+IR事件；失败 trace 回写 GoldenSet。

| 层 | LLM | 实现 |
|----|-----|------|
| L1 组件 / L2 轨迹 / L3 任务 | 否（确定性） | `EvalPipelineRunner` |
| L4 业务 | 是 fast（跨家族 mimo） | `LlmJudgeService` |

**验收（快测优先）：** `node scripts/phase7-eval-verify.mjs`（23 项 DoD）· `dotnet build` InteAssistant 0 错误

**文档：** `openspec/specs/studio-eval-pipeline/spec.md` · `.cursor/rules/studio-eval-pipeline.mdc`

## E2E 分层工具链（2026-07-06）

**禁止**日常仅依赖慢速 `.mjs`。见 `openspec/specs/studio-e2e-toolchain/spec.md` · `.cursor/rules/testing-toolchain.mdc`。

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
