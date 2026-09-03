using Foundry.FSPM.Compiler.Compiler;
using Foundry.FSPM.Compiler.Lexer;
using Foundry.FSPM.Compiler.Parser;

namespace Foundry.FSPM.Compiler.Compiler;

/// <summary>
/// FSPM Compiler entry point (Phase 4 — 施工包 §45, §46).
/// </summary>
/// <remarks>
/// Pipeline:
///   source
///     -> FspmLexer.Lex        (Phase 2)
///     -> IReadOnlyList of FspmToken
///     -> FspmParser.Parse     (Phase 4)
///     -> FspmParseResult
///     -> FspmCompilationResult
///
/// FspmCompiler does NOT throw for Parser problems: the Parser emits
/// FspmDiagnostic records and FspmParseResult.Succeeded reflects the
/// overall outcome. For Lexer exceptions (illegal characters), this
/// entry point catches FspmLexerException and converts it into a
/// single FSPM001 diagnostic so the result shape stays uniform.
///
/// Phase 5+ will extend this class to bind to real Roslyn symbols via
/// MSBuildWorkspace. For now, Phase 4 ends with Syntax + Diagnostics.
/// </remarks>
public sealed class FspmCompiler
{
    public static FspmCompilationResult Compile(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        IReadOnlyList<FspmToken> tokens;
        try
        {
            tokens = FspmLexer.Lex(source);
        }
        catch (FspmLexerException ex)
        {
            return new FspmCompilationResult(
                Succeeded: false,
                Syntax: new Syntax.FspmCompilationUnitSyntax(
                    Array.Empty<Syntax.FspmSyntaxNode>(), 0, 0, 1, 1),
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

        var parseResult = new FspmParser().Parse(tokens);

        return new FspmCompilationResult(
            Succeeded: parseResult.Succeeded,
            Syntax: parseResult.CompilationUnit,
            Diagnostics: parseResult.Diagnostics);
    }
}
