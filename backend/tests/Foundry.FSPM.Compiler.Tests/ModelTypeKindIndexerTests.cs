using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// Gap-A/B tests (P14-01 temp-fix): model TypeKind comes from the Fact
// (never re-derived), and indexer facts bind as Operations.
[Collection("RoslynWorkspace")]
public sealed class ModelTypeKindIndexerTests
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
            "model-kind");
        return NativeSemanticFactFactory.Create(symbol, compilationIdentity, anchor);
    }

    [Fact]
    public async Task User_TypeKind_Is_Class_From_Fact()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Domain.User");

            var (types, _) = FspmModelBinder.BindTypes(new[] { FactFor(compiled, user) });

            Assert.Equal("Class", Assert.Single(types).TypeKind);
        }
    }

    [Fact]
    public async Task Interface_TypeKind_Is_Interface_From_Fact()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var iface = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Relations.IUser");

            var (types, _) = FspmModelBinder.BindTypes(new[] { FactFor(compiled, iface) });

            Assert.Equal("Interface", Assert.Single(types).TypeKind);
        }
    }

    [Fact]
    public async Task Indexer_Binds_As_Operation_With_Index_Parameter()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var service = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Operations.OpService");
            var indexer = service.GetMembers().OfType<IPropertySymbol>().First(p => p.IsIndexer);

            var (operations, notes) = FspmModelBinder.BindOperations(new[]
            {
                FactFor(compiled, service),
                FactFor(compiled, indexer),
            });

            var operation = Assert.Single(operations);
            Assert.Empty(notes);
            Assert.Equal("Indexer", operation.OperationKind);
            var index = Assert.Single(operation.Parameters);
            Assert.Equal("index", index.Name);
            Assert.Equal("int", index.Type);
        }
    }

    [Fact]
    public async Task Indexer_Fact_Keeps_Property_Identity_Not_Method()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var service = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Operations.OpService");
            var indexer = service.GetMembers().OfType<IPropertySymbol>().First(p => p.IsIndexer);

            // The indexer is NOT merged into a fake ordinary method: its
            // stable id is the property-form documentation id.
            var fact = FactFor(compiled, indexer);
            Assert.Contains(".Item(", fact.Identity.Value);
        }
    }
}
