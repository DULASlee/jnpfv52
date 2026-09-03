// =============================================================================
//  Foundry.FSPM.Mcp.Tests — Infrastructure/McpTestServerFactory
// =============================================================================
//
//  MCP-05-02: central factory for spawning the REAL Foundry.FSPM.Mcp stdio
//  server process in tests. Every test class gets its server from here —
//  no test may hand-roll its own ProcessStartInfo.
//
//  The server DLL is resolved from the test assembly's base directory
//  (staged by the StageMcpRuntime MSBuild target). Both stdout and stderr
//  are captured so protocol-cleanliness can be asserted independently.
// =============================================================================

using System.Diagnostics;
using System.Text;

namespace Foundry.FSPM.Mcp.Tests.Infrastructure;

internal static class McpTestServerFactory
{
    public static string ResolveServerDllPath()
    {
        string dllPath = Path.Combine(AppContext.BaseDirectory, "Foundry.FSPM.Mcp.dll");
        if (!File.Exists(dllPath))
        {
            throw new FileNotFoundException(
                $"MCP server DLL not found at: {dllPath}. "
                + "The StageMcpRuntime MSBuild target should have copied it here.");
        }

        return dllPath;
    }

    public static Process Start(
        string serverDllPath,
        StringBuilder stdoutCapture,
        StringBuilder stderrCapture)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add(serverDllPath);

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) stdoutCapture.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) stderrCapture.AppendLine(e.Data);
        };

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("Failed to start the MCP server process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    public static void Stop(Process process, StringBuilder teardownLog)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                if (!process.WaitForExit(2000))
                {
                    teardownLog.AppendLine("[teardown] server process did not exit within 2000ms after Kill.");
                }
            }
        }
        catch (Exception ex)
        {
            teardownLog.AppendLine($"[teardown] stop server failed: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            process.Dispose();
        }
    }
}
