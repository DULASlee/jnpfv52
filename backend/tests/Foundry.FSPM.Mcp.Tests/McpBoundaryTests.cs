// =============================================================================
//  Foundry.FSPM.Mcp.Tests — McpBoundaryTests
// =============================================================================
//
//  STEP 5 — MCP Boundary & Protocol Lockdown.
//
//  These tests prove (via the REAL ModelContextProtocol 2.2.0 client
//  talking to a REAL spawned stdio server process) that:
//
//    1. The MCP server actually starts and accepts a JSON-RPC handshake
//       on stdio.
//    2. EXACTLY 3 tools are registered, and their names are precisely
//       fspm_understand, fspm_construct, fspm_verify.
//    3. The server's stdout is protocol-clean: no Console.WriteLine /
//       no "hello" strings / no LogToStandardErrorThreshold
//       misroutes — only JSON-RPC frames during the handshake window.
//
//  V6.1 MCP-05-02: server lifecycle owned by Infrastructure/McpClientFixture.
//  Discovery assertions owned by Infrastructure/McpToolDiscoveryFixture.
//
//  We deliberately do NOT touch any FSPM Core / Analyzer / Login.Mvp
//  project. The MCP Adapter (Foundry.FSPM.Mcp) is the only project
//  under test here; everything else is upstream and MISSING on disk
//  (see .fspm/evidence/baseline/MCP_UPSTREAM_GAP.md).
// =============================================================================

using System.Text.Json;
using Foundry.FSPM.Mcp.Tests.Infrastructure;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests;

public class McpBoundaryTests : IClassFixture<McpClientFixture>
{
    private readonly McpClientFixture _fx;
    public McpBoundaryTests(McpClientFixture fx) { _fx = fx; }

    // -----------------------------------------------------------------
    // 1. McpServerStarts
    //    The McpClient.CreateAsync succeeded => handshake succeeded.
    //    ServerInfo must be non-empty.
    // -----------------------------------------------------------------
    [Fact]
    public void McpServerStarts()
    {
        Assert.NotNull(_fx.Client);
        var info = _fx.Client.ServerInfo;
        Assert.NotNull(info);
        Assert.False(string.IsNullOrWhiteSpace(info.Name),
            "MCP server returned empty ServerInfo.Name — handshake succeeded but server is unnamed.");
    }

    // -----------------------------------------------------------------
    // 2. ExactlyThreeToolsAreRegistered
    // -----------------------------------------------------------------
    [Fact]
    public async Task ExactlyThreeToolsAreRegistered()
    {
        var tools = await McpToolDiscoveryFixture.ListToolsAsync(_fx.Client);
        McpToolDiscoveryFixture.AssertExactlyThreeTools(tools);
    }

    [Fact]
    public async Task Tool_Understand_IsAvailable()
    {
        var tools = await McpToolDiscoveryFixture.ListToolsAsync(_fx.Client);
        McpToolDiscoveryFixture.GetToolOrFail(tools, "fspm_understand");
    }

    [Fact]
    public async Task Tool_Construct_IsAvailable()
    {
        var tools = await McpToolDiscoveryFixture.ListToolsAsync(_fx.Client);
        McpToolDiscoveryFixture.GetToolOrFail(tools, "fspm_construct");
    }

    [Fact]
    public async Task Tool_Verify_IsAvailable()
    {
        var tools = await McpToolDiscoveryFixture.ListToolsAsync(_fx.Client);
        McpToolDiscoveryFixture.GetToolOrFail(tools, "fspm_verify");
    }

    // -----------------------------------------------------------------
    // 6. McpStdoutIsProtocolClean
    //    The captured stdout from the manually-spawned server process
    //    (which never received any input) must contain only startup
    //    frames. Without a client write, the server should not emit
    //    anything on stdout. Any line emitted is a protocol violation.
    // -----------------------------------------------------------------
    [Fact]
    public async Task McpStdoutIsProtocolClean()
    {
        // Give the server a moment to start up. It only emits a banner
        // / log line on stdout if Program.cs is misconfigured.
        await Task.Delay(500);

        var stdoutText = _fx.StdoutCapture.ToString();
        if (string.IsNullOrEmpty(stdoutText))
        {
            // Acceptable: server emitted nothing on stdout because no
            // client message arrived yet. This is the FROZEN contract
            // (Spec v2 §1: stdout is reserved for JSON-RPC frames).
            return;
        }

        var lines = stdoutText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        // Any non-empty stdout must consist of JSON-RPC frames. Each
        // frame is either a raw JSON object on its own line OR framed
        // by a Content-Length header (per the MCP stdio transport).
        foreach (var line in lines)
        {
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                continue;
            if (line.StartsWith("Content-Type:", StringComparison.OrdinalIgnoreCase))
                continue;
            Assert.True(line.StartsWith("{"),
                $"Non-JSON line on MCP stdout: {line}");
            using var doc = JsonDocument.Parse(line);
            Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        }
    }
}
