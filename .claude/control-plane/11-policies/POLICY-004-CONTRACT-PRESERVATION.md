# POLICY-004 — Contract Preservation (HARD @1.0)

**id:** `P004@1.0`
**type:** Hard — always BLOCK
**scope:** frozen contracts: `08-phase-contracts/*`, `00-governance/L0-LAWS.md`, `00-governance/GOVERNANCE-INDEX.md`, `00-governance/MASTER-GOVERNANCE.md`, `HUMAN-GATE-RULES.yaml`
**applicability:** any Write/Edit on frozen path without `cr-approved`
**enforcementPoint:** PreMutationHook (frozen path)
**requires:** `workflow-state.json` contains `cr-approved` or `crApproved` (Change Request approval)
**onViolation:** BLOCK exit 2 + `contract-guard.json` evidence

## Rule

Frozen Contract被破坏 बिना CR → BLOCK.

## Evidence

`EvidenceType=CONTRACT_GUARD, Policy=P004@1.0, file, crApproved, Result=BLOCK/ALLOW, Timestamp, Integrity`

## Gate Relation

Gate `CONTRACT-PRESERVATION` requires `cr-approved` present for any frozen mutation.
