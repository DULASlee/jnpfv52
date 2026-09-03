using Foundry.FSPM.Compiler.Lexer;
using Foundry.FSPM.Compiler.Parser;

namespace Foundry.FSPM.Compiler.Compiler;

/// <summary>
/// Phase 12 Construction entry point: source → Lexer → ConstructionParser.
/// Parse-only facade (no Resolve/Bind/Verify/Generate — those are P13+).
/// Lexer failures surface as a single FSPM001 diagnostic so the result
/// shape stays uniform (mirrors <see cref="FspmCompiler"/>).
/// </summary>
public sealed class FspmConstructionCompiler
{
    public static FspmConstructionParseResult Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        IReadOnlyList<FspmToken> tokens;
        try
        {
            tokens = FspmLexer.Lex(source);
        }
        catch (FspmLexerException ex)
        {
            return new FspmConstructionParseResult(
                Succeeded: false,
                Document: FspmConstructionDocumentSyntax_Empty(),
                Diagnostics: new Diagnostics.FspmDiagnostic[]
                {
                    new(
                        Code: Diagnostics.FspmDiagnosticCodes.UnexpectedToken,
                        Severity: Diagnostics.FspmDiagnosticSeverity.Error,
                        Message: ex.Message,
                        Line: 1,
                        Column: 1),
                });
        }

        return new FspmConstructionParser().Parse(tokens, source);
    }

    private static Syntax.FspmConstructionDocumentSyntax FspmConstructionDocumentSyntax_Empty() =>
        new(
            Pages: Array.Empty<Syntax.FspmPageSyntax>(),
            Start: 0,
            Length: 0,
            Line: 1,
            Column: 1);
}
