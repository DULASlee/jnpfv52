namespace JNPF.Runtime.Expert;

/// <summary>
/// Expert Agent 类型枚举。
/// </summary>
public enum ExpertType
{
    /// <summary>
    /// 未知类型。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 类级重构专家。
    /// </summary>
    ClassRefactor = 1,

    /// <summary>
    /// API 设计专家。
    /// </summary>
    ApiDesign = 2,

    /// <summary>
    /// 测试工程专家。
    /// </summary>
    TestEngineering = 3,

    /// <summary>
    /// 架构审查专家。
    /// </summary>
    ArchitectureReview = 4
}

/// <summary>
/// Expert Agent 核心标识。
/// 
/// IRON-01: Expert Agent 不是 Prompt，必须有真实的 Identity。
/// </summary>
public sealed class Expert
{
    /// <summary>
    /// Expert 唯一标识。
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Expert 类型。
    /// </summary>
    public ExpertType Type { get; }

    /// <summary>
    /// Expert 名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Expert 版本。
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Expert 描述。
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 支持的技能列表。
    /// </summary>
    public IReadOnlyList<string> SupportedSkills { get; }

    /// <summary>
    /// 创建时间（UTC）。
    /// </summary>
    public DateTime CreatedAtUtc { get; }

    private Expert(
        Guid id,
        ExpertType type,
        string name,
        string version,
        string description,
        IReadOnlyList<string> supportedSkills,
        DateTime createdAtUtc)
    {
        Id = id;
        Type = type;
        Name = name;
        Version = version;
        Description = description;
        SupportedSkills = supportedSkills ?? Array.Empty<string>();
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// 创建类级重构专家。
    /// </summary>
    public static Expert CreateClassRefactorExpert()
    {
        return new Expert(
            Guid.NewGuid(),
            ExpertType.ClassRefactor,
            "Class Refactoring Expert",
            "v1.0.0",
            "Specialized in class-level refactoring with contract preservation",
            new[] { "ClassDiscovery", "ContractExtraction", "ResponsibilityAnalysis", "RefactorPlanning", "Validation" },
            DateTime.UtcNow);
    }

    /// <summary>
    /// 创建自定义 Expert。
    /// </summary>
    public static Expert Create(ExpertType type, string name, string version, string description, params string[] skills)
    {
        return new Expert(
            Guid.NewGuid(),
            type,
            name,
            version,
            description,
            skills,
            DateTime.UtcNow);
    }
}
