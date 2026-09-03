using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P13-H4: Type Relationships. Chief §十一 hard requirement: explicit
// interface implementation must resolve to the REAL Roslyn symbol —
// User.Name and IUser.Name must never merge by text name.
[Collection("RoslynWorkspace")]
public sealed class H4RelationshipTests
{
    [Fact]
    public async Task AdminUser_Reports_BaseType_And_Interface()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var admin = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Relations.AdminUser");
            var rel = TypeRelationshipExtractor.ExtractTypeRelationships(admin);

            Assert.Equal("SemanticGolden.Relations.BaseAccount", rel.BaseType);
            Assert.Contains("SemanticGolden.Relations.IUser", rel.Interfaces);
        }
    }

    [Fact]
    public async Task Override_Describe_ReportsOverriddenMethod()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var admin = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Relations.AdminUser");
            var describe = admin.GetMembers("Describe").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);
            var rel = TypeRelationshipExtractor.ExtractMethodRelationships(describe);

            Assert.NotNull(rel.OverriddenMethod);
            Assert.Contains("BaseAccount.Describe", rel.OverriddenMethod);
        }
    }

    [Fact]
    public async Task ExplicitInterfaceImpl_ResolvesToRealSymbol_NotMergedByName()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var compilation = compiled.Snapshot.Compilation;
            var admin = GoldenIdentity.RequireType(compilation, "SemanticGolden.Relations.AdminUser");

            // The implicit public property...
            var implicitName = admin.GetMembers("Name").OfType<IPropertySymbol>()
                .First(p => !p.ExplicitInterfaceImplementations.Any());
            // ...and the explicit implementation are DIFFERENT real symbols.
            var explicitName = admin.GetMembers().OfType<IPropertySymbol>()
                .First(p => p.ExplicitInterfaceImplementations.Any());

            Assert.NotSame(implicitName, explicitName);

            var rel = TypeRelationshipExtractor.ExtractPropertyRelationships(explicitName);
            Assert.Contains(rel.ExplicitInterfaceImplementations,
                e => e.Contains("IUser.Name"));

            // Their stable identities differ: no text-name merge.
            Assert.NotEqual(
                Foundry.FSPM.Compiler.Symbols.FspmSymbolIdentity.Create(implicitName),
                Foundry.FSPM.Compiler.Symbols.FspmSymbolIdentity.Create(explicitName));
        }
    }

    [Fact]
    public async Task Resolver_ExplicitImpl_DoesNotLeakIntoPlainPropertyLookup()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            // Plain lookup finds the public property (single match → Resolved).
            var result = compiled.Resolver.ResolveProperty(
                "SemanticGolden.Relations.AdminUser", "Name");
            Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
            Assert.Empty(((IPropertySymbol)result.Selected!.Symbol).ExplicitInterfaceImplementations);
        }
    }
}
