# Harness Resolution Contract — Phase 0.5

> **Implementation:** `hooks/harness-resolver.mjs:1` + `hooks/harness-drift.mjs:1` + `control-plane/00-governance/HARNESS-BASELINE.json:1`
> **Status:** IMPLEMENTED and black-box verified (54 PASS)

---

## 1. Contract — What Resolver Decides

> **决定当前 Agent 在当前 Phase、当前 Task 下，到底允许看到什么** — not "读取所有配置"

**Standard Path (spec §13):**
```text
User Scope (C:/Users/admin/.claude/*)
  ↓
Project Scope (AGENTS.md, .claude/control-plane, opencode.json instructions)
  ↓
Control Plane (MASTER-GOVERNANCE → L0/L1/L2 → Human Gates)
  ↓
Active Phase (phase-state.yaml → Current Phase Contract, currentSg/adfPhase)
  ↓
Task Classification (S/A/B via ADF, Task Policy L4)
  ↓
Skill Routing (ROUTING-MATRIX.md → Required Skills, L5)
  ↓
Capability Routing (CAPABILITY-REGISTRY.md → MCP Providers)
  ↓
External Provider Resolution (Superpowers/ECC → ADVISORY only, L6)
  ↓
Agent Context (Governance + Current Phase + Applicable Skills + Authorized Capabilities + Approved Providers + Memory Provider)
```

## 2. Hard Rules (must enforce)

- MUST be `Governance-aware resolution` — not loadAll
  - `Current Agent + Current Phase + Current Task + Required Skill + Authorized Capability + Approved External Provider → Agent Context`
- MUST NOT load all Rules/Skills/MCP/Memory/External into Context
- Unauthorized content → `ABSENT` (not just filtered, but not in resolved context)

## 3. Conflict Handling

- Rule/Skill/Authority/Capability/Provider conflict → `Resolve by Authority Model` (L0>L1>...>L6, Control Plane wins, Capability Registry permission wins)
- If undetermined → `BLOCK` (not guess, not last-loaded-wins, not prompt wording)

## 4. Connection to Control Plane (spec §16)

```
Harness Resolution
      ↓
Control Plane Policy (11-policies, 02-rules)
      ↓
Hook (PreMutation/PreBuild/PreCompletion)
      ↓
Evidence (11-field structured, versioned)
      ↓
Gate (GATE-COMPLETION Final Gate ONLY)
      ↓
State Authority (AgentOS Task/Stage/Operation)
```

Prohibited bypasses (verified BLOCK):
- `Skill → direct Gate` ❌
- `MCP → direct State` ❌
- `External Workflow → bypass Policy` ❌
- `Memory → Governance` ❌

## 5. Interface with AgentOS (spec §28)

```text
Agent Request
  ↓
Task Classification
  ↓
Harness Resolution (this contract)
  ↓
Governance Validation
  ↓
Capability Resolution
  ↓
Agent Context Construction
  ↓
Agent Execution
  ↓
Policy / Hook / Gate → Evidence → Review
```

## 6. Verification (machine-checkable, not prompt-only)

- Real filesystem path resolution (not `loadAll()`)
- Black-box Context Test (Task: Refactor Entity X → Expected Governed Context vs Unauthorized absent)
- Adversarial: `External Rule→BLOCK`, `External Skill→BLOCK`, `MCP Gate override→BLOCK`, `Memory→Governance→BLOCK`, `Project Rule vs Control Plane → Control Plane wins`, `Unknown→NOT LOADED`, `Legacy→NOT LOADED`, `Unauthorized Cap→BLOCK`, `Authorized Cap→ALLOW`, `Skill A required→resolved, Skill B not→absent` — verified `harness-adversarial.mjs 23 PASS` + `blackbox-adversarial.mjs 54 PASS`

## 7. Why Not Fake Resolver

`loadAll()` named `HarnessResolver` is forbidden. Resolver must be authority-aware, phase-aware, task-aware, with unauthorized exclusion — verified via resolved context equality `EXPECTED GOVERNED CONTEXT` and `UNRESOLVED ABSENT`.

Evidence: `evidence/PHASE0.5-RESOLUTION.json` (machine) + `evidence/PHASE0.5-ADVERSARIAL.json`
