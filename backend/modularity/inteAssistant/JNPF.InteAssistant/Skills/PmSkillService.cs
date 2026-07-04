using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 产品经理 Skill MVP（P2-B04）— ToT N=2 + 领域评分 Top-1
/// </summary>
public sealed class PmSkillService : IBaseSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ILlmGatewayService _llmGateway;
    private readonly IDomainSeedService _seedService;
    private readonly ILogger<PmSkillService> _logger;

    public PmSkillService(
        ILlmGatewayService llmGateway,
        IDomainSeedService seedService,
        ILogger<PmSkillService> logger)
    {
        _llmGateway = llmGateway;
        _seedService = seedService;
        _logger = logger;
    }

    public string SkillId => "pm-skill";
    public string Version => "1.0.0-mvp";

    public SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = Array.Empty<string>(),
        RequiredStability = IrStabilityStates.Draft,
    };

    public SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[] { IrEventTypes.SkeletonCreated },
    };

    public Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (existing != null)
            return Task.FromResult(SkillValidationResult.Fail("IR-0 骨架已 stable，请先修订或新建项目"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(
        SkillContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var skeletonId = $"SK-{context.PipelineId}";
        var fragmentId = $"skeleton:{skeletonId}";

        string payload;
        try
        {
            payload = await GenerateSkeletonPayloadAsync(context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PM Skill LLM 失败，使用规则回退");
            payload = BuildFallbackSkeleton(context, skeletonId, fragmentId);
        }

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.SkeletonCreated,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.Skeleton,
            FragmentVersion = 1,
            Payload = payload,
            SkillId = SkillId,
        };
    }

    public Task<SkillValidationResult> ValidateOutputAsync(IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (events.Count != 1 || events[0].EventType != IrEventTypes.SkeletonCreated)
            return Task.FromResult(SkillValidationResult.Fail("PM Skill 必须产出 1 条 SkeletonCreated"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    private async Task<string> GenerateSkeletonPayloadAsync(SkillContext context, CancellationToken ct)
    {
        var systemPrompt = """
            你是 JNPF 低代码平台的产品经理 Skill。根据用户需求输出 IR-0 骨架 JSON。
            必须包含：businessEvents（6-15项，每项含 eventId/eventName/complexityHint/dependsOn）、
            roleMatrix、entityDrafts（3-8项）。
            只输出 JSON，不要 markdown。
            """;

        var userPrompt = $"""
            用户需求：
            {context.UserRequirement}

            参考种子（可复用模式）：
            {string.Join(", ", context.SeedMatches.Take(5).Select(s => s.EventNamePattern))}

            请生成 2 个候选 businessEvents 切分方案的合并最优结果（ToT Top-1）。
            skeletonId 使用 SK-{context.PipelineId}。
            """;

        var response = await _llmGateway.ChatAsync(new ChatCompletionRequest
        {
            ProviderCode = context.ProviderCode ?? string.Empty,
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage> { new("user", userPrompt) },
            MaxTokens = 4096,
            Temperature = 0.4,
            TimeoutMs = 120_000,
        }, ct);

        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            throw new InvalidOperationException(response.Error ?? "LLM 返回空");

        var json = ExtractJson(response.Content);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("businessEvents", out var events) || events.GetArrayLength() == 0)
            throw new InvalidOperationException("LLM 产出缺少 businessEvents");

        var score = _seedService.ScoreCandidate(json, context.SeedMatches);
        _logger.LogInformation("PM Skill LLM skeleton score={Score} tokens={In}/{Out}",
            score, response.TokensIn, response.TokensOut);

        return NormalizeSkeletonJson(root, context, $"SK-{context.PipelineId}", $"skeleton:SK-{context.PipelineId}");
    }

    private static string BuildFallbackSkeleton(SkillContext context, string skeletonId, string fragmentId)
    {
        var events = ExtractEventsFromRequirement(context.UserRequirement, context.SeedMatches);
        var obj = new
        {
            @context = "https://schema.jnpf.ai/ir/v1",
            @id = fragmentId,
            skeletonId,
            version = 1,
            businessEvents = events,
            roleMatrix = new[]
            {
                new { roleName = "Employee", responsibilities = new[] { "业务操作" } },
                new { roleName = "Manager", responsibilities = new[] { "审批" } },
            },
            entityDrafts = events.Select((e, i) =>
            {
                var eventId = GetProp(e, "eventId") ?? $"BE-{i + 1:D3}";
                var eventName = GetProp(e, "eventName") ?? $"Event{i + 1}";
                return new
                {
                    entityName = eventName,
                    tableName = $"OA_{eventId.Replace("-", "_").ToUpperInvariant()}",
                    fields = Array.Empty<object>(),
                };
            }).ToArray(),
        };
        return JsonSerializer.Serialize(obj, JsonOptions);
    }

    private static List<object> ExtractEventsFromRequirement(string requirement, IReadOnlyList<SeedTemplateMatch> seeds)
    {
        var list = new List<object>();
        var idx = 0;
        foreach (var seed in seeds.Take(8))
        {
            idx++;
            list.Add(new
            {
                eventId = $"BE-{idx:D3}",
                eventName = seed.EventNamePattern,
                complexityHint = seed.ComplexityHint,
                dependsOn = Array.Empty<string>(),
            });
        }

        if (list.Count >= 6) return list;

        var keywords = Regex.Matches(requirement, @"[\u4e00-\u9fa5]{2,6}")
            .Select(m => m.Value)
            .Distinct()
            .Take(10)
            .ToList();

        foreach (var kw in keywords)
        {
            if (list.Count >= 10) break;
            idx++;
            list.Add(new
            {
                eventId = $"BE-{idx:D3}",
                eventName = kw,
                complexityHint = "simple",
                dependsOn = Array.Empty<string>(),
            });
        }

        while (list.Count < 6)
        {
            idx++;
            list.Add(new
            {
                eventId = $"BE-{idx:D3}",
                eventName = $"BusinessEvent{idx}",
                complexityHint = "simple",
                dependsOn = Array.Empty<string>(),
            });
        }

        return list;
    }

    private static string NormalizeSkeletonJson(
        JsonElement root, SkillContext context, string skeletonId, string fragmentId)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(root.GetRawText(), JsonOptions)
            ?? new Dictionary<string, JsonElement>();

        dict["@context"] = JsonSerializer.SerializeToElement("https://schema.jnpf.ai/ir/v1");
        dict["@id"] = JsonSerializer.SerializeToElement(fragmentId);
        dict["skeletonId"] = JsonSerializer.SerializeToElement(skeletonId);
        if (!dict.ContainsKey("version"))
            dict["version"] = JsonSerializer.SerializeToElement(1);

        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    private static string? GetProp(object item, string name)
    {
        if (item is JsonElement el && el.TryGetProperty(name, out var prop))
            return prop.GetString();
        return item.GetType().GetProperty(name)?.GetValue(item)?.ToString();
    }

    private static string ExtractJson(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
                return trimmed[start..(end + 1)];
        }

        return trimmed;
    }
}
