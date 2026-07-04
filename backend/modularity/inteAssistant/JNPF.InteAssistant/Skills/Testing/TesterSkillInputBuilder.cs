using System.Text.Json;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Skills;

namespace JNPF.InteAssistant.Skills.Testing;

/// <summary>
/// 从 IR 快照组装 tester-skill 输入（Q1 schema 对齐）。
/// </summary>
public static class TesterSkillInputBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static TesterBuildResult Build(SkillContext context)
    {
        var warnings = (context.ArchGuardWarnings ?? Array.Empty<SkillArchWarning>())
            .Select(w => new TesterArchWarning
            {
                RuleId = w.RuleId,
                Message = w.Message,
                FilePath = w.FilePath,
            })
            .ToList();

        var eventSpec = context.Snapshot.Find(IrFragmentTypes.EventSpec, IrStabilityStates.Stable)
            ?? context.Snapshot.Find(IrFragmentTypes.EventSpec);
        var formPage = context.Snapshot.Find(IrFragmentTypes.FormPageIR, IrStabilityStates.Stable)
            ?? context.Snapshot.Find(IrFragmentTypes.FormPageIR);
        var systemDesign = context.Snapshot.Find(IrFragmentTypes.SystemDesign, IrStabilityStates.Locked)
            ?? context.Snapshot.Find(IrFragmentTypes.SystemDesign);

        var fields = eventSpec != null
            ? ParseConfirmedFieldsFromEventSpec(eventSpec.Payload)
            : ParseConfirmedFieldsFromFormPage(formPage?.Payload);

        if (fields.Count == 0)
            throw new InvalidOperationException("无法从 IR1_EventSpec 或 IR2_FormPageIR 解析 confirmedFields");

        var stateMachines = ParseStateMachines(systemDesign?.Payload);
        var derivationMode = stateMachines.Count > 0 ? "field-and-state-machine" : "field-only";

        var transitions = new List<TesterStateTransition>();
        var states = new List<TesterStateNode>();
        foreach (var sm in stateMachines)
        {
            transitions.AddRange(sm.Transitions);
            states.AddRange(sm.States);
        }

        return new TesterBuildResult
        {
            DerivationMode = derivationMode,
            ConfirmedFields = fields,
            Transitions = transitions,
            States = states,
            ArchGuardWarnings = warnings,
            FormPageName = ParseFormPageName(formPage?.Payload),
        };
    }

    private static List<TesterConfirmedField> ParseConfirmedFieldsFromEventSpec(string payloadJson)
    {
        var list = new List<TesterConfirmedField>();
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (!doc.RootElement.TryGetProperty("confirmedFields", out var arr))
                return list;

            foreach (var f in arr.EnumerateArray())
            {
                var name = f.TryGetProperty("name", out var n) ? n.GetString() : null;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                list.Add(new TesterConfirmedField
                {
                    Name = name,
                    Type = f.TryGetProperty("type", out var t) ? t.GetString() ?? "string" : "string",
                    Required = f.TryGetProperty("required", out var r) && r.ValueKind == JsonValueKind.True,
                });
            }
        }
        catch
        {
            // ignore
        }

        return list;
    }

    private static List<TesterConfirmedField> ParseConfirmedFieldsFromFormPage(string? payloadJson)
    {
        var list = new List<TesterConfirmedField>();
        if (string.IsNullOrWhiteSpace(payloadJson))
            return list;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (!doc.RootElement.TryGetProperty("fields", out var arr))
                return list;

            foreach (var f in arr.EnumerateArray())
            {
                var fieldId = f.TryGetProperty("fieldId", out var id) ? id.GetString() : null;
                if (string.IsNullOrWhiteSpace(fieldId) || fieldId.Equals("id", StringComparison.OrdinalIgnoreCase))
                    continue;

                var component = f.TryGetProperty("component", out var c) ? c.GetString() ?? "Input" : "Input";
                var name = ToPascalCase(fieldId);
                list.Add(new TesterConfirmedField
                {
                    Name = name,
                    Type = InferType(component),
                    Required = fieldId is "reason" or "days" or "status",
                });
            }
        }
        catch
        {
            // ignore
        }

        return list;
    }

    private static List<TesterStateMachineSlice> ParseStateMachines(string? payloadJson)
    {
        var list = new List<TesterStateMachineSlice>();
        if (string.IsNullOrWhiteSpace(payloadJson))
            return list;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (!doc.RootElement.TryGetProperty("stateMachines", out var arr))
                return list;

            foreach (var sm in arr.EnumerateArray())
            {
                var transitions = new List<TesterStateTransition>();
                if (sm.TryGetProperty("transitions", out var te))
                {
                    foreach (var t in te.EnumerateArray())
                    {
                        transitions.Add(new TesterStateTransition
                        {
                            From = t.GetProperty("from").GetString() ?? string.Empty,
                            To = t.GetProperty("to").GetString() ?? string.Empty,
                            Event = t.GetProperty("event").GetString() ?? string.Empty,
                            Guard = t.TryGetProperty("guard", out var g) ? g.GetString() : null,
                        });
                    }
                }

                var states = new List<TesterStateNode>();
                if (sm.TryGetProperty("states", out var se))
                {
                    foreach (var s in se.EnumerateArray())
                    {
                        states.Add(new TesterStateNode
                        {
                            StateId = s.GetProperty("stateId").GetString() ?? string.Empty,
                            IsTerminal = s.TryGetProperty("isTerminal", out var term)
                                && term.ValueKind == JsonValueKind.True,
                        });
                    }
                }

                if (transitions.Count > 0)
                {
                    list.Add(new TesterStateMachineSlice
                    {
                        Transitions = transitions,
                        States = states,
                    });
                }
            }
        }
        catch
        {
            // ignore
        }

        return list;
    }

    private static string? ParseFormPageName(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            return doc.RootElement.TryGetProperty("pageName", out var p) ? p.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static string InferType(string component) =>
        component.Contains("Number", StringComparison.OrdinalIgnoreCase) ? "int" : "string";

    private static string ToPascalCase(string fieldId)
    {
        if (string.IsNullOrEmpty(fieldId))
            return fieldId;
        return char.ToUpperInvariant(fieldId[0]) + fieldId[1..];
    }

    private sealed class TesterStateMachineSlice
    {
        public List<TesterStateTransition> Transitions { get; init; } = new();
        public List<TesterStateNode> States { get; init; } = new();
    }
}

public sealed class TesterBuildResult
{
    public required string DerivationMode { get; init; }
    public required IReadOnlyList<TesterConfirmedField> ConfirmedFields { get; init; }
    public required IReadOnlyList<TesterStateTransition> Transitions { get; init; }
    public required IReadOnlyList<TesterStateNode> States { get; init; }
    public required IReadOnlyList<TesterArchWarning> ArchGuardWarnings { get; init; }
    public string? FormPageName { get; init; }
}

public sealed class TesterArchWarning
{
    public required string RuleId { get; init; }
    public required string Message { get; init; }
    public string? FilePath { get; init; }
}
