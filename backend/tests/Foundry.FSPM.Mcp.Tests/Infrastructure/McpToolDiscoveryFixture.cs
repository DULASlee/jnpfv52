// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Infrastructure/McpToolDiscoveryFixture
// =============================================================================
//
//  MCP-05-02: central helper for the FROZEN 3-tool discovery contract.
//  The expected names are defined exactly once here — no test may
//  hard-code its own copy of the tool list.
// =============================================================================

using ModelContextProtocol.Client;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Infrastructure;

internal static class McpToolDiscoveryFixture
{
    public static readonly string[] ExpectedToolNames =
        new[] { "fspm_construct", "fspm_understand", "fspm_verify" };

    public static async Task<IList<McpClientTool>> ListToolsAsync(McpClient client)
    {
        return await client.ListToolsAsync().ConfigureAwait(false);
    }

    public static void AssertExactlyThreeTools(IList<McpClientTool> tools)
    {
        var names = tools.Select(t => t.Name).OrderBy(n => n).ToArray();
        Assert.Equal(ExpectedToolNames.Length, tools.Count);
        Assert.Equal(ExpectedToolNames, names);
    }

    public static McpClientTool GetToolOrFail(IList<McpClientTool> tools, string name)
    {
        var tool = tools.SingleOrDefault(t => t.Name == name);
        Assert.NotNull(tool);
        Assert.False(string.IsNullOrWhiteSpace(tool!.Description));
        return tool!;
    }
}
