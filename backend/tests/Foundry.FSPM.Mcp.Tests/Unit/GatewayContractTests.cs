// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Unit/GatewayContractTests
// =============================================================================
//
//  MCP-06-06: gateway contract tests (no server needed). Assert the
//  placeholder state explicitly: Adapter Contract Ready, Implementation
//  NOT ready (IsUpstreamAvailable == false).
// =============================================================================

using Foundry.FSPM.Mcp.Gateways;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests.Unit;

public class GatewayContractTests
{
    [Fact]
    public async Task SemanticGateway_ReturnsAwaitingCompilerPlaceholder()
    {
        ISemanticGateway gateway = new SemanticGateway();

        var outcome = await gateway.ResolveAsync("D:/w", "User.Login");

        Assert.False(outcome.IsUpstreamAvailable);
        Assert.Equal("AWAITING_COMPILER", outcome.Status);
        Assert.False(string.IsNullOrWhiteSpace(outcome.Detail));
    }

    [Fact]
    public async Task ConstructionGateway_ReturnsAwaitingCompilerPlaceholder()
    {
        IConstructionGateway gateway = new ConstructionGateway();

        var outcome = await gateway.PlanAsync("D:/w", "User.Login", "do it");

        Assert.False(outcome.IsUpstreamAvailable);
        Assert.Equal("AWAITING_COMPILER", outcome.Status);
        Assert.False(string.IsNullOrWhiteSpace(outcome.Detail));
    }

    [Fact]
    public async Task VerificationGateway_ReturnsAwaitingCompilerPlaceholder()
    {
        IVerificationGateway gateway = new VerificationGateway();

        var outcome = await gateway.VerifyAsync("D:/w", "User.Login", "exec-1");

        Assert.False(outcome.IsUpstreamAvailable);
        Assert.Equal("AWAITING_COMPILER", outcome.Status);
        Assert.False(string.IsNullOrWhiteSpace(outcome.Detail));
    }
}
