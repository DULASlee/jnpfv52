using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P13-H6: Stable Semantic Anchor + Current Location. Target:
// V1 User.PhoneNumber → insert code above → V2 User.PhoneNumber gives
// LogicalIdentity SAME + CurrentSpan CHANGED (Adhoc, no MSBuild).
[Collection("RoslynWorkspace")]
public sealed class H6SourceAnchorTests
{
    private const string V1Source = """
        namespace Evolve;
        public class Widget
        {
            public string Label { get; set; } = string.Empty;
        }
        """;

    private const string V2ShiftedSource = """
        namespace Evolve;
        // inserted comment line shifts every declaration down
        // second inserted line
        public class Widget
        {
            public string Label { get; set; } = string.Empty;
        }
        """;

    private static CSharpCompilation BuildAdhoc(string source) =>
        CSharpCompilation.Create(
            "Evolve",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static IPropertySymbol RequireLabel(CSharpCompilation compilation)
    {
        var widget = compilation.GetTypeByMetadataName("Evolve.Widget")
            ?? throw new InvalidOperationException("Fixture broken: Evolve.Widget not found.");
        return widget.GetMembers("Label").OfType<IPropertySymbol>().First();
    }

    private static SemanticSourceAnchor BuildAnchor(IPropertySymbol symbol, string snapshotId)
    {
        var logical = SemanticIdentityMint.MintLogicalIdentity(symbol);
        var location = FspmSourceLocation.From(symbol.Locations.First());
        var declarationId = DocumentationCommentId.CreateDeclarationId(symbol) ?? "<none>";
        return new SemanticSourceAnchor(
            logical,
            symbol.Locations.First().SourceTree?.FilePath ?? "<adhoc>",
            declarationId,
            location);
    }

    [Fact]
    public void Anchor_Logical_Same_Across_Edit()
    {
        var anchor1 = BuildAnchor(RequireLabel(BuildAdhoc(V1Source)), "snap-1");
        var anchor2 = BuildAnchor(RequireLabel(BuildAdhoc(V2ShiftedSource)), "snap-2");

        Assert.Equal(anchor1.Logical, anchor2.Logical);
        Assert.Equal(anchor1.DeclarationAnchor, anchor2.DeclarationAnchor);
    }

    [Fact]
    public void Anchor_CurrentSpan_Changed_After_Insert()
    {
        var anchor1 = BuildAnchor(RequireLabel(BuildAdhoc(V1Source)), "snap-1");
        var anchor2 = BuildAnchor(RequireLabel(BuildAdhoc(V2ShiftedSource)), "snap-2");

        Assert.NotEqual(anchor1.CurrentSpan.StartLine, anchor2.CurrentSpan.StartLine);
        Assert.True(anchor2.CurrentSpan.StartLine > anchor1.CurrentSpan.StartLine);
    }

    [Fact]
    public void Evidence_Pairs_Anchor_With_Snapshot()
    {
        var anchor = BuildAnchor(RequireLabel(BuildAdhoc(V1Source)), "snap-1");
        var evidence = new SemanticEvidence(anchor, "snap-1", "Evolve");

        Assert.Equal("snap-1", evidence.SnapshotId);
        Assert.Equal(anchor, evidence.Anchor);
    }

    [Fact]
    public async Task Anchor_RealMsBuild_UserPhoneNumber_HasDocumentAndSpan()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Domain.User");
            var phone = GoldenIdentity.RequireProperty(user, "PhoneNumber");
            var anchor = BuildAnchor(phone, "msbuild-proof");

            Assert.EndsWith("User.cs", anchor.DocumentIdentity);
            Assert.Contains("P:SemanticGolden.Domain.User.PhoneNumber", anchor.DeclarationAnchor);
            Assert.True(anchor.CurrentSpan.StartLine >= 1);
        }
    }
}
