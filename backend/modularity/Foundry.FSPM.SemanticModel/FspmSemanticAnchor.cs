namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-01: source anchor ("currently explainable source location").
/// LineSpan is positioning, never identity. Plain data only.
/// </summary>
public sealed record FspmSemanticAnchor(
    string Document,
    string DeclarationAnchor,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn);
