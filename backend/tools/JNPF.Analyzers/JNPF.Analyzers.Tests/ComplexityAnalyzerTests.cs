using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace JNPF.Analyzers.Tests;

public class ComplexityAnalyzerTests
{
    [Fact]
    public void JNPF009_Unbaselined_Method_CC35_ReportsDiagnostic()
    {
        var code = BuildMethodWithIfs("HotNewMethod", ifCount: 34);
        var diagnostics = GetComplexityDiagnostics(code, baselineJson: null);
        Assert.Contains(diagnostics, d => d.Id == ComplexityAnalyzer.DiagnosticId);
    }

    [Fact]
    public void JNPF009_LowComplexity_NoDiagnostic()
    {
        var code = @"
class TestClass {
    void Simple() {
        if (true) { }
    }
}";
        var diagnostics = GetComplexityDiagnostics(code, baselineJson: null);
        Assert.DoesNotContain(diagnostics, d => d.Id == ComplexityAnalyzer.DiagnosticId);
    }

    [Fact]
    public void JNPF009_Baselined_Method_Exempt()
    {
        var code = BuildMethodWithIfs("ImportDataAssemble", ifCount: 40);
        // File path in AdditionalText matching is via syntax tree path — use source path hint
        var baseline = @"{
  ""version"": 1,
  ""threshold"": 30,
  ""entries"": [
    {
      ""symbol"": ""Test.cs::ImportDataAssemble"",
      ""name"": ""ImportDataAssemble"",
      ""maxComplexity"": 138,
      ""file"": ""Test.cs""
    }
  ]
}";
        var diagnostics = GetComplexityDiagnostics(code, baseline, sourcePath: "modularity/visualdev/Test.cs");
        // Match by file suffix Test.cs + name
        Assert.DoesNotContain(diagnostics, d => d.Id == ComplexityAnalyzer.DiagnosticId);
    }

    [Fact]
    public void CyclomaticComplexity_CountsIfs()
    {
        var tree = CSharpSyntaxTree.ParseText(BuildMethodWithIfs("M", 5));
        var root = tree.GetCompilationUnitRoot();
        var method = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First();
        var cc = CyclomaticComplexityWalker.Compute(method);
        Assert.Equal(6, cc); // 1 + 5 ifs
    }

    [Fact]
    public void ComplexityBaseline_ParsesInventoryShape()
    {
        var json = @"{
  ""version"": 1,
  ""threshold"": 30,
  ""entries"": [
    {
      ""symbol"": ""modularity/visualdev/JNPF.VisualDev/VisualDevService.cs::FuncToMenu"",
      ""name"": ""FuncToMenu"",
      ""maxComplexity"": 84,
      ""file"": ""modularity/visualdev/JNPF.VisualDev/VisualDevService.cs""
    }
  ]
}";
        var baseline = ComplexityBaseline.Parse(json);
        Assert.Equal(30, baseline.Threshold);
        Assert.True(baseline.TryGetMaxComplexity(
            @"D:\repo\backend\modularity\visualdev\JNPF.VisualDev\VisualDevService.cs",
            "FuncToMenu",
            out var max));
        Assert.Equal(84, max);
    }

    private static string BuildMethodWithIfs(string name, int ifCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("class TestClass {");
        sb.Append("    void ").Append(name).AppendLine("() {");
        for (var i = 0; i < ifCount; i++)
            sb.Append("        if (x").Append(i).AppendLine(") { }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static ImmutableArray<Diagnostic> GetComplexityDiagnostics(
        string code,
        string? baselineJson,
        string sourcePath = "Test.cs")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code, path: sourcePath);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var additional = ImmutableArray<AdditionalText>.Empty;
        if (baselineJson != null)
        {
            additional = ImmutableArray.Create<AdditionalText>(
                new InMemoryAdditionalText("complexity-baseline.json", baselineJson));
        }

        var options = new AnalyzerOptions(additional);
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new ComplexityAnalyzer()), options);

        return compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _text = SourceText.From(content, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}
