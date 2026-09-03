// =============================================================================
//  Foundry.FSPM.Mcp — Gateways/ConstructionGateway
// =============================================================================
//
//  MCP-06-06: P6 contract stub. Returns AWAITING_COMPILER until P9/P10
//  wire the real Foundry.FSPM.Construction stack (FSPM-13/14).
//  See V6.1-01: Adapter Contract Ready ONLY.
//  placeholder-ok: V6.1-01 architect-authorized Adapter Contract stub;
//  Implementation tracked BLOCKED in .fspm/evidence/p6-gateways/node-status.json.
// =============================================================================

namespace Foundry.FSPM.Mcp.Gateways;

internal sealed class ConstructionGateway : IConstructionGateway
{
    public Task<GatewayOutcome> PlanAsync(
        string workspaceRoot,
        string operation,
        string instruction,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GatewayOutcome.AwaitingCompiler(
            "Foundry.FSPM.Construction.ConstructionService not delivered (FSPM-13/14)."));
    }
}
