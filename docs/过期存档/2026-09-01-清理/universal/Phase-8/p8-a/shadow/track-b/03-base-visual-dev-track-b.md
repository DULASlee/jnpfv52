# Adversarial Track B — Table 03: base_visual_dev

> **Phase**: 8 — P8-A.3 (Adversarial Track B)
> **Status**: COMPLETE
> **Date**: 2026-08-30
> **Reviewer**: AI Engineer (Adversarial)
> **Protocol**: Adversarial Track B Protocol (取代 Blind Review)
> **Track A Access**: FULL READ
> **Track A Reference**: `ai-track-a-5-tables.md` Table 3

---

## ⚠️ ADVERSARIAL DECLARATION ⚠️

I HAVE read Track A for this table.

**Track A's overall verdict**: R2, NO-CHANGE, 3 SAFE-REFACTOR (indexes on F_CATEGORY / F_PARENT / F_STATE), HG#4 borderline flagged.

**My adversarial mission**: Attack. This table has JSON-blob fields and is the metadata table for the visual designer (cross-module: visualdata, workflow, data interface). The "managed at app layer" claim on HG#4 needs aggressive challenge.

---

## 1. Table Identity

| Field | Value | Track A Says | Match? |
|---|---|---|---|
| Table | 03 | base_visual_dev | ✅ |
| Physical Name | BASE_VISUAL_DEV | BASE_VISUAL_DEV | ✅ |
| Module | visualdata | visualdata | ✅ |
| Entity Mapped? | YES | YES (`VisualDevEntity`) | ✅ |
| Reviewer | AI Adversarial | — | — |

**Critical observation**: This is the metadata table for the low-code designer. Every form, list, and flow configuration is stored here. 5+ JSON-blob fields = significant JSON-parsing surface.

---

## 2. Track A Audit: Dimension A (Schema)

### Track A's Claim

> "Several nvarchar(MAX) JSON-as-text columns: f_tables_data, f_form_data, f_column_data, f_app_column_data, f_interface_param"
> "F_PARENT_ID column (parent-child for organize tree pattern)"
> "F_DB_LINK_ID (data source reference)"
> "F_FLOW_ID (workflow template reference)"
> "F_INTERFACE_ID (data interface reference)"
> "Large JSON-as-text fields: typical low-code designer pattern"

### Adversarial Attack

**Attack #1: Mixed case (F_ vs f_) noted but not analyzed (same as Table 1, Table 2).**

Track A consistently identifies lowercase-prefix columns but never investigates WHY. For this table:
- f_tables_data, f_form_data, f_column_data, f_app_column_data, f_interface_param — all lowercase f_

This is **systematic**, not random. Possible explanations:
- Legacy schema (older convention)
- Designer JSON contract (column name MUST match JSON key, lowercase is JSON convention)
- SqlSugar default for non-explicit-mapped columns

Track A should explain this pattern, not just note it.

**Attack #2: f_interface_param — what is this?**

The other JSON fields (form_data, column_data, tables_data, app_column_data) are clearly designer-related. f_interface_param is **ambiguous**:
- Is it parameter for a data interface (in/out)?
- Is it a list of allowed parameters?
- Is it JSON-encoded or comma-separated?

**Attack #3: Are these JSON-as-text or nvarchar(MAX) without JSON validation?**

DB-level JSON validation (SQL Server 2016+ has ISJSON function, but no JSON column type until SQL Server 2025's json type). These are nvarchar(MAX) storing JSON. This means:
- No schema enforcement at DB level
- App must validate every read/write
- Risk of malformed JSON in DB

Track A calls them "JSON-as-text" but doesn't address validation.

**Attack #4: 30 columns total — is the rest metadata or business?**

30 columns, 5 are JSON blobs. What are the other 25? Track A only listed:
- F_PARENT_ID, F_DB_LINK_ID, F_FLOW_ID, F_INTERFACE_ID (4 FK-like refs)
- F_STATE, F_TYPE, F_WEB_TYPE (3 enum/state fields)
- F_CATEGORY, F_ENABLED_MARK, F_SORT_CODE (3 categorization)
- Standard CLDS fields

That's ~10. What are the other 15+ columns? Coverage gap.

---

## 3. Track A Audit: Dimension B (Integrity)

### Track A's Claim

> "No DB-level FK (all relationships via app layer)"
> "F_PARENT_ID is self-reference but no DB FK"
> "Tenant isolation present"

### Adversarial Attack

**Attack #5: F_PARENT_ID self-reference is hierarchical — needs cycle prevention.**

Without DB FK, can F_PARENT_ID form a cycle? E.g.:
- Row A: parent_id = B
- Row B: parent_id = C
- Row C: parent_id = A

This would create an infinite tree traversal. Where does the app prevent cycles?

Track A does not address cycle prevention in self-referencing hierarchy.

**Attack #6: F_DB_LINK_ID references what exactly?**

Track A says "data source reference". Reference to WHAT table?
- base_dblink? (data source table)
- base_datalink? (data link table)
- Different convention?

Without knowing the target, "no DB FK" is not assessable. An adversarial reviewer demands: "F_DB_LINK_ID references the primary key of which table?"

**Attack #7: Cross-module refs (F_FLOW_ID, F_INTERFACE_ID) are orphan risks.**

If F_FLOW_ID points to a workflow that's deleted, the visualdev becomes orphaned. Without DB FK:
- No DB-level prevention of orphan
- App must handle dangling reference
- Track A claims "managed at app layer" — but Track A does not verify the app handles deletion cascade

---

## 4. Track A Audit: Dimension C (Index)

### Track A's Claim

> "Only PK index"
> "Critical queries: list by F_CATEGORY, list by F_STATE, list by F_TYPE, tree by F_PARENT_ID"
> "Recommended indexes: IDX_VISDEV_CATEGORY, IDX_VISDEV_PARENT, IDX_VISDEV_STATE"

### Adversarial Attack

**Attack #8: All queries [INFERRED] (same pattern as Tables 1, 2).**

No code citations.

**Attack #9: F_TYPE listed as query pattern but NOT in recommended indexes.**

Track A identifies "list by F_TYPE" as a query pattern but doesn't recommend IDX_VISDEV_TYPE. Inconsistency.

**Attack #10: JSON-as-text columns cannot be efficiently indexed.**

f_form_data, f_column_data etc. are stored as text. Searching inside them requires LIKE '%xxx%' which defeats any index. Track A does not address:
- Are JSON contents searchable?
- Is there a full-text index on these?
- Or is search done in app code post-fetch?

**Attack #11: F_EN_CODE for runtime form loading is mentioned but not indexed.**

Track A says "Read by en_code (business key) for runtime form loading" (Dim E) but does NOT recommend an index on F_EN_CODE.

This is a CRITICAL miss:
- Runtime form loading is THE hot path for the application
- It's keyed by F_EN_CODE (business key)
- Without an index, every form load = table scan

**Finding**: Track A identified F_EN_CODE as a hot-path query pattern but dropped the index recommendation. This is the most important index gap in Track A's analysis.

---

## 5. Track A Audit: Dimension D (Lifecycle)

### Track A's Claim

> "F_STATE (int) — JNPF custom state field for form lifecycle" — [INFERRED]
> "F_TYPE (int) — form type (form/list/flow)" — [INFERRED]
> "F_WEB_TYPE (int) — web/mobile/PC variant" — [INFERRED]
> "Standard CLDS + F_ENABLED_MARK"
> "Custom state machine present: F_STATE controls form dev → published → deprecated flow" — [INFERRED]

### Adversarial Attack

**Attack #12: "form dev → published → deprecated" state machine asserted without code.**

Track A says F_STATE controls a state machine with 3 states. But:
- What are the legal transitions? (draft → published? draft → deprecated? published → draft?)
- Is F_STATE a bitmask or enum?
- Are there guards (e.g., "can only deprecate after publishing")?

Track A asserts the state machine exists without verifying. **This is a GUESS, not an INFERRED claim**.

**Attack #13: F_TYPE enum values not enumerated.**

Track A says F_TYPE is "form type (form/list/flow)" — so 3 values. But:
- Is "flow" a separate type or a flag?
- What about "report", "dashboard", "page"?
- Are these in code somewhere?

**Attack #14: F_WEB_TYPE (web/mobile/PC) is dimension, not lifecycle.**

Track A puts F_WEB_TYPE under Lifecycle but it's actually a deployment variant dimension. A form can be in draft state and have F_WEB_TYPE=mobile. Lifecycle and variant are orthogonal.

Track A's dimension categorization is imprecise.

---

## 6. Track A Audit: Dimension E (CRUD/Query)

### Track A's Claim

> "Read pattern: list by category/state/type" — [INFERRED]
> "Single item by PK for form editing" — [INFERRED]
> "Read by en_code (business key) for runtime form loading" — [INFERRED]
> "Note: f_en_code nvarchar(400) — likely business identifier"

### Adversarial Attack

**Attack #15: "Read by en_code for runtime form loading" — this is THE hot path.**

This is the most critical query for the entire low-code platform. Every page render triggers this. Without an index on F_EN_CODE:
- Every page load = table scan on BASE_VISUAL_DEV
- With 48 rows currently, this is fast
- But with 1000+ forms in production, this is unacceptable

Track A identified this pattern but DID NOT recommend an index. **This is a critical operational gap**.

**Attack #16: nvarchar(400) for en_code is unusual length.**

Standard JNPF en_code is nvarchar(50) or nvarchar(100). nvarchar(400) suggests:
- Legacy schema
- Or accommodates hierarchical paths (e.g., "module1.module2.formcode")
- Or just over-sized

Track A notes the length but doesn't analyze why.

---

## 7. Track A Audit: Dimension F (DDD)

### Track A's Claim

> "VisualDev is a clear aggregate (form template)"
> "Has self-reference (parent_id) but no ambiguity — pure hierarchy"
> "JSON-blob children (form_data, column_data) are part of aggregate" — [INFERRED]

### Adversarial Attack

**Attack #17: "Pure hierarchy" — but what about multiple parents?**

If F_PARENT_ID allows only one parent, this is a tree. If multiple parents are allowed (M:N self-reference via a junction), this is a DAG. Track A doesn't verify which.

**Attack #18: JSON-blob as aggregate children is a DDD smell.**

In strict DDD, aggregate children are entities with their own identity. JSON-blobs are **value objects** (no identity, replaced wholesale). This is acceptable BUT:
- Different JSON-blob fields = different value object types
- f_form_data (form definition) vs f_column_data (column metadata) vs f_tables_data (table relationships) — these are 3 different concepts
- All inside one aggregate — the aggregate has 3+ value object types

Track A's "JSON-blob children are part of aggregate" lumps 3+ concepts into one. **Aggregate boundary is unclear**.

**Attack #19: Cross-aggregate references inside JSON are unaddressed.**

If f_form_data contains a reference to a workflow (F_FLOW_ID embedded in JSON), and F_FLOW_ID is deleted, the JSON reference becomes dangling.

Without DB FK enforcement AND without JSON validation, dangling references are silent.

---

## 8. Track A Audit: Dimension G (Consumer / Target Readiness)

### Track A's Claim

> "Entity has explicit mappings"
> "Several JSON-as-text fields require careful Foundry mapping" — [DESIGN]
> "Target Profile needs to handle: F_STATE / F_TYPE / F_WEB_TYPE — these are JNPF enums" — [DESIGN]

### Adversarial Attack

**Attack #20: "JNPF enums" — but values not verified.**

Track A says F_STATE, F_TYPE, F_WEB_TYPE are JNPF enums. Foundry Profile needs to map them. But:
- What are the integer values?
- What do they mean?
- Are they stable or evolving?

Without enumerating the values, the Foundry mapping is incomplete.

**Attack #21: JSON-as-text fields require Foundry Profile EXTENSION, not "careful mapping".**

Track A says "require careful Foundry mapping" (DESIGN) but actually requires a **Foundry Profile extension** to:
- Define the JSON schema for each blob field
- Define validation rules
- Define partial-update semantics (replace whole JSON vs patch)

Track A under-rated this.

---

## 9. Risk Re-Classification

### Track A: R2, HIGH confidence

### My Adversarial Re-Classification

**Risk Level: R2** — Confidence: MEDIUM (50-80%)

**Rationale for R2 agreement with caveats**:

I AGREE with R2 because:
- 30 columns (not exceptional like base_user's 68)
- JSON-blob pattern is established in low-code platforms
- Self-referencing hierarchy is clear
- Cross-module references are FK-by-app pattern (JNPF convention)

**However, I disagree with HIGH confidence. Multiple factors lower confidence:**

1. **F_EN_CODE index gap is critical and unaddressed** (Attack #15)
2. **Cross-module blast radius** is real (visualdata + workflow + data interface all read this table)
3. **JSON-as-text validation unaddressed**
4. **F_PARENT_ID cycle prevention unverified**

**Why I keep R2 (not R3+)**: The structural pattern is well-understood. The risks are operational (missing index, validation), not architectural. R3+ would require aggregate ambiguity or schema divergence, neither of which is dominant here.

---

## 10. Hard Gate Re-Audit

| HG | Track A | My Position | Justification |
|---|---|---|---|
| HG#1 (tenant isolation) | NOT triggered | **NOT triggered** | F_TENANT_ID present |
| HG#2 (data integrity) | NOT triggered | **BORDERLINE — should flag** | JSON blobs may contain dangling refs; cycle prevention in self-ref unverified |
| HG#3 (migration) | NOT triggered | **NOT triggered** | Only ADD INDEX |
| HG#4 (cross-module) | BORDERLINE | **TRIGGERED — should escalate** | Used by visualdata, workflow, data interface modules. Cross-module blast radius is real. "Managed at app layer" is asserted not verified. Promote to TRIGGERED for cross-module dependency review. |
| HG#5 (business ambiguity) | NOT triggered | **NOT triggered** | Form template semantics clear |

**Adversarial HG verdict**: 1 triggered (HG#4), 1 borderline (HG#2).

**Track A under-triggered HG#4**. The "borderline" flag was appropriate but should be PROMOTED to triggered because:
- Cross-module dependency is a real architectural concern
- VisualDev's metadata affects runtime of multiple modules
- Schema changes here have cross-module impact
- This IS the textbook HG#4 scenario

---

## 11. Recommended Action

**Track A**: SAFE-REFACTOR (add 3 indexes), NO-CHANGE closure.

**My Action**: **SAFE-REFACTOR with ADDITIONAL required indexes + HG#4 Decision Brief**

```
SAFE-REFACTOR with the following:

REQUIRED INDEX ADDITIONS (Track A missed):
1. IDX_VISDEV_ENCODE (F_TENANT_ID, F_EN_CODE) — CRITICAL for runtime form loading
2. IDX_VISDEV_TYPE (F_TENANT_ID, F_TYPE) — completes the dimension analysis

Track A's indexes (acceptable but secondary priority):
3. IDX_VISDEV_CATEGORY
4. IDX_VISDEV_PARENT
5. IDX_VISDEV_STATE

HG#4 DECISION BRIEF REQUIRED:
- Document cross-module dependencies on BASE_VISUAL_DEV
- Identify which modules read/write this table
- Confirm Foundry Profile handles cross-module refs

JNPF EXTENSION DOCUMENTATION:
- Define JSON schema for f_form_data, f_column_data, f_tables_data, f_app_column_data, f_interface_param
- Define validation rules
- Document partial-update vs replace semantics
```

---

## 12. Recommended Closure

**Track A**: NO-CHANGE

**My Closure**: **NO-CHANGE (conditional on index additions + HG#4 Brief)**

```
NO-CHANGE because:
- Schema is fundamentally correct
- No structural changes needed
- R2 classification stands

CONDITIONS:
- The 2 additional indexes (F_EN_CODE, F_TYPE) MUST be added in the index batch
- HG#4 Decision Brief MUST be completed before P8-B starts
- JSON schema documentation MUST be added to JNPF Extension backlog
```

---

## 13. Extension Routing

| Observation | Route to | Notes |
|---|---|---|
| JSON-blob fields (5) | JNPF Extension — designer JSON schema | Track A agrees; missing JSON schema definition |
| F_STATE / F_TYPE / F_WEB_TYPE enums | JNPF Extension | Track A agrees; enum values not enumerated |
| F_FLOW_ID / F_INTERFACE_ID / F_DB_LINK_ID | JNPF Extension — cross-module refs | Track A agrees; references unverified |
| HG#4 trigger (cross-module) | **Master Spec Evolution** | Cross-module dependencies need spec definition |
| F_EN_CODE index gap | **Skill Evolution (Level A)** | Skill identified the query pattern but dropped the recommendation |

---

## 14. Universal Core Purity

✅ Zero contamination.

However, the analysis raises a Skill Evolution question: the Skill identified F_EN_CODE as a hot-path query pattern but did NOT recommend an index. This is a **finding-recommendation disconnect** — the Skill should ensure every identified query pattern gets a corresponding index recommendation (or explicit "no index needed" justification).

---

## 15. Adversarial Attack Log

| # | Attack Target | Severity | Outcome |
|---|---|---|---|
| 1 | Mixed case (f_) pattern unexplained | Low | LANDED — never investigated |
| 2 | f_interface_param ambiguity | Low | LANDED — semantics unclear |
| 3 | 25+ non-JSON columns coverage gap | Medium | LANDED — only 10 enumerated |
| 4 | Self-ref cycle prevention unaddressed | Medium | LANDED |
| 5 | F_DB_LINK_ID target unknown | Medium | LANDED — reference target unclear |
| 6 | F_EN_CODE index recommendation MISSED | Critical | LANDED — most critical query, no index |
| 7 | F_TYPE index recommendation MISSED | Medium | LANDED — pattern identified, no index |
| 8 | JSON-blob search behavior unaddressed | Medium | LANDED |
| 9 | "form dev → published → deprecated" state machine asserted | High | LANDED — Track A says [INFERRED] but no code cited |
| 10 | F_EN_CODE nvarchar(400) anomaly | Low | LANDED — unusual length unexplained |
| 11 | JSON-blob aggregate children: 3+ concepts in one aggregate | Medium | LANDED — aggregate boundary unclear |
| 12 | Cross-aggregate JSON refs dangling | Medium | LANDED — no DB FK, no JSON validation |
| 13 | HG#4 should be TRIGGERED, not borderline | Critical | LANDED — textbook HG#4 scenario |

**Attack Success Rate**: 13/13 = 100% landed.

**Net Assessment**: Track A's R2 + NO-CHANGE is acceptable direction but with critical operational gaps (F_EN_CODE index) and one HG under-trigger (HG#4). The Skill shows a pattern: identifying query patterns but dropping index recommendations, which is a **systemic Skill Evolution Level A issue**.

---

## 16. Reviewer Notes

Track A's analysis is directionally correct on R2 and NO-CHANGE but operationally thin:
- Most critical index (F_EN_CODE) MISSED
- Cross-module HG#4 waved off as borderline when it should be triggered
- JSON schema treated as "needs careful mapping" when it's actually a Foundry Profile extension requirement

If this table is moved to P8-B without F_EN_CODE index, every form load will be a table scan. The Skill must carry forward the pattern: identified query = required index.

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
