namespace Foundry.FSPM.Compiler.Diagnostics;

/// <summary>
/// Immutable diagnostic record (Phase 5 — 施工包 §16).
/// Carries Source Position so future AI / IDE can navigate to the exact
/// source slice that caused the problem.
/// Phase 12 adds optional Start/Length source span. All existing
/// 5-argument constructions keep compiling (defaults 0).
/// </summary>
public sealed record FspmDiagnostic(
    string Code,
    FspmDiagnosticSeverity Severity,
    string Message,
    int Line,
    int Column,
    int Start = 0,
    int Length = 0);
