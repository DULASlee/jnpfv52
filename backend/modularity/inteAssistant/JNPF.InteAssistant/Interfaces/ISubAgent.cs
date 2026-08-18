namespace JNPF.InteAssistant.Interfaces;

/// <summary>
/// 子 Agent 接口
/// 详细设计阶段由 DetailedDesignOrchestrator 调度 6 个 SubAgent 并行执行
/// </summary>
public interface ISubAgent
{
    /// <summary>
    /// Agent 唯一名称（如 "functional_module", "business_process", "database"）
    /// </summary>
    string AgentName { get; }

    /// <summary>
    /// Agent 显示名称（如 "功能模块设计", "业务流程设计", "数据库设计"）
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// 执行 Agent 任务
    /// </summary>
    /// <param name="context">详细设计上下文（需求 + 架构 + 知识库块）</param>
    /// <param name="previousResults">已完成的其他 Agent 结果（批次 1 产出供批次 2 使用）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>Agent 执行结果</returns>
    Task<SubAgentResult> ExecuteAsync(
        DetailedDesignContext context,
        IReadOnlyDictionary<string, SubAgentResult> previousResults,
        CancellationToken ct = default);
}

/// <summary>
/// Agent 执行结果
/// </summary>
public record SubAgentResult
{
    public string AgentName { get; init; } = "";
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public string Content { get; init; } = "";
    public string DocumentTitle { get; init; } = "";
    public int TokensUsed { get; init; }
    public int LatencyMs { get; init; }
}

/// <summary>
/// 详细设计上下文
/// </summary>
public record DetailedDesignContext
{
    public string ProjectName { get; init; } = "";
    public string Requirements { get; init; } = "";
    public string Architecture { get; init; } = "";
    public long TenantId { get; init; }
    public List<KnowledgeChunk> KnowledgeChunks { get; init; } = new();
    public Dictionary<string, string> Variables { get; init; } = new();
}

/// <summary>
/// 详细设计编排结果
/// </summary>
public record DetailedDesignResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public IReadOnlyDictionary<string, SubAgentResult> SubAgentResults { get; init; } = new Dictionary<string, SubAgentResult>();
    public MergedDocument? MergedDocument { get; init; }
    public string? DocumentUrl { get; init; }
}
