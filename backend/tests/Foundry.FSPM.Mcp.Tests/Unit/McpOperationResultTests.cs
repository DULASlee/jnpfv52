// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Unit/McpOperationResultTests
// =============================================================================
//
//  MCP-06-03: envelope unit tests (no server needed).
// =============================================================================

using System.Text.Json;
using Foundry.FSPM.Mcp.Execution;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Unit;

public class McpOperationResultTests
{
    [Fact]
    public void Success_WrapsPayloadWithExplicitIsErrorFalse()
    {
        var result = McpOperationResult.Success(new { status = "AWAITING_COMPILER", x = 1 });

        Assert.True(result.IsError == false);
        var text = Assert.Single(result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>());
        using var doc = JsonDocument.Parse(text.Text);
        Assert.Equal("AWAITING_COMPILER", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(1, doc.RootElement.GetProperty("x").GetInt32());
    }

    [Fact]
    public void InvalidRequest_SetsIsErrorTrueAndShape()
    {
        var validation = Foundry.FSPM.Mcp.Validation.McpValidationResult.Fail("target", "target is required.");

        var result = McpOperationResult.InvalidRequest(validation);

        Assert.True(result.IsError == true);
        var text = Assert.Single(result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>());
        using var doc = JsonDocument.Parse(text.Text);
        Assert.Equal("INVALID_REQUEST", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("target", doc.RootElement.GetProperty("field").GetString());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("message").GetString()));
    }

    [Fact]
    public void StatusLiterals_AreDistinct()
    {
        Assert.NotEqual(McpOperationStatus.Success, McpOperationStatus.InvalidRequest);
        Assert.NotEqual(McpOperationStatus.InvalidRequest, McpOperationStatus.AwaitingCompiler);
        Assert.NotEqual(McpOperationStatus.AwaitingCompiler, McpOperationStatus.Failed);
    }
}
