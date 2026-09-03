// =============================================================================
//  Foundry.FSPM.Mcp — Gateways/ISemanticGateway
// =============================================================================
//
//  MCP-06-06: the ONLY boundary through which MCP may reach Core semantic
//  capability. Method name + shape are the frozen Adapter Contract;
//  the body becomes real in P8 (MCP-08-03).
// =============================================================================

namespace Foundry.FSPM.Mcp.Gateways;

internal interface ISemanticGateway
{
    Task<GatewayOutcome> ResolveAsync(
        string workspaceRoot,
        string target,
        CancellationToken cancellationToken = default);
}
