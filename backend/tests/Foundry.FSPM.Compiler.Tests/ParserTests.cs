using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Lexer;
using Foundry.FSPM.Compiler.Parser;
using Foundry.FSPM.Compiler.Syntax;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

/// <summary>
/// Phase 4 Parser tests (施工包 §19 — positive + negative matrix).
/// Real source in -> real FspmParser.Parse -> FspmParseResult.
/// </summary>
public sealed class ParserTests
{
    private static FspmParseResult Parse(string source) =>
        new FspmParser().Parse(FspmLexer.Lex(source));

    // ===== §18 / Positive =====

    [Fact]
    public void Parse_EntityDeclaration_ProducesOneEntity()
    {
        var result = Parse("entity User");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        var entity = Assert.Single(result.CompilationUnit.Declarations);
        var e = Assert.IsType<FspmEntityDeclarationSyntax>(entity);
        Assert.Equal("User", e.Name);
        Assert.Equal(1, e.Line);
        Assert.Equal(1, e.Column);
    }

    [Fact]
    public void Parse_PropertyDeclaration_ProducesOneProperty()
    {
        var result = Parse("property User.PhoneNumber");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        var p = Assert.IsType<FspmPropertyDeclarationSyntax>(Assert.Single(result.CompilationUnit.Declarations));
        Assert.Equal("User", p.EntityName);
        Assert.Equal("PhoneNumber", p.PropertyName);
    }

    [Fact]
    public void Parse_OperationDeclaration_ProducesOneOperation()
    {
        var result = Parse("operation User.Login");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        var op = Assert.IsType<FspmOperationDeclarationSyntax>(Assert.Single(result.CompilationUnit.Declarations));
        Assert.Equal("User", op.EntityName);
        Assert.Equal("Login", op.OperationName);
    }

    [Fact]
    public void Parse_MultipleDeclarations_AggregatesInOrder()
    {
        const string src =
            "entity User\n" +
            "property User.UserName\n" +
            "property User.Password\n" +
            "operation User.Login";
        var result = Parse(src);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Collection(result.CompilationUnit.Declarations,
            d => Assert.IsType<FspmEntityDeclarationSyntax>(d),
            d => Assert.IsType<FspmPropertyDeclarationSyntax>(d),
            d => Assert.IsType<FspmPropertyDeclarationSyntax>(d),
            d => Assert.IsType<FspmOperationDeclarationSyntax>(d));
    }

    [Fact]
    public void Parse_BlankLinesBetweenDeclarations_AreSkipped()
    {
        const string src =
            "# header comment\n" +
            "\n" +
            "entity User\n" +
            "\n" +
            "\n" +
            "property User.UserName\n";
        var result = Parse(src);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.CompilationUnit.Declarations.Count);
    }

    [Fact]
    public void Parse_EmptySource_ProducesEmptyUnit()
    {
        var result = Parse(string.Empty);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.CompilationUnit.Declarations);
    }

    // ===== §19 / Negative =====

    [Fact]
    public void Parse_UnknownLeadingKeyword_EmitsFSPM001()
    {
        var result = Parse("foo User");

        Assert.False(result.Succeeded);
        var d = Assert.Single(result.Diagnostics);
        Assert.Equal(FspmDiagnosticCodes.UnexpectedToken, d.Code);
        Assert.Equal(FspmDiagnosticSeverity.Error, d.Severity);
        Assert.Contains("foo", d.Message);
    }

    [Fact]
    public void Parse_PropertyWithoutDot_EmitsFSPM003()
    {
        var result = Parse("property User");

        Assert.False(result.Succeeded);
        var d = Assert.Single(result.Diagnostics);
        Assert.Equal(FspmDiagnosticCodes.MissingDot, d.Code);
        Assert.Equal(FspmDiagnosticSeverity.Error, d.Severity);
    }

    [Fact]
    public void Parse_PropertyWithLeadingDot_EmitsFSPM002()
    {
        var result = Parse("property .UserName");

        Assert.False(result.Succeeded);
        var d = Assert.Single(result.Diagnostics);
        Assert.Equal(FspmDiagnosticCodes.MissingIdentifier, d.Code);
    }

    [Fact]
    public void Parse_EntityWithoutName_EmitsFSPM002()
    {
        var result = Parse("entity");

        Assert.False(result.Succeeded);
        Assert.Equal(FspmDiagnosticCodes.MissingIdentifier, Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void Parse_DuplicateEntity_EmitsFSPM004()
    {
        const string src = "entity User\nentity User";
        var result = Parse(src);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == FspmDiagnosticCodes.DuplicateDeclaration);
    }

    // ===== Position info =====

    [Fact]
    public void Parse_Diagnostic_ReportsCorrectLineAndColumn()
    {
        const string src = "entity User\nfoo User\nproperty User.Name";
        var result = Parse(src);

        var fooError = result.Diagnostics.Single(d => d.Code == FspmDiagnosticCodes.UnexpectedToken);
        Assert.Equal(2, fooError.Line);
        Assert.Equal(1, fooError.Column);
    }

    [Fact]
    public void Parse_Declaration_OnLine2_ReportsCorrectLine()
    {
        const string src = "\nentity User";
        var result = Parse(src);

        var entity = (FspmEntityDeclarationSyntax)result.CompilationUnit.Declarations[0];
        Assert.Equal(2, entity.Line);
    }

    [Fact]
    public void Parse_Parser_Recovers_AndContinuesAfterError()
    {
        // First declaration is bad (missing dot), second is good.
        // Parser should still parse the good one.
        const string src = "property User\nproperty User.Name";
        var result = Parse(src);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == FspmDiagnosticCodes.MissingDot);
        // Exactly one good declaration made it through.
        Assert.Single(result.CompilationUnit.Declarations);
    }

    [Fact]
    public void Parse_CompilationUnit_SpanCoversAllParsedDeclarations()
    {
        const string src = "entity User\nproperty User.Name";
        var result = Parse(src);

        var unit = result.CompilationUnit;
        // Start at the first declaration's start.
        Assert.Equal(0, unit.Start);
        // Length covers both declarations (ends at end of "property User.Name").
        Assert.True(unit.Length > 0);
    }
}
