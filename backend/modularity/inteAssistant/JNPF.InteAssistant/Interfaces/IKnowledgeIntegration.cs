namespace JNPF.InteAssistant.Interfaces;

/// <summary>
/// 知识库集成服务
/// Agent 从知识图谱中检索上下文，学习新领域模式
/// 对接现有 IKnowledgeGraphStore
/// </summary>
public interface IKnowledgeIntegration
{
    /// <summary>
    /// 从知识图谱中检索相关内容
    /// 基于关键字匹配 + BFS 邻居扩展
    /// V2 将引入向量检索
    /// </summary>
    /// <param name="query">搜索查询</param>
    /// <param name="domainFilter">可选的领域过滤词</param>
    /// <param name="maxChunks">最大返回条目</param>
    /// <param name="ct">取消令牌</param>
    Task<List<KnowledgeChunk>> RetrieveRelevantAsync(
        string query,
        List<string>? domainFilter,
        int maxChunks,
        CancellationToken ct = default);

    /// <summary>
    /// 存储新发现的领域模式到知识图谱
    /// </summary>
    /// <param name="pattern">领域模式</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>模式 ID</returns>
    Task<string> StorePatternAsync(DomainPattern pattern, CancellationToken ct = default);
}

/// <summary>
/// 知识块
/// </summary>
public record KnowledgeChunk
{
    public string NodeId { get; init; } = "";
    public string Content { get; init; } = "";
    public float RelevanceScore { get; init; }
    public string NodeType { get; init; } = "";
}

/// <summary>
/// 领域模式（AI 发现或人类创建的可复用模式）
/// </summary>
public record DomainPattern
{
    public string Name { get; init; } = "";
    public string Domain { get; init; } = "";
    public string Description { get; init; } = "";
    /// <summary>
    /// 来源: "ai-discovered", "human-created", "self-play"
    /// </summary>
    public string Source { get; init; } = "";
    /// <summary>
    /// 模式内容 (JSON)
    /// </summary>
    public string PatternContent { get; init; } = "";
}
