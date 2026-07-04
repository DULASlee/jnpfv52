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
/// UI 设计 Skill MVP（P3-B04）— FormPageIR 产出
/// </summary>
public sealed class UiDesignSkillService : IBaseSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISkillLlmBudgetGuard _budgetGuard;
    private readonly ILogger<UiDesignSkillService> _logger;

    public UiDesignSkillService(ISkillLlmBudgetGuard budgetGuard, ILogger<UiDesignSkillService> logger)
    {
        _budgetGuard = budgetGuard;
        _logger = logger;
    }

    public string SkillId => DesignSkillIds.UiDesign;
    public string Version => "1.0.0-mvp";

    public SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[] { IrFragmentTypes.EventSpec },
        RequiredStability = IrStabilityStates.Stable,
    };

    public SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[] { IrEventTypes.UIDesignStabilized },
    };

    public Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        if (snapshot.Find(IrFragmentTypes.EventSpec, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("IR-1 未 stable"));

        if (snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable) != null)
            return Task.FromResult(SkillValidationResult.Fail("FormPageIR 已 stable"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(
        SkillContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var fragmentId = $"formPage:{context.ProjectId}";
        string payload;

        try
        {
            payload = await GenerateFormPageIrAsync(context, fragmentId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UI Design Skill LLM 失败，使用规则回退");
            payload = BuildFallbackFormPageIr(context, fragmentId);
        }

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.UIDesignStabilized,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.FormPageIR,
            FragmentVersion = 1,
            Payload = payload,
            SkillId = SkillId,
        };
    }

    public Task<SkillValidationResult> ValidateOutputAsync(IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (events.Count != 1 || events[0].EventType != IrEventTypes.UIDesignStabilized)
            return Task.FromResult(SkillValidationResult.Fail("必须产出 1 条 UIDesignStabilized"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    private async Task<string> GenerateFormPageIrAsync(SkillContext context, string fragmentId, CancellationToken ct)
    {
        var slot = await _budgetGuard.AcquireAsync(
            context.ProjectId, SkillId, context.RunId, context.TenantId, context.PipelineId, ct);

        var response = await _budgetGuard.ExecuteAsync(slot, new ChatCompletionRequest
        {
            SystemPrompt = """
                你是 JNPF UI 设计 Skill。输出 FormPageIR JSON（pages 数组，每页含 fields）。
                字段需含 id、label、componentType。只输出 JSON。
                """,
            Messages = new List<ChatMessage>
            {
                new("user", $"需求: {context.UserRequirement}\nIR: {context.PromptContext.CompressedSummary}"),
            },
            ResponseFormat = "json",
            MaxTokens = slot.MaxTokens,
            Temperature = 0.3,
            TimeoutMs = slot.TimeoutMs,
        }, ct);

        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            throw new InvalidOperationException(response.Error ?? "LLM 返回空");

        using var doc = JsonDocument.Parse(ExtractJson(response.Content));
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(doc.RootElement.GetRawText(), JsonOptions)
            ?? new Dictionary<string, JsonElement>();
        dict["@context"] = JsonSerializer.SerializeToElement("https://schema.jnpf.ai/ir/v1");
        dict["@id"] = JsonSerializer.SerializeToElement(fragmentId);
        dict["stabilityState"] = JsonSerializer.SerializeToElement(IrStabilityStates.Stable);
        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    private static string BuildFallbackFormPageIr(SkillContext context, string fragmentId)
    {
        var obj = new
        {
            @context = "https://schema.jnpf.ai/ir/v1",
            @id = fragmentId,
            pages = new[]
            {
                new
                {
                    pageId = "main-list",
                    title = "业务列表",
                    layout = "list",
                    fields = new[]
                    {
                        new { id = "name", label = "名称", componentType = "input" },
                        new { id = "status", label = "状态", componentType = "select" },
                    },
                },
            },
            stabilityState = IrStabilityStates.Stable,
        };
        return JsonSerializer.Serialize(obj, JsonOptions);
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
