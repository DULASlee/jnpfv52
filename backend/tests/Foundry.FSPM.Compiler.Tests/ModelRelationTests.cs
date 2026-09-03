using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-01 STEP 6-J: Relation records + binder Relation path. Inheritance,
// Implementation, Override and ExplicitInterfaceImplementation come from
// P13 relationship facts; containment rides on DeclaringTypeId (no dup).
[Collection("RoslynWorkspace")]
public sealed class ModelRelationTests
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
            "model-relation");
        return NativeSemanticFactFactory.Create(symbol, compilationIdentity, anchor);
    }

    [Fact]
    public async Task AdminUser_Yields_Inheritance_And_Implementation()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var admin = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Relations.AdminUser");

            var (relations, notes) = FspmModelBinder.BindRelations(new[] { FactFor(compiled, admin) });

            Assert.Empty(notes);
            var inheritance = Assert.Single(relations, r => r.Kind == "Inheritance");
            Assert.Contains("BaseAccount", inheritance.Target);
            Assert.Equal(
                "SemanticGolden|T:SemanticGolden.Relations.AdminUser",
                inheritance.FromId);
            var implementation = Assert.Single(relations, r => r.Kind == "Implementation");
            Assert.Contains("IUser", implementation.Target);
        }
    }

    [Fact]
    public async Task Override_Yields_Override_Relation()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var admin = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Relations.AdminUser");
            var describe = admin.GetMembers("Describe").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);

            var (relations, _) = FspmModelBinder.BindRelations(new[] { FactFor(compiled, describe) });

            var over = Assert.Single(relations);
            Assert.Equal("Override", over.Kind);
            Assert.Contains("BaseAccount.Describe", over.Target);
        }
    }

    [Fact]
    public async Task ExplicitImpl_Yields_Distinct_Relation()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var admin = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Relations.AdminUser");
            var explicitName = admin.GetMembers().OfType<IPropertySymbol>()
                .First(p => p.ExplicitInterfaceImplementations.Any());

            var (relations, _) = FspmModelBinder.BindRelations(new[] { FactFor(compiled, explicitName) });

            var rel = Assert.Single(relations);
            Assert.Equal("ExplicitInterfaceImplementation", rel.Kind);
            Assert.Contains("IUser.Name", rel.Target);
        }
    }

    [Fact]
    public async Task Containment_Rides_On_DeclaringTypeId()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var admin = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Relations.AdminUser");
            var name = admin.GetMembers("Name").OfType<IPropertySymbol>()
                .First(p => !p.ExplicitInterfaceImplementations.Any());

            var (members, _) = FspmModelBinder.BindMembers(new[]
            {
                FactFor(compiled, admin),
                FactFor(compiled, name),
            });

            // No separate Declares/Contains records: the member points at
            // its owner type. P14-02 will lift these into references.
            Assert.Equal(
                "SemanticGolden|T:SemanticGolden.Relations.AdminUser",
                Assert.Single(members).DeclaringTypeId);
        }
    }
}
