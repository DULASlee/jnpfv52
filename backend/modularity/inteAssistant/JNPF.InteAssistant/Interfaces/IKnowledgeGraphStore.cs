using JNPF.InteAssistant.Entitys.Entity;

namespace JNPF.InteAssistant.Interfaces;

/// <summary>
/// 知识图谱存储接口 (Phase 6 Enhanced).
/// Studio 侧唯一真源，基于 SQL Server.
/// </summary>
public interface IKnowledgeGraphStore
{
    /// <summary>
    /// 添加节点.
    /// </summary>
    Task<KnowledgeNodeEntity> AddNodeAsync(string label, string name, string? properties);

    /// <summary>
    /// UPSERT 节点（存在则更新，不存在则插入）.
    /// </summary>
    Task<KnowledgeNodeEntity> UpsertNodeAsync(string label, string name, string? properties);

    /// <summary>
    /// 添加边.
    /// </summary>
    Task<KnowledgeEdgeEntity> AddEdgeAsync(string sourceId, string targetId, string relationType, string? properties);

    /// <summary>
    /// 获取单个节点.
    /// </summary>
    Task<KnowledgeNodeEntity?> GetNodeAsync(string id);

    /// <summary>
    /// 分页查询节点列表.
    /// </summary>
    Task<(List<KnowledgeNodeEntity> List, int Total)> ListNodesAsync(
        string? label = null, string? domain = null, int currentPage = 1, int pageSize = 20);

    /// <summary>
    /// 分页查询边列表.
    /// </summary>
    Task<(List<KnowledgeEdgeEntity> List, int Total)> ListEdgesAsync(
        string? relationType = null, int currentPage = 1, int pageSize = 20);

    /// <summary>
    /// 获取知识图谱统计.
    /// </summary>
    Task<KnowledgeStats> GetStatsAsync();

    /// <summary>
    /// 查询邻居节点（BFS，最大深度 3）.
    /// </summary>
    Task<List<KnowledgeNodeEntity>> QueryNeighborsAsync(string nodeId, string? relationType, int depth = 1);

    /// <summary>
    /// 按关键字搜索节点.
    /// </summary>
    Task<List<KnowledgeNodeEntity>> SearchNodesAsync(string keyword);
}

/// <summary>
/// 知识图谱统计信息.
/// </summary>
public class KnowledgeStats
{
    public int NodeCount { get; set; }
    public int EdgeCount { get; set; }
    public Dictionary<string, int> Labels { get; set; } = new();
    public Dictionary<string, int> RelationTypes { get; set; } = new();
    public DateTime? LastPatchAt { get; set; }
    public int PatchVersion { get; set; }
}
