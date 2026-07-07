using System.Text.Json;
using System.Text.Json.Serialization;

namespace JNPF.InteAssistant.Sa;

/// <summary>
/// S2 预分析 canonical 模型（机器层）。通常来自 IR-0 SkeletonCreated payload。
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

    /// <summary>从 SkeletonCreated JSON payload 解析。</summary>
    public static PreAnalysisModel ParseFromSkeletonJson(string skeletonJson, string? requirementSummary = null)
    {
        if (string.IsNullOrWhiteSpace(skeletonJson))
            throw new ArgumentException("skeletonJson 不能为空", nameof(skeletonJson));

        using var doc = JsonDocument.Parse(skeletonJson);
        var root = doc.RootElement;

        var events = new List<PreAnalysisBusinessEvent>();
        if (root.TryGetProperty("businessEvents", out var eventsEl) && eventsEl.ValueKind == JsonValueKind.Array)
        {
            var idx = 0;
            foreach (var e in eventsEl.EnumerateArray())
            {
                idx++;
                events.Add(new PreAnalysisBusinessEvent
                {
                    Index = idx,
                    EventId = GetString(e, "eventId") ?? $"EV-{idx:D3}",
                    EventName = GetString(e, "eventName") ?? $"事件{idx}",
                    ComplexityHint = NormalizeComplexity(GetString(e, "complexityHint")),
                    Description = GetString(e, "description"),
                    DependsOn = ParseDependsOn(e),
                });
            }
        }

        var entities = new List<PreAnalysisEntityDraft>();
        if (root.TryGetProperty("entityDrafts", out var draftsEl) && draftsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var d in draftsEl.EnumerateArray())
            {
                var fields = new List<PreAnalysisFieldDraft>();
                if (d.TryGetProperty("fields", out var fieldsEl) && fieldsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var f in fieldsEl.EnumerateArray())
                    {
                        fields.Add(new PreAnalysisFieldDraft
                        {
                            Name = GetString(f, "name") ?? "",
                            Type = GetString(f, "type") ?? "String",
                            Required = f.TryGetProperty("required", out var req) && req.ValueKind == JsonValueKind.True,
                            IsPrimaryKey = f.TryGetProperty("isPK", out var pk) && pk.ValueKind == JsonValueKind.True
                                || string.Equals(GetString(f, "name"), "id", StringComparison.OrdinalIgnoreCase),
                        });
                    }
                }

                entities.Add(new PreAnalysisEntityDraft
                {
                    EntityName = GetString(d, "entityName") ?? "Entity",
                    TableName = GetString(d, "tableName"),
                    Description = GetString(d, "description"),
                    Fields = fields,
                });
            }
        }

        var rules = new List<PreAnalysisBusinessRule>();
        if (root.TryGetProperty("businessRules", out var rulesEl) && rulesEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in rulesEl.EnumerateArray())
            {
                rules.Add(new PreAnalysisBusinessRule
                {
                    RuleId = GetString(r, "ruleId") ?? "",
                    ScopeEventId = GetString(r, "scope") ?? GetString(r, "scopeEventId"),
                    Description = GetString(r, "description") ?? "",
                });
            }
        }

        var transitions = new List<PreAnalysisStateTransition>();
        if (root.TryGetProperty("stateTransitions", out var stEl) && stEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in stEl.EnumerateArray())
            {
                transitions.Add(new PreAnalysisStateTransition
                {
                    Entity = GetString(t, "entity") ?? "",
                    From = GetString(t, "from") ?? "",
                    To = GetString(t, "to") ?? "",
                    TriggerEventId = GetString(t, "trigger") ?? GetString(t, "triggerEventId"),
                });
            }
        }

        return new PreAnalysisModel
        {
            SystemName = GetString(root, "systemName"),
            RequirementSummary = requirementSummary,
            BusinessEvents = events,
            EntityDrafts = entities,
            BusinessRules = rules,
            StateTransitions = transitions,
        };
    }

    private static string? GetString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var prop))
            return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
    }

    private static IReadOnlyList<string> ParseDependsOn(JsonElement e)
    {
        if (!e.TryGetProperty("dependsOn", out var dep))
            return Array.Empty<string>();

        if (dep.ValueKind == JsonValueKind.String)
        {
            var s = dep.GetString();
            return string.IsNullOrWhiteSpace(s) ? Array.Empty<string>() : new[] { s };
        }

        if (dep.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        return dep.EnumerateArray()
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();
    }

    private static string NormalizeComplexity(string? hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
            return "simple";

        return hint switch
        {
            "简单" or "simple" or "Simple" => "simple",
            "中等" or "medium" or "Medium" => "medium",
            "复杂" or "complex" or "Complex" => "complex",
            _ => "simple",
        };
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
    public string? TableName { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<PreAnalysisFieldDraft> Fields { get; init; } = Array.Empty<PreAnalysisFieldDraft>();
}

public sealed class PreAnalysisFieldDraft
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "String";
    public bool Required { get; init; }
    public bool IsPrimaryKey { get; init; }
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

    public SaProjectResult ToProjectResult() => new()
    {
        EventResults = EventResults,
        TotalDurationMs = CompileDurationMs,
    };
}
