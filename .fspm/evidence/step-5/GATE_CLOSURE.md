# STEP 5 Build Gate Closure — Final Report

**Commit:** `3b144f97` on `feature/fspm-mcp-stdio-adapter`
**Date:** 2026-09-03 19:30 (GMT+8)
**Branch:** `feature/fspm-mcp-stdio-adapter`
**HEAD:** `3b144f97`

---

## Final Status (裁决令 V2 §十一格式)

```
CS0103 (McpBoundaryRunner stale ref) = FIXED
IsTestProject                        = FIXED
Microsoft.NET.Test.Sdk               = FIXED
ModelContextProtocol PackageReference  = FIXED

MCP Restore = PASS
MCP Build  = PASS (0w 0e 3.02s)
MCP Tests Build = PASS (0w 0e 2.75s)
MCP Tests = 9 total | 6 PASS | 3 FAIL

MCP Server Startup = PASS
Exactly 3 Tools  = PASS (fspm_understand, fspm_construct, fspm_verify)
stdout Boundary   = PASS (protocol-clean, no contamination)
Awaiting Contract = PARTIAL (6/9 facts PASS)

M8 BUILD GATE    = PARTIAL (6/9 tests pass)
STEP 5           = NOT_COMPLETE (3 AwaitingContractTests failing)
MCP BOUNDARY     = LOCKED (6 core protocol facts verified, no regression)
```

---

## Defect Classification

```
Defect: Program.cs retained stale reference to deleted McpBoundaryRunner.
Classification: Source regression / stale reference.
Environment: NOT ROOT CAUSE.
Repair: Removed obsolete --mcp-boundary-test path.
Verification: Build/Test/E2E results below.
```

---

## Test Results (dotnet test)

**Command:** `dotnet test Foundry.FSPM.Mcp.Tests.csproj -c Debug --no-build`

```
Total:   9
Passed:  6  (McpServerStarts, ExactlyThreeToolsAreRegistered,
               Tool_Understand_IsAvailable, Tool_Construct_IsAvailable,
               Tool_Verify_IsAvailable, McpStdoutIsProtocolClean)
Failed:  3  (Understand_AwaitingUpstreamContract,
               Construct_AwaitingUpstreamContract,
               Verify_AwaitingUpstreamContract)
Skipped: 0
Duration: 7.28s
Exit:    1
Evidence: .fspm/evidence/step-5/test-output3.txt
```

### 3 Failure Analysis

Each `AwaitingContractTests` calls `Client.CallToolAsync(...)` and asserts `result.IsError == false`. All three fail because:

```
Understand/Construct/Verify tools: argument validation throws
ArgumentException → MCP error response → IsError = null (not false)
```

**Root cause:** Tool stubs validate arguments with `throw new ArgumentException(...)` but this propagates as an MCP error response. The stub implementations correctly return `AWAITING_COMPILER` JSON only in the non-exception path. Since all AwaitingContractTests pass non-existent/invalid paths that trigger validation before reaching the stub body, they fail with `IsError = null`.

**Fix required:** Either (a) change Tool parameter validation to return structured error JSON instead of throwing, or (b) update AwaitingContractTests to use valid parameter values that reach the `AWAITING_COMPILER` return path.

**This is NOT a build or environment issue.** The MCP server starts, discovers exactly 3 tools, and responds to protocol correctly. The failure is in stub implementation details only.

---

## Changed Files (20 files, +1511 lines)

| File | Change |
|---|---|
| `backend/modularity/Foundry.FSPM.Mcp/Program.cs` | Removed --mcp-boundary-test dead ref (CS0103 fix) |
| `backend/modularity/Foundry.FSPM.Mcp/Foundry.FSPM.Mcp.csproj` | New MCP SDK 2.2.0 project |
| `backend/modularity/Foundry.FSPM.Mcp/Mcp/FspmUnderstandTool.cs` | Stub: returns AWAITING_COMPILER |
| `backend/modularity/Foundry.FSPM.Mcp/Mcp/FspmConstructTool.cs` | Stub: returns AWAITING_COMPILER |
| `backend/modularity/Foundry.FSPM.Mcp/Mcp/FspmVerifyTool.cs` | Stub: returns AWAITING_COMPILER |
| `backend/modularity/Foundry.FSPM.Mcp/NuGet.Config` | NuGet config |
| `backend/modularity/Foundry.FSPM.Mcp/SelfVerification_PhaseA1.1.md` | Phase A1.1 self-verification |
| `backend/tests/Foundry.FSPM.Mcp.Tests/Foundry.FSPM.Mcp.Tests.csproj` | IsTestProject + Test.Sdk + MCP PackageReference |
| `backend/tests/Foundry.FSPM.Mcp.Tests/McpBoundaryTests.cs` | 6 boundary tests (all PASS) |
| `backend/tests/Foundry.FSPM.Mcp.Tests/AwaitingContractTests.cs` | 3 contract tests (all FAIL) |
| `backend/tests/Foundry.FSPM.Mcp.Tests/Directory.Build.props` | Test props |
| `backend/tests/Foundry.FSPM.Mcp.Tests/gen-assets.py` | Test assets generator |
| `.fspm/evidence/env-context-compare/2026-09-03-1841/*` | Environment diagnostic evidence |
| `.fspm/evidence/step-5/test-output3.txt` | Test output (6 PASS / 3 FAIL) |

---

## Evidence Paths

| Evidence | Path |
|---|---|
| Test output | `.fspm/evidence/step-5/test-output3.txt` |
| Environment diagnostic | `.fspm/evidence/env-context-compare/2026-09-03-1841/REPORT.md` |
| MCP build output | (console, 0w 0e) |
| Tests build output | (console, 0w 0e) |

---

## Commitment

`3b144f97` on `feature/fspm-mcp-stdio-adapter`

Awaiting Architect decision on:
1. 3 AwaitingContractTests stub implementation fix
2. Whether to continue MCP tool implementation or defer to Compiler AI (FSPM-04..18)

Per 裁决令 V2 §十: **STOP / REPORT / WAIT** — no further MCP work without authorization.