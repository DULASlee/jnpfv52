using Foundry.FSPM.Compiler.Syntax;

namespace Foundry.FSPM.Compiler.Parser;

/// <summary>
/// Output of FspmParser.Parse (Phase 4 — 施工包 §18).
/// Contains the declarations that were successfully parsed plus any
/// diagnostics produced during parsing (one input can produce both
/// good and bad declarations: Parser continues after recoverable errors).
///
/// Succeeded == true means NO Error-severity diagnostics were emitted.
/// Warnings may still be present.
/// </summary>
public sealed record FspmParseResult(
    bool Succeeded,
    FspmCompilationUnitSyntax CompilationUnit,
    IReadOnlyList<Foundry.FSPM.Compiler.Diagnostics.FspmDiagnostic> Diagnostics)
{
    public static FspmParseResult Empty() =>
        new(
            Succeeded: true,
            CompilationUnit: new FspmCompilationUnitSyntax(
                Array.Empty<FspmSyntaxNode>(), 0, 0, 1, 1),
            Diagnostics: Array.Empty<Foundry.FSPM.Compiler.Diagnostics.FspmDiagnostic>());
}
