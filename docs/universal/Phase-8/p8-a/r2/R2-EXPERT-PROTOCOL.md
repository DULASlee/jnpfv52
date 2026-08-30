# R2 — Independent AI Expert Judge Protocol

> **Phase**: 8 — P8-A.5 (R2)
> **Status**: 🟢 **READY FOR USE**
> **Date**: 2026-08-30
> **Authority**: Chief Architect directive 2026-08-30
> **Purpose**: Define how Independent AI Expert operates within R2 framework

---

## 1. Role Definition

### 1.1 What Independent AI Expert IS

- A **calibrated reference** for evaluating Skill's expert judgment
- An **independent path** that reads the same source evidence as Skill but uses different reasoning process
- A **comparison anchor** that produces structured output per R2 Output Schema

### 1.2 What Independent AI Expert IS NOT

- **NOT** an Oracle — does not have privileged access to ground truth
- **NOT** a judge of Skill — produces its own assessment, not commentary on Skill
- **NOT** bound by Skill's process — free to use any reasoning method as long as output is structured
- **NOT** authorized to modify Skill or Master Spec mid-R2 — escalation only

---

## 2. Isolation Procedure (HARD CONSTRAINT)

### 2.1 Source Access

Both Skill and Expert read from the SAME source set:

```
✅ Allowed sources (both Skill and Expert)
   - DB metadata: INFORMATION_SCHEMA, sys.indexes, sys.foreign_keys
   - C# entity source: backend/modularity/**/Entity/*.cs
   - JNPF Service / Repository code: backend/modularity/**/*.cs (read-only)
   - SQL metadata files: p8-0/*.txt, p8-0/*.csv
   - Frozen context: Universal Skill v1.0, JNPF Extension v1.0, Foundry Target Profile v1.0
   - Master Spec, Execution Manual (referenced by Skill; Expert may reference for HG definitions)
   - Existing pilot/batch documents (READ ONLY — for context, not for anchoring)
```

### 2.2 Files the Independent AI Expert MUST NOT Read (until Result B committed)

```
❌ p8-a/r2/round-N/skill/*                (Skill output for current round)
❌ p8-a/r2/round-N/comparison/*            (Comparison output for current round)
❌ p8-a/r2/round-N/selection.md            (R2 selection rationale — Expert must NOT see this)
❌ p8-a/r2/COMPARATIVE-GATE-DECISION.md    (Final decision — not yet made)
❌ p8-a/r2/R2-MASTER-PLAN.md               (this document — to prevent anchoring)
```

**Note**: Expert MAY read:
- `table-refactor-expert/SKILL.md` (to understand what Skill is supposed to do, but NOT its current output)
- Master Spec / Execution Manual (for HG definitions)
- Previous Round results (Round N-1, AFTER Round N-1 comparison committed)

### 2.3 Isolation Mechanism

1. **Sequential not parallel**: Result A (Skill) is committed FIRST. Then Result B (Expert) starts.
2. **Expert cannot view Result A** during execution. AI Engineer enforces by not providing access.
3. **Expert output is committed before comparison**: AI Engineer opens both for comparison only AFTER Result B committed.
4. **Post-hoc verification**: AI Engineer can ask Expert to defend any output; Expert cannot retrospectively cite Skill output.

---

## 3. Output Schema (mandatory)

### 3.1 Per-Table Output

Each table produces ONE file at `p8-a/r2/round-N/expert/[NN]-[table_name].md`:

```markdown
# R2 — Round N — Table [NN] — [table_name] — Expert Result

> **Date**: 2026-08-30
> **Reviewer type**: Independent AI Expert
> **Reviewer session ID**: [unique ID for traceability]
> **Source evidence accessed**: [explicit list of files / SQL queries]

## 1. Table Overview
- Schema: [column count, key columns, types]
- Row count: [number from DB]
- Tenant: YES (column) / NO
- SoftDelete: YES (column) / NO
- Entity: [path or "NONE — dynamic"]
- FKs in/out: [count from metadata]
- Special: [any notable characteristic]

## 2. Seven-Dimension Assessment (A–G)
| Dim | Finding | Evidence | [KNOWN]/[COMPUTED]/[INFERRED]/[GUESS] |
|-----|---------|----------|------|
| A Schema | [text] | [source ref] | [tag] |
| B Integrity | [text] | [source ref] | [tag] |
| C Index | [text] | [source ref] | [tag] |
| D Lifecycle | [text] | [source ref] | [tag] |
| E CRUD/Query | [text] | [source ref] | [tag] |
| F DDD | [text] | [source ref] | [tag] |
| G Consumer/Target | [text] | [source ref] | [tag] |

## 3. Risk Classification
- Risk: R0 / R1 / R2 / R3+
- Confidence: HIGH / MEDIUM / LOW
- Rationale: [free-form reasoning]

## 4. Hard Gate Assessment
| HG | Triggered? | Reason | Evidence |
|----|-----------|--------|----------|
| HG#1 Tenant Isolation | YES/NO/borderline | [text] | [ref] |
| HG#2 Data Integrity | YES/NO/borderline | [text] | [ref] |
| HG#3 Migration | YES/NO/borderline | [text] | [ref] |
| HG#4 Cross-Module | YES/NO/borderline | [text] | [ref] |
| HG#5 Business Ambiguity | YES/NO/borderline | [text] | [ref] |

## 5. Recommended Action
- Action: AUTO-CLOSE / AUTO-APPLY / EVIDENCE-DRIVEN / HUMAN APPROVAL / CROSS-TABLE / DESTRUCTIVE
- Closure: NO-CHANGE / REFACTOR / DEFERRED / ACCEPT-AS-IS
- Rationale: [text]

## 6. Evidence Basis
- Sources read: [explicit list]
- Evidence tags used: [KNOWN/COMPUTED/INFERRED/GUESS counts]
- Stop condition met: YES/NO
- Total tokens spent: [approximate, optional]

## 7. (Optional) Additional Reasoning
[Expert may add free-form expert commentary, design alternatives, etc.]
```

### 3.2 Output File Naming

```
p8-a/r2/round-N/expert/
├── 01-[table_name].md
├── 02-[table_name].md
├── 03-[table_name].md
├── 04-[table_name].md
└── 05-[table_name].md
```

---

## 4. Expert Reasoning Discipline

### 4.1 Must

- Use evidence tags consistently (KNOWN / COMPUTED / INFERRED / GUESS)
- Cite source for each finding (file path + line if applicable)
- State confidence explicitly
- Trigger HGs when warranted (NOT use "borderline" to dodge)
- Identify out-of-scope tables explicitly (e.g., OUT_OF_SCOPE)
- Document reasoning chain (not just conclusions)

### 4.2 Must NOT

- **NOT** reference Skill output (forbidden)
- **NOT** compare against expected Skill output (forbidden)
- **NOT** soften judgments to align with presumed Skill output
- **NOT** skip dimensions to save effort (all 7 A–G required)
- **NOT** invent Master Spec rules (escalate as Gap instead)

### 4.3 Reasoning Style

Expert is encouraged to use **richer reasoning** than Skill — free-form expert thinking is allowed. The output structure ensures comparability; the reasoning section allows Expert to demonstrate independent thinking.

**Example acceptable expert reasoning**:

> "Looking at this table, the entity file shows X. However, the DB metadata shows Y. There's a divergence here. Per Master Spec §3.X, Y should be the source of truth because the entity is auto-generated and may lag. So I'll mark this as Finding A-Schema with [COMPUTED] confidence based on Y, but note the divergence as a follow-up item."

This is richer than Skill's structured output but **must end in the same structured form** for comparison.

---

## 5. Quality Self-Check (Expert)

Before committing Result B, Expert MUST verify:

```
[ ] All 5 tables produced (5 files)
[ ] Each file follows §3.1 schema
[ ] No reference to Skill output or R2 framework docs
[ ] All 7 dimensions A–G filled per table
[ ] Risk classification present (R0/R1/R2/R3+) with rationale
[ ] All 5 HGs assessed per table (YES/NO/borderline + reason)
[ ] Evidence tags used consistently
[ ] Confidence stated explicitly
[ ] Sources cited for each finding
[ ] Stop condition addressed
```

If any check fails, Expert redoes that table before committing.

---

## 6. Post-Comparison Expert Behavior

After comparison is committed:

- Expert MAY be asked to defend its outputs (per-table or cumulative)
- Expert MAY be asked to re-examine a specific table if comparison reveals concerns
- Expert MAY NOT modify its original Result B files (immutable for traceability)
- Expert MAY add a `## 8. Post-Comparison Reflection` section if specifically requested

The **immutability of Result B** is critical for honest comparison. If Expert changes its mind, that's logged as a new finding, not a silent edit.

---

## 7. Failure Modes and Recovery

| Failure | Detection | Recovery |
|---------|-----------|----------|
| Expert reads Skill output | Post-hoc comparison reveals suspicious alignment | Re-run that table with stricter isolation |
| Expert skips dimensions | Self-check fails | Re-do that table |
| Expert invents Master Spec rules | Citation check | Flag as Gap, not Expert error |
| Expert triggers HGs incorrectly (over-trigger) | Comparison with Skill | Classify as INDEPENDENT JUDGE ERROR |
| Expert fails to trigger real HGs | Comparison with Skill | Classify as REAL SKILL MISS (if Skill triggered correctly) or both-missed |
| Expert output too brief | Self-check fails | Re-do with more depth |

---

## 8. Cross-References

- **R2 Master Plan**: `p8-a/r2/R2-MASTER-PLAN.md` — overall framework
- **R2 Comparison Protocol**: `p8-a/r2/R2-COMPARISON-PROTOCOL.md` — 8 metrics + 4 safety gates
- **table-refactor-expert Skill**: `.claude/skills/table-refactor-expert/SKILL.md` — Skill being evaluated
- **Frozen context**: Universal Skill v1.0, JNPF Extension v1.0, Foundry Target Profile v1.0

---

**Document version**: 1.0
**Prepared by**: AI Engineer
**Date**: 2026-08-30
**Status**: Ready for Independent AI Expert sessions
