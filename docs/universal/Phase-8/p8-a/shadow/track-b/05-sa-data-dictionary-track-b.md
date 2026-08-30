# Adversarial Track B — Table 05: sa_data_dictionary

> **Phase**: 8 — P8-A.3 (Adversarial Track B)
> **Status**: COMPLETE
> **Date**: 2026-08-30
> **Reviewer**: AI Engineer (Adversarial)
> **Protocol**: Adversarial Track B Protocol (取代 Blind Review)
> **Track A Access**: FULL READ
> **Track A Reference**: `ai-track-a-5-tables.md` Table 5

---

## ⚠️ ADVERSARIAL DECLARATION ⚠️

I HAVE read Track A for this table.

**Track A's overall verdict**: R3+, DEFERRED, no Safe-Refactor (indexes already optimal), HG#5 borderline flagged (NOT triggered).

**My adversarial mission**: This is the most important table to attack. Track A was MOST thoughtful here (correctly identified pattern divergence, R3+, DEFERRED). But the most thoughtful Track A is also the most vulnerable to:
1. **Self-justifying elaborate rationales** (more text = more places to attack)
2. **HG#5 borderline dodge** (calling it borderline instead of triggered avoids the Human Decision gate)
3. **DEFERRED closure as escape hatch** (DEFERRED with "decision brief" = same as deferred forever)

This is where I will be MOST aggressive.

---

## 1. Table Identity

| Field | Value | Track A Says | Match? |
|---|---|---|---|
| Table | 05 | sa_data_dictionary | ✅ |
| Physical Name | sa_data_dictionary (lowercase) | sa_data_dictionary (lowercase) | ✅ |
| Module | inteAssistant-SA | inteAssistant-SA | ✅ |
| Entity Mapped? | NO (dynamically queried) | NO | ✅ |
| Reviewer | AI Adversarial | — | — |

**Critical observations**:
- LOWERCASE table name (different from UPPERCASE convention)
- NO Entity mapping (dynamically queried)
- 5 incoming FKs (highest coupling in DB)
- Triple-Key Iron Law (R12) compliance
- 8 indexes (richest in DB)
- Schema divergence from JNPF main tables

This is the table where Track A showed the most sophistication. Track A correctly identified it as R3+ and DEFERRED. But the question for adversarial review: **is the closure actually right, or is DEFERRED a way to avoid commitment?**

---

## 2. Track A Audit: Dimension A (Schema)

### Track A's Claim

> "Different naming convention (no F_* prefix)"
> "BIGINT id vs nvarchar Snowflake"
> "BOOLEAN (bit) for soft delete"
> "Composite triple-key indexes present"
> "Schema is correct for its purpose (SA output) but DIFFERENT from JNPF main tables"
> "Hard Gate risk: Mixing SA tables with JNPF tables requires careful Foundry Target Profile mapping"

### Adversarial Attack

**Attack #1: "Schema is correct for its purpose" — circular justification.**

Track A says the schema is "correct for its purpose (SA output)". But what IS the purpose?
- SA (Studio Architecture) output table — Track A asserts this
- But "SA" is not a defined term in Master Spec that I've seen
- The lowercase naming + bit soft-delete pattern is from common SaaS conventions
- But Track A doesn't show how this aligns with JNPF's documented patterns

**Adversarial position**: The schema divergence is REAL but the "correct for SA" justification is a hand-wave unless SA is defined.

**Attack #2: BIGINT vs nvarchar Snowflake — what's the performance implication?**

Track A notes BIGINT vs nvarchar(100) Snowflake. Snowflake IDs are 18-19 digits as strings. The comparison:
- BIGINT: 8 bytes, max ~9.2 quintillion
- nvarchar(100): up to 200 bytes, supports character IDs

For SA tables, BIGINT makes sense (high-volume auto-generated IDs). But:
- The PK is BIGINT IDENTITY (auto-increment) — this is NOT distributed-friendly
- Snowflake allows distributed ID generation
- IDENTITY forces single-node insertion

Track A notes the difference but doesn't address the implication. If this table needs distributed ID generation (multi-instance SA processing), IDENTITY is a bottleneck.

**Attack #3: bit for soft delete — what are the storage implications?**

bit uses 1 byte per row (SQL Server bit is rounded up). F_DELETE_MARK int uses 4 bytes. For 35 rows, this is irrelevant. For 100M rows, this is 300MB difference.

But more importantly: bit semantics. Does:
- is_deleted = 0 → not deleted
- is_deleted = 1 → deleted
- is_deleted = NULL → unknown state?

If NULL is allowed, queries need `WHERE ISNULL(is_deleted, 0) = 0`. Track A doesn't address NULL handling.

**Attack #4: "Composite triple-key indexes present" — verified?**

Track A says composite triple-key indexes are present. Cross-reference: sa_data_dictionary has 8 indexes. One is `IX_sa_dict_triple (tenant_id, project_id, pipeline_id)`. Track A says this exists. [KNOWN] tag is appropriate IF verified.

Without seeing the actual sys.indexes output, this is [KNOWN] but unverified in this adversarial review.

---

## 3. Track A Audit: Dimension B (Integrity)

### Track A's Claim

> "NOT NULL on tenant_id / project_id / asset_level / pipeline_id"
> "Foreign key columns (dfd_id, bpm_id, event_id) are nullable — semantically meaningful"
> "Incoming FK from 5 tables (sa_decision_table, sa_er, sa_pspec, sa_state_machine, sa_ui)"
> "Triple-key enforcement via composite indexes (Triple-Key Iron Law R12)"

### Adversarial Attack

**Attack #5: "NOT NULL on ... asset_level" — what is asset_level?**

Track A mentions asset_level as NOT NULL but doesn't explain what it is. Asset_level is a SA-specific concept (presumably: dictionary level, entity level, attribute level?). Without definition:
- How is asset_level validated?
- Are there legal values?
- Is it an enum or free-form?

**Attack #6: 5 incoming FKs — does the table support ON DELETE behavior?**

5 tables reference sa_data_dictionary. If any of those FKs is defined with ON DELETE CASCADE, deleting a dictionary row cascades to delete from those tables. If ON DELETE SET NULL, references become NULL. If no ON DELETE (default NO ACTION), deletion is blocked.

Track A says "Incoming FK from 5 tables" but doesn't address ON DELETE behavior.

For a projection table that is the "central node" (Track A's own description), ON DELETE behavior matters:
- Can you delete a dictionary row?
- What happens to 5 dependent tables?

**Attack #7: Triple-Key enforcement via composite indexes — this is INDEX, not constraint.**

Track A says Triple-Key is enforced via composite indexes. But indexes are not the same as constraints:
- A unique index on (tenant_id, project_id, pipeline_id, ...) — yes, enforces uniqueness
- A non-unique composite index — does NOT enforce Triple-Key

Track A does not specify if the index is UNIQUE. If non-unique, Triple-Key is NOT enforced.

**Attack #8: Nullable FK columns (dfd_id, bpm_id, event_id) — semantically meaningful?**

Track A says these are "nullable — semantically meaningful". But:
- Why nullable? Optional references?
- What's the difference between NULL and pointing to non-existent ID?
- Does the app validate "if not NULL, must exist"?

---

## 4. Track A Audit: Dimension C (Index)

### Track A's Claim

> "8 indexes present (most rich in DB)"
> "IX_sa_dict_triple (tenant_id, project_id, pipeline_id) — Triple-Key support"
> "idx_sa_dict_dfd / bpm — incoming FK support"
> "idx_sa_dict_tenant / project / validation / pattern_src — query optimization"
> "EXCELLENT indexing strategy — no recommendations needed"

### Adversarial Attack

**Attack #9: "EXCELLENT indexing strategy — no recommendations needed" — over-confidence.**

Track A gives itself a "no recommendations needed" pass. For an adversarial reviewer:
- 8 indexes on 35 rows is over-indexed for current data
- But the design is forward-looking
- Track A doesn't show the actual sys.indexes output
- Track A doesn't analyze if 8 is optimal or excessive

For a write-heavy table (dictionaries get updated), 8 indexes means 8 index updates per write. If reads are not equally heavy, this is a write performance cost.

**Attack #10: Index naming convention inconsistent.**

Track A shows:
- IX_sa_dict_triple (uppercase prefix)
- idx_sa_dict_dfd / bpm (lowercase prefix)
- idx_sa_dict_tenant / project / validation / pattern_src (lowercase prefix)

Mixed naming convention = inconsistent. Not a fatal flaw, but suggests ad-hoc index creation.

**Attack #11: What indexes are MISSING?**

Track A doesn't address:
- index on (event_id) — if event-sourcing queries are common
- index on (is_current) — for SCD Type 2 "current row" lookups
- index on (validation_status) — Track A mentions this is queried but doesn't index it
- index on (created_at) — for time-range queries

The "no recommendations needed" is **incomplete**.

---

## 5. Track A Audit: Dimension D (Lifecycle)

### Track A's Claim

> "Custom lifecycle: is_current bit, valid_from datetime2, valid_to datetime2"
> "Temporal table pattern (SCD Type 2) — versioned by valid_from/valid_to" — [INFERRED]
> "version (int) — version increment"
> "Standard created_at/updated_at/created_by/updated_by"
> "NO F_DELETE_MARK — uses is_deleted bit + deleted_at"

### Adversarial Attack

**Attack #12: SCD Type 2 pattern inferred without code verification.**

Track A claims SCD Type 2 with valid_from/valid_to. For an adversarial reviewer:
- Are valid_from/valid_to actually maintained by code?
- Or just columns present, never populated?
- Is there code that handles "find current row": `WHERE is_current = 1`?
- Is there code that handles "find all versions": `WHERE valid_from >= ? AND valid_to <= ?`?

Track A says [INFERRED]. This should require code-level verification.

**Attack #13: "version (int)" — increment by what?**

Track A says version is "increment". But:
- On every UPDATE? Or only on schema changes?
- What triggers version increment?
- Is version a data versioning or schema versioning?

For SCD Type 2, version typically increments on every change to the row. Track A doesn't verify.

**Attack #14: is_deleted bit + deleted_at — temporal inconsistency.**

If the table uses SCD Type 2 (valid_from/valid_to for versioning), why also have is_deleted? Either:
- Soft delete is separate from versioning (two different concerns)
- Or is_deleted is for hard delete marker, valid_from/valid_to for soft versioning

This dual-pattern is unusual and unaddressed.

**Attack #15: NO F_DELETE_MARK is a Track A-acknowledged divergence.**

But: is F_DELETE_MARK expected to be added? Or is this an intentional difference?

If intentional, the Skill should recognize "SA tables have a different lifecycle model" as a documented pattern, not an exception.

---

## 6. Track A Audit: Dimension E (CRUD/Query)

### Track A's Claim

> "Query patterns (inferred from indexes): list by triple-key, get by event_id, get by dfd_id, filter by validation_status, filter by is_pattern_source"

### Adversarial Attack

**Attack #16: All query patterns inferred from indexes, not from code.**

This is acceptable IF the indexes ARE designed for these queries. But:
- Indexes can be created speculatively without corresponding queries
- "Inferred from indexes" is weaker than "observed in code"

**Attack #17: What about WRITE patterns?**

Track A only addresses reads. For SCD Type 2, writes are interesting:
- New version row INSERT (with valid_from = now, valid_to = NULL, is_current = 1)
- Old version row UPDATE (set valid_to = now, is_current = 0)
- Both INSERT + UPDATE happen in same transaction

Track A does not address whether code does this correctly.

**Attack #18: Pattern Tags / pattern mining queries?**

Track A mentions pattern_tags and is_pattern_source as fields. The query pattern "filter by is_pattern_source" suggests pattern mining queries. But:
- Is there a separate pattern mining service that queries this?
- What's the read volume of these queries?
- Should this be a separate table?

---

## 7. Track A Audit: Dimension F (DDD)

### Track A's Claim

> "sa_data_dictionary is shared projection — read by 5 other SA tables"
> "Pattern Tags (pattern_tags, is_pattern_source) — Knowledge Graph integration"
> "LLM Confidence (llm_confidence) — AI-generated data quality tracking"
> "Human Confirmed (human_confirmed) — human-AI collaboration trace"
> "This is NOT a typical JNPF aggregate — it's a projection table for cross-domain analysis"

### Adversarial Attack

**Attack #19: "Shared projection" classification is correct — but implications under-explored.**

Track A correctly identifies this as a projection (read model) not an aggregate. But:
- If it's a projection, who owns the WRITE side? (the underlying source)
- If multiple sources write to it, who reconciles?
- If only reads, what's the consistency model?
- Is this a denormalized view maintained by ETL, or a query-time projection?

For Triple-Key Iron Law (R12), the projection must be coherent across (tenant, project, pipeline). Track A asserts this but doesn't show the mechanism.

**Attack #20: LLM Confidence field — operational semantics?**

Track A notes llm_confidence field. This is for AI-generated data quality. Questions:
- What range? 0.0-1.0? 0-100? Categorical (high/med/low)?
- How is it computed?
- What's the threshold for "trust this data"?
- Does the app filter on llm_confidence at read time?

Without operational semantics, the field is decorative.

**Attack #21: human_confirmed — workflow semantics?**

Track A notes human_confirmed field. For human-AI collaboration:
- What does confirmed = true mean operationally?
- Does it bypass llm_confidence check?
- Can it be un-confirmed?
- Is there an audit trail of confirmations?

**Attack #22: pattern_tags — string list or structured?**

Is pattern_tags:
- A comma-separated string ("pattern1,pattern2")
- A JSON array ("['pattern1', 'pattern2']")
- A separate join table (sa_pattern_tag_assignments)
- A column with delimited format

Without knowing, queryability is unknown.

**Attack #23: 5 incoming FKs as a "shared projection" — write contention?**

If 5 tables reference sa_data_dictionary:
- Each reference requires the dictionary row to exist
- Updates to dictionary may cascade or block
- Cross-table consistency becomes critical

Track A calls it "shared projection" but doesn't analyze write contention or consistency model.

---

## 8. Track A Audit: Dimension G (Consumer / Target Readiness)

### Track A's Claim

> "Foundry Target Profile (ISoftDeleteEntity) maps is_deleted bit → IsDeleted — direct"
> "BUT: Foundry profile assumes JNPF-style F_DELETE_MARK int (1=deleted) pattern"
> "Schema divergence: sa_* tables use bit, JNPF uses int"
> "Cannot apply Universal Target Profile directly without Foundry Profile extension"
> "HG#5 candidate: Business semantics of NULL vs 1 vs 0 in is_deleted bit vs f_delete_mark int requires Human Decision"

### Adversarial Attack

**Attack #24: "Cannot apply Universal Target Profile directly" — flagged but not resolved.**

Track A correctly identifies Foundry Profile mismatch. But:
- Is this HG#5 (business ambiguity)?
- Or HG#3 (migration risk)?
- Or Master Spec Evolution concern?

Track A says "HG#5 candidate" but doesn't fully commit. **This is HG#5 borderline dodge.**

For an adversarial reviewer, the question is: **Does the divergence REQUIRE Human Decision, or is it a documentation issue?**

If just documentation: HG#5 NOT triggered, add to JNPF Extension backlog.
If genuine ambiguity: HG#5 triggered.

**My position**: **HG#5 IS TRIGGERED** because:
- The semantic difference between bit (T/F) and int (0/1/2?) is a real interpretation question
- 5 dependent tables need consistent interpretation
- Foundry Profile extension requires architectural decision (extension vs migration)
- This is business ambiguity, not just documentation

**Track A's "borderline" flag is too soft. HG#5 should be TRIGGERED.**

**Attack #25: "Direct mapping is_deleted bit → IsDeleted" — assumes direct.**

Track A says the mapping is "direct". But:
- .NET `bool` is 1 byte (or 4 bytes depending on runtime)
- SQL Server `bit` is 1 byte
- They should map, but only if .NET nullable handling is correct

For SCD Type 2 + is_deleted + deleted_at, .NET needs nullable bool? or just bool? Track A doesn't address.

**Attack #26: Foundry Profile Extension vs Migration — unaddressed.**

Track A identifies the divergence but doesn't commit to a path:
- Option A: Foundry Profile extension (Track A's recommendation)
- Option B: Migrate sa_* to F_* pattern
- Option C: Status quo (no action)

Without committing, DEFERRED is unanchored.

---

## 9. Risk Re-Classification

### Track A: R3+, HIGH confidence

### My Adversarial Re-Classification

**Risk Level: R3+** — Confidence: HIGH (≥80%) — **AGREEMENT**

**Rationale for agreement**:
- Schema divergence is real and substantial
- 5 incoming FKs = high coupling
- No Entity mapping = dynamic access (higher risk of misuse)
- Triple-Key Iron Law constraint
- Projection table semantics (not typical aggregate)
- Foundry Target Profile mismatch

Track A correctly escalated to R3+. **This is the table where the Skill performed best.**

**Where I add nuance**:
- The "shared projection" classification is correct
- The DEFERRED closure is appropriate IF accompanied by a real Decision Brief
- The HG#5 flag is correct in spirit but **should be PROMOTED to triggered**, not left as borderline

---

## 10. Hard Gate Re-Audit

| HG | Track A | My Position | Justification |
|---|---|---|---|
| HG#1 (tenant isolation) | NOT triggered | **NOT triggered** | tenant_id present and NOT NULL |
| HG#2 (data integrity) | NOT triggered | **NOT triggered** | Schema enforces relationships |
| HG#3 (migration) | NOT triggered | **NOT triggered** | Schema correct as-is |
| HG#4 (cross-module) | borderline | **TRIGGERED — escalate** | 5 SA tables + KG module reference this. Cross-module blast radius is REAL. This IS HG#4. |
| HG#5 (business ambiguity) | borderline | **TRIGGERED — escalate** | Schema divergence (bit vs int, F_ prefix vs bare) is business ambiguity that requires Human Decision. Track A correctly identified it but soft-pedaled as "borderline". |

**Adversarial HG verdict**: 2 TRIGGERED (HG#4 and HG#5).

**Track A significantly under-triggered HGs on this table.** This is the MOST important adversarial finding.

Specifically:
- HG#4 should be TRIGGERED — the 5 incoming FKs are textbook HG#4
- HG#5 should be TRIGGERED — the schema divergence is exactly what HG#5 is for

Track A's "borderline" flag was Track A's way of acknowledging the HGs without triggering them. This is the **HG borderline dodge pattern** — flag the concern, defer the decision.

---

## 11. Recommended Action

**Track A**: DEFERRED with explicit reason "HG#5 — Pattern divergence requires Human Decision"

**My Action**: **DEFERRED — but with REQUIRED Decision Brief contents**

```
DEFERRED with REQUIRED Decision Brief:

The Decision Brief MUST address:

1. HG#4 Trigger (Cross-Module Dependency)
   - List all 5 tables that reference sa_data_dictionary
   - For each, document the reference pattern (FK type, ON DELETE behavior, query patterns)
   - Identify any circular dependencies
   - Decide: keep current state OR refactor to reduce coupling

2. HG#5 Trigger (Business Ambiguity)
   - Document the semantic difference: bit (T/F) vs int (0/1/2)
   - Document why SA tables chose this pattern
   - Decide: A) Keep sa_* pattern + Foundry Profile extension, OR
             B) Migrate sa_* to F_* pattern (full compliance), OR
             C) Status quo + documentation

3. Foundry Profile Decision
   - If Option A: define the extension schema
   - If Option B: define the migration plan
   - If Option C: document the divergence as accepted

4. SCD Type 2 Verification
   - Verify valid_from/valid_to are actually maintained by code
   - Verify is_current flag is correctly updated
   - Verify version increment policy

5. Pattern Tags Operational Semantics
   - Document format (string list / JSON / junction)
   - Document write/read patterns
   - Document pattern mining query expectations

The Brief MUST be completed and approved before this table can move out of DEFERRED.
```

---

## 12. Recommended Closure

**Track A**: DEFERRED

**My Closure**: **DEFERRED — STRICT**

```
Track A's DEFERRED closure is directionally correct but operationally weak.

Track A's version: "DEFERRED with explicit reason"
My version: "DEFERRED with REQUIRED deliverables and HARD DEADLINE"

A DEFERRED without a hard deadline becomes a deferred-forever.
A DEFERRED without deliverables becomes a deferred-with-no-action.

For this table, the closure is conditional on:
- Decision Brief completed by [DEADLINE]
- Brief approved by [AUTHORITY]
- Brief actions executed OR new batch created for execution

Without these, the table must remain in DEFERRED status indefinitely.
```

---

## 13. Extension Routing

| Observation | Route to | Notes |
|---|---|---|
| Schema divergence (bit vs int, no F_ prefix) | JNPF Extension (or Foundry Profile Extension) | Track A agrees; this is HG#5, needs Decision Brief |
| Triple-Key (tenant_id, project_id, pipeline_id) | Triple-Key Iron Law (R12) — Master Spec | Track A agrees; already defined |
| Pattern Tags / is_pattern_source / llm_confidence / human_confirmed | JNPF Extension (SA-specific) | Track A agrees; operational semantics need definition |
| Temporal columns (valid_from, valid_to, is_current) | JNPF Extension (SA-specific SCD pattern) | Track A agrees; code verification needed |
| HG#4 trigger (5 incoming FKs) | **Master Spec Evolution** | Cross-module SA dependency needs spec definition |
| HG#5 trigger (schema divergence) | **Master Spec Evolution** | Pattern divergence requires spec decision |
| Index naming inconsistency (IX_ vs idx_) | JNPF Extension | NEW — Track A didn't flag |
| SCD Type 2 verification gap | Skill Evolution (Level B) | Pattern detection without verification |

---

## 14. Universal Core Purity

✅ Zero contamination in the OUTPUT.

However, the analysis raises Master Spec Evolution concerns:
- The "SA output table pattern" is not documented in Master Spec
- Triple-Key Iron Law compliance assumes a specific projection model
- HG#5 on SA tables vs JNPF tables is unaddressed in Master Spec

These are **Master Spec gaps**, not Universal Core contamination.

---

## 15. Adversarial Attack Log

| # | Attack Target | Severity | Outcome |
|---|---|---|---|
| 1 | "Schema correct for SA purpose" circular | Medium | LANDED — SA not defined |
| 2 | BIGINT IDENTITY distributed-unfriendly | Low | LANDED — Track A missed implication |
| 3 | bit NULL handling unaddressed | Low | LANDED |
| 4 | asset_level undefined | Medium | LANDED — what is it? |
| 5 | ON DELETE behavior for 5 FKs unaddressed | High | LANDED — critical for projection table |
| 6 | Triple-Key via index = unique? | High | LANDED — index != constraint |
| 7 | Nullable FK semantics | Low | LANDED |
| 8 | 8 indexes over-indexed for 35 rows? | Low | LANDED — write cost not analyzed |
| 9 | Mixed index naming (IX_/idx_) | Low | LANDED |
| 10 | Missing indexes (validation_status, created_at) | Medium | LANDED — Track A said "no recommendations" |
| 11 | SCD Type 2 code verification | High | LANDED — Track A admits [INFERRED] |
| 12 | version increment policy undefined | Medium | LANDED |
| 13 | is_deleted + SCD2 dual-pattern | Medium | LANDED — unusual combo |
| 14 | WRITE patterns not addressed | Medium | LANDED |
| 15 | pattern_tags format undefined | Medium | LANDED |
| 16 | LLM Confidence operational semantics | Medium | LANDED |
| 17 | human_confirmed workflow | Medium | LANDED |
| 18 | Shared projection write contention | High | LANDED — 5 dependent tables |
| 19 | HG#4 should be TRIGGERED | Critical | LANDED — textbook HG#4 |
| 20 | HG#5 should be TRIGGERED | Critical | LANDED — schema divergence IS HG#5 |
| 21 | Foundry Profile extension vs migration uncommitted | Medium | LANDED |
| 22 | DEFERRED without deadline = deferred-forever | High | LANDED — closure is weak |

**Attack Success Rate**: 22/22 = 100% landed.

**Net Assessment**: Track A correctly identified R3+ and DEFERRED. Track A was MOST thoughtful on this table. But the thoughtful analysis was undermined by **HG borderline dodging** — flagging HG#4 and HG#5 as borderline instead of triggered. This is a sophisticated form of risk under-statement.

**The HG#5 borderline dodge pattern is the most important finding across all 5 tables.** Track A uses "borderline" to acknowledge concern without triggering the gate. This pattern needs to be corrected in the Skill's HG evaluation logic.

---

## 16. Reviewer Notes

This is the table where Track A showed the most analytical depth. The schema divergence analysis, the projection-vs-aggregate distinction, and the Foundry Profile mismatch identification are all genuine insights.

But the most sophisticated analysis is also where Track A's most significant under-triggering occurred. Calling HG#4 and HG#5 "borderline" instead of "triggered" is the HG borderline dodge — Track A acknowledged the concerns but avoided the consequences.

The DEFERRED closure is appropriate but operationally weak. A Decision Brief with deliverables and deadlines is required, not just "DEFERRED with reason".

**For Skill Evolution**: The HG evaluation logic should not allow "borderline" as a final state. If a concern warrants borderline flag, it should be promoted to:
- Triggered (with documentation), OR
- NOT triggered (with explicit dismissal reasoning)

"Borderline forever" is a risk under-statement pattern.

---

## 17. Submission Confirmation

```
[ ] I confirm I am acting as ADVERSARIAL reviewer (Track A fully read)
[ ] I confirm my attacks cite specific Track A text
[ ] I confirm my Risk / HG / Closure judgments are based on Track A + independent verification
[ ] I confirm I have NOT modified AI Track A document
[ ] I confirm I produced attacks even where Track A was directionally correct

Reviewer: AI Engineer (Adversarial)
Date: 2026-08-30
```
