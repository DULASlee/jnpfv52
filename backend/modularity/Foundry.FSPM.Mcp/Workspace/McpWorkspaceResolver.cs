// =============================================================================
//  Foundry.FSPM.Mcp — Workspace/McpWorkspaceResolver
// =============================================================================
//
//  MCP-06-05: default implementation. Pure filesystem location:
//  normalize → existence check → optional solution/project hint lookup.
//  All paths returned as absolute full paths.
// =============================================================================

namespace Foundry.FSPM.Mcp.Workspace;

internal sealed class McpWorkspaceResolver : IMcpWorkspaceResolver
{
    public McpWorkspaceOutcome Resolve(
        string? workspaceRoot,
        string? solutionName = null,
        string? projectName = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return McpWorkspaceOutcome.Fail("workspaceRoot", "workspaceRoot is required.");

        string root;
        try
        {
            root = Path.GetFullPath(workspaceRoot);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException)
        {
            return McpWorkspaceOutcome.Fail("workspaceRoot", $"workspaceRoot is not a valid path: {ex.Message}");
        }

        if (!Directory.Exists(root))
            return McpWorkspaceOutcome.Fail("workspaceRoot", $"workspace root does not exist: {root}");

        string? solutionPath = null;
        if (!string.IsNullOrWhiteSpace(solutionName))
        {
            solutionPath = LocateFile(root, solutionName, ".sln");
            if (solutionPath is null)
                return McpWorkspaceOutcome.Fail("solution", $"solution not found under workspace: {solutionName}");
        }

        string? projectPath = null;
        if (!string.IsNullOrWhiteSpace(projectName))
        {
            string searchRoot = solutionPath is not null
                ? Path.GetDirectoryName(solutionPath)!
                : root;
            projectPath = LocateFile(searchRoot, projectName, ".csproj");
            if (projectPath is null)
                return McpWorkspaceOutcome.Fail("project", $"project not found under workspace: {projectName}");
        }

        return McpWorkspaceOutcome.Ok(new ResolvedWorkspace(root, solutionPath, projectPath));
    }

    private static string? LocateFile(string searchRoot, string hint, string extension)
    {
        string trimmed = hint.Trim();
        string[] candidates = Path.IsPathRooted(trimmed)
            ? new[] { trimmed }
            : new[]
            {
                Path.Combine(searchRoot, trimmed),
                Path.Combine(searchRoot, trimmed + extension),
            };

        foreach (string candidate in candidates)
        {
            string full;
            try
            {
                full = Path.GetFullPath(candidate);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException)
            {
                continue;
            }

            if (File.Exists(full))
                return full;
        }

        return null;
    }
}
