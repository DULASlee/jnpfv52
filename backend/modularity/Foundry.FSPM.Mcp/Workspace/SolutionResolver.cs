// =============================================================================
//  Foundry.FSPM.Mcp — Workspace/SolutionResolver
// =============================================================================
//
//  MCP-07-02: default implementation. TopDirectoryOnly *.sln enumeration:
//  exactly-one wins; zero/ambiguous are structured failures.
// =============================================================================

namespace Foundry.FSPM.Mcp.Workspace;

internal sealed class SolutionResolver : ISolutionResolver
{
    public SolutionOutcome Resolve(string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return SolutionOutcome.Fail("workspaceRoot", "workspaceRoot is required.");

        string root;
        try
        {
            root = Path.GetFullPath(workspaceRoot);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException)
        {
            return SolutionOutcome.Fail("workspaceRoot", $"workspaceRoot is not a valid path: {ex.Message}");
        }

        if (!Directory.Exists(root))
            return SolutionOutcome.Fail("workspaceRoot", $"workspace root does not exist: {root}");

        string[] candidates;
        try
        {
            candidates = Directory.GetFiles(root, "*.sln", SearchOption.TopDirectoryOnly);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            return SolutionOutcome.Fail("solution", $"cannot enumerate workspace: {ex.Message}");
        }

        if (candidates.Length == 0)
            return SolutionOutcome.Fail("solution", $"no solution file found under: {root}");

        Array.Sort(candidates, StringComparer.OrdinalIgnoreCase);
        if (candidates.Length > 1)
        {
            return SolutionOutcome.Fail(
                "solution",
                $"ambiguous solutions under {root}: {string.Join(", ", candidates.Select(Path.GetFileName))}");
        }

        return SolutionOutcome.Ok(new SolutionInfo(candidates[0], candidates));
    }
}
