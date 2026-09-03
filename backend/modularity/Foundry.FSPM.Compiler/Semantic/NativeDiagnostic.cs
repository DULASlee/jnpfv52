namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H7: where a diagnostic originates. Roslyn diagnostics are quoted
/// verbatim; FSPM diagnostics are minted by P13 gates.
/// </summary>
public enum NativeDiagnosticSource
{
    Roslyn,
    Fspm,
}

/// <summary>
/// P13-H7: structured native diagnostic. All fields are plain data —
/// related symbols travel as stable identity strings, never as live
/// <c>ISymbol</c> references.
/// </summary>
public sealed record NativeDiagnostic(
    string DiagnosticId,
    string Code,
    string Severity,
    string Message,
    NativeDiagnosticSource Source,
    FspmSourceLocation? Location,
    IReadOnlyList<string> CandidateIdentities,
    string? RelatedSymbolIdentity);

/// <summary>
/// P13-H7: semantic quality of a resolution performed on an imperfect
/// compilation. <c>Degraded</c> means "resolved, but the compilation has
/// errors, so the answer is partial" — never disguised as perfect.
/// </summary>
public enum SemanticQuality
{
    Perfect,
    Degraded,
}
