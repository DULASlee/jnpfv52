# Phase 10 — Negative / Ambiguity / CrossProject / Determinism / Rebind Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove the FSPM Compiler never guesses on Unknown / Ambiguous / Invalid. Phase 10 adds the regression suite that locks the Phase 7 + 8 contracts.

**Architecture:** Pure-test phase. No production code changes unless an actual gate fails (hard rule: STOP / REPORT / WAIT). All tests ride on the Phase 6 Workspace collection to serialize MSBuild's single design-time build.

**Tech Stack:** xUnit 2.6, Roslyn 4.8, existing FSPM fixtures under `backend/tests/Fixtures/`.

---

## Hard Constraints (re-stated from chief architect)

1. NEVER GUESS. Zero `First()` / `Last()` / `name-only` resolution paths.
2. NEVER mutate Phase 7 Identity contract. If a real collision appears, STOP.
3. NEVER touch MCP worktree, MCP branch, or any stdio / `fspm_understand` / `fspm_construct` / `fspm_verify` surface.
4. All tests must use real Roslyn `Compilation` + real `ISymbol`; zero mocks.
5. Phase 11 (real JNPF) and Phase 12 (18-Gate) are NOT in scope.

---

## Task 0: Pre-flight — confirm Phase 9 build/test are still green

**Files:** none modified.

- [ ] **Step 1:** `dotnet build backend/modularity/Foundry.FSPM.Compiler/Foundry.FSPM.Compiler.csproj -c Release`
  Expected: 0 warnings / 0 errors.
- [ ] **Step 2:** `dotnet test backend/tests/Foundry.FSPM.Compiler.Tests/Foundry.FSPM.Compiler.Tests.csproj -c Release`
  Expected: 135 / 0 / 0 baseline.

---

## Task 1: Probe fixture gaps for G10

Audit existing `backend/tests/Fixtures/SemanticGolden` + `SemanticGolden.Contracts` against the 14 gates. Document any missing shape (e.g. does `BaseUser` already cover shadowing? does Contracts cover the missing-ProjectReference case?).

**Decision tree (from chief architect):**
- If a fixture is **structurally already present** (it is, per the existing Phase 7/8 work) → do not duplicate. Reuse via new test cases.
- If a fixture is missing → add only the minimal new type. All new types land in the existing `SemanticGolden` or `SemanticGolden.Contracts` project so they participate in the same `MSBuildWorkspace` load.

Run: `ls backend/tests/Fixtures/SemanticGolden/Domain`
Expected: User.cs, OtherUser.cs, BaseUser.cs, DerivedUser.cs, ShadowedUser.cs, Session.cs all present.

---

## Task 2: G10-1 / G10-2 / G10-3 — Unknown Entity / Property / Operation

**Files:**
- Create: `backend/tests/Foundry.FSPM.Compiler.Tests/NegativeUnknownTests.cs`
- Test class decorated `[Collection("RoslynWorkspace")]`

**FSPM source patterns to feed the SemanticBuilder:**
- `entity DoesNotExist` → entity Status=Unknown, Symbol=null, FSPM101 in diagnostics.
- `entity SemanticGolden.Domain.User` + `property SemanticGolden.Domain.User.DoesNotExist` → entity Success, property Status=Unknown, Symbol=null, FSPM102 in diagnostics.
- `entity SemanticGolden.Domain.User` + `operation SemanticGolden.Domain.User.DoesNotExist` → operation Status=Unknown, Symbol=null, FSPM103 in diagnostics.

**Asserts per case:**
- `model.HasErrors` is true.
- The model's `FindEntity` / `FindProperty` / `FindOperation` for the failing element returns non-null (it stays in the model with diagnostics), but `Symbol` is null and `IsResolved` is false.
- The diagnostic code is exactly the one the gate requires (101/102/103).

---

## Task 3: G10-4 — Ambiguous Entity (no owner chosen)

**Files:** same test file.

**Source:** `entity User` (short name). Bind against the existing `SemanticGolden` fixture (Domain + NamespaceA + NamespaceB + Contracts collides).

**Asserts:**
- Status == Ambiguous.
- Symbol == null.
- Diagnostic code == FSPM111.
- The message lists at least the four colliding types by display name (`SemanticGolden.Domain.User`, `SemanticGolden.NamespaceA.User`, `SemanticGolden.NamespaceB.User`, `SemanticGolden.Contracts.User`).

---

## Task 4: G10-5 — Ambiguous Property (owner-first preserved)

**Files:** same test file.

**Source:** `entity User` + `property User.PhoneNumber`.

**Asserts:**
- Entity: Ambiguous (G10-4 already pinned).
- Property: Invalid, owner diagnostic propagated (FSPM111 carried through), Symbol == null, **never** silently resolved to a sibling `User` (no `BaseUser`/`OtherUser` binding allowed).

Additionally verify a **property-level** ambiguity directly using `ShadowedUser.Name` (already in fixture): `entity ShadowedUser` + `property ShadowedUser.Name` → property Ambiguous (FSPM112).

---

## Task 5: G10-6 / G10-7 — Operation Overload Ambiguity

**Files:** same test file.

**Source:** `entity SemanticGolden.Domain.User` (using the parser's FQN-free grammar this requires owner pre-resolution through `EntityBinder`; the test file must use a path that exercises the binder's existing `FindCandidates` with FQN, which is supported inside the binder even though the FSPM v1 grammar does not allow FQN entity declarations). The simplest path: use the property / operation forms whose `EntityName` is a fully-qualified C# name (the binder accepts FQN in the owner position).

**Asserts (operation `SemanticGolden.Domain.User.Create`):**
- Operation Status == Ambiguous.
- Symbol == null.
- Diagnostic code == FSPM113.
- Message lists BOTH overloads' parameter type names (`string phoneNumber`, `int legacyId`) so the user can see the unresolvable surface.
- FSPM grammar's "no parameter syntax" rule is restated in the diagnostic message (the existing message text already does this — assert it contains "FSPM v1 has no parameter syntax").

---

## Task 6: G10-8 — Cross Project

**Files:** same test file.

Use the **already-present** `SemanticGolden.Contracts` project (referenced from `SemanticGolden.csproj`).

- **Correct project:** `property SemanticGolden.Contracts.User.PhoneNumber` → Success, Symbol is the Contracts.PhoneNumber property, SymbolId carries `SemanticGolden.Contracts|…`.
- **Missing project reference:** Build a second tiny project `SemanticGolden.NotReferenced` containing `User` and confirm `entity User` against a Compilation that DOES NOT reference it (use `workspace.Projects.First(p => p.AssemblyName == "SemanticGolden")` only). The binder should still return `SemanticGolden.Domain.User` (resolved from the referenced assemblies) and the new NotReferenced type stays invisible. If the binder accidentally returned multiple `User`s, Ambiguous fires (already covered by G10-4).
- **No fallback to name-only:** assert SymbolId for the resolved `User` always carries the `SemanticGolden|…` prefix and never the synthetic prefix.

Additionally add a NotReferenced project fixture under `backend/tests/Fixtures/SemanticGolden.NotReferenced/` containing only `User.cs`. This is the **only** new fixture file Phase 10 may introduce. Add the new project to the test-only `Phase10Fixture` list — **do not** add a `<ProjectReference>` from `SemanticGolden.csproj`. (Tests that need isolation load it through a dedicated path or via the binding behaviour under missing-reference.)

---

## Task 7: G10-9 — Cross Assembly Collision

**Files:** same test file.

The fixture currently contains `SemanticGolden` and `SemanticGolden.Contracts` (two different assembly names). Verify:
- `property SemanticGolden.Domain.User.PhoneNumber` resolves to `SemanticGolden|…` (NOT `SemanticGolden.Contracts|…`).
- `property SemanticGolden.Contracts.User.PhoneNumber` resolves to `SemanticGolden.Contracts|…` (NOT `SemanticGolden|…`).
- The two SymbolIds are unequal AND the `AssemblyName` segment differs.

If the test fails (i.e. Phase 7 Identity actually collides in this scenario) → STOP and report. Do not "fix" the Identity.

---

## Task 8: G10-10 — Rebind

**Files:** same test file.

Procedure (per chief architect, no real incremental compilation):
1. `Load #1` via `GoldenIdentity.LoadGoldenAsync()`.
2. `Build Model #1` for the canonical source `entity OtherUser; property OtherUser.PhoneNumber; entity Session; operation Session.Ping`.
3. Dispose `Load #1`'s workspace.
4. `Load #2` via a second call.
5. `Build Model #2` from the same source.
6. Compare semantic content (SymbolId, diagnostic codes, entity/property/operation counts). NO `ReferenceEquals`. NO object-identity comparisons.

**Asserts:**
- Model #1.Entities.SymbolId sequence equals Model #2.Entities.SymbolId sequence (order-preserving).
- Same for Properties and Operations.
- Diagnostics set (code, line, column, severity) equal between the two builds (deterministic ordering).

---

## Task 9: G10-11 — Diagnostic Determinism

**Files:** same test file.

Feed the same bad source twice and verify the diagnostic lists compare element-wise:
- `entity User; property User.PhoneNumber; operation User.Create` against the fixture (entity ambiguous, property invalid, operation ambiguous) — diagnostic counts and codes must be identical across two builds and the order must be deterministic (lexicographic by `(Line, Column, Code)` is acceptable as a normalization step inside the assertion; otherwise the natural Lex-then-Bind order is already stable and we just compare `SequenceEqual` on the list).

---

## Task 10: G10-12 — Semantic Model Determinism (full structural compare)

**Files:** same test file.

Compare Model #1 vs Model #2 (from Task 8) using a custom structural equality:
- Entities: compare `SymbolId` (struct value equality is already there on `FspmSymbolId`), `Status`, `Name`, `QualifiedName`.
- Properties: same plus `TypeName`, `Owner?.SymbolId`.
- Operations: same plus `ReturnType`, `ParameterTypes`, `IsStatic`.
- Diagnostics: list equality on `(Code, Severity, Message, Line, Column)`.

Use a helper `static bool StructurallyEqual(FspmSemanticModel a, FspmSemanticModel b)` colocated in the test file (or a small static class).

---

## Task 11: G10-13 — Synthetic ID Isolation

**Files:** same test file.

Three declarations with the SAME short name in the same source file:
```
entity User
property User.PhoneNumber
operation User.Nope
```

Verify:
- Entity.SymbolId.Value starts with `"synthetic/Ambiguous/Entity/User@…"` (since bare `User` is ambiguous).
- Property.SymbolId.Value starts with `"synthetic/Invalid/Property/User.PhoneNumber@…"`.
- Operation.SymbolId.Value starts with `"synthetic/Invalid/Operation/User.Nope@…"`.
- All three SymbolIds are distinct.
- None of the three collide with any real `Assembly|DocId` shape — assert by prefix.
- A new run on the same source produces the same three ids in the same order (deterministic).

---

## Task 12: G10-14 — Negative E2E

**Files:** same test file.

One big test that walks the full pipeline for THREE negative source variants in sequence:
- Source A: `entity NoSuchType` (Unknown Entity path)
- Source B: `entity User; property User.DoesNotExist` (Ambiguous Entity + Invalid Property path)
- Source C: `entity SemanticGolden.Domain.User; operation SemanticGolden.Domain.User.NoSuchMethod` (Success Entity + Unknown Operation path)

**Per variant assert:**
- No exception escapes the builder.
- `model.HasErrors == true` (where applicable; Source A alone has errors).
- `model.Entities` / `Properties` / `Operations` keep their declarations visible (no silent filtering of failing items).
- Each failing item's `Symbol == null` and `Status ∈ {Unknown, Ambiguous, Invalid}`.
- Each failing item's `Binding.Diagnostics` is non-empty with the expected code.
- The model can still be queried via `FindX(SymbolId)` for every element (including failing ones — the synthetic id is a real key).
- No `model.Entities.Any(e => e.IsResolved && e.SymbolId.Value.StartsWith("synthetic/"))` — i.e. resolved and synthetic are mutually exclusive.

---

## Task 13: Phase 10 Final Gate

- [ ] `dotnet build backend/modularity/Foundry.FSPM.Compiler/Foundry.FSPM.Compiler.csproj -c Release` → 0/0
- [ ] `dotnet test backend/tests/Foundry.FSPM.Compiler.Tests/Foundry.FSPM.Compiler.Tests.csproj -c Release` → 0 failures
- [ ] `git status --short` clean (except the regenerated mtime on `openspec/specs/README.md`, which we `git checkout -- …` at the end).
- [ ] `git commit -m "test(fspm-compiler): Phase 10 negative / ambiguity / determinism regression suite"`
- [ ] `git push origin fspm-compiler` → branch moves forward, no MCP touch.
- [ ] Final REPLY to user: STOP / REPORT / WAIT, summarize gate evidence, surface the 3 fixture additions (NotReferenced project + new test file), explicitly state **"Phase 10 done; Phase 11 not entered; Phase 7 Identity contract unmodified"**.

---

## Self-Review (writing-plans)

1. **Spec coverage:** every G10-1..14 has a task. ✓
2. **Placeholder scan:** all steps have concrete asserts and file paths; no TBD. ✓
3. **Type consistency:** `FspmEntity` / `FspmProperty` / `FspmOperation` / `FspmSymbolId` / `FspmBindingStatus` already exist; no renames. ✓
4. **Forbidden-action check:** no MCP edits, no Identity contract edits, no Phase 11/12 tasks. ✓
