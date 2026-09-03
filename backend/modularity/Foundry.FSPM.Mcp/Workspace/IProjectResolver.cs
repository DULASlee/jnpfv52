// =============================================================================
//  Foundry.FSPM.Mcp — Workspace/IProjectResolver
// =============================================================================
//
//  MCP-07-03: project resolution + TargetFramework extraction (lightweight
//  csproj XML read — NOT C# parsing; semantic resolution stays in Core).
//  Malformed project XML is a structured failure; a missing
//  <TargetFramework> yields null (lenient — Build decides later).
// =============================================================================

namespace Foundry.FSPM.Mcp.Workspace;

internal sealed class ProjectInfo
{
    internal ProjectInfo(string projectPath, string projectName, string? targetFramework)
    {
        ProjectPath = projectPath;
        ProjectName = projectName;
        TargetFramework = targetFramework;
    }

    public string ProjectPath { get; }
    public string ProjectName { get; }
    public string? TargetFramework { get; }
}

internal sealed class ProjectOutcome
{
    private ProjectOutcome(bool isResolved, ProjectInfo? project, string field, string message)
    {
        IsResolved = isResolved;
        Project = project;
        Field = field;
        Message = message;
    }

    public bool IsResolved { get; }
    public ProjectInfo? Project { get; }
    public string Field { get; }
    public string Message { get; }

    public static ProjectOutcome Ok(ProjectInfo project) =>
        new(true, project, string.Empty, string.Empty);

    public static ProjectOutcome Fail(string field, string message) =>
        new(false, null, field, message);
}

internal interface IProjectResolver
{
    ProjectOutcome Resolve(
        string workspaceRoot,
        string? projectName,
        string? solutionPath = null);
}
