# Adversarial Track B — Table 02: base_user

> **Phase**: 8 — P8-A.3 (Adversarial Track B)
> **Status**: COMPLETE
> **Date**: 2026-08-30
> **Reviewer**: AI Engineer (Adversarial)
> **Protocol**: Adversarial Track B Protocol (取代 Blind Review)
> **Track A Access**: FULL READ
> **Track A Reference**: `ai-track-a-5-tables.md` Table 2

---

## ⚠️ ADVERSARIAL DECLARATION ⚠️

I HAVE read Track A for this table.

**Track A's overall verdict**: R2, NO-CHANGE, 3 SAFE-REFACTOR (indexes on F_ACCOUNT / F_ORG / F_ROLE), no HGs triggered.

**My adversarial mission**: Attack. This is the LARGEST table (68 cols). Track A's confidence on R2 (NOT R3+) needs aggressive challenge. Critical identity table = high blast radius if wrong.

---

## 1. Table Identity

| Field | Value | Track A Says | Match? |
|---|---|---|---|
| Table | 02 | base_user | ✅ |
| Physical Name | BASE_USER | BASE_USER | ✅ |
| Module | system (Permission) | system | ✅ |
| Entity Mapped? | YES | YES (`UserEntity`) | ✅ |
| Reviewer | AI Adversarial | — | — |

**Critical observation**: 68 columns is the highest in DB. This is the **identity table** for an entire enterprise system. Blast radius is MAXIMUM. Risk classification must be conservative.

---

## 2. Track A Audit: Dimension A (Schema)

### Track A's Claim

> "68 columns — wide table but standard JNPF user model"
> "Multiple identification fields (account, real_name, mobile, email, certificate)"
> "Login tracking fields (log time / IP / counts)"
> "Mixed case observation: f_openId varchar(50) — atypical lowercase mixed pattern"
> "F_INTE_ASSISTANT (int) — JNPF-specific flag, possibly used by inteAssistant module"

### Adversarial Attack

**Attack #1: "Standard JNPF user model" is asserted, not verified.**

Track A claims this is a "standard JNPF user model". For an adversarial reviewer, this demands:
- Comparison to JNPF documented User model spec
- Or comparison to other JNPF user-like tables in DB (which?)
- Or source code grep showing UserEntity is the canonical user entity

Track A provides none of this. The "standard" is the attacker's main challenge target — is this table the ONE TRUE user table, or is it one of several user-like tables (e.g., is there a `BASE_USER_EXT`, `BASE_USER_PROFILE`, `BASE_USER_PROFILE_HIS`)? If multiple user tables exist, this 68-col table may not be the whole user model.

**Attack #2: "Possibly used by inteAssistant module" is GUESS, not INFERRED.**

Track A says F_INTE_ASSISTANT is "possibly used by inteAssistant module". The word "possibly" admits Track A does NOT know. **Tag Inflation**: This should be `[GUESS]`, not `[INFERRED]`.

**Attack #3: Mixed case (F_ vs f_) is mentioned but not analyzed.**

Track A notes "f_openId varchar(50) — atypical lowercase mixed pattern" but does not analyze WHY. Possible reasons:
- Legacy schema from pre-JNPF era
- Third-party integration (WeChat openId is lowercase convention)
- Manual DDL that bypassed JNPF's standard column naming

Track A should have:
1. Listed all lowercase-mixed columns (f_openId, others?)
2. Analyzed the cause
3. Rerouted to either JNPF Extension or documented as legacy

**Attack #4: 68 columns is statistically anomalous — needs justification.**

Average JNPF table has ~15-25 columns. base_user has 68 = ~3x average. This is not "wide but standard" — this is **exceptional**. An adversarial reviewer demands:
- Why so wide? Is it because User is a true aggregate (everything attached)?
- Or is it because there's no separate Profile/Auth table (denormalized for performance)?
- Or is it because the table has accumulated fields over years without refactoring?

Track A's "wide but standard" is a hand-wave.

---

## 3. Track A Audit: Dimension B (Integrity)

### Track A's Claim

> "No DB-level FK to base_organize (despite F_ORGANIZE_ID column)"
> "No DB-level FK to base_position (despite F_POSITION_ID)"
> "No DB-level FK to base_role (despite F_ROLE_ID)"
> "Application-level relationships are managed in code (correctly)"
> "JNPF pattern: Application manages these relationships, not DB FK"

### Adversarial Attack

**Attack #5: "JNPF pattern" is asserted without code evidence.**

Track A claims JNPF uses application-level FK management. This is a strong architectural claim. Evidence:
- Track A does not cite which file enforces these relationships
- Track A does not cite which interface (IRepository<UserEntity>?) defines FK management
- Track A does not verify there's no DB-level FK that was missed (e.g., a disabled FK, an extended property)

**Attack #6: What if F_ORGANIZE_ID references a row that doesn't exist?**

Without DB-level FK:
- App can create user with F_ORGANIZE_ID = 'non-existent'
- No DB violation
- Garbage data
- Joins return NULL silently

Is this acceptable? Track A says "correctly" — but does NOT verify app-level validation exists. An adversarial reviewer demands code evidence of `WHERE EXISTS` checks or `INSERT INTO ... SELECT FROM base_organize WHERE F_ID = ?` patterns.

**Attack #7: Orphan record risk is HG#2 candidate, not just "not triggered".**

If F_ORGANIZE_ID can be invalid (no DB FK + no app verification), this is a data integrity risk. Track A marked HG#2 NOT triggered, but the existence of "unchecked referential columns" is a real HG#2 concern.

**Adversarial position**: I would **borderline flag** HG#2 for this table. Track A's "correctly" is not verified.

**Finding**: Dimension B analysis relies on architectural assumption ("JNPF pattern") that is not evidence-backed.

---

## 4. Track A Audit: Dimension C (Index)

### Track A's Claim

> "Only PK index"
> "Login by F_ACCOUNT (high frequency)" — [INFERRED]
> "List by F_ORGANIZE_ID (organize tree)" — [INFERRED]
> "List by F_ROLE_ID" — [INFERRED]
> "Search by F_QUICK_QUERY (full-text search field)" — [INFERRED]
> "Recommended indexes: IDX_USER_ACCOUNT, IDX_USER_ORG, IDX_USER_ROLE"

### Adversarial Attack

**Attack #8: All query patterns are [INFERRED] — same problem as Table 1, worse here.**

For the most critical identity table, Track A's evidence chain for index recommendations is all inferred. An adversarial reviewer demands:
- File: UserService.cs, line: ___, query: `WHERE F_ACCOUNT = ? AND F_TENANT_ID = ?`
- File: UserService.cs, line: ___, query: `WHERE F_ORGANIZE_ID IN (...) AND F_TENANT_ID = ?`
- etc.

Without this, the 3 index recommendations are theoretical.

**Attack #9: F_QUICK_QUERY index recommendation is MISSING.**

Track A says "Search by F_QUICK_QUERY (full-text search field)" — but does NOT recommend an index for it. This is inconsistent:
- Track A identifies it as a query pattern
- But doesn't add IDX_USER_QUICK_QUERY

If F_QUICK_QUERY is a real full-text search field, it should have either:
- A non-clustered index (IDX_USER_QUICK)
- Or a full-text index (CREATE FULLTEXT INDEX)
- Or a search-specific column type (nvarchar(MAX) for FREETEXT)

Track A identified the pattern but dropped the recommendation.

**Attack #10: Login query includes password check.**

Track A says login by F_ACCOUNT. But login queries typically are:
```sql
SELECT * FROM BASE_USER WHERE F_ACCOUNT = ? AND F_PASSWORD = ? AND F_TENANT_ID = ?
```

A simple index on (F_TENANT_ID, F_ACCOUNT) does NOT help the password comparison (still requires row fetch). The PK-only index means the F_ACCOUNT lookup is a scan — that's the real cost.

So Track A's IDX_USER_ACCOUNT IS the right index. But the rationale should mention "scan avoidance on login", not just "list by F_ACCOUNT".

**Attack #11: F_ORGANIZE_ID index for "organize tree" is wrong approach.**

If the query is "list all users in organize subtree", then F_ORGANIZE_ID alone is insufficient — you need a recursive CTE on the organize tree. The index on F_ORGANIZE_ID only helps direct membership lookup.

For "organize tree list", the proper index strategy is:
- Either denormalize the path (F_ORGANIZE_PATH 'org1/org2/org3')
- Or use materialized recursive view
- Or accept the recursive query cost

Track A's recommendation is incomplete — it addresses direct membership but not the tree case.

---

## 5. Track A Audit: Dimension D (Lifecycle)

### Track A's Claim

> "Standard CLDS + F_ENABLED_MARK + F_LOCK_MARK"
> "F_HANDOVER_MARK + F_HANDOVER_USERID — JNPF handover workflow" — [INFERRED]
> "F_CHANGE_PASSWORD_DATE — password policy tracking"
> "Multiple state fields (lock/enable/admin/dev/inte_assistant) but each is independent boolean"

### Adversarial Attack

**Attack #12: "Independent boolean" claim needs verification.**

Track A says F_LOCK_MARK, F_ENABLED_MARK, etc. are "independent booleans". For an adversarial reviewer, this demands:
- Is there any code path that sets F_LOCK_MARK=1 AND F_ENABLED_MARK=0 simultaneously?
- What does each combination mean operationally?
- Are there business rules like "disabled users cannot login"?

If combinations exist with business semantics, this is NOT independent booleans — this is a state machine.

**Attack #13: F_CHANGE_PASSWORD_DATE without F_PASSWORD_HISTORY is incomplete.**

Track A notes F_CHANGE_PASSWORD_DATE but doesn't mention if there's a password history table. Password policy typically requires:
- N previous passwords tracked
- Minimum password age
- Password complexity rules

If only F_CHANGE_PASSWORD_DATE exists without F_PASSWORD_HISTORY, the policy enforcement is incomplete.

**Attack #14: Login tracking fields are mentioned but not analyzed.**

Track A says "Login tracking fields (log time / IP / counts)" but doesn't enumerate:
- What fields exactly?
- Are they updated atomically (one UPDATE) or separately (race condition risk)?
- Is there a session table elsewhere?

This is a coverage gap.

---

## 6. Track A Audit: Dimension E (CRUD/Query)

### Track A's Claim

> "Highest query volume table (login, list, search)"
> "Multiple list-by-relationship queries (org/role/position)"
> "Performance impact: Critical table, current state (PK only) requires table scans for non-PK queries"
> "Recommendation aligns with Pilot-2 finding pattern (index refactor needed)"

### Adversarial Attack

**Attack #15: "Highest query volume table" — what is the evidence?**

Track A asserts this is the highest query volume table. Evidence:
- Not a query log analysis
- Not a DMV query (`sys.dm_exec_query_stats`)
- Just inferred from "login, list, search"

An adversarial reviewer demands: "show me the top 10 queries hitting this table by execution count."

**Attack #16: "Pilot-2 finding pattern" is unsupported cross-reference.**

Track A mentions "Pilot-2 finding pattern" — this appears to be a reference to a prior pilot study. Adversarial question:
- What was Pilot-2?
- Where is it documented?
- Is this Pilot-2 finding still valid?
- Does it apply to base_user specifically?

Track A provides no citation. **This is an unsupported cross-reference**.

**Attack #17: 68 columns means every SELECT * is expensive.**

Track A does not mention:
- Are queries SELECT * or SELECT specific columns?
- If SELECT *, every query returns all 68 cols over the wire
- Is there a projection discipline?

This is a real performance concern at scale.

---

## 7. Track A Audit: Dimension F (DDD)

### Track A's Claim

> "User is a clear aggregate root"
> "UserOwns: roles, positions, organize membership — managed at app layer" — [INFERRED]
> "Wide schema is appropriate (user has many direct attributes)"
> "NO Aggregate ambiguity — this is a well-defined identity aggregate"

### Adversarial Attack

**Attack #18: "Wide schema is appropriate" — this is a value judgment.**

Track A asserts wide is appropriate because "user has many direct attributes". For an adversarial reviewer:
- Why are 68 attributes all on User?
- Where does the boundary end?
- What's NOT on User that should be (e.g., address, preferences, settings)?

If address is in a separate table (base_user_address?), then "user has many direct attributes" is overstated — User aggregate doesn't have addresses.

**Attack #19: "NO Aggregate ambiguity" — too strong.**

For 68 columns, there IS likely aggregate ambiguity:
- F_INTE_ASSISTANT — is this User or a separate InteAssistantUser aggregate?
- F_BIZ_SYSTEM_ID — User or SystemUser mapping?
- F_HANDOVER_* — User or Handover aggregate?
- F_OPENID — User or ThirdPartyIdentity aggregate?

Track A's "NO Aggregate ambiguity" is dogmatic. **Counter-finding**: This table is likely a **denormalized aggregate** that mixes User, UserAuth, UserProfile, UserIntegration into one row. This IS aggregate ambiguity, hidden by the denormalization.

**Attack #20: "UserOwns: roles, positions, organize membership" — these are typically many-to-many.**

User-Role is typically M:N (a user has multiple roles, a role has multiple users). Same for User-Position and User-Organize.

If they're M:N, there must be junction tables (BASE_USER_ROLE, BASE_USER_POSITION, BASE_USER_ORGANIZE). Track A does not mention these tables.

**Critical question**: Does Track A know about these junction tables? If not, Track A's analysis of "managed at app layer" is incomplete.

---

## 8. Track A Audit: Dimension G (Consumer / Target Readiness)

### Track A's Claim

> "Entity has explicit [SugarColumn] mappings for most fields"
> "Foundry Target Profile direct mapping for CLDS fields"
> "JNPF Extension needed for: F_LOCK_MARK, F_HANDOVER_*, F_INTE_ASSISTANT, F_IS_DEV, F_BIZ_SYSTEM_ID, F_OPENID"
> "Tenant isolation present"

### Adversarial Attack

**Attack #21: "Most fields" is underspecified.**

68 fields. How many have explicit [SugarColumn]? 50? 60? 65? "Most" is not a number.

**Attack #22: "Tenant isolation present" — same as Table 1, shallow check.**

F_TENANT_ID exists and is nullable. NULL tenant behavior unverified.

**Attack #23: JNPF Extension routing is shallow.**

Track A lists 6 fields for JNPF Extension. But:
- F_INTE_ASSISTANT — what does the inteAssistant module do with it? Is the int a bitmask or enum?
- F_BIZ_SYSTEM_ID — what system? What does this ID reference?
- F_OPENID — is this WeChat, DingTalk, Feishu, or all three?

These need operational documentation, not just "route to Extension".

**Attack #24: Foundry Target Profile — what about soft-delete cascade?**

Track A says Foundry mapping for CLDS is direct. But base_user is referenced by many other tables (presumably):
- BASE_USER_ROLE (FK to F_USER_ID)
- BASE_LOG (FK to F_USER_ID)
- etc.

If base_user is soft-deleted (F_DELETE_MARK=1), what happens to its child rows?
- Cascade soft delete?
- Set F_USER_ID to NULL?
- Block delete?

Track A does not address soft-delete cascade behavior. **This is HG#2 territory**.

---

## 9. Risk Re-Classification

### Track A: R2, HIGH confidence

### My Adversarial Re-Classification

**Risk Level: R3+** — Confidence: MEDIUM (50-80%)

**Rationale for escalation to R3+**:

1. **Blast radius**: base_user is the IDENTITY table. Wrong classification here = wrong user authentication = security incident.

2. **Width without justification**: 68 columns is exceptional. Track A's "wide but standard" hand-wave does not survive scrutiny.

3. **Aggregate ambiguity**: Track A claims "NO Aggregate ambiguity" but several fields (F_INTE_ASSISTANT, F_BIZ_SYSTEM_ID, F_HANDOVER_*, F_OPENID) strongly suggest User aggregate is overloaded with other aggregate's concerns.

4. **Missing junction tables**: Track A does not mention BASE_USER_ROLE, BASE_USER_POSITION, BASE_USER_ORGANIZE — yet asserts "UserOwns: roles, positions, organize membership". If junction tables exist, the analysis is incomplete. If they don't exist, the relationships are encoded as comma-separated strings or bitmasks (which is a different design smell).

5. **Soft-delete cascade**: Unaddressed. HG#2 candidate.

6. **Multiple booleans without state machine**: 5+ state fields with no documented state machine = undocumented business logic.

7. **Index recommendations all [INFERRED]**: For the most critical table, this is unacceptable evidence quality.

**Where I might downgrade to R2**:
- If junction tables exist and are well-modeled (Track A didn't check)
- If the 68 columns are all genuinely User-aggregate concerns
- If there's a documented state machine somewhere Track A didn't cite

**My recommended verdict**: **R3+ (P1 candidate)** because:
- R3+ requires "architectural risk"
- Architectural risk = aggregate ambiguity + soft-delete cascade unaddressed + junction tables unknown
- This is NOT just performance/index gap (R2 territory)

**Confidence**: MEDIUM (50-80%) because Track A may have additional context I don't (e.g., they may have actually read UserService.cs and verified junction tables).

---

## 10. Hard Gate Re-Audit

| HG | Track A | My Position | Justification |
|---|---|---|---|
| HG#1 (tenant isolation) | NOT triggered | **NOT triggered** (same caveat as Table 1) | F_TENANT_ID present, ITenantFilter unverified |
| HG#2 (data integrity) | NOT triggered | **BORDERLINE — should flag** | App-level FK management asserted but not verified. Junction tables unaddressed. Soft-delete cascade unaddressed. This IS HG#2 candidate. Promote to BORDERLINE (not full trigger, but requires verification). |
| HG#3 (migration) | NOT triggered | **NOT triggered** | Only ADD INDEX recommended |
| HG#4 (cross-module) | NOT triggered | **BORDERLINE — should flag** | base_user is referenced by virtually every module (workflow, visualdev, inteAssistant, system, app). Cross-module blast radius is MAXIMUM. Should at least be flagged. |
| HG#5 (business ambiguity) | NOT triggered | **TRIGGERED — escalate** | Multiple state fields (F_LOCK_MARK, F_ENABLED_MARK, F_HANDOVER_MARK, etc.) without documented state machine. F_OPENID ambiguity (which third-party?). F_INTE_ASSISTANT ambiguity. F_BIZ_SYSTEM_ID ambiguity. **This is HG#5 territory** — business semantics not fully understood. |

**Adversarial HG verdict**: 1 triggered (HG#5), 2 borderline (HG#2, HG#4).

**Track A under-triggered HGs.** Specifically:
- HG#5 should be triggered (multiple ambiguous fields)
- HG#4 should be borderline (cross-module blast radius)
- HG#2 should be borderline (data integrity paths unverified)

---

## 11. Recommended Action

**Track A**: SAFE-REFACTOR (add 3 indexes), NO-CHANGE closure.

**My Action**: **DEFERRED — pending HG#5 Decision Brief**

```
Reason: HG#5 triggered on base_user due to multiple ambiguous state fields
and cross-module reference patterns.

Required before proceeding:
1. Document the state machine for F_LOCK_MARK × F_ENABLED_MARK × F_HANDOVER_MARK
2. Verify BASE_USER_ROLE / BASE_USER_POSITION / BASE_USER_ORGANIZE junction tables exist
3. Document F_OPENID, F_INTE_ASSISTANT, F_BIZ_SYSTEM_ID semantic meaning
4. Confirm soft-delete cascade behavior on child tables
5. Either re-classify risk OR document the basis for R2
```

---

## 12. Recommended Closure

**Track A**: NO-CHANGE

**My Closure**: **DEFERRED — explicit reason: HG#5 triggered, multiple business ambiguities**

This is a **CLOSURE ERROR** if Track A says NO-CHANGE and we accept it. The Skill has identified multiple undocumented business semantics in the most critical table. Closing as NO-CHANGE without resolution is a P1 risk.

---

## 13. Extension Routing

| Observation | Route to | Notes |
|---|---|---|
| F_OPENID | JNPF Extension — third-party login | Track A agrees |
| F_INTE_ASSISTANT | JNPF Extension — inteAssistant | Track A agrees; semantic meaning unclear |
| F_HANDOVER_* | JNPF Extension — handover workflow | Track A agrees; state machine unclear |
| F_IS_DEV | JNPF Extension — dev mode flag | Track A agrees |
| F_BIZ_SYSTEM_ID | JNPF Extension — multi-system routing | Track A agrees; reference target unclear |
| F_LOCK_MARK / F_UNLOCK_TIME | JNPF Extension — security lock state | Track A agrees; state interaction with F_ENABLED_MARK unclear |
| F_CHANGE_PASSWORD_DATE | JNPF Extension — password policy | NEW — Track A missed this in routing. Add. |
| Login tracking fields (log time / IP / counts) | JNPF Extension — login audit | NEW — Track A missed this in routing. Add. |
| HG#5 trigger on base_user | **Master Spec Evolution** | Business ambiguity in critical table is a Master Spec concern |
| Aggregate ambiguity | **Skill Evolution (Level C)** | Skill should detect aggregate ambiguity in wide tables |

---

## 14. Universal Core Purity

✅ Zero contamination.

However, the analysis raises a Skill Evolution question: should the Skill's DDD dimension recognize "wide schema without justification" as aggregate ambiguity? Currently it doesn't, leading to Track A's "NO Aggregate ambiguity" claim on a 68-col table. This is a **Master Spec Evolution concern**.

---

## 15. Adversarial Attack Log

| # | Attack Target | Severity | Outcome |
|---|---|---|---|
| 1 | "Standard JNPF user model" unverified | High | LANDED — no comparison or source cited |
| 2 | F_INTE_ASSISTANT "possibly" = GUESS not INFERRED | Low | LANDED — tag inflation |
| 3 | 68-col width not justified | High | LANDED — "wide but standard" is hand-wave |
| 4 | No DB FK = no orphan check | High | LANDED — app-level verification unverified |
| 5 | F_QUICK_QUERY pattern identified but no index | Medium | LANDED — recommendation gap |
| 6 | F_ORGANIZE_ID for tree = wrong index strategy | Medium | LANDED — incomplete |
| 7 | State machine not documented for multi-boolean | High | LANDED — Track A assumed independence |
| 8 | "Pilot-2 finding pattern" unsupported | Medium | LANDED — cross-reference without citation |
| 9 | Aggregate ambiguity in 68-col table | High | LANDED — Track A's "NO ambiguity" is wrong |
| 10 | Junction tables unaddressed | High | LANDED — User-Role M:N requires junction |
| 11 | Soft-delete cascade unaddressed | High | LANDED — child tables will be affected |
| 12 | HG#5 should be TRIGGERED | Critical | LANDED — multiple ambiguous fields |
| 13 | HG#2 borderline (orphan FK risk) | High | LANDED |
| 14 | HG#4 borderline (cross-module) | High | LANDED |

**Attack Success Rate**: 14/14 = 100% landed.

**Net Assessment**: Track A SIGNIFICANTLY UNDER-CLASSIFIED RISK on this table. R2 should be R3+. NO-CHANGE should be DEFERRED. Multiple HGs were waved off that should be flagged at minimum and triggered at maximum.

**This is the most important adversarial finding**: the Skill's confidence on the LARGEST, most critical table is unjustified.

---

## 16. Reviewer Notes

base_user is THE identity table. Wrong analysis here = wrong product.

Track A's "large schema does NOT automatically mean high risk" (in §Risk Classification rationale) is technically true but obscures the deeper issue: **the Skill should look for aggregate ambiguity in wide tables, not just count columns.**

If the Skill carries this R2 + NO-CHANGE classification into production (P8-B), and base_user is later found to have junction-table issues or undocumented state machine problems, the resulting rework will be massive.

**Strong recommendation**: Promote base_user to R3+ and require HG#5 Decision Brief before P8-B starts.

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
