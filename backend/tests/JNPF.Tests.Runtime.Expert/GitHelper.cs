using System.Diagnostics;
using System.Text.Json;

namespace JNPF.Tests.Agent;

/// <summary>
/// Helper to read file/blob identity from specific git commits.
///
/// CRITICAL: All "pre-refactor" reads MUST use PRE_REFACTOR_COMMIT from baseline.json,
/// NOT "HEAD" — HEAD moves as commits are added during test execution.
///
/// v5 (Chief Architect B1): provides GetBlobSha for git object-database identity,
/// independent of any text encoding pipeline (BOM, newline, Out-File).
/// </summary>
public static class GitHelper
{
    /// <summary>
    /// Read a file from a specific git commit using `git show`.
    /// Caller is responsible for passing PRE_REFACTOR_COMMIT.
    /// </summary>
    public static string GetFileFromCommit(string commitSha, string repoRelativePath, string repoRoot = @"D:\JNPF-v52")
    {
        if (string.IsNullOrEmpty(commitSha))
            throw new ArgumentException("commitSha must not be empty", nameof(commitSha));
        if (string.IsNullOrEmpty(repoRelativePath))
            throw new ArgumentException("repoRelativePath must not be empty", nameof(repoRelativePath));

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"show {commitSha}:{repoRelativePath}",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("git process failed to start");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git show failed for commit {commitSha}: {error}");

        return output;
    }

    /// <summary>
    /// [B1 v5] Get the BLOB SHA (git object-database identity) for a path at a commit.
    /// `git rev-parse &lt;commit&gt;:&lt;path&gt;` returns the SHA1 of the blob object.
    /// Independent of any text encoding pipeline.
    /// </summary>
    public static string GetBlobSha(string commitSha, string repoRelativePath, string repoRoot = @"D:\JNPF-v52")
    {
        if (string.IsNullOrEmpty(commitSha))
            throw new ArgumentException("commitSha must not be empty", nameof(commitSha));

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"rev-parse {commitSha}:{repoRelativePath}",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("git process failed to start");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git rev-parse failed for commit {commitSha}:{repoRelativePath} — {error}");

        return output.Trim();
    }

    /// <summary>
    /// Load PRE_REFACTOR_COMMIT from baseline.json.
    /// This is the immutable frozen commit for all historical reads.
    /// </summary>
    public static string GetPreRefactorCommit(string baselineJsonPath)
    {
        if (!File.Exists(baselineJsonPath))
            throw new FileNotFoundException($"baseline.json not found at {baselineJsonPath}");
        var doc = JsonDocument.Parse(File.ReadAllText(baselineJsonPath));
        return doc.RootElement.GetProperty("preRefactorCommit").GetString()
            ?? throw new InvalidOperationException("baseline.json missing preRefactorCommit");
    }
}