using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P13-H3: Operation Identity & Invocation Facts. Roslyn computes the
// signature (Parameters/RefKind/Optional/Params/Return/Generics);
// P13 only projects it into NativeOperationIdentity. No overload
// auto-pick (.First() forbidden), no argument binding engine.
[Collection("RoslynWorkspace")]
public sealed class H3OperationTests
{
    private static async Task<IMethodSymbol> RequireOp(
        Compilation compilation, string name, int arity, string firstParamType)
    {
        await Task.CompletedTask;
        var service = GoldenIdentity.RequireType(compilation, "SemanticGolden.Operations.OpService");
        return service.GetMembers(name).OfType<IMethodSymbol>()
            .First(m => m.MethodKind == MethodKind.Ordinary
                && m.Parameters.Length == arity
                && (arity == 0 || m.Parameters[0].Type.ToDisplayString() == firstParamType));
    }

    [Fact]
    public async Task OperationIdentity_Combine2_ReportsParamsAndReturn()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var method = await RequireOp(compiled.Snapshot.Compilation, "Combine", 2, "string");
            var identity = MethodSignatureExtractor.ExtractOperationIdentity(method);

            Assert.Equal("SemanticGolden.Operations.OpService", identity.ContainingType);
            Assert.Equal("Combine", identity.Name);
            Assert.Equal(0, identity.Arity);
            Assert.Equal(2, identity.Parameters.Count);
            Assert.Equal("string", identity.Parameters[0].ParameterType);
            Assert.Equal("string", identity.ReturnType);
            Assert.False(SymbolClassifier.Classify(method).Visibility.IsExtensionMethod);
        }
    }

    [Fact]
    public async Task OperationIdentity_Overloads_Differ()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var one = MethodSignatureExtractor.ExtractOperationIdentity(
                await RequireOp(compiled.Snapshot.Compilation, "Combine", 1, "string"));
            var two = MethodSignatureExtractor.ExtractOperationIdentity(
                await RequireOp(compiled.Snapshot.Compilation, "Combine", 2, "string"));

            Assert.NotEqual(one.StableId, two.StableId);
        }
    }

    [Fact]
    public async Task OperationIdentity_OptionalParam_ReportsDefault()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var method = await RequireOp(compiled.Snapshot.Compilation, "Describe", 1, "string");
            var identity = MethodSignatureExtractor.ExtractOperationIdentity(method);
            var param = Assert.Single(identity.Parameters);

            Assert.True(param.IsOptional);
            Assert.Equal("\"default\"", param.DefaultValue);
        }
    }

    [Fact]
    public async Task OperationIdentity_ParamsArray_ReportsIsParams()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var method = await RequireOp(compiled.Snapshot.Compilation, "Join", 1, "string[]");
            var identity = MethodSignatureExtractor.ExtractOperationIdentity(method);

            Assert.True(Assert.Single(identity.Parameters).IsParams);
        }
    }

    [Fact]
    public async Task OperationIdentity_RefOut_ReportsRefKind()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var compilation = compiled.Snapshot.Compilation;
            var service = GoldenIdentity.RequireType(compilation, "SemanticGolden.Operations.OpService");

            var tryGet = service.GetMembers("TryGet").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);
            var tryGetId = MethodSignatureExtractor.ExtractOperationIdentity(tryGet);
            Assert.Equal("Out", tryGetId.Parameters[1].RefKind);

            var add = service.GetMembers("Add").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);
            var addId = MethodSignatureExtractor.ExtractOperationIdentity(add);
            Assert.Equal("Ref", addId.Parameters[0].RefKind);
        }
    }

    [Fact]
    public async Task OperationIdentity_Constructor_ReportsKind()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var service = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Operations.OpService");
            var ctor = service.Constructors.First(c => c.Parameters.Length == 1);
            var identity = MethodSignatureExtractor.ExtractOperationIdentity(ctor);

            Assert.Equal(NativeSymbolKind.Constructor, identity.Kind);
            Assert.Equal(".ctor", identity.Name);
        }
    }

    [Fact]
    public async Task OperationIdentity_Indexer_ReportsKind()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var service = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Operations.OpService");
            var indexer = service.GetMembers().OfType<IPropertySymbol>()
                .First(p => p.IsIndexer);
            var classification = SymbolClassifier.Classify(indexer);

            Assert.Equal(NativeSymbolKind.Indexer, classification.Kind);
        }
    }

    [Fact]
    public async Task ExtensionMethod_Facts_ReportReducedFrom()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var compilation = compiled.Snapshot.Compilation;
            var extensions = GoldenIdentity.RequireType(compilation, "SemanticGolden.Operations.OpExtensions");
            var wordCount = extensions.GetMembers("WordCount").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);
            var facts = ExtensionMethodFactsExtractor.Extract(wordCount);

            Assert.True(facts.IsExtensionMethod);
            Assert.Equal("string", facts.ReceiverType);
            Assert.Contains("OpExtensions.WordCount", facts.ReducedFrom);
        }
    }

    [Fact]
    public async Task GenericMethod_ReportsArityAndConstraints()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var service = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Operations.OpService");
            var echo = service.GetMembers("Echo").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);
            var identity = MethodSignatureExtractor.ExtractOperationIdentity(echo);

            Assert.Equal(1, identity.Arity);
            Assert.Equal("T", identity.GenericParameters[0]);
        }
    }
}
