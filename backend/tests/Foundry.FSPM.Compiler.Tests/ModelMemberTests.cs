using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.SemanticModel;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-01 STEP 6-G: Member records + binder Member path. V1/V2 core
// regression lives here: User.Name string->int keeps LogicalId, flips
// Fingerprint (package §3 verbatim example).
[Collection("RoslynWorkspace")]
public sealed class ModelMemberTests
{
    private static Foundry.FSPM.Compiler.Compiler.FspmSemanticCompilationResult RequireCompiled() =>
        GoldenSemanticCompilation.CompileGoldenAsync().GetAwaiter().GetResult();

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
            "model-member");
        return NativeSemanticFactFactory.Create(symbol, compilationIdentity, anchor);
    }

    [Fact]
    public async Task Property_Maps_All_Required_Fields()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Domain.User");
            var phone = GoldenIdentity.RequireProperty(user, "PhoneNumber");

            // Owner linkage is batch-scoped: the owner Type fact must be
            // present so DeclaringTypeId resolves to a REAL bound identity.
            var (members, notes) = FspmModelBinder.BindMembers(new[]
            {
                FactFor(compiled, user),
                FactFor(compiled, phone),
            });

            var member = Assert.Single(members);
            Assert.Empty(notes);
            Assert.Equal("PhoneNumber", member.Name);
            Assert.Equal("Property", member.MemberKind);
            Assert.Equal("SemanticGolden|T:SemanticGolden.Domain.User", member.DeclaringTypeId);
            Assert.Equal("string", member.Type);
            Assert.Equal(FspmSemanticState.Resolved, member.State);
            Assert.EndsWith("User.cs", member.Anchor.Document);
        }
    }

    [Fact]
    public async Task Member_Without_Owner_In_Batch_Reports_Note()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Domain.User");
            var phone = GoldenIdentity.RequireProperty(user, "PhoneNumber");

            var (members, notes) = FspmModelBinder.BindMembers(new[] { FactFor(compiled, phone) });

            var member = Assert.Single(members);
            Assert.Equal(string.Empty, member.DeclaringTypeId);
            Assert.Single(notes);
        }
    }

    [Fact]
    public async Task Field_And_Event_Map_With_Own_Kinds()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var holder = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Members.MemberHolder");
            var field = holder.GetMembers("Name").OfType<IFieldSymbol>().First();
            var ev = holder.GetMembers("Changed").OfType<IEventSymbol>().First();

            var (members, notes) = FspmModelBinder.BindMembers(new[]
            {
                FactFor(compiled, holder),
                FactFor(compiled, field),
                FactFor(compiled, ev),
            });

            Assert.Empty(notes);
            Assert.Equal(2, members.Count);
            Assert.Equal("Field", members.First(m => m.Name == "Name").MemberKind);
            Assert.Equal("Event", members.First(m => m.Name == "Changed").MemberKind);
        }
    }

    [Fact]
    public void V1_V2_Logical_Same_Fingerprint_Differs_Name_String_To_Int()
    {
        // Package §3 verbatim regression: User.Name : string -> int.
        const string v1 = """
            namespace Evolve;
            public class Widget
            {
                public string Name { get; set; } = string.Empty;
            }
            """;
        const string v2 = """
            namespace Evolve;
            public class Widget
            {
                public int Name { get; set; }
            }
            """;

        static NativeSemanticFact FactForVersion(Microsoft.CodeAnalysis.CSharp.CSharpCompilation compilation)
        {
            var widget = compilation.GetTypeByMetadataName("Evolve.Widget")
                ?? throw new InvalidOperationException("Fixture broken.");
            var name = widget.GetMembers("Name").OfType<IPropertySymbol>().First();
            var anchor = new SemanticSourceAnchor(
                SemanticIdentityMint.MintLogicalIdentity(name),
                "<adhoc>",
                DocumentationCommentId.CreateDeclarationId(name) ?? "<none>",
                FspmSourceLocation.From(name.Locations.First()));
            var compilationIdentity = new CompilationIdentity(
                "Evolve", "Evolve", System.Array.Empty<string>(),
                "Release", "C# 12", new[] { "Widget.cs" }, "v");
            return NativeSemanticFactFactory.Create(name, compilationIdentity, anchor);
        }

        static Microsoft.CodeAnalysis.CSharp.CSharpCompilation Build(string source) =>
            Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
                "Evolve",
                syntaxTrees: new[] { Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source) },
                references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                options: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var fact1 = FactForVersion(Build(v1));
        var fact2 = FactForVersion(Build(v2));

        var member1 = Assert.Single(FspmModelBinder.BindMembers(new[] { fact1 }).Members);
        var member2 = Assert.Single(FspmModelBinder.BindMembers(new[] { fact2 }).Members);

        Assert.Equal(member1.Identity.LogicalId, member2.Identity.LogicalId);
        Assert.NotEqual(member1.Identity.Fingerprint, member2.Identity.Fingerprint);
    }
}
