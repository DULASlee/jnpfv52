using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Semantic.Reference;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-02-F: ParameterRef. Parameters are embedded (no standalone
// identity): TargetIdentity is the owner operation's LogicalId and
// Position selects within its list. Structural addressing, documented.
[Collection("RoslynWorkspace")]
public sealed class RefParameterTests
{
    private static Foundry.FSPM.SemanticModel.FspmSemanticModel BuildModel(
        Foundry.FSPM.Compiler.Compiler.FspmSemanticCompilationResult compiled)
    {
        var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
            compiled.Snapshot.Compilation,
            compiled.Snapshot.ProjectName,
            compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
            "ref-parameter");
        var facts = compiled.Index.Records.Select(record =>
        {
            var anchor = new SemanticSourceAnchor(
                SemanticIdentityMint.MintLogicalIdentity(record.Symbol),
                record.Symbol.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? "<metadata>",
                DocumentationCommentId.CreateDeclarationId(record.Symbol) ?? "<none>",
                FspmSourceLocation.From(record.Symbol.Locations.FirstOrDefault() ?? Location.None));
            return NativeSemanticFactFactory.Create(record.Symbol, compilationIdentity, anchor);
        }).ToArray();
        var metadata = new FspmSemanticModelMetadata("ref-parameter", "SemanticGolden", facts.Length);
        return FspmModelBinder.Assemble(facts, metadata).Model;
    }

    [Fact]
    public async Task ParameterRef_Valid_Returns_Parameter_Details()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var create = model.Operations.First(o => o.Name == "Create");

            var result = FspmReferenceResolver.ResolveParameterRef(
                new FspmParameterRef(create.Identity, "Create/phoneNumber", 0), model);

            Assert.Equal(FspmReferenceStatus.Valid, result.Status);
            Assert.True(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
            Assert.Equal("Parameter:phoneNumber", result.TargetKind);
            Assert.Equal(create.Identity.LogicalId, result.Owner);
        }
    }

    [Fact]
    public async Task ParameterRef_Bad_Position_Is_Missing()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var create = model.Operations.First(o => o.Name == "Create");

            var result = FspmReferenceResolver.ResolveParameterRef(
                new FspmParameterRef(create.Identity, "Create/position-99", 99), model);

            Assert.Equal(FspmReferenceStatus.Missing, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }

    [Fact]
    public async Task ParameterRef_Owner_Is_Type_Is_WrongKind()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var user = model.Types.First(t => t.Name == "User");

            var result = FspmReferenceResolver.ResolveParameterRef(
                new FspmParameterRef(user.Identity, "User/0", 0), model);

            Assert.Equal(FspmReferenceStatus.WrongKind, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }

    [Fact]
    public async Task ParameterRef_Stale_Operation_Fingerprint_Is_Stale()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var create = model.Operations.First(o => o.Name == "Create");

            var result = FspmReferenceResolver.ResolveParameterRef(
                new FspmParameterRef(create.Identity, "Create/phoneNumber", 0, ExpectedFingerprint: "DEADBEEF"), model);

            Assert.Equal(FspmReferenceStatus.Stale, result.Status);
            Assert.True(result.IsResolved);
            Assert.Contains("DEADBEEF", result.Reason);
        }
    }
}
