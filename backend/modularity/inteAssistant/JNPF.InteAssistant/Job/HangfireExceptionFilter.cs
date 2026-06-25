using Hangfire.Server;
using Serilog;
using System.Diagnostics;

namespace JNPF.InteAssistant.Job;

/// <summary>
/// Hangfire 后台任务异常过滤器。
/// 捕获所有 Hangfire 任务异常并写入 Serilog（与 HTTP 异常同一日志通道），
/// 确保后台任务错误可追踪、可定位。
///
/// 注册方式（PipelineModuleInitializer.cs）：
///   GlobalJobFilters.Filters.Add(new HangfireExceptionFilter());
/// </summary>
public class HangfireExceptionFilter : IServerFilter
{
    public void OnPerforming(PerformingContext context)
    {
        // 从 Hangfire Job 参数中尝试提取上游传递的 traceId
        var traceId = TryExtractTraceId(context);
        if (!string.IsNullOrEmpty(traceId))
        {
            // 恢复 W3C trace context，确保 Hangfire 任务的日志与上游 HTTP 请求串联
            var activity = new Activity("HangfireJob");
            activity.SetParentId(traceId);
            activity.Start();
            context.Items["ExceptionFilter_Activity"] = activity;
        }

        var jobId = context.BackgroundJob.Id;
        var jobType = context.BackgroundJob.Job.Type.Name;
        var method = context.BackgroundJob.Job.Method.Name;

        Log.Debug(
            "[HangfirePerforming] JobId={JobId} | Job={JobType}.{Method} | TraceId={TraceId}",
            jobId, jobType, method, traceId ?? "none");
    }

    public void OnPerformed(PerformedContext context)
    {
        var jobId = context.BackgroundJob.Id;
        var jobType = context.BackgroundJob.Job.Type.Name;
        var method = context.BackgroundJob.Job.Method.Name;
        var traceId = Activity.Current?.TraceId.ToString();

        if (context.Exception != null)
        {
            // 异常写入 Serilog Error 通道（与 HTTP 异常同一通道，可通过 Seq/日志文件统一检索）
            Log.Error(context.Exception,
                "[HangfireException] JobId={JobId} | Job={JobType}.{Method} | TraceId={TraceId}",
                jobId, jobType, method, traceId ?? "none");
        }
        else
        {
            Log.Information(
                "[HangfireCompleted] JobId={JobId} | Job={JobType}.{Method} | TraceId={TraceId}",
                jobId, jobType, method, traceId ?? "none");
        }

        // 清理 Activity
        if (context.Items.TryGetValue("ExceptionFilter_Activity", out var obj) && obj is Activity activity)
        {
            activity.Stop();
            activity.Dispose();
        }
    }

    /// <summary>
    /// 尝试从 Job 参数中提取 traceId（格式: W3C traceparent 或自定义 traceId 参数）
    /// </summary>
    private static string? TryExtractTraceId(PerformingContext context)
    {
        // 方式 1: 直接传递的 traceId 参数
        foreach (var arg in context.BackgroundJob.Job.Args)
        {
            if (arg is string strArg && strArg.Length == 32 && IsHex(strArg))
            {
                return strArg;
            }
        }

        // 方式 2: W3C traceparent 格式 (00-traceId-spanId-01)
        foreach (var arg in context.BackgroundJob.Job.Args)
        {
            if (arg is string strArg && strArg.StartsWith("00-") && strArg.Length >= 55)
            {
                var parts = strArg.Split('-');
                if (parts.Length >= 2 && parts[1].Length == 32)
                {
                    return parts[1];
                }
            }
        }

        return null;
    }

    private static bool IsHex(string s)
    {
        foreach (var c in s)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                return false;
        }
        return true;
    }
}
