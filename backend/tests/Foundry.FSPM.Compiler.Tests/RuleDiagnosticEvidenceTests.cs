using Foundry.FSPM.Compiler.Semantic.Rule;
using Foundry.FSPM.SemanticModel;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-04 decision → diagnostic → evidence projections. Codes are stable
// per kind; severity follows the verdict; reasons survive verbatim;
// evidence pins the snapshot version.
public sealed class RuleDiagnosticEvidenceTests
{
    [Theory]
    [InlineData(FspmRuleKind.Required, "RUL-REQ")]
    [InlineData(FspmRuleKind.Forbidden, "RUL-FRBD")]
    [InlineData(FspmRuleKind.Allowed, "RUL-ALLOW")]
    [InlineData(FspmRuleKind.ExactlyOne, "RUL-ONE")]
    [InlineData(FspmRuleKind.AtLeastOne, "RUL-ANY")]
    [InlineData(FspmRuleKind.TypeCompatible, "RUL-TYPE")]
    [InlineData(FspmRuleKind.OperationCompatible, "RUL-OP")]
    public void Codes_AreStable_PerKind(FspmRuleKind kind, string expected)
        => Assert.Equal(expected, FspmDiagnosticBuilder.CodeFor(kind));

    [Fact]
    public void Failed_Decision_Projects_ToError_PreservingReason()
    {
        var rule = new FspmRule("R-D1", FspmRuleKind.Required,
            new[] { "A|T:U.Ghost" }, "", "ghost required");
        var decision = FspmRuleEvaluator.Evaluate(
            new FspmRuleContext(RulePresenceTests.SyntheticModel(), "snap-1"), rule);
        Assert.False(decision.Passed);

        var diagnostic = FspmDiagnosticBuilder.FromDecision(rule, decision);
        Assert.Equal("RUL-REQ", diagnostic.Code);
        Assert.Equal("Error", diagnostic.Severity);
        Assert.Equal(decision.Reason, diagnostic.Reason);
        Assert.Contains("R-D1", diagnostic.Message);
        Assert.Null(diagnostic.Anchor);
    }

    [Fact]
    public void Passed_Decision_Projects_ToInfo_CarryingAnchor()
    {
        var rule = new FspmRule("R-D2", FspmRuleKind.Required,
            new[] { "A|T:U.User" }, "", "user required");
        var decision = FspmRuleEvaluator.Evaluate(
            new FspmRuleContext(RulePresenceTests.SyntheticModel(), "snap-1"), rule);
        Assert.True(decision.Passed);

        var diagnostic = FspmDiagnosticBuilder.FromDecision(rule, decision);
        Assert.Equal("Info", diagnostic.Severity);
        Assert.Equal(decision.Reason, diagnostic.Reason);
        Assert.NotNull(diagnostic.Anchor);
        Assert.Equal("User.cs", diagnostic.Anchor!.Document);
    }

    [Fact]
    public void Evidence_Pins_Identities_And_SnapshotVersion()
    {
        var rule = new FspmRule("R-E1", FspmRuleKind.TypeCompatible,
            new[] { "A|P:U.User.Name" }, "string", "name is string");
        var decision = FspmRuleEvaluator.Evaluate(
            new FspmRuleContext(RulePresenceTests.SyntheticModel(), "snap-7"), rule);
        var evidence = FspmEvidenceRecorder.Record(decision, "snap-7");

        Assert.Equal("R-E1", evidence.RuleId);
        Assert.True(evidence.Passed);
        Assert.Equal("A|P:U.User.Name", evidence.SubjectIdentity);
        Assert.Equal("FP-N", evidence.SubjectFingerprint);
        Assert.Equal("snap-7", evidence.SnapshotVersion);
        Assert.Equal(decision.Reason, evidence.Reason);
        Assert.NotNull(evidence.Anchor);
    }
}
