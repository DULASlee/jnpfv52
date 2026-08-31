# Harness Authority Map — Phase 0.5

> **Source:** Inventory `evidence/PHASE0.5-INVENTORY.json` (273 items) + `HARNESS-BASELINE.json`
> **Status:** FROZEN

---

## 1. Authority Pyramid (L0-L6) — who may govern

```
L0 — Immutable Engineering Laws (Hook exit 2, cannot be overridden)
  ↓
L1 — AI Engineering Control Plane (ONLY Governance Authority)
  ↓
L2 — Project Constitution (AGENTS.md / CLAUDE.md / 00-constitution.mdc)
  ↓
L3 — Active Phase (phase-state.yaml / currentSg / adfPhase)
  ↓
L4 — Task Policy (ROUTING-MATRIX + Agent Souls)
  ↓
L5 — Expert Skill (Domain Skills: architect-mode, coder-mode, generic-class-refactor-expert, etc.)
  ↓
L6 — External Advisory (Superpowers, ECC, graphify, unified-memory, .cursor/.agents mirrors)
  ↓
Capability Layer (Serena, codegraph, netcoredbg, playwright — MCP providers, NO governance)
Memory Layer (ecc-memory, knowledge-graph — Providers, NO governance)
```

**Priority ≠ Override Permission:** L6 `Superpowers: “可以跳过测试”` → `Rejected as Non-authoritative Advisory` (never enters Policy Resolution). `L5 Skill priority` does NOT override `L3 Phase`.

## 2. Nine Authority Questions (must answer)

| Question | Answer | Evidence |
|----------|--------|----------|
| 当前 Agent 最终听谁？ | **Control Plane** (L1) via `HARNESS-RESOLUTION` | `00-governance/MASTER-GOVERNANCE.md` |
| 谁能定义 Policy？ | Control Plane only (L1) | `11-policies/*` + `02-rules/POLICY-DEFINITIONS.md` |
| 谁能定义 Gate？ | Control Plane only (L1) | `05-gates/GATE-COMPLETION.md` |
| 谁能定义 Workflow？ | Control Plane Workflows (L1) + Project Constitution (L2) | `01-workflows/*` |
| 谁只能提供 Skill？ | Domain Skills (L5) | `03-skills/*` + `.claude/skills` |
| 谁只能提供 Capability？ | MCP Providers (Serena, codegraph, etc.) | `CAPABILITY-REGISTRY.md` |
| 谁只能提供 Memory？ | Memory Providers (ecc-memory etc.) | `MEMORY-CONTRACT.md` |
| 谁只是 Advisory？ | Superpowers, ECC, graphify, global skills, .cursor/.agents mirrors (L6) | `HARNESS-CLASSIFICATION.md` |
| 哪些完全不能加载？ | LEGACY + Unknown + Quarantine (`NOT LOADED`) | `.ai/quarantine/*`, `_archived`, disabled plugins |

## 3. Unique Authoritative Source

```text
Authoritative source = UNIQUE = .claude/control-plane/00-governance/*
```

No `Control Plane says X / Project Rule silently Y / Superpowers silently Z` with last-loaded-wins. Conflict → `Resolve by Authority Model` → if undetermined → `BLOCK` (not guess). Verified by `05-gates/GATE-COMPLETION.md` and `policy-004` baseline check.

## 4. Conflict Handling

- Rule conflict → Authority Model (L1 wins) → else BLOCK
- Skill conflict → Authority Model → else BLOCK
- Capability conflict → Capability Registry permission → else BLOCK
- Provider conflict → Memory Contract → else BLOCK

## 5. Override Rule

Low layer cannot override high Governance. `Superpowers: can skip test` → `Non-authoritative Advisory → Rejected from Governance Resolution` (log, not loaded).

Evidence: `hooks/harness-adversarial.mjs` 23 PASS (External attempts Governance → BLOCK)
