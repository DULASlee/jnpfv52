using System.Collections.Concurrent;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Interfaces;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// 知识图谱存储 Sql 实现 (Phase 6 Enhanced).
/// Studio 侧唯一真源（MVP2 再评估 Neo4j）.
/// 新增: UpsertNode, GetNode, ListNodes, ListEdges, GetStats.
/// 优化: 邻接表索引 — BFS 从 O(n²) 降为 O(n) 数据库往返.
/// </summary>
public class KnowledgeGraphStore : IKnowledgeGraphStore, ITransient
{
    private readonly ISqlSugarRepository<KnowledgeNodeEntity> _nodeRepository;
    private readonly ISqlSugarRepository<KnowledgeEdgeEntity> _edgeRepository;

    // 邻接表索引: nodeId → { neighborId }
    // 写入 AddEdgeAsync 时同步维护，BFS QueryNeighborsAsync 时 O(1) 查找
    private readonly ConcurrentDictionary<string, HashSet<string>> _outEdges = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _inEdges = new();
    private volatile bool _indexLoaded;

    public KnowledgeGraphStore(
        ISqlSugarRepository<KnowledgeNodeEntity> nodeRepository,
        ISqlSugarRepository<KnowledgeEdgeEntity> edgeRepository)
    {
        _nodeRepository = nodeRepository;
        _edgeRepository = edgeRepository;
    }

    /// <summary>
    /// 延迟加载邻接表索引（首次 BFS 时从数据库构建）.
    /// </summary>
    private async Task EnsureIndexLoadedAsync()
    {
        if (_indexLoaded) return;

        var edges = await _edgeRepository.AsQueryable()
            .Where(e => e.DeleteMark == null)
            .Select(e => new { e.SourceNodeId, e.TargetNodeId })
            .ToListAsync();

        foreach (var edge in edges)
        {
            _outEdges.GetOrAdd(edge.SourceNodeId, _ => new HashSet<string>()).Add(edge.TargetNodeId);
            _inEdges.GetOrAdd(edge.TargetNodeId, _ => new HashSet<string>()).Add(edge.SourceNodeId);
        }

        _indexLoaded = true;
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
    public async Task<KnowledgeNodeEntity> UpsertNodeAsync(string label, string name, string? properties)
    {
        // 按 label + name 查找已存在节点
        var existing = await _nodeRepository.AsQueryable()
            .Where(n => n.Label == label && n.Name == name && n.DeleteMark == null)
            .FirstAsync();

        if (existing != null)
        {
            // 更新
            existing.Properties = properties ?? existing.Properties;
            existing.LastModify();
            await _nodeRepository.AsUpdateable(existing)
                .IgnoreColumns(ignoreAllNullColumns: true)
                .ExecuteCommandAsync();
            return existing;
        }

        // 插入新节点
        return await AddNodeAsync(label, name, properties);
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

        // 同步维护邻接表索引
        _outEdges.GetOrAdd(sourceId, _ => new HashSet<string>()).Add(targetId);
        _inEdges.GetOrAdd(targetId, _ => new HashSet<string>()).Add(sourceId);

        return edge;
    }

    /// <inheritdoc/>
    public async Task<KnowledgeNodeEntity?> GetNodeAsync(string id)
    {
        return await _nodeRepository.AsQueryable()
            .Where(n => n.Id == id && n.DeleteMark == null)
            .FirstAsync();
    }

    /// <inheritdoc/>
    public async Task<(List<KnowledgeNodeEntity> List, int Total)> ListNodesAsync(
        string? label = null, string? domain = null, int currentPage = 1, int pageSize = 20)
    {
        var query = _nodeRepository.AsQueryable()
            .Where(n => n.DeleteMark == null)
            .WhereIF(!string.IsNullOrEmpty(label), n => n.Label == label);

        // domain 存储在 Properties JSON 中，使用 LIKE 查询
        if (!string.IsNullOrEmpty(domain))
        {
            query = query.Where(n => n.Properties.Contains(domain));
        }

        var total = await query.CountAsync();
        var list = await query
            .OrderBy(n => n.LastModifyTime, OrderByType.Desc)
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (list, total);
    }

    /// <inheritdoc/>
    public async Task<(List<KnowledgeEdgeEntity> List, int Total)> ListEdgesAsync(
        string? relationType = null, int currentPage = 1, int pageSize = 20)
    {
        var query = _edgeRepository.AsQueryable()
            .Where(e => e.DeleteMark == null)
            .WhereIF(!string.IsNullOrEmpty(relationType), e => e.RelationType == relationType);

        var total = await query.CountAsync();
        var list = await query
            .OrderBy(e => e.LastModifyTime, OrderByType.Desc)
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (list, total);
    }

    /// <inheritdoc/>
    public async Task<KnowledgeStats> GetStatsAsync()
    {
        var nodes = await _nodeRepository.AsQueryable()
            .Where(n => n.DeleteMark == null)
            .ToListAsync();

        var edges = await _edgeRepository.AsQueryable()
            .Where(e => e.DeleteMark == null)
            .ToListAsync();

        return new KnowledgeStats
        {
            NodeCount = nodes.Count,
            EdgeCount = edges.Count,
            Labels = nodes.GroupBy(n => n.Label ?? "unknown")
                          .ToDictionary(g => g.Key, g => g.Count()),
            RelationTypes = edges.GroupBy(e => e.RelationType ?? "unknown")
                                .ToDictionary(g => g.Key, g => g.Count()),
            LastPatchAt = nodes.Max(n => (DateTime?)n.LastModifyTime) ?? nodes.Max(n => n.CreatorTime),
            PatchVersion = nodes.Count + edges.Count, // 简易版本号
        };
    }

    /// <inheritdoc/>
    public async Task<List<KnowledgeNodeEntity>> QueryNeighborsAsync(string nodeId, string? relationType, int depth = 1)
    {
        if (depth < 1) depth = 1;
        if (depth > 3) depth = 3;

        // 延迟加载邻接表索引（首次调用时从 DB 构建，后续 O(1)）
        await EnsureIndexLoadedAsync();

        var visited = new HashSet<string> { nodeId };
        var result = new List<KnowledgeNodeEntity>();
        var frontier = new HashSet<string> { nodeId };

        for (int d = 0; d < depth; d++)
        {
            var nextFrontier = new HashSet<string>();

            foreach (var sourceId in frontier)
            {
                // O(1) 邻接表查找 — 替代 O(n) 边表扫描
                var neighbors = new HashSet<string>();

                if (_outEdges.TryGetValue(sourceId, out var outNeighbors))
                    foreach (var n in outNeighbors) neighbors.Add(n);

                if (_inEdges.TryGetValue(sourceId, out var inNeighbors))
                    foreach (var n in inNeighbors) neighbors.Add(n);

                // 过滤已访问节点
                neighbors.ExceptWith(visited);

                if (neighbors.Count > 0)
                {
                    // 批量加载节点
                    var nodes = await _nodeRepository.AsQueryable()
                        .Where(n => neighbors.Contains(n.Id) && n.DeleteMark == null)
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
