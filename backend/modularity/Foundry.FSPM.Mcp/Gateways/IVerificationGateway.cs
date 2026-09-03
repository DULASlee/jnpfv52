// =============================================================================
//  Foundry.FSPM.Mcp — Gateways/IVerificationGateway
// =============================================================================
//
//  MCP-06-06: the ONLY boundary through which MCP may reach Core
//  verification capability. Body becomes real in P11.
// =============================================================================

namespace Foundry.FSPM.Mcp.Gateways;

internal interface IVerificationGateway
{
    Task<GatewayOutcome> VerifyAsync(
        string workspaceRoot,
        string operation,
        string executionId,
        CancellationToken cancellationToken = default);
}
