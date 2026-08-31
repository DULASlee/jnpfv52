# Phase 3B-R Closure Design Specification (v4 — REWORK)

> **Status:** DRAFT — Awaiting Chief Architect Approval (v4)
> **Date:** 2026-09-01
> **Previous:** v1 REJECTED, v2 REJECTED, v3 REJECTED (5 P0/P1 BLOCKERs)
> **Owner:** AI Engineer

---

## 0. v4 Core Insight: Time-Freezing Discipline

**The fundamental failure pattern across v1/v2/v3:**

| Version | Problem |
|---------|---------|
| v1 | Documentation-only mutation labeled as refactor |
| v2 | Tautological equivalence test (Original vs Original) |
| v3 | Baseline captured AFTER refactor (post-refactor ≠ pre-refactor) |

**v4 principle:** Time must be FROZEN at pre-refactor state. All evidence must reference that frozen point.

### Hard Rule: PRE_REFACTOR_COMMIT

```
Step 0: git rev-parse HEAD → PRE_REFACTOR_COMMIT
        ALL historical source reads use this frozen SHA
        ALL baseline.json references this SHA
        NEVER use HEAD after this point for "pre-refactor" references
```

---

## 1. v3 BLOCKER Resolution Map

| # | v3 BLOCKER | v4 Solution |
|---|-----------|-------------|
| **1 (P0)** | Baseline captured AFTER refactor → not pre-refactor | **Task 0 FIRST**: capture PRE_REFACTOR_COMMIT + build pre-refactor + restore refactored |
| **2 (P0)** | GitHelper "HEAD = pre-refactor" doesn't work after commits | GitHelper takes explicit `commitSha`; all calls use `PRE_REFACTOR_COMMIT` from baseline.json |
| **3** | No immutable commit SHA | baseline.json has `preRefactorCommit` field; all historical reads use it |
| **4 (Gate D UserContext)** | SQL Key same, only parameters differ → false negative | Test asserts BOTH `ToSql().Key` AND `ToSql().Value` (parameters) |
| **5 (PreRefactor provenance)** | Manual copy | SHA256 of pre-refactor source stored in baseline; test verifies replicator source matches |
| **6 (P0)** | TargetedContractRepairer regex breaks on `Task<dynamic>` and `internal` | Use Roslyn `MethodDeclarationSyntax` (consistent with B/C) |
| **7 (Gate F diff)** | Naive line-by-line breaks on insert/delete | Use Myers diff algorithm for proper LCS-based comparison |
| **8 (Gate E)** | Integrity tests don't inspect original WorkstreamLPilotTests | Use Roslyn to parse original tests; verify they invoke real tools + have real asserts |
| **9** | xunit.runner.json not explicitly copied | Add `<Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />` |
| **10** | DispatchProxy Task<T> bug | Use reflection-based `Task.FromResult(default(T))` |
| **11** | SqlSugarRepositoryStub assumes only AsSugarClient | Audit interface, implement audit result |
| **12** | Gate B `<= 2 statements` too rigid | Semantic absence check (no Queryable/JoinQueryInfos/OrderBy/Select) |
| **13** | Final evidence pre-marks PASS | Generate from actual execution output |

---

## 2. Critical Design Decisions (v4)

### 2.1 Task 0 — IMMUTABLE BASELINE (FIRST action, before any code modification)

```powershell
# Step 0.1: Freeze commit SHA
$preRefactorCommit = (git rev-parse HEAD).Trim()
# Currently: 31c835ef0b72f7c2f33815e11162da1ce0edb4dd (the only commit)

# Step 0.2: Backup current refactored file
$refactoredContent = Get-Content FlowCommentService.cs -Raw

# Step 0.3: Restore pre-refactor version (overwrite working tree)
git show "${preRefactorCommit}:backend/modularity/workflow/JNPF.WorkFlow/Service/FlowCommentService.cs" |
    Out-File -Encoding UTF8 -FilePath FlowCommentService.cs

# Step 0.4: Build with PRE-REFACTOR code
$buildOutput = dotnet build JNPF.WorkFlow.csproj --no-restore --no-incremental 2>&1

# Step 0.5: Capture baseline (binary hash, error/warning counts)
$binaryHash = (Get-FileHash bin/Debug/net8.0/JNPF.WorkFlow.dll).Hash
$errorCount = ([regex]::Matches($buildOutput, "error CS\d+:")).Count
$warningCount = ([regex]::Matches($buildOutput, "warning CS\d+:")).Count

# Step 0.6: Save SHA256 of pre-refactor source
$referenceSourceHash = (Get-FileHash FlowCommentService.cs -Algorithm SHA256).Hash

# Step 0.7: RESTORE refactored file (for downstream work)
$refactoredContent | Out-File -Encoding UTF8 -FilePath FlowCommentService.cs

# Step 0.8: Write baseline.json
@{
    preRefactorCommit = $preRefactorCommit
    command = "..."
    sdkVersion = ...
    errorCount = $errorCount
    warningCount = $warningCount
    binaryHash = $binaryHash
    referenceSourceHash = $referenceSourceHash
    timestamp = ...
} | ConvertTo-Json | Out-File build-baseline.json
```

**This baseline is IMMUTABLE for the entire v4 execution.**

### 2.2 GitHelper: Takes Explicit Commit SHA

```csharp
public static class GitHelper
{
    public static string GetFileFromCommit(string commitSha, string repoRelativePath)
    {
        // Always use the explicit SHA — never default to HEAD
        // Caller is responsible for passing PRE_REFACTOR_COMMIT
        var psi = new ProcessStartInfo { ... };
        psi.Arguments = $"show {commitSha}:{repoRelativePath}";
        ...
    }

    /// <summary>
    /// Load PRE_REFACTOR_COMMIT from baseline.json
    /// </summary>
    public static string GetPreRefactorCommit(string baselineJsonPath)
    {
        var doc = JsonDocument.Parse(File.ReadAllText(baselineJsonPath));
        return doc.RootElement.GetProperty("preRefactorCommit").GetString()!;
    }
}
```

### 2.3 Gate A: Pre-Refactor Baseline Comparison

```csharp
[Fact(Timeout = 600000)]
public async Task GateA_WarningsDoNotIncreaseFromPreRefactorBaseline()
{
    var baseline = LoadBaseline();
    var baselineWarnings = baseline.WarningCount;  // From pre-refactor build

    var tools = new FileSystemExpertToolSet(RepositoryRoot);
    var result = await tools.BuildAsync(FlowCommentProjectPath);

    Assert.True(result.Success, "Build must succeed");
    Assert.True(result.WarningCount <= baselineWarnings,
        $"Warnings_after ({result.WarningCount}) must <= Warnings_baseline ({baselineWarnings})");
}

[Fact(Timeout = 600000)]
public async Task GateA_BinaryHashChanged_ProvingRebuild()
{
    var baseline = LoadBaseline();
    var baselineHash = baseline.BinaryHash;

    var tools = new FileSystemExpertToolSet(RepositoryRoot);
    await tools.BuildAsync(FlowCommentProjectPath);

    var currentHash = ComputeFileHash(FlowCommentBinaryPath);
    Assert.NotEqual(baselineHash, currentHash);  // Binary MUST change (refactor applied)
}
```

### 2.4 Gate D UserContext: Key + Parameters

```csharp
[Fact]
public void GateD_UserContext_AffectsSqlKey_AND_Parameters_L3()
{
    var client = CreateSqlSugarClient();
    var userA = UserManagerStub.Create("user-a-id");
    var userB = UserManagerStub.Create("user-b-id");

    var repoA = SqlSugarRepositoryStub.Create<FlowCommentEntity>(client);
    var repoB = SqlSugarRepositoryStub.Create<FlowCommentEntity>(client);

    var serviceA = new FlowCommentService(repoA, userA);
    var serviceB = new FlowCommentService(repoB, userB);

    var input = new FlowCommentListQuery { taskId = "t", keyword = "" };

    var sqlA = serviceA.BuildListQuery(input).ToSql();
    var sqlB = serviceB.BuildListQuery(input).ToSql();

    // Both Key AND Value must differ
    Assert.NotEqual(sqlA.Key, sqlB.Key);  // SQL structure differs (IIF with userId)
    Assert.NotEqual(sqlA.Value, sqlB.Value);  // OR parameters differ
    
    // At minimum, parameters must contain different userId
    var paramA = sqlA.Value.Select(kv => $"{kv.Key}={kv.Value}").ToList();
    var paramB = sqlB.Value.Select(kv => $"{kv.Key}={kv.Value}").ToList();
    Assert.Contains(paramA, p => p.Contains("user-a-id"));
    Assert.Contains(paramB, p => p.Contains("user-b-id"));
}
```

### 2.5 Gate F: Roslyn-based TargetedContractRepairer

```csharp
public sealed class TargetedContractRepairer
{
    public IReadOnlyList<ContractViolation> Diagnose(string filePath) { ... }

    public TargetedRepair GenerateRepair(string filePath, ContractViolation v)
    {
        var sourceCode = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();

        // Find the EXACT method via Roslyn (works for Task<dynamic>, internal, etc.)
        var method = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == v.TargetMethod)
            ?? throw new InvalidOperationException($"Method {v.TargetMethod} not found");

        // Within the method body, find and replace the target node
        var bodyRoot = method.Body!;
        var newBody = ApplyRepairWithinMethod(bodyRoot, v);
        var newMethod = method.WithBody(newBody);

        var newRoot = root.ReplaceNode(method, newMethod);
        var newContent = newRoot.NormalizeWhitespace("    ").ToFullString();

        // Compute line range from span
        var startLine = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var endLine = method.GetLocation().GetLineSpan().EndLinePosition.Line + 1;

        return new TargetedRepair(
            NewContent: newContent,
            StartLine: startLine,
            EndLine: endLine,
            Description: $"Restore {v.ContractName} in {v.TargetMethod}()");
    }
}
```

**Each ContractViolation now specifies:**
- `TargetMethod`: Method name (Roslyn-resolved)
- `TargetSyntax`: Specific syntax pattern (e.g., `CallEntityMethod(m => m.Creator())`)
- `ReplacementSyntax`: What to put back

### 2.6 Gate F Diff: Myers LCS Algorithm

```csharp
public static class LineDiff
{
    /// <summary>
    /// Compute LCS-based diff. Returns changed line ranges.
    /// Properly handles inserts/deletes (doesn't shift line numbers).
    /// </summary>
    public static IReadOnlyList<DiffRegion> Compute(string[] oldLines, string[] newLines)
    {
        var lcs = ComputeLcsTable(oldLines, newLines);
        return BacktrackDiff(oldLines, newLines, lcs);
    }
    
    // Myers LCS implementation
}
```

### 2.7 Gate E: Roslyn-inspect Original Tests

```csharp
[Fact]
public void GateE_WorkstreamLPilotTests_AllThreeUseRealTools_NotMocks()
{
    // Parse original test file via Roslyn
    var source = File.ReadAllText(WorkstreamLPilotTestsPath);
    var tree = CSharpSyntaxTree.ParseText(source);
    var root = tree.GetRoot();

    foreach (var testName in new[] {
        "Build_ShouldSucceedForTargetProject",
        "NoFunctionLossGate_ShouldVerifyBuildPassesAfterRefactor",
        "ExpertAgent_E2E_ShouldCompleteAllPhases"
    })
    {
        var method = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == testName)
            ?? throw new InvalidOperationException($"Test method {testName} not found");

        var body = method.Body!.ToString();
        
        // Test MUST call real tools
        Assert.True(body.Contains("BuildAsync") || body.Contains("ExecuteAsync"),
            $"{testName} must invoke real BuildAsync or ExecuteAsync");
        
        // Test MUST have real Assert
        Assert.True(body.Contains("Assert.True") || body.Contains("Assert.Equal"),
            $"{testName} must have real assertions");
    }
}
```

After Roslyn inspection confirms real tools + asserts, the original tests are converted from `[Fact(Skip=...)]` to `[Fact(Timeout=...)]`.

### 2.8 Gate B: Semantic Absence (Roslyn)

```csharp
[Fact]
public void GateB_GetList_BodyContainsNoQueryConstruction()
{
    var source = File.ReadAllText(FlowCommentServicePath);
    var tree = CSharpSyntaxTree.ParseText(source);
    var root = tree.GetRoot();

    var getList = root.DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .First(m => m.Identifier.Text == "GetList");

    // Find all method invocations in GetList body
    var invocations = getList.Body!.DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Select(i => i.Expression.ToString())
        .ToList();

    // GetList must NOT contain SQL chain construction
    Assert.DoesNotContain(invocations, m => m.Contains("Queryable"));
    Assert.DoesNotContain(invocations, m => m.Contains("JoinQueryInfos"));
    Assert.DoesNotContain(invocations, m => m.Contains("OrderBy"));
    Assert.DoesNotContain(invocations, m => m.Contains("Select"));

    // GetList MUST call BuildListQuery
    Assert.Contains(invocations, m => m.Contains("BuildListQuery"));
}
```

### 2.9 DispatchProxy: Handle Task<T> Correctly

```csharp
protected override object? Invoke(MethodInfo method, object?[]? args)
{
    var returnType = method.ReturnType;

    // Task (non-generic)
    if (returnType == typeof(Task)) return Task.CompletedTask;

    // Task<T> — need Task.FromResult<T>(default)
    if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
    {
        var tArg = returnType.GetGenericArguments()[0];
        var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(tArg);
        return fromResultMethod.Invoke(null, new object?[] { null });
    }

    // ValueTask / ValueTask<T>
    if (returnType == typeof(ValueTask)) return default(ValueTask);
    if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
    {
        var tArg = returnType.GetGenericArguments()[0];
        return Activator.CreateInstance(returnType, new object?[] { null });
    }

    // Other return types
    if (returnType.IsValueType) return Activator.CreateInstance(returnType);
    return null;
}
```

### 2.10 SqlSugarRepositoryStub: Audit-Driven

```csharp
// First audit: what methods does BuildListQuery path actually call?
// BuildListQuery calls:
//   _repository.AsSugarClient() — returns ISqlSugarClient
// That's the only call. Audit result captured in a comment.

public sealed class SqlSugarRepositoryStub : DispatchProxy
{
    public ISqlSugarClient SugarClient { get; set; } = null!;

    public static ISqlSugarRepository<T> Create<T>(ISqlSugarClient client) where T : class, new()
    {
        var proxy = Create<ISqlSugarRepository<T>, SqlSugarRepositoryStub>();
        ((SqlSugarRepositoryStub)proxy!).SugarClient = client;
        return proxy;
    }

    protected override object? Invoke(MethodInfo method, object?[]? args)
    {
        // AUDITED: BuildListQuery only calls AsSugarClient()
        if (method.Name == "AsSugarClient") return SugarClient;

        // All other methods (CRUD on ISimpleClient) — return safe defaults
        // For ToSql() path, these are NEVER called
        if (method.ReturnType == typeof(Task)) return Task.CompletedTask;
        if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var tArg = method.ReturnType.GetGenericArguments()[0];
            var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(tArg);
            return fromResult.Invoke(null, new object?[] { null });
        }
        if (method.ReturnType.IsValueType) return Activator.CreateInstance(method.ReturnType);
        return null;
    }
}
```

### 2.11 xunit.runner.json Explicit Copy

```xml
<ItemGroup>
  <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

Plus a test that verifies the file is loaded:
```csharp
[Fact]
public void GateX_XUnitConfig_IsLoaded()
{
    var outputDir = Path.GetDirectoryName(typeof(GateATests).Assembly.Location)!;
    var configPath = Path.Combine(outputDir, "xunit.runner.json");
    Assert.True(File.Exists(configPath), $"xunit.runner.json not copied to output. Looked at: {configPath}");
    var content = File.ReadAllText(configPath);
    Assert.Contains("\"parallelizeTestCollections\": false", content);
}
```

### 2.12 Final Evidence: Real Execution, Not Pre-Marked PASS

```csharp
// Evidence generator — runs after all tests pass
[Fact]
public async Task EvidenceGenerator_RunAllAndProduceReport()
{
    var runner = new EvidenceRunner();
    var report = await runner.RunAllAsync();

    // Each gate's verdict comes from actual test results
    foreach (var gate in report.Gates)
    {
        // NO pre-marked verdict
        Assert.True(gate.Verdict == GateVerdict.Pass || gate.Verdict == GateVerdict.Fail);
        Assert.NotEmpty(gate.EvidenceFiles);
    }

    // Write report
    await File.WriteAllTextAsync(".claude/evidence/phase3b-r-closure-final.md", report.ToMarkdown());
}
```

---

## 3. Updated File List (v4)

**Modified (5):**
1. `backend/modularity/workflow/JNPF.WorkFlow/Service/FlowCommentService.cs` — private → internal
2. `backend/modularity/workflow/JNPF.WorkFlow/JNPF.WorkFlow.csproj` — InternalsVisibleTo
3. `backend/tests/JNPF.Tests.Runtime.Expert/JNPF.Tests.Runtime.Expert.csproj` — references, Roslyn, xunit.runner.json CopyToOutputDirectory
4. `backend/tests/JNPF.Tests.Runtime.Expert/WorkstreamLPilotTests.cs` — Convert 3 Skip → Timeout (AFTER Gate E Roslyn inspection)
5. `FlowCommentService.cs` — REVERTED to pre-refactor for baseline build, then RESTORED to refactored

**Created (16):**
6. `backend/tests/JNPF.Tests.Runtime.Expert/xunit.runner.json`
7. `scripts/capture-build-baseline.ps1` — captures PRE-REFACTOR baseline (Task 0)
8. `build-baseline.json` — IMMUTABLE pre-refactor baseline
9. `backend/tests/JNPF.Tests.Runtime.Expert/GitHelper.cs` — takes explicit commit SHA
10. `backend/tests/JNPF.Tests.Runtime.Expert/SqlSugarQueryCaptureHelper.cs`
11. `backend/tests/JNPF.Tests.Runtime.Expert/UserManagerStub.cs` — Task<T> via reflection
12. `backend/tests/JNPF.Tests.Runtime.Expert/SqlSugarRepositoryStub.cs` — audit + Task<T>
13. `backend/tests/JNPF.Tests.Runtime.Expert/PreRefactorQueryReplicator.cs` — provenance-verified
14. `backend/tests/JNPF.Tests.Runtime.Expert/TargetedContractRepairer.cs` — Roslyn-based
15. `backend/tests/JNPF.Tests.Runtime.Expert/LineDiff.cs` — Myers LCS algorithm
16. `backend/tests/JNPF.Tests.Runtime.Expert/GateATests.cs`
17. `backend/tests/JNPF.Tests.Runtime.Expert/GateBAndCTests.cs`
18. `backend/tests/JNPF.Tests.Runtime.Expert/GateDTests.cs` — Key + Value verification
19. `backend/tests/JNPF.Tests.Runtime.Expert/GateEIntegrityTests.cs` — Roslyn-inspects original tests
20. `backend/tests/JNPF.Tests.Runtime.Expert/GateFTests.cs` — Roslyn + Myers diff
21. `backend/tests/JNPF.Tests.Runtime.Expert/EvidenceGenerator.cs` — produces final report from execution

---

## 4. Critical Task Order (v4)

**CRITICAL**: Task 0 (immutable baseline) MUST run BEFORE any code modification:

```
Step 0 (FIRST):  Capture PRE_REFACTOR_COMMIT and pre-refactor baseline
                 (modifies FlowCommentService.cs temporarily, then restores)

Step 1+:        Test infrastructure (references, helpers)
                 (these don't touch FlowCommentService.cs)

Step N:         Gate tests
                 (still no FlowCommentService.cs modification)

Step LAST:      Convert Skip → Timeout (in WorkstreamLPilotTests.cs only)
```

The refactored FlowCommentService.cs is the artifact being verified — we don't touch it.

---

## 5. Acceptance Criteria (v4)

| Gate | Evidence | Status |
|------|----------|--------|
| A | Pre-refactor baseline (immutable), real build comparison | ⏳ |
| B | Roslyn semantic absence (no Queryable/OrderBy/Select in GetList) | ⏳ |
| C | L2 Reflection + L1 Roslyn, honest labels | ⏳ |
| D | REAL BuildListQuery invoked, Key + Value verification, pre-refactor source from frozen commit | ⏳ |
| E | Roslyn-inspected original tests have real BuildAsync/ExecuteAsync + Assert; tests then run with Timeout | ⏳ |
| F | Roslyn-based TargetedContractRepairer, Myers diff shows only target region | ⏳ |

**Phase 3B-R COMPLETE** only when all 6 gates verified with REAL evidence.

---

## 6. Out of Scope (v4)

- ❌ No new refactoring (refactor already done)
- ❌ No entry to Phase 4
- ❌ No modification to FlowCommentService.cs (except temporarily during baseline capture)
- ❌ No pre-marked PASS verdicts

---

**Awaiting Chief Architect v4 approval before execution.**