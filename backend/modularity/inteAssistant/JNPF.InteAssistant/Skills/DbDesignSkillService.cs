using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Llm;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// DB 设计 Skill MVP（P3-B03）— DDL 生成 + 语法骨架校验
/// </summary>
public sealed class DbDesignSkillService : IBaseSkill, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISkillLlmBudgetGuard _budgetGuard;
    private readonly ILogger<DbDesignSkillService> _logger;

    public DbDesignSkillService(ISkillLlmBudgetGuard budgetGuard, ILogger<DbDesignSkillService> logger)
    {
        _budgetGuard = budgetGuard;
        _logger = logger;
    }

    public string SkillId => DesignSkillIds.DbDesign;
    public string Version => "1.0.0-mvp";

    public SkillInformationNeeds InformationNeeds { get; } = new()
    {
        IrFragmentTypes = new[] { IrFragmentTypes.EventSpec },
        RequiredStability = IrStabilityStates.Stable,
    };

    public SkillOutputDeclaration Outputs { get; } = new()
    {
        IrEventTypes = new[] { IrEventTypes.DDLStabilized },
    };

    public Task<SkillValidationResult> ValidateInputAsync(IrSnapshot snapshot, CancellationToken ct = default)
    {
        if (snapshot.Find(IrFragmentTypes.EventSpec, IrStabilityStates.Stable) == null)
            return Task.FromResult(SkillValidationResult.Fail("IR-1 未 stable"));

        if (snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable) != null)
            return Task.FromResult(SkillValidationResult.Fail("DDL 片段已 stable"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    public async IAsyncEnumerable<AppendIrEventRequest> ReasonAsync(
        SkillContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var fragmentId = $"ddl:{context.ProjectId}";
        string ddl;

        try
        {
            ddl = await GenerateDdlAsync(context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DB Design Skill LLM 失败，使用规则回退");
            ddl = BuildFallbackDdl(context);
        }

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
            SkillId = SkillId,
        };
    }

    public Task<SkillValidationResult> ValidateOutputAsync(IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct = default)
    {
        if (events.Count != 1 || events[0].EventType != IrEventTypes.DDLStabilized)
            return Task.FromResult(SkillValidationResult.Fail("必须产出 1 条 DDLStabilized"));

        return Task.FromResult(SkillValidationResult.Ok());
    }

    private async Task<string> GenerateDdlAsync(SkillContext context, CancellationToken ct)
    {
        var tables = ExtractTableNames(context);
        var slot = await _budgetGuard.AcquireAsync(
            context.ProjectId, SkillId, context.RunId, context.TenantId, context.PipelineId, ct);

        var response = await _budgetGuard.ExecuteAsync(slot, new ChatCompletionRequest
        {
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
            throw new InvalidOperationException(response.Error ?? "LLM 返回空");

        return response.Content.Trim();
    }

    private static string BuildFallbackDdl(SkillContext context)
    {
        var sb = new StringBuilder();
        foreach (var table in ExtractTableNames(context))
        {
            sb.AppendLine($"CREATE TABLE [dbo].[{table}] (");
            sb.AppendLine("    [F_Id] NVARCHAR(50) NOT NULL PRIMARY KEY,");
            sb.AppendLine("    [F_TenantId] NVARCHAR(50) NOT NULL,");
            sb.AppendLine("    [F_CreatorTime] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE()");
            sb.AppendLine(");");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static IReadOnlyList<string> ExtractTableNames(SkillContext context)
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
        catch
        {
            // ignore parse errors
        }

        return new[] { $"AI_PROJ_{context.ProjectId}" };
    }

    private static void ValidateDdlSyntax(string ddl)
    {
        if (string.IsNullOrWhiteSpace(ddl))
            throw new InvalidOperationException("DDL 为空");

        if (!ddl.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("DDL 缺少 CREATE TABLE");
    }
}
