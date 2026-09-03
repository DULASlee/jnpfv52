// =============================================================================
//  Foundry.FSPM.Mcp — Gateways/IConstructionGateway
// =============================================================================
//
//  MCP-06-06: the ONLY boundary through which MCP may reach Core
//  construction capability. Body becomes real in P9/P10.
// =============================================================================

namespace Foundry.FSPM.Mcp.Gateways;

internal interface IConstructionGateway
{
    Task<GatewayOutcome> PlanAsync(
        string workspaceRoot,
        string operation,
        string instruction,
        CancellationToken cancellationToken = default);
}
