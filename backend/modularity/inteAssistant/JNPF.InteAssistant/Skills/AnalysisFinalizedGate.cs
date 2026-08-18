using System.Text.Json;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;

namespace JNPF.InteAssistant.Skills;

/// <summary>
/// 设计阶段前置门禁（25 §6）：须 AnalysisCompleted.finalized=true。
/// Round 1/2 也会写 AnalysisCompleted，但 finalized=false，不得启动设计四 Skill。
/// </summary>
public static class AnalysisFinalizedGate
{
    /// <summary>扫描 IR 事件，是否存在 finalized=true 的 AnalysisCompleted。</summary>
    public static async Task<bool> HasFinalizedAsync(
        IIrEventStoreService eventStore,
        string tenantId,
        string projectId,
        long pipelineId,
        CancellationToken ct = default)
    {
        var events = await eventStore.ListEventsAsync(projectId, tenantId, pipelineId.ToString(), ct);
        foreach (var evt in events)
        {
            if (!string.Equals(evt.EventType, IrEventTypes.AnalysisCompleted, StringComparison.Ordinal))
                continue;
            if (IsFinalizedPayload(evt.PayloadPreview))
                return true;
        }
        return false;
    }

    public static bool IsFinalizedPayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return false;
        try
        {
            using var doc = JsonDocument.Parse(payload);
            return doc.RootElement.TryGetProperty("finalized", out var f)
                   && f.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public const string NotFinalizedMessage =
        "需求分析未 Finalize（缺少 AnalysisCompleted.finalized=true），请先完成三轮需求分析工程保障";
}
