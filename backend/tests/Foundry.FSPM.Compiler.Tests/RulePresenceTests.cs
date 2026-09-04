using Foundry.FSPM.Compiler.Semantic.Rule;
using Foundry.FSPM.SemanticModel;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-04 presence rules: Required/Forbidden/Allowed/ExactlyOne/AtLeastOne
// on a synthetic pure model. Identity-keyed only: display-like strings
// never match.
public sealed class RulePresenceTests
{
    private const string UserType = "A|T:U.User";
    private const string NameMember = "A|P:U.User.Name";
    private const string AgeMember = "A|F:U.User.Age";
    private const string CreateOp = "A|M:U.User.Create";
    private const string Ghost = "A|T:U.Ghost";

    internal static FspmSemanticModel SyntheticModel()
    {
        var userAnchor = new FspmSemanticAnchor("User.cs", "T:U.User", 1, 1, 10, 1);
        var nameAnchor = new FspmSemanticAnchor("User.cs", "P:U.User.Name", 5, 5, 5, 20);
        return new FspmSemanticModel(
            Types: new[]
            {
                new FspmSemanticType(
                    new FspmSemanticIdentity(UserType, "FP-U"),
                    "User", "U", "Named", "Class", 0, null, null,
                    System.Array.Empty<string>(), "FP-U", userAnchor, FspmSemanticState.Resolved),
            },
            Members: new[]
            {
                new FspmSemanticMember(
                    new FspmSemanticIdentity(NameMember, "FP-N"),
                    "Name", UserType, "Property", "string", "FP-N", nameAnchor, FspmSemanticState.Resolved),
                new FspmSemanticMember(
                    new FspmSemanticIdentity(AgeMember, "FP-A"),
                    "Age", UserType, "Field", "int", "FP-A", nameAnchor, FspmSemanticState.Resolved),
            },
            Operations: new[]
            {
                new FspmSemanticOperation(
                    new FspmSemanticIdentity(CreateOp, "FP-C"),
                    "Create", UserType, "Method",
                    System.Array.Empty<FspmSemanticParameter>(),
                    "void", 0, "FP-C", nameAnchor, FspmSemanticState.Resolved),
            },
            Parameters: System.Array.Empty<FspmSemanticParameter>(),
            Relations: System.Array.Empty<FspmSemanticRelation>(),
            Diagnostics: System.Array.Empty<string>(),
            Metadata: new FspmSemanticModelMetadata("synth", "Synth", 4));
    }

    private static FspmRuleDecision Run(FspmRule rule)
        => FspmRuleEvaluator.Evaluate(new FspmRuleContext(SyntheticModel(), "snap-1"), rule);

    private static FspmRule Presence(FspmRuleKind kind, params string[] targets)
        => new($"R-{kind}", kind, targets, "", $"presence {kind}");

    [Fact]
    public void Required_Present_Passes_WithAnchor()
    {
        var d = Run(Presence(FspmRuleKind.Required, UserType));
        Assert.True(d.Passed);
        Assert.Contains("present", d.Reason);
        Assert.Equal(UserType, d.SubjectIdentity);
        Assert.Equal("FP-U", d.SubjectFingerprint);
        Assert.NotNull(d.Anchor);
        Assert.Equal("User.cs", d.Anchor!.Document);
    }

    [Fact]
    public void Required_Missing_Fails_NamingSubject()
    {
        var d = Run(Presence(FspmRuleKind.Required, Ghost));
        Assert.False(d.Passed);
        Assert.Contains(Ghost, d.Reason);
        Assert.Contains("missing", d.Reason);
        Assert.Null(d.Anchor);
        Assert.Equal("", d.SubjectFingerprint);
    }

    [Fact]
    public void Required_NeverMatchesDisplayName()
    {
        // "User" is a display name, not a LogicalId: no name inference.
        var d = Run(Presence(FspmRuleKind.Required, "User"));
        Assert.False(d.Passed);
        Assert.Contains("missing", d.Reason);
    }

    [Fact]
    public void Forbidden_Absent_Passes()
    {
        var d = Run(Presence(FspmRuleKind.Forbidden, Ghost));
        Assert.True(d.Passed);
        Assert.Contains("absent", d.Reason);
    }

    [Fact]
    public void Forbidden_Present_Fails()
    {
        var d = Run(Presence(FspmRuleKind.Forbidden, UserType));
        Assert.False(d.Passed);
        Assert.Contains("forbidden", d.Reason);
        Assert.NotNull(d.Anchor);
    }

    [Fact]
    public void Allowed_Passes_WhetherPresentOrAbsent()
    {
        var present = Run(Presence(FspmRuleKind.Allowed, UserType));
        var absent = Run(Presence(FspmRuleKind.Allowed, Ghost));
        Assert.True(present.Passed);
        Assert.True(absent.Passed);
        Assert.Contains("allowed", present.Reason);
        Assert.Contains("allowed", absent.Reason);
    }

    [Fact]
    public void ExactlyOne_SingleHit_Passes()
    {
        var d = Run(Presence(FspmRuleKind.ExactlyOne, NameMember, Ghost));
        Assert.True(d.Passed);
        Assert.Contains("exactly 1", d.Reason);
    }

    [Fact]
    public void ExactlyOne_TwoHits_Fails()
    {
        var d = Run(Presence(FspmRuleKind.ExactlyOne, NameMember, AgeMember));
        Assert.False(d.Passed);
        Assert.Contains("found 2", d.Reason);
    }

    [Fact]
    public void ExactlyOne_NoHits_Fails_ListingMissing()
    {
        var d = Run(Presence(FspmRuleKind.ExactlyOne, Ghost, "A|T:U.Ghost2"));
        Assert.False(d.Passed);
        Assert.Contains(Ghost, d.Reason);
    }

    [Fact]
    public void AtLeastOne_Hit_Passes()
    {
        var d = Run(Presence(FspmRuleKind.AtLeastOne, Ghost, CreateOp));
        Assert.True(d.Passed);
        Assert.Contains("at least one", d.Reason);
    }

    [Fact]
    public void AtLeastOne_NoHits_Fails()
    {
        var d = Run(Presence(FspmRuleKind.AtLeastOne, Ghost));
        Assert.False(d.Passed);
        Assert.Contains("none", d.Reason);
    }
}
