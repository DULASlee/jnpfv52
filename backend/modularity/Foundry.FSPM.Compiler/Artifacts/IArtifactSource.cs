namespace Foundry.FSPM.Compiler.Artifacts;

/// <summary>
/// Abstraction over where artifact bytes come from.
/// Phase 11 Closure ships <see cref="FileArtifactSource"/> only; future
/// transports (http, s3, …) implement this interface without touching
/// <see cref="ArtifactAcquirer"/> or the Manifest contract.
/// </summary>
public interface IArtifactSource
{
    ArtifactSourceKind Kind { get; }

    /// <summary>
    /// Fetches the raw (still compressed) artifact bytes described by
    /// <paramref name="descriptor"/>. <paramref name="manifestDirectory"/>
    /// is the directory containing the manifest file, i.e. the root that
    /// artifact-relative paths resolve against.
    /// </summary>
    Task<byte[]> AcquireAsync(
        ArtifactDescriptor descriptor,
        string manifestDirectory,
        CancellationToken cancellationToken = default);
}
