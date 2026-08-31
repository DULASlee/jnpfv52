# Harness Authority Map — AI Engineering Control Plane v1.1

> **Thesis:** Clean Harness ≠ Empty Directory. Clean = Authority Clear + Loading Clear + Boundary Clear.  
> **Date:** 2026-09-01 · **Freeze:** Phase 0.5

---

## 1. Authority Pyramid (L0-L6)

```
L0 — Immutable Engineering Laws
      ↓ (Hook exit 2, cannot be overridden)
L1 — Control Plane (MASTER-GOVERNANCE + L0-LAWS + L1-PROJECT-RULES)
      ↓ (ONLY Governance Authority)
L2 — Project Constitution (AGENTS.md / CLAUDE.md / 00-constitution.mdc)
      ↓
L3 — Active Phase (phase-state.yaml → Current Phase Contract)
      ↓
L4 — Task Policy (ROUTING-MATRIX + AGENT Souls)
      ↓
L5 — Expert Skill (Domain Skills: architect-mode, coder-mode, generic-class-refactor-expert, etc.)
      ↓
L6 — External Advisory (Superpowers, ECC, graphify, unified-memory, legacy .cursor mirrors)
      ↓
   Capability Layer (Serena, codegraph, netcoredbg, playwright — MCP providers)
   Memory Layer (ecc-memory, knowledge-graph, .ecc — Providers)
```

**Critical Rule:** `L6 cannot override L0-L5.` Any L6 instruction that says "Skip Test" / "Ignore Gate" is `Rejected as Non-authoritative`.

> **Chief Architect Condition 2 — Semantic Separation:** Authority, Resolution, and Execution are three distinct concepts. Authority determines *who may govern* (L0-L6 pyramid). Resolution determines *what is loaded* (scoped subset via Harness Resolver). Execution determines *when it runs* (workflow/phase/task order). A higher `L5 Skill` priority does NOT mean it can override `L3 Phase` governance — priority ≠ authority. This separation prevents `L5 Skill` from being misinterpreted as able to cover `L3`.

```
Authority determines who may govern.
Resolution determines what is loaded.
Execution order determines when it runs.
```

---

## 2. Seven-Category Classification (per spec §5)

| # | Category | Definition | Authoritative? | Examples |
|---|----------|------------|----------------|----------|
| 1 | **GOVERNANCE** | 唯一治理源，定义 Laws/Gates/Hooks/Constitution | **YES — ONLY Control Plane** | `L0-LAWS.md`, `HUMAN-GATE-RULES.yaml`, `guard-write.mjs` (L0-L11), `AGENTS.md` |
| 2 | **WORKFLOW** | Phase 执行、验证、修复的流程定义 | **YES — via Control Plane** | `01-workflows/*.md`, `review-workflow.md`, `verification-before-completion` (migrated) |
| 3 | **DOMAIN SKILL** | 领域专家能力 (Architecture, Refactor, JNPF, DDD) | **YES — via Control Plane 03-skills + .claude/skills** | `architect-mode`, `coder-mode`, `generic-class-refactor-expert`, `table-refactor-expert`, `jnpf-api-cli` |
| 4 | **CAPABILITY** | 工具/执行能力提供者 (MCP, Build, Browser, Git) | **NO — Provider only** | `Serena`, `codegraph`, `netcoredbg`, `playwright`, `tool-search` |
| 5 | **MEMORY** | 记忆/上下文提供者 | **NO — Provider only** | `ecc-memory (.ecc)`, `knowledge-graph (.ai-memory)`, `unified-memory` |
| 6 | **ADVISORY** | 外部方法论/参考 (Third-party) | **NO — May advise, never govern** | `Superpowers 14 skills`, `ECC double-shot-latte`, `graphify`, `verification-loop`, `prompt-optimizer` |
| 7 | **LEGACY** | 无人维护/重复/过时/实验性 | **NO — QUARANTINED** | `_archived`, `*.bak*`, `episodic-memory (disabled)`, `graphify-out` (output only) |

### Visual Map

```
                Harness
                   │
         ┌─────────┴─────────┐
         │                   │
   Authoritative        External
         │                   │
   Control Plane        Superpowers (Advisory)
   Project Rules (L1)   ECC (Advisory/Memory)
   Active Phase (L3)    Serena (Capability)
   AgentOS Policy       codegraph (Capability)
   Domain Skills        ecc-memory (Memory)
   Workflows            graphify (Advisory)
         │
         └─────────┬─────────┘
                   ↓
               AgentOS
                   ↓
               Expert Agent
```

---

## 3. Detailed Classification Table

### GOVERNANCE (ONLY)

| Item | Location | Load Priority |
|------|----------|---------------|
| L0-LAWS.md | `.claude/control-plane/00-governance/L0-LAWS.md` | L0 |
| MASTER-GOVERNANCE.md | `.claude/control-plane/00-governance/MASTER-GOVERNANCE.md` | L1 |
| GOVERNANCE-INDEX.md | `.claude/control-plane/00-governance/GOVERNANCE-INDEX.md` | L1 |
| HUMAN-GATE-RULES.yaml | `.claude/control-plane/00-governance/HUMAN-GATE-RULES.yaml` | L1 |
| AGENTS.md | `D:\JNPF-v52\AGENTS.md` | L2 |
| .claude/rules/*.md (30) | `.claude/rules/` | L1 (via Control Plane mirror) |
| guard-write.mjs / guard-bash.mjs / guard-finish.mjs | `.claude/hooks/` | L0 Hook |

### WORKFLOW (Authoritative via Control Plane)

| Item | Location | Notes |
|------|----------|-------|
| PHASE-EXECUTION-PROTOCOL.md | `01-workflows/` | Phase 推进 |
| VERIFICATION-WORKFLOW.md | `01-workflows/` | 验证 |
| REVIEW-REPAIR-WORKFLOW.md | `01-workflows/` | 修复 |
| TDD-WORKFLOW.md | `01-workflows/` | 双 Profile |
| orchestration / phase-management / completion-verification | `03-skills/` | 编排/闭环 |

### DOMAIN SKILL (Authoritative)

| Skill | Location | Routing |
|-------|----------|---------|
| architect-mode / planner-mode / coder-mode / reviewer-mode / reporter-mode | `.claude/skills/` | Task Classification → Skill Routing |
| generic-class-refactor-expert / table-refactor-expert | `.claude/skills/` | D11 / P2/P3 |
| jnpf-api-cli / jnpf-ui-enhance / spec / start-dev | `.claude/skills/` | Domain |

### CAPABILITY (Provider — NOT Governance)

| Provider | Capability | Policy |
|----------|------------|--------|
| Serena MCP | SymbolSearch (find_symbol, find_referencing_symbols, get_symbols_overview) | Allow for Refactor Expert, NOT for governance |
| codegraph MCP | Call Graph, Impact Analysis, Architecture | Allow for architecture-gate |
| netcoredbg MCP | .NET Runtime Debug (breakpoint, call stack) | Allow for data-driven-debug |
| playwright / chrome-devtools | Browser E2E | Allow for E2E evidence |
| tool-search | Tool Router | Allow — routes to capability |
| git / build / test | CLI | Allow |

### MEMORY (Provider — NOT Governance)

| Provider | Contract | Notes |
|----------|----------|-------|
| .ecc/memory | ECC Memory Contract (contexts/decisions/facts/handoffs/lessons) | Project memory, 27 files |
| .ai-memory/knowledge-graph.json | Knowledge Graph MCP | Graph memory |
| unified-memory | ECC Vault handoff | Cross-agent |
| .claude/memory/pending-issues.md | Pending issues | Bug Discovery Protocol spillover |

### ADVISORY (External — MAY advise, NEVER govern)

| Item | Source | Treatment |
|------|--------|-----------|
| Superpowers 14 skills | `superpowers@5.1.0` | Load as Advisory, never as Governance. `verification-before-completion` is candidate for Migrate (see §5 — MUST be Principle → Policy → Hook → Gate → Test, not text copy). |
| verification-loop | `C:\Users\admin\.claude\skills\verification-loop` | Advisory — migrate principle, not dependency |
| graphify / plan-canvas / strategic-compact | `C:\Users\admin\.claude\skills` | Advisory capability |
| agent-architecture-audit / dotnet-patterns / production-audit / prompt-optimizer / rules-distill / skill-scout / skill-stocktake | `C:\Users\admin\.config\opencode\skills` | Advisory — maybe adopt via `rules-distill` |
| .cursor/skills mirrors (28) | `.cursor/skills/` | **MIRROR** — IDE convenience, manual sync, NOT AUTHORITATIVE |
| .agents/skills mirrors (14) | `.agents/skills/` | **MIRROR** — execution only, manual sync, NOT AUTHORITATIVE |
| .cursor/rules .mdc mirrors (27) | `.cursor/rules/` | **MIRROR** — IDE hint, manual sync, Control Plane wins on conflict |

> **Five mirror types:** `AUTHORITATIVE SOURCE` (Control Plane) / `MIRROR` (manual copy, no auto-sync) / `GENERATED CACHE` / `LEGACY COPY` / `EXTERNAL ADVISORY` — a Mirror without auto-sync is not a true cache; divergence → Control Plane wins.

### LEGACY (QUARANTINED — MUST NOT be part of authoritative or automatic Harness Resolution)

| Item | Destination | Action |
|------|-------------|--------|
| .claude/_archived | `.ai/quarantine/_archived/` | Manifest + blocked by Resolver |
| *.bak / *.bak-* (5 files) | `.ai/quarantine/backups/` | Moved, blocked (backups ignored by git) |
| .superpowers/brainstorm temp | `.ai/quarantine/superpowers-brainstorm/` | Manifest, blocked |
| episodic-memory / double-shot-latte (disabled plugins) | disabled in settings.json | NOT LOADED, blocked |
| graphify-out | `graphify-out/` (retained as output) | NOT GOVERNANCE, blocked |

---

## 4. Authority Conflict Resolution

| Conflict | Winner | Rule |
|----------|--------|------|
| Superpowers says "Skip Test" vs Control Plane says "Test required" | **Control Plane** | L6 cannot override L0-L5 |
| ECC says "Use fallback" vs IR says "Unique Source" | **Control Plane (IR=Write Model)** | Implementation-Integrity Law 禁止第二源 |
| .cursor/rules .mdc says X vs .claude/rules says Y | **.claude/rules + Control Plane** | Mirror never wins |
| Legacy rule says "Allow controller" vs Architecture Redlines says "Never write Controllers" | **Architecture Redlines (L0)** | Hook exit 2 |
| Any Advisory vs Human Gate H1-H5 | **Human Gate** | PAUSE required |

**Tag:** `Rejected as Non-authoritative Instruction` — any L6 instruction contradicting L0-L2 is logged and ignored.

---

## 5. Migration Candidates (Migrate / Retire, not Delete)

| Source (Advisory) | Principle | Target (Our Policy) | Status |
|-------------------|-----------|---------------------|--------|
| Superpowers `verification-before-completion` (5-step Gate) | Evidence before claim, 5-step verification | `02-rules/TESTING-RULES.md` + `03-skills/completion-verification` + `guard-finish.mjs` | **ADOPTED** — already in Control Plane, keep Advisory as reference |
| Superpowers `systematic-debugging` (4-phase) | Root cause before fix, no guessing | `.claude/skills/data-driven-debug` + `docs/构建AI软件工程agent闭环体系` | **ADOPTED** |
| Superpowers `brainstorming` → `writing-plans` | Explore before implement | `01-workflows/DESIGN-TO-IMPLEMENTATION.md` + `planner-mode` | **ADOPTED** |
| ECC `unified-memory` handoff pattern | Durable handoff between agents | `unified-memory` provider + `.claude/memory/` | **EVALUATE** — keep as Memory Provider |
| verification-loop | Self-evaluation loop | `WORKFLOW-IRON-01` 4-loop (Self Evaluation/Test/Repair/Reviewer) | **ADOPTED** |
| Serena symbol navigation | Precise symbol search | Capability Registry (Serena as provider) | **RETAIN as Capability** |
| codegraph call graph | Cross-file impact | Capability Registry (codegraph as provider) | **RETAIN as Capability** |

> **Principle:** `Superpowers principle → Our Policy → Our Skill/Hook/Gate` — then Superpowers can be uninstalled without capability loss.

---

## 6. Harness Resolution Layer — Executable Runtime (Phase 0.6)

```
User Scope (C:\Users\admin\.claude\settings.json, global skills)
     ↓
Project Scope (AGENTS.md, .claude/control-plane, opencode.json instructions)
     ↓
Control Plane (MASTER-GOVERNANCE → L0/L1/L2 → Human Gates)
     ↓
Active Phase (phase-state.yaml → Current Phase Contract)
     ↓
Task Classification (S/A/B via ADF)
     ↓
Skill Routing (ROUTING-MATRIX.md → Required Skills)
     ↓
Capability Routing (Capability Registry → MCP Providers)
     ↓
Harness Resolver (.claude/hooks/harness-resolver.mjs) → Machine-readable JSON
     ↓
AgentOS Context (Governance + Current Phase + Applicable Skills + Authorized Capabilities)
     ↓
Skill Loader / Capability Loader (ONLY accepts Resolver output)
```

**Who executes Resolution?** Not "Agent reads YAML" — the **Harness Resolver** (`harness-resolver.mjs`) is the runtime that produces `{ authoritative, advisory, capabilities, memoryProviders, blocked }` and **Loader only accepts Resolver result.** Quarantine is never in that result.

**Answers:** "当前这个 Agent，到底应该看到什么？" — not all 260 raw items, but resolved scoped subset.

- Machine: `07-skill-routing/HARNESS-RESOLUTION.yaml` + executable `../hooks/harness-resolver.mjs`
- Baseline: `00-governance/HARNESS-BASELINE.json` (drift anchor)
- Adversarial: `../hooks/harness-adversarial.mjs` (rogue injection proof)

---

## 7. Quarantine Policy

```
.ai/quarantine/        — Legacy / Unknown / Experimental (MUST NOT be part of authoritative or automatic Harness Resolution)
.ai/archive/           — Retired after Migrate (kept for audit, never loaded)
```

- **Cost:** Low (move/manifest, no deletion)
- **Risk:** Low (reversible, no implicit governance)
- **Rule:** Quarantine `MUST NOT be part of authoritative or automatic Harness Resolution` — governance guarantee, not system impossibility. Filesystem/shell/MCP can still read them, but Resolver never includes them and Loader never auto-loads them. `guard-skill-load.mjs` and `guard-write.mjs` must not load from there.
- **Auditable:** Evidence (README/MANIFEST/migration-record.yaml) IS tracked by git; backups/binaries are ignored (`.gitignore: .ai/quarantine/backups/`).

---

## 8. Gate Check (Authority Clear)

- [x] GOVERNANCE source unique: `.claude/control-plane/` only
- [x] WORKFLOW authoritative via Control Plane
- [x] DOMAIN SKILL via Control Plane + .claude/skills
- [x] CAPABILITY = MCP providers only (Serena/codegraph/netcoredbg = drivers, not governors)
- [x] MEMORY = providers only (ecc-memory = data, not policy)
- [x] ADVISORY = Superpowers/ECC = MAY advise, NEVER govern
- [x] LEGACY = quarantined, NOT LOADED
- [x] No second implicit Governance (all mirrors marked ADVISORY)
- [x] Conflict rule: L6 cannot override L0-L5 → `Rejected as Non-authoritative`

