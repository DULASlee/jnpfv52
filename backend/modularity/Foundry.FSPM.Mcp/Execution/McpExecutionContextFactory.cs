// =============================================================================
//  Foundry.FSPM.Mcp — Execution/McpExecutionContextFactory
// =============================================================================
//
//  MCP-06-01: THE ONLY place that mints McpExecutionContext instances.
//  Every Tool must call Create() exactly once per incoming request and
//  carry the returned context through the whole pipeline.
// =============================================================================

using System.Text.Json;

namespace Foundry.FSPM.Mcp.Execution;

/// <summary>
/// Sole generator of <see cref="McpExecutionContext"/> instances.
/// </summary>
public sealed class McpExecutionContextFactory
{
    public McpExecutionContext Create(
        string toolName,
        string workspaceRoot,
        object request,
        string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("toolName is required.", nameof(toolName));
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            throw new ArgumentException("workspaceRoot is required.", nameof(workspaceRoot));
        ArgumentNullException.ThrowIfNull(request);

        return McpExecutionContext.CreateInstance(
            executionId: Guid.NewGuid().ToString("N"),
            correlationId: string.IsNullOrWhiteSpace(correlationId)
                ? Guid.NewGuid().ToString("N")
                : correlationId,
            startedAt: DateTimeOffset.UtcNow,
            toolName: toolName,
            workspaceRoot: workspaceRoot,
            requestJson: JsonSerializer.Serialize(request));
    }
}
