---
name: table-refactor-expert
description: Universal single-table refactoring expert operationalizing the Master Spec + Execution Manual triad. Use when AI must systematically assess, design, refactor, verify, and close a single relational table across any database / ORM / domain. Produces Evidence Ledger, applies Risk-Adaptive Flow, enforces Hard Gates, stops at Evidence Sufficiency, executes no-change closure as first-class outcome. Project-agnostic; JNPF / Foundry / BBB / CLDEntityBase / TenantId / any specific ORM / any field naming convention are FORBIDDEN in this skill and must live in a separate Project Extension or Target Profile.
---

# Table Refactoring Expert — Universal Skill

> **Core principle**: This skill is a **router + executor**, not a third rulebook. Every technical standard is defined in `Master Spec`; every procedure is defined in `Execution Manual`. This skill routes to them and executes them. It creates no new Universal Rule.

## 1. When to Use

**Use** when:

- AI must assess one relational table systematically across seven Capabilities (A–G)
- AI must produce an Evidence Ledger + Closed Gate output for a table
- AI must apply Risk-Adaptive flow (R0–R5) and decide Process Weight
- AI must route evidence by Finding Type without full-project scans
- AI must coordinate batches of table closures

**Do NOT use** when:

- Task is pure class-level / service refactoring (use class refactoring expert)
- Task is pure query tuning without table-level semantics (use database performance expert if exists)
- Task is schema migration execution without expert assessment (use migration tool)
- Task already specifies JNPF/Foundry/project-specific knowledge — read Project Extension instead

## 2. Hard Gates (constitution — non-negotiable)

These six rules **cannot** be overridden by any project, profile, or extension:

### 2.1 Reference-First Rule

When this skill needs a technical standard (schema rule, integrity rule, index criterion, risk definition, evidence threshold, Hard Gate, DoD), it **MUST** reference `Master Spec §X.Y`. When it needs an execution procedure (flow, gate, rollback, batching, closure), it **MUST** reference `Execution Manual §X.Y`. Skill does not restate either source.

### 2.2 No Autonomous Rule Creation

When encountering a situation not defined in Master Spec or Execution Manual:

```
Undefined situation
  → classify: Decision / Extension / Gap
  → escalate: Decision Brief or Human
  → DO NOT GUESS a new Universal Rule
```

The skill must **never** silently introduce a new Universal Rule, even if it appears justified by project experience. A new Universal Rule requires modifying Master Spec via the Purity Gate process.

### 2.3 Extension Isolation

Reading order is always:

```
1. Universal Core (Master Spec + Execution Manual)
2. Project Profile (loaded only if Project Extension is present)
3. Target Profile (loaded only if Target Profile is present)
```

A Project Profile or Target Profile **cannot override** a Universal Rule. It can only:

- Add a project-specific mapping (e.g., "this project's tenant field is `X`")
- Add a target-specific mapping (e.g., "this target's audit contract requires `CreatedBy`")
- Document a project-specific exception with explicit "Extension Exception" label

If a Project Profile entry contradicts a Universal Rule, **the Universal Rule wins** and the Project Profile entry must be flagged.

### 2.4 Evidence Sufficiency Stop Rule

```
Need evidence for Finding X?
  → Identify Finding Type (A/B/C/D/E/F/G)
  → Route to evidence source per Execution Manual §6
  → Collect until Master Spec §11.3 minimum threshold is met
  → STOP — do not continue searching
  → Proceed to Design / Action
```

Continuing to search "to be more certain" after threshold is a violation.

### 2.5 No Scope Escalation by Discovery

A Finding discovered during a single Table Unit assessment **stays within that Table Unit** by default. If the Finding clearly exceeds Table Unit scope (cross-table, cross-module, cross-architecture), the skill:

1. Records the Finding in Evidence Ledger with scope = `BEYOND_TABLE`
2. Stops processing for the current table
3. Triggers Hard Gate (Master Spec §10.3) → Decision Brief → Human

The skill **must not** auto-expand into module-wide or project-wide refactoring.

### 2.6 No-change is a First-Class Outcome

```
Discover → Assess → (justified no-change) → Verify (current state intact) → CLOSED
```

The skill must allow no-change closure without pressure to produce diff. Master Spec §13.4 + Execution Manual §11.3 govern this path.

## 3. Execution Protocol (9 Interfaces)

This skill operates through nine interfaces. Each interface **routes** to Master Spec / Execution Manual; **no interface redefines rules**.

### 3.1 State Machine (`TableState`)

| State | Enter | Exit | Reference |
|---|---|---|---|
| DISCOVERED | Discovery input valid | Assess input ready | Manual §2.1 |
| ASSESSED | 7 Capabilities filled | Design input ready | Manual §2.1 |
| DESIGNED | DESIGNs aligned with Findings | Approval Gate passed | Manual §2.1 |
| READY | Approval Gate decision recorded | Refactor started | Manual §2.1 |
| REFACTORED | Refactor flow complete | Verify started | Manual §2.1 |
| VERIFIED | 13 DoDs achieved (Spec §13.2) | Closed Gate evaluated | Manual §2.1 |
| CLOSED | 5 Closed Gate conditions met | (Re-trigger only) | Manual §11 |

`READY ≠ REFACTORED` — explicitly preserved.

### 3.2 Step Executor (`runStep`)

For each Step in Execution Manual §3:

```
Input → Master Spec reference (if technical)
     → Execution Manual reference (if procedural)
     → Action → Evidence collection (with Sufficiency Stop)
     → Output → Stop Condition check → Escalation if needed
```

Each Step's six fields (Input/Action/Evidence/Output/Stop/Escalation) are **read** from Manual §3 — not redefined here.

### 3.3 Document Router (`routeDoc`)

| Question type | Route to |
|---|---|
| "What is correct schema / integrity / index / lifecycle / query / DDD / readiness?" | Master Spec §3–§9 |
| "What is the Risk level / Hard Gate / DoD?" | Master Spec §10 / §12 / §13 |
| "What evidence is sufficient?" | Master Spec §11.3 |
| "When / how to execute?" | Execution Manual §3 |
| "What Gate / Approval applies?" | Execution Manual §5 |
| "What Refactor Type / Rollback?" | Execution Manual §8 |
| "How to batch?" | Execution Manual §9 |
| "How to close?" | Execution Manual §11 |
| Project-specific field mapping | Project Profile (NOT Master Spec) |
| Target-specific contract mapping | Target Profile (NOT Master Spec) |

### 3.4 Gate Evaluator (`evaluateGate`)

| Gate | Source | Decision |
|---|---|---|
| Auto-Close | Manual §5.2 (R0) | AI autonomous |
| Auto-Apply | Manual §5.2 (R1) | AI autonomous |
| Evidence-Driven Auto | Manual §5.2 (R2) | AI autonomous (evidence-backed) |
| Human Approval | Manual §5.2 (R3) | Human decision required |
| Cross-Table | Manual §5.2 (R4) | Product + Architecture decision |
| Destructive | Manual §5.2 (R5) | Product + Architecture + Pilot Dry-run |

`Approval ≠ New Audit` — Manual §5.3 governs.

### 3.5 Evidence Router (`routeEvidence`)

For each Finding Type (A–G), Execution Manual §6 / Appendix C defines:

- Source priority (which files/code to read)
- Forbidden scope (which to NOT scan)
- Minimum threshold (when to stop)

The skill **must not** deviate from this routing. Deviation is logged as a routing violation.

### 3.6 Evidence Ledger (`updateLedger`)

Ledger fields per Execution Manual §7:

| Field group | Fields |
|---|---|
| Current Fact | `[KNOWN]` / `[COMPUTED]` / `[INFERRED]` / `[GUESS]` entries |
| Target State | `[DESIGN]` entries |
| Decision | Risk, Hard Gate detection, Gate resolution |
| Change | Refactor flow + intermediate verification |
| Verification | 13 DoDs + KPI + performance archive |

Taxonomy is the Master Spec §11.1 five labels. **No second taxonomy permitted.**

### 3.7 Refactor Action (`applyRefactor`)

Per Execution Manual §8 / Appendix D:

| Type | Trigger | Rollback |
|---|---|---|
| Schema | ALTER / ADD / DROP COLUMN | Reverse ALTER |
| Data | migration / value conversion / backfill | **Backup + dry-run required** |
| Index | CREATE / DROP / REBUILD | DROP reverse |
| Constraint | UNIQUE / FK / CHECK | DROP constraint |
| Code / Entity | Entity / Repository / Service change | git revert |

**Data Rollback ≠ Code Rollback** — Manual §8.2 governs.

### 3.8 Batch Coordinator (`coordinateBatch`)

Per Execution Manual §9:

- Batch size: 3–8 tables
- Risk homogeneity: ≤ 2 Risk-grade spread within batch
- Module clustering: same module preferred
- Dependency order: parent → child
- Pause trigger: any Hard Gate within batch

### 3.9 TABLE CLOSED Gate (`closeGate`)

Per Execution Manual §11:

- 5 required conditions (Evidence sufficient / Target settled / Refactor or no-change / Verification passed / No blocking)
- 6 required records (Before / After / Key evidence / Accepted constraints / Deferred items / Re-trigger conditions)
- No-change closure path explicitly allowed

## 4. Document Routing Table (consolidated)

| Skill needs to know... | Read this |
|---|---|
| What is correct table design | Master Spec §3–§9 |
| Risk levels | Master Spec §10 |
| Hard Gate triggers | Master Spec §10.3 / §12 |
| Evidence taxonomy | Master Spec §11.1 |
| Evidence thresholds | Master Spec §11.3 + per-Capability subsections |
| DoD | Master Spec §13.2 |
| TABLE CLOSED semantics | Master Spec §13 |
| KPI definitions | Master Spec §14 |
| Purity Gate | Master Spec §15 |
| State machine | Execution Manual §2 |
| 5-step SOP | Execution Manual §3 |
| Risk-Adaptive flow | Execution Manual §4 |
| Approval Gate | Execution Manual §5 |
| Evidence Routing | Execution Manual §6 |
| Ledger format | Execution Manual §7 |
| Refactor types | Execution Manual §8 |
| Batch rules | Execution Manual §9 |
| Failure recovery | Execution Manual §10 |
| TABLE CLOSED Gate | Execution Manual §11 |
| Efficiency discipline | Execution Manual §12 |
| Project-specific field / ORM | Project Profile (NOT this skill) |
| Target-specific contract | Target Profile (NOT this skill) |

## 5. Tooling Boundaries

**This skill does not implement MCP / CLI / independent tools.**

When Skill execution discovers a Tool Gap:

```
Tool Gap detected
  → Record in skill execution log
  → Continue with existing tools if possible
  → Note gap for Generic Validation phase
  → Tool Gap → repeated cross-project → MCP Capability Proposal (separate phase)
```

**Do not** develop MCP in Phase 3. Generic Validation comes first.

## 6. Output Contract

For each Table Unit, the skill produces:

| Output | Format | Required when |
|---|---|---|
| Evidence Ledger (machine-readable) | JSON / YAML per Manual §7 | Always (including no-change) |
| Closed Gate decision (5 conditions) | Boolean per condition | CLOSED transition |
| Closed Record (6 items) | Structured text per Manual §11.2 | CLOSED state |
| Hard Gate hits (if any) | Decision Brief template | Any Hard Gate triggered |
| Routing violations (if any) | Log entry | Any deviation from Manual §6 routing |
| Tool Gaps (if any) | Log entry | Any detected tool gap |

## 7. Failure Modes

| Failure | Detection | Recovery | Reference |
|---|---|---|---|
| Test failure | Verify step | Local rollback + reason analysis | Manual §10 |
| Schema validation failure | Verify step | Reverse ALTER + redo | Manual §10 |
| Migration failure | Refactor | Abort + backup restore | Manual §10 |
| Contradictory evidence | Any step | STOP → Decision Brief | Manual §10 |
| Unexpected behavior | Verify / after | Refactor rollback + re-Assess | Manual §10 |
| **Master Spec / Manual not loaded** | Skill start | Hard fail — skill cannot run without authoritative sources | §2.1 |
| **Project Extension conflicts with Universal Rule** | Any Step | Universal Rule wins; flag Extension | §2.3 |

**Local rollback > project restart** — Manual §10.2 governs.

## 8. Self-Contamination Defense

The skill guards against its own rule drift:

1. **No second taxonomy** — Master Spec §11.1 labels only.
2. **No new Universal Rule** — Undefined situations escalate per §2.2.
3. **No project-specific knowledge in skill** — All JNPF / Foundry / BBB / specific ORM knowledge lives in Project Extension / Target Profile.
4. **No new process steps** — Execution Manual §3 five steps only.
5. **No new Gate types** — Execution Manual §5 six Gate types only.
6. **No new DoD items** — Master Spec §13.2 13 items only.

If a perceived need for any of the above arises, the correct action is to escalate (modify Master Spec / Execution Manual), not to add to this skill.

## 9. Quick Start (operational sequence)

```
1. Load Master Spec path (canonical reference)
2. Load Execution Manual path (canonical reference)
3. (Optional) Load Project Profile if present
4. (Optional) Load Target Profile if present
5. Initialize TableState = DISCOVERED
6. For each Step in Execution Manual §3:
   a. runStep → routeDoc → collect evidence (Sufficiency Stop)
   b. Update Ledger
   c. If Hard Gate triggered → Decision Brief + STOP
   d. If Approval Gate required → gate evaluation
   e. If Refactor required → applyRefactor (per type)
   f. Verify per Master Spec §13.2
   g. Evaluate Closed Gate (5 conditions)
7. Transition to CLOSED or escalate
```

If at any point an undefined rule / behavior is needed → §2.2.

## 10. Phase 3 Exit Criteria (self-check)

- [ ] Skill can launch Table Refactoring lifecycle (State Machine initializes)
- [ ] Skill correctly references Master Spec (not restating)
- [ ] Skill correctly references Execution Manual (not restating)
- [ ] Skill executes 5-step flow via Execution Manual §3
- [ ] Skill maintains State per Execution Manual §2
- [ ] Skill routes Evidence per Execution Manual §6
- [ ] Skill identifies Hard Gate per Master Spec §10.3
- [ ] Skill executes Risk-Adaptive flow per Execution Manual §4
- [ ] Skill maintains Ledger per Execution Manual §7
- [ ] Skill executes or skips Refactor per Execution Manual §8
- [ ] Skill executes Verify per Master Spec §13.2
- [ ] Skill triggers TABLE CLOSED Gate per Execution Manual §11
- [ ] Skill supports no-change closure per Master Spec §13.4
- [ ] Skill preserves Universal Core Purity (no JNPF / Foundry / BBB / ORM / dialect / naming convention)
- [ ] No Master Spec duplication
- [ ] No Execution Manual duplication
- [ ] No new technical rules introduced
- [ ] Placeholder = 0
- [ ] No internal contradiction

## 11. Out of Scope (explicit)

- Implementing MCP / CLI / independent tools
- Loading JNPF Extension / Foundry Target Profile (Phase 5)
- Executing Pilot on real JNPF tables (Phase 6)
- Performance benchmarking methodology (Master Spec §14 references)
- DDL / migration execution tools (separate product)
- Migration rollback execution (separate product)
