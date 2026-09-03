// =============================================================================
//  Foundry.FSPM.Mcp.Tests — AwaitingContractTests
// =============================================================================
//
//  STEP 5 — Frozen Awaiting-Upstream Contract.
//
//  Each of the 3 Tools is currently a STUB that returns a structured
//  "AWAITING_COMPILER" envelope. This is NOT a Tool implementation
//  (per Architect §七: even if 8/8 pass, Tool Implementation must
//  remain NOT_COMPLETE until Compiler delivers).
//
//  These tests verify, against a REAL running server, that:
//
//    1. The Tool can actually be invoked end-to-end via the MCP
//       protocol (input contract is consumable, output is parseable).
//    2. The output carries a STRUCTURED "AWAITING_COMPILER" status —
//       we assert against the parsed JSON value, not a string
//       substring (per §六: "禁止测试字符串包含 AWAITING").
//    3. Each Tool's output is shape-stable: a known set of keys is
//       always present, and the top-level `status` field is the
//       literal string "AWAITING_COMPILER".
//
//  V6.1 MCP-05-02: fixture + response assertions owned by
//  Infrastructure/McpClientFixture + McpResponseAssertions.
//
//  Failure mode: if a future step accidentally swaps in a real
//  implementation, these tests will FAIL — which is the desired
//  behavior for a lockdown contract.
// =============================================================================

using System.Text.Json;
using Foundry.FSPM.Mcp.Tests.Infrastructure;
using Xunit;

namespace Foundry.FSPM.Mcp.Tests;

public class AwaitingContractTests : IClassFixture<McpClientFixture>
{
    private readonly McpClientFixture _fx;
    public AwaitingContractTests(McpClientFixture fx) { _fx = fx; }

    // -----------------------------------------------------------------
    // 1. Understand_AwaitingUpstreamContract
    // -----------------------------------------------------------------
    [Fact]
    public async Task Understand_AwaitingUpstreamContract()
    {
        var result = await _fx.Client.CallToolAsync(
            "fspm_understand",
            new Dictionary<string, object?>
            {
                ["workspaceRoot"] = "D:/tmp/contract-probe",
                ["target"] = "User.Login",
            });

        McpResponseAssertions.AssertSuccess(result, "fspm_understand");

        var envelope = McpResponseAssertions.ParseEnvelope(McpResponseAssertions.FirstText(result));

        // Top-level status MUST be exactly "AWAITING_COMPILER".
        McpResponseAssertions.AssertStatus(envelope, "AWAITING_COMPILER");

        // Frozen contract keys (Spec v2 §3.2 + INTERFACE_LOCKDOWN §1.3).
        Assert.True(envelope.TryGetProperty("workspaceRoot", out _));
        Assert.True(envelope.TryGetProperty("target", out _));
        Assert.True(envelope.TryGetProperty("message", out _));
        Assert.True(envelope.TryGetProperty("expectedContract", out var ec));
        Assert.Equal(JsonValueKind.Object, ec.ValueKind);
        Assert.True(ec.TryGetProperty("resolverType", out _));
        Assert.True(ec.TryGetProperty("method", out _));
        Assert.True(ec.TryGetProperty("resultType", out _));
    }

    // -----------------------------------------------------------------
    // 2. Construct_AwaitingUpstreamContract
    // -----------------------------------------------------------------
    [Fact]
    public async Task Construct_AwaitingUpstreamContract()
    {
        var result = await _fx.Client.CallToolAsync(
            "fspm_construct",
            new Dictionary<string, object?>
            {
                ["workspaceRoot"] = "D:/tmp/contract-probe",
                ["operation"] = "User.Login",
                ["instruction"] = "Ensure User entity has a Login domain method.",
            });

        McpResponseAssertions.AssertSuccess(result, "fspm_construct");

        var envelope = McpResponseAssertions.ParseEnvelope(McpResponseAssertions.FirstText(result));

        McpResponseAssertions.AssertStatus(envelope, "AWAITING_COMPILER");

        Assert.True(envelope.TryGetProperty("workspaceRoot", out _));
        Assert.True(envelope.TryGetProperty("operation", out _));
        Assert.True(envelope.TryGetProperty("instruction", out _));
        Assert.True(envelope.TryGetProperty("message", out _));
        Assert.True(envelope.TryGetProperty("contractRequired", out var cr));
        Assert.Equal(JsonValueKind.Object, cr.ValueKind);
        Assert.True(cr.TryGetProperty("inputContract", out _));
        Assert.True(cr.TryGetProperty("expectedResolution", out _));
        Assert.True(cr.TryGetProperty("mutationTarget", out _));
        Assert.True(cr.TryGetProperty("evidenceFields", out _));
    }

    // -----------------------------------------------------------------
    // 3. Verify_AwaitingUpstreamContract
    //
    //    fspm_verify takes 6 parameters. The current Tool implementation
    //    only enforces 3 of them (workspaceRoot/operation/executionId)
    //    in validation — we still pass all 6 here to exercise
    //    the full input contract.
    // -----------------------------------------------------------------
    [Fact]
    public async Task Verify_AwaitingUpstreamContract()
    {
        var result = await _fx.Client.CallToolAsync(
            "fspm_verify",
            new Dictionary<string, object?>
            {
                ["workspaceRoot"] = "D:/tmp/contract-probe",
                ["operation"] = "User.Login",
                ["projectPath"] = "D:/tmp/contract-probe/src.csproj",
                ["testPath"] = "D:/tmp/contract-probe/tests.csproj",
                ["loginMvpBaseUrl"] = "http://localhost:5099",
                ["executionId"] = "exec-0000",
            });

        McpResponseAssertions.AssertSuccess(result, "fspm_verify");

        var envelope = McpResponseAssertions.ParseEnvelope(McpResponseAssertions.FirstText(result));

        McpResponseAssertions.AssertStatus(envelope, "AWAITING_COMPILER");

        Assert.True(envelope.TryGetProperty("workspaceRoot", out _));
        Assert.True(envelope.TryGetProperty("operation", out _));
        Assert.True(envelope.TryGetProperty("executionId", out _));

        // 8-segment grid MUST be present and an object.
        Assert.True(envelope.TryGetProperty("segments", out var seg));
        Assert.Equal(JsonValueKind.Object, seg.ValueKind);
        foreach (var name in new[]
        {
            "semantic", "architecture", "security", "ui",
            "build", "test", "runtime", "evidence",
        })
        {
            Assert.True(seg.TryGetProperty(name, out _),
                $"segments.{name} missing from fspm_verify envelope.");
        }

        // Frozen Contract block.
        Assert.True(envelope.TryGetProperty("frozenContract", out var fc));
        Assert.Equal(JsonValueKind.Object, fc.ValueKind);
        Assert.True(fc.TryGetProperty("ruleDecisions", out _));
        Assert.True(fc.TryGetProperty("failureStage", out _));
        Assert.True(fc.TryGetProperty("evidenceSchema", out _));
        Assert.True(fc.TryGetProperty("closedCondition", out _));
    }
}
