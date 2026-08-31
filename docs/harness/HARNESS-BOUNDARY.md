# Harness Boundary — External Provider Boundary (Phase 0.5)

> **Principle:** External → Provider/Advisory/Capability → AgentOS-controlled interface (never direct governance)

---

## 1. Superpowers

- **Can:** `ADVISORY / WORKFLOW SOURCE` (brainstorming, planning, systematic-debugging principles)
- **Cannot:** `Governance Authority = NO` `Policy Authority = NO` `Gate Authority = NO` `State Authority = NO`
- **Enforcement:** `hooks/guard-skill-load.mjs` + `Harness Resolver` — Superpowers skills are L6 ADVISORY, never in `authoritative` (verified `harness-adversarial.mjs: 23 PASS External Rule→BLOCK`)
- **Migration:** Adopt principle → `Control Plane Policy/Hook/Gate` (e.g., `verification-before-completion` → `POLICY-002/005` + `evidence-collector`), not copy file

## 2. ECC

- **Can:** `ADVISORY`, `DOMAIN/WORKFLOW SOURCE`
- **Cannot:** `Governance Authority = NO`
- **Enforcement:** ECC memory under `.ecc` is classified MEMORY Provider, not Governance. No ECC skill is in `authoritative`.

## 3. Serena

- **Can:** `CAPABILITY PROVIDER` — `SymbolSearch`, `CodeNavigation` (`get_symbols_overview`, `find_symbol`)
- **Cannot:** `Governance Authority = NO`
- **Enforcement:** `CAPABILITY-REGISTRY.md` — `Authority: AgentOS`, `Governance: NONE`. Serena is driver, not governor. `Capability: SymbolSearch, Provider: Serena, Authority: AgentOS, Policy: Expert may use`

## 4. ecc-memory (and other Memory Providers)

- **Can:** `MEMORY PROVIDER`
- **Cannot:** `Memory Contract Authority = AgentOS` (Agent depends on Contract, not provider)
- **Architecture:**
```
AgentOS → Memory Contract → Provider
                              ├── ecc-memory (.ecc)
                              └── knowledge-graph (.ai-memory)
```
- **Enforcement:** `MEMORY-CONTRACT.md` — future replace provider without changing Agent Contract, verified `MEMORY` 5 items all `authority_level=MEMORY` not GOVERNANCE

## 5. Project Rule vs Control Plane Conflict

- **Rule:** `Project Rule conflicts with Control Plane → Control Plane wins` (Authority Map L1 > L2)
- **Verified:** `policy-004` baseline check: `L0-LAWS.md` hash must match `CONTRACT-BASELINE.json:1` (authoritative), not `workflow-state cr-approved` (agent-writable). `policy-adversarial` 19 PASS includes frozen mismatch → BLOCK.

## 6. What Cannot Be Loaded

- `LEGACY` + `Unknown` + `Quarantine` → `NOT LOADED + NOT AUTHORITATIVE` (`evidence/PHASE0.5-INVENTORY.json: quarantine_status=QUARANTINED` 11 items)
- Excluded from `Harness Resolver` authorized context (`HARNESS-RESOLUTION-CONTRACT.md: Unauthorized exclusion`)

## 7. Verification (must prove not prompt-only)

Not `“请记住 Control Plane 优先”` — but `machine-checkable boundary`:
- `harness-resolver.mjs:1` — `authoritative` list never includes L6
- `guard-skill-load.mjs` — L6 cannot load as Governance
- `harness-adversarial.mjs` + `blackbox-adversarial.mjs:1` — External attempts Governance/Policy/Gate/State → BLOCK (exit 2, decision BLOCK, evidence)

