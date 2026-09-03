// =============================================================================
//  Foundry.FSPM.Mcp — Execution/McpExecutionContext
// =============================================================================
//
//  MCP-06-01: the run context of ONE MCP call. Immutable after creation.
//  Instances can ONLY be created through McpExecutionContextFactory
//  (private constructor + internal creation method whose sole caller is
//  the factory — verified by the Core-reference-style grep audit in the
//  P6 Gate). Tools must never mint their own ExecutionIds.
// =============================================================================

namespace Foundry.FSPM.Mcp.Execution;

/// <summary>
/// Immutable run context of a single MCP tool invocation.
/// </summary>
public sealed class McpExecutionContext
{
    private McpExecutionContext(
        string executionId,
        string correlationId,
        DateTimeOffset startedAt,
        string toolName,
        string workspaceRoot,
        string requestJson)
    {
        ExecutionId = executionId;
        CorrelationId = correlationId;
        StartedAt = startedAt;
        ToolName = toolName;
        WorkspaceRoot = workspaceRoot;
        RequestJson = requestJson;
    }

    public string ExecutionId { get; }
    public string CorrelationId { get; }
    public DateTimeOffset StartedAt { get; }
    public string ToolName { get; }
    public string WorkspaceRoot { get; }
    public string RequestJson { get; }

    internal static McpExecutionContext CreateInstance(
        string executionId,
        string correlationId,
        DateTimeOffset startedAt,
        string toolName,
        string workspaceRoot,
        string requestJson)
    {
        return new McpExecutionContext(
            executionId, correlationId, startedAt, toolName, workspaceRoot, requestJson);
    }
}
