using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Semantic.Reference;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-02-C/D: MemberRef + Property/Field/Event. Owner is validated
// against DeclaringTypeId; kind mismatches and owner mismatches are
// distinct states with reasons.
[Collection("RoslynWorkspace")]
public sealed class RefMemberTests
{
    private static Foundry.FSPM.SemanticModel.FspmSemanticModel BuildModel(
        Foundry.FSPM.Compiler.Compiler.FspmSemanticCompilationResult compiled)
    {
        var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
            compiled.Snapshot.Compilation,
            compiled.Snapshot.ProjectName,
            compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
            "ref-member");
        var facts = compiled.Index.Records.Select(record =>
        {
            var anchor = new SemanticSourceAnchor(
                SemanticIdentityMint.MintLogicalIdentity(record.Symbol),
                record.Symbol.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? "<metadata>",
                DocumentationCommentId.CreateDeclarationId(record.Symbol) ?? "<none>",
                FspmSourceLocation.From(record.Symbol.Locations.FirstOrDefault() ?? Location.None));
            return NativeSemanticFactFactory.Create(record.Symbol, compilationIdentity, anchor);
        }).ToArray();
        var metadata = new FspmSemanticModelMetadata("ref-member", "SemanticGolden", facts.Length);
        return FspmModelBinder.Assemble(facts, metadata).Model;
    }

    [Fact]
    public async Task PropertyRef_With_Correct_Owner_Is_Valid()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var phone = model.Members.First(m => m.Name == "PhoneNumber");

            var result = FspmReferenceResolver.ResolveMemberRef(
                new FspmPropertyRef(phone.Identity, "User.PhoneNumber", phone.DeclaringTypeId), model);

            Assert.Equal(FspmReferenceStatus.Valid, result.Status);
            Assert.True(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
            Assert.Equal(phone.Identity, result.TargetIdentity);
            Assert.Equal("Property", result.TargetKind);
            Assert.Equal(phone.DeclaringTypeId, result.Owner);
            Assert.Equal(phone.Fingerprint, result.TargetFingerprint);
        }
    }

    [Fact]
    public async Task PropertyRef_With_Wrong_Owner_Is_WrongOwner()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var phone = model.Members.First(m => m.Name == "PhoneNumber");
            var other = model.Types.First(t => t.Name == "OtherUser");

            var result = FspmReferenceResolver.ResolveMemberRef(
                new FspmPropertyRef(phone.Identity, "User.PhoneNumber", other.Identity.LogicalId), model);

            Assert.Equal(FspmReferenceStatus.WrongOwner, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
            Assert.Equal(phone.DeclaringTypeId, result.Owner);
        }
    }

    [Fact]
    public async Task FieldRef_Pointing_At_Property_Is_WrongKind()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var phone = model.Members.First(m => m.Name == "PhoneNumber");

            var result = FspmReferenceResolver.ResolveMemberRef(
                new FspmFieldRef(phone.Identity, "PhoneNumber"), model);

            Assert.Equal(FspmReferenceStatus.WrongKind, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }

    [Fact]
    public async Task Field_And_Event_Refs_Resolve_With_Own_Kinds()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            // Fields/events are intentionally NOT in the P13 index sweep;
            // facts are built directly from real symbols (same factory).
            var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
                compiled.Snapshot.Compilation,
                compiled.Snapshot.ProjectName,
                compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
                "ref-member-fe");
            NativeSemanticFact FactFor(ISymbol symbol)
            {
                var anchor = new SemanticSourceAnchor(
                    SemanticIdentityMint.MintLogicalIdentity(symbol),
                    symbol.Locations.First().SourceTree?.FilePath ?? "<unknown>",
                    DocumentationCommentId.CreateDeclarationId(symbol) ?? "<none>",
                    FspmSourceLocation.From(symbol.Locations.First()));
                return NativeSemanticFactFactory.Create(symbol, compilationIdentity, anchor);
            }

            var holder = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Members.MemberHolder");
            var field = holder.GetMembers("Name").OfType<IFieldSymbol>().First();
            var ev = holder.GetMembers("Changed").OfType<IEventSymbol>().First();
            var metadata = new FspmSemanticModelMetadata("ref-member-fe", "SemanticGolden", 3);
            var model = FspmModelBinder.Assemble(
                new[] { FactFor(holder), FactFor(field), FactFor(ev) }, metadata).Model;

            var name = model.Members.First(m => m.Name == "Name" && m.MemberKind == "Field");
            var changed = model.Members.First(m => m.Name == "Changed");

            var fieldResult = FspmReferenceResolver.ResolveMemberRef(
                new FspmFieldRef(name.Identity, "Name", name.DeclaringTypeId), model);
            var eventResult = FspmReferenceResolver.ResolveMemberRef(
                new FspmEventRef(changed.Identity, "Changed", changed.DeclaringTypeId), model);

            Assert.Equal(FspmReferenceStatus.Valid, fieldResult.Status);
            Assert.Equal("Field", fieldResult.TargetKind);
            Assert.Equal(FspmReferenceStatus.Valid, eventResult.Status);
            Assert.Equal("Event", eventResult.TargetKind);
        }
    }

    [Fact]
    public async Task MemberRef_Missing_Reports_Missing()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var model = BuildModel(compiled);
            var bogus = new FspmSemanticIdentity("Nope|P:Nope.Nope.X", "00");

            var result = FspmReferenceResolver.ResolveMemberRef(
                new FspmPropertyRef(bogus, "X"), model);

            Assert.Equal(FspmReferenceStatus.Missing, result.Status);
            Assert.False(result.IsResolved);
            Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        }
    }
}
