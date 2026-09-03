namespace Foundry.FSPM.Compiler.Artifacts;

/// <summary>
/// <see cref="IArtifactSource"/> for <c>kind: "file"</c> manifests.
/// Resolves the artifact-relative <c>source.path</c> against the manifest
/// directory. Absolute paths and <c>..</c> escapes are rejected so a
/// manifest can never pull bytes from outside its own artifact root.
/// </summary>
public sealed class FileArtifactSource : IArtifactSource
{
    public ArtifactSourceKind Kind => ArtifactSourceKind.File;

    public Task<byte[]> AcquireAsync(
        ArtifactDescriptor descriptor,
        string manifestDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(manifestDirectory);

        var relative = (descriptor.RelativePath ?? string.Empty).Trim();
        if (relative.Length == 0)
        {
            throw new InvalidOperationException("Artifact source.path is empty.");
        }

        if (Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException(
                $"Artifact source.path must be artifact-relative, got rooted path '{relative}'.");
        }

        var full = Path.GetFullPath(Path.Combine(manifestDirectory, relative));
        var root = Path.GetFullPath(manifestDirectory);
        if (!full.Equals(root, StringComparison.OrdinalIgnoreCase) &&
            !full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Artifact source.path escapes the artifact root: '{relative}'.");
        }

        if (!File.Exists(full))
        {
            throw new FileNotFoundException($"Artifact file not found: {full}", full);
        }

        return File.ReadAllBytesAsync(full, cancellationToken);
    }
}
