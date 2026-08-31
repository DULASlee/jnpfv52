# Adversarial Track B Protocol

> **Phase**: 8 — P8-A.3
> **Status**: ACTIVE（取代 Blind Review Protocol）
> **Date**: 2026-08-30
> **生效原因**: 独立人类评审员不可得，由 AI 工程师以 Adversarial 角色替代 Blind Review
> **Phase Gate 决策**: 由用户于 2026-08-30 批准（Phase 8 阶段决策记录在 session 对话中）

---

## 1. Purpose

Define the operational protocol for **AI Adversarial Track B** — replacing the original Human Blind Review Track B — to ensure:

1. **Adversarial Independence**: Reviewer (AI) deliberately seeks Track A's errors, blind spots, and weak evidence
2. **Comparability**: Track B output uses the same template format as Blind Track B for clean comparison
3. **Auditability**: All adversarial attacks are documented with reasoning and target Track A evidence
4. **Divergence Discovery**: AI-vs-AI disagreement surfaces genuine uncertainty, not blind agreement

---

## 2. Protocol Substitution Notice

### 2.1 Original Protocol Status

The Blind Review Protocol (`Phase-8-Shadow-Mode-Blind-Review-Protocol.md`) is **WITHDRAWN for P8-A.3**:

- ❌ Original §2 (Reviewer Eligibility) — NOT applicable (no human reviewer available)
- ❌ Original §3 (Isolation Rules) — VIOLATED by definition (AI reviewer has read Track A)
- ✅ Original §4 (Review Process) — REUSED as process skeleton
- ⚠️ Original §5 (Time Budget) — REVISED (see §6 below)
- ✅ Original §6 (Evidence Discipline) — REUSED with stricter enforcement
- ⚠️ Original §7 (Common Bias Avoidance) — REPLACED with Adversarial Bias Set

### 2.2 Honest Acknowledgment

This substitution is methodologically **inferior** to true Blind Review. Specifically:

| Aspect | Blind Review | Adversarial AI Review |
|---|---|---|
| Independence from Track A | Full | None (Track A is the attack surface) |
| Different model family | Likely (different human) | No (same model family) |
| Different cognitive biases | Likely | No (same model biases) |
| Different evidence collection path | Yes | No (same code/DB access) |

**Net effect**: Adversarial Track B is a **calibration check**, not an independent validation. It cannot replace Blind Review for production systems. It is acceptable for **P8-A internal calibration only** because:
- It still surfaces disagreement that exists in the AI's own reasoning
- It documents Track A's vulnerable points for Skill Evolution
- It allows the project to proceed past P8-A in absence of a real human reviewer

---

## 3. Reviewer Role: Adversarial AI

### 3.1 Identity

| Field | Value |
|---|---|
| Reviewer | AI Engineer (this session) |
| Review Slot | P8-A.3 Adversarial |
| Review Date | 2026-08-30 |
| Adversarial Posture | Active (Track A is the target) |
| Track A access | READ (full) |

### 3.2 Mission

> **Find every place where Track A's reasoning is weak, evidence-thin, self-serving, or missing — even if no real issue exists.**

The reviewer is NOT trying to:
- ❌ Find fake problems to inflate divergence count
- ❌ Rubber-stamp Track A to avoid work
- ❌ Re-derive all evidence from scratch (Track A's evidence is admissible)

The reviewer IS trying to:
- ✅ Attack weak `[INFERRED]` claims that should be `[KNOWN]` or `[GUESS]`
- ✅ Attack HG borderline flags that should be full triggers
- ✅ Attack risk classifications that are too generous or too strict
- ✅ Attack recommendations that lack operational justification
- ✅ Identify blind spots in Track A's 7-dimension coverage
- ✅ Identify evidence chains that have circular reasoning

### 3.3 Adversarial Stance

For each Track A finding, ask:
1. **Is this true?** Can the reviewer verify the underlying claim?
2. **Is this well-evidenced?** Is the evidence tag appropriate?
3. **Is this the right risk level?** Could a more conservative (or different) classification be argued?
4. **Are there HGs that Track A waved off?** Did Track A dodge HG#5 by calling it "borderline"?
5. **What did Track A not look at?** Which dimension is thin?

---

## 4. Isolation Rules (MODIFIED)

### 4.1 No Isolation Required

This is the fundamental protocol change:

```
✅ I HAVE read AI Track A document for this table
✅ I HAVE read the AI's Risk classification
✅ I HAVE read the AI's Findings
✅ I HAVE read the AI's Hard Gate judgment
✅ I HAVE read the AI's Recommended Action
✅ I HAVE read the AI's Recommended Closure
```

This is **by design** — the reviewer attacks Track A from inside.

### 4.2 Anti-Bias Discipline (REPLACES §3)

| Bias | Mitigation |
|---|---|
| **Mimicry Bias** (just agree with Track A) | Mandatory: produce ≥ 1 divergence per table, even if artificial at minimum. If genuinely 0 divergence, document reasoning in detail. |
| **Fabrication Bias** (invent fake issues) | Each adversarial finding MUST cite specific Track A text and explain why it's wrong/weak |
| **Severity Inflation Bias** (always say R3+) | Use Confidence levels; disagree only when evidence supports it |
| **Confirmation Bias** (seek what I expect) | After identifying a divergence, search for counter-evidence |

### 4.3 Forbidden Actions

- ❌ Modify Track A documents
- ❌ Fabricate DB queries that weren't actually run (mark unverified queries as `[GUESS]`)
- ❌ Use Track A's evidence verbatim without independent verification
- ❌ Mark `AGREEMENT` on any finding without at least one verification step

---

## 5. Review Process (Per Table)

### Step 1: Re-Discovery (5-10 min)

For each table, the reviewer MUST independently verify the core facts Track A relies on:

1. Re-query `sys.columns` to confirm schema
2. Re-query `sys.indexes` to confirm index state
3. Re-query `sys.foreign_keys` to confirm FK relationships
4. Re-locate Entity code (if exists) — read mapping
5. Confirm F_TENANT_ID, F_DELETE_MARK, F_ENABLED_MARK presence
6. Note: Row count from `sys.dm_db_partition_stats`

**Critical**: Do not trust Track A's evidence ledger — verify each [KNOWN] claim with one direct DB query.

### Step 2: Track A Audit (10-15 min)

For each of Track A's 7-dimension findings:
1. Read Track A's finding text
2. Read Track A's evidence tag
3. Verify: is the evidence tag appropriate for the strength of evidence?
4. Attack: is there a hidden assumption or unstated qualification?
5. Identify: what did Track A NOT look at in this dimension?

### Step 3: Adversarial Risk Re-Classification (5 min)

Independently classify R0/R1 / R2 / R3+:
- Consider Track A's risk, but do NOT auto-agree
- Look for risk drivers Track A understated
- Look for risk reducers Track A missed

### Step 4: Hard Gate Re-Audit (5 min)

For each HG#1-5:
- Track A says NOT triggered? → Try to argue it SHOULD be triggered
- Track A says borderline? → Decide if it should be promoted to triggered
- Track A says triggered? → Try to find counter-evidence

### Step 5: Recommended Action + Closure (5 min)

Independently choose action and closure:
- Track A's choice is a prior, not an answer
- If reviewer disagrees, document the disagreement with operational reasoning

### Step 6: Submission (5 min)

Save to `docs/universal/Phase-8/p8-a/shadow/track-b/{NN}-{table-name}-track-b.md`
Use the same template as Blind Track B, but add an **Adversarial Attack Log** section.

---

## 6. Time Budget (Per Table)

| Step | Expected | Notes |
|---|---|---|
| Re-Discovery | 5-10 min | MUST be done independently |
| Track A Audit | 10-15 min | Core adversarial work |
| Risk Re-Classification | 5 min | |
| Hard Gate Re-Audit | 5 min | |
| Action + Closure | 5 min | |
| Submission | 5 min | |
| **Total per table** | **35-45 min** | Slightly faster than Blind Review (no isolation overhead) |

For 5 tables: ~3-3.75 hours total (single AI reviewer in single session).

---

## 7. Evidence Discipline (STRICTER)

### 7.1 Tag Usage

Same tag taxonomy as Blind Review, but with stricter application:

| Tag | Use when | Adversarial Test |
|---|---|---|
| `[KNOWN]` | Direct DB / code observation (highest) | "Did the reviewer verify this independently, or only from Track A?" |
| `[COMPUTED]` | Derived from known data (medium) | "Is the derivation chain shown?" |
| `[INFERRED]` | Pattern-based inference (lower) | "What pattern? Can it be cited?" |
| `[GUESS]` | Unverified assumption (lowest — avoid) | "If used, MUST be challenged" |
| `[DESIGN]` | Recommendation, not observation | "Is the recommendation operational, not theoretical?" |

### 7.2 Track A Tag Audit

For each Track A evidence tag, the reviewer must answer:
- Did Track A earn this tag, or did Track A over-rate?
- If `[KNOWN]`, did Track A actually run the query, or assert?
- If `[INFERRED]`, what code pattern supports it?
- If `[DESIGN]`, is this a recommendation or a speculation?

### 7.3 Stop Rule

Stop adversarial work when:
- All 7 dimensions have been audited
- All HGs have been re-evaluated
- Risk has been independently classified
- Action and closure have been independently chosen

Do NOT extend adversarial work to invent issues after these are complete.

---

## 8. Comparison Schema (UNCHANGED)

The Comparison Schema (`Phase-8-Shadow-Mode-Comparison-Schema.md`) applies **as-is** to Adversarial Track B:

- L1 Dimension Comparison → AGREEMENT / SAFE DISAGREE / AI FP / AI FN
- L2 Risk Comparison → RISK ERROR if ≥ 2 tier diff
- L3 Hard Gate Comparison → GATE ERROR
- L4 Action Comparison → per template
- L5 Closure Comparison → CLOSURE ERROR

**Adversarial-specific interpretation**:
- High divergence rate IS expected and is NOT a sign of Track A failure
- The purpose is calibration, not agreement maximization
- A clean AGREEMENT on everything means Adversarial Track B did not work

---

## 9. Expected Divergence Distribution

Based on protocol design, we expect the following rough distribution:

| Outcome | Expected | Implication |
|---|---|---|
| AGREEMENT | 30-50% of dimension findings | Track A solid where it agrees |
| SAFE DISAGREEMENT | 30-40% | Track A acceptable but reviewer has different perspective |
| AI FALSE POSITIVE | 5-15% | Track A over-flagged |
| AI FALSE NEGATIVE | 5-15% | Track A missed something |
| RISK ERROR | 1-3 per 5 tables | Reviewer thinks Track A mis-classified risk |
| HG ERROR | 0-1 per 5 tables | Reviewer thinks Track A mis-judged HG |
| CLOSURE ERROR | 0-1 per 5 tables | Reviewer thinks closure decision wrong |

If actual rates are far from these, it suggests either Track A is unusually good/bad or Adversarial Track B is failing its calibration mission.

---

## 10. After All 5 Tables Submitted

Same as Blind Review §8:
- AI Engineer executes comparison (not reviewer)
- Cumulative Safety Gate calculation
- Productivity baseline recording
- Shadow Gate decision

**Additional for Adversarial**:
- Divergence log includes "Attack Success Rate" (how many adversarial attacks landed)
- High attack success rate on HG borderline flags → Skill Evolution priority
- High attack success rate on Risk classification → Master Spec Evolution consideration

---

## 11. Violations

| Violation | Action |
|---|---|
| Reviewer did not attack Track A | Void that table's Track B; require fresh adversarial review |
| Reviewer fabricated issues not supported by Track A text | Void that finding only; rest of Track B stands |
| Reviewer copied Track A without independent assessment | Void that table's Track B |
| Track B contains AI Track A content without adversarial analysis | Void Track B |

---

## 12. End of Protocol

This protocol is binding for P8-A.3 Adversarial Review.

Modifications require Phase Gate approval (same as Blind Review Protocol §13).

---

## 13. Phase Gate Decision Record

```
Date:        2026-08-30
Decision:    Accept Adversarial Protocol as Track B substitute
Reason:      Independent human reviewer not available; P8-A cannot be blocked indefinitely
Risk:        Adversarial AI review is methodologically inferior to Blind Review
Mitigation:  P8-B Controlled Production will exercise the Skill in real execution,
             providing additional validation beyond Track A/B comparison
Approval:    User (project lead) — recorded in session
```

---

## 14. Compatibility Notes

- All Track B output documents use the **same template** as Blind Track B for tool compatibility
- An additional "Adversarial Attack Log" section is added per table (template modification)
- Comparison Schema is reused unchanged
- Shadow Gate calculation is reused unchanged
- The ONLY conceptual difference: Track B reviewer has read Track A
