// =============================================================================
//  Foundry.FSPM.Mcp — Gateways/VerificationGateway
// =============================================================================
//
//  MCP-06-06: P6 contract stub. Returns AWAITING_COMPILER until P11 wires
//  the real VerificationOrchestrator + Analyzers (FSPM-04..12/17/18).
//  See V6.1-01: Adapter Contract Ready ONLY.
//  placeholder-ok: V6.1-01 architect-authorized Adapter Contract stub;
//  Implementation tracked BLOCKED in .fspm/evidence/p6-gateways/node-status.json.
// =============================================================================

namespace Foundry.FSPM.Mcp.Gateways;

internal sealed class VerificationGateway : IVerificationGateway
{
    public Task<GatewayOutcome> VerifyAsync(
        string workspaceRoot,
        string operation,
        string executionId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GatewayOutcome.AwaitingCompiler(
            "VerificationOrchestrator + Analyzers not delivered (FSPM-04..12/17/18)."));
    }
}
