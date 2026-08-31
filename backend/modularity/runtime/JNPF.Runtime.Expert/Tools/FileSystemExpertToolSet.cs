using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using JNPF.Runtime.Core;

namespace JNPF.Runtime.Expert.Tools;

/// <summary>
/// 文件系统工程工具集。
/// 
/// IRON-03: Expert 不得绕过 Runtime 执行工程操作。
/// 所有工程动作必须通过此接口进入 Runtime 管控。
/// </summary>
public sealed class FileSystemExpertToolSet : IExpertToolSet
{
    private readonly string _repositoryRoot;

    public FileSystemExpertToolSet(string repositoryRoot)
    {
        _repositoryRoot = repositoryRoot;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CodeSearchResult>> SearchAsync(CodeSearchQuery query, CancellationToken cancellationToken = default)
    {
        var results = new List<CodeSearchResult>();
        var searchRoot = !string.IsNullOrEmpty(query.ProjectPath) ? query.ProjectPath : _repositoryRoot;
        
        try
        {
            var files = Directory.GetFiles(searchRoot, "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (file.Contains("obj\\") || file.Contains("bin\\") || file.Contains(".git\\"))
                    continue;
                    
                var content = File.ReadAllText(file);
                var lines = content.Split('\n');
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    bool hasMatch;
                    int matchIndex = 0;
                    int matchLength = 0;
                    
                    if (query.IsRegex)
                    {
                        var regexMatch = Regex.Match(line, query.Pattern, query.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                        hasMatch = regexMatch.Success;
                        if (hasMatch)
                        {
                            matchIndex = regexMatch.Index;
                            matchLength = regexMatch.Length;
                        }
                    }
                    else
                    {
                        matchIndex = line.IndexOf(query.Pattern, query.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                        hasMatch = matchIndex >= 0;
                        matchLength = query.Pattern.Length;
                    }
                    
                    if (hasMatch)
                    {
                        results.Add(new CodeSearchResult(
                            file,
                            i + 1,
                            line.Trim(),
                            matchIndex,
                            matchLength));
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Search failed, return empty results
        }
        
        return Task.FromResult<IReadOnlyList<CodeSearchResult>>(results);
    }

    /// <inheritdoc />
    public async Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");
            
        return await File.ReadAllTextAsync(filePath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteFileAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        // Create backup before write
        if (File.Exists(filePath))
        {
            var backupPath = filePath + ".orig";
            if (!File.Exists(backupPath))
            {
                File.Copy(filePath, backupPath, false);
            }
        }
        
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileDiff> DiffAsync(string oldPath, string newPath, CancellationToken cancellationToken = default)
    {
        var chunks = new List<DiffChunk>();
        
        if (!File.Exists(oldPath) || !File.Exists(newPath))
        {
            return Task.FromResult(new FileDiff(oldPath, newPath, chunks));
        }
        
        var oldLines = File.ReadAllLines(oldPath);
        var newLines = File.ReadAllLines(newPath);
        
        // Simple line-by-line diff
        int oldIdx = 0, newIdx = 0;
        var currentChunkLines = new List<string>();
        int chunkOldStart = 0, chunkNewStart = 0;
        int added = 0, removed = 0;
        
        while (oldIdx < oldLines.Length || newIdx < newLines.Length)
        {
            if (oldIdx < oldLines.Length && newIdx < newLines.Length && oldLines[oldIdx] == newLines[newIdx])
            {
                if (added > 0 || removed > 0)
                {
                    chunks.Add(new DiffChunk(chunkOldStart, chunkNewStart, currentChunkLines, added, removed));
                    currentChunkLines = new List<string>();
                    added = 0;
                    removed = 0;
                }
                oldIdx++;
                newIdx++;
            }
            else if (oldIdx < oldLines.Length && (newIdx >= newLines.Length || removed < 2))
            {
                currentChunkLines.Add($"- {oldLines[oldIdx]}");
                if (chunkOldStart == 0) chunkOldStart = oldIdx + 1;
                if (chunkNewStart == 0) chunkNewStart = newIdx + 1;
                removed++;
                oldIdx++;
            }
            else if (newIdx < newLines.Length)
            {
                currentChunkLines.Add($"+ {newLines[newIdx]}");
                if (chunkOldStart == 0) chunkOldStart = oldIdx + 1;
                if (chunkNewStart == 0) chunkNewStart = newIdx + 1;
                added++;
                newIdx++;
            }
        }
        
        if (added > 0 || removed > 0)
        {
            chunks.Add(new DiffChunk(chunkOldStart, chunkNewStart, currentChunkLines, added, removed));
        }
        
        return Task.FromResult(new FileDiff(oldPath, newPath, chunks));
    }

    /// <inheritdoc />
    public async Task<BuildResult> BuildAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{projectPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(psi);
            if (process == null)
            {
                return BuildResult.Failed(new[] { "Failed to start build process" }, stopwatch.Elapsed);
            }
            
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // Parse build output - look for error patterns
            var allOutput = output + error;
            var lines = allOutput.Split('\n');
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Contains("error CS") || trimmedLine.StartsWith("error ", StringComparison.OrdinalIgnoreCase))
                    errors.Add(trimmedLine);
                else if (trimmedLine.Contains("warning CS"))
                    warnings.Add(trimmedLine);
            }
            
            // Build succeeds if exit code is 0 AND no errors
            bool buildSucceeded = process.ExitCode == 0 && errors.Count == 0;
            
            return new BuildResult(
                buildSucceeded,
                errors.Count,
                warnings.Count,
                errors,
                warnings,
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return BuildResult.Failed(new[] { "Build cancelled" }, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            return BuildResult.Failed(new[] { $"Build error: {ex.Message}" }, stopwatch.Elapsed);
        }
    }

    /// <inheritdoc />
    public async Task<TestResult> TestAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Find test project
            var testProjectPath = FindTestProject(projectPath);
            
            if (testProjectPath == null)
            {
                return TestResult.Succeeded(0, stopwatch.Elapsed);
            }
            
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"test \"{testProjectPath}\" --no-build --verbosity quiet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(psi);
            if (process == null)
            {
                return TestResult.Failed(0, 0, new[] { "Failed to start test process" }, stopwatch.Elapsed);
            }
            
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            
            // Parse test output
            var totalMatch = Regex.Match(output, @"Total:\s*(\d+)");
            var passedMatch = Regex.Match(output, @"Passed:\s*(\d+)");
            var failedMatch = Regex.Match(output, @"Failed:\s*(\d+)");
            
            int total = totalMatch.Success ? int.Parse(totalMatch.Groups[1].Value) : 0;
            int passed = passedMatch.Success ? int.Parse(passedMatch.Groups[1].Value) : total;
            int failed = failedMatch.Success ? int.Parse(failedMatch.Groups[1].Value) : 0;
            
            return process.ExitCode == 0 
                ? TestResult.Succeeded(total, stopwatch.Elapsed)
                : TestResult.Failed(total, failed, new[] { "Some tests failed" }, stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return TestResult.Failed(0, 0, new[] { "Test cancelled" }, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            return TestResult.Failed(0, 0, new[] { $"Test error: {ex.Message}" }, stopwatch.Elapsed);
        }
    }

    private string? FindTestProject(string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath);
        if (projectDir == null) return null;
        
        var testDir = Path.Combine(projectDir, "..", "tests");
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        
        // Look for matching test project
        var possiblePaths = new[]
        {
            Path.Combine(testDir, $"{projectName}.Tests", $"{projectName}.Tests.csproj"),
            Path.Combine(testDir, $"JNPF.Tests.{projectName}", $"JNPF.Tests.{projectName}.csproj")
        };
        
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }
        
        return null;
    }
}
