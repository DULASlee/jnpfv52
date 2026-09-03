// =============================================================================
//  Foundry.FSPM.Mcp — fspm_understand MCP Tool
// =============================================================================
//
//  Phase A2 — STUB awaiting Foundry.FSPM.Core.Semantic.SemanticResolver delivery
//  by the Compiler AI (FSPM-07/08 in EXECUTION_ROADMAP.md).
//
//  Per Architect §六: "MCP 只作为 Adapter，不重新实现 Compiler 的 Semantic"。
//  This Tool is a STDO-ONLY adapter; its body forwards to Compiler API which
//  is not yet present in the build (Foundry.FSPM.Core currently contains only
//  an empty .csproj per FSPM-01 baseline).
//
//  The interface signature, parameter descriptions, and tool name are FROZEN
//  per Spec v2 §3.2 — once Compiler delivers SemanticResolver, the
//  implementation in this file is the only thing that changes.
//
//  Until then, this Tool returns a structured "AWAITING_COMPILER" response
//  so MCP clients can detect the state programmatically.
//
//  V6.1 MCP-05-01: validation returns INVALID_REQUEST envelopes (never throws
//  across Transport); success returns CallToolResult with explicit IsError=false.
//  V6.1 MCP-06-02: validation via shared IMcpRequestValidator.
//  V6.1 MCP-06-03: envelopes via internal McpOperationResult.
//  V6.1 MCP-06-04: whole body wrapped — escaped exceptions go to
//  IMcpExceptionMapper, never raw across Transport.
// =============================================================================

using System.ComponentModel;
using Foundry.FSPM.Mcp.Errors;
using Foundry.FSPM.Mcp.Execution;
using Foundry.FSPM.Mcp.Validation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Foundry.FSPM.Mcp.Mcp;

[McpServerToolType]
public static class FspmUnderstandTool
{
    private static readonly IMcpRequestValidator Validator = new McpRequestValidator();
    private static readonly IMcpExceptionMapper ExceptionMapper = new McpExceptionMapper();

    [McpServerTool(Name = "fspm_understand")]
    [Description(
        "Resolves an FSPM semantic target (e.g. User, User.UserName, User.Login) "
        + "inside a real .NET workspace. Returns RESOLVED + real symbol location.")]
    public static Task<CallToolResult> Understand(
        [Description("Absolute path of the real workspace root.")]
        string workspaceRoot,

        [Description("Target, e.g. User.Login.")]
        string target)
    {
        try
        {
            var workspaceCheck = Validator.ValidateRequired("workspaceRoot", workspaceRoot);
            if (!workspaceCheck.IsValid)
                return Task.FromResult(McpOperationResult.InvalidRequest(workspaceCheck));
            var targetCheck = Validator.ValidateQualifiedName("target", target);
            if (!targetCheck.IsValid)
                return Task.FromResult(McpOperationResult.InvalidRequest(targetCheck));

            // Architect §六: "MCP 不得自己重新解析、绑定、Semantic Model"。
            // Architect §三: "等新的 MCP Worktree 建成后重新进入" + "MCP must work through
            // frozen Compiler/semantic contract"。
            //
            // The contract (INTERFACE_LOCKDOWN.md §1.3) freezes
            //   Foundry.FSPM.Core.Semantic.SemanticResolver
            //   Foundry.FSPM.Core.Semantic.SourceLocation
            //   Foundry.FSPM.Core.Semantic.SemanticIdentity
            // as the canonical resolution path. Foundry.FSPM.Core currently has
            // an empty .csproj only; the Compiler is producing the implementation
            // (EXECUTION_ROADMAP FSPM-07/08).
            //
            // To avoid creating a parallel Semantic Model, this Tool returns an
            // explicit "AWAITING_COMPILER" envelope rather than a fake resolution.

            var result = new
            {
                status = "AWAITING_COMPILER",
                workspaceRoot,
                target,
                message =
                    "Foundry.FSPM.Core.Semantic.SemanticResolver is not yet delivered by "
                    + "the Compiler AI. Once Compiler publishes FSPM-07/08 (Semantic "
                    + "Core), this Tool will resolve the target to a real "
                    + "SemanticIdentity + SourceLocation. See "
                    + "docs/superpowers/specs/2026-09-03-fspm-mcp-stdio-adapter-design.md "
                    + "§3.2 and docs/FSPM/INTERFACE_LOCKDOWN.md §1.3.",
                expectedContract = new
                {
                    resolverType = "Foundry.FSPM.Core.Semantic.SemanticResolver",
                    method = "ResolveProjectAsync(string projectPath, CancellationToken)",
                    resultType = "Foundry.FSPM.Core.Semantic.SemanticModel",
                },
            };

            return Task.FromResult(McpOperationResult.Success(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ExceptionMapper.Map(ex, "fspm_understand"));
        }
    }
}
