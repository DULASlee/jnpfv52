namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-04: machine-readable, source-addressable diagnostic.
/// Code/Severity/Message are stable strings; Reason preserves the full
/// decision chain; Anchor locates the subject when addressable.
/// </summary>
public sealed record FspmDiagnostic(
    string Code,
    string Severity,
    string Message,
    string Reason,
    FspmSemanticAnchor? Anchor);
