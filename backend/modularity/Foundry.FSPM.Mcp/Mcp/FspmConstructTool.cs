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
//  The interface signature, parameter descriptions, ConstructionEvidence
//  fields (target, changedFiles, beforeFingerprint, afterFingerprint, diffSummary,
//  writerTransactionId, status, reason), and the beforeFp != afterFp invariant
//  are FROZEN per Spec v2 §3.3 + §3.5 + INTERFACE_LOCKDOWN.md §2.
//
//  Once Compiler delivers the construction contract, SourceWriter (Phase A1
//  already created) is the only thing that needs to be wired here.
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
public static class FspmConstructTool
{
    // MCP-06-02: all Tools validate through the shared validator.
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
        var workspaceCheck = Validator.ValidateRequired("workspaceRoot", workspaceRoot);
        if (!workspaceCheck.IsValid)
            return Task.FromResult(McpOperationResult.InvalidRequest(workspaceCheck));
        var operationCheck = Validator.ValidateQualifiedName("operation", operation);
        if (!operationCheck.IsValid)
            return Task.FromResult(McpOperationResult.InvalidRequest(operationCheck));

        // The actual mutation pipeline requires Foundry.FSPM.Core.Semantic
        // symbol resolution (which Foundry.FSPM.Core does not yet provide)
        // and Foundry.FSPM.Mcp.Construction.SourceWriter (already present at
        // Phase A1.1, awaiting integration with the resolved symbol).
        //
        // To respect Architect §六 (no parallel semantic/binding layer in MCP),
        // this Tool returns an explicit AWAITING_COMPILER envelope. Real
        // mutation will be activated once Compiler delivers the binding
        // contract per FSPM-07/08.

        var result = new
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

        return Task.FromResult(McpOperationResult.Success(result));
    }
}
