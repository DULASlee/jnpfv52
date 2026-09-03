namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P13-H6: stable semantic anchor (chief §十二 revised definition).
/// <list type="bullet">
/// <item>Logical: which semantic node (survives edits).</item>
/// <item>DocumentIdentity: which document (file path).</item>
/// <item>DeclarationAnchor: declaration-site identity (DocId).</item>
/// <item>CurrentSpan: TextSpan + LineSpan for THIS snapshot only.</item>
/// </list>
/// LineSpan is positioning, never identity.
/// </summary>
public sealed record SemanticSourceAnchor(
    LogicalSemanticIdentity Logical,
    string DocumentIdentity,
    string DeclarationAnchor,
    FspmSourceLocation CurrentSpan);

/// <summary>
/// P13-H6: machine-traceable evidence pairing an anchor with the
/// compilation snapshot it was observed in.
/// </summary>
public sealed record SemanticEvidence(
    SemanticSourceAnchor Anchor,
    string SnapshotId,
    string CompilationAssembly);
