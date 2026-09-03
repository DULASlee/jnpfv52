// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Unit/McpExceptionMapperTests
// =============================================================================
//
//  MCP-06-04: mapper unit tests (no server needed).
// =============================================================================

using System.Text.Json;
using Foundry.FSPM.Mcp.Errors;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Unit;

public class McpExceptionMapperTests
{
    private readonly IMcpExceptionMapper _mapper = new McpExceptionMapper();

    private static JsonElement Envelope(CallToolResult result)
    {
        var text = Assert.Single(result.Content.OfType<TextContentBlock>());
        using var doc = JsonDocument.Parse(text.Text);
        return doc.RootElement.Clone();
    }

    [Fact]
    public void Map_ArgumentException_YieldsInvalidRequest()
    {
        var result = _mapper.Map(new ArgumentException("bad field"), "fspm_understand", "exec-1");

        Assert.True(result.IsError == true);
        var envelope = Envelope(result);
        Assert.Equal("INVALID_REQUEST", envelope.GetProperty("status").GetString());
        Assert.Equal("fspm_understand", envelope.GetProperty("tool").GetString());
        Assert.Equal("exec-1", envelope.GetProperty("executionId").GetString());
    }

    [Theory]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    [InlineData(typeof(DirectoryNotFoundException))]
    [InlineData(typeof(TimeoutException))]
    [InlineData(typeof(OperationCanceledException))]
    [InlineData(typeof(InvalidOperationException))]
    public void Map_KnownAndUnknownExceptions_YieldFailedWithIsErrorTrue(Type exceptionType)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType, "boom")!;

        var result = _mapper.Map(ex, "fspm_verify");

        Assert.True(result.IsError == true);
        var envelope = Envelope(result);
        Assert.Equal("FAILED", envelope.GetProperty("status").GetString());
        Assert.Equal("unknown", envelope.GetProperty("executionId").GetString());
        // No stack leak: envelope must not contain a stack trace.
        Assert.False(envelope.GetRawText().Contains(" at ", StringComparison.Ordinal));
    }

    [Fact]
    public void Map_NeverProducesSuccess()
    {
        foreach (var ex in new Exception[]
        {
            new ArgumentException("a"),
            new IOException("b"),
            new Exception("c"),
        })
        {
            var result = _mapper.Map(ex, "fspm_construct");
            var envelope = Envelope(result);
            Assert.NotEqual("SUCCESS", envelope.GetProperty("status").GetString());
            Assert.NotEqual("AWAITING_COMPILER", envelope.GetProperty("status").GetString());
        }
    }
}
