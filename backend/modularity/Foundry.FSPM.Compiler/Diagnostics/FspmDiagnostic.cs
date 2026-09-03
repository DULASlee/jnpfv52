namespace Foundry.FSPM.Compiler.Diagnostics;

/// <summary>
/// Immutable diagnostic record (Phase 5 — 施工包 §16).
/// Carries Source Position so future AI / IDE can navigate to the exact
/// source slice that caused the problem.
/// </summary>
public sealed record FspmDiagnostic(
    string Code,
    FspmDiagnosticSeverity Severity,
    string Message,
    int Line,
    int Column);
