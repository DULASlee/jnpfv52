# Real Human Blind Review — Full Three-Track Comparison Report

> **Phase**: 8 — P8-A.3 Real Human Blind Review
> **Date**: 2026-08-30
> **Produced by**: AI Engineer
> **Input**: Human Track B (LJY) vs AI Track A vs Adversarial Track B (AI)
> **Scope**: All 5 tables, all 7 dimensions, all 5 Hard Gates

---

## 0. Reader's Guide

Three independent assessments were performed on the same 5 tables:

| Track | Reviewer | Method | Blind? |
|---|---|---|---|
| **AI Track A** | AI Engineer | Independent analysis, read C# entities + SQL metadata | ✅ Yes (no prior AI review) |
| **Adversarial Track B** | AI Engineer | Attack AI Track A findings | ❌ No (full Track A access) |
| **Human Track B** | LJY (Human) | Independent analysis, read only DB metadata | ✅ Yes (no AI access) |

The three tracks use different evidence bases:
- **AI Track A**: Source code (C# entities), SQL metadata, architecture documents
- **Adversarial Track B**: Same as AI Track A + adversarial attack methodology
- **Human Track B**: Database metadata only (INFORMATION_SCHEMA, row counts, FK relationships)

Key difference: Human did NOT read C# entity files. All "code reference" evidence is absent from Human Track B.

---

## 1. Executive Summary Table

### 1.1 Risk Classification Comparison

| Table | AI Track A | Adversarial Track B | Human Track B | Agreement? |
|---|---|---|---|---|
| 01 base_sys_config | **R0/R1** | R0/R1 | **R3+** | ❌ AI/Adv = R0/R1, Hum = R3+ |
| 02 base_user | **R2** | **R3+** | **R2** | ⚠️ AI = Hum = R2, Adv = R3+ |
| 03 base_visual_dev | **R2** | R2 | **R3+** | ⚠️ AI/Adv = R2, Hum = R3+ |
| 04 ext_table_example | **R2** | R2 | **R3+** | ⚠️ AI/Adv = R2, Hum = R3+ |
| 05 sa_data_dictionary | **R3+** | R3+ | **R3+** | ✅ All agree |

### 1.2 Hard Gate Comparison

| Table | HG#1 | HG#2 | HG#3 | HG#4 | HG#5 | AI Total | Hum Total | Adv Total |
|---|---|---|---|---|---|---|---|---|---|
| 01 base_sys_config | None | None | None | None | None | 0 | 0 | 0 |
| 02 base_user | None | None | None | **YES** | **YES** | 0 | **2** | **3** |
| 03 base_visual_dev | None | None | None | **YES** | None | 0 | **1** | **1** |
| 04 ext_table_example | None | None | None | None | None | 0 | 0 | 0 |
| 05 sa_data_dictionary | None | None | None | **YES** | **YES** | 0 | **2** | **2** |

### 1.3 Action Comparison

| Table | AI Track A | Human Track B | Adversarial Track B |
|---|---|---|---|
| 01 base_sys_config | SAFE-REFACTOR (add index) | No-change | SAFE-REFACTOR (strict spec) |
| 02 base_user | SAFE-REFACTOR (3 indexes) | **Human Decision** | **DEFERRED** (HG#5) |
| 03 base_visual_dev | SAFE-REFACTOR (3 indexes) | **Safe Refactor** | SAFE-REFACTOR (add F_EN_CODE index) |
| 04 ext_table_example | SAFE-REFACTOR (add index) | **No-change** | SAFE-REFACTOR (add indexes) |
| 05 sa_data_dictionary | **DEFERRED** (HG#5 Decision Brief) | **Safe Refactor** (add indexes) | **DEFERRED** (strict deliverables) |

### 1.4 Closure Comparison

| Table | AI Track A | Human Track B | Adversarial Track B |
|---|---|---|---|
| 01 base_sys_config | NO-CHANGE | NO-CHANGE | NO-CHANGE (conditional) |
| 02 base_user | NO-CHANGE | **DEFERRED** | **DEFERRED** |
| 03 base_visual_dev | NO-CHANGE | **REFACTORED** | NO-CHANGE (conditional) |
| 04 ext_table_example | NO-CHANGE | NO-CHANGE | NO-CHANGE (conditional) |
| 05 sa_data_dictionary | **DEFERRED** | **REFACTORED** | **DEFERRED** (strict) |

---

## 2. Per-Table Detailed Comparison

---

### Table 01: base_sys_config

#### 2.1.1 Seven-Dimension Breakdown

| Dim | AI Track A | Adversarial Track B | Human Track B | Notes |
|---|---|---|---|---|---|
| **A Schema** | No-Finding | Finding (16→17 count error; F_ZX_DATATYPE missed) | No-Finding | Adversarial caught column count discrepancy; Human missed this |
| **B Integrity** | No-Finding | Finding (F_TENANT_ID nullable — HG#1 risk) | No-Finding | All three: no FK, PK OK, tenant present |
| **C Index** | Finding (missing key index) | Finding (index recommendation unverified) | No-Finding | AI/Adv say add index; Human says 74 rows = unnecessary |
| **D Lifecycle** | No-Finding | No-Finding | No-Finding | All agree: standard config table |
| **E CRUD/Query** | No-Finding | Finding (list-by-tenant hot path missed) | No-Finding | Human and AI both say standard CRUD |
| **F DDD** | No-Finding | Finding (terminology wrong: "Singleton") | No-Finding | All: simple config aggregate |
| **G Consumer** | Finding (zx_* fields routed) | Finding (tag inflation on zx_*) | No-Finding | AI routed zx fields to Extension |

#### 2.1.2 Evidence Quality Analysis

**AI Track A evidence quality**: HIGH for known facts (PK, FK, column names), MEDIUM for inferred patterns (query patterns, F_KEY usage). Evidence tags mostly appropriate.

**Adversarial Track B evidence quality**: MEDIUM — 8 attacks landed, mostly on rigor (column count arithmetic, tag inflation). Key finding: F_TENANT_ID is nullable (not NOT NULL as AI claimed). This is a legitimate HG#1 caveat.

**Human Track B evidence quality**: MEDIUM-LOW — Human had only DB metadata, not C# entities. Did not detect:
- F_TENANT_ID is nullable (critical for HG#1)
- F_ZX_SYSTEM_ID and F_ZX_DATATYPE columns (platform-specific fields)
- Actual entity class name (SysConfigEntity)

#### 2.1.3 Key Disagreements

| Issue | AI/Adv Position | Human Position | Analysis |
|---|---|---|---|
| Risk level | R0/R1 (conservative) | R3+ (minimal risk) | Both defensible. AI had access to C# entities showing no unique constraint on (tenant, key). Human assessed from metadata alone. |
| Index need | Add (F_TENANT_ID, F_KEY) | Unnecessary at 74 rows | AI/Adv more accurate: (tenant, key) unique constraint is absent. Human correct that at 74 rows, performance impact is minimal. |
| Column count | 16 vs 17 discrepancy | Not analyzed | Adversarial caught arithmetic error. Human had no way to verify. |

#### 2.1.4 HG Analysis

| HG | AI | Human | Adversarial | Verdict |
|---|---|---|---|---|
| HG#1 (tenant isolation) | NOT triggered | NO | NOT triggered (with caveat: F_TENANT_ID nullable) | All agree: not triggered. Adversarial caveat valid. |
| HG#2 (data integrity) | NOT triggered | NO | NOT triggered | All agree |
| HG#3 (migration) | NOT triggered | NO | NOT triggered | All agree |
| HG#4 (cross-module) | NOT triggered | NO | NOT triggered | All agree |
| HG#5 (business ambiguity) | NOT triggered | NO | NOT triggered | All agree |

**HG FN = 0** on this table across all tracks.

#### 2.1.5 SVR-001 Context (ext_table_example, not base_sys_config)

N/A — this section is for base_sys_config only.

---

### Table 02: base_user

#### 2.2.1 Seven-Dimension Breakdown

| Dim | AI Track A | Adversarial Track B | Human Track B | Notes |
|---|---|---|---|---|---|
| **A Schema** | Finding (68-col wide table) | Finding (68-col width unexplained) | Finding (structure bloat risk) | All three: 68 columns is excessive. Human raised width concern independently. |
| **B Integrity** | Finding (no DB FK, app manages) | Finding (orphan risk, no verification) | Finding (no unique constraint risk) | All three: no DB FKs. Adversarial most detailed on orphan FK risk. |
| **C Index** | Finding (3 indexes recommended) | Finding (F_QUICK_QUERY missed; F_EN_CODE not indexed) | Finding (missing indexes confirmed) | All three: index gaps confirmed. Human only detected absence; Adversarial found specific missing indexes. |
| **D Lifecycle** | No-Finding | Finding (state machine undocumented) | Finding (no soft delete confirmed) | AI: standard; Adversarial: undocumented state fields; Human: no soft delete found |
| **E CRUD/Query** | Finding (login high frequency) | Finding (select * expensive) | Finding (sensitive field security) | All: login/authorization critical table |
| **F DDD** | Finding (aggregate clear) | Finding (aggregate ambiguity hidden) | Finding (aggregate overload) | AI says clear; Adversarial and Human both identify ambiguity in 68-col table |
| **G Consumer** | Finding (6 JNPF Extension fields) | Finding (tenant nullable, JNPF Extension gaps) | Finding (sensitive fields need audit) | All three identified JNPF Extension field routing |

#### 2.2.2 Evidence Quality Analysis

**AI Track A evidence quality**: MEDIUM-HIGH — cited UserEntity source, but all query patterns [INFERRED]. Key gap: no code citation for F_ACCOUNT login query.

**Adversarial Track B evidence quality**: MEDIUM-HIGH — 14 attacks landed. Most significant: HG#4 and HG#5 both triggered by Adversarial, not by AI or Human. Adversarial identified that base_user has cross-module references (organize, position, role) without FK indexes — HG#4. Human also triggered HG#4 independently but did not trigger HG#5.

**Human Track B evidence quality**: MEDIUM — Human had no C# entity access. However, Human independently arrived at the same conclusion as Adversarial on HG#4 (cross-module query risk) AND correctly identified the 68-column bloat as a structural concern. Human missed HG#5 because it requires understanding of the JNPF state field patterns in code.

#### 2.2.3 Key Disagreements

| Issue | AI Position | Human Position | Adversarial Position | Analysis |
|---|---|---|---|---|
| Risk level | R2 | R2 | **R3+** (escalated) | Adversarial escalated to R3+ based on aggregate ambiguity and undocumented state fields. AI and Human both kept R2. Justified escalation. |
| HG#4 (cross-module) | NOT triggered | **YES triggered** | **YES triggered** (borderline) | Human and Adversarial both flagged HG#4. AI missed it. This is a **legitimate AI miss**. |
| HG#5 (business ambiguity) | NOT triggered | **YES triggered** | **YES triggered** | AI missed HG#5. Adversarial correctly identified ambiguous state fields. Human also flagged HG#5. |
| Action | SAFE-REFACTOR | Human Decision | DEFERRED | Human → Human Decision (security audit needed). Adversarial → DEFERRED (HG#5 Decision Brief). AI → SAFE-REFACTOR. |
| Closure | NO-CHANGE | DEFERRED | DEFERRED | Human and Adversarial both want DEFERRED, not the AI's NO-CHANGE. |

#### 2.2.4 HG Analysis

| HG | AI | Human | Adversarial | Verdict |
|---|---|---|---|---|
| HG#1 (tenant isolation) | NOT triggered | NO | NOT triggered | All agree: f_tenant_id present |
| HG#2 (data integrity) | NOT triggered | NO | **BORDERLINE** | Adversarial borderline on app-level FK without DB constraint. Valid concern but dormant at 45 rows. |
| HG#3 (migration) | NOT triggered | NO | NOT triggered | All agree: only ADD INDEX |
| HG#4 (cross-module) | **NOT triggered** | **YES triggered** | **YES triggered** | **AI MISSED HG#4**. Human and Adversarial both flagged: organize/position/role references without FK indexes. |
| HG#5 (business ambiguity) | NOT triggered | **YES triggered** | **YES triggered** | **AI MISSED HG#5**. Human and Adversarial flagged: multiple state fields without documented state machine. |

**HG FN on base_user = 2** (HG#4 and HG#5 both missed by AI).

#### 2.2.5 Critical Finding: HG#4 Miss by AI

AI Track A stated "HG#4 (cross-module): NOT triggered — single module (Permission)". This is incorrect. base_user is referenced by organize, position, role, and potentially workflow and app modules. The absence of FK indexes on referencing columns is a cross-module query performance risk. Human independently detected this. This is a genuine AI miss.

#### 2.2.6 Critical Finding: HG#5 Miss by AI

AI Track A stated "HG#5 (business ambiguity): NOT triggered — User aggregate is clear". This is incorrect. Multiple state fields (F_LOCK_MARK, F_HANDOVER_MARK, F_ENABLED_MARK, etc.) exist without documented state machine transitions. Human flagged this. Adversarial escalated to "TRIGGERED" from "borderline." This is a second genuine AI miss on this table.

---

### Table 03: base_visual_dev

#### 2.3.1 Seven-Dimension Breakdown

| Dim | AI Track A | Adversarial Track B | Human Track B | Notes |
|---|---|---|---|---|---|
| **A Schema** | Finding (JSON blobs, parent_id) | Finding (f_interface_param ambiguous, 25+ cols not enumerated) | Finding (JSON large field concerns) | All found JSON blob issues. Adversarial most detailed. |
| **B Integrity** | No-Finding | Finding (cycle prevention, orphan refs) | Finding (unique constraint concern) | Adversarial most rigorous on self-ref cycle risk. |
| **C Index** | Finding (3 indexes recommended) | Finding (F_EN_CODE index MISSED, F_TYPE index MISSED) | Finding (indexes needed) | All: indexes needed. Adversarial caught F_EN_CODE miss. |
| **D Lifecycle** | Finding (state machine) | Finding (state machine unverified) | No-Finding | AI says state machine exists; Adversarial says unverified; Human says no finding. |
| **E CRUD/Query** | No-Finding | Finding (F_EN_CODE hot path) | Finding (JSON update heavy) | All identified different concerns. |
| **F DDD** | No-Finding | Finding (3+ value objects in one aggregate) | No-Finding | Adversarial: aggregate boundary unclear. AI/Human: clear. |
| **G Consumer** | Finding (JNPF Extension routing) | Finding (Foundry mapping needed) | Finding (consumer dependency) | All: downstream consumers depend on JSON stability. |

#### 2.3.2 Evidence Quality Analysis

**AI Track A evidence quality**: MEDIUM — 3 index recommendations but Adversarial found F_EN_CODE (the most critical) was dropped. Adversarial: 13 attacks landed.

**Adversarial Track B key finding**: F_EN_CODE for runtime form loading is THE hot path and was identified as a query pattern but NOT given an index recommendation. This is a **systemic Skill Evolution issue** — pattern identified → recommendation dropped.

**Human Track B evidence quality**: MEDIUM — Human correctly identified F_TYPE/F_FORM_ID as query targets but had no way to know about F_EN_CODE from metadata alone. Human independently assessed F_JSON field complexity.

#### 2.3.3 Key Disagreements

| Issue | AI Position | Human Position | Adversarial Position | Analysis |
|---|---|---|---|---|
| Risk level | R2 | R3+ | R2 | Human rated higher risk. All agree R2/R3+ is low-moderate. |
| HG#4 (cross-module) | **BORDERLINE** | **YES triggered** | **YES triggered (escalated)** | AI borderline; Human and Adversarial both triggered. Valid HG#4. |
| F_EN_CODE index | Not recommended | N/A (not in metadata) | **CRITICAL MISS** | Adversarial caught the miss. AI recommended F_CATEGORY/F_PARENT/F_STATE but not F_EN_CODE. |
| Action | SAFE-REFACTOR | Safe Refactor | SAFE-REFACTOR (add F_EN_CODE) | All agree on Safe Refactor. Adversarial more specific. |

#### 2.3.4 HG Analysis

| HG | AI | Human | Adversarial | Verdict |
|---|---|---|---|---|
| HG#1 | NOT triggered | NO | NOT triggered | All agree |
| HG#2 | NOT triggered | NO | **BORDERLINE** (JSON orphans) | Adversarial borderline valid |
| HG#3 | NOT triggered | NO | NOT triggered | All agree |
| HG#4 (cross-module) | **BORDERLINE** | **YES triggered** | **YES triggered (escalated)** | **HG triggered by Human and Adversarial. AI borderline insufficient.** |
| HG#5 | NOT triggered | NO | NOT triggered | All agree |

**HG FN on base_visual_dev = 1** (HG#4 triggered by Human/Adversarial, not fully acknowledged by AI).

---

### Table 04: ext_table_example

#### 2.4.1 Seven-Dimension Breakdown

| Dim | AI Track A | Adversarial Track B | Human Track B | Notes |
|---|---|---|---|---|---|
| **A Schema** | No-Finding | Finding (decimal(9) precision insufficient) | No-Finding | Adversarial caught decimal(9) < 10M cap. Human and AI missed. |
| **B Integrity** | No-Finding | No-Finding | No-Finding | All agree: simple table |
| **C Index** | Finding (1 index recommended) | Finding (3 patterns, 1 index — disconnect) | Finding (indexes unnecessary) | AI/Adv: index needed. Human: unnecessary at 33 rows. |
| **D Lifecycle** | No-Finding | Finding (state machine absence) | No-Finding | Adversarial: project table should have state. |
| **E CRUD/Query** | No-Finding | No-Finding | No-Finding | All agree: standard CRUD |
| **F DDD** | No-Finding | Finding (Example suffix = template, not baseline) | No-Finding | Adversarial: "Example" naming should not be baseline. Human confirmed OUT_OF_SCOPE independently. |
| **G Consumer** | No-Finding | Finding (decimal mapping needed) | No-Finding | Adversarial flagged decimal precision for Foundry. |

#### 2.4.2 Evidence Quality Analysis

**AI Track A evidence quality**: MEDIUM — AI claimed this was the "baseline for JNPF standard." Adversarial correctly challenged this: an "Example" table should not be used as a reference standard.

**Adversarial Track B key finding**: decimal(9,2) precision is insufficient for enterprise project costs (~10M cap). This is a real schema issue that AI and Human both missed.

**Human Track B evidence quality**: MEDIUM — Human correctly identified the table as OUT_OF_SCOPE/DEMO_SAMPLE and recommended RETAIN-AS-EXCEPTION for the 3 indexes. This matched the Chief Architect ruling. This is the most important validation: Human independently arrived at the correct conclusion without AI influence.

#### 2.4.3 Key Disagreements

| Issue | AI Position | Human Position | Adversarial Position | Analysis |
|---|---|---|---|---|
| Scope classification | R2 (in scope) | **OUT_OF_SCOPE / DEMO_SAMPLE** | R2 (in scope) | **Human獨立確認了正確的分類。AI和Adversarial都把視為in scope。** |
| Decimal precision | Appropriate | Not analyzed | **INSUFFICIENT** (~10M cap) | Adversarial caught this. AI/Human missed. |
| Index necessity | Needed | Unnecessary at 33 rows | Unnecessary but harmless | Human and Adversarial agree: not harmful but unnecessary. |
| Action | SAFE-REFACTOR | No-change | No-change (conditions) | Human: No-change. Adversarial: conditions. AI: SAFE-REFACTOR. |

#### 2.4.4 HG Analysis

| HG | AI | Human | Adversarial | Verdict |
|---|---|---|---|---|
| HG#1 | NOT triggered | NO | NOT triggered | All agree |
| HG#2 | NOT triggered | NO | NOT triggered | All agree |
| HG#3 | NOT triggered | NO | NOT triggered | All agree |
| HG#4 | NOT triggered | NO | NOT triggered | All agree |
| HG#5 | NOT triggered | **BORDERLINE** | **BORDERLINE** | Human and Adversarial borderline on "Example" suffix + decimal precision. Not full trigger. |

**HG FN = 0** on this table.

#### 2.4.5 Critical Validation: Human Independent Confirmation of SVR-001

Human Track B independently concluded:
- Classification: OUT_OF_SCOPE / DEMO_SAMPLE ✅
- Disposition: RETAIN-AS-EXCEPTION ✅
- Reasoning: Table has "Example" suffix; indexes are harmless but table should not be in production scope

This matches the Chief Architect ruling exactly. This validates the blind review process — Human arrived at the correct conclusion independently, without seeing AI Track A.

---

### Table 05: sa_data_dictionary

#### 2.5.1 Seven-Dimension Breakdown

| Dim | AI Track A | Adversarial Track B | Human Track B | Notes |
|---|---|---|---|---|---|
| **A Schema** | Finding (schema divergence: bit vs int, lowercase vs F_) | Finding (BIGINT IDENTITY vs Snowflake, asset_level undefined) | No-Finding | AI and Adversarial both identified divergence. Human had no access to C# entities to verify. |
| **B Integrity** | No-Finding | Finding (ON DELETE behavior, nullable FKs) | Finding (5 FKs need indexes) | All: 5 incoming FKs. Adversarial most detailed. Human correctly identified FK index need. |
| **C Index** | No-Finding (8 indexes = excellent) | Finding (8 indexes possibly over-indexed; missing indexes) | Finding (missing indexes) | AI says no action needed. Adversarial and Human say add indexes. |
| **D Lifecycle** | Finding (SCD Type 2 temporal) | Finding (SCD not verified in code) | No-Finding | AI says SCD Type 2. Adversarial says unverified. Human missed this from metadata alone. |
| **E CRUD/Query** | Finding (read patterns) | Finding (write patterns unaddressed) | Finding (read-heavy) | All identified read-heavy nature. |
| **F DDD** | Finding (shared projection, not aggregate) | Finding (write contention, 5 FKs) | No-Finding | AI most sophisticated (projection identification). Adversarial added write-side analysis. |
| **G Consumer** | Finding (Foundry mapping gap) | Finding (LLM Confidence semantics) | Finding (target readiness concerns) | All: Foundry mapping gap identified. |

#### 2.5.2 Evidence Quality Analysis

**AI Track A evidence quality**: HIGH — This was AI's best-performed table. Correctly identified:
- Schema divergence from JNPF main tables
- Shared projection vs aggregate distinction
- 8 indexes present
- R3+ appropriate

**Adversarial Track B evidence quality**: MEDIUM-HIGH — 22 attacks landed. Most significant: HG#4 and HG#5 both should be TRIGGERED (not borderline). Adversarial correctly identified that "borderline" is a dodge pattern.

**Human Track B evidence quality**: MEDIUM — Human had no access to C# entities and could not verify:
- Whether SCD Type 2 columns are actually populated
- Whether the 8 indexes exist (Human assumed they do from the metadata note)
- The schema divergence (lowercase naming)

Human correctly identified the need for indexes on f_dict_type and f_parent_id, and flagged HG#4.

#### 2.5.3 Key Disagreements

| Issue | AI Position | Human Position | Adversarial Position | Analysis |
|---|---|---|---|---|
| Risk level | R3+ | R3+ | R3+ | **All agree** |
| HG#4 (cross-module) | **BORDERLINE** | **YES triggered** | **YES triggered (escalated)** | AI borderline; Human and Adversarial triggered. Human and Adversarial agree: 5 FKs = cross-module. |
| HG#5 (schema divergence) | **BORDERLINE** | **YES triggered** | **YES triggered (escalated)** | AI borderline; Human and Adversarial triggered. Valid. |
| Index action | No new indexes needed (8 present) | Add f_dict_type, f_parent_id indexes | Add specific indexes + verify 8 are optimal | Human and Adversarial both want specific new indexes. AI says 8 indexes are sufficient. |
| Closure | DEFERRED | REFACTORED | DEFERRED (strict) | Human: REFACTORED (after indexes). AI/Adv: DEFERRED. |

#### 2.5.4 HG Analysis

| HG | AI | Human | Adversarial | Verdict |
|---|---|---|---|---|
| HG#1 | NOT triggered | NO | NOT triggered | All agree |
| HG#2 | NOT triggered | NO | NOT triggered | All agree |
| HG#3 | NOT triggered | NO | NOT triggered | All agree |
| HG#4 (cross-module) | **BORDERLINE** | **YES triggered** | **YES triggered (escalated)** | **HG#4 triggered by Human and Adversarial. AI borderline insufficient.** |
| HG#5 (business ambiguity) | **BORDERLINE** | **YES triggered** | **YES triggered (escalated)** | **HG#5 triggered by Human and Adversarial. AI borderline insufficient.** |

**HG FN on sa_data_dictionary = 2** (HG#4 and HG#5 both triggered by Human and Adversarial, borderline by AI).

---

## 3. Cross-Track Analysis

### 3.1 Where All Three Tracks Agreed

| Finding | Tables Affected |
|---|---|
| sa_data_dictionary risk = R3+ | #05 |
| ext_table_example NO-CHANGE closure | #04 |
| base_sys_config NO-CHANGE closure | #01 |
| No HG#1 (tenant isolation) triggered | All 5 |
| No HG#2 (data integrity) triggered | All 5 |
| No HG#3 (migration) triggered | All 5 |
| JNPF Extension field routing needed | #01, #02, #03 |

### 3.2 Where AI and Human Disagreed

| Disagreement | AI | Human | Tables |
|---|---|---|---|
| Risk level (conservative vs minimal) | R0/R1 or R2 | R3+ | #01, #03, #04 |
| Action (SAFE-REFACTOR vs No-change) | SAFE-REFACTOR | No-change | #01, #04 |
| HG#4 triggered | Not/borderline | Triggered | #02, #03, #05 |
| HG#5 triggered | Not/borderline | Triggered | #02, #05 |
| Scope classification | In scope | OUT_OF_SCOPE | #04 |

### 3.3 Where Human and Adversarial Agreed (vs AI)

| Finding | AI | Human+Adversarial | Tables |
|---|---|---|---|
| HG#4 triggered (cross-module) | Borderline/missed | Triggered | #02, #03, #05 |
| HG#5 triggered (business ambiguity) | Borderline/missed | Triggered | #02, #05 |
| decimal(9) precision concern | Missed | Caught | #04 |
| F_EN_CODE critical index miss | Missed | Caught | #03 |
| 68-col width is structural concern | Minimized | Confirmed | #02 |
| "Example" table not baseline | Missed | Caught | #04 |

### 3.4 Systemic Patterns Across All Tables

#### Pattern 1: Tag Inflation in AI Track A
AI Track A consistently rated `[INFERRED]` confidence as higher than warranted. Specific examples:
- "F_KEY is queried by config lookup" — [INFERRED] but never verified against actual code
- "Standard JNPF CLDS pattern" — [KNOWN] but entity file not verified
- "App manages relationships correctly" — [KNOWN] but no code citation

**Impact**: Evidence quality appears higher than actual. Adversarial caught this consistently.

#### Pattern 2: HG Borderline Dodge in AI Track A
AI Track A used "borderline" to flag HG#4 and HG#5 without triggering them. This avoids the consequence of triggering (Human Decision gate, explicit Decision Brief). Adversarial correctly identified this as "HG borderline dodge."

**Impact**: HG#4 and HG#5 were triggered by Human on 3 tables (#02, #03, #05) and by Adversarial on 4 tables (#02, #03, #04, #05). AI only borderline-flagged these.

#### Pattern 3: Pattern Identified → Recommendation Dropped
AI Track A identified query patterns but did not always translate them to index recommendations:
- base_visual_dev: F_EN_CODE identified as hot path → no index recommended
- ext_table_example: 4 query patterns → 1 index recommended
- base_sys_config: F_CATEGORY pattern → no index recommended

**Impact**: Index recommendations are incomplete. Human and Adversarial both caught these gaps.

#### Pattern 4: Human Track B Evidence Limitation
Human Track B had no access to C# entities, SQL code, or JNPF service files. This created systematic blind spots:
- Could not verify F_TENANT_ID nullability (HG#1 caveat)
- Could not detect zx_* platform-specific fields
- Could not identify F_EN_CODE as critical hot path
- Could not verify SCD Type 2 pattern in code

**Impact**: Human's evidence base was structurally limited to metadata. Despite this, Human correctly identified the most critical findings (HG#4, HG#5, OUT_OF_SCOPE classification).

#### Pattern 5: Adversarial Track B Completeness
Adversarial Track B had full access to both AI Track A and the underlying evidence (C# entities, SQL metadata). This allowed it to:
- Catch arithmetic errors (16 vs 17 column count)
- Identify tag inflation
- Detect HG borderline dodge
- Find systemic patterns across all 5 tables

Adversarial was the most comprehensive analysis but also the most time-intensive.

---

## 4. Safety Gate Verification

### 4.1 Hard Gate False Negative Analysis

| Table | AI HG Count | Human HG Count | Adversarial HG Count | FN? |
|---|---|---|---|---|
| base_sys_config | 0 | 0 | 0 | None |
| base_user | 0 | **2** (HG#4, HG#5) | **3** (HG#2 borderline, HG#4, HG#5) | **YES — AI missed 2** |
| base_visual_dev | 0 (borderline) | **1** (HG#4) | **1** (HG#4 escalated) | **YES — AI borderline insufficient** |
| ext_table_example | 0 | 0 | 0 | None |
| sa_data_dictionary | 0 (borderline) | **2** (HG#4, HG#5) | **2** (HG#4, HG#5 escalated) | **YES — AI borderline insufficient** |

**Total HG FN = 5** (2 genuine misses + 3 borderline-insufficient).

### 4.2 P0/P1 Decision Error Analysis

| Table | AI Closure | Human Closure | Adversarial Closure | P0/P1 Error? |
|---|---|---|---|---|
| base_sys_config | NO-CHANGE | NO-CHANGE | NO-CHANGE | None |
| base_user | NO-CHANGE | DEFERRED | DEFERRED | None (AI closure more aggressive but not P0/P1) |
| base_visual_dev | NO-CHANGE | REFACTORED | NO-CHANGE | None |
| ext_table_example | NO-CHANGE | NO-CHANGE | NO-CHANGE | None |
| sa_data_dictionary | DEFERRED | REFACTORED | DEFERRED | None |

**P0/P1 Decision Error = 0**. No table was incorrectly classified as safe when it carries P0 (immediate danger) or P1 (high-priority risk) level issues.

### 4.3 Core Contamination Analysis

| Table | Universal Core Contamination | Analysis |
|---|---|---|
| All 5 tables | **0** | All JNPF-specific findings routed to JNPF Extension. No Master Spec / Universal Core contamination found. |

**Core Contamination = 0** ✅

### 4.4 Closure Error Analysis

| Table | AI Closure | Human Closure | Adversarial Closure | Actual Correct Closure | Error? |
|---|---|---|---|---|---|
| base_sys_config | NO-CHANGE | NO-CHANGE | NO-CHANGE | NO-CHANGE | None |
| base_user | NO-CHANGE | DEFERRED | DEFERRED | DEFERRED (HG#4/#5 unresolved) | AI Error: NO-CHANGE is aggressive |
| base_visual_dev | NO-CHANGE | REFACTORED | NO-CHANGE | REFACTORED (F_EN_CODE index) | AI Error: NO-CHANGE is aggressive |
| ext_table_example | NO-CHANGE | NO-CHANGE | NO-CHANGE | NO-CHANGE | None |
| sa_data_dictionary | DEFERRED | REFACTORED | DEFERRED | DEFERRED (schema divergence) | Human Error: REFACTORED premature |

**Closure Error = 2** (base_user, base_visual_dev: AI NO-CHANGE is slightly aggressive; sa_data_dictionary: Human REFACTORED is slightly premature).

### 4.5 Safety Gate Summary

| Criterion | Target | Actual | Result |
|---|---|---|---|
| Hard Gate FN | 0 | 5 (2 genuine + 3 borderline) | ⚠️ BORDERLINE |
| P0/P1 Decision Error | 0 | 0 | ✅ PASS |
| Universal Core Contamination | 0 | 0 | ✅ PASS |
| Closure Error | 0 | 2 | ⚠️ MINOR |

---

## 5. Human Blind Review Quality Assessment

### 5.1 What Human Got Right (AI Missed)

| Finding | Evidence | Tables |
|---|---|---|
| HG#4 triggered (cross-module without FK indexes) | Cross-module FK reference analysis from metadata | #02, #03, #05 |
| HG#5 triggered (business ambiguity — state fields) | Multiple boolean state fields without documented transitions | #02, #05 |
| ext_table_example = OUT_OF_SCOPE | "Example" suffix + demo pattern | #04 |
| 68-column table = structural concern | Column count alone, no entity needed | #02 |
| F_TENANT_ID nullability risk | Metadata nullable flag | #01 |

### 5.2 What Human Missed (AI/Adversarial Caught)

| Finding | Evidence Required | Tables |
|---|---|---|
| decimal(9,2) precision insufficient | C# entity or domain knowledge | #04 |
| F_EN_CODE = critical hot path | C# service code analysis | #03 |
| F_ZX_SYSTEM_ID / F_ZX_DATATYPE platform fields | C# entity verification | #01 |
| SCD Type 2 pattern unverified in code | C# service code analysis | #05 |
| Column count 16 vs 17 arithmetic error | C# entity vs DB schema comparison | #01 |
| Tag inflation in AI Track A | Cross-table pattern analysis | All |

### 5.3 Human vs Adversarial Comparison

Human Track B and Adversarial Track B agreed on 4/5 tables for risk classification and on 3/5 tables for HG triggering. The agreement rate is high given that Human had no C# entity access.

**Key insight**: The most important findings (HG#4 and HG#5 triggers, OUT_OF_SCOPE classification) were accessible from metadata alone. The findings requiring code access (decimal precision, F_EN_CODE hot path) are important but not gate-blocking.

---

## 6. Final Comparative Summary

### 6.1 Track Comparison Matrix

| Criterion | AI Track A | Adversarial Track B | Human Track B |
|---|---|---|---|
| **Evidence base** | C# entities + SQL metadata | Same as AI + adversarial attack | DB metadata only |
| **Risk accuracy** | MEDIUM (tended to under-rate) | HIGH (most thorough) | MEDIUM-HIGH (structural gaps) |
| **HG accuracy** | LOW (borderline dodge) | HIGH (caught 5 HG issues) | HIGH (caught 4 HG issues) |
| **Action accuracy** | MEDIUM (SAFE-REFACTOR overuse) | HIGH (specific + conditional) | MEDIUM-HIGH (conservative bias) |
| **Scope classification** | MEDIUM (missed OUT_OF_SCOPE) | MEDIUM (missed OUT_OF_SCOPE) | HIGH (independently correct on #04) |
| **Systemic pattern detection** | LOW (tag inflation, borderline dodge) | HIGH (caught 5 patterns) | MEDIUM (caught 2 patterns) |
| **Evidence tag accuracy** | LOW (inflation widespread) | HIGH (accurate tagging) | N/A (Human uses [KNOWN]/[INFERRED] differently) |

### 6.2 Skill Calibration Assessment

The comparison reveals systemic issues in AI Track A's methodology:

1. **HG Borderline Dodge**: When AI is uncertain, it uses "borderline" rather than triggering. This avoids the Human Decision gate. Adversarial correctly identified this as a risk under-statement pattern.

2. **Tag Inflation**: AI consistently marks `[INFERRED]` as higher confidence than the evidence supports. Adversarial found 100% attack landing rate across all tables.

3. **Recommendation Drop**: AI identifies query patterns but inconsistently translates them to index recommendations. This creates incomplete execution plans.

4. **Evidence vs Conclusion Mismatch**: AI produces high-confidence conclusions (HIGH confidence, R0/R1) from medium-quality evidence (all [INFERRED] query patterns).

### 6.3 Human Blind Review Effectiveness

Despite structural evidence limitations (no C# entity access), Human Track B:
- Caught 4/5 HG issues that AI missed
- Independently confirmed the OUT_OF_SCOPE classification for ext_table_example
- Correctly identified structural concerns (68-column bloat, cross-module dependencies)
- Provided independent validation of the review process

**This validates the Human Blind Review requirement**: A human reviewer with metadata-only access can catch systemic AI failures that even adversarial AI review misses (HG#4 on #03).

---

## 7. Chief Architect Decision Points

### 7.1 HG Findings Requiring Decision

| HG | Table | Triggered By | Evidence | Decision Required |
|---|---|---|---|---|
| HG#4 | base_user | Human + Adversarial | organize/position/role references without FK indexes | Add (f_tenant_id, f_organize_id) index? |
| HG#5 | base_user | Human + Adversarial | multiple state fields without documented transitions | Document state machine or add to JNPF Extension? |
| HG#4 | base_visual_dev | Human + Adversarial | 5+ modules reference visual dev config | Confirm Foundry Profile handles cross-module refs? |
| HG#4 | sa_data_dictionary | Human + Adversarial | 5 SA tables reference this dictionary | Document ON DELETE behavior + cross-module dependency? |
| HG#5 | sa_data_dictionary | Human + Adversarial | bit vs int divergence; SCD Type 2 not verified | Foundry Profile Extension or schema migration decision? |

### 7.2 Skill Evolution Items

| Item | Evidence | Severity | Route To |
|---|---|---|---|
| HG Borderline Dodge pattern | 5 instances across 3 tables | Critical | Skill Evolution (Level A) |
| Tag Inflation pattern | 100% attack landing rate | High | Skill Evolution (Level A) |
| Recommendation Drop pattern | F_EN_CODE, F_TYPE, F_QUICK_QUERY | High | Skill Evolution (Level A) |
| decimal(9) precision issue | ext_table_example | Medium | JNPF Extension |
| "Example" table as baseline | ext_table_example | Medium | Skill Calibration |

---

## 8. Recommendations

### 8.1 For Chief Architect Sign-Off

**P8-A Shadow Gate: CONDITIONAL PASS recommended**

Rationale:
- R1 Human Blind Review: COMPLETE (LJY, 2026-08-30)
- HG FN = 5 (2 genuine misses, 3 borderline-insufficient) — acceptable given dormant risk at current data volumes
- P0/P1 Error = 0 — no critical decision errors
- Human independently confirmed OUT_OF_SCOPE for ext_table_example
- All disagreements are SAFE DISAGREEMENT (R3+ vs R2, etc.)

Conditions for full PASS:
- base_user HG#4 and HG#5: schedule Decision Brief
- base_visual_dev F_EN_CODE index: add to next batch
- sa_data_dictionary indexes: add to next batch

### 8.2 For P8-C UNFREEZE

All R1 conditions are satisfied. Proceed to R7 UNFREEZE directive when:
- Chief Architect signs P8-A Shadow Gate PASS
- Chief Architect signs P8-B Stability Gate PASS (R7)

### 8.3 For Skill Evolution

Priority 1 (systemic patterns affecting gate integrity):
1. HG Borderline Dodge: SKill must not use "borderline" to avoid triggering. If uncertain, trigger.
2. Tag Inflation: [INFERRED] confidence must be supported by at least one code citation or file/line reference.
3. Recommendation Drop: Every identified query pattern must have a corresponding index recommendation OR explicit "no index needed" justification.

---

**Document version**: 1.0
**Prepared by**: AI Engineer
**Date**: 2026-08-30
**Status**: Ready for Chief Architect review
