using Foundry.FSPM.Compiler.Lexer;
using System.Linq;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

/// <summary>
/// Phase 2 Lexer Gate (G01) — real source in, real token stream out.
/// Token sequence asserted against 施工包 §9, never against hardcoded fixtures.
/// </summary>
public sealed class LexerTests
{
    private const string NL = "\n";
    private const string RT = "\r";
    private const string TB = "\t";

    private static IReadOnlyList<FspmToken> Lex(string source) =>
        FspmLexer.Lex(source);

    // ===== §9 / G01 / Positive =====

    [Fact]
    public void Lex_EntityDeclaration_Produces_Entity_Identifier_EOF()
    {
        var tokens = Lex("entity User");

        Assert.Collection(
            tokens,
            t => { Assert.Equal(FspmTokenKind.EntityKeyword, t.Kind); Assert.Equal("entity", t.Text); },
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("User", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Lex_PropertyDeclaration_Produces_Property_Identifier_Dot_Identifier_EOF()
    {
        var tokens = Lex("property User.PhoneNumber");

        Assert.Collection(
            tokens,
            t => { Assert.Equal(FspmTokenKind.PropertyKeyword, t.Kind); Assert.Equal("property", t.Text); },
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("User", t.Text); },
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("PhoneNumber", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Lex_OperationDeclaration_Produces_Operation_Identifier_Dot_Identifier_EOF()
    {
        var tokens = Lex("operation User.Login");

        Assert.Collection(
            tokens,
            t => { Assert.Equal(FspmTokenKind.OperationKeyword, t.Kind); Assert.Equal("operation", t.Text); },
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("User", t.Text); },
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("Login", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    // ===== §9 / Comment handling =====

    [Fact]
    public void Lex_LineComment_IsSkipped()
    {
        var source = "# hello" + NL + NL + "entity User";
        var tokens = Lex(source);

        // Phase 2 grammar (施工包 §8): comment runs to next `n, which IS emitted as
        // a structural NewLine token. So the actual sequence is:
        //   NewLine   (terminator of "# hello")
        //   NewLine   (the blank line)
        //   EntityKeyword
        //   Identifier(User)
        //   EndOfFile
        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.NewLine, t.Kind),
            t => Assert.Equal(FspmTokenKind.NewLine, t.Kind),
            t => Assert.Equal(FspmTokenKind.EntityKeyword, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Lex_TrailingInlineComment_IsSkipped()
    {
        var source = "entity User  # this is the user entity" + NL + "property User.Name";
        var tokens = Lex(source);

        // Trailing inline comment: comment is consumed to `n; the `n is emitted as
        // a structural NewLine token.
        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.EntityKeyword, t.Kind),
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("User", t.Text); },
            t => Assert.Equal(FspmTokenKind.NewLine, t.Kind),
            t => Assert.Equal(FspmTokenKind.PropertyKeyword, t.Kind),
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("User", t.Text); },
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("Name", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    // ===== Whitespace handling =====

    [Fact]
    public void Lex_MultipleSpaces_AreSkipped()
    {
        var tokens = Lex("   entity    User   ");

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.EntityKeyword, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Lex_Tab_IsSkipped()
    {
        var source = "entity" + TB + "User";
        var tokens = Lex(source);

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.EntityKeyword, t.Kind),
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("User", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    // ===== Newline tokens =====

    [Fact]
    public void Lex_MultipleLines_Produce_NewLineTokens()
    {
        var source = "entity User" + NL + "property User.Name";
        var tokens = Lex(source);

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.EntityKeyword, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.NewLine, t.Kind),
            t => Assert.Equal(FspmTokenKind.PropertyKeyword, t.Kind),
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("User", t.Text); },
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("Name", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Lex_CRLF_IsTreatedAsSingleNewLine()
    {
        // CR must be silently skipped so that CRLF behaves identically to LF.
        var source = "entity User" + RT + NL + "property User.Name";
        var tokens = Lex(source);

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.EntityKeyword, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.NewLine, t.Kind),
            t => Assert.Equal(FspmTokenKind.PropertyKeyword, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => Assert.Equal(FspmTokenKind.Identifier, t.Kind),
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    // ===== Identifier rules =====

    [Fact]
    public void Lex_Identifier_AllowsUnderscoreStart()
    {
        var tokens = Lex("entity _User");

        Assert.Equal(FspmTokenKind.Identifier, tokens[1].Kind);
        Assert.Equal("_User", tokens[1].Text);
    }

    [Fact]
    public void Lex_Identifier_AllowsDigitsAfterLetter()
    {
        var tokens = Lex("entity User2_3");

        Assert.Equal(FspmTokenKind.Identifier, tokens[1].Kind);
        Assert.Equal("User2_3", tokens[1].Text);
    }

    [Fact]
    public void Lex_KeywordInsideIdentifier_IsNotKeyword()
    {
        // "entityUser" is a single identifier, NOT (keyword entity + identifier User).
        var tokens = Lex("entityUser");

        Assert.Single(tokens.Where(t => t.Kind != FspmTokenKind.EndOfFile));
        Assert.Equal(FspmTokenKind.Identifier, tokens[0].Kind);
        Assert.Equal("entityUser", tokens[0].Text);
    }

    // ===== Symbols / dots =====

    [Fact]
    public void Lex_ConsecutiveDots_ProduceMultipleDotTokens()
    {
        var tokens = Lex("entity A..B");

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.EntityKeyword, t.Kind),
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("A", t.Text); },
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => Assert.Equal(FspmTokenKind.Dot, t.Kind),
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("B", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    // ===== Illegal character =====

    [Fact]
    public void Lex_IllegalCharacter_Throws_LexerException_WithLocation()
    {
        var ex = Assert.Throws<FspmLexerException>(() => Lex("entity $User"));
        Assert.Contains("'$'", ex.Message);
        Assert.Contains("line 1", ex.Message);
    }

    [Fact]
    public void Lex_IllegalCharacter_OnSecondLine_LocatesCorrectLine()
    {
        var source = "entity User" + NL + "property @Name";
        var ex = Assert.Throws<FspmLexerException>(() => Lex(source));
        Assert.Contains("line 2", ex.Message);
    }

    [Fact]
    public void Lex_NumericLiteral_IsLegal()
    {
        // P12 extends the language with NumericLiteral tokens. The Lexer
        // recognizes '123' as a token WITHOUT knowing its semantic role;
        // a Parser / Native Expression / Roslyn decides where digits are
        // valid (an Identifier may NOT start with a digit, that is a
        // separate fact covered by the next test).
        var tokens = Lex("123");

        Assert.Collection(
            tokens,
            t => { Assert.Equal(FspmTokenKind.NumericLiteral, t.Kind); Assert.Equal("123", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    [Fact]
    public void Lex_Identifier_CannotStartWithDigit()
    {
        // 123User is NOT a single Identifier. The Lexer splits it into
        // NumericLiteral(123) + Identifier(User). The 123User surface in
        // a position expecting a name is then a Parser-level failure
        // (the new lexer contract; the former single-name failure is
        // renamed and intent-corrected here).
        var tokens = Lex("entity 123User");

        Assert.Collection(
            tokens,
            t => Assert.Equal(FspmTokenKind.EntityKeyword, t.Kind),
            t => { Assert.Equal(FspmTokenKind.NumericLiteral, t.Kind); Assert.Equal("123", t.Text); },
            t => { Assert.Equal(FspmTokenKind.Identifier, t.Kind); Assert.Equal("User", t.Text); },
            t => Assert.Equal(FspmTokenKind.EndOfFile, t.Kind));
    }

    // ===== EOF behavior =====

    [Fact]
    public void Lex_EmptySource_ProducesOnlyEOF()
    {
        var tokens = Lex(string.Empty);

        Assert.Single(tokens);
        Assert.Equal(FspmTokenKind.EndOfFile, tokens[0].Kind);
        Assert.Equal(0, tokens[0].Start);
        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(1, tokens[0].Column);
    }

    [Fact]
    public void Lex_AlwaysEndsWithEOF_RegardlessOfTrailingNewline()
    {
        var source = "entity User" + NL;
        var tokens = Lex(source);

        Assert.Equal(FspmTokenKind.EndOfFile, tokens[^1].Kind);
    }

    [Fact]
    public void Lex_AlwaysEndsWithEOF_WithoutTrailingNewline()
    {
        var tokens = Lex("entity User");

        Assert.Equal(FspmTokenKind.EndOfFile, tokens[^1].Kind);
    }

    // ===== Position info (Hard Constraint #2) =====

    [Fact]
    public void Lex_Tokens_CarryStableStartLengthLineColumn()
    {
        // Layout (1-based):
        //   123456789012
        //   entity User
        //   ^6       ^11
        // (column 1 is 'e')
        var tokens = Lex("entity User");

        Assert.Equal(FspmTokenKind.EntityKeyword, tokens[0].Kind);
        Assert.Equal("entity", tokens[0].Text);
        Assert.Equal(0, tokens[0].Start);
        Assert.Equal(6, tokens[0].Length);
        Assert.Equal(1, tokens[0].Line);
        Assert.Equal(1, tokens[0].Column);

        Assert.Equal(FspmTokenKind.Identifier, tokens[1].Kind);
        Assert.Equal("User", tokens[1].Text);
        Assert.Equal(7, tokens[1].Start);
        Assert.Equal(4, tokens[1].Length);
        Assert.Equal(1, tokens[1].Line);
        Assert.Equal(8, tokens[1].Column);
    }

    [Fact]
    public void Lex_Tokens_OnLine2_ReportCorrectLineAndColumn()
    {
        // Line 1: "entity User" (length 11, no NL)
        // Line 2: "property User.Name"
        //          col 1=p, col 9=U, col 13=N
        var source = "entity User" + NL + "property User.Name";
        var tokens = Lex(source);

        var propertyToken = tokens.First(t => t.Kind == FspmTokenKind.PropertyKeyword);
        Assert.Equal(2, propertyToken.Line);
        Assert.Equal(1, propertyToken.Column);

        var userOnLine2 = tokens.Where(t => t.Kind == FspmTokenKind.Identifier).ElementAt(1);
        Assert.Equal(2, userOnLine2.Line);
        Assert.Equal(10, userOnLine2.Column); // "property " = 9 chars, column starts at 10
    }

    [Fact]
    public void Lex_Tokens_Slice_ReproducesOriginalText()
    {
        const string source = "property User.PhoneNumber";
        var tokens = Lex(source);

        foreach (var t in tokens)
        {
            if (t.Kind == FspmTokenKind.EndOfFile) continue;
            var slice = source.Substring(t.Start, t.Length);
            Assert.Equal(t.Text, slice);
        }
    }
}
