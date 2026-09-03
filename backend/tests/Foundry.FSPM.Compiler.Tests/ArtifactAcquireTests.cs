using Foundry.FSPM.Compiler.Artifacts;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Foundry.FSPM.Compiler.Tests;

// Phase 11 Closure G11-5c/5d: acquisition pipeline gates.
// Hermetic tiny fixtures only; the 260MB real baseline is exercised
// by the G11-5d clean-directory proof (NOW-06), not by unit tests.
public sealed class ArtifactAcquireTests
{
    private const string CountsJson =
        """{"baseline":{"counts":{"projects":2,"types":10,"properties":20,"methods":30,"namespaces":3,"symbols":60}}}""";

    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fspm_acq_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static byte[] Gzip(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }

        return output.ToArray();
    }

    private static string WriteManifest(
        string dir, string fileName, string shaHex, string sourceKind = "file", string? sourcePath = null)
    {
        var doc = new
        {
            manifestVersion = "1.1",
            schemaVersion = "1.0",
            artifactId = "test-baseline",
            artifactName = fileName,
            artifactVersion = "1",
            artifactFormat = "jsonl.gz",
            compression = "gzip",
            contentType = "application/gzip+jsonl",
            artifactStorage = "local",
            source = new { kind = sourceKind, path = sourcePath ?? fileName },
            artifactSizeBytes = 1L,
            artifactSha256 = shaHex,
            repositoryCommit = "test",
            compilerCommit = "test",
            baselineGenerator = "test",
            generatedUtc = "2026-09-03T00:00:00Z",
            counts = new { projects = 2, types = 10, properties = 20, methods = 30, namespaces = 3, symbols = 60 },
        };
        var path = Path.Combine(dir, "test.manifest.json");
        File.WriteAllText(path, JsonSerializer.Serialize(doc));
        return path;
    }

    private static string ShaHex(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    [Fact]
    public async Task Acquire_Positive_MaterializesWithMatchingCounts()
    {
        var manifestDir = NewTempDir();
        var outDir = NewTempDir();
        try
        {
            var artifactBytes = Gzip(CountsJson);
            await File.WriteAllBytesAsync(Path.Combine(manifestDir, "t.jsonl.gz"), artifactBytes);
            var manifestPath = WriteManifest(manifestDir, "t.jsonl.gz", ShaHex(artifactBytes));

            var result = await ArtifactAcquirer.AcquireAsync(manifestPath, outDir);

            Assert.True(result.Succeeded);
            Assert.Equal("Materialized", result.Stage);
            Assert.False(result.BaselineExistedBefore);
            Assert.True(result.BaselineExistsAfter);
            Assert.NotNull(result.MaterializedPath);
            Assert.True(File.Exists(result.MaterializedPath));
            Assert.Equal(2, result.MaterializedCounts!["projects"]);
            Assert.Equal(60, result.MaterializedCounts!["symbols"]);
        }
        finally
        {
            Directory.Delete(manifestDir, recursive: true);
            Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task Acquire_TamperedByte_FailsWithoutMaterialization()
    {
        var manifestDir = NewTempDir();
        var outDir = NewTempDir();
        try
        {
            var artifactBytes = Gzip(CountsJson);
            var expectedSha = ShaHex(artifactBytes);
            artifactBytes[artifactBytes.Length / 2] ^= 0xFF; // Negative 1: 1-byte tamper
            var artifactPath = Path.Combine(manifestDir, "t.jsonl.gz");
            await File.WriteAllBytesAsync(artifactPath, artifactBytes);
            var manifestPath = WriteManifest(manifestDir, "t.jsonl.gz", expectedSha);

            var result = await ArtifactAcquirer.AcquireAsync(manifestPath, outDir);

            Assert.False(result.Succeeded);
            Assert.Equal("VerifySha256", result.Stage);
            Assert.Null(result.MaterializedPath);
            Assert.False(File.Exists(Path.Combine(outDir, ArtifactAcquirer.MaterializedFileName)));
            Assert.False(result.BaselineExistsAfter);
        }
        finally
        {
            Directory.Delete(manifestDir, recursive: true);
            Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task Acquire_WrongManifestSha_FailsWithoutMaterialization()
    {
        var manifestDir = NewTempDir();
        var outDir = NewTempDir();
        try
        {
            var artifactBytes = Gzip(CountsJson);
            await File.WriteAllBytesAsync(Path.Combine(manifestDir, "t.jsonl.gz"), artifactBytes);
            // Negative 2: manifest carries a sha that does not match the bytes.
            var manifestPath = WriteManifest(manifestDir, "t.jsonl.gz", new string('0', 64));

            var result = await ArtifactAcquirer.AcquireAsync(manifestPath, outDir);

            Assert.False(result.Succeeded);
            Assert.Equal("VerifySha256", result.Stage);
            Assert.Null(result.MaterializedPath);
            Assert.False(File.Exists(Path.Combine(outDir, ArtifactAcquirer.MaterializedFileName)));
        }
        finally
        {
            Directory.Delete(manifestDir, recursive: true);
            Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task Acquire_AbsoluteSourcePath_FailsAsNonPortable()
    {
        var manifestDir = NewTempDir();
        var outDir = NewTempDir();
        try
        {
            var artifactBytes = Gzip(CountsJson);
            var absPath = Path.Combine(manifestDir, "t.jsonl.gz");
            await File.WriteAllBytesAsync(absPath, artifactBytes);
            var manifestPath = WriteManifest(manifestDir, "t.jsonl.gz", ShaHex(artifactBytes), sourcePath: absPath);

            var result = await ArtifactAcquirer.AcquireAsync(manifestPath, outDir);

            Assert.False(result.Succeeded);
            Assert.Equal("Acquire", result.Stage);
            Assert.False(File.Exists(Path.Combine(outDir, ArtifactAcquirer.MaterializedFileName)));
        }
        finally
        {
            Directory.Delete(manifestDir, recursive: true);
            Directory.Delete(outDir, recursive: true);
        }
    }

    [Fact]
    public async Task Acquire_UnsupportedSourceKind_FailsWithoutFetching()
    {
        var manifestDir = NewTempDir();
        var outDir = NewTempDir();
        try
        {
            var artifactBytes = Gzip(CountsJson);
            var manifestPath = WriteManifest(manifestDir, "t.jsonl.gz", ShaHex(artifactBytes), sourceKind: "http");

            var result = await ArtifactAcquirer.AcquireAsync(manifestPath, outDir);

            Assert.False(result.Succeeded);
            Assert.Equal("ResolveSource", result.Stage);
            Assert.False(Directory.EnumerateFiles(outDir).Any());
        }
        finally
        {
            Directory.Delete(manifestDir, recursive: true);
            Directory.Delete(outDir, recursive: true);
        }
    }
}
