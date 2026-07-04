namespace JNPF.InteAssistant.Codegen;

public sealed class CodeSandboxBuildResult
{
    public bool Success { get; init; }
    public string Phase { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? StandardOutput { get; init; }
    public string? StandardError { get; init; }
    public int ExitCode { get; init; }
    public TimeSpan Elapsed { get; init; }

    public static CodeSandboxBuildResult Pass(string phase, TimeSpan elapsed, string? stdout = null) => new()
    {
        Success = true,
        Phase = phase,
        Elapsed = elapsed,
        StandardOutput = stdout,
    };

    public static CodeSandboxBuildResult Fail(
        string phase,
        string message,
        int exitCode = 1,
        string? stderr = null,
        string? stdout = null,
        TimeSpan elapsed = default) => new()
    {
        Success = false,
        Phase = phase,
        ErrorMessage = message,
        ExitCode = exitCode,
        StandardError = stderr,
        StandardOutput = stdout,
        Elapsed = elapsed,
    };
}
