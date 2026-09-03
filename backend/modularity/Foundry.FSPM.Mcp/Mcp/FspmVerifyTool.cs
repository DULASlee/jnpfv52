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
//  Interface signature, 8-segment grid, Decision vs Stage orthogonality and
//  FailureStage contract are FROZEN per Spec v2 §3.4 + §5 + INTERFACE_LOCKDOWN
//  §1 + §2.
//
//  V6.1 MCP-06-07: thin pipeline caller (Validate → Context → Workspace →
//  Gateway(stub) → Projection → Response). Exception boundary owned by
//  the pipeline.
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
        var pipeline = new McpExecutionPipeline();
        return pipeline.ExecuteAsync(
            toolName: "fspm_verify",
            workspaceRoot: workspaceRoot,
            request: new { workspaceRoot, operation, projectPath, testPath, loginMvpBaseUrl, executionId },
            validate: () => McpValidationResult.FirstInvalid(
                Validator.ValidateRequired("workspaceRoot", workspaceRoot),
                Validator.ValidateQualifiedName("operation", operation),
                Validator.ValidateRequired("executionId", executionId)),
            invoke: (_, _) => Task.FromResult<object>(BuildAwaitingPayload(
                workspaceRoot, operation, executionId)));
    }

    private static object BuildAwaitingPayload(string workspaceRoot, string operation, string executionId)
    {
        // Shape FROZEN per Spec v2 §3.4 + §5 + INTERFACE_LOCKDOWN §1 + §2.
        return new
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
    }
}
