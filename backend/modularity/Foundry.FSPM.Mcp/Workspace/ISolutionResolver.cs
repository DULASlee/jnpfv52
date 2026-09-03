// =============================================================================
//  Foundry.FSPM.Mcp — Workspace/ISolutionResolver
// =============================================================================
//
//  MCP-07-02: solution discovery within a workspace root (TopDirectoryOnly,
//  deterministic). Zero or ambiguous solutions are structured failures,
//  never exceptions.
// =============================================================================

namespace Foundry.FSPM.Mcp.Workspace;

internal sealed class SolutionInfo
{
    internal SolutionInfo(string solutionPath, string[] allCandidates)
    {
        SolutionPath = solutionPath;
        AllCandidates = allCandidates;
    }

    public string SolutionPath { get; }
    public string[] AllCandidates { get; }
}

internal sealed class SolutionOutcome
{
    private SolutionOutcome(bool isResolved, SolutionInfo? solution, string field, string message)
    {
        IsResolved = isResolved;
        Solution = solution;
        Field = field;
        Message = message;
    }

    public bool IsResolved { get; }
    public SolutionInfo? Solution { get; }
    public string Field { get; }
    public string Message { get; }

    public static SolutionOutcome Ok(SolutionInfo solution) =>
        new(true, solution, string.Empty, string.Empty);

    public static SolutionOutcome Fail(string field, string message) =>
        new(false, null, field, message);
}

internal interface ISolutionResolver
{
    SolutionOutcome Resolve(string workspaceRoot);
}
