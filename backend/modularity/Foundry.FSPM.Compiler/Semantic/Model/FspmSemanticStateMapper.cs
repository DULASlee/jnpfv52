using Foundry.FSPM.SemanticModel;

namespace Foundry.FSPM.Compiler.Semantic;

/// <summary>
/// P14-01 STEP 6-D: mechanical 1:1 mapping from the P13 eight-state
/// contract to the model-side mirror enum. No semantics invented here —
/// any drift between the two enums is caught by SemanticStateParityTests
/// (name + ordinal + count + full 8-way mapping).
/// </summary>
public static class FspmSemanticStateMapper
{
    public static FspmSemanticState FromResolutionStatus(FspmResolutionStatus status) => status switch
    {
        FspmResolutionStatus.Resolved => FspmSemanticState.Resolved,
        FspmResolutionStatus.NotFound => FspmSemanticState.NotFound,
        FspmResolutionStatus.Ambiguous => FspmSemanticState.Ambiguous,
        FspmResolutionStatus.Invalid => FspmSemanticState.Invalid,
        FspmResolutionStatus.Unsupported => FspmSemanticState.Unsupported,
        FspmResolutionStatus.Degraded => FspmSemanticState.Degraded,
        FspmResolutionStatus.Cancelled => FspmSemanticState.Cancelled,
        FspmResolutionStatus.InfrastructureFailure => FspmSemanticState.InfrastructureFailure,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown resolution status; parity test must cover it."),
    };
}
