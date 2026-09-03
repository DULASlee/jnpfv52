using System.Globalization;

namespace Foundry.FSPM.Compiler.Lexer;

/// <summary>
/// Lexer for the FSPM source language (Phase 2 — locked by 施工包 §2 / §8).
/// </summary>
/// <remarks>
/// Scope:
///   - Whitespace, \r handling (skipped)
///   - \n (emitted as NewLine token so Parser can preserve structure)
///   - # ... \n line comments (skipped)
///   - . dot separator
///   - identifier / _ ([LetterOrDigit | _]*)
///   - 3 keywords: entity / property / operation
///   - Phase 12: page / form / field / submit keywords,
///     { } ( ) [ ] ? punctuation, -&gt; arrow,
///     "..." string literals, 0-9 numeric literals
///     (literals exist only so native expressions cross the boundary;
///     the Parser never interprets their values)
///   - illegal character => FspmLexerException
///   - always emits a final EndOfFile token
/// Every emitted token preserves Source Position (Start, Length, Line, Column)
/// so downstream layers can always reach back to the original source slice.
/// </remarks>
public sealed class FspmLexer
{
    public static IReadOnlyList<FspmToken> Lex(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var tokens = new List<FspmToken>();

        var position = 0;
        var line = 1;
        var column = 1;

        while (position < source.Length)
        {
            var ch = source[position];

            // Skip \r (handle CRLF / bare CR consistently).
            if (ch == '\r')
            {
                position++;
                continue;
            }

            if (ch == '\n')
            {
                tokens.Add(new FspmToken(
                    FspmTokenKind.NewLine,
                    "\n",
                    position,
                    1,
                    line,
                    column));

                position++;
                line++;
                column = 1;
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                position++;
                column++;
                continue;
            }

            if (ch == '#')
            {
                while (position < source.Length && source[position] != '\n')
                {
                    position++;
                    column++;
                }
                continue;
            }

            if (ch == '.')
            {
                tokens.Add(new FspmToken(
                    FspmTokenKind.Dot,
                    ".",
                    position,
                    1,
                    line,
                    column));

                position++;
                column++;
                continue;
            }

            // ===== Phase 12 punctuation =====
            if (ch == '{' || ch == '}' || ch == '(' || ch == ')' ||
                ch == '[' || ch == ']' || ch == '?')
            {
                var kind = ch switch
                {
                    '{' => FspmTokenKind.LBrace,
                    '}' => FspmTokenKind.RBrace,
                    '(' => FspmTokenKind.LParen,
                    ')' => FspmTokenKind.RParen,
                    '[' => FspmTokenKind.LBracket,
                    ']' => FspmTokenKind.RBracket,
                    _ => FspmTokenKind.Question,
                };

                tokens.Add(new FspmToken(
                    kind,
                    ch.ToString(),
                    position,
                    1,
                    line,
                    column));

                position++;
                column++;
                continue;
            }

            if (ch == '-')
            {
                if (position + 1 < source.Length && source[position + 1] == '>')
                {
                    tokens.Add(new FspmToken(
                        FspmTokenKind.Arrow,
                        "->",
                        position,
                        2,
                        line,
                        column));

                    position += 2;
                    column += 2;
                    continue;
                }

                throw new FspmLexerException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Invalid character '{0}' at line {1}, column {2}. Did you mean '->'?",
                        ch,
                        line,
                        column));
            }

            if (ch == '"')
            {
                var start = position;
                var startColumn = column;
                position++;
                column++;

                var closed = false;
                while (position < source.Length)
                {
                    var c = source[position];
                    if (c == '\n' || c == '\r')
                    {
                        break;
                    }

                    if (c == '\\' && position + 1 < source.Length)
                    {
                        position += 2;
                        column += 2;
                        continue;
                    }

                    if (c == '"')
                    {
                        closed = true;
                        position++;
                        column++;
                        break;
                    }

                    position++;
                    column++;
                }

                if (!closed)
                {
                    throw new FspmLexerException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Unterminated string literal at line {0}, column {1}.",
                            line,
                            startColumn));
                }

                tokens.Add(new FspmToken(
                    FspmTokenKind.StringLiteral,
                    source.Substring(start, position - start),
                    start,
                    position - start,
                    line,
                    startColumn));

                continue;
            }

            // ===== Phase 12 numeric literal (native-expression boundary only;
            // the Parser never interprets the value) =====
            if (char.IsDigit(ch))
            {
                var start = position;
                var startColumn = column;

                while (position < source.Length && char.IsDigit(source[position]))
                {
                    position++;
                    column++;
                }

                tokens.Add(new FspmToken(
                    FspmTokenKind.NumericLiteral,
                    source.Substring(start, position - start),
                    start,
                    position - start,
                    line,
                    startColumn));

                continue;
            }

            if (char.IsLetter(ch) || ch == '_')
            {
                var start = position;
                var startColumn = column;

                while (position < source.Length &&
                       (char.IsLetterOrDigit(source[position]) || source[position] == '_'))
                {
                    position++;
                    column++;
                }

                var text = source.Substring(start, position - start);

                var kind = text switch
                {
                    "entity" => FspmTokenKind.EntityKeyword,
                    "property" => FspmTokenKind.PropertyKeyword,
                    "operation" => FspmTokenKind.OperationKeyword,
                    "page" => FspmTokenKind.PageKeyword,
                    "form" => FspmTokenKind.FormKeyword,
                    "field" => FspmTokenKind.FieldKeyword,
                    "submit" => FspmTokenKind.SubmitKeyword,
                    _ => FspmTokenKind.Identifier,
                };

                tokens.Add(new FspmToken(
                    kind,
                    text,
                    start,
                    position - start,
                    line,
                    startColumn));

                continue;
            }

            throw new FspmLexerException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Invalid character '{0}' at line {1}, column {2}.",
                    ch,
                    line,
                    column));
        }

        tokens.Add(new FspmToken(
            FspmTokenKind.EndOfFile,
            string.Empty,
            position,
            0,
            line,
            column));

        return tokens;
    }
}
