using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Dto.Skills;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills.Bugfix;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 阶段五 P5-B02 — 根据 diff + 根因层生成增量重算计划（≤3 Skill，对齐文档 13 §15 #3）。
/// </summary>
public static class BugfixRerunPlanner
{
    public const int MaxRerunSkills = 3;

    public const string StepDeveloperOrchestrator = "developer-orchestrator";
    public const string StepDesignSkill = "design-skill";
    public const string StepAnalystRerun = "analyst-rerun";

    public static BugfixRerunPlan Build(
        IrDiffResult diff,
        string rootCauseLayer,
        IReadOnlyDictionary<string, string> fragmentIdToType,
        string? revisionType)
    {
        var invalidatedTypes = CollectFragmentTypes(diff.Invalidated, fragmentIdToType);
        var changedTypes = CollectFragmentTypes(diff.Changed, fragmentIdToType);
        var touchedTypes = invalidatedTypes.Union(changedTypes).ToHashSet(StringComparer.Ordinal);

        var steps = new List<BugfixRerunStep>();

        if (rootCauseLayer == BugfixRootCauseClassifier.LayerIr1
            && touchedTypes.Contains(IrFragmentTypes.EventSpec))
        {
            steps.Add(new BugfixRerunStep
            {
                Kind = StepAnalystRerun,
                SkillId = "analyst-skill",
                AnalystInput = new RerunAffectedStepsInput
                {
                    RevisionType = revisionType ?? EventSpecRevisionPlanner.FieldTypeOrConstraint,
                },
            });
        }

        if (invalidatedTypes.Contains(IrFragmentTypes.DDL)
            || changedTypes.Contains(IrFragmentTypes.DDL))
        {
            steps.Add(new BugfixRerunStep
            {
                Kind = StepDesignSkill,
                SkillId = DesignSkillIds.DbDesign,
            });
        }

        if (invalidatedTypes.Contains(IrFragmentTypes.Architecture)
            || changedTypes.Contains(IrFragmentTypes.Architecture))
        {
            steps.Add(new BugfixRerunStep
            {
                Kind = StepDesignSkill,
                SkillId = DesignSkillIds.Architect,
            });
        }

        if (invalidatedTypes.Contains(IrFragmentTypes.FormPageIR)
            || changedTypes.Contains(IrFragmentTypes.FormPageIR))
        {
            steps.Add(new BugfixRerunStep
            {
                Kind = StepDesignSkill,
                SkillId = DesignSkillIds.UiDesign,
            });
        }

        if (invalidatedTypes.Contains(IrFragmentTypes.GeneratedCode)
            || invalidatedTypes.Contains(IrFragmentTypes.TestSuite)
            || changedTypes.Contains(IrFragmentTypes.GeneratedCode)
            || changedTypes.Contains(IrFragmentTypes.TestSuite))
        {
            steps.Add(new BugfixRerunStep
            {
                Kind = StepDeveloperOrchestrator,
                SkillId = DevelopmentSkillIds.Developer,
            });
        }

        steps = DeduplicateSteps(steps);

        if (steps.Count > MaxRerunSkills)
        {
            throw new InvalidOperationException(
                $"Bugfix 重算 Skill 数 {steps.Count} 超过上限 {MaxRerunSkills}");
        }

        if (steps.Count == 0)
        {
            throw new InvalidOperationException(
                "Bugfix 无法生成重算计划：diff 非空但无匹配 Skill");
        }

        return new BugfixRerunPlan
        {
            RootCauseLayer = rootCauseLayer,
            Steps = steps,
            PreservedFragmentTypes = ComputePreservedTypes(touchedTypes, steps),
        };
    }

    private static HashSet<string> ComputePreservedTypes(HashSet<string> touchedTypes, List<BugfixRerunStep> steps)
    {
        var rerunTypes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in steps)
        {
            switch (step.SkillId)
            {
                case var id when id == DesignSkillIds.Architect:
                    rerunTypes.Add(IrFragmentTypes.Architecture);
                    break;
                case var id when id == DesignSkillIds.DbDesign:
                    rerunTypes.Add(IrFragmentTypes.DDL);
                    break;
                case var id when id == DesignSkillIds.UiDesign:
                    rerunTypes.Add(IrFragmentTypes.FormPageIR);
                    break;
                case var id when id == DevelopmentSkillIds.Developer:
                    rerunTypes.Add(IrFragmentTypes.GeneratedCode);
                    rerunTypes.Add(IrFragmentTypes.TestSuite);
                    break;
            }
        }

        var preserved = new HashSet<string>(StringComparer.Ordinal)
        {
            IrFragmentTypes.Architecture,
            IrFragmentTypes.FormPageIR,
            IrFragmentTypes.SystemDesign,
        };

        foreach (var t in rerunTypes)
            preserved.Remove(t);

        foreach (var t in touchedTypes)
            preserved.Remove(t);

        return preserved;
    }

    private static List<BugfixRerunStep> DeduplicateSteps(List<BugfixRerunStep> steps)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<BugfixRerunStep>();
        foreach (var step in steps)
        {
            var key = $"{step.Kind}:{step.SkillId}";
            if (seen.Add(key))
                list.Add(step);
        }

        return list;
    }

    private static HashSet<string> CollectFragmentTypes(
        IReadOnlyList<string> fragmentIds,
        IReadOnlyDictionary<string, string> fragmentIdToType)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in fragmentIds)
        {
            if (fragmentIdToType.TryGetValue(id, out var type) && !string.IsNullOrWhiteSpace(type))
                set.Add(type);
        }

        return set;
    }
}

public sealed class BugfixRerunPlan
{
    public string RootCauseLayer { get; init; } = string.Empty;
    public IReadOnlyList<BugfixRerunStep> Steps { get; init; } = Array.Empty<BugfixRerunStep>();
    /// <summary>重算后须保持 payload 不变的片段类型（D3 验收）。</summary>
    public IReadOnlySet<string> PreservedFragmentTypes { get; init; } = new HashSet<string>();
}

public sealed class BugfixRerunStep
{
    public string Kind { get; init; } = string.Empty;
    public string SkillId { get; init; } = string.Empty;
    public RerunAffectedStepsInput? AnalystInput { get; init; }
}
