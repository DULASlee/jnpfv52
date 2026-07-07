using System.Text.Json;
using System.Text.RegularExpressions;
using JNPF.Engine.Entity.Model.CodeGen;
using JNPF.InteAssistant.Codegen.TemplateContext;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills;
using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Codegen;

/// <summary>
/// 从 IR-2 快照构建 <see cref="Ir2CodegenContext"/>（A5 TemplateContext 契约）。
/// </summary>
public sealed class TemplateContextBuilder : ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Regex ColumnLineRegex = new(
        @"\[\s*(?<col>F_\w+)\s*\]\s+(?<sqlType>\w+)(?<rest>[^,\n]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
        var formPage = snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.FormPageIR);
        var architecture = snapshot.Find(IrFragmentTypes.Architecture, IrStabilityStates.Stable)
            ?? snapshot.Find(IrFragmentTypes.Architecture);

        var entityName = ResolveEntityName(skeleton, options);
        var tableName = ResolveTableName(skeleton, ddl, options, entityName);
        var nameSpace = ResolveNameSpace(architecture, options, entityName);
        var ddlText = ExtractDdlText(ddl);
        var fieldLabels = ExtractFormFieldLabels(formPage);

        if (options.StrictMode && string.IsNullOrWhiteSpace(ddlText))
            throw new TemplateContextBuildException($"[{options.SampleId}] IR2_DDL 片段缺少 ddl 文本，无法构建 TemplateContext");

        var columns = ParseColumnsFromDdl(ddlText, fieldLabels, options);
        if (columns.Count == 0)
            columns = BuildColumnsFromFormPage(fieldLabels, options);

        if (columns.Count == 0)
        {
            if (options.StrictMode)
                throw new TemplateContextBuildException($"[{options.SampleId}] 无法从 DDL/FormPageIR 解析任何列定义");

            columns = BuildMinimalLeaveColumns(options);
        }

        var primary = columns.FirstOrDefault(c => c.PrimaryKey)
            ?? throw new TemplateContextBuildException($"[{options.SampleId}] DDL 未定义主键列");

        return new Ir2CodegenContext
        {
            ProjectId = options.ProjectId,
            TenantId = options.TenantId,
            SampleId = options.SampleId,
            TemplateProfileId = options.TemplateProfileId ?? VmTemplateIds.ProfileSingleTable,
            NameSpace = nameSpace,
            ClassName = entityName,
            BusName = options.BusName ?? "请假申请",
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
        });
    }

    /// <summary>从 Skill 上下文构建 TemplateContext（IR-2 stable 片段 → Ir2CodegenContext）。</summary>
    public Ir2CodegenContext BuildFromSkillContext(SkillContext context)
    {
        return Build(context.Snapshot, new Ir2CodegenBuildOptions
        {
            ProjectId = context.ProjectId,
            TenantId = context.TenantId,
            SampleId = context.ProjectId,
            StrictMode = true,
        });
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
            catch
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
            catch
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
            catch
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
            catch
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

    private static string ExtractDdlText(IrSnapshotFragment? ddl)
    {
        if (ddl == null) return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(ddl.Payload);
            if (doc.RootElement.TryGetProperty("ddl", out var ddlProp))
                return NormalizeDdlText(ddlProp.GetString() ?? string.Empty);
        }
        catch
        {
            // ignore
        }

        return string.Empty;
    }

    private static string NormalizeDdlText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return trimmed;

        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline >= 0)
            trimmed = trimmed[(firstNewline + 1)..];

        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (lastFence >= 0)
            trimmed = trimmed[..lastFence];

        return trimmed.Trim();
    }

    private static Dictionary<string, string> ExtractFormFieldLabels(IrSnapshotFragment? formPage)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (formPage == null) return map;

        try
        {
            using var doc = JsonDocument.Parse(formPage.Payload);
            if (doc.RootElement.TryGetProperty("fields", out var fields) && fields.ValueKind == JsonValueKind.Array)
            {
                CollectFieldLabels(fields, map);
                return map;
            }

            if (doc.RootElement.TryGetProperty("pages", out var pages) && pages.ValueKind == JsonValueKind.Array)
            {
                foreach (var page in pages.EnumerateArray())
                {
                    if (page.TryGetProperty("fields", out var pageFields) && pageFields.ValueKind == JsonValueKind.Array)
                        CollectFieldLabels(pageFields, map);
                }
            }
        }
        catch
        {
            // ignore
        }

        return map;
    }

    private static void CollectFieldLabels(JsonElement fields, Dictionary<string, string> map)
    {
        foreach (var field in fields.EnumerateArray())
        {
            var fieldId = TryGetJsonString(field, "fieldId", "FieldId", "id", "Id");
            var label = TryGetJsonString(field, "label", "Label");
            if (!string.IsNullOrWhiteSpace(fieldId) && !string.IsNullOrWhiteSpace(label) && !map.ContainsKey(fieldId))
                map[fieldId] = label!;
        }
    }

    private static string? TryGetJsonString(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        return null;
    }

    private static List<TableColumnConfigModel> ParseColumnsFromDdl(
        string ddl,
        IReadOnlyDictionary<string, string> fieldLabels,
        Ir2CodegenBuildOptions options)
    {
        var list = new List<TableColumnConfigModel>();
        foreach (Match match in ColumnLineRegex.Matches(ddl))
        {
            var original = match.Groups["col"].Value;
            var sqlType = match.Groups["sqlType"].Value;
            var rest = match.Groups["rest"].Value;
            var columnName = ToPropertyName(original);
            var isPk = rest.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)
                || original.Equals("F_Id", StringComparison.OrdinalIgnoreCase);

            if (options.EnableFlow && original.Equals("F_Flow_Id", StringComparison.OrdinalIgnoreCase))
                continue;

            if (options.EnableFlow && original.Equals("F_Flow_Task_Id", StringComparison.OrdinalIgnoreCase))
                continue;

            // IsTenantColumn 时 Entity.cs.vm 末尾注入 F_Tenant_Id，DDL 中的 F_TenantId 跳过避免重复属性
            if (options.IsTenantColumn &&
                (original.Equals("F_TenantId", StringComparison.OrdinalIgnoreCase) ||
                 original.Equals("F_Tenant_Id", StringComparison.OrdinalIgnoreCase)))
                continue;

            fieldLabels.TryGetValue(columnName, out var label);
            if (string.IsNullOrWhiteSpace(label))
                label = DefaultLabel(columnName);

            list.Add(new TableColumnConfigModel
            {
                ColumnName = columnName,
                OriginalColumnName = original,
                ColumnComment = label,
                NetType = MapSqlTypeToNetType(sqlType),
                PrimaryKey = isPk,
                jnpfKey = isPk ? null : "input",
                IsImportField = false,
            });
        }

        return list;
    }

    private static List<TableColumnConfigModel> BuildColumnsFromFormPage(
        IReadOnlyDictionary<string, string> fieldLabels,
        Ir2CodegenBuildOptions options)
    {
        var list = new List<TableColumnConfigModel>
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

        foreach (var (fieldId, label) in fieldLabels)
        {
            if (fieldId.Equals("id", StringComparison.OrdinalIgnoreCase))
                continue;

            list.Add(new TableColumnConfigModel
            {
                ColumnName = char.ToUpperInvariant(fieldId[0]) + fieldId[1..],
                OriginalColumnName = $"F_{char.ToUpperInvariant(fieldId[0])}{fieldId[1..]}",
                ColumnComment = label,
                NetType = "string",
                jnpfKey = "input",
            });
        }

        // IsTenantColumn 时 Entity.cs.vm 末尾注入 F_Tenant_Id，此处不得重复添加 TenantId
        return list;
    }

    private static List<TableColumnConfigModel> BuildMinimalLeaveColumns(Ir2CodegenBuildOptions options)
    {
        var columns = new List<TableColumnConfigModel>
        {
            Col("Id", "F_Id", "主键", "string", primaryKey: true),
            Col("Reason", "F_Reason", "请假事由", "string"),
            Col("Days", "F_Days", "请假天数", "int"),
            Col("Status", "F_Status", "状态", "string"),
        };

        if (options.IsTenantColumn)
            columns.Add(Col("TenantId", "F_TenantId", "租户", "string"));

        if (options.EnableFlow)
        {
            columns.Add(Col("FlowId", "F_Flow_Id", "流程引擎ID", "string"));
            columns.Add(Col("FlowTaskId", "F_Flow_Task_Id", "流程任务ID", "string"));
        }

        return columns;
    }

    private static TableColumnConfigModel Col(
        string name,
        string original,
        string comment,
        string netType,
        bool primaryKey = false) => new()
    {
        ColumnName = name,
        OriginalColumnName = original,
        ColumnComment = comment,
        NetType = netType,
        PrimaryKey = primaryKey,
        jnpfKey = primaryKey ? null : "input",
    };

    private static string ToPropertyName(string originalColumn)
    {
        var raw = originalColumn.StartsWith("F_", StringComparison.OrdinalIgnoreCase)
            ? originalColumn[2..]
            : originalColumn;
        return char.ToUpperInvariant(raw[0]) + raw[1..];
    }

    private static string MapSqlTypeToNetType(string sqlType) => sqlType.ToUpperInvariant() switch
    {
        "INT" => "int",
        "BIGINT" => "long",
        "BIT" => "bool",
        "DECIMAL" or "NUMERIC" or "MONEY" => "decimal",
        "FLOAT" => "double",
        "DATETIME" or "DATETIME2" or "DATE" or "SMALLDATETIME" => "DateTime?",
        _ => "string",
    };

    private static string DefaultLabel(string columnName) => columnName switch
    {
        "Reason" => "请假事由",
        "Days" => "请假天数",
        "Status" => "状态",
        "StartDate" => "开始日期",
        "EndDate" => "结束日期",
        "LeaveType" => "请假类型",
        "TenantId" => "租户",
        _ => columnName,
    };

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
