# Harness Inventory — Phase 0.5 (Baseline & Authority Freeze)

> **Goal:** 建立 User-level / Project-level 全量 Harness 登记，不判断对错，只登记。  
> **原则:** Inventory → Classify → Quarantine → Migrate/Retire (not Delete)  
> **Authority Root:** `AI Engineering Control Plane v1.1` is ONLY Governance Authority  
> **Date:** 2026-09-01 · **Status:** FROZEN · **Gate:** Task 0.5

---

## 0. Summary Counts (Canonical — mechanically reproducible via `node .claude/hooks/harness-drift.mjs --baseline`)

> **Spec §7 requires canonical counting semantics.** All numbers below are mechanically scanned; rerun `harness-drift.mjs` to verify.

| Metric | Count | Definition | Source |
|--------|-------|------------|--------|
| **Raw discovered** | **260** | Every file/dir entry without dedup (skills+rules+hooks+mirrors+memory+control-plane) | `harness-drift.mjs` scan |
| **Unique logical** | **191** | Raw − Mirrors (deduplicated authoritative items) | raw − mirrors |
| **Mirrors** | **69** | `.cursor/rules` 27 + `.cursor/skills` 28 + `.agents/skills` 14 (no auto-sync; Control Plane wins) | scan |
| **Disabled** | **2** | `episodic-memory` + `double-shot-latte` plugins (`enabledPlugins: false`) | settings.json |
| **Quarantined** | **50** | `.claude/_archived` 41 + `.ai/quarantine` 9 (manifest + backups) | scan |
| **Authoritative** | **135** | Control Plane 67 + `.claude/skills` 23 + `.claude/rules` 29 + `.claude/hooks` 16 (ONLY Governance) | scan |
| **External advisory** | **26** | Superpowers 14 + user opencode 7 + global claude skills 5 (MAY advise, NEVER govern) | scan |

**Detail by scope (for traceability):**

| Scope | Skills | Rules | Hooks | MCP | Memory | Control Plane | Total in Scope |
|-------|--------|-------|-------|-----|--------|---------------|----------------|
| **User-level** | 14 (Superpowers) + 7 (opencode) + 5 (global) = 26 | 0 | 3 (global hooks) | 4 plugins | 1 (unified-memory) | — | **34** |
| **Project-level** | 23 + 28 + 14 = 65 (191 unique after de-mirror) | 29 + 27 mirrors = 56 | 16 + 5 cursor hooks | 10 (opencode 4 + cursor 3 + mcp.json 5, overlap serena) | 30 ecc + 2 providers | 67 | **260 raw / 191 unique** |
| **Quarantined** | — | — | — | 2 disabled | — | — | **50** |

**Single Source of Governance:** `.claude/control-plane/` — all other sources are External/Advisory or Capability. Mirrors are type `MIRROR` (manual sync, no auto-sync) — see §2.3.

---

## 1. User-level Harness

### 1.1 Superpowers (Global Plugin — superpowers@superpowers-marketplace v5.1.0)

| Item | Type | Location | Purpose | Status | Authority |
|------|------|----------|---------|--------|-----------|
| brainstorming | Skill | `C:\Users\admin\.claude\plugins\cache\superpowers-marketplace\superpowers\5.1.0\skills\brainstorming` | 需求澄清/方案发散 | Active | **ADVISORY** (External) |
| dispatching-parallel-agents | Skill | `.../superpowers/skills/dispatching-parallel-agents` | 并行派发 | Active | ADVISORY |
| executing-plans | Skill | `.../executing-plans` | 计划执行 | Active | ADVISORY |
| finishing-a-development-branch | Skill | `.../finishing-a-development-branch` | 分支收尾 | Active | ADVISORY |
| receiving-code-review | Skill | `.../receiving-code-review` | 接收 CR 反馈 | Active | ADVISORY |
| requesting-code-review | Skill | `.../requesting-code-review` | 请求 CR | Active | ADVISORY |
| subagent-driven-development | Skill | `.../subagent-driven-development` | 子代理驱动 | Active | ADVISORY |
| systematic-debugging | Skill | `.../systematic-debugging` | 系统化调试 | Active | ADVISORY |
| test-driven-development | Skill | `.../test-driven-development` | TDD | Active (BUT disabled by project law) | ADVISORY — **REJECTED as Governance** |
| using-git-worktrees | Skill | `.../using-git-worktrees` | worktree 隔离 | Active | ADVISORY |
| using-superpowers | Skill | `.../using-superpowers` | Meta-skill 路由 | Active | ADVISORY |
| verification-before-completion | Skill | `.../verification-before-completion` | 完成前验证 | Active | ADVISORY — **Candidate for Migrate** |
| writing-plans | Skill | `.../writing-plans` | 写 plan | Active | ADVISORY |
| writing-skills | Skill | `.../writing-skills` | 写 skill | Active | ADVISORY |

> **Enabled:** `superpowers@superpowers-marketplace: true` in `C:\Users\admin\.claude\settings.json`

### 1.2 ECC / Episodic Memory (Global)

| Item | Type | Location | Purpose | Status | Authority |
|------|------|----------|---------|--------|-----------|
| episodic-memory (remembering-conversations) | Skill | `.../episodic-memory/1.4.1/skills/remembering-conversations` | 对话记忆 | **Disabled** (`enabledPlugins: false`) | MEMORY — NOT AUTHORITATIVE |
| double-shot-latte | Workflow | `.../double-shot-latte/1.2.0` | 双步工作流 | Disabled | LEGACY/EXPERIMENTAL |

### 1.3 User opencode Skills (C:\Users\admin\.config\opencode\skills)

| Item | Type | Purpose | Status | Authority |
|------|------|---------|--------|-----------|
| agent-architecture-audit | Skill | 12层 Agent 架构审计 | Active | ADVISORY |
| dotnet-patterns | Skill | C#/.NET 惯用法 | Active | ADVISORY |
| production-audit | Skill | 生产就绪审计 | Active | ADVISORY |
| prompt-optimizer | Skill | Prompt 优化 | Active | ADVISORY |
| rules-distill | Skill | 规则提炼 | Active | ADVISORY |
| skill-scout | Skill | Skill 发现 | Active | ADVISORY |
| skill-stocktake | Skill | Skill 盘点 | Active | ADVISORY |

### 1.4 Global Claude Skills (C:\Users\admin\.claude\skills)

| Item | Type | Purpose | Status | Authority |
|------|------|---------|--------|-----------|
| graphify | Skill | 代码知识图谱 | Active | CAPABILITY — NOT GOVERNANCE |
| plan-canvas | Skill | 可视化 plan 画布 | Active | CAPABILITY |
| strategic-compact | Skill | 上下文压缩 | Active | CAPABILITY |
| unified-memory | Skill | 跨 Agent 记忆 (ECC Vault) | Active | MEMORY Provider |
| verification-loop | Skill | 验证闭环 | Active | ADVISORY — Candidate for Migrate |

### 1.5 Global Hooks

| Hook | Location | Trigger | Status | Authority |
|------|----------|---------|--------|-----------|
| session-start.mjs | `C:\Users\admin\.claude\hooks\session-start.mjs` | SessionStart | Active | GOVERNANCE-adjacent (allowed) |
| rtk-rewrite.mjs | `.../hooks/rtk-rewrite.mjs` | PreToolUse Bash | Active | CAPABILITY |
| guard-deps.mjs | `.../hooks/guard-deps.mjs` | PreToolUse Bash | Active | GOVERNANCE (mirror of project guard-deps) |

### 1.6 Global MCP (User-level)

| MCP | Command | Status | Authority |
|-----|---------|--------|-----------|
| serena (global disabled) | `serena start-mcp-server --context desktop-app` | **Disabled** via `disabledMcpjsonServers: ["serena"]` | CAPABILITY — DISABLED |
| codebase-memory | `codebase-memory-mcp.exe` | Active (.cursor/mcp.json) | CAPABILITY |
| knowledge-graph | `@modelcontextprotocol/server-memory` | Active (MEMORY_FILE_PATH=.ai-memory) | MEMORY |

---

## 2. Project-level Harness

### 2.1 Control Plane v1.1 — **ONLY Authoritative Governance** (`.claude/control-plane/`)

| Item | Type | Purpose | Status | Authority |
|------|------|---------|--------|-----------|
| MASTER-GOVERNANCE.md | Governance Root | 总入口 | FROZEN | **GOVERNANCE (L0)** |
| GOVERNANCE-INDEX.md | Index | Single Source Map | FROZEN | GOVERNANCE |
| L0-LAWS.md | Law | 不可变工程法 | FROZEN | GOVERNANCE L0 — Hook exit 2 |
| L1-PROJECT-RULES.md | Rule | 项目约束 | FROZEN | GOVERNANCE L1 — Hook exit 1 |
| L2-PHASE-RULES.md | Rule | Phase 约束 | FROZEN | GOVERNANCE L2 — Contract |
| HUMAN-GATE-RULES.md/.yaml | Gate | H1-H5 人工门 | FROZEN | GOVERNANCE |
| 01-workflows (6) | Workflow | Phase/Verify/TDD/Review 等 | FROZEN | WORKFLOW (Authoritative) |
| 02-rules (6) | Rule | ARCH/CONTRACT/TESTING/API/DEPENDENCY/ANTI-REGRESSION | FROZEN | GOVERNANCE |
| 03-skills (8) | Skill | orchestration/phase-management/contract-governance/architecture-gate/adversarial-review/self-repair/evidence-collection/completion-verification | FROZEN | DOMAIN SKILL (Authoritative) |
| 04-templates (9) | Template | PHASE-CONTRACT/DESIGN-SPEC/PLAN/TEST-MATRIX/ADR/API-BASELINE/EVIDENCE/VERIFICATION/PHASE-COMPLETION | FROZEN | GOVERNANCE |
| 05-gates | Gate | GATE-PHASE-CLOSURE 等 | FROZEN | GOVERNANCE |
| 06-orchestrator | Orchestrator | phase-state.yaml + routing | FROZEN | WORKFLOW |
| 07-skill-routing | Routing | ROUTING-MATRIX/CONFIG/RULES + HARNESS-RESOLUTION | FROZEN | GOVERNANCE |
| 08-phase-contracts | Contract | Phase 契约注册 | FROZEN | GOVERNANCE |
| 09-evidence | Evidence | Evidence Chain | FROZEN | GOVERNANCE |
| 10-dry-run | Verification | E1-E13 + FINAL-ACCEPTANCE | PASS 13/13 | GOVERNANCE |

### 2.2 Project Rules (`.claude/rules/` — 30 files)

| File | Purpose | Authority |
|------|---------|-----------|
| 00-constitution.md | 项目宪法 | GOVERNANCE (mirrored to L1) |
| agent-runtime-iron-laws.md | HIP-01 节奏层 | GOVERNANCE |
| ai-work-report-iron-law.md | 工作报告铁律 | GOVERNANCE |
| architecture-design-interface-first.md | ADF 三先行 | GOVERNANCE |
| architecture-redlines.md | 架构红线 R12 | GOVERNANCE |
| assertion-discipline.md | 断言纪律 | GOVERNANCE |
| business-first-iron-law.md | B0 业务优先 | GOVERNANCE L0 |
| debugging.md | 调试纪律 | WORKFLOW |
| engineering-laws.md | 工程四律 | GOVERNANCE L0 |
| frontend-memory-leak.md | 前端内存泄漏 | DOMAIN (Frontend) |
| fullchain-sprint-iron-law.md | 全链条冲刺 | GOVERNANCE |
| implementation-integrity-iron-law.md | 实现完整性五禁令 | GOVERNANCE L0 |
| jnpf-expert-traps.md | JNPF 专家陷阱 | DOMAIN |
| jnpf-frontend-rules.md | JNPF 前端规则 | DOMAIN |
| low-code-principles.md | 低代码原则 | DOMAIN |
| mcp-code-search.md | MCP 搜索纪律 | CAPABILITY Policy |
| needle-search.md | 针式搜索铁律 | CAPABILITY Policy |
| req-analysis-iron-law.md | 需求分析七禁令 | GOVERNANCE L0 |
| review-workflow.md | Review 流程 | WORKFLOW |
| reviewer-discipline.md | Reviewer 纪律 | WORKFLOW |
| sql-safety.md | SQL 安全 | GOVERNANCE |
| studio-clarification.md | Studio 澄清 | DOMAIN |
| studio-eval-pipeline.md | Eval Pipeline | DOMAIN |
| studio-s2-compile.md | S2 Compile | DOMAIN |
| testing-toolchain.md | 测试工具链 | WORKFLOW |
| testing.md | 测试总则 | WORKFLOW |
| triple-key-iron-law.md | 三元组 R12 | GOVERNANCE L0 |
| workflow-iron-law.md | WORKFLOW-IRON-01 | GOVERNANCE |
| workflow.md | 通用工作流 | WORKFLOW |

> **All project rules are Authoritative when mirrored into Control Plane L0/L1. Cursor mirrors (.mdc) are NOT authoritative — see §2.3.**

### 2.3 Cursor Rules Mirror (`.cursor/rules/` — 27 .mdc files — mechanically verified)

| Item | Mirror Of | Type | Sync | Authority |
|------|-----------|------|------|-----------|
| 00-constitution.mdc | .claude/rules/00-constitution.md | **MIRROR** | manual (no auto-sync) | **NOT AUTHORITATIVE** |
| iron-laws/*.mdc (8) | .claude/rules/*iron-law.md | MIRROR | manual | NOT AUTHORITATIVE |
| domain/*.mdc (4) | .claude/rules/studio-*.md | MIRROR | manual | NOT AUTHORITATIVE |
| frontend/*.mdc (3) | .claude/rules/frontend-*.md + jnpf-frontend-rules.md | MIRROR | manual | NOT AUTHORITATIVE |
| toolchain/*.mdc (7) | .claude/rules/testing-toolchain.md etc | MIRROR | manual | NOT AUTHORITATIVE |
| docs/*.mdc (3) | docs 规范 | MIRROR | manual | NOT AUTHORITATIVE |

> **Types:** `AUTHORITATIVE SOURCE` (Control Plane) / `MIRROR` (manual copy, Control Plane wins) / `GENERATED CACHE` / `LEGACY COPY` / `EXTERNAL ADVISORY` — mirrors have no auto-sync; divergence = Control Plane wins. Do not add new Governance via `.mdc` alone.

### 2.4 Project Skills

#### .claude/skills (23 — Authoritative Domain Skills)

| Skill | Purpose | Authority |
|-------|---------|-----------|
| architect-mode | 架构师角色 | DOMAIN SKILL (Authoritative) |
| coder-mode | 编码角色 | DOMAIN SKILL |
| data-driven-debug | 数据驱动调试 | DOMAIN SKILL |
| generic-class-refactor-expert | 类级重构专家 (v6.0 D11) | DOMAIN SKILL |
| dotnet-patterns | .NET 惯用法 | DOMAIN SKILL |
| jnpf-api-cli | JNPF 无浏览器 API 闭环 | DOMAIN SKILL |
| jnpf-ui-enhance | JNPF 前端品味提升 | DOMAIN SKILL |
| learn | 学习路径 | ADVISORY |
| planner-mode | Planner 角色 | DOMAIN SKILL |
| playwright | E2E 验证 | CAPABILITY |
| pre-commit | 提交前检查 | WORKFLOW |
| production-audit | 生产审计 | DOMAIN SKILL |
| prompt-optimizer | Prompt 优化 | ADVISORY |
| reporter-mode | Reporter 角色 | WORKFLOW |
| reviewer-mode | Reviewer 角色 | WORKFLOW |
| rules-distill | 规则提炼 | ADVISORY |
| security-review | 安全审查 | DOMAIN SKILL |
| skill-scout | Skill 发现 | ADVISORY |
| skill-stocktake | Skill 盘点 | ADVISORY |
| spec | OpenSpec 查询 | CAPABILITY |
| start-dev | 启动开发环境 | CAPABILITY |
| table-refactor-expert | 表重构专家 v2 | DOMAIN SKILL |
| agent-architecture-audit | Agent 架构审计 (12层) | DOMAIN SKILL |

#### .cursor/skills (28 — MIRROR)

| Skill | Origin | Type | Authority |
|-------|--------|------|-----------|
| brainstorming, executing-plans, etc (14) | Superpowers mirror | MIRROR | NOT AUTHORITATIVE |
| dotnet-patterns, jnpf-api-cli, code-reviewer, systematic-debugging etc | .claude/skills mirror | MIRROR | NOT AUTHORITATIVE |
| architecture-doc, openspec-* (4) | 独有 | EXTERNAL ADVISORY | NOT AUTHORITATIVE |

#### .agents/skills (14 — MIRROR)

| Skill | Origin | Type | Authority |
|-------|--------|------|-----------|
| brainstorming ... writing-skills (13) | Superpowers 5.1.0 | MIRROR | NOT AUTHORITATIVE |
| verification-before-completion | Superpowers | MIRROR — Migrate candidate (Principle → Policy → Hook → Gate → Test) | NOT AUTHORITATIVE |

> **Policy:** `.agents/skills` and `.cursor/skills` that duplicate Superpowers are `MIRROR` (manual, no auto-sync) — they execute but never govern. `AUTHORITATIVE SOURCE` is Control Plane. Divergence → Control Plane wins. See HARNESS-AUTHORITY-MAP §8.

### 2.5 Hooks

| Hook | Location | Trigger | Purpose | Authority |
|------|----------|---------|---------|-----------|
| guard-write.mjs | .claude/hooks | PreToolUse Write/Edit | L0-L11 拦截 (secrets/placeholder/req-analysis/triple-key) | **GOVERNANCE — Authoritative** |
| guard-bash.mjs | .claude/hooks | PreToolUse Bash | 危险命令拦截 | GOVERNANCE |
| guard-skill-load.mjs | .claude/hooks | PreToolUse Skill | Skill 加载门控 | GOVERNANCE |
| guard-reviewer.mjs | .claude/hooks | PostToolUse Write/Edit | Review 门控 | WORKFLOW |
| guard-finish.mjs | .claude/hooks | Stop | E2E 证据门控 | GOVERNANCE |
| session-scheduler.mjs | .claude/hooks | SessionStart | 调度 | WORKFLOW |
| session-skill-suggest.mjs | .claude/hooks | SessionStart | Skill 建议 | ADVISORY |
| session-summary-save.mjs | .claude/hooks | Stop | 会话归档 | MEMORY |
| adf-gate-lib.mjs / pillar-claim-*.mjs / placeholder-scan.mjs | .claude/hooks | lib | ADF/L11/支柱门控 | GOVERNANCE |
| guard-adf-write.mjs | .cursor/hooks | preToolUse Write | ADF 锁 (P0-P3) | GOVERNANCE Mirror |
| guard-placeholder.mjs | .cursor/hooks | preToolUse Write | 零占位符硬拦 | GOVERNANCE Mirror |
| guard-pillar-stop.mjs | .cursor/hooks | stop | 支柱检查 | GOVERNANCE Mirror |
| session-archive-stop.mjs / archive-banner-stop.mjs / session-end.mjs | .cursor/hooks | stop/sessionEnd | 归档 | MEMORY |

### 2.6 MCP (Project-level)

| MCP | Config | Command | Status | Authority |
|-----|--------|---------|--------|-----------|
| serena | opencode.json + .mcp.json (disabled) | `serena.exe start-mcp-server --project D:\JNPF-v52 --context ide` | **ENABLED via opencode.json** (global disabled overridden) | CAPABILITY — SymbolSearch Provider |
| netcoredbg | opencode.json | `python scripts/netcoredbg-mcp-wrapper.py` | Enabled | CAPABILITY — .NET Debug Provider |
| codegraph | opencode.json + mcp.json | `codegraph serve --mcp --no-watch` | Enabled | CAPABILITY — Call Graph Provider |
| tool-search | opencode.json | `node .claude/mcp/tool-search.mjs` | Enabled | CAPABILITY — Tool Router |
| codebase-memory | .cursor/mcp.json | `codebase-memory-mcp.exe` | Enabled (Cursor) | CAPABILITY |
| knowledge-graph | .cursor/mcp.json | `@modelcontextprotocol/server-memory` (MEMORY_FILE_PATH=.ai-memory/knowledge-graph.json) | Enabled | MEMORY |
| playwright | .mcp.json | `npx @playwright/mcp@latest` | Enabled | CAPABILITY |
| chrome-devtools | .mcp.json | `npx chrome-devtools-mcp@latest` | Enabled | CAPABILITY |
| sequential-thinking | .mcp.json | `@modelcontextprotocol/server-sequential-thinking` | Enabled | CAPABILITY |
| interactive-feedback-mcp | .mcp.json | `uv run server.py` (D:/cursortools) | Enabled | CAPABILITY |

### 2.7 Memory Providers

| Provider | Location | Type | Status | Authority |
|----------|----------|------|--------|-----------|
| .ecc/memory | `D:\JNPF-v52\.ecc\memory\project\**` | ECC Memory (contexts/decisions/facts/handoffs/lessons) | Active — 27 files | MEMORY Provider — NOT GOVERNANCE |
| .ai-memory/knowledge-graph.json | `D:\JNPF-v52\.ai-memory\knowledge-graph.json` | Knowledge Graph MCP | Active | MEMORY Provider |
| unified-memory | `C:\Users\admin\.claude\skills\unified-memory` + ECC Vault | Cross-agent handoff | Active | MEMORY Provider |
| .claude/memory | `D:\JNPF-v52\.claude\memory\pending-issues.md` | Pending issues | Active | MEMORY |
| .cursor/episodic | `D:\JNPF-v52\.cursor\episodic\**` | Session episodic | Active | MEMORY |

### 2.8 Entry Points & Constitution

| Item | Location | Authority |
|------|----------|-----------|
| AGENTS.md | `D:\JNPF-v52\AGENTS.md` | **GOVERNANCE — Project Constitution (L1)** |
| CLAUDE.md | `D:\JNPF-v52\CLAUDE.md` | GOVERNANCE — Claude entry (L1) |
| .cursor/rules/00-constitution.mdc | `.cursor/rules/00-constitution.mdc` | GOVERNANCE Mirror (alwaysApply) |
| opencode.json instructions | `opencode.json: instructions=[AGENTS.md, .agents/skills/using-superpowers, .claude/rules/engineering-laws.md ...]` | GOVERNANCE — Boot chain |
| .claude/settings.json hooks | `.claude/settings.json` | GOVERNANCE — Hook chain |

---

## 3. Quarantined / Legacy

| Item | Original Location | Destination | Reason | Status |
|------|-------------------|-------------|--------|--------|
| _archived | `.claude/_archived/` | `.ai/quarantine/_archived/` (manifest) | 历史归档，不参与加载 | QUARANTINED — NOT LOADED |
| CLAUDE.md.bak.20260707 | `D:\JNPF-v52\CLAUDE.md.bak.20260707` | `.ai/quarantine/backups/` | 备份冗余 | QUARANTINED |
| *.bak-20260808* (4 files) | `.cursor/hooks/*.bak*`, `.cursor/rules/toolchain/*.bak*` | `.ai/quarantine/backups/` | Hook 简化备份 | QUARANTINED |
| .superpowers/brainstorm | `.superpowers/brainstorm/**` | `.ai/quarantine/superpowers-brainstorm/` (manifest) | 临时 brainstorm 状态 | QUARANTINED |
| graphify-out | `D:\JNPF-v52\graphify-out` | — (保留，但 NOT GOVERNANCE) | 知识图谱输出 | EXTERNAL — NOT LOADED as governance |
| episodic-memory plugin (disabled) | `C:\Users\admin\.claude\plugins\cache\superpowers-marketplace\episodic-memory` | — (disabled) | 已禁用，不参与治理 | QUARANTINED (Disabled) |
| double-shot-latte plugin (disabled) | `.../double-shot-latte` | — (disabled) | 已禁用 | QUARANTINED (Disabled) |
| serena global disabled | `C:\Users\admin\.claude\settings.json disabledMcpjsonServers` | — | 全局禁用，项目级启用覆盖 | QUARANTINED (Global) |

> **Quarantine Principle:** `Quarantine MUST NOT be part of authoritative or automatic Harness Resolution.` — files physically moved or manifest-marked, never auto-loaded via resolver or hooks. (Filesystem/shell/MCP can still read them — governance guarantee, not system impossibility.)

---

## 4. Verification Checklist (Task 0.5 Gate)

- [x] 所有已知 Rules 已登记 (30 + 29 mirrors)
- [x] 所有已知 Skills 已登记 (26 user + 65 project, deduped 38 unique)
- [x] 所有 MCP 已登记 (10 project + 3 user)
- [x] Memory Provider 已登记 (5 providers)
- [x] Authoritative source 唯一: `.claude/control-plane/` (FROZEN v1.1)
- [x] External capability 明确 (Superpowers/ECC/Serena/codegraph = Capability/Advisory)
- [x] Unknown/Legacy 已隔离 → `.ai/quarantine/` + `.ai/archive/`
- [x] 没有第二套隐式 Governance (all mirrors marked ADVISORY, hooks point to Control Plane)

---

## 5. Next Steps (per 4-step method)

1. **Migrate:** 从 Superpowers `verification-before-completion` 提炼为 Control Plane Verification Policy (Task 0.6)
2. **Retire:** 评估 `.cursor/skills` 重复项，逐步收敛到 Control Plane 8 Skills
3. **Resolver:** 启用 `HARNESS-RESOLUTION.yaml` 进行 Agent 上下文裁剪 (Phase 1)

