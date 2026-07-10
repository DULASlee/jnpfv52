using System.Runtime.CompilerServices;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Entitys.Ir.Contracts;
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
    private readonly EntityDesignRepository _entityDesignRepo;

    public DbDesignSkillService(
        ICognitiveSkillToolkit toolkit,
        ISkillLlmBudgetGuard budgetGuard,
        EntityDesignRepository entityDesignRepo)
        : base(toolkit)
    {
        _budgetGuard = budgetGuard;
        _entityDesignRepo = entityDesignRepo;
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
            return Task.FromResult(SkillValidationResult.Fail("IR-1 EventSpec 未 stable（设计启动另须 Finalize + ai_entity_field）"));

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
        var projection = EntityDesignProjector.Project(context.Snapshot, new EntityDesignProjectionOptions
        {
            TenantId = context.TenantId,
            ProjectId = context.ProjectId,
            PipelineId = context.PipelineId.ToString(),
        });

        // 宪法三：投影无字段 = 显式失败，禁止 AI_PROJ_ 静默兜底
        if (projection.Fields.Count == 0)
            throw Oops.Bah("DbDesign Skill: Skeleton 投影产出 0 个字段（Skeleton 缺失或无 entityDrafts），拒绝继续");

        // P9-S5：投影持久化到 ai_entity_field（CQRS Read Model），先于 DDL 生成落库
        await _entityDesignRepo.PersistAsync(projection, ct);

        var tableNames = projection.TableNames();
        var (ddl, structuredTables) = await GenerateDdlAsync(context, projection, ct);
        ValidateDdlSyntax(ddl);

        var payload = JsonSerializer.Serialize(new
        {
            @context = "https://schema.jnpf.ai/ir/v1",
            @id = fragmentId,
            dialect = "sqlserver",
            ddl,                              // 向后兼容：保留原始 SQL
            tables = structuredTables,         // P9-S1 新增：结构化表定义（编译器直接消费）
            tableNames,                        // 从确定性投影派生（不再用 ExtractTableNames）
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

    /// <summary>P9-S1：返回 (rawSql, structuredTablesJson) 双产出。宪法二：LLM 只精化不增删。</summary>
    private async Task<(string ddl, string structuredTables)> GenerateDdlAsync(
        SkillContext context, EntityDesignProjection projection, CancellationToken ct)
    {
        // 宪法二：实体和字段清单作为不可变输入注入 prompt，LLM 只精化 SQL 属性
        var entityContext = BuildEntityContextForPrompt(projection);

        var slot = await _budgetGuard.AcquireAsync(
            context.ProjectId, SkillId, context.RunId, context.TenantId, context.PipelineId, ct);

        var response = await _budgetGuard.ExecuteAsync(slot, new ChatCompletionRequest
        {
            ProviderCode = context.ProviderCode ?? string.Empty,
            SystemPrompt = """
                你是 SQL Server DDL 专家。根据给定的实体和字段清单（不可变输入），生成 JSON：
                1. "ddl": 可执行的 CREATE TABLE 脚本（含主键、外键、索引）
                2. "tables": 结构化表定义数组，每项含 tableName/entityName/columns[]/foreignKeys[]/indexes[]

                约束（违反则拒绝）：
                - 不得增删实体（tables 数量必须与输入实体清单一致）
                - 不得增删字段（columns 必须与输入字段一一对应）
                - 只允许精化：补充 SQL 数据类型、IDENTITY、NOT NULL、外键、索引

                tables 结构示例：
                {"ddl": "CREATE TABLE ...", "tables": [
                  {"tableName": "LeaveRequest", "entityName": "LeaveRequest",
                   "columns": [{"name": "id", "dataType": "BIGINT", "primaryKey": true, "nullable": false, "identity": true},
                               {"name": "employeeId", "dataType": "BIGINT", "nullable": false}],
                   "foreignKeys": [{"column": "employeeId", "referencesTable": "Employee", "referencesColumn": "id"}],
                   "indexes": [{"name": "IX_emp", "columns": ["employeeId"], "unique": false}]}]}

                只输出 JSON，不要 markdown。
                """,
            Messages = new List<ChatMessage>
            {
                new("user", $"实体与字段清单（不可变输入）:\n{entityContext}\n\n需求: {context.UserRequirement}\nIR摘要: {context.PromptContext.CompressedSummary}"),
            },
            ResponseFormat = "json",
            MaxTokens = slot.MaxTokens,
            Temperature = 0.2,
            TimeoutMs = slot.TimeoutMs,
        }, ct);

        if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            throw Oops.Bah(response.Error ?? "DB Design Skill LLM 返回空");

        // P9-S1：解析双产出（ddl + tables）
        var rawContent = PmSkillService.ExtractJson(response.Content.Trim());
        try
        {
            using var doc = JsonDocument.Parse(rawContent);
            var ddl = doc.RootElement.TryGetProperty("ddl", out var ddlEl)
                ? ddlEl.GetString() ?? rawContent : rawContent;
            var tables = doc.RootElement.TryGetProperty("tables", out var tablesEl)
                ? tablesEl.GetRawText() : "[]";
            return (ddl, tables);
        }
        catch
        {
            // 兜底：LLM 只输出了 SQL 文本，无 JSON 包装
            return (rawContent, "[]");
        }
    }

    /// <summary>宪法二：把投影的实体+字段清单序列化为 LLM 不可变输入。</summary>
    private static string BuildEntityContextForPrompt(EntityDesignProjection projection)
    {
        var entities = projection.Fields
            .GroupBy(f => f.EntityName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                entityName = g.Key,
                tableName = g.First().TableName,
                displayName = g.First().EntityDisplayName,
                fields = g.Select(f => new
                {
                    name = f.FieldName,
                    csharpType = f.CSharpType,
                    isPrimaryKey = f.IsPrimaryKey,
                    isRequired = f.IsRequired,
                    references = f.References,
                }),
            });
        return JsonSerializer.Serialize(entities, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        });
    }

    public static void ValidateDdlSyntax(string ddl)
    {
        if (string.IsNullOrWhiteSpace(ddl))
            throw Oops.Bah("DDL 为空");

        if (!ddl.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
            throw Oops.Bah("DDL 缺少 CREATE TABLE");
    }
}
