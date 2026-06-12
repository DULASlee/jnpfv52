using JNPF.InteAssistant.Entitys.Entity;

namespace JNPF.InteAssistant.Interfaces;

/// <summary>
/// 知识图谱存储接口
/// Studio 侧唯一真源，基于 SQL Server
/// </summary>
public interface IKnowledgeGraphStore
{
    /// <summary>
    /// 添加节点
    /// </summary>
    Task<KnowledgeNodeEntity> AddNodeAsync(string label, string name, string? properties);

    /// <summary>
    /// 添加边
    /// </summary>
    Task<KnowledgeEdgeEntity> AddEdgeAsync(string sourceId, string targetId, string relationType, string? properties);

    /// <summary>
    /// 查询邻居节点（BFS，最大深度 3）
    /// </summary>
    Task<List<KnowledgeNodeEntity>> QueryNeighborsAsync(string nodeId, string? relationType, int depth = 1);

    /// <summary>
    /// 按关键字搜索节点
    /// </summary>
    Task<List<KnowledgeNodeEntity>> SearchNodesAsync(string keyword);
}
