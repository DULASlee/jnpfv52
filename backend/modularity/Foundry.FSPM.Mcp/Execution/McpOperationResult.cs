// =============================================================================
//  Foundry.FSPM.Mcp — Execution/McpOperationResult
// =============================================================================
//
//  MCP-06-03: INTERNAL adapter model for building MCP responses.
//
//  Contract-ownership ladder (V6.1-02) outcome:
//    ① Spec v2 / INTERFACE_LOCKDOWN contain NO shared result-envelope type
//       (only per-Tool JSON shapes + Core-side evidence types).
//    ② Therefore nothing to reuse.
//    ③ This type is an INTERNAL adapter vocabulary (internal, never a
//       frozen public contract). It centralizes status literals +
//       serialization + CallToolResult wrapping. Per-Tool payload shapes
//       stay frozen per Spec v2 and are NOT unified here.
// =============================================================================

using System.Text.Json;
using Foundry.FSPM.Mcp.Validation;
using ModelContextProtocol.Protocol;

namespace Foundry.FSPM.Mcp.Execution;

/// <summary>
/// Internal status vocabulary. NOT a frozen public contract.
/// </summary>
internal static class McpOperationStatus
{
    public const string Success = "SUCCESS";
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string AwaitingCompiler = "AWAITING_COMPILER";
    public const string Failed = "FAILED";
}

/// <summary>
/// Internal helper that wraps Tool payloads into CallToolResult envelopes.
/// </summary>
internal static class McpOperationResult
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static CallToolResult Success(object payload)
    {
        return Wrap(false, payload);
    }

    public static CallToolResult InvalidRequest(McpValidationResult validation)
    {
        return Wrap(true, new
        {
            status = McpOperationStatus.InvalidRequest,
            field = validation.Field,
            message = validation.Message,
        });
    }

    private static CallToolResult Wrap(bool isError, object payload)
    {
        string json = JsonSerializer.Serialize(payload, JsonOptions);
        return new CallToolResult
        {
            IsError = isError,
            Content = new List<ContentBlock> { new TextContentBlock { Text = json } },
        };
    }
}
