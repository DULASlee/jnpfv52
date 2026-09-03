using Foundry.FSPM.Compiler.Syntax;

namespace Foundry.FSPM.Compiler.Parser;

/// <summary>
/// Output of <see cref="FspmConstructionParser"/> (Phase 12).
/// Mirrors <see cref="FspmParseResult"/> shape: successfully parsed pages
/// plus diagnostics (one input can yield both). Succeeded == true means
/// NO Error-severity diagnostics were emitted.
/// </summary>
public sealed record FspmConstructionParseResult(
    bool Succeeded,
    FspmConstructionDocumentSyntax Document,
    IReadOnlyList<Foundry.FSPM.Compiler.Diagnostics.FspmDiagnostic> Diagnostics)
{
    public static FspmConstructionParseResult Empty() =>
        new(
            Succeeded: true,
            Document: new FspmConstructionDocumentSyntax(
                Array.Empty<FspmPageSyntax>(), 0, 0, 1, 1),
            Diagnostics: Array.Empty<Foundry.FSPM.Compiler.Diagnostics.FspmDiagnostic>());
}
