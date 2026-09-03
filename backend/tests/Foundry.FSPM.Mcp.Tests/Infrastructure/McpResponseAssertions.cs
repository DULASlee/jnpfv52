// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Infrastructure/McpResponseAssertions
// =============================================================================
//
//  MCP-05-02: central assertions for MCP CallToolResult responses.
//  Success means IsError is EXPLICITLY false (not null, not absent) —
//  post MCP-05-01 every Tool sets IsError explicitly.
// =============================================================================

using System.Text.Json;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Infrastructure;

internal static class McpResponseAssertions
{
    public static void AssertSuccess(CallToolResult result, string toolName)
    {
        Assert.True(result.IsError == false, $"{toolName} call did not return explicit IsError=false.");
    }

    public static string FirstText(CallToolResult result)
    {
        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        Assert.NotNull(text);
        return text!.Text;
    }

    public static JsonElement ParseEnvelope(string json)
    {
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    public static void AssertStatus(JsonElement envelope, string expected)
    {
        Assert.True(envelope.TryGetProperty("status", out var statusEl),
            "Envelope missing top-level `status` field.");
        Assert.Equal(JsonValueKind.String, statusEl.ValueKind);
        Assert.Equal(expected, statusEl.GetString());
    }
}
