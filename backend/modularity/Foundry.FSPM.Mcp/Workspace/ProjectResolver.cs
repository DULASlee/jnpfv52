// =============================================================================
//  Foundry.FSPM.Mcp — Workspace/ProjectResolver
// =============================================================================
//
//  MCP-07-03: default implementation. Name lookup reuses McpWorkspaceResolver
//  (no duplicated locate logic); the null-name path accepts exactly one
//  top-level *.csproj. TargetFramework comes from a guarded XML read.
// =============================================================================

using System.Xml.Linq;

namespace Foundry.FSPM.Mcp.Workspace;

internal sealed class ProjectResolver : IProjectResolver
{
    private readonly IMcpWorkspaceResolver _workspace;

    public ProjectResolver(IMcpWorkspaceResolver? workspace = null)
    {
        _workspace = workspace ?? new McpWorkspaceResolver();
    }

    public ProjectOutcome Resolve(
        string workspaceRoot,
        string? projectName,
        string? solutionPath = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return ProjectOutcome.Fail("workspaceRoot", "workspaceRoot is required.");

        string searchRoot = workspaceRoot;
        if (!string.IsNullOrWhiteSpace(solutionPath))
        {
            string? solutionDir = Path.GetDirectoryName(Path.GetFullPath(solutionPath));
            if (solutionDir is null || !Directory.Exists(solutionDir))
                return ProjectOutcome.Fail("solution", $"solution directory does not exist: {solutionPath}");
            searchRoot = solutionDir;
        }
        else if (!Directory.Exists(workspaceRoot))
        {
            return ProjectOutcome.Fail("workspaceRoot", $"workspace root does not exist: {workspaceRoot}");
        }

        string? projectPath;
        if (string.IsNullOrWhiteSpace(projectName))
        {
            string[] candidates;
            try
            {
                candidates = Directory.GetFiles(searchRoot, "*.csproj", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return ProjectOutcome.Fail("project", $"cannot enumerate workspace: {ex.Message}");
            }

            if (candidates.Length == 0)
                return ProjectOutcome.Fail("project", $"no project file found under: {searchRoot}");
            Array.Sort(candidates, StringComparer.OrdinalIgnoreCase);
            if (candidates.Length > 1)
            {
                return ProjectOutcome.Fail(
                    "project",
                    $"ambiguous projects under {searchRoot}: {string.Join(", ", candidates.Select(Path.GetFileName))}");
            }

            projectPath = candidates[0];
        }
        else
        {
            var located = _workspace.Resolve(searchRoot, projectName: projectName);
            if (!located.IsResolved || located.Workspace?.ProjectPath is null)
                return ProjectOutcome.Fail("project", located.Message);
            projectPath = located.Workspace.ProjectPath;
        }

        string? targetFramework = ReadTargetFramework(projectPath);
        if (targetFramework is null && !IsWellFormedXml(projectPath))
            return ProjectOutcome.Fail("project", $"project file is not well-formed XML: {projectPath}");

        return ProjectOutcome.Ok(new ProjectInfo(
            projectPath,
            Path.GetFileNameWithoutExtension(projectPath),
            targetFramework));
    }

    private static string? ReadTargetFramework(string projectPath)
    {
        try
        {
            var doc = XDocument.Load(projectPath);
            return doc.Descendants("TargetFramework").FirstOrDefault()?.Value.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsWellFormedXml(string projectPath)
    {
        try
        {
            _ = XDocument.Load(projectPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
