using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Symbols;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// G13-MEMBER-SHAPE-01: field / event identity + shape through the Fact.
// Asserts MemberKind + Name + DeclaringType + TypeShape + Identity +
// Fingerprint per member, and proves Property != Field, Field != Event,
// Event != Method (no fake-Property/Operation conversion in the factory).
[Collection("RoslynWorkspace")]
public sealed class MemberShapeTests
{
    private static async Task<INamedTypeSymbol> RequireHolderAsync(Compilation compilation)
    {
        await Task.CompletedTask;
        return GoldenIdentity.RequireType(compilation, "SemanticGolden.Members.MemberHolder");
    }

    [Fact]
    public async Task InstanceField_Fact_Has_FieldKind_And_StringShape()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var holder = await RequireHolderAsync(compiled.Snapshot.Compilation);
            var field = holder.GetMembers("Name").OfType<IFieldSymbol>().First();
            var fact = NativeSemanticFactFactory.Create(
                field,
                CompilationFor(compiled),
                AnchorFor(compiled, field));

            Assert.Equal(NativeSymbolKind.Field, fact.Kind);
            Assert.Equal("Name", fact.Name);
            Assert.Equal("MemberHolder", fact.Logical.ContainingTypeName);
            Assert.Equal("SemanticGolden.Members", fact.Logical.Namespace);
            Assert.Equal("string", fact.TypeShape!.OriginalDefinition);
            Assert.Null(fact.Operation);
            Assert.Equal("SemanticGolden|F:SemanticGolden.Members.MemberHolder.Name", fact.Identity.Value);
            Assert.Equal(64, fact.Fingerprint.Value.Length);
        }
    }

    [Fact]
    public async Task Static_Readonly_Fields_Keep_ModifierFacts()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var holder = await RequireHolderAsync(compiled.Snapshot.Compilation);

            var counter = holder.GetMembers("Counter").OfType<IFieldSymbol>().First();
            Assert.True(SymbolClassifier.Classify(counter).Visibility.IsStatic);

            var tag = holder.GetMembers("Tag").OfType<IFieldSymbol>().First();
            Assert.True(SymbolClassifier.Classify(tag).Visibility.IsReadOnly);
        }
    }

    [Fact]
    public async Task Event_Fact_Has_EventKind_And_Identity()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var holder = await RequireHolderAsync(compiled.Snapshot.Compilation);
            var ev = holder.GetMembers("Changed").OfType<IEventSymbol>().First();
            var fact = NativeSemanticFactFactory.Create(
                ev,
                CompilationFor(compiled),
                AnchorFor(compiled, ev));

            Assert.Equal(NativeSymbolKind.Event, fact.Kind);
            Assert.Equal("Changed", fact.Name);
            Assert.Equal("SemanticGolden|E:SemanticGolden.Members.MemberHolder.Changed", fact.Identity.Value);
            Assert.Null(fact.Operation);
        }
    }

    [Fact]
    public async Task MemberKinds_Are_Pairwise_Distinct()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var holder = await RequireHolderAsync(compiled.Snapshot.Compilation);
            var field = holder.GetMembers("Name").OfType<IFieldSymbol>().First();
            var property = holder.GetMembers("Label").OfType<IPropertySymbol>().First();
            var method = holder.GetMembers("Touch").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);
            var ev = holder.GetMembers("Changed").OfType<IEventSymbol>().First();

            var fieldId = FspmSymbolIdentity.Create(field);
            var propertyId = FspmSymbolIdentity.Create(property);
            var methodId = FspmSymbolIdentity.Create(method);
            var eventId = FspmSymbolIdentity.Create(ev);

            Assert.NotEqual(fieldId, propertyId);
            Assert.NotEqual(fieldId, eventId);
            Assert.NotEqual(eventId, methodId);
            Assert.NotEqual(propertyId, methodId);

            Assert.Equal(FspmSymbolKind.Property, FspmSymbolIdentity.GetKind(propertyId));
            Assert.Equal(NativeSymbolKind.Field, SymbolClassifier.Classify(field).Kind);
            Assert.Equal(NativeSymbolKind.Event, SymbolClassifier.Classify(ev).Kind);
        }
    }

    private static CompilationIdentity CompilationFor(
        Foundry.FSPM.Compiler.Compiler.FspmSemanticCompilationResult compiled) =>
        SemanticIdentityMint.MintCompilationIdentity(
            compiled.Snapshot.Compilation,
            compiled.Snapshot.ProjectName,
            compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
            "member-shape");

    private static SemanticSourceAnchor AnchorFor(
        Foundry.FSPM.Compiler.Compiler.FspmSemanticCompilationResult compiled, ISymbol symbol) =>
        new SemanticSourceAnchor(
            SemanticIdentityMint.MintLogicalIdentity(symbol),
            symbol.Locations.First().SourceTree?.FilePath ?? "<unknown>",
            DocumentationCommentId.CreateDeclarationId(symbol) ?? "<none>",
            FspmSourceLocation.From(symbol.Locations.First()));
}
