// =============================================================================
//  Foundry.FSPM.Mcp — Workspace/IMcpWorkspaceResolver
// =============================================================================
//
//  MCP-06-05: filesystem-level workspace location ONLY. No C# parsing,
//  no compilation, no semantic resolution here — that is Core's job
//  (Architect §六). Existence checks that fail here produce structured
//  failures; they never throw across the Transport boundary (the Tools
//  route them through McpOperationResult / McpExceptionMapper).
// =============================================================================

namespace Foundry.FSPM.Mcp.Workspace;

/// <summary>
/// Located workspace paths. Null Solution/Project means "no hint given".
/// </summary>
internal sealed class ResolvedWorkspace
{
    internal ResolvedWorkspace(string rootPath, string? solutionPath, string? projectPath)
    {
        RootPath = rootPath;
        SolutionPath = solutionPath;
        ProjectPath = projectPath;
    }

    public string RootPath { get; }
    public string? SolutionPath { get; }
    public string? ProjectPath { get; }
}

/// <summary>
/// Outcome of a workspace resolution attempt.
/// </summary>
internal sealed class McpWorkspaceOutcome
{
    private McpWorkspaceOutcome(
        bool isResolved, ResolvedWorkspace? workspace, string field, string message)
    {
        IsResolved = isResolved;
        Workspace = workspace;
        Field = field;
        Message = message;
    }

    public bool IsResolved { get; }
    public ResolvedWorkspace? Workspace { get; }
    public string Field { get; }
    public string Message { get; }

    public static McpWorkspaceOutcome Ok(ResolvedWorkspace workspace) =>
        new(true, workspace, string.Empty, string.Empty);

    public static McpWorkspaceOutcome Fail(string field, string message) =>
        new(false, null, field, message);
}

internal interface IMcpWorkspaceResolver
{
    McpWorkspaceOutcome Resolve(
        string? workspaceRoot,
        string? solutionName = null,
        string? projectName = null);
}
