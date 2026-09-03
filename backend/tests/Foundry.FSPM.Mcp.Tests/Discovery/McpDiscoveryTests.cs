// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Discovery/McpDiscoveryTests
// =============================================================================
//
//  MCP-05-02: the FROZEN 3-tool discovery contract, verified through the
//  REAL MCP Tool Registry (ListToolsAsync) — never via source grep.
// =============================================================================

using Foundry.FSPM.Mcp.Tests.Infrastructure;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Discovery;

public class McpDiscoveryTests : IClassFixture<McpClientFixture>
{
    private readonly McpClientFixture _fx;
    public McpDiscoveryTests(McpClientFixture fx) { _fx = fx; }

    [Fact]
    public async Task Discovery_ReturnsExactlyThreeFrozenTools()
    {
        var tools = await McpToolDiscoveryFixture.ListToolsAsync(_fx.Client);
        McpToolDiscoveryFixture.AssertExactlyThreeTools(tools);
    }

    [Fact]
    public async Task Discovery_EachToolHasDescription()
    {
        var tools = await McpToolDiscoveryFixture.ListToolsAsync(_fx.Client);
        foreach (var name in McpToolDiscoveryFixture.ExpectedToolNames)
        {
            McpToolDiscoveryFixture.GetToolOrFail(tools, name);
        }
    }
}
