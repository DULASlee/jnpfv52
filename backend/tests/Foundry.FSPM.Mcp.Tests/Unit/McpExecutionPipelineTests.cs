// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Unit/McpExecutionPipelineTests
// =============================================================================
//
//  MCP-06-07: pipeline unit tests (no server needed).
// =============================================================================

using System.Text.Json;
using Foundry.FSPM.Mcp.Execution;
using Foundry.FSPM.Mcp.Validation;
using Foundry.FSPM.Mcp.Workspace;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Unit;

public class McpExecutionPipelineTests
{
    private static readonly McpExecutionPipeline Pipeline = new();

    private static JsonElement Payload(CallToolResult result)
    {
        var text = Assert.Single(result.Content.OfType<TextContentBlock>());
        using var doc = JsonDocument.Parse(text.Text);
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task Execute_ValidationFailure_ShortCircuitsWithInvalidRequest()
    {
        bool gatewayInvoked = false;

        var result = await Pipeline.ExecuteAsync(
            "fspm_understand",
            "D:/w",
            new { target = "User." },
            validate: () => McpValidationResult.Fail("target", "target is malformed."),
            invoke: (_, _) =>
            {
                gatewayInvoked = true;
                return Task.FromResult<object>(new { status = "NEVER" });
            });

        Assert.True(result.IsError == true);
        Assert.False(gatewayInvoked);
        Assert.Equal("INVALID_REQUEST", Payload(result).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Execute_SuccessFlow_WrapsPayloadAndPassesContext()
    {
        McpExecutionContext? seenContext = null;

        var result = await Pipeline.ExecuteAsync(
            "fspm_construct",
            "D:/w",
            new { operation = "User.Login" },
            validate: () => null,
            invoke: (ctx, _) =>
            {
                seenContext = ctx;
                return Task.FromResult<object>(new { status = "AWAITING_COMPILER" });
            });

        Assert.True(result.IsError == false);
        Assert.Equal("AWAITING_COMPILER", Payload(result).GetProperty("status").GetString());
        Assert.NotNull(seenContext);
        Assert.Equal("fspm_construct", seenContext!.ToolName);
        Assert.False(string.IsNullOrWhiteSpace(seenContext.ExecutionId));
    }

    [Fact]
    public async Task Execute_GatewayThrows_MapsToFailed()
    {
        var result = await Pipeline.ExecuteAsync(
            "fspm_verify",
            "D:/w",
            new { operation = "User.Login" },
            validate: () => null,
            invoke: (_, _) => throw new IOException("disk gone"));

        Assert.True(result.IsError == true);
        Assert.Equal("FAILED", Payload(result).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Execute_UnresolvableWorkspace_StillReachesGateway()
    {
        // P6 policy pin: resolution runs but does not fail the call;
        // enforcement belongs to real gateway bodies (P8+).
        bool gatewayInvoked = false;
        McpWorkspaceOutcome? seenOutcome = null;

        var result = await Pipeline.ExecuteAsync(
            "fspm_understand",
            "D:/no-such-workspace-anywhere",
            new { target = "User" },
            validate: () => null,
            invoke: (_, outcome) =>
            {
                gatewayInvoked = true;
                seenOutcome = outcome;
                return Task.FromResult<object>(new { status = "AWAITING_COMPILER" });
            });

        Assert.True(gatewayInvoked);
        Assert.NotNull(seenOutcome);
        Assert.False(seenOutcome!.IsResolved);
        Assert.True(result.IsError == false);
    }
}
