# Harness Governance — Phase 0.5

> **执行原则:** Inventory → Classify → Quarantine → Migrate/Retire (not Delete)  
> **权威唯一:** `AI Engineering Control Plane v1.1`  
> **Clean 定义:** Authority Clear + Loading Clear + Boundary Clear ≠ Empty Directory

---

## Artifacts

| Artifact | Location | Purpose |
|----------|----------|---------|
| Harness Inventory | `.claude/control-plane/00-governance/HARNESS-INVENTORY.md` | 全量登记 (User + Project, 142项) |
| Authority Map | `.claude/control-plane/00-governance/HARNESS-AUTHORITY-MAP.md` | 7类权威映射 + 冲突裁决 |
| Harness Resolution (machine) | `.claude/control-plane/07-skill-routing/HARNESS-RESOLUTION.yaml` | Resolver 配置 (L0-L6) |
| Harness Resolution (doc) | `.claude/control-plane/07-skill-routing/HARNESS-RESOLUTION.md` | Resolver 设计说明 |
| Quarantine | `.ai/quarantine/README.md` | 隔离区 (NOT LOADED) |
| Archive | `.ai/archive/README.md` | 退役冷存储 |

---

## 4-Step Method (per user spec)

```
Inventory (done — 261 raw / 192 unique, canonical via harness-drift.mjs)
   ↓
Classify (done — 7 categories: GOVERNANCE/WORKFLOW/DOMAIN SKILL/CAPABILITY/MEMORY/ADVISORY/LEGACY)
   ↓
Quarantine (done — 6 bak files moved, _archived/.superpowers manifest, disabled plugins marked)
   ↓
Migrate/Retire (next — Superpowers principles → Control Plane Policy, Phase 0.6 proof first)
```

Not:

```
Inventory → Delete (DANGER — would lose valuable engineering capability)
```

## Phase 0.6 Resolution Proof (Chief Architect Conditions)

| Condition | Fix | Evidence |
|-----------|-----|----------|
| Authority ≠ Resolution ≠ Execution | `HARNESS-AUTHORITY-MAP.md` §1 semantic separation + `harness-resolver.mjs` SEMANTICS | `adversarial.mjs` PASS |
| Resolver must be executable | `harness-resolver.mjs` Resolve(User,Project,Phase,Task) → JSON, Loader ONLY accepts Resolver | `resolver.mjs --phase P0 --task A` deterministic |
| NOT WATCHED phrasing | `MUST NOT be part of authoritative or automatic Harness Resolution` | AUTHORITY-MAP §7 + RESOLUTION.yaml |
| .gitignore auditable | Evidence tracked, backups ignored | `git check-ignore` evidence NOT ignored, backups ignored |
| Canonical counting | Raw 261 / Unique 192 / Mirrors 69 / Disabled 2 / Quarantined 50 / Authoritative 136 / External 26 | `HARNESS-BASELINE.json` |
| Drift detection | `harness-drift.mjs` scan vs baseline → UNAUTHORIZED DRIFT | injected rogue → drift detected |
| Mirror types | AUTHORITATIVE SOURCE / MIRROR / GENERATED CACHE / LEGACY COPY / EXTERNAL ADVISORY | INVENTORY.md §2.3 + RESOLUTION.yaml |

**Governance Paradox Resolved:** Quarantine is governance guarantee (never auto-loaded), not system impossibility (filesystem still readable).

---

## Authority Pyramid

```
L0 Immutable Laws (Hook exit 2)
L1 Control Plane (ONLY Governance Authority)
L2 Project Constitution (AGENTS.md)
L3 Active Phase (phase-state.yaml)
L4 Task Policy (ROUTING-MATRIX)
L5 Expert Skill (Domain Skills)
L6 External Advisory (Superpowers/ECC — NEVER governs)
   + Capability (Serena/codegraph/netcoredbg — drivers)
   + Memory (ecc-memory/knowledge-graph — providers)
```

---

## Gate Check (Phase 0.5)

- [x] 所有 Rules 已登记
- [x] 所有 Skills 已登记
- [x] 所有 MCP 已登记
- [x] Memory Provider 已登记
- [x] Authoritative source 唯一
- [x] External capability 明确
- [x] Unknown/Legacy 已隔离
- [x] 没有第二套隐式 Governance

---

## Next: Phase 0.6 → Phase 1

- Task 0.6: Migrate valuable Advisory principles into Control Plane (verification-before-completion etc.)
- Phase 1: Policy / Gate / Hook (with clean authority)

