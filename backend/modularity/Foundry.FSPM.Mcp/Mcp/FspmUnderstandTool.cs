// =============================================================================
//  Foundry.FSPM.Mcp — fspm_understand MCP Tool
// =============================================================================
//
//  Phase A2 — STUB awaiting Foundry.FSPM.Core.Semantic.SemanticResolver delivery
//  by the Compiler AI (FSPM-07/08 in EXECUTION_ROADMAP.md).
//
//  Per Architect §六: "MCP 只作为 Adapter，不重新实现 Compiler 的 Semantic"。
//
//  The interface signature, parameter descriptions, and tool name are FROZEN
//  per Spec v2 §3.2 — once Compiler delivers SemanticResolver, the
//  gateway body wired here is the only thing that changes.
//
//  V6.1 MCP-06-07: thin pipeline caller. Stages:
//  Validate (shared Validator) → Context → Workspace → Gateway(stub) →
//  Projection → Response. Exception boundary owned by the pipeline.
// =============================================================================

using System.ComponentModel;
using Foundry.FSPM.Mcp.Execution;
using Foundry.FSPM.Mcp.Validation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Foundry.FSPM.Mcp.Mcp;

[McpServerToolType]
public static class FspmUnderstandTool
{
    private static readonly IMcpRequestValidator Validator = new McpRequestValidator();

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
        var pipeline = new McpExecutionPipeline();
        return pipeline.ExecuteAsync(
            toolName: "fspm_understand",
            workspaceRoot: workspaceRoot,
            request: new { workspaceRoot, target },
            validate: () => McpValidationResult.FirstInvalid(
                Validator.ValidateRequired("workspaceRoot", workspaceRoot),
                Validator.ValidateQualifiedName("target", target)),
            invoke: (_, _) => Task.FromResult<object>(BuildAwaitingPayload(workspaceRoot, target)));
    }

    private static object BuildAwaitingPayload(string workspaceRoot, string target)
    {
        // Architect §六: "MCP 不得自己重新解析、绑定、Semantic Model"。
        // The contract (INTERFACE_LOCKDOWN.md §1.3) freezes
        //   Foundry.FSPM.Core.Semantic.SemanticResolver
        //   Foundry.FSPM.Core.Semantic.SourceLocation
        //   Foundry.FSPM.Core.Semantic.SemanticIdentity
        // as the canonical resolution path (Compiler: FSPM-07/08).
        // Shape below is FROZEN per Spec v2 §3.2.
        return new
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
    }
}
