using System.Collections.Generic;
using System.Text.Json;

namespace JNPF.InteAssistant.Entitys.Ir.Contracts;

/// <summary>
/// IR0_Skeleton 完整契约（阶段九 P9-S1）。
///
/// 修复的 10 个缺口中的 4 个：
///   ① roleMatrix 不再丢弃（LLM 已产出，解析器之前忽略）
///   ② EntityDraft 增加 Relations（不再靠 EndsWith("Id") 猜关系）
///   ③ FieldDraft 增加 References 外键引用 + 双向兼容 primaryKey/isPK
///   ④ EntityDraft 增加 DisplayName（不丢弃）
///
/// 此契约同时作为：序列化模型 + 校验依据 + SA 编译器输入。
/// </summary>
public sealed class SkeletonPayload
{
    public string SkeletonId { get; set; } = "";
    public string SystemName { get; set; } = "";
    public string RequirementSummary { get; set; } = "";
    public string SchemaVersion { get; set; } = "2.0";
    public string ContractVersion { get; set; } = "entity-field.v1";

    /// <summary>实体草稿（含关系声明）</summary>
    public List<EntityDraftContract> EntityDrafts { get; set; } = new();

    /// <summary>业务事件</summary>
    public List<BusinessEventContract> BusinessEvents { get; set; } = new();

    /// <summary>权限矩阵（角色×事件→操作）。不再丢弃。</summary>
    public RoleMatrixContract? RoleMatrix { get; set; }

    /// <summary>业务规则</summary>
    public List<BusinessRuleContract> BusinessRules { get; set; } = new();

    /// <summary>状态转换</summary>
    public List<StateTransitionContract> StateTransitions { get; set; } = new();

    /// <summary>
    /// 从 SkeletonCreated JSON payload 解析（双向兼容：primaryKey/isPK 都认）。
    /// 替代 PreAnalysisModel.ParseFromSkeletonJson 的解析逻辑，补全缺失字段。
    /// </summary>
    public static SkeletonPayload Parse(string skeletonJson)
    {
        if (string.IsNullOrWhiteSpace(skeletonJson))
            throw new System.ArgumentException("skeletonJson 不能为空", nameof(skeletonJson));

        using var doc = JsonDocument.Parse(skeletonJson);
        var root = doc.RootElement;

        var payload = new SkeletonPayload
        {
            SkeletonId = GetString(root, "skeletonId") ?? "",
            SystemName = GetString(root, "systemName") ?? "",
            // 漏洞2修补：Parse 补读 requirementSummary（SaNineViewCompiler 消费此字段）
            RequirementSummary = GetString(root, "requirementSummary") ?? "",
            SchemaVersion = GetString(root, "version") ?? "2.0",
            ContractVersion = GetString(root, "contractVersion") ?? "entity-field.v1",
        };

        // 解析 entityDrafts（补全 displayName / relations / references / primaryKey 双向兼容）
        if (root.TryGetProperty("entityDrafts", out var draftsEl) && draftsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var d in draftsEl.EnumerateArray())
            {
                var entity = new EntityDraftContract
                {
                    EntityName = GetString(d, "entityName") ?? "Entity",
                    DisplayName = GetString(d, "displayName") ?? GetString(d, "entityName") ?? "Entity",
                    TableName = GetString(d, "tableName") ?? "",
                    Description = GetString(d, "description") ?? "",
                };

                // 解析 fields（双向兼容 primaryKey / isPK；新增 references）
                if (d.TryGetProperty("fields", out var fieldsEl) && fieldsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var f in fieldsEl.EnumerateArray())
                    {
                        // 双向兼容：优先读 primaryKey（LLM 实际产出），回退 isPK，再回退 name=="id"
                        var isPk = ReadBool(f, "primaryKey")
                            ?? ReadBool(f, "isPK")
                            ?? string.Equals(GetString(f, "name"), "id", StringComparison.OrdinalIgnoreCase);

                        entity.Fields.Add(new FieldDraftContract
                        {
                            Name = GetString(f, "name") ?? "",
                            Type = GetString(f, "type") ?? "string",
                            Required = ReadBool(f, "required") ?? false,
                            PrimaryKey = isPk,
                            // 新增：外键引用（格式 "EntityName.FieldName"），不再猜
                            References = GetString(f, "references") ?? GetString(f, "ref"),
                        });
                    }
                }

                // 新增：解析实体级关系声明
                if (d.TryGetProperty("relations", out var relEl) && relEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var r in relEl.EnumerateArray())
                    {
                        entity.Relations.Add(new EntityRelationContract
                        {
                            FromField = GetString(r, "fromField") ?? GetString(r, "from") ?? "",
                            ToEntity = GetString(r, "toEntity") ?? GetString(r, "to") ?? "",
                            ToField = GetString(r, "toField") ?? GetString(r, "field") ?? "id",
                            RelationType = GetString(r, "relationType") ?? GetString(r, "type") ?? "many-to-one",
                        });
                    }
                }

                payload.EntityDrafts.Add(entity);
            }
        }

        // 解析 businessEvents
        if (root.TryGetProperty("businessEvents", out var eventsEl) && eventsEl.ValueKind == JsonValueKind.Array)
        {
            var idx = 0;
            foreach (var e in eventsEl.EnumerateArray())
            {
                idx++;
                payload.BusinessEvents.Add(new BusinessEventContract
                {
                    Index = idx,  // 漏洞1修补：SaNineViewCompiler 消费 Index 做节点连线 ID
                    EventId = GetString(e, "eventId") ?? $"EV-{idx:D3}",
                    EventName = GetString(e, "eventName") ?? $"事件{idx}",
                    ComplexityHint = NormalizeComplexity(GetString(e, "complexityHint")),
                    Description = GetString(e, "description"),
                    DependsOn = ParseStringArray(e, "dependsOn"),
                });
            }
        }

        // 新增：解析 roleMatrix（不再丢弃！）
        if (root.TryGetProperty("roleMatrix", out var rmEl))
        {
            var roles = ParseStringArray(rmEl, "roles");
            var matrix = new Dictionary<string, Dictionary<string, List<string>>>();

            if (rmEl.TryGetProperty("matrix", out var matrixEl) && matrixEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var eventIdProp in matrixEl.EnumerateObject())
                {
                    var eventId = eventIdProp.Name;
                    var roleDict = new Dictionary<string, List<string>>();
                    if (eventIdProp.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var roleProp in eventIdProp.Value.EnumerateObject())
                        {
                            roleDict[roleProp.Name] = ParseStringArrayFromElement(roleProp.Value);
                        }
                    }
                    matrix[eventId] = roleDict;
                }
            }

            payload.RoleMatrix = new RoleMatrixContract { Roles = roles.ToList(), Matrix = matrix };
        }

        // 解析 businessRules
        if (root.TryGetProperty("businessRules", out var rulesEl) && rulesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in rulesEl.EnumerateArray())
            {
                payload.BusinessRules.Add(new BusinessRuleContract
                {
                    RuleId = GetString(r, "ruleId") ?? "",
                    ScopeEventId = GetString(r, "scope") ?? GetString(r, "scopeEventId"),
                    Description = GetString(r, "description") ?? "",
                });
            }
        }

        // 解析 stateTransitions
        if (root.TryGetProperty("stateTransitions", out var stEl) && stEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in stEl.EnumerateArray())
            {
                payload.StateTransitions.Add(new StateTransitionContract
                {
                    Entity = GetString(t, "entity") ?? "",
                    From = GetString(t, "from") ?? "",
                    To = GetString(t, "to") ?? "",
                    TriggerEventId = GetString(t, "trigger") ?? GetString(t, "triggerEventId"),
                });
            }
        }

        return payload;
    }

    /// <summary>校验：非空 businessEvents + 每个 entity 至少有 1 字段</summary>
    public void Validate()
    {
        if (BusinessEvents.Count == 0)
            throw new System.InvalidOperationException("Skeleton 缺少非空 businessEvents");

        foreach (var e in BusinessEvents)
        {
            if (string.IsNullOrWhiteSpace(e.EventId))
                throw new System.InvalidOperationException("businessEvent 缺少 eventId");
            if (string.IsNullOrWhiteSpace(e.EventName))
                throw new System.InvalidOperationException($"businessEvent {e.EventId} 缺少 eventName");
        }

        foreach (var entity in EntityDrafts)
        {
            if (entity.Fields.Count == 0)
                throw new System.InvalidOperationException($"实体 {entity.EntityName} 无字段");
        }
    }

    // ─── 辅助解析方法 ───

    private static string? GetString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop))
            return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
    }

    private static bool? ReadBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop))
            return null;
        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static List<string> ParseStringArray(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop))
            return new List<string>();
        return ParseStringArrayFromElement(prop);
    }

    private static List<string> ParseStringArrayFromElement(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.String)
        {
            var s = el.GetString();
            return string.IsNullOrWhiteSpace(s) ? new List<string>() : new List<string> { s };
        }
        if (el.ValueKind != JsonValueKind.Array)
            return new List<string>();

        return el.EnumerateArray()
            .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() ?? "" : x.ToString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }

    /// <summary>复杂度归一化（修复 high/low 被折叠为 simple 的 bug）</summary>
    internal static string NormalizeComplexity(string? hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return "simple";

        var h = hint.Trim().ToLowerInvariant();
        return h switch
        {
            "简单" or "simple" or "low" or "低" => "simple",
            "中等" or "medium" or "中" => "medium",
            "复杂" or "complex" or "high" or "高" => "complex",  // 修复：high → complex
            _ => "simple",
        };
    }
}

// ─── 契约子类型 ───

public sealed class EntityDraftContract
{
    public string EntityName { get; set; } = "";
    /// <summary>显示名（中文）。不再丢弃。</summary>
    public string DisplayName { get; set; } = "";
    public string TableName { get; set; } = "";
    public string Description { get; set; } = "";
    public List<FieldDraftContract> Fields { get; set; } = new();
    /// <summary>实体间关系声明（不再靠 EndsWith("Id") 猜）</summary>
    public List<EntityRelationContract> Relations { get; set; } = new();
}

public sealed class FieldDraftContract
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "string";
    public bool Required { get; set; }
    /// <summary>是否主键（双向兼容 primaryKey / isPK）</summary>
    public bool PrimaryKey { get; set; }
    /// <summary>外键引用，格式 "EntityName.FieldName"。null=非外键。</summary>
    public string? References { get; set; }
}

public sealed class EntityRelationContract
{
    public string FromField { get; set; } = "";
    public string ToEntity { get; set; } = "";
    public string ToField { get; set; } = "id";
    /// <summary>关系类型：many-to-one / one-to-many / many-to-many</summary>
    public string RelationType { get; set; } = "many-to-one";
}

public sealed class BusinessEventContract
{
    /// <summary>事件序号（1-based），用于 SA DFD/BPM 节点连线 ID。</summary>
    public int Index { get; set; }
    public string EventId { get; set; } = "";
    public string EventName { get; set; } = "";
    public string ComplexityHint { get; set; } = "simple";
    public string? Description { get; set; }
    public List<string> DependsOn { get; set; } = new();
}

/// <summary>权限矩阵（角色×事件→操作列表）。修复 roleMatrix 丢弃缺口。</summary>
public sealed class RoleMatrixContract
{
    public List<string> Roles { get; set; } = new();
    /// <summary>Matrix[eventId][role] = ["create","approve","read",...]</summary>
    public Dictionary<string, Dictionary<string, List<string>>> Matrix { get; set; } = new();
}

public sealed class BusinessRuleContract
{
    public string RuleId { get; set; } = "";
    public string? ScopeEventId { get; set; }
    public string Description { get; set; } = "";
}

public sealed class StateTransitionContract
{
    public string Entity { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string? TriggerEventId { get; set; }
}
