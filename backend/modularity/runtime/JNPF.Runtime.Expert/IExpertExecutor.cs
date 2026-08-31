using JNPF.Runtime.Core;
using JNPF.Runtime.Core.Observation;
using JNPF.Runtime.Core.SelfRepair;
using RuntimeExecCtx = JNPF.Runtime.Core.ExecutionContext;

namespace JNPF.Runtime.Expert;

/// <summary>
/// Expert 执行结果。
/// </summary>
public sealed class ExpertResult
{
    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 结果状态。
    /// </summary>
    public ExpertTaskStatus Status { get; }

    /// <summary>
    /// 总结消息。
    /// </summary>
    public string Summary { get; }

    /// <summary>
    /// 最终阶段。
    /// </summary>
    public ExpertPhase FinalPhase { get; }

    /// <summary>
    /// 自评估结果。
    /// </summary>
    public SelfEvaluationResult? SelfEvaluation { get; }

    /// <summary>
    /// 测试结果。
    /// </summary>
    public SelfTestResult? TestResult { get; }

    /// <summary>
    /// 产物列表。
    /// </summary>
    public IReadOnlyList<ExpertArtifact> Artifacts { get; }

    /// <summary>
    /// 执行时长。
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// 完成时间（UTC）。
    /// </summary>
    public DateTime CompletedAtUtc { get; }

    private ExpertResult(
        bool isSuccess,
        ExpertTaskStatus status,
        string summary,
        ExpertPhase finalPhase,
        SelfEvaluationResult? selfEvaluation,
        SelfTestResult? testResult,
        IReadOnlyList<ExpertArtifact> artifacts,
        TimeSpan duration,
        DateTime completedAtUtc)
    {
        IsSuccess = isSuccess;
        Status = status;
        Summary = summary;
        FinalPhase = finalPhase;
        SelfEvaluation = selfEvaluation;
        TestResult = testResult;
        Artifacts = artifacts ?? Array.Empty<ExpertArtifact>();
        Duration = duration;
        CompletedAtUtc = completedAtUtc;
    }

    /// <summary>
    /// 创建成功结果。
    /// </summary>
    public static ExpertResult Succeeded(
        string summary,
        ExpertPhase finalPhase,
        SelfEvaluationResult? selfEvaluation,
        SelfTestResult? testResult,
        IReadOnlyList<ExpertArtifact> artifacts,
        TimeSpan duration) =>
        new(true, ExpertTaskStatus.Succeeded, summary, finalPhase, selfEvaluation, testResult, artifacts, duration, DateTime.UtcNow);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    public static ExpertResult Failed(
        string summary,
        ExpertPhase finalPhase,
        IReadOnlyList<ExpertArtifact> artifacts,
        TimeSpan duration) =>
        new(false, ExpertTaskStatus.Failed, summary, finalPhase, null, null, artifacts, duration, DateTime.UtcNow);
}

/// <summary>
/// Expert 执行器接口。
/// 
/// 定义 Expert Agent 的执行契约。
/// IRON-02: Runtime 不懂类级重构业务，业务知识属于 Expert。
/// </summary>
public interface IExpertExecutor
{
    /// <summary>
    /// 获取关联的 Expert。
    /// </summary>
    Expert Expert { get; }

    /// <summary>
    /// 创建执行上下文。
    /// </summary>
    ExpertExecutionContext CreateContext(ClassRefactorTask task, RuntimeExecCtx runtimeContext);

    /// <summary>
    /// 执行任务。
    /// </summary>
    Task<ExpertResult> ExecuteAsync(
        ExpertExecutionContext context,
        IExpertToolSet tools,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证任务结果。
    /// </summary>
    Task<SelfEvaluationResult> ValidateAsync(
        ExpertExecutionContext context,
        IExpertToolSet tools,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Expert 工具集接口。
/// 
/// IRON-03: Expert 不得绕过 Runtime 执行工程操作。
/// 所有工程动作必须通过此接口进入 Runtime 管控。
/// </summary>
public interface IExpertToolSet
{
    /// <summary>
    /// 搜索代码。
    /// </summary>
    Task<IReadOnlyList<CodeSearchResult>> SearchAsync(CodeSearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取文件。
    /// </summary>
    Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入文件。
    /// </summary>
    Task WriteFileAsync(string filePath, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// 差异比较。
    /// </summary>
    Task<FileDiff> DiffAsync(string oldPath, string newPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 构建项目。
    /// </summary>
    Task<BuildResult> BuildAsync(string projectPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 运行测试。
    /// </summary>
    Task<TestResult> TestAsync(string projectPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// 代码搜索查询。
/// </summary>
public sealed class CodeSearchQuery
{
    public string Pattern { get; }
    public string? FilePath { get; }
    public string? ProjectPath { get; }
    public bool IsRegex { get; }
    public bool IgnoreCase { get; }

    public CodeSearchQuery(string pattern, string? filePath = null, string? projectPath = null, bool isRegex = false, bool ignoreCase = true)
    {
        Pattern = pattern;
        FilePath = filePath;
        ProjectPath = projectPath;
        IsRegex = isRegex;
        IgnoreCase = ignoreCase;
    }
}

/// <summary>
/// 代码搜索结果。
/// </summary>
public sealed class CodeSearchResult
{
    public string FilePath { get; }
    public int LineNumber { get; }
    public string LineContent { get; }
    public int MatchStart { get; }
    public int MatchLength { get; }

    public CodeSearchResult(string filePath, int lineNumber, string lineContent, int matchStart, int matchLength)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
        LineContent = lineContent;
        MatchStart = matchStart;
        MatchLength = matchLength;
    }
}

/// <summary>
/// 文件差异。
/// </summary>
public sealed class FileDiff
{
    public string OldPath { get; }
    public string NewPath { get; }
    public IReadOnlyList<DiffChunk> Chunks { get; }
    public int AddedLines { get; }
    public int RemovedLines { get; }

    public FileDiff(string oldPath, string newPath, IReadOnlyList<DiffChunk> chunks)
    {
        OldPath = oldPath;
        NewPath = newPath;
        Chunks = chunks;
        AddedLines = chunks.Sum(c => c.AddedLines);
        RemovedLines = chunks.Sum(c => c.RemovedLines);
    }
}

/// <summary>
/// 差异块。
/// </summary>
public sealed class DiffChunk
{
    public int OldStart { get; }
    public int NewStart { get; }
    public IReadOnlyList<string> Lines { get; }
    public int AddedLines { get; }
    public int RemovedLines { get; }

    public DiffChunk(int oldStart, int newStart, IReadOnlyList<string> lines, int addedLines, int removedLines)
    {
        OldStart = oldStart;
        NewStart = newStart;
        Lines = lines;
        AddedLines = addedLines;
        RemovedLines = removedLines;
    }
}

/// <summary>
/// 构建结果。
/// </summary>
public sealed class BuildResult
{
    public bool Success { get; }
    public int ErrorCount { get; }
    public int WarningCount { get; }
    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> Warnings { get; }
    public TimeSpan Duration { get; }

    public BuildResult(bool success, int errorCount, int warningCount, IReadOnlyList<string> errors, IReadOnlyList<string> warnings, TimeSpan duration)
    {
        Success = success;
        ErrorCount = errorCount;
        WarningCount = warningCount;
        Errors = errors ?? Array.Empty<string>();
        Warnings = warnings ?? Array.Empty<string>();
        Duration = duration;
    }

    public static BuildResult Succeeded(TimeSpan duration) => new(true, 0, 0, Array.Empty<string>(), Array.Empty<string>(), duration);
    public static BuildResult Failed(IReadOnlyList<string> errors, TimeSpan duration) => new(false, errors.Count, 0, errors, Array.Empty<string>(), duration);
}

/// <summary>
/// 测试结果。
/// </summary>
public sealed class TestResult
{
    public bool Success { get; }
    public int TotalTests { get; }
    public int PassedTests { get; }
    public int FailedTests { get; }
    public IReadOnlyList<string> FailedTestNames { get; }
    public TimeSpan Duration { get; }

    public TestResult(bool success, int totalTests, int passedTests, int failedTests, IReadOnlyList<string> failedTestNames, TimeSpan duration)
    {
        Success = success;
        TotalTests = totalTests;
        PassedTests = passedTests;
        FailedTests = failedTests;
        FailedTestNames = failedTestNames ?? Array.Empty<string>();
        Duration = duration;
    }

    public static TestResult Succeeded(int totalTests, TimeSpan duration) => new(true, totalTests, totalTests, 0, Array.Empty<string>(), duration);
    public static TestResult Failed(int totalTests, int failedTests, IReadOnlyList<string> failedTestNames, TimeSpan duration) => new(false, totalTests, totalTests - failedTests, failedTests, failedTestNames, duration);
}
