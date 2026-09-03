using Foundry.FSPM.Compiler.Lexer;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// Phase 12 Lexer extension (P12-01/P12-02): page/form/field/submit keywords,
// braces/parens/arrow punctuation, string literals. Legacy entity/property/
// operation tokenization must keep working (compatibility gate).
public sealed class ConstructionLexerTests
{
    private static IReadOnlyList<FspmToken> Lex(string source) =>
        FspmLexer.Lex(source);

    [Fact]
    public void Tokenizes_PageKeyword()
    {
        var tokens = Lex("page");

        Assert.Collection(
            tokens,
            t => { Assert.Equal(FspmTokenKind.PageKeyword, t.Kind); Assert.Equal("page", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Tokenizes_FormKeyword()
    {
        var tokens = Lex("form");

        Assert.Collection(
            tokens,
            t => { Assert.Equal(FspmTokenKind.FormKeyword, t.Kind); Assert.Equal("form", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Tokenizes_FieldKeyword()
    {
        var tokens = Lex("field");

        Assert.Collection(
            tokens,
            t => { Assert.Equal(FspmTokenKind.FieldKeyword, t.Kind); Assert.Equal("field", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Tokenizes_SubmitKeyword()
    {
        var tokens = Lex("submit");

        Assert.Collection(
            tokens,
            t => { Assert.Equal(FspmTokenKind.SubmitKeyword, t.Kind); Assert.Equal("submit", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Tokenizes_Braces()
    {
        var tokens = Lex("page X { }");

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.PageKeyword, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => { Assert.Equal(FspmTokenKind.LBrace, t.Kind); Assert.Equal("{", t.Text); },
            t => { Assert.Equal(FspmTokenKind.RBrace, t.Kind); Assert.Equal("}", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Tokenizes_Parens()
    {
        var tokens = Lex("User.Create(user)");

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => { Assert.Equal(FspmTokenKind.LParen, t.Kind); Assert.Equal("(", t.Text); },
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => { Assert.Equal(FspmTokenKind.RParen, t.Kind); Assert.Equal(")", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Tokenizes_Arrow()
    {
        var tokens = Lex("submit Create -> User.Create");

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.SubmitKeyword, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => { Assert.Equal(FspmTokenKind.Arrow, t.Kind); Assert.Equal("->", t.Text); },
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Tokenizes_StringLiteral()
    {
        var tokens = Lex("\"UserManagement\"");

        Assert.Collection(
            tokens,
            t => { Assert.Equal(FspmTokenKind.StringLiteral, t.Kind); Assert.Equal("\"UserManagement\"", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Tokenizes_BracketsQuestionNumeric_ForNativeBoundary()
    {
        var tokens = Lex("obj?.Prop obj[0].Prop 42");

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.Question, t.Kind),
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.LBracket, t.Kind),
            t => { Assert.Equal(FspmTokenKind.NumericLiteral, t.Kind); Assert.Equal("0", t.Text); },
            t => Assert.Equal(FspmTokenKind.RBracket, t.Kind),
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => { Assert.Equal(FspmTokenKind.NumericLiteral, t.Kind); Assert.Equal("42", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Lexes_NumericLiteral_Inside_NativeExpression()
    {
        // Phase 12 hard evidence: the canonical P12 native expression
        // shape 'obj[0].Property' must tokenize COMPLETELY. P12 owns
        // the Lexer + Parser boundary; Roslyn (P13) owns the meaning
        // of the '[0]' indexer. No lexer mode flag, no special
        // context awareness, no Split('.').
        var tokens = Lex("obj[0].Property");

        Assert.Collection(
            tokens,
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("obj", t.Text); },
            t => Assert.Equal(FspmTokenKind.LBracket, t.Kind),
            t => { Assert.Equal(FspmTokenKind.NumericLiteral, t.Kind); Assert.Equal("0", t.Text); },
            t => Assert.Equal(FspmTokenKind.RBracket, t.Kind),
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("Property", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void DashWithoutGreaterThan_ThrowsLexerException()
    {
        Assert.Throws<FspmLexerException>(() => Lex("submit Create - User.Create"));
    }

    [Fact]
    public void UnterminatedString_ThrowsLexerException()
    {
        Assert.Throws<FspmLexerException>(() => Lex("\"UserManagement"));
    }

    [Fact]
    public void LegacyEntitySyntaxStillWorks()
    {
        var tokens = Lex("entity User");

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.EntityKeyword, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void LegacyPropertySyntaxStillWorks()
    {
        var tokens = Lex("property User.PhoneNumber");

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.PropertyKeyword, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void LegacyOperationSyntaxStillWorks()
    {
        var tokens = Lex("operation User.Create");

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.OperationKeyword, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }
}
