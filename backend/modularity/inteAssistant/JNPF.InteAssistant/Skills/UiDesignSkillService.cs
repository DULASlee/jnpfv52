using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Skills.Cognitive;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// UI 设计 Skill（R3 认知模具版）— FormPageIR 经 BudgetGuard 生成；字段源 = ai_entity_field（25 §6）。
/// </summary>
public sealed class UiDesignSkillService : CognitiveSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISkillLlmBudgetGuard _budgetGuard;
    private readonly EntityDesignRepository _entityDesignRepo;

    public UiDesignSkillService(
        ICognitiveSkillToolkit toolkit,
        ISkillLlmBudgetGuard budgetGuard,
        EntityDesignRepository entityDesignRepo)
        : base(toolkit)
    {
        _budgetGuard = budgetGuard;
        _entityDesignRepo = entityDesignRepo;
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
        // Finalize / ai_entity_field 由 DesignSkillsApi / Orchestrator 门禁；此处保留 EventSpec 存在性
        if (snapshot.Find(IrFragmentTypes.EventSpec, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("IR-1 EventSpec 未 stable（且须已 Finalize）"));

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

        var fields = await _entityDesignRepo.ListFieldsAsync(
            context.TenantId, context.ProjectId, context.PipelineId.ToString(), ct);
        if (fields.Count == 0)
            throw Oops.Bah("UiDesign Skill: ai_entity_field 无字段，拒绝继续（须先 Round 3 投影）");

        var payload = await GenerateFormPageIrAsync(context, fragmentId, fields, ct);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.UIDesignStabilized,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.FormPageIR,
            FragmentVersion = 1,
            Payload = payload,
        };
    }

    private async Task<string> GenerateFormPageIrAsync(
        SkillContext context,
        string fragmentId,
        IReadOnlyList<AiEntityFieldEntity> fields,
        CancellationToken ct)
    {
        var slot = await _budgetGuard.AcquireAsync(
            context.ProjectId, SkillId, context.RunId, context.TenantId, context.PipelineId, ct);

        var fieldContext = BuildFieldContext(fields);

        var response = await _budgetGuard.ExecuteAsync(slot, new ChatCompletionRequest
        {
            ProviderCode = context.ProviderCode ?? string.Empty,
            SystemPrompt = """
                你是 JNPF UI 设计 Skill。输出 FormPageIR JSON（pages 数组，每页含 fields）。

                每页必须含：
                - id: 页面唯一标识
                - title: 页面标题
                - pageType: 页面类型（list=列表页 / form=表单页 / detail=详情页）
                - entityBinding: 绑定的实体名（必须来自下方 ai_entity_field 实体列表）
                - fields[]: 每字段含 id/label/componentType/required（字段 id 必须来自 ai_entity_field.FieldName）

                列表页额外字段：
                - listColumns[]: 列表显示的列名
                - searchFields[]: 搜索/筛选字段名

                字段 componentType 可选值：Input/Textarea/Number/InputNumber/Select/Radio/Checkbox/
                DatePicker/DateTimePicker/TimePicker/Switch/UploadFile/UploadImg/Table/Cascader/Rate/Slider/Editor

                只输出 JSON，不要 markdown。禁止发明不在 ai_entity_field 中的实体或字段名。
                """,
            Messages = new List<ChatMessage>
            {
                new("user",
                    $"需求: {context.UserRequirement}\n" +
                    $"ai_entity_field（唯一字段源）:\n{fieldContext}\n" +
                    $"IR 摘要: {context.PromptContext.CompressedSummary}"),
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

    private static string BuildFieldContext(IReadOnlyList<AiEntityFieldEntity> fields)
    {
        var sb = new StringBuilder();
        foreach (var g in fields.GroupBy(f => f.EntityName))
        {
            sb.AppendLine($"实体 {g.Key} (表 {g.First().TableName}):");
            foreach (var f in g)
                sb.AppendLine($"  - {f.FieldName} ({f.CSharpType}, required={f.IsRequired})");
        }
        return sb.ToString();
    }
}
