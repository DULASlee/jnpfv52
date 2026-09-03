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
//  V6.1 MCP-05-01: parameter validation no longer throws ArgumentException
//  across the Transport boundary (which surfaced as IsError=null and broke
//  AwaitingContractTests). Invalid input now returns a structured
//  INVALID_REQUEST CallToolResult; the success path returns CallToolResult
//  with explicit IsError=false. Envelope shapes are unchanged.
// =============================================================================

using System.ComponentModel;
using System.Text.Json;
using Foundry.FSPM.Mcp.Validation;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Foundry.FSPM.Mcp.Mcp;

[McpServerToolType]
public static class FspmUnderstandTool
{
    // MCP-06-02: all Tools validate through the shared validator.
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
        var workspaceCheck = Validator.ValidateRequired("workspaceRoot", workspaceRoot);
        if (!workspaceCheck.IsValid)
            return Task.FromResult(InvalidRequest(workspaceCheck));
        var targetCheck = Validator.ValidateQualifiedName("target", target);
        if (!targetCheck.IsValid)
            return Task.FromResult(InvalidRequest(targetCheck));

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

        string json = JsonSerializer.Serialize(
            result,
            new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(new CallToolResult
        {
            IsError = false,
            Content = new List<ContentBlock> { new TextContentBlock { Text = json } },
        });
    }

    private static CallToolResult InvalidRequest(McpValidationResult validation)
    {
        string json = JsonSerializer.Serialize(
            new { status = "INVALID_REQUEST", field = validation.Field, message = validation.Message },
            new JsonSerializerOptions { WriteIndented = true });
        return new CallToolResult
        {
            IsError = true,
            Content = new List<ContentBlock> { new TextContentBlock { Text = json } },
        };
    }
}
