using Foundry.FSPM.Compiler.Binding;
using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Symbols;
using Foundry.FSPM.Compiler.Syntax;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

/// <summary>
/// G08-2 Property binding (directive 8.2): owner-first resolution from REAL
/// member symbols; unknown → diagnostic; cross-type names never misbind.
/// </summary>
[Collection("RoslynWorkspace")]
public sealed class PropertyBindingTests
{
    private static FspmPropertyDeclarationSyntax Decl(string entity, string property) =>
        new FspmPropertyDeclarationSyntax(entity, property, 0, entity.Length + property.Length + 1, 1, 1);

    [Fact]
    public async Task UserPhoneNumber_WithUniqueOwner_BindsToRealPropertySymbol()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = PropertyBinder.Bind(Decl("OtherUser", "PhoneNumber"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            var symbol = Assert.IsAssignableFrom<IPropertySymbol>(result.Symbol);
            Assert.Equal("PhoneNumber", symbol.Name);
            Assert.Equal("SemanticGolden.Domain.OtherUser", symbol.ContainingType.ToDisplayString());
            Assert.Equal(SpecialType.System_String, symbol.Type.SpecialType);
            Assert.Equal(
                "SemanticGolden|P:SemanticGolden.Domain.OtherUser.PhoneNumber",
                result.SymbolId!.Value.ToString());
        }
    }

    [Fact]
    public async Task UnknownProperty_OtherUserUnknown_YieldsFSPM102()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = PropertyBinder.Bind(Decl("OtherUser", "Unknown"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Unknown, result.Status);
            Assert.Null(result.Symbol);
            Assert.Null(result.SymbolId);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(FspmDiagnosticCodes.PropertyNotFound, diagnostic.Code);
            Assert.Equal(FspmDiagnosticSeverity.Error, diagnostic.Severity);
        }
    }

    [Fact]
    public async Task AmbiguousOwner_UserPhoneNumber_YieldsInvalid_WithOwnerDiagnostic()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            // Owner "User" is ambiguous → the property is Invalid (not Unknown:
            // the declaration itself is well-formed), owner's FSPM111 propagates.
            var result = PropertyBinder.Bind(Decl("User", "PhoneNumber"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Invalid, result.Status);
            Assert.Null(result.Symbol);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(FspmDiagnosticCodes.AmbiguousEntity, diagnostic.Code);
        }
    }

    [Fact]
    public async Task UnknownOwner_NoSuchEntity_YieldsInvalid_WithFSPM101()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = PropertyBinder.Bind(Decl("Nope", "Phone"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Invalid, result.Status);
            Assert.Equal(FspmDiagnosticCodes.EntityNotFound, Assert.Single(result.Diagnostics).Code);
        }
    }

    [Fact]
    public async Task SameMemberName_DifferentOwners_NeverMisbind()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var otherResult = PropertyBinder.Bind(Decl("OtherUser", "PhoneNumber"), loaded.Compilation);
            var sessionResult = PropertyBinder.Bind(
                Decl("SemanticGolden.Domain.User", "PhoneNumber"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Success, otherResult.Status);
            Assert.Equal(FspmBindingStatus.Success, sessionResult.Status);
            Assert.NotEqual(otherResult.SymbolId, sessionResult.SymbolId);

            var otherSymbol = Assert.IsAssignableFrom<IPropertySymbol>(otherResult.Symbol);
            var userSymbol = Assert.IsAssignableFrom<IPropertySymbol>(sessionResult.Symbol);
            Assert.Equal("SemanticGolden.Domain.OtherUser", otherSymbol.ContainingType.ToDisplayString());
            Assert.Equal("SemanticGolden.Domain.User", userSymbol.ContainingType.ToDisplayString());
        }
    }

    [Fact]
    public async Task InheritedProperty_DerivedUserName_BindsBaseSymbol_Deterministically()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            // 施工包 §57: inherited member must resolve, never NOT_FOUND.
            var result = PropertyBinder.Bind(Decl("DerivedUser", "Name"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Success, result.Status);
            var symbol = Assert.IsAssignableFrom<IPropertySymbol>(result.Symbol);
            Assert.Equal("SemanticGolden.Domain.BaseUser", symbol.ContainingType.ToDisplayString());
        }
    }

    [Fact]
    public async Task ShadowedProperty_TwoDistinctSymbols_YieldsAmbiguous()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            var result = PropertyBinder.Bind(Decl("ShadowedUser", "Name"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Ambiguous, result.Status);
            Assert.Equal(FspmDiagnosticCodes.AmbiguousProperty, Assert.Single(result.Diagnostics).Code);
        }
    }

    [Fact]
    public async Task PropertyOnMethodName_ToString_YieldsFSPM102_WithKindHint()
    {
        var loaded = await GoldenIdentity.LoadGoldenAsync();
        using (loaded.Workspace)
        {
            // ToString exists on every object but is a METHOD, not a property.
            var result = PropertyBinder.Bind(Decl("OtherUser", "ToString"), loaded.Compilation);

            Assert.Equal(FspmBindingStatus.Unknown, result.Status);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(FspmDiagnosticCodes.PropertyNotFound, diagnostic.Code);
            Assert.Contains("not a property", diagnostic.Message);
        }
    }
}
