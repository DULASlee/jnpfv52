namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-04: one generic rule. Targets are LogicalIds; Expected carries
/// kind-specific expectation (expected type display for TypeCompatible,
/// expected return display for OperationCompatible, empty otherwise).
/// Per-kind parameter contracts (documented, test-pinned):
/// Required/Forbidden/Allowed: Targets[0] is the subject.
/// ExactlyOne/AtLeastOne: Targets is the candidate set.
/// TypeCompatible: Targets[0] is a member LogicalId, Expected is the type display.
/// OperationCompatible: Targets[0] is an operation LogicalId, Expected is the return display.
/// </summary>
public sealed record FspmRule(
    string Id,
    FspmRuleKind Kind,
    IReadOnlyList<string> Targets,
    string Expected,
    string Description);
