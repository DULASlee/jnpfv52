# Harness Resolution Layer — 设计说明 (Phase 0.6 Executable)

> **问题:** "当前这个 Agent，到底应该看到什么？"  
> **答案:** Governance + Current Phase + Applicable Skills + Authorized Capabilities — 而不是全部 261 raw 项。
> **Execution:** `Resolve(User, Project, Phase, Task)` via `.claude/hooks/harness-resolver.mjs` → machine JSON → Loader ONLY accepts Resolver output.

---

## 1. 为什么需要 Resolver

之前:

```
Superpowers + ECC + 20 Rules + 50 Skills + 10 MCP 全部塞上下文
→ 权威不明 → 后面逐渐跑偏
```

现在:

```
Load Harness
  → Governance Root (L0-L2)
  → Active Phase (phase-state.yaml)
  → Applicable Project Rules (only those in GOVERNANCE-INDEX.md)
  → Required Skills (ROUTING-MATRIX + Task Classification)
  → Authorized Capabilities (Capability Registry)
  → Harness Resolver (harness-resolver.mjs) → Machine JSON
  → Loader (ONLY accepts Resolver JSON) → Agent Context (scoped, minimal, authoritative)
```

## 2. Resolution Steps (Machine: HARNESS-RESOLUTION.yaml + harness-resolver.mjs)

1. Load Governance Root
2. Load Active Phase
3. Load Applicable Project Rules
4. Resolve Required Skills
5. Resolve Capabilities
6. Resolve Memory Providers
7. Produce machine-readable JSON `{ authoritative, advisory, capabilities, memoryProviders, blocked, mirrors, quarantined }`
8. Loader consumes ONLY Resolver JSON → Agent Context (Governance + Current Phase + Applicable Skills + Authorized Capabilities)

`notIncluded`: Superpowers 全量、所有 .cursor 镜像、Quarantine 内容 — 除非 Task 显式需要 Advisory 建议。

## 3. Scope Priority L0-L6 — Authority ≠ Resolution ≠ Execution

| Level | Source | Governs? | Overrides? |
|-------|--------|----------|------------|
| L0 Immutable Laws | Hook exit 2 | YES | Never overridden |
| L1 Control Plane | MASTER-GOVERNANCE + L0/L1 | YES | Only L0 |
| L2 Project Constitution | AGENTS.md / CLAUDE.md | YES | Only L0-L1 |
| L3 Active Phase | phase-state.yaml | YES | Only L0-L2 |
| L4 Task Policy | ROUTING-MATRIX + Souls | YES | Only L0-L3 |
| L5 Expert Skill | .claude/skills Domain | NO | Only L0-L4 (cannot override L3) |
| L6 External Advisory | Superpowers/ECC/graphify | NO | **Never overrides L0-L5** |

```
Authority determines who may govern.
Resolution determines what is loaded.
Execution order determines when it runs.
```

`Superpowers: "可以 Skip Test"` → `Rejected as Non-authoritative Instruction` (never enters Policy Resolution). A higher `L5 Skill` priority NEVER means it can override `L3 Phase` governance.

## 4. Capability vs Governance

| 维度 | Governance | Capability |
|------|-----------|------------|
| 定义 | 规则/门控/宪法 | 工具/执行能力 |
| 例子 | L0-LAWS, Human Gates | Serena, codegraph, netcoredbg |
| 能否决定"能不能做" | YES | NO |
| 能否提供"怎么做更快" | NO | YES |
| 卸载后系统是否受损 | YES (broken) | NO (replaceable driver) |

MCP 只是 **驱动程序**，AgentOS 持有 **Policy**。

```
Capability: SymbolSearch
Provider:   Serena MCP
Authority:  AgentOS
Policy:     ClassRefactorExpert = ALLOW
```

换 Provider (Serena → codegraph) 不影响 Agent 契约。

## 5. Memory as Provider

`ecc-memory` 不是"删还是留"的 Skill，而是 `Memory Provider`。

```
AgentOS Memory Contract
      ↓
Provider: ecc-memory / knowledge-graph / unified-memory
```

能用就用，换 Provider 不影响 Agent。

## 6. 与 Phase 0 的关系

```
Phase 0 — Baseline & Contract Freeze
  └─ Task 0.5 — Harness Governance Inventory (FROZEN 2026-09-01)
       ├─ HARNESS-INVENTORY.md (261 raw / 192 unique, canonical)
       ├─ HARNESS-AUTHORITY-MAP.md (7类权威 + L0-L6 + 语义分离)
       └─ HARNESS-RESOLUTION.yaml (Resolver 配置)
  └─ Task 0.6 — Harness Resolution Proof (Current)
       ├─ harness-resolver.mjs (executable, deterministic)
       ├─ HARNESS-BASELINE.json (drift anchor)
       ├─ harness-drift.mjs (Unknown/MCP/Hook/Skill 漂移检测)
       ├─ harness-adversarial.mjs (rogue injection proof)
       └─ .gitignore (evidence tracked, backups ignored)
  └─ Task 0.7 — Migrate verification-before-completion (Principle → Policy → Hook → Gate → Test, NOT text copy)
```

验收 (Phase 0.6 Gate):

- [x] Same input → deterministic resolution (`harness-adversarial.mjs` PASS)
- [x] Advisory cannot become Governance (L6 never in authoritative)
- [x] Quarantine never auto-loads (resolver `blocked` includes `.ai/quarantine/**`)
- [x] Unknown Harness item detected (drift: 261→262 triggers UNAUTHORIZED DRIFT)
- [x] Duplicate Governance detected (mirrors NOT AUTHORITATIVE)
- [x] Resolver output is machine-readable (JSON with authoritative/advisory/capabilities/memoryProviders/blocked)
- [x] AgentOS can consume resolver output (smoke in adversarial tests)
- [x] Existing Control Plane 1.0 behavior unchanged (44/44 hooks PASS)

Then: **Gate-0.6 PASS** → Phase 1 Policy/Gate/Hook.

## 7. Mirror Types (5-way)

`AUTHORITATIVE SOURCE` / `MIRROR` (manual, no auto-sync) / `GENERATED CACHE` / `LEGACY COPY` / `EXTERNAL ADVISORY`

A Mirror without auto-sync is not a true cache; divergence → Control Plane wins. Do not add new Governance via `.mdc` alone.
