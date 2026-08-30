# R2 — AI Expert Comparative Validation Master Plan

> **Phase**: 8 — P8-A.5 (R2)
> **Status**: 🟢 **READY FOR EXECUTION** (Framework established; Round 1 table selection in progress)
> **Date**: 2026-08-30
> **Authority**: Chief Architect directive 2026-08-30 (R2 Comparative Validation Upgrade)
> **Goal**: Verify `table-refactor-expert` Skill has stable, repeatable, transferable expert judgment
> **Method**: Skill vs Independent AI Expert Judge — fully isolated, blind comparative

---

## 1. Why This Document Exists

### 1.1 Background

The original validation model treated **Human Blind Review** as the primary Skill verification mechanism (R1). This required human reviewers to redo 5+ tables independently — 20–30 hours of repeated auditing labor.

The Chief Architect directive of 2026-08-30 **upgrades the validation model**:

| Role | Original (R1-centric) | New (R2 Comparative) |
|---|---|---|
| **Skill** | Subject | Subject |
| **Human Reviewer** | Primary Reference | High-risk Governance Only |
| **Independent AI Expert** | Adversarial (Track B) | Primary Reference |

### 1.2 What R2 Tests

R2 validates whether **`table-refactor-expert` Skill** has:

1. **Stability** — Same input produces same output (intra-Skill)
2. **Repeatability** — Different reviewers can verify the output (inter-reviewer)
3. **Transferability** — The judgment pattern generalizes across table types (cross-domain)
4. **Safety** — Hard Gate False Negative Rate ≤ 0; P0/P1 Error ≤ 0

R2 does NOT prove "AI = Human". R2 proves "**Skill's expert judgment is stable enough to operate production at scale, with humans reserved for high-risk governance only**".

---

## 2. Core Design Principles

### 2.1 Independent Execution (HARD CONSTRAINT)

```
Source Evidence (DB metadata + C# entity + frozen context)
        │
        ├──→ table-refactor-expert Skill
        │       └──→ Result A
        │
        └──→ Independent AI Expert Judge
                └──→ Result B

ONLY AFTER both A and B are committed:
        └──→ Comparison Engine
                └──→ 8 metrics + 4 safety gates
```

**Anti-patterns FORBIDDEN**:

```
❌ AI Expert reads Skill output → "I agree with Skill"
❌ Skill sees AI Expert output → adjusts judgment
❌ One-shot joint review → no isolation
❌ Sequential execution → implicit anchoring
```

### 2.2 Equal Capability, Different Process

| Aspect | Skill | Independent AI Expert |
|---|---|---|
| **Reads** | DB metadata + C# entity + Master Spec + Execution Manual + Universal Skill + JNPF Extension + Foundry Profile | Same source set + freedom to use any extra tools |
| **Output protocol** | A–G assessment per Execution Manual §6 | A–G assessment per R2 Output Schema (§3 below) |
| **Process weight** | Risk-Adaptive Flow per Execution Manual §4 | Free-form expert reasoning, must produce structured output |
| **Hard Gate detection** | Per Master Spec §10.3 | Per Master Spec §10.3 (same source, no privileged access) |
| **Decision authority** | None (Skill = advisor) | None (Independent Expert = reference) |

The two paths are **symmetric in source access** but **independent in process**. This is the only way to test whether Skill's process produces stable judgments.

### 2.3 Stop Rule — Evidence Sufficiency

```
After 10 tables (5 + 5):

IF  P0/P1 Decision Error = 0
AND Hard Gate False Negative = 0
AND Scope Error = 0
AND Closure Error = 0
AND no repeated systematic defect patterns:
    → Comparative Gate PASS
    → STOP (do not extend to Round 3)

IF any of above fail OR ≥3 same-type disagreement pattern:
    → Root Cause Analysis
    → Local Calibration (Skill / Extension / Master Spec / Foundry)
    → Targeted Regression (subset of tables, NOT full re-run)
    → Resume
```

**Stop rule is mandatory**. R2 is NOT a process of "more rounds = more confidence". R2 is a process of "enough evidence to commit".

---

## 3. R2 Output Schema

Both Skill (Result A) and Independent AI Expert (Result B) MUST produce this structure for each Table Unit:

```markdown
# R2 — [Round N] Table [NN] — [table_name]

> **Date**: 2026-08-30
> **Reviewer type**: Skill / Independent AI Expert
> **Source evidence**: [list of files / SQL queries read]

## 1. Table Overview
- Schema: [column count, key columns, types]
- Row count: [number]
- Tenant: YES/NO ([column name])
- SoftDelete: YES/NO ([column name])
- Entity: [path or "NONE — dynamic"]
- FKs in/out: [count]
- Special: [any notable characteristic]

## 2. Seven-Dimension Assessment (A–G)
| Dim | Finding | Evidence | [KNOWN]/[COMPUTED]/[INFERRED]/[GUESS] |
|-----|---------|----------|------|
| A Schema | ... | ... | ... |
| B Integrity | ... | ... | ... |
| C Index | ... | ... | ... |
| D Lifecycle | ... | ... | ... |
| E CRUD/Query | ... | ... | ... |
| F DDD | ... | ... | ... |
| G Consumer/Target | ... | ... | ... |

## 3. Risk Classification
- Risk: R0 / R1 / R2 / R3+
- Confidence: HIGH / MEDIUM / LOW
- Rationale: [why this risk level]

## 4. Hard Gate Assessment (5 HGs)
| HG | Triggered? | Reason |
|----|-----------|--------|
| HG#1 Tenant Isolation | YES/NO/borderline | ... |
| HG#2 Data Integrity | YES/NO/borderline | ... |
| HG#3 Migration | YES/NO/borderline | ... |
| HG#4 Cross-Module | YES/NO/borderline | ... |
| HG#5 Business Ambiguity | YES/NO/borderline | ... |

## 5. Recommended Action
- Action: AUTO-CLOSE / AUTO-APPLY / EVIDENCE-DRIVEN / HUMAN APPROVAL / CROSS-TABLE / DESTRUCTIVE
- Closure: NO-CHANGE / REFACTOR / DEFERRED / ACCEPT-AS-IS

## 6. Evidence Basis
- Sources read: [list]
- Evidence tags used: [KNOWN/COMPUTED/INFERRED/GUESS counts]
- Stop condition met: YES/NO
```

This is the **minimum viable output**. Independent AI Expert may add additional reasoning sections. Skill produces this naturally per Execution Manual §6 / §7.

---

## 4. Comparison Engine

### 4.1 8 Comparison Metrics

For each Table Unit, compute:

| # | Metric | Definition | Pass Threshold |
|---|--------|------------|----------------|
| 1 | **Dimension Agreement** | Per-dimension (A–G) match rate between Skill and Expert | ≥ 75% dimensions agree |
| 2 | **Finding Agreement** | Substantive findings (issues identified) overlap | ≥ 60% overlap on critical findings |
| 3 | **Risk Agreement** | Risk level (R0/R1/R2/R3+) matches | Exact match OR adjacent (R1 vs R2 OK, R0 vs R3+ NOT OK) |
| 4 | **Hard Gate Agreement** | HG triggers match exactly | Exact match (borderline ≠ triggered) |
| 5 | **Action Agreement** | Recommended action matches | Exact match OR semantically equivalent |
| 6 | **Closure Agreement** | Final closure matches | Exact match OR documented justification |
| 7 | **Evidence Sufficiency Agreement** | Both met stop condition at appropriate time | Both = YES |
| 8 | **Scope/Boundary Agreement** | Both correctly identified table's scope boundary | Both consistent |

### 4.2 4 Safety Gates

| # | Safety Gate | Threshold | Escalation |
|---|-------------|-----------|------------|
| **S1** | **Hard Gate False Negative** | ≤ 0 across 10 tables | ANY → Human Governance review |
| **S2** | **P0/P1 Decision Error** | ≤ 0 across 10 tables | ANY → Human Governance review |
| **S3** | **Scope Error** (Out-of-scope table marked in-scope or vice versa) | ≤ 0 | ANY → Human Governance review |
| **S4** | **Closure Error** (CLOSED but actually has unresolved Finding) | ≤ 2 (minor only) | ANY major → Human Governance review |

### 4.3 Disagreement Classification

When Skill and Expert disagree, classify the disagreement:

| Class | Meaning | Default Resolution |
|-------|---------|-------------------|
| **AGREEMENT** | Both produce same conclusion | Record as evidence |
| **SAFE DISAGREEMENT** | Different but neither wrong (e.g., R2 vs R3+) | Record; not blocking |
| **REAL SKILL MISS** | Expert correct, Skill missed critical finding | Skill calibration item |
| **INDEPENDENT JUDGE ERROR** | Skill correct, Expert made error | Expert feedback; not Skill issue |
| **EVIDENCE DIFFERENCE** | Both used different evidence bases | Resolve via Master Spec |
| **RUBRIC DIFFERENCE** | Both correctly applied but different rubric interpretation | Document; may need spec clarification |

This classification is the **core diagnostic output** of R2.

---

## 5. Round Structure

### 5.1 Round 1 — Normal Production Stability

**Goal**: Verify Skill performs reliably on normal production scenarios.

**Selection criteria**:
- Mix of R0/R1, R2, R3+
- Mix of Entity-mapped and dynamic
- Mix of modules (system, workflow, inteAssistant, etc.)
- Mix of complexity (low, medium, high)
- Must include: ≥1 dependency graph hub, ≥1 lifecycle-complex table, ≥1 dynamic/no-entity table
- Must NOT include: any P8-A Shadow table (5 already done), any P8-B executed table (30 already done)

### 5.2 Round 2 — Adversarial / Boundary Stability

**Goal**: Stress-test Skill against harder cases likely to expose calibration gaps.

**Selection criteria**:
- FK-heavy (multiple incoming/outgoing)
- Self-reference patterns (via application logic even if no DB-level self-ref)
- Lifecycle ambiguity (multiple state fields, unclear transitions)
- Tenant + soft-delete + audit combinations
- Legacy naming / unusual column names
- Unusual indexing patterns
- Dynamic / no-entity
- High-impact / large-row-count tables
- **Strictly different** from Round 1 (no overlap)

---

## 6. Coverage Matrix

R2 must satisfy this matrix across the 10 tables:

| Dimension \ Risk | R0/R1 | R2 | R3+ |
|----------------|-------|----|----|
| **A Schema** | ✓ (1+) | ✓ (1+) | ✓ (1+) |
| **B Integrity** | ✓ (1+) | ✓ (1+) | ✓ (1+) |
| **C Index** | ✓ (1+) | ✓ (1+) | ✓ (1+) |
| **D Lifecycle** | ✓ (1+) | ✓ (1+) | ✓ (1+) |
| **E CRUD/Query** | ✓ (1+) | ✓ (1+) | ✓ (1+) |
| **F DDD** | ✓ (1+) | ✓ (1+) | ✓ (1+) |
| **G Consumer/Target** | ✓ (1+) | ✓ (1+) | ✓ (1+) |

**Key principle**: Same dimension can appear at multiple difficulties. E.g., Index — normal in Round 1, Index — FK-heavy in Round 2.

---

## 7. Execution Workflow

### 7.1 Round N Workflow

```
Step 1: Selection Confirmed (Round N — 5 tables)
   ↓
Step 2: Skill Execution (Result A)
        - table-refactor-expert invoked on each table
        - Produces Result A (5 files in r2/round-N/skill/)
        - Skill does NOT see Result B
   ↓
Step 3: Independent AI Expert Execution (Result B)
        - Independent AI Engineer session (no Skill output visible)
        - Produces Result B (5 files in r2/round-N/expert/)
        - Expert does NOT see Result A
   ↓
Step 4: AI Engineer Comparison (only after both committed)
        - Per-table comparison (8 metrics + 4 safety gates)
        - Disagreement classification
        - Output: r2/round-N/comparison/per-table-comparison.md
        - Output: r2/round-N/comparison/cumulative-comparison.md
   ↓
Step 5: Decision
        - All safety gates PASS → continue to next Round
        - ANY safety gate FAIL → Human Governance review
        - Systematic pattern detected → Root Cause Analysis
```

### 7.2 Cumulative Analysis (After Round 2)

```
Round 1 cumulative
   +
Round 2 cumulative
   ↓
Cross-Round Analysis
   - Hard Gate FN rate: [count]/10
   - P0/P1 rate: [count]/10
   - Closure error rate: [count]/10
   - Disagreement distribution: [AGREE/SAFE/REAL MISS/JUDGE ERROR/EVIDENCE/RUBRIC]
   ↓
Comparative Gate Decision (PASS / FAIL / CONDITIONAL)
```

---

## 8. Deliverables

### 8.1 Per-Round Outputs

```
p8-a/r2/
├── round-1/
│   ├── selection.md                    (5 selected tables + rationale)
│   ├── skill/                          (Result A — 5 files)
│   ├── expert/                         (Result B — 5 files)
│   └── comparison/
│       ├── per-table-comparison.md     (5 tables × 8 metrics + 4 safety gates)
│       └── cumulative-comparison.md    (Round 1 summary)
└── round-2/
    ├── selection.md                    (5 different selected tables + rationale)
    ├── skill/                          (Result A — 5 files)
    ├── expert/                         (Result B — 5 files)
    └── comparison/
        ├── per-table-comparison.md
        └── cumulative-comparison.md    (Round 2 summary)
```

### 8.2 Final Outputs

```
p8-a/r2/
├── R2-MASTER-PLAN.md                   (this document)
├── R2-EXPERT-PROTOCOL.md               (Independent AI Expert execution protocol)
├── R2-COMPARISON-PROTOCOL.md           (8 metrics + 4 safety gates + 6 disagreement classes)
├── COVERAGE-MATRIX.md                  (Risk × Dimension coverage proof)
├── comparative-gate-decision.md        (PASS/FAIL/conditional + evidence)
└── skill-calibration-items.md          (REAL SKILL MISS items, routed to skill evolution)
```

---

## 9. Stop Conditions (Hard)

R2 STOPS when:

```
[C1] 10 tables completed (5 + 5)
[C2] All 4 safety gates PASS (or CONDITIONAL with documented exceptions)
[C3] No repeated systematic defect pattern (≥3 same-type)
[C4] Cumulative analysis committed

→ Comparative Gate decision recorded
→ Skill calibration items routed
→ Production resumption authorized
```

R2 CONTINUES only if:

```
[+] Systematic pattern detected → Root Cause Analysis → Targeted Regression
[+] New table type discovered → Add to coverage matrix → Targeted Round
```

R2 NEVER:

```
[❌] Auto-extends to Round 3
[❌] Re-runs existing tables (history preserved)
[❌] Modifies Master Spec / Execution Manual mid-validation
[❌] Skips safety gate evaluation
```

---

## 10. Cross-References

- **Master Spec / Execution Manual**: `table-refactor-expert` Skill references
- **P8-A.2 AI Track A** (`p8-a/shadow/ai-track-a-5-tables.md`): Historical Skill execution output (R1 evidence)
- **P8-A.3 Adversarial Track B** (`p8-a/shadow/track-b/*.md`): Historical AI-vs-AI comparison (R1 evidence)
- **P8-A.4 Human Blind Review** (`p8-a/shadow/real-human-blind-review/*.md`): Historical Human Review (kept as evidence, no longer primary)
- **P8-B Executed (Batches 01-06)**: 30 tables / 70 indexes — preserved as Historical Production Evidence
- **P8-C Frozen (Batches 07-17)**: 58 tables / 128 indexes — pending UNFREEZE per R5+R7
- **phase-gate-state.md**: §2.1 Mechanical Execution Gate; §3.2 P8-A Shadow Gate; §3.4 P8-C Exit Gate
- **Universal Skill v1.0 / JNPF Extension v1.0 / Foundry Target Profile v1.0**: Frozen context (REUSED)

---

## 11. Honest Limitations

1. **R2 cannot prove "Skill is perfect"**. It proves "Skill has stable, defensible judgment within defined scope".
2. **Independent AI Expert is not an Oracle**. It is a calibrated reference; can also be wrong.
3. **10 tables is a sample**, not full population. Statistical confidence increases with production volume, not with validation volume.
4. **Coverage Matrix is constructed**, not statistically sampled. Tables are selected to maximize coverage, not randomly.
5. **Stop rule is conservative**, not aggressive. It may stop earlier than some would prefer. That's by design.
6. **No human full-audit replacement** is implied. R2 changes the *primary* validation, not the *total* validation. Humans still review P0/P1, Hard Gate disputes, and Core Evolution.

---

**Document version**: 1.0
**Prepared by**: AI Engineer
**Date**: 2026-08-30
**Status**: Ready for Round 1 execution
