namespace Foundry.FSPM.Compiler.Artifacts;

/// <summary>
/// Resolved, validated view of one manifest's artifact.
/// Produced by <see cref="ArtifactManifest.ToDescriptor"/>; consumed by
/// <see cref="IArtifactSource"/> and <see cref="ArtifactAcquirer"/>.
/// <see cref="RelativePath"/> is always artifact-relative (never rooted),
/// so manifests stay portable across machines.
/// </summary>
public sealed record ArtifactDescriptor(
    string ArtifactId,
    string ArtifactName,
    string ArtifactVersion,
    ArtifactSourceKind SourceKind,
    string RelativePath,
    string Compression,
    string ContentType,
    long SizeBytes,
    string Sha256Hex,
    IReadOnlyDictionary<string, long> ExpectedCounts);
