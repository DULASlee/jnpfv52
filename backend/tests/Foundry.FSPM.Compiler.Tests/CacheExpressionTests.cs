using Foundry.FSPM.Compiler.Semantic;
using Foundry.FSPM.Compiler.WorkspaceNS;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// G13-CACHE-EXPR-01: the ConditionalWeakTable<Compilation, …> emit cache
// must not leak bindings across compilations. V1 (string Foo) and V2
// (int Foo) resolve independently and repeatably. Fully-qualified
// expressions keep the test hermetic (no using-directive dependence).
public sealed class CacheExpressionTests
{
    private const string V1Source = """
        namespace Evolve;
        public class Widget
        {
            public string Foo { get; set; } = string.Empty;
        }
        """;

    private const string V2Source = """
        namespace Evolve;
        public class Widget
        {
            public int Foo { get; set; }
        }
        """;

    private static CSharpResolver BuildResolver(string source)
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
        return new CSharpResolver(snapshot);
    }

    private static string ResolvedPropertyType(FspmResolutionResult result)
    {
        Assert.Equal(FspmResolutionStatus.Resolved, result.Status);
        Assert.True(result.IsResolved);
        return ((IPropertySymbol)result.Selected!.Symbol).Type.SpecialType.ToString();
    }

    [Fact]
    public void V1_And_V2_Resolve_Independently_And_Repeatably()
    {
        var v1 = BuildResolver(V1Source);
        var v2 = BuildResolver(V2Source);

        var r1 = v1.ResolveExpression("Evolve.Widget.Foo");
        Assert.Equal("System_String", ResolvedPropertyType(r1));

        var r2 = v2.ResolveExpression("Evolve.Widget.Foo");
        Assert.Equal("System_Int32", ResolvedPropertyType(r2));

        // Repeat: neither direction pollutes the other.
        var r1b = v1.ResolveExpression("Evolve.Widget.Foo");
        Assert.Equal("System_String", ResolvedPropertyType(r1b));

        var r2b = v2.ResolveExpression("Evolve.Widget.Foo");
        Assert.Equal("System_Int32", ResolvedPropertyType(r2b));

        // Identity is DocId+assembly: V1 and V2 share the logical node
        // (same declaration), so identities are EQUAL while resolved
        // TYPES differ. The fingerprint (not asserted here) is what
        // detects the string->int change. Repeatability holds per version.
        Assert.Equal(r1.Selected!.Identity, r2.Selected!.Identity);
        Assert.Equal(r1.Selected!.Identity, r1b.Selected!.Identity);
        Assert.Equal(r2.Selected!.Identity, r2b.Selected!.Identity);
    }
}
