using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace JNPF.InteAssistant.Infrastructure.Telemetry;

/// <summary>
/// P6-O01 OpenTelemetry 埋点 — InteAssistant 自定义 ActivitySource。
///
/// 零 NuGet 依赖：用 BCL 内置 System.Diagnostics.ActivitySource（.NET 8 自带 OTel 桥接）。
/// 在 ObservabilityModule 里通过 AddSource("JNPF.Studio") 注册到 OTel SDK。
///
/// 三个核心 Span：
///   skill.run  — SkillHarness.RunAsync 入口（skillId/runId/projectId/tenantId/pipelineId）
///   llm.call   — SkillLlmBudgetGuard.ExecuteAsync（model/tokens）
///   ir.append  — IrEventStoreService.AppendCoreAsync（eventType/fragmentId）
/// </summary>
public static class StudioActivitySource
{
    /// <summary>ActivitySource 名称（ObservabilityModule.AddSource 用）。</summary>
    public const string Name = "JNPF.Studio";

    public static readonly ActivitySource Instance = new(Name, "6.0.0");

    // ── Activity 名称常量 ──
    public const string SkillRun = "skill.run";
    public const string LlmCall = "llm.call";
    public const string IrAppend = "ir.append";

    /// <summary>skill.run Span — Skill 执行全生命周期。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? StartSkillRun(
        string skillId, string runId, string projectId, string tenantId, long pipelineId)
    {
        var activity = Instance.StartActivity(SkillRun, ActivityKind.Internal);
        if (activity == null) return null;

        activity.SetTag("skillId", skillId);
        activity.SetTag("runId", runId);
        activity.SetTag("projectId", projectId);
        activity.SetTag("tenantId", tenantId);
        activity.SetTag("pipelineId", pipelineId);
        return activity;
    }

    /// <summary>llm.call Span — 单次 LLM Gateway 调用。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? StartLlmCall(
        string skillId, string runId, string providerCode, int? maxTokens = null)
    {
        var activity = Instance.StartActivity(LlmCall, ActivityKind.Client);
        if (activity == null) return null;

        activity.SetTag("skillId", skillId);
        activity.SetTag("runId", runId);
        activity.SetTag("provider", providerCode);
        if (maxTokens.HasValue) activity.SetTag("maxTokens", maxTokens.Value);
        return activity;
    }

    /// <summary>ir.append Span — IR 事件追加。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Activity? StartIrAppend(string eventType, string? fragmentId)
    {
        var activity = Instance.StartActivity(IrAppend, ActivityKind.Internal);
        if (activity == null) return null;

        activity.SetTag("eventType", eventType);
        if (!string.IsNullOrEmpty(fragmentId)) activity.SetTag("fragmentId", fragmentId);
        return activity;
    }
}
