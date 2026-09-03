namespace Foundry.FSPM.Compiler.Artifacts;

/// <summary>
/// Artifact source kinds understood by <see cref="ArtifactAcquirer"/>.
/// Phase 11 Closure implements <see cref="File"/> only. Additional kinds
/// (http, s3, …) are added as new <see cref="IArtifactSource"/>
/// implementations without changing the Manifest contract.
/// </summary>
public enum ArtifactSourceKind
{
    Unknown = 0,
    File = 1,
}
