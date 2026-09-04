using Foundry.FSPM.Compiler.Semantic.Rule;
using Foundry.FSPM.SemanticModel;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-04 compatibility rules on the synthetic pure model. Member-kind and
// entry-kind confusion never silently hit: a type id is not a member, a
// member id is not an operation.
public sealed class RuleTypeOperationTests
{
    private const string NameMember = "A|P:U.User.Name";
    private const string CreateOp = "A|M:U.User.Create";
    private const string UserType = "A|T:U.User";
    private const string Ghost = "A|M:U.Ghost";

    private static FspmRuleDecision Run(FspmRule rule)
        => FspmRuleEvaluator.Evaluate(
            new FspmRuleContext(RulePresenceTests.SyntheticModel(), "snap-1"), rule);

    [Fact]
    public void TypeCompatible_Match_Passes()
    {
        var d = Run(new FspmRule("R-T1", FspmRuleKind.TypeCompatible,
            new[] { NameMember }, "string", "name is string"));
        Assert.True(d.Passed);
        Assert.Contains("matching", d.Reason);
        Assert.Equal("FP-N", d.SubjectFingerprint);
        Assert.NotNull(d.Anchor);
    }

    [Fact]
    public void TypeCompatible_Mismatch_Fails_WithBothSides()
    {
        var d = Run(new FspmRule("R-T2", FspmRuleKind.TypeCompatible,
            new[] { NameMember }, "int", "name is int"));
        Assert.False(d.Passed);
        Assert.Contains("string", d.Reason);
        Assert.Contains("int", d.Reason);
    }

    [Fact]
    public void TypeCompatible_MissingMember_Fails()
    {
        var d = Run(new FspmRule("R-T3", FspmRuleKind.TypeCompatible,
            new[] { Ghost }, "string", "ghost is string"));
        Assert.False(d.Passed);
        Assert.Contains("missing", d.Reason);
    }

    [Fact]
    public void TypeCompatible_TypeEntry_IsNotAMember()
    {
        var d = Run(new FspmRule("R-T4", FspmRuleKind.TypeCompatible,
            new[] { UserType }, "User", "type as member"));
        Assert.False(d.Passed);
        Assert.Contains("missing", d.Reason);
    }

    [Fact]
    public void OperationCompatible_Match_Passes()
    {
        var d = Run(new FspmRule("R-O1", FspmRuleKind.OperationCompatible,
            new[] { CreateOp }, "void", "create returns void"));
        Assert.True(d.Passed);
        Assert.Contains("matching", d.Reason);
        Assert.Equal("FP-C", d.SubjectFingerprint);
    }

    [Fact]
    public void OperationCompatible_Mismatch_Fails_WithBothSides()
    {
        var d = Run(new FspmRule("R-O2", FspmRuleKind.OperationCompatible,
            new[] { CreateOp }, "User", "create returns user"));
        Assert.False(d.Passed);
        Assert.Contains("void", d.Reason);
        Assert.Contains("User", d.Reason);
    }

    [Fact]
    public void OperationCompatible_MemberId_IsNotAnOperation()
    {
        var d = Run(new FspmRule("R-O3", FspmRuleKind.OperationCompatible,
            new[] { NameMember }, "string", "member as operation"));
        Assert.False(d.Passed);
        Assert.Contains("missing", d.Reason);
    }
}
