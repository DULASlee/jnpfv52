using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Semantic.Rule;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;
using Model = Foundry.FSPM.SemanticModel;

namespace Foundry.FSPM.Compiler.Tests;

// P14-04 integration: rules evaluated against the assembled FirstSlice
// real-project model. LogicalIds come from bound entries (never typed by
// hand); evidence pins the evaluation snapshot version. A Missing
// reference-level resolution for the same target does not move a rule
// verdict — the rule layer never sees resolutions (§8.14).
[Collection("RoslynWorkspace")]
public sealed class RuleFirstSliceIntegrationTests
{
    private static NativeSemanticFact FactFor(
        Foundry.FSPM.Compiler.Compiler.FspmSemanticCompilationResult compiled, ISymbol symbol)
    {
        var anchor = new SemanticSourceAnchor(
            SemanticIdentityMint.MintLogicalIdentity(symbol),
            symbol.Locations.First().SourceTree?.FilePath ?? "<unknown>",
            DocumentationCommentId.CreateDeclarationId(symbol) ?? "<none>",
            FspmSourceLocation.From(symbol.Locations.First()));
        var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
            compiled.Snapshot.Compilation,
            compiled.Snapshot.ProjectName,
            compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
            "rule-first-slice");
        return NativeSemanticFactFactory.Create(symbol, compilationIdentity, anchor);
    }

    private async Task<Model.FspmSemanticModel> AssembleFirstSliceAsync()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var compilation = compiled.Snapshot.Compilation;
            var user = GoldenIdentity.RequireType(compilation, "SemanticGolden.FirstSlice.User");
            var name = GoldenIdentity.RequireProperty(user, "Name");
            var age = user.GetMembers("Age").OfType<IFieldSymbol>().First();
            var changed = user.GetMembers("Changed").OfType<IEventSymbol>().First();
            var create = user.GetMembers("Create").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);

            var metadata = new FspmSemanticModelMetadata(
                SnapshotId: "rule-first-slice",
                SourceAssembly: "SemanticGolden",
                FactCount: 5);
            var (model, notes) = FspmModelBinder.Assemble(
                new[]
                {
                    FactFor(compiled, user),
                    FactFor(compiled, name),
                    FactFor(compiled, age),
                    FactFor(compiled, changed),
                    FactFor(compiled, create),
                },
                metadata);
            Assert.Empty(notes);
            return model;
        }
    }

    [Fact]
    public async Task Required_UserType_Passes_OnRealModel()
    {
        var model = await AssembleFirstSliceAsync();
        var userId = Assert.Single(model.Types).Identity.LogicalId;

        var rule = new FspmRule("R-FS-REQ", FspmRuleKind.Required,
            new[] { userId }, "", "slice has user type");
        var decision = FspmRuleEvaluator.Evaluate(new FspmRuleContext(model, "rule-first-slice"), rule);

        Assert.True(decision.Passed);
        Assert.NotNull(decision.Anchor);
        Assert.EndsWith("FirstSlice.cs", decision.Anchor!.Document);
    }

    [Fact]
    public async Task TypeCompatible_Name_IsString_OnRealModel()
    {
        var model = await AssembleFirstSliceAsync();
        var name = model.Members.First(m => m.Name == "Name" && m.MemberKind == "Property");

        var rule = new FspmRule("R-FS-TYPE", FspmRuleKind.TypeCompatible,
            new[] { name.Identity.LogicalId }, name.Type, "name keeps its bound type");
        var decision = FspmRuleEvaluator.Evaluate(new FspmRuleContext(model, "rule-first-slice"), rule);

        Assert.True(decision.Passed);
        Assert.Equal(name.Fingerprint, decision.SubjectFingerprint);
    }

    [Fact]
    public async Task TypeCompatible_WrongExpectation_Fails_OnRealModel()
    {
        var model = await AssembleFirstSliceAsync();
        var name = model.Members.First(m => m.Name == "Name" && m.MemberKind == "Property");

        var rule = new FspmRule("R-FS-TYPE-NEG", FspmRuleKind.TypeCompatible,
            new[] { name.Identity.LogicalId }, "int", "name is int");
        var decision = FspmRuleEvaluator.Evaluate(new FspmRuleContext(model, "rule-first-slice"), rule);

        Assert.False(decision.Passed);
        Assert.Contains(name.Type, decision.Reason);
    }

    [Fact]
    public async Task OperationCompatible_Create_Passes_OnRealModel()
    {
        var model = await AssembleFirstSliceAsync();
        var create = Assert.Single(model.Operations);

        var rule = new FspmRule("R-FS-OP", FspmRuleKind.OperationCompatible,
            new[] { create.Identity.LogicalId }, create.ReturnType, "create keeps its return");
        var decision = FspmRuleEvaluator.Evaluate(new FspmRuleContext(model, "rule-first-slice"), rule);

        Assert.True(decision.Passed);

        var evidence = FspmEvidenceRecorder.Record(decision, "rule-first-slice");
        Assert.Equal("rule-first-slice", evidence.SnapshotVersion);
        Assert.Equal(create.Fingerprint, evidence.SubjectFingerprint);
    }

    [Fact]
    public async Task Missing_Resolution_DoesNotMove_RuleVerdict()
    {
        var model = await AssembleFirstSliceAsync();
        var userId = Assert.Single(model.Types).Identity.LogicalId;

        // A reference-level Missing for the same target exists alongside...
        var missing = new FspmReferenceResolution(
            FspmReferenceStatus.Missing, false, "no entry", null, "", "Type", "");
        Assert.Equal(FspmReferenceStatus.Missing, missing.Status);

        // ...but the rule layer never receives resolutions: entry present → pass.
        var rule = new FspmRule("R-FS-LAYER", FspmRuleKind.Required,
            new[] { userId }, "", "presence ignores resolutions");
        var decision = FspmRuleEvaluator.Evaluate(new FspmRuleContext(model, "rule-first-slice"), rule);
        Assert.True(decision.Passed);
    }
}
