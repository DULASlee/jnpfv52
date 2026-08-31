# Memory Contract — Phase 0.5

> **Agent depends on Contract, not Provider**

---

## Architecture

```text
AgentOS
  ↓
Memory Contract (stable interface)
  ↓
Provider
  ├── ecc-memory (.ecc/memory/project 30 files: contexts/decisions/facts/handoffs/lessons)
  ├── knowledge-graph (.ai-memory/knowledge-graph.json — Knowledge Graph MCP)
  ├── unified-memory (C:/Users/admin/.claude/skills/unified-memory — cross-agent handoff)
  └── pending-issues (.claude/memory/pending-issues.md)
```

`evidence/PHASE0.5-INVENTORY.json` MEMORY 5 items all `authority_level=MEMORY` (not GOVERNANCE), `consumer=AgentOS Memory Contract`

## Contract

- **Agent Contract:** `read_context(task, phase) → memory`, `write_context(handoff) → memory`, `search(query) → results` — stable, provider-agnostic
- **Provider Contract:** implements `search_nodes`, `search`, `read`, `handoffs` — driver
- **Authority:** AgentOS owns Memory Contract Authority; `ecc-memory` is provider with `Governance Authority = NO` (cannot mutate Governance)

## Boundary Verified

- `hooks/blackbox-adversarial.mjs` — Memory Provider attempts Governance mutation → BLOCK (ecc-memory cannot change Policy/Gate/State)
- Future replace `ecc-memory` with `future-provider` → `Agent Contract` unchanged (proven by registry abstraction)

## Evidence

- Inventory MEMORY 5, all `load_status=LOADED`, `active_status=ACTIVE`, `quarantine_status=NOT_QUARANTINED` except legacy disabled (quarantined)
- `HARNESS-BASELINE.json` counts MEMORY 5
