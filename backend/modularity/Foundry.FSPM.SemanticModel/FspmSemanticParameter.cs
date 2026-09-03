namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-01 STEP 6-H/I: pure-data semantic parameter. OwnerId is the owning
/// operation's LogicalId (wired by the binder, never guessed).
/// </summary>
public sealed record FspmSemanticParameter(
    string Name,
    string Type,
    int Position,
    string RefKind,
    bool IsOptional,
    bool HasDefaultValue,
    string? DefaultValue,
    bool IsParams,
    string NullableShape,
    string OwnerId);
