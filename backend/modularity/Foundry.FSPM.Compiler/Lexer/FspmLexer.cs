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
