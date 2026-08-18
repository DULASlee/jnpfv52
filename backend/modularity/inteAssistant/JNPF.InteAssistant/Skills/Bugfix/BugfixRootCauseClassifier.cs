using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;

namespace JNPF.InteAssistant.Skills.Bugfix;

/// <summary>
/// Bug 根因层判定（IR-1 / IR-2 / IR-3）。
/// </summary>
public static class BugfixRootCauseClassifier
{
    public const string LayerIr1 = "IR-1";
    public const string LayerIr2 = "IR-2";
    public const string LayerIr3 = "IR-3";

    public static string Classify(
        IrDiffResult diff,
        IReadOnlyDictionary<string, string> fragmentIdToType,
        string? overrideLayer)
    {
        if (!string.IsNullOrWhiteSpace(overrideLayer))
            return overrideLayer.Trim();

        var touchedTypes = diff.Changed
            .Concat(diff.Invalidated)
            .Concat(diff.Added)
            .Select(id => fragmentIdToType.TryGetValue(id, out var t) ? t : null)
            .Where(t => t != null)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (touchedTypes.Any(t => t == IrFragmentTypes.EventSpec))
            return LayerIr1;

        if (touchedTypes.Any(t =>
                t is IrFragmentTypes.Architecture
                    or IrFragmentTypes.DDL
                    or IrFragmentTypes.FormPageIR
                    or IrFragmentTypes.SystemDesign))
        {
            return LayerIr2;
        }

        return LayerIr3;
    }
}
