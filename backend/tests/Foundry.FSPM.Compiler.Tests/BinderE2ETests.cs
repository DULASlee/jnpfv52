using Foundry.FSPM.Compiler.Binding;
using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Lexer;
using Foundry.FSPM.Compiler.Parser;
using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.Compiler.Syntax;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

/// <summary>
/// G08-8 Real Roslyn E2E (directive 8.6): REAL FSPM source text → Lexer →
/// Parser → Binders → REAL symbols + FspmSymbolIds. Zero mocks.
///
/// <para>Honesty note (reported to the chief architect): the literal 8.6 source
/// (<c>entity User</c> …) CANNOT yield Success for the entity — the Phase 7
/// collision fixtures (NamespaceA/B + Contracts) make bare <c>User</c>
/// genuinely ambiguous, and 8.1 mandates verifying exactly that. The first
/// test pins the TRUTHFUL outcomes (Ambiguous/Invalid, never guessed); the
/// second test pins the full success chain (declaration → symbol → ID →
/// re-resolve) on unambiguous declarations through the same pipeline.</para>
/// </summary>
[Collection("RoslynWorkspace")]
public sealed class BinderE2ETests
{
    private static FspmCompilationUnitSyntax ParseOrThrow(string source)
    {
        var tokens = FspmLexer.Lex(source);
        var parse = new FspmParser().Parse(tokens);
        Assert.True(parse.Succeeded);
        Assert.Empty(parse.Diagnostics);
        return parse.CompilationUnit;
    }

    [Fact]
    public async Task Literal86Source_ParsesClean_BindsTruthfully_NeverGuesses()
    {
        const string Source = """
            entity User
            property User.PhoneNumber
            operation User.Create
            """;

        var unit = ParseOrThrow(Source);
        Assert.Equal(3, unit.Declarations.Count);

        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var entity = EntityBinder.Bind(
                Assert.IsType<FspmEntityDeclarationSyntax>(unit.Declarations[0]), loaded.Compilation);
            Assert.Equal(FspmBindingStatus.Ambiguous, entity.Status);
            Assert.Equal(FspmDiagnosticCodes.AmbiguousEntity, Assert.Single(entity.Diagnostics).Code);
            Assert.Null(entity.Symbol);

            var property = PropertyBinder.Bind(
                Assert.IsType<FspmPropertyDeclarationSyntax>(unit.Declarations[1]), loaded.Compilation);
            Assert.Equal(FspmBindingStatus.Invalid, property.Status);
            Assert.Equal(FspmDiagnosticCodes.AmbiguousEntity, Assert.Single(property.Diagnostics).Code);
            Assert.Null(property.Symbol);

            var operation = OperationBinder.Bind(
                Assert.IsType<FspmOperationDeclarationSyntax>(unit.Declarations[2]), loaded.Compilation);
            Assert.Equal(FspmBindingStatus.Invalid, operation.Status);
            Assert.Equal(FspmDiagnosticCodes.AmbiguousEntity, Assert.Single(operation.Diagnostics).Code);
            Assert.Null(operation.Symbol);
        }
    }

    [Fact]
    public async Task SuccessChain_Declarations_BindToRealSymbols_AndReResolve()
    {
        const string Source = """
            entity OtherUser
            property OtherUser.PhoneNumber
            entity Session
            operation Session.Ping
            """;

        var unit = ParseOrThrow(Source);
        Assert.Equal(4, unit.Declarations.Count);

        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var compilation = loaded.Compilation;

            var entity = EntityBinder.Bind(
                Assert.IsType<FspmEntityDeclarationSyntax>(unit.Declarations[0]), compilation);
            Assert.Equal(FspmBindingStatus.Success, entity.Status);
            var entitySymbol = Assert.IsAssignableFrom<INamedTypeSymbol>(entity.Symbol);
            Assert.NotNull(entity.SymbolId);

            var property = PropertyBinder.Bind(
                Assert.IsType<FspmPropertyDeclarationSyntax>(unit.Declarations[1]), compilation);
            Assert.Equal(FspmBindingStatus.Success, property.Status);
            var propertySymbol = Assert.IsAssignableFrom<IPropertySymbol>(property.Symbol);
            Assert.NotNull(property.SymbolId);

            var sessionEntity = EntityBinder.Bind(
                Assert.IsType<FspmEntityDeclarationSyntax>(unit.Declarations[2]), compilation);
            Assert.Equal(FspmBindingStatus.Success, sessionEntity.Status);

            var operation = OperationBinder.Bind(
                Assert.IsType<FspmOperationDeclarationSyntax>(unit.Declarations[3]), compilation);
            Assert.Equal(FspmBindingStatus.Success, operation.Status);
            var operationSymbol = Assert.IsAssignableFrom<IMethodSymbol>(operation.Symbol);
            Assert.NotNull(operation.SymbolId);

            // declaration → symbol → ID → SAME semantic symbol (directive §七, E2E scale).
            Assert.Equal(entity.SymbolId!.Value, FspmSymbolIdentity.Create(entitySymbol));
            Assert.Equal(property.SymbolId!.Value, FspmSymbolIdentity.Create(propertySymbol));
            Assert.Equal(operation.SymbolId!.Value, FspmSymbolIdentity.Create(operationSymbol));

            var reResolved = FspmSymbolIdentity.Resolve(entity.SymbolId!.Value, compilation);
            Assert.Equal("SemanticGolden.Domain.OtherUser", reResolved.ToDisplayString());
        }
    }
}
