using JNPF.Runtime.Core;
using JNPF.Runtime.Core.SelfRepair;
using RuntimeExecCtx = JNPF.Runtime.Core.ExecutionContext;

namespace JNPF.Runtime.Expert;

/// <summary>
/// Expert 执行上下文。
/// 
/// 将 Expert 与 Runtime 连接起来。
/// </summary>
public sealed class ExpertExecutionContext
{
    /// <summary>
    /// Expert 标识。
    /// </summary>
    public Expert Expert { get; }

    /// <summary>
    /// 当前任务。
    /// </summary>
    public ClassRefactorTask Task { get; }

    /// <summary>
    /// 关联的 Runtime ExecutionContext。
    /// </summary>
    public RuntimeExecCtx RuntimeContext { get; }

    /// <summary>
    /// 当前阶段。
    /// </summary>
    public ExpertPhase Phase { get; private set; }

    /// <summary>
    /// 当前阶段开始时间。
    /// </summary>
    public DateTime PhaseStartedAtUtc { get; private set; }

    /// <summary>
    /// 任务状态。
    /// </summary>
    public ExpertTaskStatus Status { get; private set; }

    /// <summary>
    /// 产物列表。
    /// </summary>
    public IReadOnlyList<ExpertArtifact> Artifacts { get; private set; }

    /// <summary>
    /// 当前阶段消息。
    /// </summary>
    public string? CurrentMessage { get; private set; }

    /// <summary>
    /// 重试计数。
    /// </summary>
    public int RetryCount { get; private set; }

    private ExpertExecutionContext(
        Expert expert,
        ClassRefactorTask task,
        RuntimeExecCtx runtimeContext)
    {
        Expert = expert;
        Task = task;
        RuntimeContext = runtimeContext;
        Phase = ExpertPhase.Created;
        PhaseStartedAtUtc = DateTime.UtcNow;
        Status = ExpertTaskStatus.Pending;
        Artifacts = Array.Empty<ExpertArtifact>();
        RetryCount = 0;
    }

    /// <summary>
    /// 创建执行上下文。
    /// </summary>
    public static ExpertExecutionContext Create(Expert expert, ClassRefactorTask task, RuntimeExecCtx runtimeContext)
    {
        ArgumentNullException.ThrowIfNull(expert);
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(runtimeContext);
        
        return new ExpertExecutionContext(expert, task, runtimeContext);
    }

    /// <summary>
    /// 进入新阶段。
    /// </summary>
    public void TransitionTo(ExpertPhase newPhase, string? message = null)
    {
        Phase = newPhase;
        PhaseStartedAtUtc = DateTime.UtcNow;
        CurrentMessage = message;
        
        if (newPhase == ExpertPhase.Completed)
            Status = ExpertTaskStatus.Succeeded;
        else if (newPhase == ExpertPhase.Failed)
            Status = ExpertTaskStatus.Failed;
        else if (newPhase == ExpertPhase.Cancelled)
            Status = ExpertTaskStatus.Cancelled;
        else if (newPhase == ExpertPhase.Rejected)
            Status = ExpertTaskStatus.Rejected;
        else if (Status == ExpertTaskStatus.Pending)
            Status = ExpertTaskStatus.Running;
    }

    /// <summary>
    /// 添加产物。
    /// </summary>
    public void AddArtifact(ExpertArtifact artifact)
    {
        var list = new List<ExpertArtifact>(Artifacts) { artifact };
        Artifacts = list;
    }

    /// <summary>
    /// 增加重试计数。
    /// </summary>
    public bool IncrementRetry()
    {
        RetryCount++;
        return RetryCount <= Task.Constraints.MaxRetryCount;
    }

    /// <summary>
    /// 检查是否可继续。
    /// </summary>
    public bool CanContinue => Status == ExpertTaskStatus.Running || Status == ExpertTaskStatus.Pending;

    /// <summary>
    /// 获取阶段时长。
    /// </summary>
    public TimeSpan PhaseDuration => DateTime.UtcNow - PhaseStartedAtUtc;
}

/// <summary>
/// Expert 执行产物。
/// </summary>
public sealed class ExpertArtifact
{
    /// <summary>
    /// 产物类型。
    /// </summary>
    public ExpertArtifactType Type { get; }

    /// <summary>
    /// 产物名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 产物描述。
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 相关文件路径。
    /// </summary>
    public IReadOnlyList<string> FilePaths { get; }

    /// <summary>
    /// 产物内容（JSON 序列化）。
    /// </summary>
    public string? Content { get; }

    /// <summary>
    /// 创建时间（UTC）。
    /// </summary>
    public DateTime CreatedAtUtc { get; }

    public ExpertArtifact(
        ExpertArtifactType type,
        string name,
        string description,
        IReadOnlyList<string> filePaths,
        string? content)
    {
        Type = type;
        Name = name;
        Description = description;
        FilePaths = filePaths ?? Array.Empty<string>();
        Content = content;
        CreatedAtUtc = DateTime.UtcNow;
    }
}

/// <summary>
/// 产物类型。
/// </summary>
public enum ExpertArtifactType
{
    /// <summary>
    /// 发现报告。
    /// </summary>
    DiscoveryReport = 0,

    /// <summary>
    /// 合同基线。
    /// </summary>
    ContractBaseline = 1,

    /// <summary>
    /// 重构计划。
    /// </summary>
    RefactorPlan = 2,

    /// <summary>
    /// 代码差异。
    /// </summary>
    CodeDiff = 3,

    /// <summary>
    /// 构建结果。
    /// </summary>
    BuildResult = 4,

    /// <summary>
    /// 测试结果。
    /// </summary>
    TestResult = 5,

    /// <summary>
    /// 验证报告。
    /// </summary>
    ValidationReport = 6,

    /// <summary>
    /// 修复记录。
    /// </summary>
    RepairRecord = 7,

    /// <summary>
    /// 审查报告。
    /// </summary>
    ReviewReport = 8
}
