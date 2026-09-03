# FSPM MCP × FSPM Compiler — Interface Lockdown

> **For agentic workers:** This is the binding contract between the two
> parallel sub-projects in `D:\JNPF-v52`. Violating it blocks the other
> side's build. Read this before touching anything under
> `backend/modularity/Foundry.FSPM.*` or `backend/tools/Foundry.FSPM.*`.

**Date:** 2026-09-03
**Status:** FROZEN (brainstorming-approved; user-selected Option X)
**Owners:**
- **MCP side:** Forge (sub-task: Foundry.FSPM.Mcp stdio adapter)
- **Compiler side:** (parallel AI engineer; sub-task: Foundry.FSPM.Compiler
  + FSPM-04 → FSPM-18 in `docs/FSPM/EXECUTION_ROADMAP.md`)

---

## 0. Why this document exists

Both sub-tasks touch the same `Foundry.FSPM.Core` and
`Foundry.FSPM.Analyzer` namespaces. Without a lockdown, the MCP side
will reference an API the Compiler side later renames, and one side
will block the other's build.

This document freezes the **public API surface** that the Compiler must
not break and the MCP must not expand.

---

## 1. FROZEN — public API surface (Compiler MUST NOT break)

These are the symbols that **Foundry.FSPM.Mcp** depends on via
`ProjectReference`. Their **signatures, namespaces, and types are
frozen** until further notice. Compiler-side refactors must remain
**internally backward-compatible** with respect to this surface.

### 1.1 Foundry.FSPM.Core / Evidence

```csharp
namespace Foundry.FSPM.Core.Evidence;

// FROZEN
public interface IFspmEvidenceCollector
{
    void Add(FspmVerificationEvidence evidence);
    IReadOnlyList<FspmVerificationEvidence> GetAll();
    IReadOnlyList<FspmVerificationEvidence> GetByRule(string ruleId);
    void Complete();
}

// FROZEN
public sealed record FspmVerificationEvidence
{
    public required string SchemaVersion { get; init; }
    public required string EvidenceKind { get; init; }
    public required string RuleId { get; init; }
    public required Foundry.FSPM.Core.Rules.RuleEvaluationState Decision { get; init; }
    public required string RelationKind { get; init; }
    public required Foundry.FSPM.Core.Semantic.SemanticIdentity? Source { get; init; }
    public Foundry.FSPM.Core.Semantic.SemanticIdentity? Subject { get; init; }
    public Foundry.FSPM.Core.Semantic.SemanticIdentity? Target { get; init; }
    public required Foundry.FSPM.Core.Semantic.SourceLocation Location { get; init; }
    public required Foundry.FSPM.Core.Semantic.SemanticResolutionStatus ResolutionStatus { get; init; }
    public string? Reason { get; init; }
    public string? AnalyzerVersion { get; init; }
    public string? CompilationFingerprint { get; init; }
}

// FROZEN — SHA256 hex fingerprint API
public static class EvidenceId
{
    public static string Compute(FspmVerificationEvidence evidence);
}

// FROZEN — Diagnostics property keys (8 keys)
public static class FspmDiagnosticProperties
{
    public const string EvidenceId = "Fspm.EvidenceId";
    public const string RuleId = "Fspm.RuleId";
    public const string Dimension = "Fspm.Dimension";
    public const string SemanticLevel = "Fspm.SemanticLevel";
    public const string Subject = "Fspm.Subject";
    public const string Target = "Fspm.Target";
    public const string RelationKind = "Fspm.RelationKind";
    public const string Decision = "Fspm.Decision";
}
```

### 1.2 Foundry.FSPM.Analyzer / RuleIds

```csharp
namespace Foundry.FSPM.Analyzer;

// FROZEN — these constants are referenced by Foundry.FSPM.Mcp
public static class FspmRuleIds
{
    public const string Architecture001 = "ARCH001";
    public const string Security001 = "SEC001";
    public const string Ui001 = "UI001";
}
```

### 1.3 Foundry.FSPM.Core / Semantic (read-only types)

These are value types the MCP side consumes. Their **fields are read**;
the MCP side never mutates them. Compiler may rename or restructure
*implementation*, but the **field set below must remain** for the
duration of the lockdown:

```csharp
namespace Foundry.FSPM.Core.Semantic;

// FROZEN — read-only consumers
public sealed record SourceLocation(string FilePath, int StartLine, int EndLine);

public sealed record SemanticIdentity(
    Foundry.FSPM.Core.Semantic.AssemblyIdentity Assembly,
    string Namespace,
    string MetadataName,
    string? ContainingType,
    int GenericArity,
    Foundry.FSPM.Core.Semantic.SymbolKind Kind);

public sealed record AssemblyIdentity(string Name, string? Version);

public enum SemanticResolutionStatus { Resolved, Unresolved, Truncated, Ambiguous }
public enum SymbolKind { Type, Method, Property, Field, Parameter, Namespace }
```

---

## 2. FROZEN — namespaces MCP must not expand

Foundry.FSPM.Mcp adds the following **new** files only. It must **not**
add files inside `Foundry.FSPM.Core.*` or `Foundry.FSPM.Analyzer.*`:

```
modularity/Foundry.FSPM.Mcp/
├── Foundry.FSPM.Mcp.csproj
├── Program.cs
├── Mcp/
│   ├── FspmUnderstandTool.cs
│   ├── FspmConstructTool.cs
│   └── FspmVerifyTool.cs
├── Construction/
│   ├── ConstructionEvidence.cs        (new record, NOT inside Core)
│   ├── SourceWriter.cs
│   └── HttpLoginProbe.cs
└── Evidence/
    └── EvidenceWriter.cs              (uses Foundry.FSPM.Core.Evidence types only)

tests/Foundry.FSPM.Mcp.Tests/
├── Foundry.FSPM.Mcp.Tests.csproj
├── Fixtures/
│   └── LoginMutationFixture.cs        (self-contained; mirrors User.Login shape)
├── McpToolDiscoveryTests.cs
├── UnderstandToolTests.cs
├── ConstructToolTests.cs
├── VerifyToolTests.cs
├── RuntimeEvidenceTests.cs            (4-scenario Login.Mvp HTTP probes)
├── ConstructionMutationTests.cs        (beforeFp != afterFp proof)
└── StdoutProtocolCleanTests.cs
```

**Rule:** Foundry.FSPM.Mcp never modifies anything under
`backend/modularity/Foundry.FSPM.{Core,Login,Login.Mvp}/**` or
`backend/tools/Foundry.FSPM.Analyzer/**` or
`backend/tests/Foundry.FSPM.SemanticProof.Tests/**`.

---

## 3. FROZEN — namespaces Compiler must not break (MCP depends on)

The Compiler is free to refactor everything in
`Foundry.FSPM.Compiler/**`, `Foundry.FSPM.Compiler.Tests/**`,
`Foundry.FSPM.Analyzer/**`, and the **internal implementation** of
`Foundry.FSPM.Core/**`. The Compiler **must not**:

1. Rename any type / namespace / property listed in §1.
2. Change the signature of any method listed in §1.
3. Remove any member of `FspmVerificationEvidence` (adding optional
   members is OK).
4. Change `EvidenceId.Compute` algorithm (the SHA256 over the existing
   canonical payload must remain stable so historical evidence remains
   verifiable).
5. Change `FspmRuleIds` constant string values.

The Compiler **may**:

1. Add new public types / methods (the MCP side ignores them).
2. Add new `FspmVerificationEvidence` *optional* members.
4. Refactor internals (private / internal) freely.
5. Add new rule IDs (MCP ignores them unless explicitly listed).

---

## 4. Branch topology & merge order

```text
baseline: fspm-semantic-analyzer-v1 @ 1d6a0784

Compiler side:
  branch: feature/fspm-compiler-p1 (base = fspm-semantic-analyzer-v1)
  work on: Foundry.FSPM.Compiler/**, Foundry.FSPM.Compiler.Tests/**,
            internal of Foundry.FSPM.Core/**, internal of Foundry.FSPM.Analyzer/**

MCP side:
  branch: feature/fspm-mcp-stdio-adapter (base = fspm-semantic-analyzer-v1)
  work on: Foundry.FSPM.Mcp/**, Foundry.FSPM.Mcp.Tests/**,
            docs/FSPM/**, docs/superpowers/specs/**, docs/superpowers/plans/**
  NOT touching: Foundry.FSPM.Core/**, Foundry.FSPM.Analyzer/**,
                 Foundry.FSPM.Login*/, Foundry.FSPM.SemanticProof.Tests/**,
                 zx_lowcode_netcore.sln (will be modified in Phase 2 only)

Merge order (FROZEN):
  Step 1: Compiler PR merged to fspm-semantic-analyzer-v1 (or main).
  Step 2: MCP rebases onto the post-Compiler commit.
  Step 3: MCP PR merged to fspm-semantic-analyzer-v1 (or main).
```

**Why this order:** the Compiler changes *internals* that the MCP side
reads; the Compiler's commit is the prerequisite for MCP to verify its
references still compile. The MCP side never lands first because
Foundry.FSPM.Mcp doesn't change the Compiler's input.

---

## 5. Conflict detection mechanism

Both sides must, before each PR:

1. Run on a clean clone of the *other* side's branch:
   ```bash
   dotnet build backend/zx_lowcode_netcore.sln --no-restore -m:1
   ```
2. Run the existing test baseline:
   ```bash
   dotnet exec <vstest.console.dll> tests/Foundry.FSPM.SemanticProof.Tests.dll
   ```
   (Exact `dotnet exec` invocation per the SDK blocker workaround.)

**If the other side's build or baseline tests regress** → the PR is
**rejected** until the regression is fixed or this lockdown is updated
by mutual agreement.

---

## 6. Update procedure

Changing the lockdown requires:

1. Both sides (MCP + Compiler) explicitly agree on the change.
2. A new commit on both branches updating this file.
3. A new commit on each branch's code adapting to the change.

Any unilateral change to §1-§5 is a **lockdown violation** and is grounds
for reverting the offending commit.

---

## 7. Reference

- MCP design spec: `docs/superpowers/specs/2026-09-03-fspm-mcp-stdio-adapter-design.md`
- Compiler roadmap: `docs/FSPM/EXECUTION_ROADMAP.md` (FSPM-04 → FSPM-18)
- MVP baseline: `docs/FSPM/MVP_BASELINE.md`, `docs/FSPM/MVP_TEST_RESULT.md`
- Working memory: `D:/JNPF-v52/.workbuddy/memory/MEMORY.md`
- Working memory daily: `D:/JNPF-v52/.workbuddy/memory/2026-09-03.md`

---

**End of INTERFACE_LOCKDOWN.**