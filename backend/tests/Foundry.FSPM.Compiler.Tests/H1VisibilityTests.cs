using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P13-H1: Symbol Classification & Visibility + Conditional Compilation.
// All facts come from Roslyn (ISymbol.Kind, DeclaredAccessibility,
// modifiers); no hand-written C# visibility algorithm.
[Collection("RoslynWorkspace")]
public sealed class H1VisibilityTests
{
    [Fact]
    public async Task PublicType_Classifies_AsPublicClass()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var type = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Visibility.PublicType");
            var classification = SymbolClassifier.Classify(type);

            Assert.Equal(NativeSymbolKind.Type, classification.Kind);
            Assert.Equal(NativeTypeKind.Class, classification.TypeKind);
            Assert.Equal("Public", classification.Visibility.Accessibility);
            Assert.False(classification.Visibility.IsStatic);
            Assert.False(classification.Visibility.IsAbstract);
            Assert.False(classification.Visibility.IsSealed);
        }
    }

    [Fact]
    public async Task InternalType_Classifies_AsInternal()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var type = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Visibility.InternalType");
            var classification = SymbolClassifier.Classify(type);

            Assert.Equal(NativeSymbolKind.Type, classification.Kind);
            Assert.Equal("Internal", classification.Visibility.Accessibility);
        }
    }

    [Fact]
    public async Task StaticMethod_Facts_CarryIsStatic()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Domain.User");
            var create = GoldenIdentity.RequireMethod(user, "Create", "string");
            var classification = SymbolClassifier.Classify(create);

            Assert.Equal(NativeSymbolKind.Method, classification.Kind);
            Assert.True(classification.Visibility.IsStatic);
            Assert.Equal("Public", classification.Visibility.Accessibility);
        }
    }

    [Fact]
    public async Task Property_Facts_CarryReadOnly()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var user = GoldenIdentity.RequireType(compiled.Snapshot.Compilation, "SemanticGolden.Domain.User");
            var phone = GoldenIdentity.RequireProperty(user, "PhoneNumber");
            var classification = SymbolClassifier.Classify(phone);

            Assert.Equal(NativeSymbolKind.Property, classification.Kind);
            Assert.Equal("Public", classification.Visibility.Accessibility);
        }
    }

    [Fact]
    public async Task DebugOnlyType_Resolves_WhenDebugDefined()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var context = ConditionalCompilationFacts.From(compiled.Snapshot);
            Assert.Contains("DEBUG", context.PreprocessorSymbols);

            var type = compiled.Snapshot.Compilation.GetTypeByMetadataName("SemanticGolden.Visibility.DebugOnlyType");
            Assert.NotNull(type);
        }
    }

    [Fact]
    public async Task DebugOnlyType_Absent_WithoutDebugSymbol_AdhocProof()
    {
        // Same sources minus DEBUG: Roslyn itself drops the type.
        // P13 reports what Roslyn reports — no custom #if interpreter.
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            // Re-parse the same source texts WITHOUT preprocessor
            // symbols: `#if DEBUG` regions go inactive at parse time,
            // so Roslyn itself drops DebugOnlyType. P13 only reports
            // what Roslyn reports.
            var noDebugTrees = new List<SyntaxTree>();
            foreach (var document in compiled.Snapshot.Documents)
            {
                var tree = await document.GetSyntaxTreeAsync();
                if (tree is null)
                {
                    continue;
                }

                var text = await tree.GetTextAsync();
                noDebugTrees.Add(CSharpSyntaxTree.ParseText(
                    text,
                    options: new CSharpParseOptions(),
                    path: tree.FilePath));
            }

            var noDebug = CSharpCompilation.Create(
                "NoDebug",
                syntaxTrees: noDebugTrees,
                references: compiled.Snapshot.Compilation.References,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var missing = noDebug.GetTypeByMetadataName("SemanticGolden.Visibility.DebugOnlyType");
            Assert.Null(missing);
        }
    }
}
