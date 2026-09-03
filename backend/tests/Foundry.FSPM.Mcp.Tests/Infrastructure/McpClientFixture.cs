// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Infrastructure/McpClientFixture
// =============================================================================
//
//  MCP-05-02: the ONE shared xUnit class fixture for all MCP integration
//  tests. Owns the dual-path stdio setup:
//
//    1. A manually-spawned server process (via McpTestServerFactory) whose
//       raw stdout/stderr are captured for the protocol-clean test.
//    2. An McpClient connected through StdioClientTransport (which spawns
//       its own child process) for tool discovery and invocation.
//
//  Why stdio, not in-process: STEP 5 §1 requires proving the real MCP
//  server can start. In-process hosting would not exercise the actual
//  stdio transport and was rejected at architecture review.
// =============================================================================

using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Client;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Infrastructure;

public sealed class McpClientFixture : IAsyncLifetime
{
    public McpClient Client { get; private set; } = null!;
    public Process ServerProcess { get; private set; } = null!;
    public StringBuilder StderrCapture { get; } = new();
    public StringBuilder StdoutCapture { get; } = new();
    public string ServerDllPath { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        ServerDllPath = McpTestServerFactory.ResolveServerDllPath();
        ServerProcess = McpTestServerFactory.Start(ServerDllPath, StdoutCapture, StderrCapture);

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "Foundry.FSPM.Mcp",
            Command = "dotnet",
            Arguments = new[] { ServerDllPath },
        });
        Client = await McpClient.CreateAsync(transport).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (Client is not null)
        {
            try
            {
                await Client.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StderrCapture.AppendLine(
                    $"[teardown] client dispose failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        if (ServerProcess is not null)
        {
            McpTestServerFactory.Stop(ServerProcess, StderrCapture);
        }
    }
}
