using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Semantic.Reference;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-02-B: EntityRef. Same identity machinery as TypeRef; the record
// type keeps the entity role explicit for P14-03 construction.
[Collection("RoslynWorkspace")]
public sealed class RefEntityTests
{
    private static Foundry.FSPM.SemanticModel.FspmSemanticModel BuildModel(
        Foundry.FSPM.Compiler.Compiler.FspmSemanticCompilationResult compiled)
    {
        var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
            compiled.Snapshot.Compilation,
            compiled.Snapshot.ProjectName,
            compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
            "ref-entity");
        var facts = compiled.Index.Records.Select(record =>
        {
            var anchor = new SemanticSourceAnchor(
                SemanticIdentityMint.MintLogicalIdentity(record.Symbol),
                record.Symbol.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? "<metadata>",
                DocumentationCommentId.CreateDeclarationId(record.Symbol) ?? "<none>",
                FspmSourceLocation.From(record.Symbol.Locations.FirstOrDefault() ?? Location.None));
            return NativeSemanticFactFactory.Create(record.Symbol, compilationIdentity, anchor);
        }).ToArray();
        var metadata = new FspmSemanticModelMetadata("ref-entity", "SemanticGolden", facts.Length);
        return FspmModelBinder.Assemble(facts, metadata).Model;
    }

    [Fact]
    public async Task EntityRef_Valid_Returns_Full_Resolution()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var user = model.Types.First(t => t.Name == "User");

            var result = FspmReferenceResolver.ResolveEntityRef(
                new FspmEntityRef(user.Identity, "User"), model);

            Assert.Equal(FspmReferenceStatus.Valid, result.Status);
            Assert.True(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
            Assert.Equal(user.Identity, result.TargetIdentity);
            Assert.Equal(user.Fingerprint, result.TargetFingerprint);
            Assert.Equal("Type", result.TargetKind);
        }
    }

    [Fact]
    public async Task EntityRef_Pointing_At_Method_Is_WrongKind()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var create = model.Operations.First(o => o.Name == "Create");

            var result = FspmReferenceResolver.ResolveEntityRef(
                new FspmEntityRef(create.Identity, "Create"), model);

            Assert.Equal(FspmReferenceStatus.WrongKind, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }

    [Fact]
    public async Task EntityRef_Missing_Reports_Missing()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var bogus = new FspmSemanticIdentity("Nope|T:Nope.Nope", "00");

            var result = FspmReferenceResolver.ResolveEntityRef(
                new FspmEntityRef(bogus, "Nope"), model);

            Assert.Equal(FspmReferenceStatus.Missing, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }
}
