// =============================================================================
//  Foundry.FSPM.Mcp — Gateways/GatewayOutcome
// =============================================================================
//
//  MCP-06-06: minimal internal outcome of a gateway call. At P6 all three
//  gateways return AwaitingCompiler (V6.1-01: compile-time contract stub +
//  test double — NOT counted as real function completion). P8/P9/P11
//  replace the internals with real Core calls; this shape stays.
//  placeholder-ok: V6.1-01 architect-authorized Adapter Contract stub family.
// =============================================================================

namespace Foundry.FSPM.Mcp.Gateways;

internal sealed class GatewayOutcome
{
    private GatewayOutcome(bool isUpstreamAvailable, string status, string detail)
    {
        IsUpstreamAvailable = isUpstreamAvailable;
        Status = status;
        Detail = detail;
    }

    public bool IsUpstreamAvailable { get; }
    public string Status { get; }
    public string Detail { get; }

    public static GatewayOutcome AwaitingCompiler(string detail) =>
        new(false, "AWAITING_COMPILER", detail);
}
