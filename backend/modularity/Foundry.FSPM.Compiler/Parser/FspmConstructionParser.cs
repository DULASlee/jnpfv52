using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Lexer;
using Foundry.FSPM.Compiler.Syntax;

namespace Foundry.FSPM.Compiler.Parser;

/// <summary>
/// Parser for the Phase 12 Construction language (L2 composite layer).
/// </summary>
/// <remarks>
/// Grammar (frozen by P12施工包 §9):
///   document        := page*
///   page            := "page" identifier "{" form* "}"
///   form            := "form" identifier "{" binding* "}"
///   binding         := entityBinding | fieldBinding | submitBinding
///   entityBinding   := "entity" nativeExpression
///   fieldBinding    := "field" nativeExpression
///   submitBinding   := "submit" identifier "-&gt;" nativeExpression
///
/// L1 flat declarations (entity/property/operation) stay in
/// <see cref="FspmParser"/>: this parser is an incremental extension,
/// never a rewrite. Native expressions are captured verbatim
/// (see <see cref="FspmNativeExpressionSyntax"/>): no '.' splitting,
/// no receiver/member analysis — that is P13 Roslyn territory.
///
/// Recovery: after an error the parser skips to the next structural
/// boundary (a keyword, newline, or closing brace). Nested page-in-page
/// or form-in-form is reported as FSPM007 and consumed by a recursive
/// discard parse so brace depth always resolves.
/// </remarks>
public sealed class FspmConstructionParser
{
    private readonly List<FspmPageSyntax> _pages = new();
    private readonly List<FspmDiagnostic> _diagnostics = new();
    private string _source = string.Empty;

    public FspmConstructionParseResult Parse(IReadOnlyList<FspmToken> tokens, string sourceText)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(sourceText);

        _pages.Clear();
        _diagnostics.Clear();
        _source = sourceText;

        var position = SkipNewLines(tokens, 0);

        while (position < tokens.Count && tokens[position].Kind != FspmTokenKind.EndOfFile)
        {
            var token = tokens[position];

            if (token.Kind == FspmTokenKind.PageKeyword)
            {
                position = ParsePage(tokens, position, discard: false);
            }
            else
            {
                EmitError(FspmDiagnosticCodes.UnexpectedToken,
                    $"Unexpected token '{token.Text}' at line {token.Line}, column {token.Column}. Expected 'page'.",
                    token);
                position = RecoverToPageBoundary(tokens, position + 1);
            }

            position = SkipNewLines(tokens, position);
        }

        var hasError = _diagnostics.Any(d => d.Severity == FspmDiagnosticSeverity.Error);
        var document = new FspmConstructionDocumentSyntax(
            _pages.ToArray(),
            Start: 0,
            Length: 0,
            Line: 1,
            Column: 1);

        return new FspmConstructionParseResult(
            Succeeded: !hasError,
            Document: document,
            Diagnostics: _diagnostics.ToArray());
    }

    // Step 02: parse page. When discard is true the page is consumed for
    // recovery (nested page-in-page) and dropped instead of recorded.
    private int ParsePage(IReadOnlyList<FspmToken> tokens, int position, bool discard)
    {
        var keyword = tokens[position];
        position++;

        if (!ExpectIdentifier(tokens, ref position, out var nameToken, $"'page'"))
        {
            return RecoverToPageBoundary(tokens, position);
        }

        if (!ExpectBrace(tokens, ref position, nameToken))
        {
            return RecoverToPageBoundary(tokens, position);
        }

        var forms = new List<FspmFormSyntax>();
        position = SkipNewLines(tokens, position);

        while (position < tokens.Count)
        {
            var kind = tokens[position].Kind;
            if (kind == FspmTokenKind.EndOfFile)
            {
                EmitError(FspmDiagnosticCodes.MissingBrace,
                    $"Missing closing '}}' for page '{nameToken.Text}' opened at line {keyword.Line}, column {keyword.Column}.",
                    nameToken);
                break;
            }

            if (kind == FspmTokenKind.RBrace)
            {
                var close = tokens[position];
                position++;
                if (!discard)
                {
                    _pages.Add(new FspmPageSyntax(
                        Name: nameToken.Text,
                        Forms: forms.ToArray(),
                        Start: keyword.Start,
                        Length: close.Start + close.Length - keyword.Start,
                        Line: keyword.Line,
                        Column: keyword.Column));
                }

                return position;
            }

            if (kind == FspmTokenKind.FormKeyword)
            {
                position = ParseForm(tokens, position, forms, discard: false);
            }
            else if (kind == FspmTokenKind.PageKeyword)
            {
                EmitError(FspmDiagnosticCodes.InvalidNesting,
                    $"Unexpected nested 'page' at line {tokens[position].Line}, column {tokens[position].Column}. Pages cannot nest.",
                    tokens[position]);
                position = ParsePage(tokens, position, discard: true);
            }
            else if (kind == FspmTokenKind.EntityKeyword ||
                     kind == FspmTokenKind.FieldKeyword ||
                     kind == FspmTokenKind.SubmitKeyword)
            {
                EmitError(FspmDiagnosticCodes.InvalidNesting,
                    $"Unexpected '{tokens[position].Text}' directly under page '{nameToken.Text}' at line {tokens[position].Line}, column {tokens[position].Column}. Bindings belong inside a form.",
                    tokens[position]);
                position = RecoverToFormBoundary(tokens, position + 1);
            }
            else
            {
                EmitError(FspmDiagnosticCodes.UnexpectedToken,
                    $"Unexpected token '{tokens[position].Text}' at line {tokens[position].Line}, column {tokens[position].Column}. Expected 'form' or '}}'.",
                    tokens[position]);
                position = RecoverToFormBoundary(tokens, position + 1);
            }

            position = SkipNewLines(tokens, position);
        }

        return position;
    }

    // Step 03: parse form.
    private int ParseForm(
        IReadOnlyList<FspmToken> tokens, int position, List<FspmFormSyntax> forms, bool discard)
    {
        var keyword = tokens[position];
        position++;

        if (!ExpectIdentifier(tokens, ref position, out var nameToken, "'form'"))
        {
            return RecoverToFormBoundary(tokens, position);
        }

        if (!ExpectBrace(tokens, ref position, nameToken))
        {
            return RecoverToFormBoundary(tokens, position);
        }

        var bindings = new List<FspmSyntaxNode>();
        var end = keyword.Start + keyword.Length;
        position = SkipNewLines(tokens, position);

        while (position < tokens.Count)
        {
            var token = tokens[position];
            switch (token.Kind)
            {
                case FspmTokenKind.EndOfFile:
                    EmitError(FspmDiagnosticCodes.MissingBrace,
                        $"Missing closing '}}' for form '{nameToken.Text}' opened at line {keyword.Line}, column {keyword.Column}.",
                        nameToken);
                    end = token.Start;
                    goto Done;

                case FspmTokenKind.RBrace:
                    end = token.Start + token.Length;
                    position++;
                    goto Done;

                case FspmTokenKind.EntityKeyword:
                    position = ParseEntityBinding(tokens, position, bindings);
                    break;

                case FspmTokenKind.FieldKeyword:
                    position = ParseFieldBinding(tokens, position, bindings);
                    break;

                case FspmTokenKind.SubmitKeyword:
                    position = ParseSubmitBinding(tokens, position, bindings);
                    break;

                case FspmTokenKind.FormKeyword:
                case FspmTokenKind.PageKeyword:
                    EmitError(FspmDiagnosticCodes.InvalidNesting,
                        $"Unexpected nested '{token.Text}' inside form '{nameToken.Text}' at line {token.Line}, column {token.Column}.",
                        token);
                    position = DiscardNestedBlock(tokens, position);
                    break;

                default:
                    EmitError(FspmDiagnosticCodes.UnexpectedToken,
                        $"Unexpected token '{token.Text}' at line {token.Line}, column {token.Column}. Expected 'entity', 'field', 'submit' or '}}'.",
                        token);
                    position = RecoverToBindingBoundary(tokens, position + 1);
                    break;
            }

            position = SkipNewLines(tokens, position);
        }

    Done:
        if (!discard)
        {
            forms.Add(new FspmFormSyntax(
                Name: nameToken.Text,
                Bindings: bindings.ToArray(),
                Start: keyword.Start,
                Length: Math.Max(0, end - keyword.Start),
                Line: keyword.Line,
                Column: keyword.Column));
        }

        return position;
    }

    // Steps 04-06: bindings.
    private int ParseEntityBinding(
        IReadOnlyList<FspmToken> tokens, int position, List<FspmSyntaxNode> bindings)
    {
        var keyword = tokens[position];
        position++;

        var (expression, next) = ParseNativeExpression(tokens, position, "'entity'");
        if (expression is null)
        {
            return next;
        }

        bindings.Add(new FspmEntityBindingSyntax(
            Expression: expression,
            Start: keyword.Start,
            Length: expression.Start + expression.Length - keyword.Start,
            Line: keyword.Line,
            Column: keyword.Column));
        return next;
    }

    private int ParseFieldBinding(
        IReadOnlyList<FspmToken> tokens, int position, List<FspmSyntaxNode> bindings)
    {
        var keyword = tokens[position];
        position++;

        var (expression, next) = ParseNativeExpression(tokens, position, "'field'");
        if (expression is null)
        {
            return next;
        }

        bindings.Add(new FspmFieldBindingSyntax(
            Expression: expression,
            Start: keyword.Start,
            Length: expression.Start + expression.Length - keyword.Start,
            Line: keyword.Line,
            Column: keyword.Column));
        return next;
    }

    private int ParseSubmitBinding(
        IReadOnlyList<FspmToken> tokens, int position, List<FspmSyntaxNode> bindings)
    {
        var keyword = tokens[position];
        position++;

        if (!ExpectIdentifier(tokens, ref position, out var nameToken, "'submit'"))
        {
            return RecoverToBindingBoundary(tokens, position);
        }

        if (position >= tokens.Count || tokens[position].Kind != FspmTokenKind.Arrow)
        {
            var at = position < tokens.Count ? tokens[position] : nameToken;
            EmitError(FspmDiagnosticCodes.MissingArrow,
                $"Expected '->' after submit name '{nameToken.Text}' at line {nameToken.Line}, column {nameToken.Column}.",
                at);
            return RecoverToBindingBoundary(tokens, position);
        }

        position++;

        var (target, next) = ParseNativeExpression(tokens, position, "'submit ... ->'");
        if (target is null)
        {
            return next;
        }

        bindings.Add(new FspmSubmitBindingSyntax(
            Name: nameToken.Text,
            Target: target,
            Start: keyword.Start,
            Length: target.Start + target.Length - keyword.Start,
            Line: keyword.Line,
            Column: keyword.Column));
        return next;
    }

    // Step 07: native expression boundary. Consumes tokens up to (not
    // including) NewLine / RBrace / EOF and captures the verbatim source
    // slice. NEVER splits on '.' or interprets the contents.
    private (FspmNativeExpressionSyntax? Expression, int Next) ParseNativeExpression(
        IReadOnlyList<FspmToken> tokens, int position, string context)
    {
        var start = position;
        while (position < tokens.Count)
        {
            var kind = tokens[position].Kind;
            if (kind == FspmTokenKind.NewLine ||
                kind == FspmTokenKind.RBrace ||
                kind == FspmTokenKind.EndOfFile)
            {
                break;
            }

            position++;
        }

        if (position == start)
        {
            var at = position < tokens.Count ? tokens[position] : tokens[tokens.Count - 1];
            EmitError(FspmDiagnosticCodes.MissingExpression,
                $"Expected expression after {context} at line {at.Line}, column {at.Column}.",
                at);
            return (null, position);
        }

        var first = tokens[start];
        var last = tokens[position - 1];
        var end = Math.Min(last.Start + last.Length, _source.Length);
        var textStart = Math.Min(first.Start, _source.Length);
        var text = end > textStart ? _source.Substring(textStart, end - textStart) : string.Empty;

        return (new FspmNativeExpressionSyntax(
            Text: text,
            Start: first.Start,
            Length: end - first.Start,
            Line: first.Line,
            Column: first.Column), position);
    }

    private bool ExpectIdentifier(
        IReadOnlyList<FspmToken> tokens, ref int position, out FspmToken name, string context)
    {
        if (position < tokens.Count && tokens[position].Kind == FspmTokenKind.Identifier)
        {
            name = tokens[position];
            position++;
            return true;
        }

        var at = position < tokens.Count ? tokens[position] : tokens[tokens.Count - 1];
        EmitError(FspmDiagnosticCodes.MissingIdentifier,
            $"Expected name after {context} at line {at.Line}, column {at.Column}.",
            at);
        name = at;
        return false;
    }

    private bool ExpectBrace(IReadOnlyList<FspmToken> tokens, ref int position, FspmToken nameToken)
    {
        if (position < tokens.Count && tokens[position].Kind == FspmTokenKind.LBrace)
        {
            position++;
            return true;
        }

        var at = position < tokens.Count ? tokens[position] : nameToken;
        EmitError(FspmDiagnosticCodes.MissingBrace,
            $"Expected '{{' after '{nameToken.Text}' at line {nameToken.Line}, column {nameToken.Column}.",
            at);
        return false;
    }

    // Consumes a nested page/form block (already reported as FSPM007) by
    // brace-depth matching so the outer parse always regains its level.
    private static int DiscardNestedBlock(IReadOnlyList<FspmToken> tokens, int position)
    {
        var depth = 0;
        while (position < tokens.Count && tokens[position].Kind != FspmTokenKind.EndOfFile)
        {
            var kind = tokens[position].Kind;
            if (kind == FspmTokenKind.LBrace)
            {
                depth++;
            }
            else if (kind == FspmTokenKind.RBrace)
            {
                if (depth == 0)
                {
                    break;
                }

                depth--;
            }
            else if (depth == 0 && kind == FspmTokenKind.NewLine)
            {
                position++;
                break;
            }

            position++;
        }

        return position;
    }

    private static int SkipNewLines(IReadOnlyList<FspmToken> tokens, int position)
    {
        while (position < tokens.Count && tokens[position].Kind == FspmTokenKind.NewLine)
        {
            position++;
        }

        return position;
    }

    private static int RecoverToPageBoundary(IReadOnlyList<FspmToken> tokens, int position)
    {
        while (position < tokens.Count && tokens[position].Kind != FspmTokenKind.EndOfFile)
        {
            var k = tokens[position].Kind;
            if (k == FspmTokenKind.PageKeyword || k == FspmTokenKind.NewLine)
            {
                break;
            }

            position++;
        }

        return position;
    }

    private static int RecoverToFormBoundary(IReadOnlyList<FspmToken> tokens, int position)
    {
        while (position < tokens.Count && tokens[position].Kind != FspmTokenKind.EndOfFile)
        {
            var k = tokens[position].Kind;
            if (k == FspmTokenKind.FormKeyword ||
                k == FspmTokenKind.PageKeyword ||
                k == FspmTokenKind.RBrace ||
                k == FspmTokenKind.NewLine)
            {
                break;
            }

            position++;
        }

        return position;
    }

    private static int RecoverToBindingBoundary(IReadOnlyList<FspmToken> tokens, int position)
    {
        while (position < tokens.Count && tokens[position].Kind != FspmTokenKind.EndOfFile)
        {
            var k = tokens[position].Kind;
            if (k == FspmTokenKind.EntityKeyword ||
                k == FspmTokenKind.FieldKeyword ||
                k == FspmTokenKind.SubmitKeyword ||
                k == FspmTokenKind.FormKeyword ||
                k == FspmTokenKind.PageKeyword ||
                k == FspmTokenKind.RBrace ||
                k == FspmTokenKind.NewLine)
            {
                break;
            }

            position++;
        }

        return position;
    }

    private void EmitError(string code, string message, FspmToken location)
    {
        _diagnostics.Add(new FspmDiagnostic(
            Code: code,
            Severity: FspmDiagnosticSeverity.Error,
            Message: message,
            Line: location.Line,
            Column: location.Column,
            Start: location.Start,
            Length: location.Length));
    }
}
