using JNPF.Runtime.Core;

namespace JNPF.Runtime.Expert;

/// <summary>
/// 类级重构任务输入。
/// 
/// Workstream B: Expert Task Contract
/// </summary>
public sealed class ClassRefactorTask
{
    /// <summary>
    /// 任务唯一标识。
    /// </summary>
    public Guid TaskId { get; }

    /// <summary>
    /// 关联的 Runtime 会话。
    /// </summary>
    public Guid SessionId { get; }

    /// <summary>
    /// 目标类完整路径。
    /// </summary>
    public string TargetClassPath { get; }

    /// <summary>
    /// 目标类名称（不含命名空间）。
    /// </summary>
    public string TargetClassName { get; }

    /// <summary>
    /// 目标项目路径。
    /// </summary>
    public string TargetProjectPath { get; }

    /// <summary>
    /// 仓储根目录。
    /// </summary>
    public string RepositoryRoot { get; }

    /// <summary>
    /// 重构目标。
    /// </summary>
    public string RefactorObjective { get; }

    /// <summary>
    /// 约束条件。
    /// </summary>
    public TaskConstraints Constraints { get; }

    /// <summary>
    /// 必须保留的合同规则。
    /// </summary>
    public PreservationContract Preservation { get; }

    /// <summary>
    /// 验证要求。
    /// </summary>
    public ValidationRequirements Validation { get; }

    /// <summary>
    /// 允许使用的工具。
    /// </summary>
    public IReadOnlyList<string> AllowedTools { get; }

    /// <summary>
    /// 创建时间（UTC）。
    /// </summary>
    public DateTime CreatedAtUtc { get; }

    /// <summary>
    /// 任务创建者。
    /// </summary>
    public string Creator { get; }

    private ClassRefactorTask(
        Guid taskId,
        Guid sessionId,
        string targetClassPath,
        string targetClassName,
        string targetProjectPath,
        string repositoryRoot,
        string refactorObjective,
        TaskConstraints constraints,
        PreservationContract preservation,
        ValidationRequirements validation,
        IReadOnlyList<string> allowedTools,
        DateTime createdAtUtc,
        string creator)
    {
        TaskId = taskId;
        SessionId = sessionId;
        TargetClassPath = targetClassPath;
        TargetClassName = targetClassName;
        TargetProjectPath = targetProjectPath;
        RepositoryRoot = repositoryRoot;
        RefactorObjective = refactorObjective;
        Constraints = constraints;
        Preservation = preservation;
        Validation = validation;
        AllowedTools = allowedTools ?? Array.Empty<string>();
        CreatedAtUtc = createdAtUtc;
        Creator = creator;
    }

    /// <summary>
    /// 创建重构任务。
    /// </summary>
    public static ClassRefactorTask Create(
        Guid sessionId,
        string targetClassPath,
        string targetProjectPath,
        string repositoryRoot,
        string refactorObjective,
        string creator = "system")
    {
        return new ClassRefactorTask(
            Guid.NewGuid(),
            sessionId,
            targetClassPath,
            Path.GetFileNameWithoutExtension(targetClassPath),
            targetProjectPath,
            repositoryRoot,
            refactorObjective,
            TaskConstraints.Default,
            PreservationContract.Default,
            ValidationRequirements.Default,
            new[] { "Search", "Read", "Write", "Build", "Test" },
            DateTime.UtcNow,
            creator);
    }
}

/// <summary>
/// 任务约束条件。
/// </summary>
public sealed class TaskConstraints
{
    /// <summary>
    /// 最大执行时间（分钟）。
    /// </summary>
    public int MaxExecutionMinutes { get; }

    /// <summary>
    /// 最大重试次数。
    /// </summary>
    public int MaxRetryCount { get; }

    /// <summary>
    /// 是否允许破坏性更改。
    /// </summary>
    public bool AllowDestructiveChanges { get; }

    /// <summary>
    /// 必须保留的命名空间。
    /// </summary>
    public IReadOnlyList<string> PreserveNamespaces { get; }

    public TaskConstraints(int maxExecutionMinutes, int maxRetryCount, bool allowDestructiveChanges, IReadOnlyList<string> preserveNamespaces)
    {
        MaxExecutionMinutes = maxExecutionMinutes;
        MaxRetryCount = maxRetryCount;
        AllowDestructiveChanges = allowDestructiveChanges;
        PreserveNamespaces = preserveNamespaces ?? Array.Empty<string>();
    }

    /// <summary>
    /// 默认约束。
    /// </summary>
    public static TaskConstraints Default => new(30, 3, false, Array.Empty<string>());
}

/// <summary>
/// 必须保留的合同规则（防止 Agent "重构成功但业务死掉"）。
/// 
/// 这是防止 IRON-04 违规的核心。
/// </summary>
public sealed class PreservationContract
{
    /// <summary>
    /// 是否保留 Public API。
    /// </summary>
    public bool PreservePublicApi { get; }

    /// <summary>
    /// 是否保留行为。
    /// </summary>
    public bool PreserveBehavior { get; }

    /// <summary>
    /// 是否保留授权逻辑。
    /// </summary>
    public bool PreserveAuthorization { get; }

    /// <summary>
    /// 是否保留事务逻辑。
    /// </summary>
    public bool PreserveTransaction { get; }

    /// <summary>
    /// 是否保留异常语义。
    /// </summary>
    public bool PreserveExceptionSemantics { get; }

    /// <summary>
    /// 是否保留租户逻辑。
    /// </summary>
    public bool PreserveTenantSemantics { get; }

    /// <summary>
    /// 是否保留数据访问逻辑。
    /// </summary>
    public bool PreserveDataAccess { get; }

    /// <summary>
    /// 是否保留并发逻辑。
    /// </summary>
    public bool PreserveConcurrency { get; }

    public PreservationContract(
        bool preservePublicApi,
        bool preserveBehavior,
        bool preserveAuthorization,
        bool preserveTransaction,
        bool preserveExceptionSemantics,
        bool preserveTenantSemantics,
        bool preserveDataAccess,
        bool preserveConcurrency)
    {
        PreservePublicApi = preservePublicApi;
        PreserveBehavior = preserveBehavior;
        PreserveAuthorization = preserveAuthorization;
        PreserveTransaction = preserveTransaction;
        PreserveExceptionSemantics = preserveExceptionSemantics;
        PreserveTenantSemantics = preserveTenantSemantics;
        PreserveDataAccess = preserveDataAccess;
        PreserveConcurrency = preserveConcurrency;
    }

    /// <summary>
    /// 默认合同。
    /// </summary>
    public static PreservationContract Default => new(true, true, true, true, true, true, true, true);
}

/// <summary>
/// 验证要求。
/// </summary>
public sealed class ValidationRequirements
{
    /// <summary>
    /// 是否必须编译通过。
    /// </summary>
    public bool RequireBuildPass { get; }

    /// <summary>
    /// 是否必须所有测试通过。
    /// </summary>
    public bool RequireAllTestsPass { get; }

    /// <summary>
    /// 是否必须保留测试覆盖。
    /// </summary>
    public bool RequireTestCoverage { get; }

    /// <summary>
    /// 是否必须通过代码质量检查。
    /// </summary>
    public bool RequireCodeQuality { get; }

    public ValidationRequirements(bool requireBuildPass, bool requireAllTestsPass, bool requireTestCoverage, bool requireCodeQuality)
    {
        RequireBuildPass = requireBuildPass;
        RequireAllTestsPass = requireAllTestsPass;
        RequireTestCoverage = requireTestCoverage;
        RequireCodeQuality = requireCodeQuality;
    }

    /// <summary>
    /// 默认验证要求。
    /// </summary>
    public static ValidationRequirements Default => new(true, true, true, true);
}
