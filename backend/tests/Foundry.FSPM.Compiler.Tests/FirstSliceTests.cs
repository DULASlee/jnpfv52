using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-01 §8.15: first real project (§8.15 verbatim sample). Assembles the
// whole slice through Facts → Model and asserts the exact tree plus the
// four inequalities. This is the P14-01 acceptance specimen.
[Collection("RoslynWorkspace")]
public sealed class FirstSliceTests
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
            "first-slice");
        return NativeSemanticFactFactory.Create(symbol, compilationIdentity, anchor);
    }

    [Fact]
    public async Task FirstSlice_Assembles_Exact_Tree()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var compilation = compiled.Snapshot.Compilation;
            var user = GoldenIdentity.RequireType(compilation, "SemanticGolden.FirstSlice.User");
            var name = GoldenIdentity.RequireProperty(user, "Name");
            var age = user.GetMembers("Age").OfType<IFieldSymbol>().First();
            var changed = user.GetMembers("Changed").OfType<IEventSymbol>().First();
            var create = user.GetMembers("Create").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);

            var metadata = new FspmSemanticModelMetadata(
                SnapshotId: "first-slice",
                SourceAssembly: "SemanticGolden",
                FactCount: 5);
            var (model, notes) = FspmModelBinder.Assemble(
                new[]
                {
                    FactFor(compiled, user),
                    FactFor(compiled, name),
                    FactFor(compiled, age),
                    FactFor(compiled, changed),
                    FactFor(compiled, create),
                },
                metadata);

            Assert.Empty(notes);

            var modelUser = Assert.Single(model.Types);
            Assert.Equal("User", modelUser.Name);
            Assert.Equal("Class", modelUser.TypeKind);

            Assert.Contains(model.Members, m => m.Name == "Name" && m.MemberKind == "Property");
            Assert.Contains(model.Members, m => m.Name == "Age" && m.MemberKind == "Field");
            Assert.Contains(model.Members, m => m.Name == "Changed" && m.MemberKind == "Event");

            var createOp = Assert.Single(model.Operations);
            Assert.Equal("Create", createOp.Name);
            Assert.Equal(2, createOp.Parameters.Count);
            Assert.True(createOp.Parameters[1].IsOptional);

            Assert.Contains(model.Relations, r =>
                r.Kind == "Implementation" && r.Target.Contains("IUser"));
            Assert.Contains(model.Relations, r =>
                r.Kind == "Declares" && r.ResolvedTargetId.Contains(".Name"));

            Assert.Equal(5, model.Metadata.FactCount);
            Assert.All(model.Types.Select(t => t.State)
                .Concat(model.Members.Select(m => m.State))
                .Concat(model.Operations.Select(o => o.State)),
                s => Assert.Equal(FspmSemanticState.Resolved, s));
        }
    }

    [Fact]
    public async Task FirstSlice_MemberKinds_Are_Pairwise_Distinct()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.FirstSlice.User");
            var name = GoldenIdentity.RequireProperty(user, "Name");
            var age = user.GetMembers("Age").OfType<IFieldSymbol>().First();
            var changed = user.GetMembers("Changed").OfType<IEventSymbol>().First();
            var create = user.GetMembers("Create").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);

            var nameId = Foundry.FSPM.Compiler.Symbols.FspmSymbolIdentity.Create(name);
            var ageId = Foundry.FSPM.Compiler.Symbols.FspmSymbolIdentity.Create(age);
            var changedId = Foundry.FSPM.Compiler.Symbols.FspmSymbolIdentity.Create(changed);
            var createId = Foundry.FSPM.Compiler.Symbols.FspmSymbolIdentity.Create(create);

            // Property != Field, Field != Event, Event != Method.
            Assert.NotEqual(nameId, ageId);
            Assert.NotEqual(ageId, changedId);
            Assert.NotEqual(changedId, createId);

            // User.Name != IUser.Name (explicit impl stays distinct).
            var explicitName = user.GetMembers().OfType<IPropertySymbol>()
                .First(p => p.ExplicitInterfaceImplementations.Any());
            var explicitId = Foundry.FSPM.Compiler.Symbols.FspmSymbolIdentity.Create(explicitName);
            Assert.NotEqual(nameId, explicitId);
        }
    }
}
