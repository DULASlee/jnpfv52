using System.Text.Json;
using System.Text.Json.Serialization;
using JNPF.InteAssistant.Entitys.Ir.Contracts;

namespace JNPF.InteAssistant.Sa;

/// <summary>
/// S2 预分析 canonical 模型（机器层）。通常来自 IR-0 SkeletonCreated payload。
///
/// 契约主权（S1）：本类不再自行 JsonDocument.Parse Skeleton JSON。
/// 唯一解析入口为 <see cref="SkeletonPayload.Parse"/>；本方法仅做 SkeletonPayload → PreAnalysisModel 的映射。
/// </summary>
public sealed class PreAnalysisModel
{
    public string SchemaVersion { get; init; } = "1.0";

    public string? SystemName { get; init; }

    public string? RequirementSummary { get; init; }

    public IReadOnlyList<PreAnalysisBusinessEvent> BusinessEvents { get; init; } = Array.Empty<PreAnalysisBusinessEvent>();

    public IReadOnlyList<PreAnalysisEntityDraft> EntityDrafts { get; init; } = Array.Empty<PreAnalysisEntityDraft>();

    public IReadOnlyList<PreAnalysisBusinessRule> BusinessRules { get; init; } = Array.Empty<PreAnalysisBusinessRule>();

    public IReadOnlyList<PreAnalysisStateTransition> StateTransitions { get; init; } = Array.Empty<PreAnalysisStateTransition>();

    /// <summary>权限矩阵。P9-S1 修复：不再丢弃 LLM 产出的 roleMatrix。</summary>
    public PreAnalysisRoleMatrix? RoleMatrix { get; init; }

    /// <summary>
    /// 企业可用：补全空的 SystemName / RequirementSummary，禁止说明书表头输出「—」。
    /// 拒绝把 Skeleton JSON 误当作需求概要。
    /// </summary>
    public PreAnalysisModel ResolveIdentity(string? pipelineTitle, string? requirementText)
    {
        var text = SanitizeRequirementText(
            !string.IsNullOrWhiteSpace(requirementText) ? requirementText : RequirementSummary);

        var systemName = !string.IsNullOrWhiteSpace(SystemName) && SystemName is not ("—" or "-")
            ? SystemName!.Trim()
            : Studio.RequirementTitleHelper.ExtractSystemName(text, pipelineTitle);

        var summary = !string.IsNullOrWhiteSpace(RequirementSummary)
                      && RequirementSummary is not ("—" or "-")
                      && !LooksLikeJson(RequirementSummary)
            ? RequirementSummary!.Trim()
            : TruncateSummary(text);

        if (string.IsNullOrWhiteSpace(systemName) || systemName is "—" or "-")
            systemName = !string.IsNullOrWhiteSpace(pipelineTitle)
                ? Studio.RequirementTitleHelper.ExtractSystemName(null, pipelineTitle)
                : "业务";
        if (string.IsNullOrWhiteSpace(summary) || summary is "—" or "-")
            summary = $"（待补充）{systemName}相关业务需求";

        if (systemName == SystemName && summary == RequirementSummary)
            return this;

        return new PreAnalysisModel
        {
            SchemaVersion = SchemaVersion,
            SystemName = systemName,
            RequirementSummary = summary,
            BusinessEvents = BusinessEvents,
            EntityDrafts = EntityDrafts,
            BusinessRules = BusinessRules,
            StateTransitions = StateTransitions,
            RoleMatrix = RoleMatrix,
        };
    }

    private static string? SanitizeRequirementText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || LooksLikeJson(text))
            return null;
        return text.Trim();
    }

    private static bool LooksLikeJson(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var t = text.TrimStart();
        return t.StartsWith('{') || t.StartsWith('[');
    }

    private static string TruncateSummary(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        var oneLine = text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        var first = oneLine.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? oneLine;
        return first.Length > 120 ? first[..120] + "…" : first;
    }

    /// <summary>
    /// 从 SkeletonCreated JSON payload 解析。
    /// 契约主权（S1）：委托 <see cref="SkeletonPayload.Parse"/> 做唯一解析，本方法仅映射到 PreAnalysis* 类型。
    /// </summary>
    public static PreAnalysisModel ParseFromSkeletonJson(string skeletonJson, string? requirementSummary = null)
    {
        if (string.IsNullOrWhiteSpace(skeletonJson))
            throw new ArgumentException("skeletonJson 不能为空", nameof(skeletonJson));

        // 唯一解析入口：SkeletonPayload.Parse（契约主权）
        var skeleton = SkeletonPayload.Parse(skeletonJson);

        // 映射 BusinessEvents（Index 来自 SkeletonPayload.BusinessEventContract.Index）
        var events = skeleton.BusinessEvents.Select(e => new PreAnalysisBusinessEvent
        {
            Index = e.Index,
            EventId = e.EventId,
            EventName = e.EventName,
            ComplexityHint = e.ComplexityHint,
            Description = e.Description,
            DependsOn = (IReadOnlyList<string>)(e.DependsOn as List<string> ?? e.DependsOn?.ToList() ?? new List<string>()),
        }).ToList();

        // 映射 EntityDrafts（含字段级 FK → 实体级 Relations 派生）
        // Fix-6: 预计算实体→PK列名映射，用于 ToField 推断（不再硬编码 "id"）
        var entityPkMap = skeleton.EntityDrafts.ToDictionary(
            d => d.EntityName,
            d => d.Fields.FirstOrDefault(f => f.PrimaryKey)?.Name ?? "id",
            StringComparer.OrdinalIgnoreCase);

        var entities = skeleton.EntityDrafts.Select(d => new PreAnalysisEntityDraft
        {
            EntityName = d.EntityName,
            DisplayName = d.DisplayName,
            TableName = string.IsNullOrWhiteSpace(d.TableName) ? null : d.TableName,
            Description = string.IsNullOrWhiteSpace(d.Description) ? null : d.Description,
            Fields = d.Fields.Select(f => new PreAnalysisFieldDraft
            {
                Name = f.Name,
                Type = f.Type,
                Required = f.Required,
                IsPrimaryKey = f.PrimaryKey,
                References = f.References,
            }).ToList(),
            Relations = MapRelations(d, entityPkMap),
        }).ToList();

        // 映射 BusinessRules
        var rules = skeleton.BusinessRules.Select(r => new PreAnalysisBusinessRule
        {
            RuleId = r.RuleId,
            ScopeEventId = r.ScopeEventId,
            Description = r.Description,
        }).ToList();

        // 映射 StateTransitions
        var transitions = skeleton.StateTransitions.Select(t => new PreAnalysisStateTransition
        {
            Entity = t.Entity,
            From = t.From,
            To = t.To,
            TriggerEventId = t.TriggerEventId,
        }).ToList();

        // 映射 RoleMatrix
        PreAnalysisRoleMatrix? roleMatrix = null;
        if (skeleton.RoleMatrix != null)
        {
            var matrix = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>();
            foreach (var (eventId, roleDict) in skeleton.RoleMatrix.Matrix)
            {
                var inner = new Dictionary<string, IReadOnlyList<string>>();
                foreach (var (role, ops) in roleDict)
                    inner[role] = (IReadOnlyList<string>)(ops as List<string> ?? ops?.ToList() ?? new List<string>());
                matrix[eventId] = inner;
            }
            roleMatrix = new PreAnalysisRoleMatrix
            {
                Roles = (IReadOnlyList<string>)(skeleton.RoleMatrix.Roles as List<string> ?? skeleton.RoleMatrix.Roles?.ToList() ?? new List<string>()),
                Matrix = matrix,
            };
        }

        // RequirementSummary：优先用外部参数（LLM 可能不产出该 JSON 字段），回退到 Parse 读出的值
        var resolvedSummary = !string.IsNullOrWhiteSpace(requirementSummary)
            ? requirementSummary
            : skeleton.RequirementSummary;

        return new PreAnalysisModel
        {
            SystemName = skeleton.SystemName,
            RequirementSummary = resolvedSummary,
            BusinessEvents = events,
            EntityDrafts = entities,
            BusinessRules = rules,
            StateTransitions = transitions,
            RoleMatrix = roleMatrix,
        };
    }

    /// <summary>
    /// 映射实体关系：优先用 Skeleton 显式声明的 Relations；
    /// 若无声明，从字段级 references 派生（保持原有行为）。
    /// </summary>
    private static IReadOnlyList<PreAnalysisRelation> MapRelations(EntityDraftContract d, IReadOnlyDictionary<string, string> entityPkMap)
    {
        var relations = d.Relations.Select(r => new PreAnalysisRelation
        {
            FromField = r.FromField,
            ToEntity = r.ToEntity,
            ToField = r.ToField,
            RelationType = r.RelationType,
        }).ToList();

        // 若实体无 relations 声明，从 field.references 派生
        if (relations.Count == 0)
        {
            foreach (var f in d.Fields)
            {
                if (!string.IsNullOrEmpty(f.References))
                {
                    var parts = f.References.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length >= 1)
                    {
                        // Fix-6: ToField 从目标实体 PK 列名推断，不再硬编码 "id"
                        var toField = parts.Length > 1
                            ? parts[1]
                            : (entityPkMap.TryGetValue(parts[0], out var pk) ? pk : "id");
                        relations.Add(new PreAnalysisRelation
                        {
                            FromField = f.Name,
                            ToEntity = parts[0],
                            ToField = toField,
                            RelationType = "many-to-one",
                        });
                    }
                }
            }
        }

        return relations;
    }
}

public sealed class PreAnalysisBusinessEvent
{
    public int Index { get; init; }
    public string EventId { get; init; } = "";
    public string EventName { get; init; } = "";
    public string ComplexityHint { get; init; } = "simple";
    public string? Description { get; init; }
    public IReadOnlyList<string> DependsOn { get; init; } = Array.Empty<string>();
}

public sealed class PreAnalysisEntityDraft
{
    public string EntityName { get; init; } = "";
    /// <summary>显示名（中文）。P9-S1 修复：不再丢弃 displayName。</summary>
    public string DisplayName { get; init; } = "";
    public string? TableName { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<PreAnalysisFieldDraft> Fields { get; init; } = Array.Empty<PreAnalysisFieldDraft>();

    /// <summary>实体间关系声明。P9-S1 修复：不再靠 EndsWith("Id") 猜。</summary>
    public IReadOnlyList<PreAnalysisRelation> Relations { get; init; } = Array.Empty<PreAnalysisRelation>();
}

public sealed class PreAnalysisFieldDraft
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "String";
    public bool Required { get; init; }
    public bool IsPrimaryKey { get; init; }

    /// <summary>外键引用（格式 "EntityName.FieldName"）。P9-S1 修复：不再猜 FK。</summary>
    public string? References { get; init; }
}

/// <summary>实体间关系声明（P9-S1 新增）。</summary>
public sealed class PreAnalysisRelation
{
    public string FromField { get; init; } = "";
    public string ToEntity { get; init; } = "";
    public string ToField { get; init; } = "id";
    /// <summary>关系类型：many-to-one / one-to-many / many-to-many</summary>
    public string RelationType { get; init; } = "many-to-one";
}

/// <summary>权限矩阵（P9-S1 新增，不再丢弃 roleMatrix）。</summary>
public sealed class PreAnalysisRoleMatrix
{
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    /// <summary>Matrix[eventId][role] = ["create","approve","read",...]</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>> Matrix { get; init; }
        = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<string>>>();
}

public sealed class PreAnalysisBusinessRule
{
    public string RuleId { get; init; } = "";
    public string? ScopeEventId { get; init; }
    public string Description { get; init; } = "";
}

public sealed class PreAnalysisStateTransition
{
    public string Entity { get; init; } = "";
    public string From { get; init; } = "";
    public string To { get; init; } = "";
    public string? TriggerEventId { get; init; }
}

/// <summary>Compiler 完整输出（可物化、可写 IR）。</summary>
public sealed class SaNineViewCompileResult
{
    public required PreAnalysisModel Source { get; init; }

    public required IReadOnlyDictionary<string, object> ProjectSteps { get; init; }

    public required IReadOnlyList<SaEventResult> EventResults { get; init; }

    public int CompileDurationMs { get; init; }

    public string BundleHash { get; init; } = "";

    /// <summary>
    /// 编译过程中收集的假设项（C# 推导的非用户明确指定的决策）。
    /// Round 1/2 内存传递（注入 LLM prompt），Round 3 落库到 sa_assumptions。
    /// </summary>
    public List<Assumption> Assumptions { get; init; } = new();

    public SaProjectResult ToProjectResult() => new()
    {
        EventResults = EventResults,
        TotalDurationMs = CompileDurationMs,
    };
}
