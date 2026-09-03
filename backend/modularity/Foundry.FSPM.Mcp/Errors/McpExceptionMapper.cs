// =============================================================================
//  Foundry.FSPM.Mcp — Errors/McpExceptionMapper
// =============================================================================
//
//  MCP-06-04: default implementation of IMcpExceptionMapper.
//
//  Mapping table (stable, no stack leaks — Exception.ToString() is NEVER
//  serialized into the envelope; only type name + message):
//    ArgumentException            → INVALID_REQUEST
//    OperationCanceledException   → FAILED (cancelled)
//    TimeoutException             → FAILED (timeout)
//    IOException (incl.          → FAILED (infrastructure)
//    DirectoryNotFoundException) /
//    UnauthorizedAccessException  → FAILED (infrastructure)
//    other Exception              → FAILED (internal failure)
//
//  Future Core exception families (Resolution / Construction /
//  Verification) land in the default FAILED branch until the Core
//  contracts arrive — they are NEVER mapped to Success.
// =============================================================================

using System.Text.Json;
using Foundry.FSPM.Mcp.Execution;
using ModelContextProtocol.Protocol;

namespace Foundry.FSPM.Mcp.Errors;

public sealed class McpExceptionMapper : IMcpExceptionMapper
{
    public CallToolResult Map(Exception exception, string toolName, string executionId = "unknown")
    {
        ArgumentNullException.ThrowIfNull(exception);

        string status = McpOperationStatus.Failed;
        string reason = "internal failure";
        string safeTool = string.IsNullOrWhiteSpace(toolName) ? "unknown-tool" : toolName;
        string safeExecution = string.IsNullOrWhiteSpace(executionId) ? "unknown" : executionId;

        switch (exception)
        {
            case ArgumentException:
                status = McpOperationStatus.InvalidRequest;
                reason = "invalid request";
                break;
            case OperationCanceledException:
                reason = "cancelled";
                break;
            case TimeoutException:
                reason = "timeout";
                break;
            case IOException:
            case UnauthorizedAccessException:
                reason = "infrastructure failure";
                break;
        }

        string json = JsonSerializer.Serialize(
            new
            {
                status,
                tool = safeTool,
                executionId = safeExecution,
                reason,
                errorType = exception.GetType().Name,
                message = exception.Message,
            },
            new JsonSerializerOptions { WriteIndented = true });

        return new CallToolResult
        {
            IsError = true,
            Content = new List<ContentBlock> { new TextContentBlock { Text = json } },
        };
    }
}
