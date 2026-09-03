// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Discovery/McpServerLifecycleTests
// =============================================================================
//
//  MCP-05-02: Start → Discover → Invoke → Shutdown lifecycle proof
//  against the REAL stdio server (via Infrastructure/McpClientFixture).
//
//  Must prove: startup exception = 0, unexpected stdout = 0, the server
//  stays alive across calls, and a Tool can be invoked on the same
//  connection that discovered it.
// =============================================================================

using Foundry.FSPM.Mcp.Tests.Infrastructure;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Discovery;

public class McpServerLifecycleTests : IClassFixture<McpClientFixture>
{
    private readonly McpClientFixture _fx;
    public McpServerLifecycleTests(McpClientFixture fx) { _fx = fx; }

    [Fact]
    public async Task Start_Discover_Invoke_StaysAlive()
    {
        // Start: fixture handshake already succeeded; server process alive.
        Assert.NotNull(_fx.Client);
        Assert.NotNull(_fx.ServerProcess);
        Assert.False(string.IsNullOrWhiteSpace(_fx.ServerDllPath));
        Assert.False(_fx.ServerProcess.HasExited);

        // Discover: exactly the 3 frozen tools.
        var tools = await McpToolDiscoveryFixture.ListToolsAsync(_fx.Client);
        McpToolDiscoveryFixture.AssertExactlyThreeTools(tools);

        // Invoke: fspm_understand on the SAME connection.
        var result = await _fx.Client.CallToolAsync(
            "fspm_understand",
            new Dictionary<string, object?>
            {
                ["workspaceRoot"] = "D:/tmp/lifecycle-probe",
                ["target"] = "User",
            });
        McpResponseAssertions.AssertSuccess(result, "fspm_understand");
        var envelope = McpResponseAssertions.ParseEnvelope(McpResponseAssertions.FirstText(result));
        McpResponseAssertions.AssertStatus(envelope, "AWAITING_COMPILER");

        // Still alive after the call (no crash, no silent exit).
        Assert.False(_fx.ServerProcess.HasExited);
        // Shutdown is owned by the fixture DisposeAsync (STOP kills the tree).
    }
}
