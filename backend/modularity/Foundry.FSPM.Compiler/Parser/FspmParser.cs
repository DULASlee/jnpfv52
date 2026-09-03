using Foundry.FSPM.Compiler.Diagnostics;
using Foundry.FSPM.Compiler.Lexer;
using Foundry.FSPM.Compiler.Syntax;

namespace Foundry.FSPM.Compiler.Parser;

/// <summary>
/// Parser for the FSPM source language (Phase 4 — 施工包 §15, §18).
/// </summary>
/// <remarks>
/// Grammar (locked by 施工包 §2):
///   entity       IDENT                    -> EntityDeclaration
///   property     IDENT . IDENT            -> PropertyDeclaration
///   operation    IDENT . IDENT            -> OperationDeclaration
///
/// Tokens other than keywords / identifiers / Dot / NewLine are
/// unexpected at declaration start and produce FSPM001.
///
/// Required sequencing errors produce FSPM002 (MissingIdentifier)
/// or FSPM003 (MissingDot). Duplicates produce FSPM004.
///
/// Parser recovers from errors: after an error it skips to the next
/// declaration boundary (a keyword or newline). This prevents
/// cascading diagnostics from a single root cause.
/// </remarks>
public sealed class FspmParser
{
    private readonly List<FspmSyntaxNode> _declarations = new();
    private readonly List<FspmDiagnostic> _diagnostics = new();
    private readonly HashSet<string> _entityNames = new(StringComparer.Ordinal);

    public FspmParseResult Parse(IReadOnlyList<FspmToken> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        _declarations.Clear();
        _diagnostics.Clear();
        _entityNames.Clear();

        var position = SkipNewLines(tokens, 0);

        while (position < tokens.Count && tokens[position].Kind != FspmTokenKind.EndOfFile)
        {
            var token = tokens[position];

            switch (token.Kind)
            {
                case FspmTokenKind.EntityKeyword:
                    position = ParseEntity(tokens, position);
                    break;
                case FspmTokenKind.PropertyKeyword:
                    position = ParseProperty(tokens, position);
                    break;
                case FspmTokenKind.OperationKeyword:
                    position = ParseOperation(tokens, position);
                    break;
                default:
                    EmitError(FspmDiagnosticCodes.UnexpectedToken,
                        $"Unexpected token '{token.Text}' at line {token.Line}, column {token.Column}.",
                        token);
                    position = RecoverToDeclarationBoundary(tokens, position + 1);
                    break;
            }

            position = SkipNewLines(tokens, position);
        }

        var hasError = _diagnostics.Any(d => d.Severity == FspmDiagnosticSeverity.Error);

        var firstToken = tokens.Count > 0 ? tokens[0] : null;
        var lastNonEof = tokens.Count - 1;
        while (lastNonEof > 0 && tokens[lastNonEof].Kind == FspmTokenKind.EndOfFile)
        {
            lastNonEof--;
        }

        var unitLength = (lastNonEof >= 0 && firstToken != null)
            ? tokens[lastNonEof].Start + tokens[lastNonEof].Length - firstToken.Start
            : 0;

        var unit = new FspmCompilationUnitSyntax(
            _declarations.ToArray(),
            Start: firstToken?.Start ?? 0,
            Length: unitLength,
            Line: firstToken?.Line ?? 1,
            Column: firstToken?.Column ?? 1);

        return new FspmParseResult(
            Succeeded: !hasError,
            CompilationUnit: unit,
            Diagnostics: _diagnostics.ToArray());
    }

    private int ParseEntity(IReadOnlyList<FspmToken> tokens, int position)
    {
        var keyword = tokens[position];
        var start = keyword.Start;
        var line = keyword.Line;
        var column = keyword.Column;

        position++;

        if (position >= tokens.Count || tokens[position].Kind != FspmTokenKind.Identifier)
        {
            EmitError(FspmDiagnosticCodes.MissingIdentifier,
                $"Expected entity name after 'entity' at line {line}, column {column}.",
                keyword);
            return RecoverToDeclarationBoundary(tokens, position);
        }

        var ident = tokens[position];
        position++;

        if (!_entityNames.Add(ident.Text))
        {
            EmitError(FspmDiagnosticCodes.DuplicateDeclaration,
                $"Duplicate entity '{ident.Text}' at line {ident.Line}, column {ident.Column}.",
                ident);
        }

        var totalLength = ident.Start + ident.Length - start;

        _declarations.Add(new FspmEntityDeclarationSyntax(
            Name: ident.Text,
            Start: start,
            Length: totalLength,
            Line: line,
            Column: column));

        return position;
    }

    private int ParseProperty(IReadOnlyList<FspmToken> tokens, int position)
    {
        var keyword = tokens[position];
        var start = keyword.Start;
        var line = keyword.Line;
        var column = keyword.Column;

        position++;

        if (position >= tokens.Count || tokens[position].Kind != FspmTokenKind.Identifier)
        {
            EmitError(FspmDiagnosticCodes.MissingIdentifier,
                $"Expected entity name after 'property' at line {line}, column {column}.",
                keyword);
            return RecoverToDeclarationBoundary(tokens, position);
        }

        var entityName = tokens[position];
        position++;

        if (position >= tokens.Count || tokens[position].Kind != FspmTokenKind.Dot)
        {
            EmitError(FspmDiagnosticCodes.MissingDot,
                $"Expected '.' after entity name '{entityName.Text}' at line {entityName.Line}, column {entityName.Column}.",
                entityName);
            return RecoverToDeclarationBoundary(tokens, position);
        }

        position++;

        if (position >= tokens.Count || tokens[position].Kind != FspmTokenKind.Identifier)
        {
            EmitError(FspmDiagnosticCodes.MissingIdentifier,
                $"Expected property name after '.' at line {entityName.Line}, column {entityName.Column}.",
                entityName);
            return RecoverToDeclarationBoundary(tokens, position);
        }

        var propertyName = tokens[position];
        position++;

        var totalLength = propertyName.Start + propertyName.Length - start;

        _declarations.Add(new FspmPropertyDeclarationSyntax(
            EntityName: entityName.Text,
            PropertyName: propertyName.Text,
            Start: start,
            Length: totalLength,
            Line: line,
            Column: column));

        return position;
    }

    private int ParseOperation(IReadOnlyList<FspmToken> tokens, int position)
    {
        var keyword = tokens[position];
        var start = keyword.Start;
        var line = keyword.Line;
        var column = keyword.Column;

        position++;

        if (position >= tokens.Count || tokens[position].Kind != FspmTokenKind.Identifier)
        {
            EmitError(FspmDiagnosticCodes.MissingIdentifier,
                $"Expected entity name after 'operation' at line {line}, column {column}.",
                keyword);
            return RecoverToDeclarationBoundary(tokens, position);
        }

        var entityName = tokens[position];
        position++;

        if (position >= tokens.Count || tokens[position].Kind != FspmTokenKind.Dot)
        {
            EmitError(FspmDiagnosticCodes.MissingDot,
                $"Expected '.' after entity name '{entityName.Text}' at line {entityName.Line}, column {entityName.Column}.",
                entityName);
            return RecoverToDeclarationBoundary(tokens, position);
        }

        position++;

        if (position >= tokens.Count || tokens[position].Kind != FspmTokenKind.Identifier)
        {
            EmitError(FspmDiagnosticCodes.MissingIdentifier,
                $"Expected operation name after '.' at line {entityName.Line}, column {entityName.Column}.",
                entityName);
            return RecoverToDeclarationBoundary(tokens, position);
        }

        var operationName = tokens[position];
        position++;

        var totalLength = operationName.Start + operationName.Length - start;

        _declarations.Add(new FspmOperationDeclarationSyntax(
            EntityName: entityName.Text,
            OperationName: operationName.Text,
            Start: start,
            Length: totalLength,
            Line: line,
            Column: column));

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

    private static int RecoverToDeclarationBoundary(IReadOnlyList<FspmToken> tokens, int position)
    {
        while (position < tokens.Count && tokens[position].Kind != FspmTokenKind.EndOfFile)
        {
            var k = tokens[position].Kind;
            if (k == FspmTokenKind.EntityKeyword ||
                k == FspmTokenKind.PropertyKeyword ||
                k == FspmTokenKind.OperationKeyword ||
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
            Column: location.Column));
    }
}
