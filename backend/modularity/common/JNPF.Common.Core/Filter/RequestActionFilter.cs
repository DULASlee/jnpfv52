using JNPF.Common.Const;
using JNPF.Common.Core.Diagnostics;
using JNPF.Common.Net;
using JNPF.Common.Security;
using JNPF.EventBus;
using JNPF.EventHandler;
using JNPF.Logging.Attributes;
using JNPF.Systems.Entitys.System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace JNPF.Common.Core.Filter;

/// <summary>
/// 请求日志拦截.
/// </summary>
public class RequestActionFilter : IAsyncActionFilter
{
    /// <summary>
    /// 事件总线.
    /// </summary>
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// 日志.
    /// </summary>
    private readonly ILogger<RequestActionFilter> _logger;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public RequestActionFilter(IEventPublisher eventPublisher, ILogger<RequestActionFilter> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    /// <summary>
    /// 请求日记写入.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userContext = App.User;
        var httpContext = context.HttpContext;
        var httpRequest = httpContext.Request;
        UserAgent userAgent = new UserAgent(httpContext);

        // Read LogPolicy from action metadata
        var policy = LogPolicy.Full;
        var policyAttr = context.ActionDescriptor.EndpointMetadata
            .OfType<LogPolicyAttribute>().FirstOrDefault();
        if (policyAttr != null)
        {
            policy = policyAttr.Policy;
        }

        // Check IgnoreAll
        if (policy.HasFlag(LogPolicy.IgnoreAll))
        {
            await next();
            return;
        }

        // Also support legacy [IgnoreLog] during transition
        if (context.ActionDescriptor.EndpointMetadata.Any(m => m.GetType() == typeof(IgnoreLogAttribute)))
        {
            await next();
            return;
        }

        // ── agent-probe 诊断探针钩子 ──
        var diagHeader = context.HttpContext.Request.Headers["X-Diagnostics"].FirstOrDefault();
        DiagnosticsProbe? probe = null;
        if (!string.IsNullOrEmpty(diagHeader))
        {
            try { probe = JsonSerializer.Deserialize<DiagnosticsProbe>(diagHeader); } catch { }
            if (probe != null)
            {
                DiagnosticsLog.Log(probe.Category, "request_begin",
                    new { method = context.HttpContext.Request.Method, path = context.HttpContext.Request.Path.ToString() },
                    probe.Level);
            }
        }

        Stopwatch sw = new Stopwatch();
        sw.Start();
        var actionContext = await next();
        sw.Stop();

        var traceId = httpContext.Items["TraceId"]?.ToString() ?? "unknown";
        var userId = userContext?.FindFirstValue(ClaimConst.CLAINMUSERID);
        var userName = userContext?.FindFirstValue(ClaimConst.CLAINMREALNAME);
        var userAccount = userContext?.FindFirstValue(ClaimConst.CLAINMACCOUNT);
        var tenantId = userContext?.FindFirstValue(ClaimConst.TENANTID);

        var ipAddress = NetHelper.Ip;
        var ipAddressName = await NetHelper.GetLocation(ipAddress);

        // Summary mode serialization
        string args = null;
        if (!policy.HasFlag(LogPolicy.IgnoreRequest))
        {
            args = SerializeAsSummary(context.ActionArguments);
        }

        string resultJson = null;
        if (!policy.HasFlag(LogPolicy.IgnoreResponse))
        {
            var result = (actionContext.Result as JsonResult)?.Value;
            resultJson = result?.ToJsonString();
            if (resultJson != null && resultJson.Length > 500)
            {
                resultJson = resultJson[..500] + $"...(truncated, total {resultJson.Length} chars)";
            }
        }

        try
        {
            await _eventPublisher.PublishAsync(new LogEventSource("Log:CreateReLog", tenantId, new SysLogEntity
            {
                Id = SnowflakeIdHelper.NextId(),
                UserId = userId,
                UserName = string.Format("{0}/{1}", userName, userAccount),
                Type = 5,
                IPAddress = ipAddress,
                IPAddressName = ipAddressName,
                RequestURL = httpRequest.Path,
                RequestDuration = (int)sw.ElapsedMilliseconds,
                RequestMethod = httpRequest.Method,
                PlatForm = userAgent.OS.ToString(),
                Browser = userAgent.userAgent.ToString(),
                CreatorTime = DateTime.Now,
                RequestParam = args,
                RequestTarget = context.ActionDescriptor.DisplayName,
                Json = resultJson,
                TraceId = traceId,
                TenantId = tenantId
            }));

            if (context.ActionDescriptor.EndpointMetadata.Any(m => m.GetType() == typeof(OperateLogAttribute)))
            {
                var module = context.ActionDescriptor.EndpointMetadata
                    .Where(x => x.GetType() == typeof(OperateLogAttribute))
                    .FirstOrDefault() as OperateLogAttribute;

                await _eventPublisher.PublishAsync(new LogEventSource("Log:CreateOpLog", tenantId, new SysLogEntity
                {
                    Id = SnowflakeIdHelper.NextId(),
                    UserId = userId,
                    UserName = string.Format("{0}/{1}", userName, userAccount),
                    Type = 3,
                    IPAddress = ipAddress,
                    IPAddressName = ipAddressName,
                    RequestURL = httpRequest.Path,
                    RequestDuration = (int)sw.ElapsedMilliseconds,
                    RequestMethod = httpRequest.Method,
                    PlatForm = userAgent.OS.ToString(),
                    Browser = userAgent.userAgent.ToString(),
                    CreatorTime = DateTime.Now,
                    ModuleName = module.ModuleName,
                    RequestParam = args,
                    RequestTarget = context.ActionDescriptor.DisplayName,
                    Json = resultJson,
                    TraceId = traceId,
                    TenantId = tenantId
                }));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    /// <summary>
    /// Serialize action arguments in summary mode, truncating large values.
    /// </summary>
    private string SerializeAsSummary(IDictionary<string, object> args)
    {
        if (args == null || args.Count == 0) return null;

        var summaries = args.Select(kv =>
        {
            var value = kv.Value;
            if (value == null) return $"{kv.Key}=null";

            var json = value.ToJsonString();
            if (json.Length > 200)
            {
                return $"{kv.Key}={json[..200]}...(truncated, total {json.Length} chars)";
            }
            return $"{kv.Key}={json}";
        });

        return "{" + string.Join(", ", summaries) + "}";
    }
}