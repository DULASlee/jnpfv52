using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Foundry.FSPM.Compiler.Artifacts;

/// <summary>
/// Closed acquisition pipeline (Phase 11 G11-5c/5d):
/// Manifest → Descriptor → <see cref="IArtifactSource"/> → SHA256 gate →
/// decompress → counts gate → materialize. Any gate failure returns
/// <c>Succeeded: false</c> WITHOUT writing the materialized baseline.
/// </summary>
public static class ArtifactAcquirer
{
    public const string MaterializedFileName = "jnpf-baseline.json";

    private static readonly string[] CountKeys =
        ["projects", "types", "properties", "methods", "namespaces", "symbols"];

    public static async Task<ArtifactAcquisitionResult> AcquireAsync(
        string manifestPath,
        string outputDirectory,
        IArtifactSource? sourceOverride = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifestPath);
        ArgumentNullException.ThrowIfNull(outputDirectory);

        var manifestFull = Path.GetFullPath(manifestPath);
        var manifestDir = Path.GetDirectoryName(manifestFull)!;
        var materializedPath = Path.Combine(Path.GetFullPath(outputDirectory), MaterializedFileName);

        if (!File.Exists(manifestFull))
        {
            return Fail("LoadManifest", "Manifest file not found.", null,
                artifactBefore: false, baselineBefore: File.Exists(materializedPath));
        }

        ArtifactManifest manifest;
        try
        {
            manifest = ArtifactManifest.Parse(await File.ReadAllTextAsync(manifestFull, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is JsonException)
        {
            return Fail("ParseManifest", $"Invalid manifest: {ex.Message}", null,
                artifactBefore: false, baselineBefore: File.Exists(materializedPath));
        }

        var descriptor = manifest.ToDescriptor();
        var baselineBefore = File.Exists(materializedPath);

        IArtifactSource source;
        if (sourceOverride is not null)
        {
            source = sourceOverride;
        }
        else if (descriptor.SourceKind == ArtifactSourceKind.File)
        {
            source = new FileArtifactSource();
        }
        else
        {
            return Fail("ResolveSource",
                $"Unsupported artifact source kind '{manifest.SourceKindRaw}'. No bytes fetched.",
                null, artifactBefore: false, baselineBefore: baselineBefore);
        }

        byte[] artifactBytes;
        try
        {
            artifactBytes = await source.AcquireAsync(descriptor, manifestDir, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is FileNotFoundException || ex is IOException)
        {
            return Fail("Acquire", $"Acquisition failed: {ex.Message}", null,
                artifactBefore: false, baselineBefore: baselineBefore);
        }

        var artifactBefore = true; // bytes were fetched, so the artifact exists
        var actualSha = Convert.ToHexString(SHA256.HashData(artifactBytes));
        if (!actualSha.Equals(descriptor.Sha256Hex.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Fail("VerifySha256",
                $"SHA256 mismatch. Manifest expects {descriptor.Sha256Hex}, got {actualSha}.",
                actualSha, artifactBefore: artifactBefore, baselineBefore: baselineBefore);
        }

        if (!descriptor.Compression.Trim().Equals("gzip", StringComparison.OrdinalIgnoreCase))
        {
            return Fail("Decompress",
                $"Unsupported compression '{descriptor.Compression}'. Only 'gzip' is implemented.",
                actualSha, artifactBefore: artifactBefore, baselineBefore: baselineBefore);
        }

        byte[] baselineBytes;
        try
        {
            using var input = new MemoryStream(artifactBytes, writable: false);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await gzip.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            baselineBytes = output.ToArray();
        }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException)
        {
            return Fail("Decompress", $"Gzip decompression failed: {ex.Message}",
                actualSha, artifactBefore: artifactBefore, baselineBefore: baselineBefore);
        }

        IReadOnlyDictionary<string, long> materializedCounts;
        try
        {
            materializedCounts = ExtractCounts(baselineBytes);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is JsonException)
        {
            return Fail("ExtractCounts", $"Materialized baseline has no valid counts: {ex.Message}",
                actualSha, artifactBefore: artifactBefore, baselineBefore: baselineBefore);
        }

        foreach (var key in CountKeys)
        {
            descriptor.ExpectedCounts.TryGetValue(key, out var expected);
            materializedCounts.TryGetValue(key, out var actual);
            if (expected != actual)
            {
                return Fail("VerifyCounts",
                    $"Counts mismatch on '{key}'. Manifest expects {expected}, materialized has {actual}.",
                    actualSha, artifactBefore: artifactBefore, baselineBefore: baselineBefore);
            }
        }

        Directory.CreateDirectory(Path.GetFullPath(outputDirectory));
        await File.WriteAllBytesAsync(materializedPath, baselineBytes, cancellationToken).ConfigureAwait(false);

        return new ArtifactAcquisitionResult(
            Succeeded: true,
            Stage: "Materialized",
            Reason: "OK",
            ActualSha256Hex: actualSha,
            MaterializedPath: materializedPath,
            ArtifactExistedBefore: artifactBefore,
            BaselineExistedBefore: baselineBefore,
            ArtifactExistsAfter: true,
            BaselineExistsAfter: true,
            MaterializedCounts: materializedCounts);
    }

    private static ArtifactAcquisitionResult Fail(
        string stage, string reason, string? actualSha,
        bool artifactBefore, bool baselineBefore)
    {
        return new ArtifactAcquisitionResult(
            Succeeded: false,
            Stage: stage,
            Reason: reason,
            ActualSha256Hex: actualSha,
            MaterializedPath: null,
            ArtifactExistedBefore: artifactBefore,
            BaselineExistedBefore: baselineBefore,
            ArtifactExistsAfter: artifactBefore,
            BaselineExistsAfter: baselineBefore,
            MaterializedCounts: null);
    }

    private static IReadOnlyDictionary<string, long> ExtractCounts(byte[] baselineJson)
    {
        using var doc = JsonDocument.Parse(baselineJson);
        var root = doc.RootElement;

        JsonElement countsEl;
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("baseline", out var baselineEl) &&
            baselineEl.ValueKind == JsonValueKind.Object &&
            baselineEl.TryGetProperty("counts", out var nested) &&
            nested.ValueKind == JsonValueKind.Object)
        {
            countsEl = nested;
        }
        else if (root.ValueKind == JsonValueKind.Object &&
                 root.TryGetProperty("counts", out var direct) &&
                 direct.ValueKind == JsonValueKind.Object)
        {
            countsEl = direct;
        }
        else
        {
            throw new InvalidOperationException("No 'baseline.counts' (or 'counts') object found.");
        }

        var counts = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var key in CountKeys)
        {
            if (!countsEl.TryGetProperty(key, out var el) || !el.TryGetInt64(out var value))
            {
                throw new InvalidOperationException($"Counts is missing integer field '{key}'.");
            }

            counts[key] = value;
        }

        return counts;
    }
}
