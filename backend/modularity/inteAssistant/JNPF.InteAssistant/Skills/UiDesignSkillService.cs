using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Skills.Cognitive;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// UI 设计 Skill（R3 认知模具版）— FormPageIR 经 BudgetGuard 生成；禁止 fallback。
/// </summary>
public sealed class UiDesignSkillService : CognitiveSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISkillLlmBudgetGuard _budgetGuard;

    public UiDesignSkillService(
        ICognitiveSkillToolkit toolkit,
        ISkillLlmBudgetGuard budgetGuard)
        : base(toolkit)
    {
        _budgetGuard = budgetGuard;
    }

    public override string SkillId => DesignSkillIds.UiDesign;
    public override string Version => "2.0.0-cognitive";
    public override SkillLayer Layer => SkillLayer.Refinement;
    public override SkillMission Mission => SkillMission.GenerateArtifact;

    public override SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[] { IrFragmentTypes.EventSpec },
        RequiredStability = IrStabilityStates.Stable,
    };

    public override SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[] { IrEventTypes.UIDesignStabilized },
    };

    public override Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        if (snapshot.Find(IrFragmentTypes.EventSpec, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("IR-1 未 stable"));

        if (snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable) != null)
            return Task.FromResult(SkillValidationResult.Fail("FormPageIR 已 stable"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public override Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (events.Count != 1 || events[0].EventType != IrEventTypes.UIDesignStabilized)
            return Task.FromResult(SkillValidationResult.Fail("必须产出 1 条 UIDesignStabilized"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    protected override async IAsyncEnumerable<AppendIrEventRequest> ThinkAsync(
        SkillPerception perception,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = perception.Context;
        var fragmentId = $"formPage:{context.ProjectId}";
        var payload = await GenerateFormPageIrAsync(context, fragmentId, ct);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.UIDesignStabilized,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.FormPageIR,
            FragmentVersion = 1,
            Payload = payload,
        };
    }

    private async Task<string> GenerateFormPageIrAsync(SkillContext context, string fragmentId, CancellationToken ct)
    {
        var slot = await _budgetGuard.AcquireAsync(
            context.ProjectId, SkillId, context.RunId, context.TenantId, context.PipelineId, ct);

        var response = await _budgetGuard.ExecuteAsync(slot, new ChatCompletionRequest
        {
            ProviderCode = context.ProviderCode ?? string.Empty,
            SystemPrompt = """
                你是 JNPF UI 设计 Skill。输出 FormPageIR JSON（pages 数组，每页含 fields）。

                每页必须含：
                - id: 页面唯一标识
                - title: 页面标题
                - pageType: 页面类型（list=列表页 / form=表单页 / detail=详情页）
                - entityBinding: 绑定的实体名（对应 entityDrafts[].entityName）
                - fields[]: 每字段含 id/label/componentType/required

                列表页额外字段：
                - listColumns[]: 列表显示的列名
                - searchFields[]: 搜索/筛选字段名

                字段 componentType 可选值：Input/Textarea/Number/InputNumber/Select/Radio/Checkbox/
                DatePicker/DateTimePicker/TimePicker/Switch/UploadFile/UploadImg/Table/Cascader/Rate/Slider/Editor

                输出示例：
                {"pages": [
                  {"id":"leave-list","title":"请假申请列表","pageType":"list","entityBinding":"LeaveRequest",
                   "listColumns":["employeeName","leaveType","startDate","endDate","status"],
                   "searchFields":["leaveType","status"],
                   "fields":[{"id":"leaveType","label":"请假类型","componentType":"Select","required":true}]},
                  {"id":"leave-form","title":"请假申请表单","pageType":"form","entityBinding":"LeaveRequest",
                   "fields":[{"id":"leaveType","label":"请假类型","componentType":"Select","required":true},
                             {"id":"reason","label":"请假事由","componentType":"Textarea","required":true}]}]}

                只输出 JSON，不要 markdown。
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
            throw Oops.Bah(response.Error ?? "UI Design Skill LLM 返回空");

        using var doc = JsonDocument.Parse(PmSkillService.ExtractJson(response.Content));
        if (!doc.RootElement.TryGetProperty("pages", out var pages)
            || pages.ValueKind != JsonValueKind.Array
            || pages.GetArrayLength() == 0)
        {
            throw Oops.Bah("UI Design Skill 产出缺少非空 pages 数组");
        }

        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(doc.RootElement.GetRawText(), JsonOptions)
            ?? new Dictionary<string, JsonElement>();
        dict["@context"] = JsonSerializer.SerializeToElement("https://schema.jnpf.ai/ir/v1");
        dict["@id"] = JsonSerializer.SerializeToElement(fragmentId);
        dict["stabilityState"] = JsonSerializer.SerializeToElement(IrStabilityStates.Stable);
        return JsonSerializer.Serialize(dict, JsonOptions);
    }
}
