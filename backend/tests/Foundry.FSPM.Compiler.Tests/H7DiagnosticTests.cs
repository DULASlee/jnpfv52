using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P13-H7: Diagnostic / Failure / Degraded Semantics. Eight states exist,
// are classified (semantic vs execution), and Degraded is proven on a
// real error-containing compilation that still resolves.
public sealed class H7DiagnosticTests
{
    [Fact]
    public void All_Eight_Statuses_Are_Classified()
    {
        Assert.Equal(ResolutionOutcomeClass.Semantic, FspmResolutionStatus.Resolved.Classify());
        Assert.Equal(ResolutionOutcomeClass.Semantic, FspmResolutionStatus.NotFound.Classify());
        Assert.Equal(ResolutionOutcomeClass.Semantic, FspmResolutionStatus.Ambiguous.Classify());
        Assert.Equal(ResolutionOutcomeClass.Semantic, FspmResolutionStatus.Invalid.Classify());
        Assert.Equal(ResolutionOutcomeClass.Semantic, FspmResolutionStatus.Unsupported.Classify());
        Assert.Equal(ResolutionOutcomeClass.Semantic, FspmResolutionStatus.Degraded.Classify());
        Assert.Equal(ResolutionOutcomeClass.Execution, FspmResolutionStatus.Cancelled.Classify());
        Assert.Equal(ResolutionOutcomeClass.Execution, FspmResolutionStatus.InfrastructureFailure.Classify());
    }

    [Fact]
    public void InfrastructureFailure_IsExecution_NotSemanticVerdict()
    {
        var result = new FspmResolutionResult(
            FspmResolutionStatus.InfrastructureFailure,
            Array.Empty<FspmSymbolRecord>(),
            "MSBuildWorkspace failed to load.",
            null);

        Assert.Equal(ResolutionOutcomeClass.Execution, result.Status.Classify());
        Assert.Null(result.Selected);
    }

    [Fact]
    public void Degraded_Compilation_Still_Resolves_Real_Symbol()
    {
        const string broken = """
            namespace Evolve;
            public class Widget
            {
                public string Label { get; set; } = string.Empty;
                public int Broken( { }
            }
            """;

        var compilation = CSharpCompilation.Create(
            "Degraded",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(broken) },
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Assert.True(compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Equal(SemanticQuality.Degraded, SemanticQualityAssessor.AssessQuality(compilation));

        // Roslyn recovered: the valid Widget.Label is still resolvable.
        var widget = compilation.GetTypeByMetadataName("Evolve.Widget");
        Assert.NotNull(widget);
        Assert.NotEmpty(widget!.GetMembers("Label").OfType<IPropertySymbol>());
    }

    [Fact]
    public void Degraded_EndToEnd_ResolvedFact_Carries_DegradedQuality()
    {
        const string broken = """
            namespace Evolve;
            public class Widget
            {
                public string Label { get; set; } = string.Empty;
                public int Broken( { }
            }
            """;

        var compilation = CSharpCompilation.Create(
            "Degraded",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(broken) },
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var quality = SemanticQualityAssessor.AssessQuality(compilation);
        var widget = compilation.GetTypeByMetadataName("Evolve.Widget")
            ?? throw new InvalidOperationException("Fixture broken.");
        var label = widget.GetMembers("Label").OfType<IPropertySymbol>().First();

        var anchor = new SemanticSourceAnchor(
            SemanticIdentityMint.MintLogicalIdentity(label),
            "<adhoc>",
            DocumentationCommentId.CreateDeclarationId(label) ?? "<none>",
            FspmSourceLocation.From(label.Locations.First()));
        var compilationIdentity = SemanticIdentityMint.MintCompilationIdentity(
            compilation, "Degraded", new[] { "Widget.cs" }, "degraded-1");
        var fact = NativeSemanticFactFactory.Create(label, compilationIdentity, anchor, quality: quality);

        Assert.Equal(FspmResolutionStatus.Resolved, fact.Status);
        Assert.Equal(SemanticQuality.Degraded, fact.Quality);
    }

    [Fact]
    public void Clean_Compilation_Assesses_Perfect()
    {
        const string clean = """
            namespace Evolve;
            public class Widget
            {
                public string Label { get; set; } = string.Empty;
            }
            """;

        var compilation = CSharpCompilation.Create(
            "Clean",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(clean) },
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Assert.Equal(SemanticQuality.Perfect, SemanticQualityAssessor.AssessQuality(compilation));
    }

    [Fact]
    public void NativeDiagnostic_Carries_Source_And_Stable_References()
    {
        var diagnostic = new NativeDiagnostic(
            DiagnosticId: "CS1002",
            Code: "CS1002",
            Severity: "Error",
            Message: "; expected",
            Source: NativeDiagnosticSource.Roslyn,
            Location: null,
            CandidateIdentities: new[] { "Asm|T:N.C" },
            RelatedSymbolIdentity: "Asm|P:N.C.P");

        Assert.Equal(NativeDiagnosticSource.Roslyn, diagnostic.Source);
        Assert.Single(diagnostic.CandidateIdentities);
    }
}
