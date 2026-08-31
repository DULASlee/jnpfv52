# Deferred Verification Register — Phase 1 WARN → Future Phases

> **Principle:** Deferred ≠ Forgotten. Each WARN is traceable with TargetPhase and ClosureGate.
> **Source:** Phase 1 Independent Review #2 9 WARN (non-blocking for Final Gate)

| WarningId | OriginalFinding | Risk | CurrentStatus | WhyNonBlocking | Owner | TargetPhase | RequiredProof | VerificationMethod | ClosureGate |
|-----------|-----------------|------|---------------|----------------|-------|-------------|---------------|--------------------|-------------|
| WARN-001 | P001 semantic fake-green beyond count (`Assert.True(true)`) | High: structural check passes but semantic true→true bypasses | Baseline Guard active (count-1) | Phase 1 scope is structural baseline; semantic requires AST/mutation testing | AgentOS Verification | Phase 4 Intelligent Verification | AST arg hash + mutation kill report, 10 semantic attacks BLOCK | Black-box semantic attacks (Assert arg tamper) | `PHASE4-INTELLIGENT-VERIFICATION` PASS |
| WARN-002 | P002 target binding (solution/project/commit not bound) | Medium: `dotnet build trivial` fakes REAL_BUILD | Conditional Hard with type/exitCode/30min | Requires build system integration, not Phase 1 vertical slice | AgentOS Build | Phase 2 AgentOS Runtime | `build-evidence.json` contains `solution, commit, cwd, artifactsHash` and gate verifies `solution==expected` | Black-box trivial project attack → BLOCK, real project → ALLOW | `PHASE2-BUILD-BINDING` PASS |
| WARN-003 | Evidence crypto attestation (integrity 64-bit truncated, not validated, exitCode not in canonical) | Medium: tamper exitCode 1→0 not detected | 11-field exists, not enforced | Full crypto is Phase 4 hardening, Phase 1 proves structure | Control Plane | Phase 4 | `integrity = sha256(policy+version+result+exitCode+timestamp+logHash) full 64-char validated on read → EVIDENCE_CORRUPTED BLOCK` | Tamper `exitCode` → BLOCK, `integrity` mismatch → BLOCK | `PHASE4-EVIDENCE-CRYPTO` PASS |
| WARN-004 | Determinism harden (isDeterministicKey unused, timestamp/fs-order flake, only 2× exit check) | Medium: same input could diverge via timestamp or git order | Determinism via exit equality proven for P001 2× | N=10 pure function harden is Phase 2 | AgentOS | Phase 2 | `isDeterministicKey(task,phase,scope,evidenceHash,policyVersion)` pure, sorted git, mocked timestamp, N=10 assert Decision/FailureCode/RequiredEvidence equal | Determinism harness N=10 | `PHASE2-DETERMINISM` PASS |
| WARN-005 | Version hash-pin (literal 1.0, not file-hash bound, replay stale) | Medium: policy logic change without bump replays old evidence | Version 1.0 string in 6 places, mismatch check exists (`P005:30`) | Hash-pin is Phase 4 evolution, Phase 1 proves version field exists | Control Plane | Phase 4 | `policy_version = sha256(policyFile)[:8] + semver`, integrity covers policyHash, replay stale → BLOCK | Version-replay adversarial (mutate policy without bump → BLOCK) | `PHASE4-VERSION-HASH` PASS |
| WARN-006 | PreTest (H-TEST) unwired | Medium: P001 only at PreMutation, not PreTest | H-TEST has zero coverage; P001 via PreMutation still blocks | Phase 1 has no independent PreTest policy (P001 is mutation guard) | Control Plane | Phase 2 | Wire `policy-006-test-evidence.mjs` at `PostToolUse Bash(dotnet test)` → H-TEST, P005 requires test-evidence | `H-TEST` mapping table + test evidence adversarial | `PHASE2-H-TEST-WIRED` PASS |
| WARN-007 | PreBuild only at Stop (H-BUILD) | Medium: `PostToolUse Bash(dotnet build)` not captured, Stop-only can be bypassed in long session | Stop gate still blocks at completion | Immediate PreBuild capture is Phase 2 hardening | Control Plane | Phase 2 | Wire P002 also at `PostToolUse Bash(build)` to collect evidence immediately | `dotnet build` then `Stop` without evidence → BLOCK | `PHASE2-H-BUILD-WIRED` PASS |
| WARN-008 | Positive/Negative/Boundary triple gaps (P001/P003 no systematic positive) | Medium: gate could be always-BLOCK false positive | 17 policy adversarial has positives for P002/P004/P005, partial for P001/P003 | Full triple is Phase 2 audit quality, Phase 1 proves core BLOCK | QA | Phase 2 | Per policy: Positive ALLOW + Negative BLOCK + Boundary TTL/-1 edge, assert structured fields | Policy Test Matrix per P00X | `PHASE2-POLICY-MATRIX` PASS |
| WARN-009 | Capability/Precedence engine (conflict/precedence not general) | Low: 5 policies independent, no conflict | Vertical slice proves 5 independent evaluable | Full engine is Phase 2+ | Control Plane | Phase 3+ | `Policy Resolution + Conflict + Scope + Version + Precedence` general proof | Conflict scenario: same file two policies → precedence → deterministic BLOCK | `PHASE3-POLICY-ENGINE` PASS |

## Semantic Fake-Green & Target Binding Future Verification

- **semantic fake-green** → WARN-001 → Owner `Intelligent Verification` → `Phase 4` → Proof: semantic attacks (arg tamper) BLOCK
- **target binding** → WARN-002 → Owner `AgentOS Runtime` → `Phase 2` → Proof: trivial project BLOCK, real ALLOW

Crypto, determinism, version → similarly traced. No `Deferred → Forgotten`.

## Evidence

- Phase 1 black-box 54 PASS proves baseline enforcement; deferred items are explicitly not required for Phase 1 Final Gate per Chief §Non-Blocking
- Each WARN has `RequiredProof` with adversarial method and `ClosureGate` for future phase
