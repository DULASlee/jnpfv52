namespace JNPF.Runtime.Core.SelfRepair;

/// <summary>
/// Self Evaluation 结果。
/// 
/// 回答以下问题：
/// - Did I satisfy the task?
/// - Did I preserve contracts?
/// - Did I preserve existing behavior?
/// - Did I remove functionality?
/// - Did I violate architecture?
/// </summary>
public sealed class SelfEvaluationResult
{
    /// <summary>
    /// 任务是否满足。
    /// </summary>
    public bool TaskSatisfied { get; }

    /// <summary>
    /// 合同是否保持。
    /// </summary>
    public bool ContractsPreserved { get; }

    /// <summary>
    /// 现有行为是否保持。
    /// </summary>
    public bool BehaviorPreserved { get; }

    /// <summary>
    /// 功能是否完整（未被删除）。
    /// </summary>
    public bool FunctionalityIntact { get; }

    /// <summary>
    /// 架构是否合规。
    /// </summary>
    public bool ArchitectureCompliant { get; }

    /// <summary>
    /// 是否通过评估。
    /// </summary>
    public bool IsPassed => TaskSatisfied 
        && ContractsPreserved 
        && BehaviorPreserved 
        && FunctionalityIntact 
        && ArchitectureCompliant;

    /// <summary>
    /// 评估详情。
    /// </summary>
    public IReadOnlyList<string> Details { get; }

    /// <summary>
    /// 问题列表。
    /// </summary>
    public IReadOnlyList<string> Issues { get; }

    private SelfEvaluationResult(
        bool taskSatisfied,
        bool contractsPreserved,
        bool behaviorPreserved,
        bool functionalityIntact,
        bool architectureCompliant,
        IReadOnlyList<string> details,
        IReadOnlyList<string> issues)
    {
        TaskSatisfied = taskSatisfied;
        ContractsPreserved = contractsPreserved;
        BehaviorPreserved = behaviorPreserved;
        FunctionalityIntact = functionalityIntact;
        ArchitectureCompliant = architectureCompliant;
        Details = details ?? Array.Empty<string>();
        Issues = issues ?? Array.Empty<string>();
    }

    /// <summary>
    /// 创建评估结果。
    /// </summary>
    public static SelfEvaluationResult Evaluate(
        bool taskSatisfied,
        bool contractsPreserved,
        bool behaviorPreserved,
        bool functionalityIntact,
        bool architectureCompliant,
        params string[] details)
    {
        var issues = new List<string>();
        
        if (!taskSatisfied) issues.Add("Task requirements not fully satisfied");
        if (!contractsPreserved) issues.Add("Contracts have been violated");
        if (!behaviorPreserved) issues.Add("Existing behavior has changed");
        if (!functionalityIntact) issues.Add("Functionality has been removed");
        if (!architectureCompliant) issues.Add("Architecture constraints violated");

        return new SelfEvaluationResult(
            taskSatisfied,
            contractsPreserved,
            behaviorPreserved,
            functionalityIntact,
            architectureCompliant,
            details,
            issues);
    }

    /// <summary>
    /// 通过的评估。
    /// </summary>
    public static SelfEvaluationResult Pass(params string[] details) =>
        Evaluate(true, true, true, true, true, details);

    /// <summary>
    /// 失败的评估。
    /// </summary>
    public static SelfEvaluationResult Fail(params string[] issues) =>
        new(false, false, false, false, false, Array.Empty<string>(), issues);
}

/// <summary>
/// Self Test 结果。
/// </summary>
public sealed class SelfTestResult
{
    /// <summary>
    /// 编译是否通过。
    /// </summary>
    public bool BuildPassed { get; }

    /// <summary>
    /// 单元测试是否通过。
    /// </summary>
    public bool UnitTestsPassed { get; }

    /// <summary>
    /// 集成测试是否通过。
    /// </summary>
    public bool IntegrationTestsPassed { get; }

    /// <summary>
    /// 回归测试是否通过。
    /// </summary>
    public bool RegressionTestsPassed { get; }

    /// <summary>
    /// 测试总数。
    /// </summary>
    public int TotalTests { get; }

    /// <summary>
    /// 通过的测试数。
    /// </summary>
    public int PassedTests { get; }

    /// <summary>
    /// 失败的测试数。
    /// </summary>
    public int FailedTests { get; }

    /// <summary>
    /// 是否通过。
    /// </summary>
    public bool IsPassed => BuildPassed 
        && UnitTestsPassed 
        && IntegrationTestsPassed 
        && RegressionTestsPassed;

    /// <summary>
    /// 失败的测试名称。
    /// </summary>
    public IReadOnlyList<string> FailedTestNames { get; }

    private SelfTestResult(
        bool buildPassed,
        bool unitTestsPassed,
        bool integrationTestsPassed,
        bool regressionTestsPassed,
        int totalTests,
        int passedTests,
        int failedTests,
        IReadOnlyList<string> failedTestNames)
    {
        BuildPassed = buildPassed;
        UnitTestsPassed = unitTestsPassed;
        IntegrationTestsPassed = integrationTestsPassed;
        RegressionTestsPassed = regressionTestsPassed;
        TotalTests = totalTests;
        PassedTests = passedTests;
        FailedTests = failedTests;
        FailedTestNames = failedTestNames ?? Array.Empty<string>();
    }

    /// <summary>
    /// 创建测试结果。
    /// </summary>
    public static SelfTestResult Create(
        bool buildPassed,
        bool unitTestsPassed = true,
        bool integrationTestsPassed = true,
        bool regressionTestsPassed = true,
        int totalTests = 0,
        int passedTests = 0,
        int failedTests = 0,
        params string[] failedTestNames) =>
        new(
            buildPassed,
            unitTestsPassed,
            integrationTestsPassed,
            regressionTestsPassed,
            totalTests,
            passedTests,
            failedTests,
            failedTestNames);

    /// <summary>
    /// 编译失败。
    /// </summary>
    public static SelfTestResult BuildFailed(string[] errors) =>
        new(false, false, false, false, 0, 0, errors.Length, errors);
}

/// <summary>
/// 修复结果。
/// </summary>
public sealed class RepairResult
{
    /// <summary>
    /// 修复是否成功。
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// 修复说明。
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 修复的文件。
    /// </summary>
    public IReadOnlyList<string> ModifiedFiles { get; }

    /// <summary>
    /// 修复后的验证结果。
    /// </summary>
    public SelfTestResult? VerificationResult { get; }

    private RepairResult(
        bool success,
        string description,
        IReadOnlyList<string> modifiedFiles,
        SelfTestResult? verificationResult)
    {
        Success = success;
        Description = description;
        ModifiedFiles = modifiedFiles ?? Array.Empty<string>();
        VerificationResult = verificationResult;
    }

    /// <summary>
    /// 成功修复。
    /// </summary>
    public static RepairResult Succeeded(string description, params string[] modifiedFiles) =>
        new(true, description, modifiedFiles, null);

    /// <summary>
    /// 成功修复并验证。
    /// </summary>
    public static RepairResult SucceededWithVerification(
        string description, 
        SelfTestResult verification,
        params string[] modifiedFiles) =>
        new(true, description, modifiedFiles, verification);

    /// <summary>
    /// 修复失败。
    /// </summary>
    public static RepairResult Failed(string description) =>
        new(false, description, Array.Empty<string>(), null);
}
