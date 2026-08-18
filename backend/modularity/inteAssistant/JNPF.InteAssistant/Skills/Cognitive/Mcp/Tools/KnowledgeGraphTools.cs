using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Skills;

namespace JNPF.InteAssistant.Skills.Cognitive.Mcp.Tools;

/// <summary>
/// kg.search-seeds——包装 IDomainSeedService.MatchAsync 的领域种子检索工具。
/// </summary>
public sealed class KgSearchSeedsTool : IMcpToolHandler, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IDomainSeedService _seedService;

    public KgSearchSeedsTool(IDomainSeedService seedService) => _seedService = seedService;

    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "kg.search-seeds",
        Description = "按关键词检索行业种子模板（BASE_AI_SEED_TEMPLATE）",
        ArgumentsSchema = """{"keyword":"string 必填，检索关键词"}""",
    };

    public async Task<McpToolResult> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
    {
        var keyword = ReadStringArg(argumentsJson, "keyword");
        if (string.IsNullOrWhiteSpace(keyword))
            return McpToolResult.Fail("kg.search-seeds 缺少 keyword 参数");

        var matches = await _seedService.MatchAsync(keyword, ct);
        return McpToolResult.Ok(JsonSerializer.Serialize(new { matches }, JsonOptions));
    }

    internal static string? ReadStringArg(string argumentsJson, string name)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty(name, out var el)
                && el.ValueKind == JsonValueKind.String)
            {
                return el.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}

/// <summary>
/// kg.score-candidate——包装 IDomainSeedService.ScoreCandidate，
/// 供 ToT 生成的候选方案做知识图谱契合度评分（生成与评估分离，施工包 21 §3.5）。
/// </summary>
public sealed class KgScoreCandidateTool : IMcpToolHandler, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IDomainSeedService _seedService;

    public KgScoreCandidateTool(IDomainSeedService seedService) => _seedService = seedService;

    public McpToolDescriptor Descriptor { get; } = new()
    {
        Name = "kg.score-candidate",
        Description = "对候选方案 JSON 做种子模板契合度评分（0-1）",
        ArgumentsSchema = """{"candidateJson":"string 必填，候选方案 JSON","keyword":"string 必填，用于圈定参照种子"}""",
    };

    public async Task<McpToolResult> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
    {
        var candidateJson = KgSearchSeedsTool.ReadStringArg(argumentsJson, "candidateJson");
        var keyword = KgSearchSeedsTool.ReadStringArg(argumentsJson, "keyword");
        if (string.IsNullOrWhiteSpace(candidateJson))
            return McpToolResult.Fail("kg.score-candidate 缺少 candidateJson 参数");
        if (string.IsNullOrWhiteSpace(keyword))
            return McpToolResult.Fail("kg.score-candidate 缺少 keyword 参数");

        var seeds = await _seedService.MatchAsync(keyword, ct);
        var score = _seedService.ScoreCandidate(candidateJson, seeds);
        return McpToolResult.Ok(JsonSerializer.Serialize(new { score, seedCount = seeds.Count }, JsonOptions));
    }
}
