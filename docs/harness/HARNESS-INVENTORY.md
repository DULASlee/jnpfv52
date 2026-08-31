# Harness Inventory — Phase 0.5 (Mechanical Scan)

> **Generated:** 2026-09-01 via `evidence/generate-inventory.js` (filesystem scan, no fixture, no convention inference)
> **Source:** `evidence/PHASE0.5-INVENTORY.json` (273 items, 19 fields each)
> **Status:** FROZEN — PRE-AGENTOS-GATE precondition

---

## 1. Mechanical Verification

```bash
node evidence/generate-inventory.js
# → evidence/PHASE0.5-INVENTORY.json (273 items)
# Counts are mechanical: USER 27 / PROJECT 246, not hand-wavy
```

Inventory source is traceable to real paths: `.claude/*`, `.cursor/*`, `.agents/*`, `C:/Users/admin/.claude/*`, `C:/Users/admin/.config/opencode/*`, `opencode.json`, `.cursor/mcp.json`, `mcp.json`, `.ecc/*`, `.ai/quarantine/*`, etc. No `convention推测`.

## 2. Required Fields (19)

Each item contains: `id, name, path, scope[USER|PROJECT|EXTERNAL], type[RULE|SKILL|MCP|MEMORY|WORKFLOW|HOOK|CONFIG|TEMPLATE], source, purpose, classification[7-class], authority_level[L0-L6|CAPABILITY|MEMORY], load_status[LOADED|NOT_LOADED|DISABLED], active_status[ACTIVE|DISABLED], dependency, consumer, conflict_status, migration_status, quarantine_status, replacement, notes`

Example (from JSON):
```json
{
  "id": "RULE-001",
  "name": "L0-LAWS.md",
  "path": ".claude/control-plane/00-governance/L0-LAWS.md",
  "scope": "PROJECT",
  "type": "RULE",
  "classification": "GOVERNANCE",
  "authority_level": "L1",
  "load_status": "LOADED",
  "quarantine_status": "NOT_QUARANTINED"
}
```

## 3. Canonical Counts (mechanical)

| Metric | Count | Source |
|--------|-------|--------|
| **Total** | **273** | inventory.length |
| USER | 27 | scope=USER (Superpowers 14 + opencode 7 + global 5 + hooks) |
| PROJECT | 246 | scope=PROJECT |
| GOVERNANCE | 95 | classification=GOVERNANCE |
| WORKFLOW | 25 | WORKFLOW |
| DOMAIN SKILL | 24 | DOMAIN SKILL |
| CAPABILITY | 11 | CAPABILITY (MCP) |
| MEMORY | 5 | MEMORY |
| ADVISORY | 103 | ADVISORY (Superpowers/ECC/mirrors) |
| LEGACY | 10 | LEGACY (quarantined) |
| RULE | 126 | type=RULE |
| SKILL | 90 | type=SKILL |
| MCP | 11 | type=MCP |
| HOOK | 25 | type=HOOK |
| Quarantined | 11 | quarantine_status=QUARANTINED |

## 4. Coverage (must登记 all)

- [x] All Rules (126) — `.claude/rules`, `.claude/control-plane/00-governance|02-rules`, `.cursor/rules` mirrors
- [x] All Skills (90) — `.claude/skills` 23 + `.cursor/skills` 28 + `.agents/skills` 14 + USER 27
- [x] All MCP (11) — `opencode.json:4` + `.cursor/mcp.json:3` + `mcp.json:5` (Serena, codegraph, netcoredbg, tool-search, codebase-memory, knowledge-graph, playwright, chrome-devtools, sequential-thinking, interactive-feedback)
- [x] All Memory Providers (5) — ecc-memory, knowledge-graph, unified-memory, pending-issues, .ecc
- [x] All Workflows (6) — `01-workflows/*`, TDD, Verification, Review
- [x] All control configs — `settings.json`, `workflow-state.json`, `HARNESS-BASELINE.json`, `CONTRACT-BASELINE.json`, hooks
- [x] Legacy/Experimental (10) — `_archived` 41 files, `superpowers/brainstorm`, `graphify-out`, disabled plugins, quarantine backups

## 5. Scope Distinction

- **USER:** `C:/Users/admin/.claude/*`, `C:/Users/admin/.config/opencode/*` (Superpowers 5.1.0, global skills, user hooks)
- **PROJECT:** `D:/JNPF-v52/.claude/*`, `.cursor/*`, `.agents/*`, `.ecc`, `.ai/*`, `opencode.json`
- **EXTERNAL:** 0 in current scan — external is via USER scope with source `superpowers-marketplace` (treated as USER but classified ADVISORY)

Full JSON: `evidence/PHASE0.5-INVENTORY.json` (273 items, traceable paths)
