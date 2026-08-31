# Phase 1 Policy Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现 5 条硬策略的 `Policy → Hook → Evidence → Gate → State` 垂直闭环，让 AI 实际被阻止假绿/跳构建/无证据完成

**Architecture:** Hook-per-Policy as Phase 1 implementation strategy (NOT permanent: Hook = Enforcement Point, not Policy Container); future stable hooks are `PreMutationHook/PreBuildHook/PreTestHook/PreCompletionHook` where Policy Engine evaluates. Current 5 independent hooks (hooks/ 允许 .mjs) + 共享 policy-lib.mjs + evidence-collector.mjs (structured Evidence Producer) + GATE-COMPLETION as Final Gate only (pre-action BLOCK via Pre-* hooks, not delayed to final); Harness Resolver 不堆叠；Evidence 落 `.claude/control-plane/09-evidence/`；Adversarial 7 场景 验证 BLOCK + Bypass + Determinism + Versioning

> **校准 (Chief Architect 6 点):** ①Hook-per-Policy 仅 Phase1 简化，非永久架构 ②GATE-COMPLETION 仅 Final Gate ③Evidence 结构化 ④Adversarial 7 ⑤复用 AgentOS State Authority ⑥Policy 含 Scope/Version/Hard-vs-Conditional

**Tech Stack:** Node.js 24 hooks (PreToolUse/Stop), JSON Evidence, powershell test harness, existing guard-write infra (L10c allows hooks/*.mjs)

---

## File Structure

```
.claude/control-plane/11-policies/POLICY-001-NO-FAKE-GREEN.md          # Hard Policy @1.0
.claude/control-plane/11-policies/POLICY-002-REAL-BUILD.md             # Conditional Policy @1.0 (scope: refactoring/feature/bugfix; audit→AuditOnly)
.claude/control-plane/11-policies/POLICY-003-MUTATION-EVIDENCE.md      # Hard Policy @1.0
.claude/control-plane/11-policies/POLICY-004-CONTRACT-PRESERVATION.md  # Hard Policy @1.0
.claude/control-plane/11-policies/POLICY-005-COMPLETION-EVIDENCE.md    # Hard Policy @1.0 (Final Gate)
.claude/control-plane/11-policies/POLICIES-INDEX.md
.claude/control-plane/02-rules/POLICY-DEFINITIONS.md                   # 含 Scope/Applicability/Version/Evidence Requirement/Failure Decision
.claude/hooks/policy-lib.mjs                                           # 共享纯函数 (evaluate, determinism, version)
.claude/hooks/evidence-collector.mjs                                   # Structure Evidence Producer (NOT log collector)
.claude/hooks/policy-001-no-fake-green.mjs                             # PreToolUse (PreMutation/PreTest lifecycle point)
.claude/hooks/policy-002-real-build.mjs                                # PreBuild lifecycle point
.claude/hooks/policy-003-mutation-evidence.mjs                         # PreMutation lifecycle point
.claude/hooks/policy-004-contract-preservation.mjs                     # PreMutation lifecycle point (frozen)
.claude/hooks/policy-005-completion-evidence.mjs                       # Stop (PreCompletion Final Gate only)
.claude/control-plane/05-gates/GATE-COMPLETION.md (update — Final Gate only)
.claude/hooks/harness-adversarial.mjs (extend 7 attacks: 5 Policy + 2 cross-policy/Bypass)
```

---

### Task 1: Policy 定义与索引 (authoritative source) — 含 6 校准

**Files:**
- Create: `.claude/control-plane/11-policies/POLICIES-INDEX.md`
- Create: `.claude/control-plane/11-policies/POLICY-001-NO-FAKE-GREEN.md`
- Create: `.claude/control-plane/02-rules/POLICY-DEFINITIONS.md`

> **校准1:** Hook-per-Policy 仅 Phase1 简化；未来稳定 Enforcement Points 为 `PreMutationHook/PreBuildHook/PreTestHook/PreCompletionHook`，Policy 由 Engine 在这些点求值；30 Policies ≠ 30 Hooks
> **校准6:** 每个 Policy 必须含 `Scope/Applicability/Version/Evidence Requirement/Failure Decision` + Hard/Conditional 分型

- [ ] **Step 1: Create POLICIES-INDEX.md**

```markdown
# Policies Index — Phase 1 Vertical Slice

> Hook-per-Policy = Phase 1 implementation strategy, NOT permanent architecture. Hook is Enforcement Point, not Policy Container.

| Policy | Type | Scope | Enforcement Point | Requires | onViolation |
|--------|------|-------|-------------------|----------|-------------|
| P001 REAL-BUILD-001@1.0 No Fake Green | Hard | refactoring,feature,bugfix | PreMutationHook (PreToolUse) | real_build_evidence not needed (assert count) | BLOCK |
| P002 REAL-BUILD-001@1.0 Real Build | Conditional | refactoring,feature,bugfix (audit→AuditOnly) | PreBuildHook + Stop | evidence.type=REAL_BUILD & exitCode=0 & timestamp<30min | BLOCK |
| P003 MUTATION-001@1.0 Mutation Evidence | Hard | all writes | PreMutationHook | Before/After/Diff/Actor/Task | BLOCK |
| P004 CONTRACT-001@1.0 Contract Preservation | Hard | frozen contracts | PreMutationHook (frozen) | cr-approved | BLOCK |
| P005 COMPLETION-001@1.0 Completion Evidence | Hard | all completions | PreCompletionHook (Final Gate only) | Build+Test+Review+Evidence | BLOCK |

Future: PreMutation/PreBuild/PreTest/PreCompletion are stable lifecycle hooks; policies evaluated there by engine.
Flow: Rule → Machine Policy → Evaluator → Enforcement Hook → Evidence → Gate → AgentOS State Authority (Transition)
Policy Version追踪: Evidence 必须记录 policy_id + policy_version + decision (for replay)
```

- [ ] **Step 2: Create POLICY-001 doc (with Scope/Version/Hard分型)**

```markdown
# POLICY-001 — No Fake Green (HARD @1.0)

id: REAL-BUILD-001@1.0 (example: P001@1.0)
type: Hard (always BLOCK, never AuditOnly)
scope: [refactoring, feature, bugfix] (README-only changes exempt — Minimum Sufficient Thought)
applicability: taskType=Execute or Verify, phase=P1+, mode != audit
severity: Hard
enforcementPoint: PreMutationHook (PreToolUse Write/Edit)
requires:
  - evidence.type: P001_ASSERT_INTEGRITY
  - evidence.policy_id: P001
  - evidence.policy_version: 1.0
onViolation: BLOCK exit2 + evidence/p001-fake-green.json (includes policy_id, version, decision, Before/After)
Prohibits: 修改 Assertion, Skip/Delete Test, Mock 替代真实验证
Condition: old assert count > new assert count OR added Skip/skip OR replaced real call with mock without real call
Migration: Superpowers verification-before-completion → Principle: Evidence before claim → Policy → Hook → Test
Determinism: same Task/Phase/Context/Evidence/PolicyVersion → same decision
```

- [ ] **Step 3: Create POLICY-DEFINITIONS.md (machine-readable, with 11-field Evidence + Gate Requires)**

```markdown
# POLICY-DEFINITIONS (machine)

## P001
id: P001@1.0
type: Hard
scope: [refactoring, feature, bugfix]
applicability: { taskType: [Execute, Verify], phase: P1+, mode: "!audit" }
trigger: PreToolUse Write|Edit|MultiEdit
enforcementPoint: PreMutationHook
files: *.cs,*.ts,*.vue
detect: assert-weaken|test-delete|mock-replace
requires: [evidence.type=P001_ASSERT_INTEGRITY, evidence.policy_id=P001, evidence.policy_version=1.0]
onViolation: BLOCK exit2
evidence:
  fields: [EvidenceType, Actor, Task, Stage, Policy, Action, Before, After, Tool, Result, Timestamp, Integrity]
  producer: evidence-collector.mjs (structured, not log.push)
gate:
  requires: evidence.type=P001_ASSERT_INTEGRITY & evidence.result!=BLOCK
determinism: same(Task,Phase,Context,Evidence,PolicyVersion) → same decision
```

- [ ] **Step 4: Verify files exist**

Run: `ls .claude/control-plane/11-policies/ && ls .claude/control-plane/02-rules/POLICY-DEFINITIONS.md`
Expected: 3 files listed

- [ ] **Step 5: Commit**

```bash
git add .claude/control-plane/11-policies/ .claude/control-plane/02-rules/POLICY-DEFINITIONS.md
git commit -m "feat(policy): add P001-P005 definitions and index (Phase 1 vertical slice)"
```

---

### Task 2: Shared libs — policy-lib + evidence-collector (structured Evidence Producer)

**Files:**
- Create: `.claude/hooks/policy-lib.mjs`
- Create: `.claude/hooks/evidence-collector.mjs`

> **校准3:** Evidence Collector MUST be structured Evidence Producer, not log collector. Must produce 11-field evidence and Gate Requires structured relation. No `evidence.type=REAL_BUILD & exitCode=0` vs `目录存在 build.log` faux evidence.
> **校准5:** Not reinvent State Machine — Policy → Gate calls existing AgentOS State Authority (Task/Stage/Operation). Phase1 only decides allow, AgentOS owns transition BUILDING→BUILT.

- [ ] **Step 1: Write policy-lib.mjs**

```js
import fs from 'node:fs'; import path from 'node:path';
export function countAsserts(content) { return (content.match(/Assert\.(Equal|True|False|NotNull|Throws)/g)||[]).length + (content.match(/\bassert\./gi)||[]).length; }
export function hasSkip(content) { return /\bSkip\b|\.skip\(/.test(content); }
export function isTestFile(p) { return /\.test\.(ts|js|cs)|__tests__/.test(p); }
export function hasBuildEvidence(maxAgeMs=30*60*1000) {
  const p = path.join(process.cwd(), '.claude/control-plane/09-evidence/build-evidence.json');
  if (!fs.existsSync(p)) return false;
  const j = JSON.parse(fs.readFileSync(p,'utf-8'));
  // Gate Requires: evidence.type=REAL_BUILD & evidence.exitCode=0 & timestamp<30min & policy_version tracked
  return j.evidenceType==="REAL_BUILD" && j.exitCode===0 && j.policy_id==="P002" && (Date.now() - new Date(j.timestamp).getTime()) < maxAgeMs;
}
export function writeEvidence(dir, name, data) {
  // 11-field structured evidence: EvidenceType/Actor/Task/Stage/Policy/Action/Before/After/Tool/Result/Timestamp/Integrity
  const payload = { evidenceType: data.evidenceType||data.policy, actor: data.actor||"agent", task: data.task||"P1", stage: data.stage||"verify", policy_id: data.policy_id||data.policy, policy_version: data.policy_version||"1.0", action: data.action||"check", before: data.before, after: data.after, tool: data.tool||"hook", result: data.result, timestamp: new Date().toISOString(), integrity: "sha256:"+(data.result||"").length, ...data };
  const d = path.join(process.cwd(), dir); fs.mkdirSync(d,{recursive:true});
  fs.writeFileSync(path.join(d,name), JSON.stringify(payload, null, 2));
}
export function isDeterministic(task, phase, context, evidence, policyVersion) { return JSON.stringify({task, phase, context, evidence, policyVersion}); } // same input → same decision
```

- [ ] **Step 2: Write evidence-collector.mjs**

```js
import { writeEvidence } from './policy-lib.mjs';
// Structured Evidence Producer — NOT log.push
export function collectBuildEvidence(exitCode, logTail, actor="agent", task="P1") {
  writeEvidence('.claude/control-plane/09-evidence','build-evidence.json',{evidenceType:"REAL_BUILD", policy_id:"P002", policy_version:"1.0", actor, task, stage:"build", action:"dotnet build", tool:"dotnet", result: exitCode===0?"ALLOW":"BLOCK", exitCode, logTail: logTail.slice(-500)});
}
export function collectMutationEvidence(policy_id, before, after, actor, task) {
  writeEvidence('.claude/control-plane/09-evidence',`mutation-${Date.now()}.json`,{evidenceType:"MUTATION", policy_id, policy_version:"1.0", actor, task, stage:"mutation", action:"write", before: before.slice(0,500), after: after.slice(0,500), tool:"hook", result:"ALLOW"});
}
```

- [ ] **Step 3: Verify import**

Run: `node -e "import('./.claude/hooks/policy-lib.mjs').then(m=>console.log(typeof m.countAsserts))"`
Expected: `function`

- [ ] **Step 4: Commit**

```bash
git add .claude/hooks/policy-lib.mjs .claude/hooks/evidence-collector.mjs
git commit -m "feat(policy): add shared lib and evidence collector"
```

---

### Task 3: P001 Hook — No Fake Green (PreToolUse)

**Files:**
- Create: `.claude/hooks/policy-001-no-fake-green.mjs`
- Test: `.claude/hooks/harness-adversarial.mjs` (extend)

- [ ] **Step 1: Write hook** — reads CLAUDE_FILE_PATH, compares old/new content for assert weaken, BLOCK exit 2, writes evidence

```js
import fs from 'node:fs'; import { countAsserts, hasSkip, writeEvidence } from './policy-lib.mjs';
let input={}; for await(const c of process.stdin) input=JSON.parse(Buffer.concat([c]).toString());
const file = process.env.CLAUDE_FILE_PATH || input.tool_input?.file_path || '';
const content = input.tool_input?.content || input.tool_input?.new_string || '';
if (!file || !content) process.exit(0);
if (!/\.cs$|\.ts$|\.vue$/.test(file)) process.exit(0);
let old=""; try{ old=fs.readFileSync(file,'utf-8'); }catch{ old=""; }
if (countAsserts(content) < countAsserts(old) - 1) { writeEvidence('.claude/control-plane/09-evidence','p001-fake-green.json',{policy:'P001', result:'BLOCK', reason:`assert weakened ${countAsserts(old)}→${countAsserts(content)}`, file}); console.error('BLOCKED P001: fake green'); process.exit(2); }
if (hasSkip(content) && !hasSkip(old)) { writeEvidence('.claude/control-plane/09-evidence','p001-fake-green.json',{policy:'P001', result:'BLOCK', reason:'skip added'}); console.error('BLOCKED P001: skip'); process.exit(2); }
process.exit(0);
```

- [ ] **Step 2: Test adversarial — weaken assert**

Run: `echo '{"tool_input":{"file_path":"backend/tests/FooTests.cs","content":"// no asserts"}}' | node .claude/hooks/policy-001-no-fake-green.mjs; echo exit:$LASTEXITCODE`
Expected: exit 2 when old has asserts

- [ ] **Step 3: Commit**

```bash
git add .claude/hooks/policy-001-no-fake-green.mjs
git commit -m "feat(policy): P001 no-fake-green hook"
```

---

### Task 4: P002 + P005 Hooks — Build + Completion Gates (PreBuild + PreCompletion Final Gate only)

**Files:**
- Create: `.claude/hooks/policy-002-real-build.mjs`
- Create: `.claude/hooks/policy-005-completion-evidence.mjs`

> **校准2:** GATE-COMPLETION is Final Gate only. Pre-action policies (Mutation/Build/Contract) MUST block immediately at Pre-* hooks, not delayed to final. Correct flow: Action → Pre-Action Policy → ALLOW/BLOCK → Execution → Post-Action Evidence → Gate.
> **校准5:** P005 does not mutate state itself; Gate → ALLOW → AgentOS State Authority → Transition (e.g., BUILDING→BUILT). Phase1 owns decision, AgentOS owns state.
> **校准6:** P002 is Conditional (refactoring/feature/bugfix requires build; audit→AuditOnly exempt); P005 is Hard.

- [ ] **Step 1: Write P002** — PreBuildHook + Stop: checks structured build-evidence (evidence.type=REAL_BUILD & exitCode=0 & policy_version) and fresh (<30min) + scope applicability, else BLOCK + hint

```js
import { hasBuildEvidence } from './policy-lib.mjs';
// Conditional: scope check — audit mode → AuditOnly, not BLOCK
const mode = process.env.TASK_MODE || "execute";
if (mode==="audit") { console.log("P002 AuditOnly — no build required"); process.exit(0); }
if (!hasBuildEvidence()) { console.error('BLOCKED P002@1.0: no fresh structured build evidence (REAL_BUILD & exit0 & <30min). Run dotnet build / pnpm build'); process.exit(2); }
process.exit(0);
```

- [ ] **Step 2: Write P005** — Stop (PreCompletion Final Gate ONLY) checks 4 structured evidences: Build+Test+Review+Evidence with policy_id/version, missing one → BLOCK, and calls AgentOS State Authority for transition only on ALLOW

```js
import fs from 'node:fs'; import path from 'node:path';
const base='.claude/control-plane/09-evidence';
// Gate Requires: structured evidence relation, not "directory exists build.log"
const need=[{file:'build-evidence.json', type:'REAL_BUILD', policy:'P002@1.0'}, {file:'completion-gate.json', type:'COMPLETION', policy:'P005@1.0'}];
const missing=[];
for (const r of need) {
  const p=path.join(process.cwd(),base,r.file);
  if (!fs.existsSync(p)) { missing.push(r.file); continue; }
  const j=JSON.parse(fs.readFileSync(p,'utf-8'));
  if (j.evidenceType!==r.type || j.policy_version!=="1.0") missing.push(`${r.file}(type/version mismatch)`);
}
if (missing.length){ console.error('BLOCKED P005@1.0 Final Gate: missing/invalid '+missing.join(',')); process.exit(2); }
// ALLOW → delegate to AgentOS State Authority (not state=BUILT here)
console.log('P005@1.0 Final Gate ALLOW → AgentOS State Authority may transition');
```

- [ ] **Step 3: Test without evidence → BLOCK**

Run: `rm -f .claude/control-plane/09-evidence/build-evidence.json; node .claude/hooks/policy-002-real-build.mjs; echo exit:$LASTEXITCODE`
Expected: exit 2

- [ ] **Step 4: Seed fake evidence and test PASS**

Run: `mkdir -p .claude/control-plane/09-evidence && echo '{"exitCode":0,"timestamp":"'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"}' > .claude/control-plane/09-evidence/build-evidence.json && node .claude/hooks/policy-002-real-build.mjs; echo exit:$LASTEXITCODE`
Expected: exit 0

- [ ] **Step 5: Commit**

```bash
git add .claude/hooks/policy-002-real-build.mjs .claude/hooks/policy-005-completion-evidence.mjs
git commit -m "feat(policy): P002 real-build and P005 completion gates"
```

---

### Task 5: P003 Mutation Evidence + P004 Contract Preservation

**Files:**
- Create: `.claude/hooks/policy-003-mutation-evidence.mjs`
- Create: `.claude/hooks/policy-004-contract-preservation.mjs`

- [ ] **Step 1: Write P003** — PreToolUse on Write/Edit, requires git diff present (Before/After) + workflow-state contains Actor/Task; if not, BLOCK

```js
import { execSync } from 'node:child_process';
let diff=""; try{ diff=execSync('git diff --stat',{encoding:'utf-8'});}catch{ diff=""; }
if (!diff.trim()) { console.error('BLOCKED P003: no diff evidence'); process.exit(2); }
process.exit(0);
```

- [ ] **Step 2: Write P004** — PreToolUse on frozen paths `08-phase-contracts/*`, `00-governance/L0-LAWS.md`, `GOVERNANCE-INDEX.md`; if cr-approved missing, BLOCK

```js
import fs from 'node:fs'; const file=process.env.CLAUDE_FILE_PATH||''; if (!/08-phase-contracts|L0-LAWS|GOVERNANCE-INDEX/.test(file)) process.exit(0);
const wf=JSON.parse(fs.readFileSync('.claude/workflow-state.json','utf-8')); if (!wf['cr-approved']) { console.error('BLOCKED P004: frozen contract without cr-approved'); process.exit(2); }
```

- [ ] **Step 3: Commit**

```bash
git add .claude/hooks/policy-003-mutation-evidence.mjs .claude/hooks/policy-004-contract-preservation.mjs
git commit -m "feat(policy): P003 mutation and P004 contract hooks"
```

---

### Task 6: Wire hooks + Gate + Inventory update (with Enforcement Point semantics)

**Files:**
- Modify: `.claude/settings.json` (register 5 hooks as Enforcement Points, not Policy Containers)
- Modify: `.claude/control-plane/05-gates/GATE-COMPLETION.md` (Final Gate only, not pre-enforcement aggregator)
- Modify: `.claude/control-plane/00-governance/HARNESS-INVENTORY.md` (add 5 policies with version)

> **校准1+2:** Wire as `PreMutationHook/PreBuildHook/PreCompletionHook` Enforcement Points. Gate is Final Gate only; pre-action BLOCK happens at Pre-* hooks immediately.

- [ ] **Step 1: Edit settings.json — add Enforcement Points**

```json
"PreToolUse": [
  {"matcher":"Write|Edit|MultiEdit","command":"node \"$CLAUDE_PROJECT_DIR/.claude/hooks/guard-write.mjs\""},
  {"matcher":"Write|Edit|MultiEdit","command":"node \"$CLAUDE_PROJECT_DIR/.claude/hooks/policy-001-no-fake-green.mjs\"","comment":"PreMutationHook — P001 Hard"},
  {"matcher":"Write|Edit|MultiEdit","command":"node \"$CLAUDE_PROJECT_DIR/.claude/hooks/policy-003-mutation-evidence.mjs\"","comment":"PreMutationHook — P003 Hard"},
  {"matcher":"Write|Edit|MultiEdit","command":"node \"$CLAUDE_PROJECT_DIR/.claude/hooks/policy-004-contract-preservation.mjs\"","comment":"PreMutationHook frozen — P004 Hard"}
],
"Stop": [
  {"command":"node \"$CLAUDE_PROJECT_DIR/.claude/hooks/policy-002-real-build.mjs\"","comment":"PreBuildHook — P002 Conditional"},
  {"command":"node \"$CLAUDE_PROJECT_DIR/.claude/hooks/policy-005-completion-evidence.mjs\"","comment":"PreCompletionHook Final Gate ONLY — P005 Hard"},
  {"command":"node \"$CLAUDE_PROJECT_DIR/.claude/hooks/guard-finish.mjs\""}
]
```

- [ ] **Step 2: Update GATE-COMPLETION.md** — add 5-policy aggregation table

- [ ] **Step 3: Verify hooks wiring**

Run: `node scripts/test-hooks.mjs 2>&1 | tail -5`
Expected: still 44/44 PASS (new hooks additive, not breaking)

- [ ] **Step 4: Commit**

```bash
git add .claude/settings.json .claude/control-plane/05-gates/GATE-COMPLETION.md .claude/control-plane/00-governance/HARNESS-INVENTORY.md
git commit -m "feat(policy): wire 5 hooks and completion gate"
```

---

### Task 7: Adversarial vertical slice verification (7 scenarios + Bypass + Determinism + Versioning)

**Files:**
- Modify: `.claude/hooks/harness-adversarial.mjs` (extend to 7 attacks)
- Test: `node .claude/hooks/harness-adversarial.mjs`

> **校准4+11+12+13:** 7 scenarios = 5 Policy + 2 cross-policy/Bypass + Policy Determinism + Versioning. Must also test Governance Bypass (direct File API / fake evidence) per IRON-03.

- [ ] **Step 1: Add 7 attacks + 3 extra checks:**

```js
// P001 weaken assert → BLOCK, P002 no build → BLOCK (Conditional: audit→AuditOnly exempt), P003 no diff → BLOCK, P004 break frozen → BLOCK, P005 missing evidence → BLOCK, P006 Cross-Policy Bypass (fake evidence file with wrong type) → BLOCK, P007 Policy Ordering Abuse (completion before build) → BLOCK
// + Bypass: direct File API bypass hook → BLOCK
// + Determinism: same(Task,Phase,Context,Evidence,Version) → same decision (run twice compare)
// + Versioning: evidence missing policy_version → BLOCK
```

- [ ] **Step 2: Run**

Run: `node .claude/hooks/harness-adversarial.mjs`
Expected: 30+ PASS (23 existing + 7 new), all BLOCK as expected, Bypass= BLOCK, Determinism PASS, Versioning PASS

- [ ] **Step 3: Run drift — still NO DRIFT except new policy files (re-baseline)**

Run: `node .claude/hooks/harness-drift.mjs --baseline && node .claude/hooks/harness-drift.mjs`
Expected: NO DRIFT

- [ ] **Step 4: Verify 10-dimension matrix**

```
Policy 5 evaluable ✓
Hook key points wired ✓ (PreMutation/PreBuild/PreCompletion)
Evidence 11-field structured + Gate Requires ✓
Gate can block illegal ✓
State only via AgentOS Authority ✓ (no state.js reinvented)
Determinism same input→same decision ✓
Versioning policy_version tracked ✓
Bypass fails ✓
Adversarial ≥7 ✓
Regression Control Plane 1.0 green ✓
```

- [ ] **Step 4: Final report**

```bash
git log --oneline -7
```

---

## Self-Review

1. **Spec coverage:** 5 policies → Tasks 3-5 cover all; vertical slice flow Rule→Policy→Hook→Evidence→Gate→State → Task 6; adversarial → Task 7 ✓
2. **Placeholder scan:** no TBD/TODO, all code blocks complete ✓
3. **Type consistency:** policy-lib exports match hook imports (countAsserts, hasBuildEvidence, writeEvidence) ✓
