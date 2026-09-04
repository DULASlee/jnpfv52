using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Semantic.Reference;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-02-G/H: RelationRef + unified ValidateReference/ValidateAll.
// Relations are embedded (FromId-anchored); the resolver never invents
// target identities.
[Collection("RoslynWorkspace")]
public sealed class RefRelationTests
{
    private static Foundry.FSPM.SemanticModel.FspmSemanticModel BuildModel(
        Foundry.FSPM.Compiler.Compiler.FspmSemanticCompilationResult compiled)
    {
        var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
            compiled.Snapshot.Compilation,
            compiled.Snapshot.ProjectName,
            compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
            "ref-relation");
        var facts = compiled.Index.Records.Select(record =>
        {
            var anchor = new SemanticSourceAnchor(
                SemanticIdentityMint.MintLogicalIdentity(record.Symbol),
                record.Symbol.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? "<metadata>",
                DocumentationCommentId.CreateDeclarationId(record.Symbol) ?? "<none>",
                FspmSourceLocation.From(record.Symbol.Locations.FirstOrDefault() ?? Location.None));
            return NativeSemanticFactFactory.Create(record.Symbol, compilationIdentity, anchor);
        }).ToArray();
        var metadata = new FspmSemanticModelMetadata("ref-relation", "SemanticGolden", facts.Length);
        return FspmModelBinder.Assemble(facts, metadata).Model;
    }

    [Fact]
    public async Task RelationRef_Inheritance_Resolves()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var admin = model.Types.First(t => t.Name == "AdminUser");

            var result = FspmReferenceResolver.ResolveRelationRef(
                new FspmRelationRef(admin.Identity, "AdminUser/base", "Inheritance", "BaseAccount"), model);

            Assert.Equal(FspmReferenceStatus.Valid, result.Status);
            Assert.True(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
            Assert.Equal("Relation:Inheritance", result.TargetKind);
            Assert.Equal(admin.Identity.LogicalId, result.Owner);
        }
    }

    [Fact]
    public async Task RelationRef_Missing_Kind_Reports_Missing()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            // NOTE: every class inherits System.Object, so "Inheritance"
            // always exists; a class with no interfaces (User) is the
            // honest Missing case for "Implementation".
            var model = BuildModel(compiled);
            var user = model.Types.First(t => t.Name == "User");

            var result = FspmReferenceResolver.ResolveRelationRef(
                new FspmRelationRef(user.Identity, "User/ifaces", "Implementation"), model);

            Assert.Equal(FspmReferenceStatus.Missing, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }

    [Fact]
    public async Task RelationRef_With_Fingerprint_Pin_Is_Invalid()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var admin = model.Types.First(t => t.Name == "AdminUser");

            var result = FspmReferenceResolver.ResolveRelationRef(
                new FspmRelationRef(admin.Identity, "AdminUser/base", "Inheritance", "BaseAccount", ExpectedFingerprint: "XX"), model);

            Assert.Equal(FspmReferenceStatus.Invalid, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }

    [Fact]
    public async Task ValidateReference_Dispatches_All_Kinds()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var user = model.Types.First(t => t.Name == "User");
            var phone = model.Members.First(m => m.Name == "PhoneNumber");
            var create = model.Operations.First(o => o.Name == "Create");
            var admin = model.Types.First(t => t.Name == "AdminUser");

            var results = FspmReferenceResolver.ValidateAll(new Foundry.FSPM.SemanticModel.FspmSemanticReference[]
            {
                new FspmTypeRef(user.Identity, "User"),
                new FspmEntityRef(user.Identity, "User"),
                new FspmPropertyRef(phone.Identity, "PhoneNumber", phone.DeclaringTypeId),
                new FspmOperationRef(create.Identity, "Create", create.DeclaringTypeId),
                new FspmParameterRef(create.Identity, "Create/phoneNumber", 0),
                new FspmRelationRef(admin.Identity, "AdminUser/base", "Inheritance", "BaseAccount"),
                new FspmTypeRef(new FspmSemanticIdentity("Nope|T:Nope", "00"), "Nope"),
            }, model);

            Assert.Equal(7, results.Count);
            Assert.True(results.Take(6).All(r => r.Status == FspmReferenceStatus.Valid));
            Assert.Equal(FspmReferenceStatus.Missing, results[6].Status);
            Assert.All(results, r => Assert.False(string.IsNullOrWhiteSpace(r.Reason)));
        }
    }
}
