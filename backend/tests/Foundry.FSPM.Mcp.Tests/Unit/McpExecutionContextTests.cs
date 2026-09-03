// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Unit/McpExecutionContextTests
// =============================================================================
//
//  MCP-06-01: ExecutionContext unit tests (no server needed).
// =============================================================================

using Foundry.FSPM.Mcp.Execution;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Unit;

public class McpExecutionContextTests
{
    private readonly McpExecutionContextFactory _factory = new();

    [Fact]
    public void Create_ReturnsUniqueExecutionIds()
    {
        var a = _factory.Create("fspm_understand", "D:/w", new { target = "User" });
        var b = _factory.Create("fspm_understand", "D:/w", new { target = "User" });

        Assert.False(string.IsNullOrWhiteSpace(a.ExecutionId));
        Assert.False(string.IsNullOrWhiteSpace(b.ExecutionId));
        Assert.NotEqual(a.ExecutionId, b.ExecutionId);
    }

    [Fact]
    public void Create_PreservesToolNameWorkspaceAndCorrelation()
    {
        var ctx = _factory.Create(
            "fspm_construct",
            "D:/w",
            new { operation = "User.Login" },
            correlationId: "corr-1");

        Assert.Equal("fspm_construct", ctx.ToolName);
        Assert.Equal("D:/w", ctx.WorkspaceRoot);
        Assert.Equal("corr-1", ctx.CorrelationId);
        Assert.Contains("User.Login", ctx.RequestJson);
    }

    [Fact]
    public void Create_GeneratesCorrelationIdWhenMissing()
    {
        var ctx = _factory.Create("fspm_verify", "D:/w", new { operation = "User.Login" });

        Assert.False(string.IsNullOrWhiteSpace(ctx.CorrelationId));
    }

    [Fact]
    public void Create_StartedAtIsRecentUtc()
    {
        var before = DateTimeOffset.UtcNow;
        var ctx = _factory.Create("fspm_understand", "D:/w", new { target = "User" });
        var after = DateTimeOffset.UtcNow;

        Assert.True(ctx.StartedAt >= before && ctx.StartedAt <= after);
        Assert.Equal(TimeSpan.Zero, ctx.StartedAt.Offset);
    }

    [Fact]
    public void Create_RejectsEmptyToolName()
    {
        Assert.Throws<ArgumentException>(() =>
            _factory.Create("", "D:/w", new { target = "User" }));
    }
}
