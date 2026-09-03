// =============================================================================
//  Foundry.FSPM.Mcp — fspm_verify MCP Tool
// =============================================================================
//
//  Phase A2 — STUB awaiting Foundry.FSPM.Analyzer + Foundry.FSPM.Core
//  delivery by the Compiler AI (FSPM-04 → FSPM-18 in EXECUTION_ROADMAP.md).
//
//  Per Architect §六: "MCP 只作为 Adapter"。
//  Per Architect §九 禁止 3: "MCP AI 不得修改 Compiler / Parser / Binder /
//  SemanticResolver / Analyzer"。
//
//  The interface signature, 8-segment verification (Semantic / Architecture /
//  Security / UI / Build / Test / Runtime / Evidence), the Decision vs Stage
//  orthogonality, and the FailureStage contract are FROZEN per
//  Spec v2 §3.4 + §5 + INTERFACE_LOCKDOWN.md §1 + §2.
//
//  This Tool is a STDO-ONLY adapter; its body wires the 8 segments to the
//  frozen contracts once Compiler publishes them.
//
//  V6.1 MCP-05-01: parameter validation no longer throws ArgumentException
//  across the Transport boundary (which surfaced as IsError=null and broke
//  AwaitingContractTests). Invalid input now returns a structured
//  INVALID_REQUEST CallToolResult; the success path returns CallToolResult
//  with explicit IsError=false. Envelope shapes are unchanged.
// =============================================================================

using System.ComponentModel;
using Foundry.FSPM.Mcp.Execution;
using Foundry.FSPM.Mcp.Validation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Foundry.FSPM.Mcp.Mcp;

[McpServerToolType]
public static class FspmVerifyTool
{
    // MCP-06-02: all Tools validate through the shared validator.
    private static readonly IMcpRequestValidator Validator = new McpRequestValidator();

    [McpServerTool(Name = "fspm_verify")]
    [Description(
        "Verifies an FSPM operation across 8 independent segments: Semantic, "
        + "Architecture, Security, UI, Build, Test, Runtime, Evidence. Returns "
        + "VerificationResult with Decision vs Stage orthogonal to runtime "
        + "failure stage (BUILD/TEST/RUNTIME/EVIDENCE/CONSTRUCT).")]
    public static Task<CallToolResult> Verify(
        [Description("Workspace root.")] string workspaceRoot,
        [Description("Qualified operation, e.g. User.Login.")] string operation,
        [Description("Absolute path of the project that must build.")]
        string projectPath,
        [Description("Absolute path of the test project that must pass tests.")]
        string testPath,
        [Description("Base URL of the runtime target, e.g. http://localhost:5099.")]
        string loginMvpBaseUrl,
        [Description("Execution ID produced by fspm_construct.")]
        string executionId)
    {
        var workspaceCheck = Validator.ValidateRequired("workspaceRoot", workspaceRoot);
        if (!workspaceCheck.IsValid)
            return Task.FromResult(McpOperationResult.InvalidRequest(workspaceCheck));
        var operationCheck = Validator.ValidateQualifiedName("operation", operation);
        if (!operationCheck.IsValid)
            return Task.FromResult(McpOperationResult.InvalidRequest(operationCheck));
        var executionCheck = Validator.ValidateRequired("executionId", executionId);
        if (!executionCheck.IsValid)
            return Task.FromResult(McpOperationResult.InvalidRequest(executionCheck));

        // 8-segment verification requires (currently NOT in build):
        //   1. Semantic     — Foundry.FSPM.Core.Semantic.SemanticResolver  (Compiler AI)
        //   2. Architecture — Foundry.FSPM.Analyzer.FspmArchitectureAnalyzer (Compiler AI)
        //   3. Security     — Foundry.FSPM.Analyzer.FspmSecurityAnalyzer    (Compiler AI)
        //   4. UI           — Foundry.FSPM.Analyzer.FspmUiAnalyzer           (Compiler AI)
        //   5. Build        — Process/dotnet build (MCP local; SDK blocker)
        //   6. Test         — Process/dotnet test (MCP local; SDK blocker)
        //   7. Runtime      — HttpClient to Foundry.FSPM.Login.Mvp (MCP local)
        //   8. Evidence     — Foundry.FSPM.Core.Evidence.IFspmEvidenceCollector (Compiler AI)
        //
        // Of these, segments 1–4 + 8 require Compiler AI deliveries
        // (FSPM-04/05/06/07/08). Segments 5/6/7 are MCP-local but blocked by
        // the .NET 8 SDK Windows-container incompatibility (see
        // .fspm/evidence/mcp-reentry-checkpoint/checkpoint.md M8).
        //
        // To respect Architect §六 + §九 禁止 3, this Tool returns an
        // explicit AWAITING_COMPILER envelope instead of fabricating
        // verification.

        var result = new
        {
            status = "AWAITING_COMPILER",
            executionId,
            operation,
            workspaceRoot,
            segments = new
            {
                semantic = new { status = "NOT_RUN", reason = "Compiler FSPM-07/08 not delivered" },
                architecture = new { status = "NOT_RUN", reason = "Compiler FSPM-09/10 not delivered" },
                security = new { status = "NOT_RUN", reason = "Compiler FSPM-11 not delivered" },
                ui = new { status = "NOT_RUN", reason = "Compiler FSPM-12 not delivered" },
                build = new { status = "NOT_RUN", reason = ".NET 8 SDK Windows-container incompatibility (see checkpoint.md M8)" },
                test = new { status = "NOT_RUN", reason = "depends on Build" },
                runtime = new { status = "NOT_RUN", reason = "depends on Build + Foundry.FSPM.Login.Mvp presence" },
                evidence = new { status = "NOT_RUN", reason = "Compiler FSPM-15/16 not delivered" },
            },
            frozenContract = new
            {
                ruleDecisions = "Pass / Violation / NotApplicable / Unknown (orthogonal to FailureStage)",
                failureStage = "BUILD | TEST | RUNTIME | EVIDENCE | CONSTRUCT | null",
                evidenceSchema = "Foundry.FSPM.Core.Evidence.FspmVerificationEvidence (INTERFACE_LOCKDOWN §1.1)",
                closedCondition = "all 8 segments Status==PASS && FailureStage==null",
            },
        };

        return Task.FromResult(McpOperationResult.Success(result));
    }
}
