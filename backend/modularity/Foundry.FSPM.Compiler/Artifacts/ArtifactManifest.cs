using System.Text.Json;

namespace Foundry.FSPM.Compiler.Artifacts;

/// <summary>
/// Typed view over a jnpf-baseline.manifest.json document (schema v1.1).
/// Parsing never touches the network or the filesystem beyond reading
/// the manifest file itself; acquisition is <see cref="ArtifactAcquirer"/>.
/// </summary>
public sealed class ArtifactManifest
{
    private static readonly string[] RequiredCountKeys =
        ["projects", "types", "properties", "methods", "namespaces", "symbols"];

    public string ManifestVersion { get; }
    public string ArtifactId { get; }
    public string ArtifactName { get; }
    public string ArtifactVersion { get; }
    public string ArtifactFormat { get; }
    public string Compression { get; }
    public string ContentType { get; }
    public string SourceKindRaw { get; }
    public string SourcePathRaw { get; }
    public long ArtifactSizeBytes { get; }
    public string ArtifactSha256 { get; }
    public IReadOnlyDictionary<string, long> Counts { get; }

    private ArtifactManifest(
        string manifestVersion, string artifactId, string artifactName,
        string artifactVersion, string artifactFormat, string compression,
        string contentType, string sourceKindRaw, string sourcePathRaw,
        long artifactSizeBytes, string artifactSha256,
        IReadOnlyDictionary<string, long> counts)
    {
        ManifestVersion = manifestVersion;
        ArtifactId = artifactId;
        ArtifactName = artifactName;
        ArtifactVersion = artifactVersion;
        ArtifactFormat = artifactFormat;
        Compression = compression;
        ContentType = contentType;
        SourceKindRaw = sourceKindRaw;
        SourcePathRaw = sourcePathRaw;
        ArtifactSizeBytes = artifactSizeBytes;
        ArtifactSha256 = artifactSha256;
        Counts = counts;
    }

    public static ArtifactManifest Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string Str(string name)
        {
            if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException($"Manifest is missing required string field '{name}'.");
            }

            return el.GetString()!;
        }

        if (!root.TryGetProperty("source", out var source) || source.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Manifest is missing required object field 'source'.");
        }

        string SourceStr(string name)
        {
            if (!source.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException($"Manifest is missing required string field 'source.{name}'.");
            }

            return el.GetString()!;
        }

        if (!root.TryGetProperty("counts", out var countsEl) || countsEl.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Manifest is missing required object field 'counts'.");
        }

        var counts = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var key in RequiredCountKeys)
        {
            if (!countsEl.TryGetProperty(key, out var el) || !el.TryGetInt64(out var value))
            {
                throw new InvalidOperationException($"Manifest counts is missing required integer field '{key}'.");
            }

            counts[key] = value;
        }

        if (!root.TryGetProperty("artifactSizeBytes", out var sizeEl) || !sizeEl.TryGetInt64(out var sizeBytes))
        {
            throw new InvalidOperationException("Manifest is missing required integer field 'artifactSizeBytes'.");
        }

        return new ArtifactManifest(
            Str("manifestVersion"), Str("artifactId"), Str("artifactName"),
            Str("artifactVersion"), Str("artifactFormat"), Str("compression"),
            Str("contentType"), SourceStr("kind"), SourceStr("path"),
            sizeBytes, Str("artifactSha256"), counts);
    }

    public ArtifactDescriptor ToDescriptor()
    {
        var kind = SourceKindRaw.Trim().ToLowerInvariant() switch
        {
            "file" => ArtifactSourceKind.File,
            _ => ArtifactSourceKind.Unknown,
        };

        return new ArtifactDescriptor(
            ArtifactId, ArtifactName, ArtifactVersion, kind, SourcePathRaw,
            Compression, ContentType, ArtifactSizeBytes, ArtifactSha256, Counts);
    }
}
