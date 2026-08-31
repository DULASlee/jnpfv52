# POLICY-003 — Mutation Must Be Evidenced (HARD @1.0)

**id:** `P003@1.0`
**type:** Hard
**scope:** all writes (except `.gitignore`, `09-evidence/**` itself, `.claude/memory/**` transient)
**applicability:** always (any Write/Edit/MultiEdit)
**enforcementPoint:** PreMutationHook (PreToolUse Write/Edit/MultiEdit)
**requires:** Before/After/Diff/Actor/Task — 5-tuple via `git diff --stat` + `workflow-state.json` (Task/Actor)
**onViolation:** BLOCK exit 2

## Rule

Every file mutation must produce 5-tuple evidence; no diff or missing Actor/Task → BLOCK.

## Evidence (11-field)

`EvidenceType=MUTATION, Actor, Task, Stage=mutation, Policy=P003, Action=write, Before (old hash/content head), After (new hash/head), Tool=hook, Result=ALLOW/BLOCK, Timestamp, Integrity, policy_id=P003, policy_version=1.0, diffStat`

## Note

Not log collector — structured producer with integrity hash for replay.
