# FSPM MCP — stdio Adapter Design

> **For agentic workers:** This is a brainstorming-approved design spec.
> Next step: invoke the **writing-plans** skill to convert this spec into an
> implementation plan with bite-sized tasks.

**Date:** 2026-09-03
**Status:** Design v2 (brainstorming-approved; **supersedes v1** with 4
core corrections: construct职责, verify 8段, Decision vs Stage, real-change proof)
**Owner:** Forge
**Reference:** `docs/FSPM/《FSPM MCP：极致 AI 工程生产力与 JNPF 首个闭环施工包》.md` (施工包原文)

---

## v1 → v2 修正记录

| § | v1 错误 | v2 修正 |
|---|---|---|
| 3.3 FspmConstructTool | 用 HttpClient POST /login 来"构造" | 改为真实 SourceWriter 改源码 + ConstructionEvidence (before/after fingerprint + diff + tx id) |
| 4 Data Flow | 3 步（understand/construct/verify）| 改为 4 段闭环（UNDERSTAND / CONSTRUCT / BUILD-ANALYZE / RUNTIME-VERIFY）|
| 5 Error Handling | Build FAIL → Decision=Violation | 拆分为 VerificationResult.FailureStage（BUILD/TEST/RUNTIME） + FspmVerificationEvidence.Decision（仅规则判定）|
| 6 Testing | 12 Facts | 扩展 19 Facts：增加 ConstructionRealMutationTests（before/after 不等）+ RuntimeEvidenceTests（4 场景）+ StdoutProtocolCleanTests |
| 9 Acceptance Gates | G0-G18 | 重构为 14 个硬门禁 G0-G13（process/stdout/tools/understand/construct/fingerprint/build/analyzer/tests/runtime-login/evidence/refs/closed）|
| 1 Architecture | 隐含 stdout 可写日志 | 显式冻结 stdout 边界：stdout ONLY MCP JSON-RPC；stderr ONLY logs/diagnostics |

---

## 0. Context

The施工包《FSPM MCP》 defines a 24-phase construction package for an FSPM
MCP server with three tools (`fspm_understand`, `fspm_construct`,
`fspm_verify`) that closes a `Understand → Construct → Verify` loop on the
real JNPF workspace's `User.Login` scenario.

The施工包 assumes a **white-paper starting point** under
`tools/fspm-mcp/` with brand-new `.sln` / `.csproj`. However, the JNPF
workspace already contains 8 days of FSPM MVP work that has been frozen
as `FSPM-01/02/03` baseline (`docs/FSPM/MVP_BASELINE.md`,
`docs/FSPM/MVP_TEST_RESULT.md`), plus a hard `.NET SDK NuGet blocker`
that prevents `dotnet new` / `dotnet add` / `dotnet restore` from
running normally.

After brainstorming, the user selected:

1. **Direction A:** Refactor the existing Foundry.FSPM modules rather
   than scaffolding a brand-new project.
2. **MCP形态 A:** `stdio-only` strict compliance with the施工包
   (`Foundry.FSPM.Mcp` = stdio MCP Adapter, `Foundry.FSPM.Login.Mvp`
   = HTTP-被测 application).

This spec locks the resulting design.

---

## 1. Architecture Overview

```text
┌─────────────────────────────────────────────────────────────────┐
│                       AI / MCP Client                            │
└────────────────────────┬────────────────────────────────────────┘
                         │ MCP / stdio JSON-RPC
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│            Foundry.FSPM.Mcp  (new, net8.0 console)               │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ Mcp/                                                      │  │
│  │   FspmUnderstandTool    ──┐                               │  │
│  │   FspmConstructTool     ──┼── internal → Foundry.FSPM.Core │  │
│  │   FspmVerifyTool        ──┘                               │  │
│  └───────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ Construction/  (.fspm.tmp atomic write)                    │  │
│  │ Evidence/      (EvidenceWriter → .fspm/evidence/...)       │  │
│  └───────────────────────────────────────────────────────────┘  │
└────────┬──────────────────────────────┬─────────────────────────┘
         │ ProjectReference             │ HttpClient
         ▼                              ▼
┌──────────────────────┐     ┌────────────────────────────────────┐
│ Foundry.FSPM.Core    │     │ Foundry.FSPM.Login.Mvp              │
│ Foundry.FSPM.Analyzer│     │ (Kestrel, POST /login, frozen)      │
└──────────────────────┘     └────────────────────────────────────┘
```

### Architectural Boundaries

| Concern | Boundary |
|---|---|
| MCP Transport | **stdio only** (施工包 Phase 1) |
| HTTP Transport | Foundry.FSPM.Login.Mvp only (frozen, no change) |
| Semantic Kernel | Foundry.FSPM.Core + Foundry.FSPM.Analyzer (frozen) |
| MCP Adapter | Foundry.FSPM.Mcp (new) |
| Evidence | Foundry.FSPM.Core.Evidence (frozen SHA256 protocol) |

**Rule of separation:** Foundry.FSPM.Analyzer ≠ Foundry.FSPM.Mcp.
Analyzer 负责语义内核；Mcp 负责 MCP 协议适配。Login.Mvp 仅作为验证样例。

### Process Stream Contract (FROZEN — Iron Rule)

Since `Foundry.FSPM.Mcp` is a **stdio MCP server**, every byte on stdout
is part of the MCP JSON-RPC framing. Any contamination breaks the
protocol.

```text
stdin   ← MCP JSON-RPC frames (from MCP client)
stdout  → ONLY MCP JSON-RPC frames (to MCP client)
stderr  → ALL diagnostics, logs, startup errors
```

**Hard constraints (frozen):**

1. `Console.WriteLine(...)` to stdout **MUST NOT** appear in source.
   Verified by `StdoutProtocolCleanTests`.
2. `builder.Logging.AddConsole` MUST set
   `LogToStandardErrorThreshold = LogLevel.Trace` (施工包 Phase 1 明确).
3. `ILogger<T>` is the only allowed log surface in Foundry.FSPM.Mcp.
4. Startup banners / version strings / "Hello MCP" writes MUST go to
   stderr only.
5. Any test that calls `fspm_understand/construct/verify` and reads
   stdout MUST see **only** JSON-RPC frames (no log lines).

**Why this matters:** if a developer adds `Console.WriteLine("hi")` to
stdout, the MCP client sees `{ "hi"` and the entire handshake fails
silently with a JSON parse error from the client side. This is the #1
historical cause of "MCP server starts but no tools appear" bugs.

---

## 2. Project Structure

### New files (Foundry.FSPM.Mcp)

```text
backend/modularity/Foundry.FSPM.Mcp/
├── Foundry.FSPM.Mcp.csproj                  # net8.0 console, MCP SDK 2.2.0
├── Program.cs                               # Host + AddMcpServer + stdio
│
├── Mcp/
│   ├── FspmUnderstandTool.cs                # [McpServerToolType]
│   ├── FspmConstructTool.cs                 # [McpServerToolType]
│   └── FspmVerifyTool.cs                    # [McpServerToolType]
│
├── Construction/
│   ├── SourceWriter.cs                      # atomic .fspm.tmp → File.Move
│   └── HttpLoginProbe.cs                    # HttpClient POST /login helper
│
└── Evidence/
    └── EvidenceWriter.cs                    # serialize → .fspm/evidence/<id>/

backend/tests/Foundry.FSPM.Mcp.Tests/
├── Foundry.FSPM.Mcp.Tests.csproj
├── McpToolDiscoveryTests.cs                 # 1 Fact
├── UnderstandToolTests.cs                   # 4 Facts (User/UserName/Password/Login)
├── ConstructToolTests.cs                    # 4 Facts (4 Login.Mvp scenarios)
├── VerifyToolTests.cs                       # 1 Fact (CLOSED loop)
└── MutationTests.cs                         # 2 Facts (ARCH001 mutation triggers)
```

### Modified files

- `backend/zx_lowcode_netcore.sln`: add 2 new projects under existing
  solution folder structure.
- `backend/modularity/Foundry.FSPM.Login.Mvp/`: **unchanged**
  (frozen baseline, FSPM-01 PROVEN).
- `backend/modularity/Foundry.FSPM.Core/`: **unchanged**
  (8-day MVP frozen).
- `backend/tools/Foundry.FSPM.Analyzer/`: **unchanged**
  (FSPM Diagnostic Property protocol frozen).

### Config files (workspace root)

```text
.fspm/
├── workspace.json                            # auto-generated by WorkspaceDiscovery
├── settings.json                             # Foundry.FSPM.* module list
├── architecture-rules.json                   # ARCH001/SEC001/UI001 references
└── evidence/
    └── <execution-id>/
        ├── intent.json
        ├── semantic.json
        ├── construction.json
        ├── verification.json
        └── result.json
```

---

## 3. Components & Responsibilities

### 3.1 Foundry.FSPM.Mcp/Program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// CRITICAL: MCP stdio transport requires logs on stderr, NOT stdout.
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

### 3.2 FspmUnderstandTool

```csharp
[McpServerToolType]
public static class FspmUnderstandTool
{
    [McpServerTool]
    [Description("Resolves an FSPM semantic target (User.Login, User.Name, ...)")]
    public static async Task<string> Understand(
        [Description("Absolute workspace root")] string workspaceRoot,
        [Description("Target e.g. User.Login")] string target)
    {
        // Calls Foundry.FSPM.Core.Semantic to resolve real symbols.
    }
}
```

### 3.3 FspmConstructTool — REAL SOURCE MUTATION (corrected v2)

> **v1 was wrong:** `fspm_construct` was designed to POST /login and
> call that "construction". That only proves the business system runs,
> not that FSPM produced a real source change. v2 makes
> `fspm_construct` actually mutate source.

```csharp
[McpServerToolType]
public static class FspmConstructTool
{
    [McpServerTool]
    [Description("Performs REAL source mutation for an FSPM operation and returns before/after evidence.")]
    public static async Task<string> Construct(
        [Description("Workspace root")] string workspaceRoot,
        [Description("e.g. User.Login")] string operation,
        [Description("Human instruction")] string instruction)
    {
        // 1. SemanticResolver 解析 SemanticRef（不能写死 if User.Login）
        // 2. Constructor.ConstructAsync：生成 ConstructionPlan
        // 3. SourceWriter.WriteAtomicAsync(.fspm.tmp → File.Move)
        // 4. 计算 beforeFingerprint / afterFingerprint（SHA256 over canonical source）
        // 5. 收集 ConstructionEvidence（target, changedFiles, beforeFp, afterFp, diff, txId, timestamp）
        // 6. 返回 {status, executionId, changedFiles, beforeFingerprint, afterFingerprint, txId}
        //    NO_CHANGE 时：changedFiles=[], status="NO_CHANGE", reason required
    }
}
```

**Why this fix matters:** "test green ≠ real capability proof" is the
single most expensive mistake the project has been guarding against.
The earlier `HttpClient POST /login` design would have produced green
tests with zero real source mutation — the exact "测试绿色冒充真实能力"
failure mode the user vetoed.

### 3.4 FspmVerifyTool — 8-SEGMENT VERIFICATION (corrected v2)

> **v1 was wrong:** `fspm_verify` was written as "调 4 Analyzer → CLOSED".
> That collapses Build/Analyze/Test/Runtime into a single binary. v2
> separates them into 8 explicit verification stages.

```csharp
[McpServerToolType]
public static class FspmVerifyTool
{
    [McpServerTool]
    [Description("Verifies an FSPM operation across 8 segments: Semantic, Architecture, Security, UI, Build, Test, Runtime, Evidence.")]
    public static async Task<string> Verify(
        [Description("Workspace root")] string workspaceRoot,
        [Description("e.g. User.Login")] string operation,
        [Description("Absolute path of Foundry.FSPM.Login.Mvp.csproj")] string projectPath,
        [Description("Absolute path of Foundry.FSPM.SemanticProof.Tests.csproj")] string testPath,
        [Description("Login.Mvp base URL")] string loginMvpBaseUrl,
        [Description("Execution ID from fspm_construct")] string executionId)
    {
        // 8 段验证（每段独立 status，可短路）：
        //   1. Semantic Verification  (FspmSemanticAnalyzer)
        //   2. Architecture Verification (FspmArchitectureAnalyzer → ARCH001)
        //   3. Security Verification (FspmSecurityAnalyzer → SEC001)
        //   4. UI Verification (FspmUiAnalyzer → UI001)
        //   5. Build Verification (dotnet build)
        //   6. Test Verification (dotnet test)
        //   7. Runtime Verification (start Login.Mvp + POST /login × 4 scenarios)
        //   8. Evidence Verification (verify .fspm/evidence/<id>/* files exist + SHA256)
        //
        // 任一段 FAIL → VerificationResult.FailureStage 设值并短路
        // 8 段全 PASS → VerificationResult.Status = "CLOSED"
    }
}
```

### 3.5 ConstructionEvidence (new in v2 — REAL CHANGE PROOF)

```csharp
public sealed record ConstructionEvidence
{
    public required string Target { get; init; }               // e.g. "User.Login"
    public required string ExecutionId { get; init; }          // guid hex
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
    public required IReadOnlyList<string> ChangedFiles { get; init; }
    public required string Status { get; init; }              // CONSTRUCTED | NO_CHANGE | REJECTED
    public string? Reason { get; init; }                      // required when Status == NO_CHANGE
    public required string WriterTransactionId { get; init; } // unique per WriteAtomicAsync call
    public required IReadOnlyList<FileFingerprint> BeforeFingerprints { get; init; }
    public required IReadOnlyList<FileFingerprint> AfterFingerprints { get; init; }
    public required string? DiffSummary { get; init; }        // short human summary
}

public sealed record FileFingerprint(string Path, string Sha256, int LineCount);
```

**Hard rules (frozen):**

1. `Status == "CONSTRUCTED"` ⟺ `ChangedFiles.Length > 0` ⟺
   `BeforeFingerprints[i].Sha256 != AfterFingerprints[i].Sha256` for every
   changed file.
2. `Status == "NO_CHANGE"` ⟺ `ChangedFiles.Length == 0` ⟺
   `Reason != null`.
3. `WriterTransactionId` is a UUID4 generated at the start of
   `WriteAtomicAsync`; emitted into both the audit log and the result
   JSON. This prevents an engineer from faking `changedFiles = [...]`
   without an actual write transaction.

### 3.6 SourceWriter (atomic write + fingerprint)

```csharp
public sealed class SourceWriter
{
    public string Backup(string filePath, string executionId) { ... }

    public async Task<WriteResult> WriteAtomicAsync(
        string filePath,
        string content,
        CancellationToken ct = default)
    {
        // 1. Compute beforeFingerprint = SHA256(filePath)
        // 2. Generate writerTransactionId = Guid.NewGuid().ToString("N")
        // 3. Write .fspm.tmp
        // 4. File.Move(tempPath, filePath, overwrite: true)
        // 5. Compute afterFingerprint = SHA256(filePath)
        // 6. Return WriteResult(writerTransactionId, beforeFp, afterFp)
    }
}
```

### 3.7 EvidenceWriter

Reuses `Foundry.FSPM.Core.Evidence.FspmVerificationEvidence` (frozen
SHA256-fingerprinted record) and serializes to
`.fspm/evidence/<execution-id>/result.json`. Now also writes
`construction.json` (containing the ConstructionEvidence) and
`verification.json` (containing the 8-segment VerificationResult).

---

## 4. Data Flow — 4-Stage Closed Loop (`User.Login`) (corrected v2)

> **v1 was a 3-step flow (understand/construct/verify) where verify was
> monolithic.** v2 makes it explicit that **construction must produce a
> real source mutation before verification even starts**, and verification
> is composed of **8 independent segments**.

```text
┌────────────────────────────────────────────────────────────────┐
│ STAGE 1 — UNDERSTAND                                            │
│                                                                  │
│ AI: fspm_understand({workspaceRoot, target:"User.Login"})        │
│   → SemanticResolver 解析真实符号（不能 if op == "User.Login"）   │
│   → 返回 RESOLVED + 真实 file + 真实 line                         │
└────────────────────────┬───────────────────────────────────────┘
                         ↓ executionId (optional / planning)
┌────────────────────────────────────────────────────────────────┐
│ STAGE 2 — CONSTRUCT  (REAL SOURCE MUTATION)                      │
│                                                                  │
│ AI: fspm_construct({workspaceRoot, operation, instruction})      │
│   → SemanticResolver 解析出 target file + symbol                 │
│   → Constructor.ConstructAsync 生成 ConstructionPlan             │
│   → SourceWriter.WriteAtomicAsync：                              │
│       beforeFp = SHA256(file)                                    │
│       写 .fspm.tmp → File.Move(overwrite)                       │
│       afterFp  = SHA256(file)                                    │
│   → ConstructionEvidence {                                       │
│       target, executionId, changedFiles,                         │
│       beforeFingerprints, afterFingerprints,                     │
│       writerTransactionId, status, diffSummary                   │
│     }                                                            │
│   → 返回 CONSTRUCTED / NO_CHANGE / REJECTED                      │
└────────────────────────┬───────────────────────────────────────┘
                         ↓ executionId
┌────────────────────────────────────────────────────────────────┐
│ STAGE 3 — BUILD + ANALYZE                                        │
│                                                                  │
│ AI: fspm_verify({workspaceRoot, operation, projectPath,          │
│                  testPath, loginMvpBaseUrl, executionId})        │
│   → Build: dotnet build <projectPath> --no-restore               │
│   → Analyzer: 4 个 Foundry.FSPM.Analyzer 跑 ARCH001/SEC001/UI001│
│   → 任一 FAIL → VerificationResult.FailureStage=BUILD/ANALYZER   │
│                  并短路到 STAGE 4（跳过 Runtime）                  │
└────────────────────────┬───────────────────────────────────────┘
                         ↓
┌────────────────────────────────────────────────────────────────┐
│ STAGE 4 — RUNTIME VERIFY                                         │
│                                                                  │
│   → Start Foundry.FSPM.Login.Mvp (dotnet run, 后台进程)         │
│   → POST /login × 4 场景：                                       │
│       admin/123456    → success:true (PASS)                      │
│       admin/wrong     → Invalid credentials (PASS)                │
│       unknown/x       → User not found       (PASS)              │
│       空               → Missing credentials  (PASS)              │
│   → 任一场景 FAIL → FailureStage=RUNTIME 短路                    │
│   → Stop Login.Mvp 进程                                           │
└────────────────────────┬───────────────────────────────────────┘
                         ↓
┌────────────────────────────────────────────────────────────────┐
│ EVIDENCE FINALIZATION                                            │
│                                                                  │
│   → .fspm/evidence/<executionId>/                                │
│       intent.json        (AI 输入)                                │
│       semantic.json      (STAGE 1 输出)                           │
│       construction.json  (STAGE 2 ConstructionEvidence)          │
│       verification.json  (STAGE 3+4 8-segment result)            │
│       result.json        (汇总 CLOSED/FAILED + evidence sha256)  │
│                                                                  │
│   → 返回 { status: "CLOSED" | "FAILED",                          │
│            verification: { semantic, architecture, security, ui, │
│                            build, test, runtime, evidence },    │
│            failureStage: BUILD | TEST | RUNTIME | null }         │
└────────────────────────────────────────────────────────────────┘
```

**Iron rule (frozen):** STAGE 2 must produce real source mutation
(`beforeFp != afterFp` for at least one file) before STAGE 3 begins.
If STAGE 2 returns `NO_CHANGE` and `Reason` claims the source is
already correct, the AI may still proceed, **but** the
VerificationResult must record `ConstructionStatus = "NO_CHANGE"` so
that future audits can detect "green without proof of change".

---

## 5. Error Handling — DECISION vs STAGE (corrected v2)

> **v1 conflated "rule decision" (ARCH001/SEC001/UI001 pass/fail) with
> "execution failure" (Build/Test/Runtime fail).** This is a deep
> semantic confusion: a Build failure does NOT mean ARCH001 violated a
> rule. v2 separates the two concerns cleanly.

### 5.1 Two failure concepts, never to be merged

```text
A. FspmVerificationEvidence.Decision   (rule evaluation)
   = Pass | Violation | NotApplicable | Unknown

B. VerificationResult.FailureStage     (execution gate)
   = null | "BUILD" | "TEST" | "RUNTIME" | "EVIDENCE" | "CONSTRUCT"
```

These are **orthogonal dimensions**:

- `Decision` is produced by Analyzer rule evaluation (ARCH001/SEC001/UI001).
- `FailureStage` is set when an execution gate (build/test/runtime/etc.)
  fails to complete.

A `dotnet build` failure is **not** an `ARCH001` violation. ARCH001
may still be `Pass` even when the build fails (e.g. a syntax error in
unrelated code doesn't change ARCH001's verdict on a `new DbContext`
expression).

### 5.2 Failure matrix (v2)

| Stage | Failure | VerificationResult.FailureStage | Evidence.Decision (for that stage) | Short-Circuit |
|---|---|---|---|---|
| UNDERSTAND | Semantic not resolved | null (rejected pre-loop) | n/a | yes (skip loop) |
| CONSTRUCT | SourceWriter I/O error | `CONSTRUCT` | `Unknown` | yes (skip STAGE 3/4) |
| CONSTRUCT | Before/after identical (Status=NO_CHANGE) | null (allowed) | n/a | no (continue but mark `ConstructionStatus = "NO_CHANGE"`) |
| BUILD | `dotnet build` exit != 0 | `BUILD` | `Unknown` | **yes (施工包 Rule 5)** |
| ANALYZE | ARCH001/SEC001/UI001 raises diagnostic | (analysis runs regardless) | `Violation` (per rule) | **no — analysis runs to completion even if BUILD fails; this is by design** |
| TEST | `dotnet test` exit != 0 | `TEST` | `Unknown` | yes (skip Runtime) |
| RUNTIME | Login.Mvp won't start | `RUNTIME` | `Unknown` | yes (skip 4-scenario probes) |
| RUNTIME | Any of 4 scenarios returns non-PASS | `RUNTIME` | `Violation` (per scenario) | yes (stop probing remaining scenarios) |
| EVIDENCE | `.fspm/evidence/<id>/result.json` write fails | `EVIDENCE` | `Unknown` | no (terminal only — already done) |
| Architecture rule missing | n/a | null | `NotApplicable` (per rule) | no — rule not established does not mean rule violated |

### 5.3 Example: Build FAIL but ARCH001 PASS

```json
{
  "status": "FAILED",
  "failureStage": "BUILD",
  "verification": {
    "semantic":     "PASS",
    "architecture": "PASS",
    "security":     "PASS",
    "ui":           "PASS",
    "build":        "FAIL",
    "test":         "NOT_RUN",
    "runtime":      "NOT_RUN",
    "evidence":     "PASS"
  },
  "evidence": [
    { "ruleId": "ARCH001", "decision": "Pass", "location": "...LoginService.cs:12" },
    { "stage": "BUILD",    "failure": "CS1: Unexpected token", "exitCode": 1 }
  ]
}
```

This tells the AI: *"the architecture rule for new DbContext was satisfied,
but the build broke on something unrelated — don't conflate the two."*

### 5.4 Iron rule (frozen)

Any Gate FAIL → `VerificationResult.FailureStage` is set and subsequent
gates short-circuit (施工包 Rule 5). Analyzer rules (`ARCH001/SEC001/UI001`)
always run to completion to produce their `Decision` — they are
informational, not gates.

---

## 6. Testing Strategy — 19 FACTS MATRIX (corrected v2)

> **v1 listed 12 Facts missing 3 critical classes:**
> real-construction mutation tests, real-runtime 4-scenario tests, and
> stdout-protocol-clean tests. v2 lists **19 Facts** covering all 4
> stages of the closed loop plus stream hygiene.

### 6.1 Test matrix

| ID | Layer | Tool | Count | Workaround |
|---|---|---|---|---|
| T01 | Tool Discovery | xUnit Fact (3 tools exactly) | 1 | `dotnet exec vstest.console.dll` |
| T02 | Semantic Real Bind | xUnit (User/Login/UserName/Password) | 4 | `MetadataReference` load Foundry.FSPM.Login.Mvp.dll |
| T03 | Construction Real Mutation | xUnit (beforeFp != afterFp, writer tx id, NO_CHANGE) | 3 | temp fixture + SourceWriter |
| T04 | Runtime 4-Scenario | xUnit (admin/123456 / wrong / unknown / empty) | 4 | start `Foundry.FSPM.Login.Mvp` background Kestrel |
| T05 | Architecture Mutation (ARCH001) | xUnit (good 0 ARCH001 / bad 1+ ARCH001) | 2 | Foundry.FSPM.Analyzer fixture |
| T06 | Build/Test Verifier | xUnit (build PASS / build FAIL → FailureStage=BUILD) | 2 | `--no-restore` build |
| T07 | Stdout Protocol Clean | xUnit (start process, capture stdout, assert only JSON-RPC) | 1 | `Process.Start` redirect stdout |
| T08 | E2E CLOSED Loop | xUnit (run all 4 stages end-to-end) | 1 | integration of T01-T07 |
| T09 | Decision vs Stage separation | xUnit (Build FAIL but ARCH001 Decision=Pass) | 1 | deliberately break unrelated file |

**Total: 19 Facts.** All executed via
`dotnet exec vstest.console.dll` (only path that survives
`dotnet test` SDK blocker).

### 6.2 Why each test exists (real-capability proof)

- **T03 Construction Real Mutation** — proves that `fspm_construct`
  actually changed bytes on disk; rejects any fake `changedFiles=[…]`
  pattern.
- **T04 Runtime 4-Scenario** — proves Login.Mvp responds with the
  expected 4 outcomes; rejects any HTTP-mocked proof.
- **T05 ARCH001 Mutation** — proves the rule is real (positive +
  negative + mutation); rejects rule-checker that always returns Pass.
- **T07 Stdout Protocol Clean** — proves that no `Console.WriteLine`
  contaminates stdout; prevents the #1 historical "MCP tools not
  visible" failure mode.
- **T09 Decision vs Stage** — proves the two failure concepts are
  orthogonally recorded; rejects conflation in v1.

---

## 7. Solution Topology Changes

### `backend/zx_lowcode_netcore.sln` (add 2 projects)

```text
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Foundry.FSPM.Mcp", "modularity\Foundry.FSPM.Mcp\Foundry.FSPM.Mcp.csproj", "{<NEW-GUID>}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Foundry.FSPM.Mcp.Tests", "tests\Foundry.FSPM.Mcp.Tests\Foundry.FSPM.Mcp.Tests.csproj", "{<NEW-GUID>}"
EndProject
```

Build verification:
```bash
dotnet build backend/zx_lowcode_netcore.sln --no-restore -m:1
```
Expected: `0 errors` (consistent with current 0-error baseline).

---

## 8. Out of Scope (Phase 1)

- HTTP/SSE MCP transport (施工包 forbids it; rejected brainstorming option B/D)
- Embedding MCP inside Foundry.FSPM.Login.Mvp (rejected brainstorming option C)
- Modifying Foundry.FSPM.Login.Mvp / Core / Analyzer (frozen baseline)
- New Architecture Rule engine beyond `Foundry.FSPM.Analyzer`'s existing
  ARCH001/SEC001/UI001.
- FSPM-04 → FSPM-18 (18-Task Roadmap in `docs/FSPM/EXECUTION_ROADMAP.md`)
  deferred until Phase 2 of this design.
- **Login.Mvp as the construction target** — Login.Mvp is the runtime
  verification target (Stage 4), not the source mutation target
  (Stage 2). Stage 2's mutation target is the SemanticRef (e.g.
  `Foundry.FSPM.Login.Mvp/Application/LoginService.cs` itself), and
  the mutation is **bounded by the Foundry.FSPM.Mcp project's own
  evolution surface** (its own files + a designated test fixture).
  This avoids any actual mutation of the frozen Login.Mvp baseline.

---

## 9. Acceptance Gates — 14 HARD GATES G0–G13 (corrected v2)

> **v1 mapped 18 施工包 gates to 18 FSPM gates** which collapsed too
> many concerns. v2 collapses to **14 hard gates** that each have a
> binary PASS/FAIL and an automatic test that proves it.

| Gate | What it proves | Verifying test |
|---|---|---|
| **G0** MCP process starts | `dotnet run --project Foundry.FSPM.Mcp` exits 0 in startup probe | T01 setup |
| **G1** stdout protocol-clean | stdout contains ZERO non-JSON-RPC bytes during 10s lifetime | T07 |
| **G2** exactly 3 tools discoverable | `tools/list` JSON-RPC returns `fspm_understand/construct/verify` and nothing else | T01 |
| **G3** understand resolves REAL symbol | `fspm_understand({target:"User.Login"})` returns RESOLVED with real file + line | T02 |
| **G4** construct produces REAL source mutation | `fspm_construct` writes bytes; `beforeFp != afterFp` for changed file | T03 |
| **G5** before/after fingerprint proven | ConstructionEvidence.BeforeFingerprints[i].Sha256 != AfterFingerprints[i].Sha256 for every i | T03 |
| **G6** build succeeds | `dotnet build Foundry.FSPM.Login.Mvp.csproj --no-restore` exit 0 | T06 |
| **G7** Analyzer succeeds | 4 Foundry.FSPM.Analyzer run with 0 unhandled exception (Decision may be Pass/Violation/NotApplicable) | T05 |
| **G8** tests succeed | `dotnet test Foundry.FSPM.SemanticProof.Tests.csproj` ≥ baseline (currently 34/34) | T06 |
| **G9** Login.Mvp starts | `dotnet run --project Foundry.FSPM.Login.Mvp` opens Kestrel on real port | T04 setup |
| **G10** `/login` 4 scenarios verified | admin/123456, admin/wrong, unknown/x, 空 — all 4 expected outcomes | T04 |
| **G11** evidence persisted | `.fspm/evidence/<id>/{intent,semantic,construction,verification,result}.json` all exist | T08 |
| **G12** evidence references actual execution | every evidence file's txId / executionId / fingerprint traces back to a real test execution | T08 + T09 |
| **G13** final status CLOSED | VerificationResult.Status == "CLOSED" with all 8 segments PASS | T08 |

**Iron rule:** Any single Gate FAIL → **CLOSED is impossible**. The
phase-1 deliverable is not "code that compiles" but "G0..G13 all PASS
with binary, reproducible evidence on disk".

### 9.1 Gate-to-test binding (auto-fail if test missing)

| Gate | Required passing tests |
|---|---|
| G0 | T01 |
| G1 | T07 |
| G2 | T01 |
| G3 | T02 (4 Facts) |
| G4 | T03 (3 Facts) |
| G5 | T03 (3 Facts) |
| G6 | T06 (1 Fact for PASS) |
| G7 | T05 (2 Facts) |
| G8 | T06 (1 Fact for PASS) |
| G9 | T04 setup |
| G10 | T04 (4 Facts) |
| G11 | T08 (1 Fact) |
| G12 | T08 + T09 (2 Facts) |
| G13 | T08 (1 Fact with 8-segment verification) |

### 9.2 Why 14 gates not 18

The original 施工包 G0–G18 mixed policy (G3 Workspace discovery, G4–G6
SemanticRef) with capability (G9 mutation, G10 build). v2 keeps only
gates that produce **direct binary evidence**. Workspace discovery and
SemanticRef coverage are now verified inside T01–T03 implicitly
(SemanticResolver must resolve symbols → must scan workspace).

---

## 10. Risks & Mitigations

| Risk | Severity | Mitigation |
|---|---|---|
| `dotnet new` / `dotnet add` blocked by SDK NuGet bug | High | manual `mkdir` + manual `.csproj` XML editing; build with `dotnet build --no-restore` |
| Foundry.FSPM.Analyzer may not be loadable as `ProjectReference` | Medium | verify in Phase 1 Task 2; fallback to `ReferenceOutputAssembly=true` if needed |
| HttpClient to Foundry.FSPM.Login.Mvp requires running Kestrel | Medium | E2E test spins up `dotnet run --project Foundry.FSPM.Login.Mvp` as background process |
| `dotnet exec vstest.console.dll` may miss test discovery | Low | use `--TestCaseFilter` explicitly for each Fact |
| 施工包 `ModelContextProtocol 2.2.0` SDK may pull transitive NuGet blocked by SDK bug | High | if blocked, fall back to manual `csproj` write + `--no-restore` build |

---

## 11. Open Questions (deferred to writing-plans)

1. Login.Mvp port during E2E: **Decision (v2 lock-in)** — use
   `launchSettings.json` with a fixed port `5099` for Stage 4
   runtime verification. E2E test starts `dotnet run --launch-profile
   FspmE2E` which binds deterministically. (Was Open in v1; closed in v2.)
2. WorkspaceManifest shape (project name → csproj path map) —
   `.fspm/workspace.json` schema TBD in writing-plans.
3. `fspm_construct` Stage 2 mutation target: **Decision (v2 lock-in)** —
   first-phase mutation target is a self-contained test fixture under
   `tests/Foundry.FSPM.Mcp.Tests/Fixtures/LoginMutationFixture.cs`
   that mirrors the `User.Login` shape but is **owned by the new
   project**. This avoids mutating the frozen `Foundry.FSPM.Login.Mvp`
   while still proving real source-mutation capability.
4. HttpClient pool size / timeout (5s recommended) — TBD in
   writing-plans, defaults to `Timeout = TimeSpan.FromSeconds(5)`,
   `MaxConnectionsPerServer = 4`.

---

## 12. References

- 施工包原文: `docs/FSPM/《FSPM MCP：极致 AI 工程生产力与 JNPF 首个闭环施工包》.md`
- Baseline: `docs/FSPM/MVP_BASELINE.md`, `docs/FSPM/MVP_TEST_RESULT.md`
- Roadmap: `docs/FSPM/EXECUTION_ROADMAP.md` (FSPM-04 → FSPM-18, deferred)
- FSPM Diagnostic Property Protocol: `backend/modularity/Foundry.FSPM.Core/Evidence/FspmDiagnosticProperties.cs`
- SDK Blocker skill: `~/.workbuddy/skills/dotnet-sdk-nuget-blocker.md`
- Memory: `D:/JNPF-v52/.workbuddy/memory/MEMORY.md`

---

**End of design spec. Awaiting user review before invoking writing-plans.**