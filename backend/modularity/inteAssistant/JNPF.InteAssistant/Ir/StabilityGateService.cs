using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using System.Text.Json;

namespace JNPF.InteAssistant.Ir;

public interface IStabilityGateService
{
    /// <summary>
    /// SA 九步完成后是否应追加 FragmentStabilized 事件
    /// </summary>
    bool ShouldStabilize(AiIrFragmentSnapshotEntity snapshot, string triggeringEventType);
}

/// <summary>
/// TDR-002 稳定性门控 MVP：9 个 SA 步骤全部完成 → stable
/// </summary>
public sealed class StabilityGateService : IStabilityGateService, ITransient
{
    public bool ShouldStabilize(AiIrFragmentSnapshotEntity snapshot, string triggeringEventType)
    {
        if (triggeringEventType != IrEventTypes.SaStepCompleted)
            return false;

        if (snapshot.StabilityState == IrStabilityStates.Stable
            || snapshot.StabilityState == IrStabilityStates.Locked)
            return false;

        var completed = ParseDistinctSteps(snapshot.SaStepsCompleted);
        return completed.Count >= IrSaSteps.All.Length;
    }

    private static HashSet<string> ParseDistinctSteps(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new HashSet<string>(StringComparer.Ordinal);

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            return list.Where(s => !string.IsNullOrWhiteSpace(s))
                .ToHashSet(StringComparer.Ordinal);
        }
        catch
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }
}
