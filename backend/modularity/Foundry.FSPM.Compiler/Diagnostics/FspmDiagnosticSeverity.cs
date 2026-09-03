namespace Foundry.FSPM.Compiler.Diagnostics;

/// <summary>
/// Diagnostic severity levels (Phase 5 — 施工包 §16).
/// Phase 4 (Parser) emits Errors and Warnings; Info is reserved for Phase 6+ tools.
/// </summary>
public enum FspmDiagnosticSeverity
{
    Info,
    Warning,
    Error
}
