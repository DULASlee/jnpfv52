using System.Text.Json;
using JNPF.InteAssistant.Sa;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 从 SA 九步 previousSteps 提取 EventSpec 结构化字段与业务规则（P0 业务闭环）。
/// </summary>
public static class EventSpecAssembler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static string BuildPayloadJson(
        string eventId,
        AnalystSkillService.BusinessEventMeta meta,
        IReadOnlyDictionary<string, object> previousSteps)
    {
        var fields = ExtractConfirmedFields(previousSteps);
        var rules = ExtractBusinessRules(previousSteps, meta.EventName);

        if (fields.Count == 0)
        {
            fields.Add(new Dictionary<string, object>
            {
                ["name"] = "id",
                ["type"] = "BIGINT",
                ["required"] = true,
                ["source"] = "default-pk",
            });
        }

        if (rules.Count == 0)
        {
            rules.Add(new Dictionary<string, object>
            {
                ["ruleId"] = "R1",
                ["description"] = meta.EventName,
                ["source"] = "event-name",
            });
        }

        var previousStepsJson = previousSteps.ToDictionary(
            kv => kv.Key,
            kv => NormalizeStepValue(kv.Value),
            StringComparer.Ordinal);

        return JsonSerializer.Serialize(new
        {
            eventId,
            eventName = meta.EventName,
            complexityHint = meta.ComplexityHint,
            version = 1,
            confirmedFields = fields,
            businessRules = rules,
            ioiInvariants = Array.Empty<object>(),
            saStepsCompleted = previousSteps.Keys
                .Where(k => SaStepMapping.IrStepOrder.Any(s => s == k))
                .ToList(),
            previousSteps = previousStepsJson,
        }, JsonOptions);
    }

    internal static List<Dictionary<string, object>> ExtractConfirmedFields(
        IReadOnlyDictionary<string, object> previousSteps)
    {
        var fields = new List<Dictionary<string, object>>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddField(string name, string type, bool required, string source)
        {
            if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                return;
            fields.Add(new Dictionary<string, object>
            {
                ["name"] = name,
                ["type"] = type,
                ["required"] = required,
                ["source"] = source,
            });
        }

        if (TryGetStepRoot(previousSteps, "CommandQuery", out var dictRoot)
            && dictRoot.TryGetProperty("elements", out var elements)
            && elements.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in elements.EnumerateArray())
            {
                var name = GetString(el, "name");
                var type = GetString(el, "type") ?? "NVARCHAR(255)";
                var required = el.TryGetProperty("isRequired", out var req) && req.GetBoolean();
                AddField(name, type, required, "CommandQuery.elements");
            }
        }

        if (fields.Count == 0
            && TryGetStepRoot(previousSteps, "DataModel", out var erRoot)
            && erRoot.TryGetProperty("entities", out var entities)
            && entities.ValueKind == JsonValueKind.Array)
        {
            foreach (var entity in entities.EnumerateArray())
            {
                if (!entity.TryGetProperty("columns", out var cols) || cols.ValueKind != JsonValueKind.Array)
                    continue;
                foreach (var col in cols.EnumerateArray())
                {
                    var name = GetString(col, "name");
                    var type = GetString(col, "type") ?? GetString(col, "dataType") ?? "NVARCHAR(255)";
                    AddField(name, type, false, "DataModel.entities");
                }
            }
        }

        if (fields.Count == 0
            && TryGetStepRoot(previousSteps, "DeliveryChecklist", out var uiRoot)
            && uiRoot.TryGetProperty("screens", out var screens)
            && screens.ValueKind == JsonValueKind.Array)
        {
            foreach (var screen in screens.EnumerateArray())
            {
                if (!screen.TryGetProperty("fields", out var uiFields) || uiFields.ValueKind != JsonValueKind.Array)
                    continue;
                foreach (var f in uiFields.EnumerateArray())
                {
                    var name = GetString(f, "name");
                    var type = GetString(f, "type") ?? "string";
                    var required = f.TryGetProperty("required", out var r) && r.GetBoolean();
                    AddField(name, type, required, "DeliveryChecklist.screens");
                }
            }
        }

        return fields;
    }

    internal static List<Dictionary<string, object>> ExtractBusinessRules(
        IReadOnlyDictionary<string, object> previousSteps,
        string eventName)
    {
        var rules = new List<Dictionary<string, object>>();
        var idx = 0;

        if (TryGetStepRoot(previousSteps, "WorkflowSpec", out var dtRoot)
            && dtRoot.TryGetProperty("tables", out var tables)
            && tables.ValueKind == JsonValueKind.Array)
        {
            foreach (var table in tables.EnumerateArray())
            {
                var tableId = GetString(table, "id") ?? $"DT-{++idx}";
                if (table.TryGetProperty("rules", out var tableRules) && tableRules.ValueKind == JsonValueKind.Array)
                {
                    var ruleIdx = 0;
                    foreach (var _ in tableRules.EnumerateArray())
                    {
                        rules.Add(new Dictionary<string, object>
                        {
                            ["ruleId"] = $"{tableId}-R{++ruleIdx}",
                            ["description"] = $"判定表 {tableId} 规则 #{ruleIdx}",
                            ["source"] = "WorkflowSpec.decisionTable",
                        });
                    }
                }

                if (table.TryGetProperty("conditions", out var conds) && conds.ValueKind == JsonValueKind.Array)
                {
                    foreach (var cond in conds.EnumerateArray())
                    {
                        var cName = GetString(cond, "name");
                        if (string.IsNullOrWhiteSpace(cName)) continue;
                        rules.Add(new Dictionary<string, object>
                        {
                            ["ruleId"] = $"{tableId}-C{++idx}",
                            ["description"] = $"条件: {cName}",
                            ["source"] = "WorkflowSpec.conditions",
                        });
                    }
                }
            }
        }

        if (TryGetStepRoot(previousSteps, "IntegrationPoints", out var pspecRoot)
            && pspecRoot.TryGetProperty("processSpecs", out var specs)
            && specs.ValueKind == JsonValueKind.Array)
        {
            foreach (var spec in specs.EnumerateArray())
            {
                var name = GetString(spec, "name");
                var validation = GetString(spec, "validation");
                if (string.IsNullOrWhiteSpace(validation)) continue;
                rules.Add(new Dictionary<string, object>
                {
                    ["ruleId"] = $"PS-{++idx}",
                    ["description"] = $"{name}: {validation}",
                    ["source"] = "IntegrationPoints.processSpecs",
                });
            }
        }

        if (rules.Count == 0
            && TryGetStepRoot(previousSteps, "EventCatalog", out var bpmRoot)
            && bpmRoot.TryGetProperty("exceptionPaths", out var exceptions)
            && exceptions.ValueKind == JsonValueKind.Array)
        {
            foreach (var ex in exceptions.EnumerateArray())
            {
                var desc = ex.ValueKind == JsonValueKind.String
                    ? ex.GetString()
                    : ex.GetRawText();
                if (string.IsNullOrWhiteSpace(desc)) continue;
                rules.Add(new Dictionary<string, object>
                {
                    ["ruleId"] = $"EX-{++idx}",
                    ["description"] = desc.Length > 200 ? desc[..200] : desc,
                    ["source"] = "EventCatalog.exceptionPaths",
                });
            }
        }

        if (rules.Count == 0 && !string.IsNullOrWhiteSpace(eventName))
        {
            rules.Add(new Dictionary<string, object>
            {
                ["ruleId"] = "R1",
                ["description"] = eventName,
                ["source"] = "event-name-fallback",
            });
        }

        return rules;
    }

    private static bool TryGetStepRoot(
        IReadOnlyDictionary<string, object> steps,
        string stepName,
        out JsonElement root)
    {
        root = default;
        if (!steps.TryGetValue(stepName, out var raw) || raw == null)
            return false;

        try
        {
            var json = raw switch
            {
                JsonElement el => el.GetRawText(),
                string s => s,
                _ => JsonSerializer.Serialize(raw, JsonOptions),
            };
            using var doc = JsonDocument.Parse(json);
            root = doc.RootElement.Clone();
            return root.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }

    private static object NormalizeStepValue(object? raw) => raw switch
    {
        null => new { },
        JsonElement el => JsonSerializer.Deserialize<object>(el.GetRawText(), JsonOptions) ?? new { },
        _ => raw,
    };

    private static string? GetString(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
