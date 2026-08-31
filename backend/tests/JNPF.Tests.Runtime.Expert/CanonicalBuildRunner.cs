using System.Diagnostics;

namespace JNPF.Tests.Agent;

/// <summary>
/// v5 — Canonical build invocation. Shared command string for baseline
/// capture (Task 0 PowerShell uses the same command literal) and
/// after-refactor Gate A tests.
///
/// Chief Architect B4: baseline.command MUST equal after-build command.
/// The command literal: dotnet build {projectPath} --no-restore --no-incremental
///
/// FileSystemExpertToolSet.BuildAsync uses `dotnet build {path}` WITHOUT
/// the canonical flags, so we MUST NOT use it for Gate A — we use this
/// CanonicalBuildRunner instead.
/// </summary>
public static class CanonicalBuildRunner
{
    public const string NoRestoreFlag = "--no-restore";
    public const string NoIncrementalFlag = "--no-incremental";

    public static string ComposeCommandLine(string projectPath)
        => $"dotnet build \"{projectPath}\" {NoRestoreFlag} {NoIncrementalFlag}";

    public sealed record CanonicalBuildResult(
        bool Success,
        int ExitCode,
        int ErrorCount,
        int WarningCount,
        string StdOut,
        string StdErr,
        TimeSpan Elapsed);

    public static CanonicalBuildResult Run(string projectPath, TimeSpan? timeout = null)
    {
        // Resolve working directory: global.json (if any) governs SDK selection.
        // We pick the parent of the project file so SDK selection walks up correctly.
        var projectDir = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Invalid projectPath: {projectPath}");
        // Walk up to find the nearest global.json (so SDK matches the rest of the repo)
        var workDir = projectDir;
        while (!string.IsNullOrEmpty(workDir) && !File.Exists(Path.Combine(workDir, "global.json")))
        {
            var parent = Path.GetDirectoryName(workDir);
            if (string.IsNullOrEmpty(parent) || parent == workDir) { workDir = projectDir; break; }
            workDir = parent;
        }

        var sw = Stopwatch.StartNew();
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"msbuild \"{projectPath}\" -t:Build -p:Configuration=Debug -p:{NoRestoreFlag.Replace("--", "")}=true -p:{NoIncrementalFlag.Replace("--", "")}=true",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workDir,
        };

        // When hosted by `dotnet test` (which may use SDK 10.0.301), the spawned
        // dotnet can inherit environment that overrides global.json. Clear the
        // SDK-locking env vars so global.json takes effect.
        psi.EnvironmentVariables["DOTNET_CLI_HOME"] = "";
        psi.EnvironmentVariables["DOTNET_HOST_PATH"] = "";
        psi.EnvironmentVariables.Remove("MSBuildSDKsPath");

        // Diagnostic: write the resolved working dir + args to the test output
        // (visible only if the test fails; useful for SDK/environment debugging)
        var diagLine = $"[CanonicalBuildRunner] workDir='{workDir}' args='{psi.Arguments}'";

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet build");

        // Echo diagnostic to stdout (will surface in test output if it fails)
        Console.WriteLine(diagLine);
        Console.WriteLine($"[CanonicalBuildRunner] test process cwd='{Environment.CurrentDirectory}'");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (timeout.HasValue)
        {
            if (!process.WaitForExit((int)timeout.Value.TotalMilliseconds))
            {
                try { process.Kill(); } catch { /* ignore */ }
                throw new TimeoutException($"Build exceeded timeout {timeout.Value.TotalSeconds}s");
            }
        }
        process.WaitForExit();

        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        sw.Stop();

        var combined = stdout + "\n" + stderr;
        var errorCount = System.Text.RegularExpressions.Regex.Matches(combined, @"error CS\d+:").Count;
        var warningCount = System.Text.RegularExpressions.Regex.Matches(combined, @"warning CS\d+:").Count;

        return new CanonicalBuildResult(
            Success: process.ExitCode == 0 && errorCount == 0,
            ExitCode: process.ExitCode,
            ErrorCount: errorCount,
            WarningCount: warningCount,
            StdOut: stdout,
            StdErr: stderr,
            Elapsed: sw.Elapsed);
    }
}