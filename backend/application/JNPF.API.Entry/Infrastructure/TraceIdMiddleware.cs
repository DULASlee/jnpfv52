using System.Diagnostics;
using Serilog.Context;

namespace JNPF.API.Entry.Infrastructure;

/// <summary>
/// TraceId middleware - generates unique trace ID per request, injects into response header and context.
/// </summary>
public class TraceIdMiddleware
{
    private const string TraceIdHeader = "X-Trace-Id";
    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Priority: request header (distributed) > Activity.Current > new GUID
        var traceId = context.Request.Headers[TraceIdHeader].FirstOrDefault()
                      ?? Activity.Current?.Id
                      ?? Guid.NewGuid().ToString("N");

        context.Items["TraceId"] = traceId;
        TraceContext.TraceId = traceId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[TraceIdHeader] = traceId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("TraceId", traceId))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Async context TraceId propagation (works across Task.Run/ThreadPool).
/// </summary>
public static class TraceContext
{
    private static readonly AsyncLocal<string> _traceId = new();

    public static string TraceId
    {
        get => _traceId.Value ?? "unknown";
        set => _traceId.Value = value;
    }
}
