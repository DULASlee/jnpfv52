// =============================================================================
//  Foundry.FSPM.Mcp — Gateways/SemanticGateway
// =============================================================================
//
//  MCP-06-06: P6 contract stub. Returns AWAITING_COMPILER until P8 wires
//  the real Foundry.FSPM.Core.Semantic.SemanticResolver (FSPM-07/08).
//  See V6.1-01: Adapter Contract Ready ONLY.
//  placeholder-ok: V6.1-01 architect-authorized Adapter Contract stub;
//  Implementation tracked BLOCKED in .fspm/evidence/p6-gateways/node-status.json.
// =============================================================================

namespace Foundry.FSPM.Mcp.Gateways;

internal sealed class SemanticGateway : ISemanticGateway
{
    public Task<GatewayOutcome> ResolveAsync(
        string workspaceRoot,
        string target,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GatewayOutcome.AwaitingCompiler(
            "Foundry.FSPM.Core.Semantic.SemanticResolver not delivered (FSPM-07/08)."));
    }
}
