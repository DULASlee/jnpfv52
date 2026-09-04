using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Semantic.Reference;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-02-A: TypeRef. References resolve by SemanticIdentity only —
// DisplayName is display, never identity (Row: bogus identity + real
// display name must still be Missing).
[Collection("RoslynWorkspace")]
public sealed class RefTypeTests
{
    private static Foundry.FSPM.SemanticModel.FspmSemanticModel BuildModel(
        Foundry.FSPM.Compiler.Compiler.FspmSemanticCompilationResult compiled)
    {
        var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
            compiled.Snapshot.Compilation,
            compiled.Snapshot.ProjectName,
            compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
            "ref-type");
        var facts = compiled.Index.Records.Select(record =>
        {
            var anchor = new SemanticSourceAnchor(
                SemanticIdentityMint.MintLogicalIdentity(record.Symbol),
                record.Symbol.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? "<metadata>",
                DocumentationCommentId.CreateDeclarationId(record.Symbol) ?? "<none>",
                FspmSourceLocation.From(record.Symbol.Locations.FirstOrDefault() ?? Location.None));
            return NativeSemanticFactFactory.Create(record.Symbol, compilationIdentity, anchor);
        }).ToArray();
        var metadata = new FspmSemanticModelMetadata("ref-type", "SemanticGolden", facts.Length);
        return FspmModelBinder.Assemble(facts, metadata).Model;
    }

    [Fact]
    public async Task TypeRef_Valid_Returns_Full_Resolution()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var user = model.Types.First(t => t.Name == "User");

            var reference = new FspmTypeRef(user.Identity, "User");
            var result = FspmReferenceResolver.ResolveTypeRef(reference, model);

            Assert.Equal(FspmReferenceStatus.Valid, result.Status);
            Assert.True(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
            Assert.Equal(user.Identity, result.TargetIdentity);
            Assert.Equal(user.Fingerprint, result.TargetFingerprint);
            Assert.Equal("Type", result.TargetKind);
            Assert.Equal(string.Empty, result.Owner);
        }
    }

    [Fact]
    public async Task TypeRef_BogusIdentity_RealDisplayName_Is_Missing()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var bogus = new FspmSemanticIdentity("Nope|T:Nope.Nope", "00");

            var result = FspmReferenceResolver.ResolveTypeRef(new FspmTypeRef(bogus, "User"), model);

            Assert.Equal(FspmReferenceStatus.Missing, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }

    [Fact]
    public async Task TypeRef_Pointing_At_Property_Is_WrongKind()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var phone = model.Members.First(m => m.Name == "PhoneNumber");

            var result = FspmReferenceResolver.ResolveTypeRef(
                new FspmTypeRef(phone.Identity, "PhoneNumber"), model);

            Assert.Equal(FspmReferenceStatus.WrongKind, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }

    [Fact]
    public async Task TypeRef_StaleFingerprint_Is_Stale_But_Resolved()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var user = model.Types.First(t => t.Name == "User");

            var reference = new FspmTypeRef(user.Identity, "User", ExpectedFingerprint: "DEADBEEF");
            var result = FspmReferenceResolver.ResolveTypeRef(reference, model);

            Assert.Equal(FspmReferenceStatus.Stale, result.Status);
            Assert.True(result.IsResolved);
            Assert.Contains("DEADBEEF", result.Reason);
            Assert.Contains(user.Fingerprint, result.Reason);
        }
    }
}
