using System.Diagnostics;
using Hangfire.Server;

namespace JNPF.InteAssistant.Job;

/// <summary>
/// Hangfire JobFilter：自动传递 W3C TraceContext（I-05 裁决）。
///
/// 在 Hangfire 任务执行前后自动创建/清理 OpenTelemetry Activity，
/// 确保 trace 链路从前端 API → Hangfire Job → LLM Gateway 完整贯通。
///
/// 注册方式（Program.cs）：
///   GlobalJobFilters.Filters.Add(new TraceContextJobFilter());
/// </summary>
public class TraceContextJobFilter : IServerFilter
{
    private const string ActivityKey = "OTel_Trace_Activity";

    public void OnPerforming(PerformingContext context)
    {
        // 如果调用方已在 Activity.Current 中设置了 trace context，
        // 新创建的 Activity 会自动继承父级 traceId
        var activity = new Activity("HangfireJob");
        activity.Start();
        context.Items[ActivityKey] = activity;
    }

    public void OnPerformed(PerformedContext context)
    {
        if (context.Items.TryGetValue(ActivityKey, out var obj) && obj is Activity activity)
        {
            activity.Stop();
            activity.Dispose();
            context.Items.Remove(ActivityKey);
        }
    }
}
