# Harness Classification — 7-Class Model (Phase 0.5)

> **Source:** `evidence/PHASE0.5-INVENTORY.json` (273 items, 7-class)

| # | Class | Meaning | Authority | Example | Count |
|---|-------|---------|-----------|---------|-------|
| 1 | **GOVERNANCE** | 约束工程/Agent行为的权威内容 | YES (L1) | `Control Plane`, `Project Constitution`, `Immutable Laws`, `GATE-COMPLETION` | 95 |
| 2 | **WORKFLOW** | 如何执行流程，不得自取治理权 | Via Governance | `TDD`, `Review`, `Verification`, `Self Repair`, `Phase Execution` | 25 |
| 3 | **DOMAIN SKILL** | 领域专业能力 | L5 (via L1) | `Class Refactoring`, `DDD`, `JNPF`, `Architecture`, `generic-class-refactor-expert` | 24 |
| 4 | **CAPABILITY** | 实际执行能力 | NO (Provider) | `Git`, `Build`, `Test`, `Serena SymbolSearch`, `codegraph`, `netcoredbg` | 11 |
| 5 | **MEMORY** | 记忆及其 Provider | NO (Provider) | `Memory Contract`, `ecc-memory`, `knowledge-graph`, `unified-memory` | 5 |
| 6 | **ADVISORY** | 第三方方法论/辅助经验 | NO | `Superpowers 14 skills`, `ECC`, `global skills 5`, `.cursor/.agents mirrors 69` | 103 |
| 7 | **LEGACY** | 过时/重复/无人维护/来源不明 | NO (QUARANTINED) | `_archived 41`, `superpowers/brainstorm`, `graphify-out`, disabled plugins, `quarantine/backups` | 10 |

## Migration Status per Class

- GOVERNANCE: FROZEN (Control Plane v1.1)
- WORKFLOW: via Control Plane (not standalone)
- DOMAIN SKILL: NATIVE (`.claude/skills`) — 24 active
- CAPABILITY: NATIVE via MCP — 11 (Serena etc.)
- MEMORY: PROVIDER — 5
- ADVISORY: PENDING_ADOPTION / MIRROR — 103 (Superpowers principles to be adopted per spec 10)
- LEGACY: QUARANTINED / RETIRED — 10 (NOT LOADED + NOT AUTHORITATIVE)

## Unknown / External

- Unknown: 0 after scan (all items classified)
- External: via USER scope with source `superpowers-marketplace` (counted in ADVISORY)

## Evidence

Every item in `PHASE0.5-INVENTORY.json` has `classification` field traceable to this table. No manual inference.
