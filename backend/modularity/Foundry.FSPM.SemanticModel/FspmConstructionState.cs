namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-03: construction lifecycle. Mutable → Validated → Frozen.
/// Only the builder advances states; Frozen nodes reject mutation.
/// </summary>
public enum FspmConstructionState
{
    Mutable,
    Validated,
    Frozen,
}
