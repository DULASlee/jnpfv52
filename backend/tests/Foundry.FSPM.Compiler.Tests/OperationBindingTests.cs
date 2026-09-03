using Foundry.FSPM.Compiler.Binding;
using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.Compiler.Syntax;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

/// <summary>
/// G08-3 + G08-6 Operation binding (directive 8.3): REAL candidate method
/// symbols + count rule; overloads are never guessed apart in FSPM v1.
/// </summary>
[Collection("RoslynWorkspace")]
public sealed class OperationBindingTests
{
    private static FspmOperationDeclarationSyntax Decl(string entity, string operation) =>
        new FspmOperationDeclarationSyntax(entity, operation, 0, entity.Length + operation.Length + 1, 1, 1);

    [Fact]
    public async Task SingleOverload_SessionPing_BindsToRealMethodSymbol()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = OperationBinder.Bind(Decl("Session", "Ping"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            var symbol = Assert.IsAssignableFrom<IMethodSymbol>(result.Symbol);
            Assert.Equal("Ping", symbol.Name);
            Assert.Equal(MethodKind.Ordinary, symbol.MethodKind);
            Assert.Equal("SemanticGolden.Domain.Session", symbol.ContainingType.ToDisplayString());
            Assert.NotNull(result.SymbolId);
        }
    }

    [Fact]
    public async Task SingleOverload_Login_BindsWithUniqueOwner()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = OperationBinder.Bind(
                Decl("SemanticGolden.Domain.User", "Login"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Success, result.Status);
            var symbol = Assert.IsAssignableFrom<IMethodSymbol>(result.Symbol);
            Assert.Empty(symbol.Parameters);
            Assert.Equal(
                FspmSymbolIdentity.Create(symbol),
                result.SymbolId!.Value);
        }
    }

    [Fact]
    public async Task MultiOverload_Create_YieldsAmbiguous_ListingBothSignatures()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = OperationBinder.Bind(
                Decl("SemanticGolden.Domain.User", "Create"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Ambiguous, result.Status);
            Assert.Null(result.Symbol);
            Assert.Null(result.SymbolId);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(FspmDiagnosticCodes.AmbiguousOperation, diagnostic.Code);
            Assert.Contains("string phoneNumber", diagnostic.Message);
            Assert.Contains("int legacyId", diagnostic.Message);
        }
    }

    [Fact]
    public async Task OverrideCollapsesToOneSlot_DerivedDescribe_BindsMostDerived()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = OperationBinder.Bind(Decl("DerivedUser", "Describe"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Success, result.Status);
            var symbol = Assert.IsAssignableFrom<IMethodSymbol>(result.Symbol);
            Assert.Equal("SemanticGolden.Domain.DerivedUser", symbol.ContainingType.ToDisplayString());
        }
    }

    [Fact]
    public async Task InheritedObjectProtocol_ToString_BindsRealObjectMethod()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            // No special-casing: the full chain is walked, so object.ToString
            // truthfully binds. Documented real behavior, not a scope accident.
            var result = OperationBinder.Bind(Decl("Session", "ToString"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Success, result.Status);
            var symbol = Assert.IsAssignableFrom<IMethodSymbol>(result.Symbol);
            Assert.Equal(SpecialType.System_Object, symbol.ContainingType.SpecialType);
        }
    }

    [Fact]
    public async Task UnknownOperation_YieldsFSPM103()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = OperationBinder.Bind(
                Decl("SemanticGolden.Domain.User", "Nope"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Unknown, result.Status);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(FspmDiagnosticCodes.OperationNotFound, diagnostic.Code);
            Assert.Equal(FspmDiagnosticSeverity.Error, diagnostic.Severity);
        }
    }

    [Fact]
    public async Task OperationOnProperty_PhoneNumber_YieldsFSPM104Invalid()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = OperationBinder.Bind(Decl("OtherUser", "PhoneNumber"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Invalid, result.Status);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(FspmDiagnosticCodes.InvalidOperationSignature, diagnostic.Code);
            Assert.Contains("not an operation", diagnostic.Message);
        }
    }

    [Fact]
    public async Task AmbiguousOwner_UserLogin_YieldsInvalid_WithOwnerDiagnostic()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = OperationBinder.Bind(Decl("User", "Login"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Invalid, result.Status);
            Assert.Equal(FspmDiagnosticCodes.AmbiguousEntity, Assert.Single(result.Diagnostics).Code);
        }
    }

    [Fact]
    public async Task UnknownOwner_NoSuchEntity_YieldsInvalid_WithFSPM101()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = OperationBinder.Bind(Decl("Nope", "Run"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Invalid, result.Status);
            Assert.Equal(FspmDiagnosticCodes.EntityNotFound, Assert.Single(result.Diagnostics).Code);
        }
    }
}
