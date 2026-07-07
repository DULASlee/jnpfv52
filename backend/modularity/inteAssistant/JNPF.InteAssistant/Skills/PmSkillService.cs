using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills.Cognitive;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 产品经理 Skill（R1 认知模具版）— 真 ToT：TreeSearchAsync 多路并行 + kg.score-candidate 裁决 Top-1。
/// LLM/校验全败 → 抛 Oops.Bah，禁止 fallback 假骨架（施工包 21 R1 / 红线 RL-1）。
/// </summary>
public sealed class PmSkillService : CognitiveSkill, ITransient
{
    private const int TotBranchCount = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ILogger<PmSkillService> _logger;

    public PmSkillService(ICognitiveSkillToolkit toolkit, ILogger<PmSkillService> logger)
        : base(toolkit)
    {
        _logger = logger;
    }

    public override string SkillId => "pm-skill";
    public override string Version => "2.0.0-cognitive";
    public override SkillLayer Layer => SkillLayer.Decision;
    public override SkillMission Mission => SkillMission.DefineBoundary;

    public override SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = Array.Empty<string>(),
        RequiredStability = IrStabilityStates.Draft,
    };

    public override SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[] { IrEventTypes.SkeletonCreated },
    };

    public override Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable);
        if (existing != null)
            return Task.FromResult(SkillValidationResult.Fail("IR-0 骨架已 stable，请先修订或新建项目"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public override Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (events.Count != 1 || events[0].EventType != IrEventTypes.SkeletonCreated)
            return Task.FromResult(SkillValidationResult.Fail("PM Skill 必须产出 1 条 SkeletonCreated"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    protected override async IAsyncEnumerable<AppendIrEventRequest> ThinkAsync(
        SkillPerception perception,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = perception.Context;
        var skeletonId = $"SK-{context.PipelineId}";
        var fragmentId = $"skeleton:{skeletonId}";

        var payload = await GenerateSkeletonViaTotAsync(context, skeletonId, fragmentId, ct);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.SkeletonCreated,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.Skeleton,
            FragmentVersion = 1,
            Payload = payload,
        };
    }

    private async Task<string> GenerateSkeletonViaTotAsync(
        SkillContext context, string skeletonId, string fragmentId, CancellationToken ct)
    {
        var systemPrompt = """
            你是 JNPF 低代码平台的产品经理 Skill。根据用户需求输出 IR-0 骨架 JSON。
            必须包含：businessEvents（6-15项，每项含 eventId/eventName/complexityHint/dependsOn）、
            roleMatrix、entityDrafts（3-8项）。
            只输出 JSON，不要 markdown。
            """;

        var seedHints = string.Join(", ", context.SeedMatches.Take(5).Select(s => s.EventNamePattern));
        var userPrompt = $"""
            用户需求：
            {RequirementTextHelper.ForPmPrompt(context)}

            参考种子（可复用模式）：
            {(string.IsNullOrWhiteSpace(seedHints) ? "（无）" : seedHints)}

            请给出一种 businessEvents 切分方案的完整 IR-0 骨架。
            skeletonId 使用 {skeletonId}。
            """;

        var tot = await Llm.TreeSearchAsync(new TreeSearchRequest
        {
            ProviderCode = context.ProviderCode ?? string.Empty,
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage> { new("user", userPrompt) },
            BranchCount = TotBranchCount,
            BaseTemperature = 0.3,
            TemperatureStep = 0.35,
            ResponseFormat = "json",
            MaxTokens = 4096,
            TimeoutMs = 120_000,
        }, ct);

        if (!tot.IsSuccess || !tot.Succeeded.Any())
            throw Oops.Bah($"PM Skill ToT 全部分支 LLM 失败: {tot.Error}");

        var keyword = ExtractSearchKeyword(context);
        var scored = new List<(string Json, decimal Score, int BranchIndex, double Temperature)>();

        foreach (var candidate in tot.Succeeded)
        {
            string json;
            try
            {
                json = ExtractJson(candidate.Content);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("businessEvents", out var events)
                    || events.ValueKind != JsonValueKind.Array
                    || events.GetArrayLength() == 0)
                {
                    _logger.LogWarning(
                        "PM ToT 分支 {Branch}@{Temp} 缺少 businessEvents，跳过",
                        candidate.BranchIndex, candidate.Temperature);
                    continue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "PM ToT 分支 {Branch}@{Temp} JSON 无效，跳过",
                    candidate.BranchIndex, candidate.Temperature);
                continue;
            }

            var score = await ScoreCandidateAsync(json, keyword, ct);
            scored.Add((json, score, candidate.BranchIndex, candidate.Temperature));
            _logger.LogInformation(
                "PM ToT 分支 {Branch}@{Temp} score={Score} tokens={In}/{Out}",
                candidate.BranchIndex, candidate.Temperature, score,
                candidate.TokensIn, candidate.TokensOut);
        }

        if (scored.Count == 0)
            throw Oops.Bah("PM Skill ToT 全部分支产出无效（JSON 或 businessEvents 校验失败）");

        var top = SelectTopCandidate(scored);
        _logger.LogInformation(
            "PM ToT Top-1: branch={Branch} temp={Temp} score={Score}",
            top.BranchIndex, top.Temperature, top.Score);

        using var topDoc = JsonDocument.Parse(top.Json);
        return NormalizeSkeletonJson(topDoc.RootElement, skeletonId, fragmentId);
    }

    private async Task<decimal> ScoreCandidateAsync(string candidateJson, string keyword, CancellationToken ct)
    {
        var args = JsonSerializer.Serialize(new { candidateJson, keyword }, JsonOptions);
        var result = await Mcp.CallToolAsync("kg.score-candidate", args, ct);
        if (!result.IsSuccess)
            throw Oops.Bah($"kg.score-candidate 失败: {result.Error}");

        using var doc = JsonDocument.Parse(result.ContentJson);
        if (doc.RootElement.TryGetProperty("score", out var scoreEl)
            && scoreEl.TryGetDecimal(out var score))
        {
            return score;
        }

        return 0m;
    }

    /// <summary>按 kg.score-candidate 评分选 Top-1；同分取首条。</summary>
    public static (string Json, decimal Score, int BranchIndex, double Temperature) SelectTopCandidate(
        IReadOnlyList<(string Json, decimal Score, int BranchIndex, double Temperature)> scored)
    {
        var best = scored[0];
        for (var i = 1; i < scored.Count; i++)
        {
            if (scored[i].Score > best.Score)
                best = scored[i];
        }

        return best;
    }

    public static string ExtractSearchKeyword(SkillContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.UserRequirement))
        {
            var trimmed = context.UserRequirement.Trim();
            return trimmed.Length <= 80 ? trimmed : trimmed[..80];
        }

        return context.SeedMatches.FirstOrDefault()?.EventNamePattern ?? "enterprise";
    }

    private static string NormalizeSkeletonJson(JsonElement root, string skeletonId, string fragmentId)
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

    public static string ExtractJson(string content)
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
