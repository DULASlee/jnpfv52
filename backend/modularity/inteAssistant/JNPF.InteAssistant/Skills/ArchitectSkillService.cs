using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Llm;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 架构设计 Skill MVP（P3-B02）— ToT N=2 → ArchitectureDecisionRecorded
/// </summary>
public sealed class ArchitectSkillService : IBaseSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISkillLlmBudgetGuard _budgetGuard;
    private readonly ILogger<ArchitectSkillService> _logger;

    public ArchitectSkillService(ISkillLlmBudgetGuard budgetGuard, ILogger<ArchitectSkillService> logger)
    {
        _budgetGuard = budgetGuard;
        _logger = logger;
    }

    public string SkillId => DesignSkillIds.Architect;
    public string Version => "1.0.0-mvp";

    public SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[] { IrFragmentTypes.EventSpec },
        RequiredStability = IrStabilityStates.Stable,
    };

    public SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[] { IrEventTypes.ArchitectureDecisionRecorded },
    };

    public Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        var ir1 = snapshot.Find(IrFragmentTypes.EventSpec, IrStabilityStates.Stable);
        if (ir1 == null)
            return Task.FromResult(SkillValidationResult.Fail("IR-1 EventSpec 未 stable"));

        if (snapshot.Find(IrFragmentTypes.Architecture, IrStabilityStates.Stable) != null)
            return Task.FromResult(SkillValidationResult.Fail("架构片段已 stable"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(
        SkillContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var fragmentId = $"architecture:{context.ProjectId}";
        string payload;

        try
        {
            payload = await GenerateArchitectureAsync(context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Architect Skill LLM 失败，使用规则回退");
            payload = BuildFallbackArchitecture(context, fragmentId);
        }

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.ArchitectureDecisionRecorded,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.Architecture,
            FragmentVersion = 1,
            Payload = payload,
            SkillId = SkillId,
        };
    }

    public Task<SkillValidationResult> ValidateOutputAsync(IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (events.Count != 1 || events[0].EventType != IrEventTypes.ArchitectureDecisionRecorded)
            return Task.FromResult(SkillValidationResult.Fail("必须产出 1 条 ArchitectureDecisionRecorded"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    private async Task<string> GenerateArchitectureAsync(SkillContext context, CancellationToken ct)
    {
        var systemPrompt = """
            你是 JNPF 架构师 Skill。根据 IR-1 业务事件输出架构决策 JSON。
            必须包含：pattern（layered|cqrs）、modules（数组）、candidates（2 个架构候选，含 score）、selectedIndex（Top-1）。
            只输出 JSON。
            """;

        var userPrompt = $"""
            用户需求：
            {context.UserRequirement}

            IR-1 上下文：
            {context.PromptContext.CompressedSummary}

            请生成 2 个架构候选并选出 Top-1（ToT N=2）。
            projectId={context.ProjectId}
            """;

        var slot = await _budgetGuard.AcquireAsync(
            context.ProjectId, SkillId, context.RunId, context.TenantId, context.PipelineId, ct);

        var response = await _budgetGuard.ExecuteAsync(slot, new ChatCompletionRequest
        {
            ProviderCode = context.ProviderCode ?? string.Empty,
            SystemPrompt = systemPrompt,
            Messages = new List<ChatMessage> { new("user", userPrompt) },
            MaxTokens = slot.MaxTokens,
            Temperature = 0.3,
            TimeoutMs = slot.TimeoutMs,
        }, ct);

        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            throw new InvalidOperationException(response.Error ?? "LLM 返回空");

        var json = ExtractJson(response.Content);
        using var doc = JsonDocument.Parse(json);
        return NormalizeArchitectureJson(doc.RootElement, context, fragmentId: $"architecture:{context.ProjectId}");
    }

    private static string BuildFallbackArchitecture(SkillContext context, string fragmentId)
    {
        var obj = new
        {
            @context = "https://schema.jnpf.ai/ir/v1",
            @id = fragmentId,
            pattern = "layered",
            modules = new[]
            {
                new { name = "Application", layer = "presentation" },
                new { name = "Domain", layer = "domain" },
                new { name = "Infrastructure", layer = "infrastructure" },
            },
            candidates = new[]
            {
                new { pattern = "layered", score = 0.85, rationale = "标准分层，适配 JNPF 模块化单体" },
                new { pattern = "cqrs", score = 0.72, rationale = "读写分离，复杂度较高" },
            },
            selectedIndex = 0,
            stabilityState = IrStabilityStates.Stable,
        };
        return JsonSerializer.Serialize(obj, JsonOptions);
    }

    private static string NormalizeArchitectureJson(JsonElement root, SkillContext context, string fragmentId)
    {
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(root.GetRawText(), JsonOptions)
            ?? new Dictionary<string, JsonElement>();

        dict["@context"] = JsonSerializer.SerializeToElement("https://schema.jnpf.ai/ir/v1");
        dict["@id"] = JsonSerializer.SerializeToElement(fragmentId);
        dict["stabilityState"] = JsonSerializer.SerializeToElement(IrStabilityStates.Stable);

        return JsonSerializer.Serialize(dict, JsonOptions);
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
