namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-01 STEP 6-F: pure-data semantic type. Every field is a string,
/// string list, int, or another pure record — no Roslyn, no Compiler.
/// Projected from NativeSemanticFact by FspmModelBinder (Compiler side).
/// </summary>
public sealed record FspmSemanticType(
    FspmSemanticIdentity Identity,
    string Name,
    string Namespace,
    string Kind,
    int GenericArity,
    string? NullableShape,
    string? BaseType,
    IReadOnlyList<string> Interfaces,
    string Fingerprint,
    FspmSemanticAnchor Anchor,
    FspmSemanticState State);
