using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// Gap-C tests: Declaration/Containment are explicit relation records
// (not just implied by DeclaringTypeId). "Declares" points from a type
// to a directly-declared member; "Contains" is the reverse direction.
// Both carry resolved model identities on both ends (no display-only
// links) because Assemble sees the whole batch.
[Collection("RoslynWorkspace")]
public sealed class ModelContainmentTests
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
            "model-containment");
        return NativeSemanticFactFactory.Create(symbol, compilationIdentity, anchor);
    }

    [Fact]
    public async Task Declares_Relation_Links_Type_To_Direct_Member()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Domain.User");
            var phone = GoldenIdentity.RequireProperty(user, "PhoneNumber");

            var (relations, notes) = FspmModelBinder.BindRelations(new[]
            {
                FactFor(compiled, user),
                FactFor(compiled, phone),
            });

            Assert.Empty(notes);
            var declares = Assert.Single(relations, r => r.Kind == "Declares");
            Assert.Equal("SemanticGolden|T:SemanticGolden.Domain.User", declares.FromId);
            Assert.Equal("SemanticGolden|P:SemanticGolden.Domain.User.PhoneNumber",
                declares.ResolvedTargetId);
            Assert.Contains("PhoneNumber", declares.Target);
        }
    }

    [Fact]
    public async Task Contains_Relation_Is_Reverse_Of_Declares()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Domain.User");
            var phone = GoldenIdentity.RequireProperty(user, "PhoneNumber");

            var (relations, _) = FspmModelBinder.BindRelations(new[]
            {
                FactFor(compiled, user),
                FactFor(compiled, phone),
            });

            var contains = Assert.Single(relations, r => r.Kind == "Contains");
            Assert.Equal("SemanticGolden|P:SemanticGolden.Domain.User.PhoneNumber", contains.FromId);
            Assert.Equal("SemanticGolden|T:SemanticGolden.Domain.User", contains.ResolvedTargetId);
        }
    }

    [Fact]
    public async Task Inherited_Member_Produces_No_Declares_From_Derived_Type()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            // DerivedUser inherits Name from BaseUser: the derived type
            // must NOT claim to declare it (no text-name merge).
            var derived = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Domain.DerivedUser");
            var baseUser = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Domain.BaseUser");
            var nameOnBase = GoldenIdentity.RequireProperty(baseUser, "Name");

            var (relations, _) = FspmModelBinder.BindRelations(new[]
            {
                FactFor(compiled, derived),
                FactFor(compiled, baseUser),
                FactFor(compiled, nameOnBase),
            });

            Assert.DoesNotContain(relations, r =>
                r.Kind == "Declares" && r.FromId.Contains("DerivedUser"));
            Assert.Contains(relations, r =>
                r.Kind == "Declares" && r.FromId.Contains("BaseUser"));
        }
    }
}
