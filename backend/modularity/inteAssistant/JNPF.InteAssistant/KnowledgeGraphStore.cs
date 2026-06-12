using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Interfaces;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// 知识图谱存储 Sql 实现
/// Studio 侧唯一真源（MVP2 再评估 Neo4j）
/// </summary>
public class KnowledgeGraphStore : IKnowledgeGraphStore, ITransient
{
    private readonly ISqlSugarRepository<KnowledgeNodeEntity> _nodeRepository;
    private readonly ISqlSugarRepository<KnowledgeEdgeEntity> _edgeRepository;

    /// <summary>
    /// 初始化一个<see cref="KnowledgeGraphStore"/>类型的新实例
    /// </summary>
    public KnowledgeGraphStore(
        ISqlSugarRepository<KnowledgeNodeEntity> nodeRepository,
        ISqlSugarRepository<KnowledgeEdgeEntity> edgeRepository)
    {
        _nodeRepository = nodeRepository;
        _edgeRepository = edgeRepository;
    }

    /// <inheritdoc/>
    public async Task<KnowledgeNodeEntity> AddNodeAsync(string label, string name, string? properties)
    {
        var node = new KnowledgeNodeEntity
        {
            Label = label,
            Name = name,
            Properties = properties ?? "{}",
        };
        node.Create();

        await _nodeRepository.AsInsertable(node).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        return node;
    }

    /// <inheritdoc/>
    public async Task<KnowledgeEdgeEntity> AddEdgeAsync(string sourceId, string targetId, string relationType, string? properties)
    {
        var edge = new KnowledgeEdgeEntity
        {
            SourceNodeId = sourceId,
            TargetNodeId = targetId,
            RelationType = relationType,
            Properties = properties ?? "{}",
        };
        edge.Create();

        await _edgeRepository.AsInsertable(edge).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        return edge;
    }

    /// <inheritdoc/>
    public async Task<List<KnowledgeNodeEntity>> QueryNeighborsAsync(string nodeId, string? relationType, int depth = 1)
    {
        if (depth < 1) depth = 1;
        if (depth > 3) depth = 3;

        var visited = new HashSet<string> { nodeId };
        var result = new List<KnowledgeNodeEntity>();
        var frontier = new List<string> { nodeId };

        for (int d = 0; d < depth; d++)
        {
            var nextFrontier = new List<string>();

            foreach (var sourceId in frontier)
            {
                // 查询以当前节点为起点的所有边
                var edges = await _edgeRepository.AsQueryable()
                    .WhereIF(!string.IsNullOrEmpty(relationType), e => e.RelationType == relationType)
                    .Where(e => e.SourceNodeId == sourceId && e.DeleteMark == null)
                    .ToListAsync();

                // 也查反向边
                var reverseEdges = await _edgeRepository.AsQueryable()
                    .WhereIF(!string.IsNullOrEmpty(relationType), e => e.RelationType == relationType)
                    .Where(e => e.TargetNodeId == sourceId && e.DeleteMark == null)
                    .ToListAsync();

                var targetIds = edges.Select(e => e.TargetNodeId)
                    .Concat(reverseEdges.Select(e => e.SourceNodeId))
                    .Where(id => !visited.Contains(id))
                    .Distinct()
                    .ToList();

                if (targetIds.Any())
                {
                    var nodes = await _nodeRepository.AsQueryable()
                        .Where(n => targetIds.Contains(n.Id) && n.DeleteMark == null)
                        .ToListAsync();

                    result.AddRange(nodes);
                    foreach (var n in nodes)
                    {
                        visited.Add(n.Id);
                        nextFrontier.Add(n.Id);
                    }
                }
            }

            frontier = nextFrontier;
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<List<KnowledgeNodeEntity>> SearchNodesAsync(string keyword)
    {
        return await _nodeRepository.AsQueryable()
            .Where(n => n.DeleteMark == null)
            .Where(n => n.Name.Contains(keyword) || n.Label.Contains(keyword))
            .ToListAsync();
    }
}
