namespace Foundry.FSPM.Compiler.Artifacts;

/// <summary>
/// Auditable outcome of one <see cref="ArtifactAcquirer"/> run.
/// Before/after existence flags are recorded truthfully (never reset by
/// the acquirer): on any failure path no materialized file is written.
/// </summary>
public sealed record ArtifactAcquisitionResult(
    bool Succeeded,
    string Stage,
    string Reason,
    string? ActualSha256Hex,
    string? MaterializedPath,
    bool ArtifactExistedBefore,
    bool BaselineExistedBefore,
    bool ArtifactExistsAfter,
    bool BaselineExistsAfter,
    IReadOnlyDictionary<string, long>? MaterializedCounts);
