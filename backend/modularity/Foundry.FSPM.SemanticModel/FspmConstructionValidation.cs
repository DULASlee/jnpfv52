namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-03: construction validation verdict. IsValid requires every
/// binding valid; Issues carries one human-readable line per failure
/// (binding Reasons are preserved on the bindings themselves — nothing
/// is flattened away).
/// </summary>
public sealed record FspmConstructionValidation(
    bool IsValid,
    IReadOnlyList<FspmReferenceBinding> Bindings,
    IReadOnlyList<string> Issues);
