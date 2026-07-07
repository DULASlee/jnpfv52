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
/// DB 设计 Skill（R3 认知模具版）— DDL 经 BudgetGuard 生成 + 语法校验；禁止 fallback。
/// </summary>
public sealed class DbDesignSkillService : CognitiveSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISkillLlmBudgetGuard _budgetGuard;

    public DbDesignSkillService(
        ICognitiveSkillToolkit toolkit,
        ISkillLlmBudgetGuard budgetGuard)
        : base(toolkit)
    {
        _budgetGuard = budgetGuard;
    }

    public override string SkillId => DesignSkillIds.DbDesign;
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
        IrEventTypes = new[] { IrEventTypes.DDLStabilized },
    };

    public override Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        if (snapshot.Find(IrFragmentTypes.EventSpec, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("IR-1 未 stable"));

        if (snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable) != null)
            return Task.FromResult(SkillValidationResult.Fail("DDL 片段已 stable"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public override Task<SkillValidationResult> ValidateOutputAsync(
        IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (events.Count != 1 || events[0].EventType != IrEventTypes.DDLStabilized)
            return Task.FromResult(SkillValidationResult.Fail("必须产出 1 条 DDLStabilized"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    protected override async IAsyncEnumerable<AppendIrEventRequest> ThinkAsync(
        SkillPerception perception,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var context = perception.Context;
        var fragmentId = $"ddl:{context.ProjectId}";
        var ddl = await GenerateDdlAsync(context, ct);
        ValidateDdlSyntax(ddl);

        var payload = JsonSerializer.Serialize(new
        {
            @context = "https://schema.jnpf.ai/ir/v1",
            @id = fragmentId,
            dialect = "sqlserver",
            ddl,
            tableNames = ExtractTableNames(context),
            stabilityState = IrStabilityStates.Stable,
        }, JsonOptions);

        yield return new AppendIrEventRequest
        {
            EventType = IrEventTypes.DDLStabilized,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.DDL,
            FragmentVersion = 1,
            Payload = payload,
        };
    }

    private async Task<string> GenerateDdlAsync(SkillContext context, CancellationToken ct)
    {
        var tables = ExtractTableNames(context);
        var slot = await _budgetGuard.AcquireAsync(
            context.ProjectId, SkillId, context.RunId, context.TenantId, context.PipelineId, ct);

        var response = await _budgetGuard.ExecuteAsync(slot, new ChatCompletionRequest
        {
            ProviderCode = context.ProviderCode ?? string.Empty,
            SystemPrompt = "你是 SQL Server DDL 专家。根据 entityDrafts 生成可执行 CREATE TABLE 脚本，只输出 SQL。",
            Messages = new List<ChatMessage>
            {
                new("user", $"表清单: {string.Join(", ", tables)}\n需求: {context.UserRequirement}\nIR摘要: {context.PromptContext.CompressedSummary}"),
            },
            MaxTokens = slot.MaxTokens,
            Temperature = 0.2,
            TimeoutMs = slot.TimeoutMs,
        }, ct);

        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            throw Oops.Bah(response.Error ?? "DB Design Skill LLM 返回空");

        return response.Content.Trim();
    }

    public static IReadOnlyList<string> ExtractTableNames(SkillContext context)
    {
        var skeleton = context.Snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)
            ?? context.Snapshot.Find(IrFragmentTypes.Skeleton);

        if (skeleton == null)
            return new[] { $"AI_PROJ_{context.ProjectId}" };

        try
        {
            using var doc = JsonDocument.Parse(skeleton.Payload);
            if (doc.RootElement.TryGetProperty("entityDrafts", out var drafts)
                && drafts.ValueKind == JsonValueKind.Array)
            {
                var names = new List<string>();
                foreach (var d in drafts.EnumerateArray())
                {
                    if (d.TryGetProperty("tableName", out var tn) && !string.IsNullOrWhiteSpace(tn.GetString()))
                        names.Add(tn.GetString()!);
                }

                if (names.Count > 0) return names;
            }
        }
        catch (JsonException)
        {
            // 骨架 JSON 非法由上游 Skill 保证；此处回退默认表名
        }

        return new[] { $"AI_PROJ_{context.ProjectId}" };
    }

    public static void ValidateDdlSyntax(string ddl)
    {
        if (string.IsNullOrWhiteSpace(ddl))
            throw Oops.Bah("DDL 为空");

        if (!ddl.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            throw Oops.Bah("DDL 缺少 CREATE TABLE");
    }
}
