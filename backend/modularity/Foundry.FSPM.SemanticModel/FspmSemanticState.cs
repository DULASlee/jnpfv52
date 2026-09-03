namespace Foundry.FSPM.SemanticModel;

/// <summary>
/// P14-01: model-side resolution state. Intentionally mirrors the P13
/// eight-state contract member-for-member (names AND ordinals):
/// Resolved=0, NotFound=1, Ambiguous=2, Invalid=3, Unsupported=4,
/// Degraded=5, Cancelled=6, InfrastructureFailure=7.
/// A parity test (SemanticStateParityTests) fails the build if the two
/// enums ever drift. This is the same protocol in a Roslyn-free assembly,
/// not a second semantic.
/// </summary>
public enum FspmSemanticState
{
    Resolved = 0,
    NotFound = 1,
    Ambiguous = 2,
    Invalid = 3,
    Unsupported = 4,
    Degraded = 5,
    Cancelled = 6,
    InfrastructureFailure = 7,
}
