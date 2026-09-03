using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-01 STEP 6-F: SemanticType + binder Type path. Facts in (P13),
// pure model out. No Roslyn re-lookup inside the binder.
[Collection("RoslynWorkspace")]
public sealed class ModelTypeTests
{
    private static NativeSemanticFact FactForType(
        Foundry.FSPM.Compiler.Compiler.FspmSemanticCompilationResult compiled, string metadataName)
    {
        var type = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, metadataName);
        var anchor = new SemanticSourceAnchor(
            SemanticIdentityMint.MintLogicalIdentity(type),
            type.Locations.First().SourceTree?.FilePath ?? "<unknown>",
            DocumentationCommentId.CreateDeclarationId(type) ?? "<none>",
            FspmSourceLocation.From(type.Locations.First()));
        var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
            compiled.Snapshot.Compilation,
            compiled.Snapshot.ProjectName,
            compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
            "model-type");
        return NativeSemanticFactFactory.Create(type, compilationIdentity, anchor);
    }

    [Fact]
    public async Task UserType_Maps_All_Required_Fields()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var (types, notes) = FspmModelBinder.BindTypes(
                new[] { FactForType(compiled, "SemanticGolden.Domain.User") });

            var type = Assert.Single(types);
            Assert.Empty(notes);
            Assert.Equal("User", type.Name);
            Assert.Equal("SemanticGolden.Domain", type.Namespace);
            Assert.Equal("Type", type.Kind);
            Assert.Equal(0, type.GenericArity);
            Assert.Equal("SemanticGolden|T:SemanticGolden.Domain.User", type.Identity.LogicalId);
            Assert.Equal(64, type.Identity.Fingerprint.Length);
            Assert.Equal(FspmSemanticState.Resolved, type.State);
            Assert.EndsWith("User.cs", type.Anchor.Document);
        }
    }

    [Fact]
    public async Task GenericDefinition_Reports_Arity_One()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var (types, _) = FspmModelBinder.BindTypes(
                new[] { FactForType(compiled, "SemanticGolden.Shapes.ShapeRepository`1") });

            Assert.Equal(1, Assert.Single(types).GenericArity);
        }
    }

    [Fact]
    public async Task NullableShape_Differs_String_Vs_NullableString()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var compilation = compiled.Snapshot.Compilation;
            var holder = GoldenIdentity.RequireType(compilation, "SemanticGolden.Shapes.ShapeHolder");

            FspmSemanticType BindTypeSymbol(ITypeSymbol symbol)
            {
                var anchor = new SemanticSourceAnchor(
                    SemanticIdentityMint.MintLogicalIdentity(symbol),
                    symbol.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? "<metadata>",
                    DocumentationCommentId.CreateDeclarationId(symbol) ?? "<none>",
                    FspmSourceLocation.From(symbol.Locations.FirstOrDefault() ?? Location.None));
                var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
                    compilation,
                    compiled.Snapshot.ProjectName,
                    compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
                    "model-type-null");
                var fact = NativeSemanticFactFactory.Create(symbol, compilationIdentity, anchor);
                return Assert.Single(FspmModelBinder.BindTypes(new[] { fact }).Types);
            }

            var plain = BindTypeSymbol(
                (INamedTypeSymbol)GoldenIdentity.RequireProperty(holder, "Name").Type);
            var nullable = BindTypeSymbol(
                (INamedTypeSymbol)GoldenIdentity.RequireProperty(holder, "MaybeName").Type);

            Assert.NotEqual(plain.NullableShape, nullable.NullableShape);
        }
    }
}
