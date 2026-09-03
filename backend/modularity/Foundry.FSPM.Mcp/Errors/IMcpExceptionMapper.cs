// =============================================================================
//  Foundry.FSPM.Mcp — Errors/IMcpExceptionMapper
// =============================================================================
//
//  MCP-06-04: the ONE place that converts escaped exceptions into stable
//  MCP responses. Every Tool body must be wrapped so that NO exception
//  ever crosses the Transport boundary raw.
//
//  Hard ban: catch(Exception) → Success is FORBIDDEN. The mapper only
//  produces IsError=true envelopes.
// =============================================================================

using ModelContextProtocol.Protocol;

namespace Foundry.FSPM.Mcp.Errors;

/// <summary>
/// Maps escaped exceptions to stable MCP error responses.
/// </summary>
public interface IMcpExceptionMapper
{
    CallToolResult Map(Exception exception, string toolName, string executionId = "unknown");
}
