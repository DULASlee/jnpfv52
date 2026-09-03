using Foundry.FSPM.Compiler.Compiler;
using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Syntax;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

/// <summary>
/// End-to-end FspmCompiler tests (Phase 4 — 施工包 §45-§48).
///
/// Exercises the full pipeline:
///   string source -> FspmCompiler.Compile -> FspmCompilationResult
///
/// CompilerTests verifies that the same API can be driven end-to-end
/// without any test-only fixtures or hand-injected AST.
/// </summary>
public sealed class CompilerTests
{
    // ===== Happy path =====

    [Fact]
    public void Compile_ValidSource_Succeeds_AndProducesAST()
    {
        const string src =
            "entity User\n" +
            "property User.UserName\n" +
            "property User.Password\n" +
            "operation User.Login";
        var result = FspmCompiler.Compile(src);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(4, result.Syntax.Declarations.Count);
    }

    [Fact]
    public void Compile_MinimalValidFSPM_Succeeds()
    {
        var result = FspmCompiler.Compile("entity User");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.IsType<FspmEntityDeclarationSyntax>(result.Syntax.Declarations[0]);
    }

    [Fact]
    public void Compile_EmptySource_SucceedsWithEmptyAST()
    {
        var result = FspmCompiler.Compile(string.Empty);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.Syntax.Declarations);
    }

    // ===== Error path =====

    [Fact]
    public void Compile_InvalidSyntax_FailsAndEmitsDiagnostic()
    {
        var result = FspmCompiler.Compile("foo User");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == FspmDiagnosticCodes.UnexpectedToken);
    }

    [Fact]
    public void Compile_LexerError_IsConvertedToDiagnostic()
    {
        // "$" is an illegal character in Phase 2 grammar.
        var result = FspmCompiler.Compile("entity $User");

        Assert.False(result.Succeeded);
        var d = Assert.Single(result.Diagnostics);
        Assert.Equal(FspmDiagnosticSeverity.Error, d.Severity);
        Assert.Contains('$', d.Message);
    }

    [Fact]
    public void Compile_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FspmCompiler.Compile(null!));
    }

    // ===== Real-world-ish scenarios =====

    [Fact]
    public void Compile_MultipleEntities_ParsesInOrder()
    {
        const string src =
            "entity User\n" +
            "entity Order\n" +
            "property User.Name\n" +
            "property Order.Total";
        var result = FspmCompiler.Compile(src);

        Assert.True(result.Succeeded);
        Assert.Equal(4, result.Syntax.Declarations.Count);
    }

    [Fact]
    public void Compile_DuplicateEntity_FailsWithFSPM004()
    {
        const string src = "entity User\nentity User";
        var result = FspmCompiler.Compile(src);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == FspmDiagnosticCodes.DuplicateDeclaration);
    }

    [Fact]
    public void Compile_ResultSyntax_HasMeaningfulStartPosition()
    {
        const string src = "entity User";
        var result = FspmCompiler.Compile(src);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.Syntax.Start);
        Assert.True(result.Syntax.Length > 0);
    }
    [Fact]
    public void Compile_Comments_AreIgnored()
    {
        const string src =
            "# this is a comment\n" +
            "entity User  # trailing comment\n" +
            "# another comment\n" +
            "property User.Name";
        var result = FspmCompiler.Compile(src);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.Syntax.Declarations.Count);
    }
}
