using System.Text.Json;
using JNPF.Engine.Entity.Model.CodeGen;
using JNPF.InteAssistant.Codegen.EntityDesign;
using JNPF.InteAssistant.Codegen.TemplateContext;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Entitys.Ir.Naming;
using JNPF.InteAssistant.Skills;
using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// 从 IR-2 快照构建 <see cref="Ir2CodegenContext"/>（A5 TemplateContext 契约）。
///
/// P9-S4：列定义来源从 DDL 正则解析切换为 EntityDesignProjector 确定性投影。
/// 消灭 ParseColumnsFromDdl/BuildColumnsFromFormPage/BuildMinimalLeaveColumns 三套兜底。
/// </summary>
public sealed class TemplateContextBuilder : ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public Ir2CodegenContext Build(IrSnapshot snapshot, Ir2CodegenBuildOptions options)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(options);

        if (options.StrictMode)
            ValidateIr2Fragments(snapshot, options);

        var skeleton = snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.Skeleton);
        var ddl = snapshot.Find(IrFragmentTypes.DDL, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.DDL);
        var architecture = snapshot.Find(IrFragmentTypes.Architecture, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.Architecture);

        var entityName = ResolveEntityName(skeleton, options);
        var tableName = ResolveTableName(skeleton, ddl, options, entityName);
        var nameSpace = ResolveNameSpace(architecture, options, entityName);

        // P9-S4：列定义从 EntityDesignProjector 确定性投影获取（契约主权 — 宪法一）
        List<TableColumnConfigModel> columns;
        if (options.Projection != null)
        {
            var entityFields = options.Projection.ForEntity(entityName);
            columns = MapProjectionToColumns(entityFields, options);
        }
        else
        {
            columns = new List<TableColumnConfigModel>();
        }

        // 宪法三：投影无字段 = 显式失败（无兜底）
        if (columns.Count == 0)
        {
            if (options.StrictMode)
                throw new TemplateContextBuildException(
                    $"[{options.SampleId}] 实体 '{entityName}' 的投影无字段定义（Skeleton entityDrafts 可能缺少 fields 或 DDL 未解析出列）");

            // 非严格模式：构建仅含主键的最小列集合
            columns = BuildMinimalFallbackColumns(options);
        }

        var primary = columns.FirstOrDefault(c => c.PrimaryKey)
            ?? throw new TemplateContextBuildException($"[{options.SampleId}] 投影未定义主键列（实体 '{entityName}'）");

        return new Ir2CodegenContext
        {
            ProjectId = options.ProjectId,
            TenantId = options.TenantId,
            SampleId = options.SampleId,
            TemplateProfileId = options.TemplateProfileId ?? VmTemplateIds.ProfileSingleTable,
            NameSpace = nameSpace,
            ClassName = entityName,
            BusName = options.BusName ?? Ir2CodegenDefaults.FallbackBusName,
            OriginalMainTableName = tableName,
            PrimaryKey = primary.ColumnName,
            OriginalPrimaryKey = primary.OriginalColumnName,
            PrimaryKeyPolicy = options.PrimaryKeyPolicy,
            WebType = options.WebType,
            Type = options.Type,
            EnableFlow = options.EnableFlow,
            IsTenantColumn = options.IsTenantColumn,
            IsLogicalDelete = options.IsLogicalDelete,
            ConcurrencyLock = options.ConcurrencyLock,
            TableField = columns,
            Function = BuildDefaultFunctions(options),
        };
    }

    public Ir2CodegenContext BuildFromSampleJson(string sampleJsonPath)
    {
        var json = File.ReadAllText(sampleJsonPath);
        var fixture = JsonSerializer.Deserialize<Ir2SampleFixture>(json, JsonOptions)
            ?? throw new InvalidOperationException($"样本 JSON 无效: {sampleJsonPath}");

        var fragments = fixture.Fragments.Select(f => new IrSnapshotFragment
        {
            FragmentId = f.FragmentId,
            FragmentType = f.FragmentType,
            StabilityState = f.StabilityState ?? IrStabilityStates.Stable,
            Payload = f.Payload.ValueKind == JsonValueKind.String
                ? f.Payload.GetString() ?? "{}"
                : f.Payload.GetRawText(),
        }).ToList();

        var snapshot = new IrSnapshot { Fragments = fragments };
        var defaults = fixture.Defaults ?? new Ir2SampleDefaults();

        // P9-S4：样本 JSON 也走确定性投影
        var projectionOptions = new EntityDesignProjectionOptions
        {
            TenantId = defaults.TenantId ?? "000000",
            ProjectId = defaults.ProjectId ?? fixture.SampleId,
            PipelineId = "0", // 样本数据无真实 pipeline
        };

        return Build(snapshot, new Ir2CodegenBuildOptions
        {
            ProjectId = defaults.ProjectId ?? fixture.SampleId,
            TenantId = defaults.TenantId ?? "000000",
            SampleId = fixture.SampleId,
            TemplateProfileId = fixture.TemplateProfileId ?? VmTemplateIds.ProfileSingleTable,
            NameSpace = defaults.NameSpace,
            ClassName = defaults.ClassName,
            BusName = defaults.BusName,
            TableName = defaults.TableName,
            EnableFlow = defaults.EnableFlow,
            IsTenantColumn = defaults.IsTenantColumn,
            PrimaryKeyPolicy = defaults.PrimaryKeyPolicy,
            WebType = defaults.WebType,
            Type = defaults.Type,
            IsLogicalDelete = defaults.IsLogicalDelete,
            ConcurrencyLock = defaults.ConcurrencyLock,
            StrictMode = true,
            Projection = EntityDesignProjector.Project(snapshot, projectionOptions),
        });
    }

    /// <summary>从 Skill 上下文构建 TemplateContext（IR-2 stable 片段 → Ir2CodegenContext）。</summary>
    public Ir2CodegenContext BuildFromSkillContext(SkillContext context)
    {
        var projectionOptions = new EntityDesignProjectionOptions
        {
            TenantId = context.TenantId,
            ProjectId = context.ProjectId,
            PipelineId = context.PipelineId.ToString(),
        };

        return Build(context.Snapshot, new Ir2CodegenBuildOptions
        {
            ProjectId = context.ProjectId,
            TenantId = context.TenantId,
            SampleId = context.ProjectId,
            StrictMode = true,
            Projection = EntityDesignProjector.Project(context.Snapshot, projectionOptions),
        });
    }

    /// <summary>
    /// P9-S2：多实体编译入口 — 遍历 skeleton 所有 entityDrafts，为每个实体构建 Ir2CodegenContext。
    /// 替代单实体的 BuildFromSkillContext（保留向后兼容）。
    /// 这是确定性编译器的核心：从 IR 派生所有实体的代码生成上下文，零 LLM。
    ///
    /// P9-S4：投影计算提升到循环外（一次 Project，N 次 ForEntity）。
    /// P9-S5：支持外部预计算投影（DeveloperSkillService 计算一次 → 持久化 + 编译复用）。
    /// </summary>
    /// <param name="context">Skill 上下文</param>
    /// <param name="prebuiltProjection">
    /// 可选：外部预计算的投影。不为 null 时跳过内部计算（DeveloperSkillService 计算一次后
    /// 先持久化到 ai_entity_field，再传给此处编译复用）。
    /// </param>
    public IReadOnlyList<Ir2CodegenContext> BuildAllFromSkillContext(
        SkillContext context,
        EntityDesignProjection? prebuiltProjection = null)
    {
        var skeleton = context.Snapshot.Find(IrFragmentTypes.Skeleton, IrStabilityStates.Stable)
            ?? context.Snapshot.Find(IrFragmentTypes.Skeleton);

        if (skeleton == null)
            throw new TemplateContextBuildException($"[{context.ProjectId}] 缺少 Skeleton，无法多实体编译");

        // P9-S4/S5：一次投影，多次消费。外部预计算优先（避免重复解析 Skeleton+DDL）
        var projection = prebuiltProjection;
        if (projection == null)
        {
            var projectionOptions = new EntityDesignProjectionOptions
            {
                TenantId = context.TenantId,
                ProjectId = context.ProjectId,
                PipelineId = context.PipelineId.ToString(),
            };
            projection = EntityDesignProjector.Project(context.Snapshot, projectionOptions);
        }

        // 解析所有 entityDrafts
        List<(string entityName, string tableName)> entities;
        try
        {
            using var doc = JsonDocument.Parse(skeleton.Payload);
            entities = new List<(string, string)>();
            if (doc.RootElement.TryGetProperty("entityDrafts", out var drafts) && drafts.ValueKind == JsonValueKind.Array)
            {
                foreach (var d in drafts.EnumerateArray())
                {
                    var entityName = GetString(d, "entityName") ?? "";
                    var tableName = GetString(d, "tableName") ?? "";
                    if (!string.IsNullOrWhiteSpace(entityName))
                        entities.Add((entityName, tableName));
                }
            }
        }
        catch (JsonException)
        {
            throw new TemplateContextBuildException($"[{context.ProjectId}] Skeleton 解析失败，无法多实体编译");
        }

        if (entities.Count == 0)
            throw new TemplateContextBuildException($"[{context.ProjectId}] Skeleton 无 entityDrafts，无法多实体编译");

        // 为每个实体构建上下文（复用 Build，传入 ClassName/TableName 避免取 drafts[0]）
        var contexts = new List<Ir2CodegenContext>();
        foreach (var (entityName, tableName) in entities)
        {
            try
            {
                var ctx = Build(context.Snapshot, new Ir2CodegenBuildOptions
                {
                    ProjectId = context.ProjectId,
                    TenantId = context.TenantId,
                    SampleId = context.ProjectId,
                    StrictMode = false,  // 多实体模式宽容：单个实体缺列不阻断整体
                    ClassName = entityName,
                    TableName = !string.IsNullOrWhiteSpace(tableName) ? tableName : EntityNamingPolicy.ToSnakeUpper(entityName),
                    BusName = entityName,
                    Projection = projection,  // P9-S4：复用循环外投影
                });
                contexts.Add(ctx);
            }
            catch (TemplateContextBuildException)
            {
                // 单实体失败不阻断整体（跳过该实体）
                // 实际场景应由 IrCompletenessGate 在编译前拦截并报告缺口
            }
        }

        return contexts;
    }

    #region Column mapping (P9-S4: projection → TableColumnConfigModel)

    /// <summary>
    /// 将 EntityDesignProjection 中指定实体的字段映射为 <see cref="TableColumnConfigModel"/> 列表。
    /// 这是 DDL 正则解析的唯一替代路径（宪法一：唯一解析器）。
    /// </summary>
    private static List<TableColumnConfigModel> MapProjectionToColumns(
        IReadOnlyList<EntityFieldDesign> entityFields,
        Ir2CodegenBuildOptions options)
    {
        var columns = new List<TableColumnConfigModel>(entityFields.Count);

        foreach (var field in entityFields)
        {
            // 跳过工作流列（模板自行注入）
            if (options.EnableFlow && IsFlowColumn(field.FieldName, field.DbColumnName))
                continue;

            // 跳过租户列（模板自行注入）
            if (options.IsTenantColumn && IsTenantColumnName(field.FieldName, field.DbColumnName))
                continue;

            columns.Add(new TableColumnConfigModel
            {
                ColumnName = field.PropertyName,
                OriginalColumnName = field.DbColumnName,
                ColumnComment = field.FieldDescription ?? field.FieldName,
                NetType = field.CSharpType,
                PrimaryKey = field.IsPrimaryKey,
                jnpfKey = field.IsPrimaryKey ? null : "input",
                IsImportField = false,
            });
        }

        return columns;
    }

    private static bool IsFlowColumn(string fieldName, string dbColumnName)
    {
        return string.Equals(fieldName, "FlowId", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fieldName, "FlowTaskId", StringComparison.OrdinalIgnoreCase)
            || string.Equals(dbColumnName, "F_Flow_Id", StringComparison.OrdinalIgnoreCase)
            || string.Equals(dbColumnName, "F_Flow_Task_Id", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTenantColumnName(string fieldName, string dbColumnName)
    {
        return string.Equals(fieldName, "TenantId", StringComparison.OrdinalIgnoreCase)
            || string.Equals(dbColumnName, "F_TenantId", StringComparison.OrdinalIgnoreCase)
            || string.Equals(dbColumnName, "F_Tenant_Id", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 非严格模式兜底：构建仅含主键的最小列集合。
    /// 不再硬编码"请假"实体（消灭 BuildMinimalLeaveColumns）。
    /// </summary>
    private static List<TableColumnConfigModel> BuildMinimalFallbackColumns(Ir2CodegenBuildOptions options)
    {
        var columns = new List<TableColumnConfigModel>
        {
            new()
            {
                ColumnName = "Id",
                OriginalColumnName = "F_Id",
                ColumnComment = "主键",
                NetType = "string",
                PrimaryKey = true,
            },
        };

        if (options.IsTenantColumn)
        {
            columns.Add(new TableColumnConfigModel
            {
                ColumnName = "TenantId",
                OriginalColumnName = "F_TenantId",
                ColumnComment = "租户",
                NetType = "string",
            });
        }

        if (options.EnableFlow)
        {
            columns.Add(new TableColumnConfigModel
            {
                ColumnName = "FlowId",
                OriginalColumnName = "F_Flow_Id",
                ColumnComment = "流程引擎ID",
                NetType = "string",
            });
            columns.Add(new TableColumnConfigModel
            {
                ColumnName = "FlowTaskId",
                OriginalColumnName = "F_Flow_Task_Id",
                ColumnComment = "流程任务ID",
                NetType = "string",
            });
        }

        return columns;
    }

    #endregion

    #region Helpers (unchanged from original, kept for JSON/name resolution)

    private static string? GetString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
    }

    private static void ValidateIr2Fragments(IrSnapshot snapshot, Ir2CodegenBuildOptions options)
    {
        RequireFragment(snapshot, IrFragmentTypes.Skeleton, IrStabilityStates.Stable, options.SampleId);
        RequireFragment(snapshot, IrFragmentTypes.Architecture, IrStabilityStates.Stable, options.SampleId);
        RequireFragment(snapshot, IrFragmentTypes.DDL, IrStabilityStates.Stable, options.SampleId);
        RequireFragment(snapshot, IrFragmentTypes.FormPageIR, IrStabilityStates.Stable, options.SampleId);
    }

    private static void RequireFragment(
        IrSnapshot snapshot,
        string fragmentType,
        string minStability,
        string sampleId)
    {
        if (snapshot.Find(fragmentType, minStability) == null)
            throw new TemplateContextBuildException($"[{sampleId}] 缺少 stable 的 {fragmentType} 片段，SystemDesignLocked 前置条件未满足");
    }

    private static string ResolveEntityName(IrSnapshotFragment? skeleton, Ir2CodegenBuildOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ClassName))
            return options.ClassName!;

        if (skeleton != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(skeleton.Payload);
                if (doc.RootElement.TryGetProperty("entityDrafts", out var drafts)
                    && drafts.ValueKind == JsonValueKind.Array
                    && drafts.GetArrayLength() > 0)
                {
                    var first = drafts[0];
                    if (first.TryGetProperty("entityName", out var en) && !string.IsNullOrWhiteSpace(en.GetString()))
                        return en.GetString()!;
                }
            }
            catch (JsonException)
            {
                // ignore parse errors
            }
        }

        if (options.StrictMode)
            throw new TemplateContextBuildException($"[{options.SampleId}] 无法解析 entityName：Skeleton 无 entityDrafts 且未提供 ClassName");

        return "LeaveRequest";
    }

    private static string ResolveTableName(
        IrSnapshotFragment? skeleton,
        IrSnapshotFragment? ddl,
        Ir2CodegenBuildOptions options,
        string entityName)
    {
        if (!string.IsNullOrWhiteSpace(options.TableName))
            return options.TableName!;

        if (ddl != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(ddl.Payload);
                if (doc.RootElement.TryGetProperty("tableNames", out var names)
                    && names.ValueKind == JsonValueKind.Array
                    && names.GetArrayLength() > 0)
                {
                    return names[0].GetString() ?? $"OA_{entityName.ToUpperInvariant()}";
                }
            }
            catch (JsonException)
            {
                // ignore
            }
        }

        if (skeleton != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(skeleton.Payload);
                if (doc.RootElement.TryGetProperty("entityDrafts", out var drafts)
                    && drafts.ValueKind == JsonValueKind.Array
                    && drafts.GetArrayLength() > 0)
                {
                    var first = drafts[0];
                    if (first.TryGetProperty("tableName", out var tn) && !string.IsNullOrWhiteSpace(tn.GetString()))
                        return tn.GetString()!;
                }
            }
            catch (JsonException)
            {
                // ignore
            }
        }

        if (options.StrictMode)
            throw new TemplateContextBuildException($"[{options.SampleId}] 无法解析表名：DDL/Skeleton 均无 tableName");

        return $"OA_{entityName.ToUpperInvariant()}";
    }

    private static string ResolveNameSpace(IrSnapshotFragment? architecture, Ir2CodegenBuildOptions options, string entityName)
    {
        if (!string.IsNullOrWhiteSpace(options.NameSpace))
            return options.NameSpace!;

        if (architecture != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(architecture.Payload);
                if (doc.RootElement.TryGetProperty("modules", out var modules)
                    && modules.ValueKind == JsonValueKind.Array)
                {
                    foreach (var module in modules.EnumerateArray())
                    {
                        var ns = ExtractModuleNameSpace(module);
                        if (!string.IsNullOrWhiteSpace(ns))
                            return ns;
                    }
                }
            }
            catch (JsonException)
            {
                // ignore
            }
        }

        if (options.StrictMode)
            throw new TemplateContextBuildException($"[{options.SampleId}] 无法解析 NameSpace：Architecture 无 modules 且未提供 NameSpace");

        return entityName.Contains("Leave", StringComparison.OrdinalIgnoreCase) ? "OaLeave" : "Generated";
    }

    private static string? ExtractModuleNameSpace(JsonElement module)
    {
        if (module.ValueKind == JsonValueKind.String)
        {
            var raw = module.GetString();
            return string.IsNullOrWhiteSpace(raw) ? null : ToPascalNameSpace(raw);
        }

        if (module.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var prop in new[] { "name", "Name", "moduleName", "ModuleName", "code", "Code" })
        {
            if (module.TryGetProperty(prop, out var name) && name.ValueKind == JsonValueKind.String)
            {
                var raw = name.GetString();
                if (!string.IsNullOrWhiteSpace(raw))
                    return ToPascalNameSpace(raw);
            }
        }

        return null;
    }

    private static string ToPascalNameSpace(string raw)
    {
        var parts = raw.Split(new[] { '-', '_', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "Generated";

        return string.Concat(parts.Select(p =>
            p.Length == 1
                ? char.ToUpperInvariant(p[0]).ToString()
                : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    private static IReadOnlyList<CodeGenFunctionModel> BuildDefaultFunctions(Ir2CodegenBuildOptions options)
    {
        _ = options;
        return new List<CodeGenFunctionModel>
        {
            new() { FullName = "GetList", orderBy = 1 },
            new() { FullName = "GetInfo", orderBy = 2 },
            new() { FullName = "Create", orderBy = 3 },
            new() { FullName = "Update", orderBy = 4 },
            new() { FullName = "Delete", orderBy = 5 },
        };
    }

    #endregion
}

public sealed class Ir2CodegenBuildOptions
{
    public required string ProjectId { get; init; }
    public required string TenantId { get; init; }
    public required string SampleId { get; init; }
    public string? TemplateProfileId { get; init; }
    public string? NameSpace { get; init; }
    public string? ClassName { get; init; }
    public string? BusName { get; init; }
    public string? TableName { get; init; }
    public int WebType { get; init; } = 2;
    public int Type { get; init; } = 1;
    public bool EnableFlow { get; init; }
    public bool IsTenantColumn { get; init; } = true;
    public int PrimaryKeyPolicy { get; init; } = 1;
    public bool IsLogicalDelete { get; init; }
    public bool ConcurrencyLock { get; init; }

    /// <summary>默认 true：缺 IR-2 片段时 TemplateContextBuildException（API 层映射 Oops.Bah）。</summary>
    public bool StrictMode { get; init; } = true;

    /// <summary>
    /// P9-S4：确定性投影（EntityDesignProjector.Project 输出）。
    /// 列定义不再从 DDL 正则解析，统一从此投影读取。
    /// null = 未计算投影（向后兼容；StrictMode 下将失败）。
    /// </summary>
    public EntityDesignProjection? Projection { get; init; }
}

internal sealed class Ir2SampleFixture
{
    public string SampleId { get; set; } = string.Empty;
    public string? TemplateProfileId { get; set; }
    public Ir2SampleDefaults? Defaults { get; set; }
    public List<Ir2SampleFragment> Fragments { get; set; } = new();
}

internal sealed class Ir2SampleDefaults
{
    public string? ProjectId { get; set; }
    public string? TenantId { get; set; }
    public string? NameSpace { get; set; }
    public string? ClassName { get; set; }
    public string? BusName { get; set; }
    public string? TableName { get; set; }
    public int WebType { get; set; } = 2;
    public int Type { get; set; } = 1;
    public bool EnableFlow { get; set; }
    public bool IsTenantColumn { get; set; } = true;
    public int PrimaryKeyPolicy { get; set; } = 1;
    public bool IsLogicalDelete { get; set; }
    public bool ConcurrencyLock { get; set; }
}

internal sealed class Ir2SampleFragment
{
    public string FragmentId { get; set; } = string.Empty;
    public string FragmentType { get; set; } = string.Empty;
    public string? StabilityState { get; set; }
    public JsonElement Payload { get; set; }
}
