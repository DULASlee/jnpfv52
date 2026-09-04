using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.Semantic.Reference;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-02-I/J: rebinding across versions + full-kind integration sweep.
// V1 (string Foo) → V2 (int Foo): same logical identity, changed
// fingerprint; a V1-pinned ref resolved against V2 reports Stale
// (never Missing), carrying both fingerprints in Reason.
public sealed class RefRebindIntegrationTests
{
    private const string V1Source = """
        namespace Evolve;
        public class Widget
        {
            public string Foo { get; set; } = string.Empty;
            public string Bar(string x) => x;
        }
        """;

    private const string V2Source = """
        namespace Evolve;
        public class Widget
        {
            public int Foo { get; set; }
            public string Bar(string x) => x;
        }
        """;

    private static Foundry.FSPM.SemanticModel.FspmSemanticModel BuildModel(string source, string snapshotId)
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("Evolve", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var document = project.AddDocument("Widget.cs", source);
        project = document.Project;
        var compilation = project.GetCompilationAsync().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Fixture broken: Adhoc compilation missing.");
        var snapshot = new FspmCompilationSnapshot(compilation, project);

        var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
            compilation, "Evolve", new[] { "Widget.cs" }, snapshotId);
        var resolver = new CSharpResolver(snapshot);
        var typeResult = resolver.ResolveType("Evolve.Widget");
        Assert.Equal(FspmResolutionStatus.Resolved, typeResult.Status);

        var collector = new System.Collections.Generic.List<NativeSemanticFact>();
        void Collect(ISymbol symbol)
        {
            var anchor = new SemanticSourceAnchor(
                SemanticIdentityMint.MintLogicalIdentity(symbol),
                "<adhoc>",
                DocumentationCommentId.CreateDeclarationId(symbol) ?? "<none>",
                FspmSourceLocation.From(symbol.Locations.FirstOrDefault() ?? Location.None));
            collector.Add(NativeSemanticFactFactory.Create(symbol, compilationIdentity, anchor));
        }

        var widget = (INamedTypeSymbol)typeResult.Selected!.Symbol;
        Collect(widget);
        foreach (var member in widget.GetMembers().OfType<IPropertySymbol>())
        {
            Collect(member);
        }

        foreach (var method in widget.GetMembers().OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary))
        {
            Collect(method);
        }

        var metadata = new FspmSemanticModelMetadata(snapshotId, "Evolve", collector.Count);
        return FspmModelBinder.Assemble(collector, metadata).Model;
    }

    [Fact]
    public void Rebind_V1Ref_Against_V2Model_Is_Stale_With_Both_Fingerprints()
    {
        var v1 = BuildModel(V1Source, "snap-v1");
        var v2 = BuildModel(V2Source, "snap-v2");

        var fooV1 = v1.Members.First(m => m.Name == "Foo");
        var fooV2 = v2.Members.First(m => m.Name == "Foo");

        // Same logical node across versions...
        Assert.Equal(fooV1.Identity.LogicalId, fooV2.Identity.LogicalId);
        // ...but the semantic version changed.
        Assert.NotEqual(fooV1.Fingerprint, fooV2.Fingerprint);

        // A V1-pinned reference resolved against V2 is Stale, not Missing.
        var pinned = new FspmPropertyRef(
            fooV1.Identity, "Widget.Foo", fooV1.DeclaringTypeId, fooV1.Fingerprint);
        var result = FspmReferenceResolver.ResolveMemberRef(pinned, v2);

        Assert.Equal(FspmReferenceStatus.Stale, result.Status);
        Assert.True(result.IsResolved);
        Assert.Contains(fooV1.Fingerprint, result.Reason);
        Assert.Contains(fooV2.Fingerprint, result.Reason);
        Assert.Equal(fooV2.Identity, result.TargetIdentity);
    }

    [Fact]
    public void Rebind_Deleted_Target_Is_Missing_Not_Stale()
    {
        var v1 = BuildModel(V1Source, "snap-v1");

        var gone = new FspmPropertyRef(
            new FspmSemanticIdentity("Evolve|P:Evolve.Widget.Gone", "00"), "Widget.Gone");
        var result = FspmReferenceResolver.ResolveMemberRef(gone, v1);

        Assert.Equal(FspmReferenceStatus.Missing, result.Status);
        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Integration_All_Nine_Kinds_Validate_Against_Real_Model()
    {
        var compiled = GoldenSemanticCompilation.CompileGoldenAsync().GetAwaiter().GetResult();
        using (compiled.Workspace)
        {
            var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
                compiled.Snapshot.Compilation,
                compiled.Snapshot.ProjectName,
                compiled.Snapshot.Documents.Select(d => d.FilePath ?? d.Name).ToArray(),
                "ref-integration");
            // P13 index sweeps types/properties/methods only; fields and
            // events are factored directly from real symbols (factory
            // path identical to index records).
            NativeSemanticFact FactFor(ISymbol symbol)
            {
                var anchor = new SemanticSourceAnchor(
                    SemanticIdentityMint.MintLogicalIdentity(symbol),
                    symbol.Locations.First().SourceTree?.FilePath ?? "<unknown>",
                    DocumentationCommentId.CreateDeclarationId(symbol) ?? "<none>",
                    FspmSourceLocation.From(symbol.Locations.First()));
                return NativeSemanticFactFactory.Create(symbol, compilationIdentity, anchor);
            }

            var facts = compiled.Index.Records.Select(record =>
            {
                var anchor = new SemanticSourceAnchor(
                    SemanticIdentityMint.MintLogicalIdentity(record.Symbol),
                    record.Symbol.Locations.FirstOrDefault()?.SourceTree?.FilePath ?? "<metadata>",
                    DocumentationCommentId.CreateDeclarationId(record.Symbol) ?? "<none>",
                    FspmSourceLocation.From(record.Symbol.Locations.FirstOrDefault() ?? Location.None));
                return NativeSemanticFactFactory.Create(record.Symbol, compilationIdentity, anchor);
            }).ToList();
            var holder = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Members.MemberHolder");
            facts.Add(FactFor(holder));
            facts.Add(FactFor(holder.GetMembers("Name").OfType<IFieldSymbol>().First()));
            facts.Add(FactFor(holder.GetMembers("Changed").OfType<IEventSymbol>().First()));
            var model = FspmModelBinder.Assemble(facts,
                new FspmSemanticModelMetadata("ref-integration", "SemanticGolden", facts.Count)).Model;

            var user = model.Types.First(t => t.Name == "User");
            var phone = model.Members.First(m => m.Name == "PhoneNumber");
            var create = model.Operations.First(o => o.Name == "Create");
            var admin = model.Types.First(t => t.Name == "AdminUser");

            var results = FspmReferenceResolver.ValidateAll(new Foundry.FSPM.SemanticModel.FspmSemanticReference[]
            {
                new FspmTypeRef(user.Identity, "User"),
                new FspmEntityRef(user.Identity, "User"),
                new FspmPropertyRef(phone.Identity, "User.PhoneNumber", phone.DeclaringTypeId),
                new FspmFieldRef(
                    model.Members.First(m => m.MemberKind == "Field").Identity,
                    "field", model.Members.First(m => m.MemberKind == "Field").DeclaringTypeId),
                new FspmEventRef(
                    model.Members.First(m => m.MemberKind == "Event").Identity,
                    "event", model.Members.First(m => m.MemberKind == "Event").DeclaringTypeId),
                new FspmOperationRef(create.Identity, "Create", create.DeclaringTypeId),
                new FspmParameterRef(create.Identity, "Create/phoneNumber", 0),
                new FspmRelationRef(admin.Identity, "AdminUser/base", "Inheritance", "BaseAccount"),
                new FspmTypeRef(new FspmSemanticIdentity("Nope|T:Nope", "00"), "Nope"),
            }, model);

            Assert.Equal(9, results.Count);
            Assert.True(results.Take(8).All(r => r.Status == FspmReferenceStatus.Valid));
            Assert.Equal(FspmReferenceStatus.Missing, results[8].Status);
            Assert.All(results, r => Assert.False(string.IsNullOrWhiteSpace(r.Reason)));
        }
    }
}
