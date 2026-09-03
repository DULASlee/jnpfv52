using Foundry.FSPM.Compiler.Lexer;
using Foundry.FSPM.Compiler.Parser;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// Phase 12 negative matrix (P12-09 N01-N08). Every malformed source must
// yield deterministic diagnostics — never a raw exception. Recovery must
// terminate: each test would hang the runner otherwise.
public sealed class ConstructionParserNegativeTests
{
    private static FspmConstructionParseResult Parse(string source)
    {
        var tokens = FspmLexer.Lex(source);
        return new FspmConstructionParser().Parse(tokens, source);
    }

    private static void AssertFailsWithCode(string source, string code)
    {
        var result = Parse(source);

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
        Assert.All(result.Diagnostics, d =>
            Assert.Equal(Foundry.FSPM.Compiler.Diagnostics.FspmDiagnosticSeverity.Error, d.Severity));
        Assert.Contains(result.Diagnostics, d => d.Code == code);
    }

    [Fact]
    public void N01_EmptyDocument_YieldsZeroPagesZeroDiagnostics()
    {
        // Grammar: document := page*. Empty input is valid (zero pages),
        // deterministic and documented here — not an error.
        var result = Parse(string.Empty);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.Document.Pages);
    }

    [Fact]
    public void N02_UnclosedPageBrace_ReportsMissingBrace()
    {
        AssertFailsWithCode(
            "page UserManagement {",
            Foundry.FSPM.Compiler.Diagnostics.FspmDiagnosticCodes.MissingBrace);
    }

    [Fact]
    public void N03_MisspelledKeyword_ReportsUnexpectedToken()
    {
        AssertFailsWithCode(
            "pgae UserManagement {\n}",
            Foundry.FSPM.Compiler.Diagnostics.FspmDiagnosticCodes.UnexpectedToken);
    }

    [Fact]
    public void N04_FieldWithoutExpression_ReportsMissingExpression()
    {
        AssertFailsWithCode(
            "page P {\n    form F {\n        field\n    }\n}",
            Foundry.FSPM.Compiler.Diagnostics.FspmDiagnosticCodes.MissingExpression);
    }

    [Fact]
    public void N05_SubmitWithoutArrowTarget_ReportsMissingExpression()
    {
        AssertFailsWithCode(
            "page P {\n    form F {\n        submit Create ->\n    }\n}",
            Foundry.FSPM.Compiler.Diagnostics.FspmDiagnosticCodes.MissingExpression);
    }

    [Fact]
    public void N06_EntityDirectlyUnderPage_ReportsInvalidNesting()
    {
        AssertFailsWithCode(
            "page P {\n    entity User\n}",
            Foundry.FSPM.Compiler.Diagnostics.FspmDiagnosticCodes.InvalidNesting);
    }

    [Fact]
    public void N07_FieldDirectlyUnderPage_ReportsInvalidNesting()
    {
        AssertFailsWithCode(
            "page P {\n    field User.Name\n}",
            Foundry.FSPM.Compiler.Diagnostics.FspmDiagnosticCodes.InvalidNesting);
    }

    [Fact]
    public void N08_SubmitWithoutName_ReportsMissingIdentifier()
    {
        AssertFailsWithCode(
            "page P {\n    form F {\n        submit -> User.Create\n    }\n}",
            Foundry.FSPM.Compiler.Diagnostics.FspmDiagnosticCodes.MissingIdentifier);
    }

    [Fact]
    public void NestedForm_ReportsInvalidNesting_AndTerminates()
    {
        AssertFailsWithCode(
            "page P {\n    form Outer {\n        form Inner {\n        }\n    }\n}",
            Foundry.FSPM.Compiler.Diagnostics.FspmDiagnosticCodes.InvalidNesting);
    }

    [Fact]
    public void Diagnostics_CarrySourceSpan()
    {
        var result = Parse("page P {\n    entity User\n}");

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.True(diagnostic.Start >= 0);
        Assert.True(diagnostic.Length > 0);
        Assert.True(diagnostic.Line >= 1);
        Assert.True(diagnostic.Column >= 1);
    }
}
