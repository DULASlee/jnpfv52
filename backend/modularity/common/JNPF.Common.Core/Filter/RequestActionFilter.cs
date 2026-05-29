using JNPF.Common.Const;
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

        Stopwatch sw = new Stopwatch();
        sw.Start();
        var actionContext = await next();
        sw.Stop();

        // 判断是否请求成功（没有异常就是请求成功）
        var isRequestSucceed = actionContext.Exception == null;
        var headers = httpRequest.Headers;
        if (!context.ActionDescriptor.EndpointMetadata.Any(m => m.GetType() == typeof(IgnoreLogAttribute)))
        {
            var userId = userContext?.FindFirstValue(ClaimConst.CLAINMUSERID);
            var userName = userContext?.FindFirstValue(ClaimConst.CLAINMREALNAME);
            var userAccount = userContext?.FindFirstValue(ClaimConst.CLAINMACCOUNT);
            var tenantId = userContext?.FindFirstValue(ClaimConst.TENANTID);

            var ipAddress = NetHelper.Ip;
            var ipAddressName = await NetHelper.GetLocation(ipAddress);
            var args = context.ActionArguments.ToJsonString();
            var result = (actionContext.Result as JsonResult)?.Value;

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
                    Json = result?.ToJsonString()
                }));

                if (context.ActionDescriptor.EndpointMetadata.Any(m => m.GetType() == typeof(OperateLogAttribute)))
                {
                    // 操作参数
                    var module = context.ActionDescriptor.EndpointMetadata.Where(x => x.GetType() == typeof(OperateLogAttribute)).ToList().FirstOrDefault() as OperateLogAttribute;

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
                        Json = result?.ToJsonString()
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}