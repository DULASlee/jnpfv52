// =============================================================================
//  Foundry.FSPM.Mcp — fspm_construct MCP Tool
// =============================================================================
//
//  Phase A2 — STUB awaiting Foundry.FSPM.Core.Semantic.Symbol resolution
//  delivery by the Compiler AI (FSPM-07/08 in EXECUTION_ROADMAP.md).
//
//  Per Architect §六: "MCP 只作为 Adapter，不重新实现 Compiler 的 Construction"。
//  Per Architect §九 禁止 3: "MCP AI 不得自己实现 SymbolBinder / SemanticResolver"。
//
//  Interface signature, parameter descriptions, ConstructionEvidence fields
//  and the beforeFp != afterFp invariant are FROZEN per Spec v2 §3.3 + §3.5
//  + INTERFACE_LOCKDOWN.md §2.
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
public static class FspmConstructTool
{
    private static readonly IMcpRequestValidator Validator = new McpRequestValidator();

    [McpServerTool(Name = "fspm_construct")]
    [Description(
        "Performs REAL source mutation for an FSPM operation and returns a "
        + "ConstructionEvidence record (target, changedFiles, beforeFingerprint, "
        + "afterFingerprint, writerTransactionId, status, reason).")]
    public static Task<CallToolResult> Construct(
        [Description("Workspace root.")] string workspaceRoot,
        [Description("Qualified operation, e.g. User.Login.")] string operation,
        [Description("Human-readable construction instruction.")] string instruction)
    {
        var pipeline = new McpExecutionPipeline();
        return pipeline.ExecuteAsync(
            toolName: "fspm_construct",
            workspaceRoot: workspaceRoot,
            request: new { workspaceRoot, operation, instruction },
            validate: () => McpValidationResult.FirstInvalid(
                Validator.ValidateRequired("workspaceRoot", workspaceRoot),
                Validator.ValidateQualifiedName("operation", operation)),
            invoke: (_, _) => Task.FromResult<object>(
                BuildAwaitingPayload(workspaceRoot, operation, instruction)));
    }

    private static object BuildAwaitingPayload(string workspaceRoot, string operation, string instruction)
    {
        // Shape FROZEN per Spec v2 §3.3 + §3.5 + INTERFACE_LOCKDOWN §2.
        return new
        {
            status = "AWAITING_COMPILER",
            workspaceRoot,
            operation,
            instruction,
            message =
                "fspm_construct needs Foundry.FSPM.Core.Semantic symbol resolution "
                + "to map operation → SourceLocation. That resolution is owned by "
                + "the Compiler AI (FSPM-07/08). SourceWriter (Phase A1.1) is in "
                + "place and will be wired here as soon as the symbol resolution is "
                + "delivered. Per INTERFACE_LOCKDOWN.md §2, the MCP side must not "
                + "implement its own SemanticResolver / SymbolBinder.",
            contractRequired = new
            {
                inputContract = "operation: string (e.g. User.Login)",
                expectedResolution = "Foundry.FSPM.Core.Semantic.SemanticIdentity + SourceLocation",
                mutationTarget = "Foundry.FSPM.Mcp.Construction.SourceWriter (Phase A1.1, ready)",
                evidenceFields = "target, changedFiles, beforeFingerprint, afterFingerprint, "
                    + "writerTransactionId, status, reason (Spec v2 §3.5 + INTERFACE_LOCKDOWN §2)",
            },
        };
    }
}
