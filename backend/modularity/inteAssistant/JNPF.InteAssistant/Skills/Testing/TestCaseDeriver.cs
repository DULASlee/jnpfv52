namespace JNPF.InteAssistant.Skills.Testing;

public sealed class DerivedTestCase
{
    public required string CaseId { get; init; }
    public required string Rule { get; init; }
    public required string Description { get; init; }
    public required string Kind { get; init; }
}

public sealed class TesterConfirmedField
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool Required { get; init; }
}

public sealed class TesterStateTransition
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Event { get; init; }
    public string? Guard { get; init; }
}

public sealed class TesterStateNode
{
    public required string StateId { get; init; }
    public bool IsTerminal { get; init; }
}

/// <summary>
/// Q1 确定性推导 — field-only ≥3 · field+stateMachine ≥5。
/// </summary>
public static class TestCaseDeriver
{
    public const int MinFieldOnly = 3;
    public const int MinFieldAndStateMachine = 5;

    public static IReadOnlyList<DerivedTestCase> DeriveFieldCases(IReadOnlyList<TesterConfirmedField> fields)
    {
        var cases = new List<DerivedTestCase>();
        var required = fields.Where(f => f.Required).ToList();
        if (required.Count > 0)
        {
            cases.Add(new DerivedTestCase
            {
                CaseId = "valid-all-required",
                Rule = "F-REQ",
                Kind = "valid",
                Description = $"提交全部必填字段：{string.Join(", ", required.Select(r => r.Name))}",
            });
        }

        foreach (var field in required)
        {
            cases.Add(new DerivedTestCase
            {
                CaseId = $"invalid-missing-{field.Name.ToLowerInvariant()}",
                Rule = "F-REQ",
                Kind = "invalid",
                Description = $"缺少必填字段 {field.Name}",
            });
        }

        foreach (var field in required.Where(f => !string.Equals(f.Type, "int", StringComparison.OrdinalIgnoreCase)))
        {
            cases.Add(new DerivedTestCase
            {
                CaseId = $"invalid-empty-{field.Name.ToLowerInvariant()}",
                Rule = "F-REQ",
                Kind = "invalid",
                Description = $"必填字段 {field.Name} 为空字符串",
            });
        }

        foreach (var field in fields.Where(f => f.Type == "int"))
        {
            cases.Add(new DerivedTestCase
            {
                CaseId = $"valid-boundary-int-{field.Name.ToLowerInvariant()}",
                Rule = "F-TYPE",
                Kind = "valid",
                Description = $"{field.Name} 边界值 1",
            });
            cases.Add(new DerivedTestCase
            {
                CaseId = $"invalid-wrong-type-{field.Name.ToLowerInvariant()}",
                Rule = "F-TYPE",
                Kind = "invalid",
                Description = $"{field.Name} 类型错误（非 int）",
            });
        }

        return Deduplicate(cases);
    }

    public static IReadOnlyList<DerivedTestCase> DeriveStateMachineCases(
        IReadOnlyList<TesterStateTransition> transitions,
        IReadOnlyList<TesterStateNode> states)
    {
        var cases = new List<DerivedTestCase>();
        foreach (var t in transitions)
        {
            cases.Add(new DerivedTestCase
            {
                CaseId = $"happy-path-{t.From}-{t.Event}-{t.To}",
                Rule = "SM-EDGE",
                Kind = "valid",
                Description = $"状态 {t.From} --[{t.Event}]--> {t.To}",
            });
            cases.Add(new DerivedTestCase
            {
                CaseId = $"illegal-{t.From}-skip-{t.To}",
                Rule = "SM-EDGE",
                Kind = "invalid",
                Description = $"非法跳跃 {t.From} 直接到 {t.To}（跳过 {t.Event}）",
            });
        }

        foreach (var s in states.Where(x => x.IsTerminal))
        {
            cases.Add(new DerivedTestCase
            {
                CaseId = $"no-outbound-from-{s.StateId}",
                Rule = "SM-TERMINAL",
                Kind = "valid",
                Description = $"终态 {s.StateId} 无出站转移",
            });
        }

        return Deduplicate(cases);
    }

    public static IReadOnlyList<DerivedTestCase> DeriveAll(
        string derivationMode,
        IReadOnlyList<TesterConfirmedField> fields,
        IReadOnlyList<TesterStateTransition> transitions,
        IReadOnlyList<TesterStateNode> states)
    {
        var cases = new List<DerivedTestCase>();
        cases.AddRange(DeriveFieldCases(fields));

        if (derivationMode == "field-and-state-machine")
            cases.AddRange(DeriveStateMachineCases(transitions, states));

        cases = Deduplicate(cases);
        var min = derivationMode == "field-and-state-machine" ? MinFieldAndStateMachine : MinFieldOnly;
        if (cases.Count < min)
        {
            throw new InvalidOperationException(
                $"推导场景数 {cases.Count} < 最低 {min}（mode={derivationMode}）");
        }

        return cases;
    }

    private static List<DerivedTestCase> Deduplicate(List<DerivedTestCase> cases)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<DerivedTestCase>();
        foreach (var c in cases)
        {
            if (seen.Add(c.CaseId))
                list.Add(c);
        }

        return list;
    }
}
