using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Semantic.Reference;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-02-E: OperationRef. Overloads are distinct identities; owner and
// fingerprint checks mirror the member path.
[Collection("RoslynWorkspace")]
public sealed class RefOperationTests
{
    private static Foundry.FSPM.SemanticModel.FspmSemanticModel BuildModel(
        Foundry.FSPM.Compiler.Compiler.FspmSemanticCompilationResult compiled)
    {
        var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
            compiled.Snapshot.Compilation,
            compiled.Snapshot.ProjectName,
            compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
            "ref-operation");
        var facts = compiled.Index.Records.Select(record =>
        {
            var anchor = new SemanticSourceAnchor(
                SemanticIdentityMint.MintLogicalIdentity(record.Symbol),
                record.Symbol.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? "<metadata>",
                DocumentationCommentId.CreateDeclarationId(record.Symbol) ?? "<none>",
                FspmSourceLocation.From(record.Symbol.Locations.FirstOrDefault() ?? Location.None));
            return NativeSemanticFactFactory.Create(record.Symbol, compilationIdentity, anchor);
        }).ToArray();
        var metadata = new FspmSemanticModelMetadata("ref-operation", "SemanticGolden", facts.Length);
        return FspmModelBinder.Assemble(facts, metadata).Model;
    }

    [Fact]
    public async Task OverloadRefs_Resolve_Distinctly()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var creates = model.Operations.Where(o => o.Name == "Create").ToArray();
            Assert.True(creates.Length >= 2);

            foreach (var create in creates)
            {
                var result = FspmReferenceResolver.ResolveOperationRef(
                    new FspmOperationRef(create.Identity, "Create", create.DeclaringTypeId), model);

                Assert.Equal(FspmReferenceStatus.Valid, result.Status);
                Assert.True(result.IsResolved);
                Assert.False(string.IsNullOrWhiteSpace(result.Reason));
                Assert.Equal(create.Identity, result.TargetIdentity);
            }

            Assert.Equal(creates.Length, creates.Select(c => c.Identity.LogicalId).Distinct().Count());
        }
    }

    [Fact]
    public async Task OperationRef_Pointing_At_Property_Is_WrongKind()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var phone = model.Members.First(m => m.Name == "PhoneNumber");

            var result = FspmReferenceResolver.ResolveOperationRef(
                new FspmOperationRef(phone.Identity, "PhoneNumber"), model);

            Assert.Equal(FspmReferenceStatus.WrongKind, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }

    [Fact]
    public async Task OperationRef_With_Wrong_Owner_Is_WrongOwner()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var create = model.Operations.First(o => o.Name == "Create");
            var other = model.Types.First(t => t.Name == "OtherUser");

            var result = FspmReferenceResolver.ResolveOperationRef(
                new FspmOperationRef(create.Identity, "Create", other.Identity.LogicalId), model);

            Assert.Equal(FspmReferenceStatus.WrongOwner, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }

    [Fact]
    public async Task OperationRef_Missing_Reports_Missing()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var bogus = new FspmSemanticIdentity("Nope|M:Nope.Nope.Wibble()", "00");

            var result = FspmReferenceResolver.ResolveOperationRef(
                new FspmOperationRef(bogus, "Wibble"), model);

            Assert.Equal(FspmReferenceStatus.Missing, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }
}
