using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant;

/// <summary>
/// 知识库集成服务
/// 基于现有 KnowledgeGraphStore 提供 Agent 所需的知识检索能力
/// V1: 关键字分词 + BFS 邻居扩展 + 简单相关性评分
/// </summary>
public class KnowledgeIntegrationService : IKnowledgeIntegration, ITransient
{
    private readonly IKnowledgeGraphStore _graphStore;
    private readonly ILogger<KnowledgeIntegrationService> _logger;

    public KnowledgeIntegrationService(
        IKnowledgeGraphStore graphStore,
        ILogger<KnowledgeIntegrationService> logger)
    {
        _graphStore = graphStore;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<KnowledgeChunk>> RetrieveRelevantAsync(
        string query,
        List<string>? domainFilter,
        int maxChunks,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new();

        // Step 1: 关键字分词（中英文混合）
        var keywords = Tokenize(query);
        if (keywords.Count == 0)
            return new();

        var allChunks = new List<KnowledgeChunk>();

        // Step 2: 从知识图谱搜索匹配节点
        foreach (var keyword in keywords.Take(5)) // 最多 5 个关键词
        {
            var nodes = await _graphStore.SearchNodesAsync(keyword);
            foreach (var node in nodes)
            {
                // 解析节点属性获取内容
                var content = node.Properties ?? node.Name ?? "";
                var score = keywords.Count(k =>
                    content.Contains(k, StringComparison.OrdinalIgnoreCase));

                allChunks.Add(new KnowledgeChunk
                {
                    NodeId = node.Id,
                    Content = content,
                    NodeType = node.Label ?? "unknown",
                    RelevanceScore = score
                });
            }
        }

        if (allChunks.Count == 0)
            return new();

        // Step 3: 去重 + 按相关性排序
        var seen = new HashSet<string>();
        var ranked = allChunks
            .Where(c => seen.Add(c.NodeId))
            .OrderByDescending(c => c.RelevanceScore)
            .ToList();

        // Step 4: BFS 邻居扩展（取前 5 个高分节点，深度 1）
        var topNodes = ranked.Take(5).ToList();
        foreach (var node in topNodes)
        {
            try
            {
                var neighbors = await _graphStore.QueryNeighborsAsync(node.NodeId, null, 1);
                foreach (var neighbor in neighbors)
                {
                    if (!seen.Contains(neighbor.Id))
                    {
                        seen.Add(neighbor.Id);
                        ranked.Add(new KnowledgeChunk
                        {
                            NodeId = neighbor.Id,
                            Content = neighbor.Properties ?? neighbor.Name ?? "",
                            NodeType = neighbor.Label ?? "unknown",
                            RelevanceScore = node.RelevanceScore * 0.5f // 邻居权重衰减
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BFS 邻居扩展失败: {NodeId}", node.NodeId);
            }
        }

        // Step 5: 领域过滤 + 截断
        var result = ranked
            .Where(c =>
            {
                if (domainFilter == null || domainFilter.Count == 0) return true;
                return domainFilter.Any(d =>
                    c.Content.Contains(d, StringComparison.OrdinalIgnoreCase) ||
                    c.NodeType.Contains(d, StringComparison.OrdinalIgnoreCase));
            })
            .OrderByDescending(c => c.RelevanceScore)
            .Take(maxChunks)
            .ToList();

        _logger.LogInformation(
            "知识检索: query='{Query}', keywords={Keywords}, results={Count}",
            query[..Math.Min(query.Length, 100)],
            keywords.Count,
            result.Count);

        return result;
    }

    /// <inheritdoc/>
    public async Task<string> StorePatternAsync(
        DomainPattern pattern, CancellationToken ct = default)
    {
        var properties = System.Text.Json.JsonSerializer.Serialize(new
        {
            pattern.Description,
            pattern.Source,
            pattern.PatternContent,
            pattern.Domain
        });

        var node = await _graphStore.UpsertNodeAsync(
            $"pattern:{pattern.Domain}",
            pattern.Name,
            properties);

        _logger.LogInformation("领域模式已存储: {Name} ({Domain}), ID={Id}",
            pattern.Name, pattern.Domain, node.Id);

        return node.Id;
    }

    /// <summary>
    /// 简单中英文分词
    /// </summary>
    private static List<string> Tokenize(string text)
    {
        // 按中英文标点 + 空格分割
        var separators = new[] { ' ', '，', '。', '、', '；', '：', ',', '.', ';', ':', '\n', '\r', '\t' };
        return text.Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1) // 过滤单字
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
