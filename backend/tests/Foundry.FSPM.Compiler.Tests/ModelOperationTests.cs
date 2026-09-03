using Foundry.FSPM.Compiler.Semantic;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// P14-01 STEP 6-H/I: Operation + Parameter records + binder Operation
// path. Overloads stay distinct; OwnerId wiring is asserted per
// parameter; ref/out/optional/params/nullable shapes come from Facts.
[Collection("RoslynWorkspace")]
public sealed class ModelOperationTests
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
            "model-operation");
        return NativeSemanticFactFactory.Create(symbol, compilationIdentity, anchor);
    }

    private static IMethodSymbol RequireOp(
        Compilation compilation, string typeName, string method, int arity, string firstParam)
    {
        var type = GoldenIdentity.RequireType(compilation, typeName);
        return type.GetMembers(method).OfType<IMethodSymbol>()
            .First(m => m.MethodKind == MethodKind.Ordinary
                && m.Parameters.Length == arity
                && (arity == 0 || m.Parameters[0].Type.ToDisplayString() == firstParam));
    }

    [Fact]
    public async Task Overloads_Bind_As_Distinct_Operations()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var compilation = compiled.Snapshot.Compilation;
            var user = GoldenIdentity.RequireType(compilation, "SemanticGolden.Domain.User");
            var stringOp = await Task.FromResult(RequireOp(compilation,
                "SemanticGolden.Domain.User", "Create", 1, "string"));
            var intOp = RequireOp(compilation, "SemanticGolden.Domain.User", "Create", 1, "int");

            var (operations, notes) = FspmModelBinder.BindOperations(new[]
            {
                FactFor(compiled, user),
                FactFor(compiled, stringOp),
                FactFor(compiled, intOp),
            });

            Assert.Empty(notes);
            Assert.Equal(2, operations.Count);
            Assert.NotEqual(operations[0].Identity.LogicalId, operations[1].Identity.LogicalId);
            Assert.Equal("string", operations[0].Parameters[0].Type);
            Assert.Equal("int", operations[1].Parameters[0].Type);
        }
    }

    [Fact]
    public async Task Parameters_Carry_OwnerId_RefKind_Optional_Default_Params()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var compilation = compiled.Snapshot.Compilation;
            var service = GoldenIdentity.RequireType(compilation, "SemanticGolden.Operations.OpService");
            var join = service.GetMembers("Join").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);
            var describe = service.GetMembers("Describe").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);
            var tryGet = service.GetMembers("TryGet").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);

            var (operations, _) = FspmModelBinder.BindOperations(new[]
            {
                FactFor(compiled, service),
                FactFor(compiled, join),
                FactFor(compiled, describe),
                FactFor(compiled, tryGet),
            });

            var joinOp = operations.First(o => o.Name == "Join");
            var parts = Assert.Single(joinOp.Parameters);
            Assert.Equal(0, parts.Position);
            Assert.True(parts.IsParams);
            Assert.Equal(joinOp.Identity.LogicalId, parts.OwnerId);

            var describeOp = operations.First(o => o.Name == "Describe");
            var nameParam = Assert.Single(describeOp.Parameters);
            Assert.True(nameParam.IsOptional);
            Assert.True(nameParam.HasDefaultValue);
            Assert.Equal("\"default\"", nameParam.DefaultValue);

            var tryGetOp = operations.First(o => o.Name == "TryGet");
            Assert.Equal("Out", tryGetOp.Parameters[1].RefKind);
        }
    }

    [Fact]
    public async Task Constructor_Binds_With_Ctor_Kind()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var service = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Operations.OpService");
            var ctor = service.Constructors.First(c => c.Parameters.Length == 1);

            var (operations, notes) = FspmModelBinder.BindOperations(new[]
            {
                FactFor(compiled, service),
                FactFor(compiled, ctor),
            });

            var operation = Assert.Single(operations);
            Assert.Empty(notes);
            Assert.Equal("Constructor", operation.OperationKind);
            Assert.Equal(".ctor", operation.Name);
        }
    }

    [Fact]
    public async Task GenericMethod_Reports_Arity()
    {
        var compiled = await GoldenSemanticCompilation.CompileGoldenAsync();
        using (compiled.Workspace)
        {
            var service = GoldenIdentity.RequireType(
                compiled.Snapshot.Compilation, "SemanticGolden.Operations.OpService");
            var echo = service.GetMembers("Echo").OfType<IMethodSymbol>()
                .First(m => m.MethodKind == MethodKind.Ordinary);

            var (operations, _) = FspmModelBinder.BindOperations(new[] { FactFor(compiled, echo) });

            Assert.Equal(1, Assert.Single(operations).GenericArity);
        }
    }
}
