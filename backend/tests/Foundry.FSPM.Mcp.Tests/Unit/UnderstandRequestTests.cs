// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Unit/UnderstandRequestTests
// =============================================================================
//
//  MCP-08-01: request shape tests (no server needed).
// =============================================================================

using Foundry.FSPM.Mcp.Tools.Requests;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Unit;

public class UnderstandRequestTests
{
    [Fact]
    public void Record_PreservesWorkspaceProjectTarget()
    {
        var request = new UnderstandRequest("D:/w", "demo", "User.Login");

        Assert.Equal("D:/w", request.WorkspaceRoot);
        Assert.Equal("demo", request.ProjectName);
        Assert.Equal("User.Login", request.Target);
    }

    [Fact]
    public void Record_AllowsNullProject()
    {
        var request = new UnderstandRequest("D:/w", null, "User");

        Assert.Null(request.ProjectName);
    }
}
