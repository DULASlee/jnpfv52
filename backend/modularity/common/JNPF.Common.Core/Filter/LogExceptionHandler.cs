using JNPF.Common.Const;
using JNPF.Common.Net;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.EventBus;
using JNPF.EventHandler;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using JNPF.Systems.Entitys.System;
using Microsoft.AspNetCore.Mvc.Filters;
using SqlSugar;
using System.Security.Claims;

namespace JNPF.Common.Core.Filter;

/// <summary>
/// 全局异常处理.
/// </summary>
public class LogExceptionHandler : IGlobalExceptionHandler, ISingleton
{
    private readonly IEventPublisher _eventPublisher;

    public LogExceptionHandler(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// 异步写入异常日记.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        var userContext = App.User;
        var httpContext = context.HttpContext;
        var httpRequest = httpContext?.Request;
        var headers = httpRequest?.Headers;
        UserAgent userAgent = new UserAgent(httpContext);

        if (!context.ActionDescriptor.EndpointMetadata.Any(m => m.GetType() == typeof(IgnoreLogAttribute)))
        {
            var userId = userContext?.FindFirstValue(ClaimConst.CLAINMUSERID);
            var userName = userContext?.FindFirstValue(ClaimConst.CLAINMREALNAME);
            var userAccount = userContext?.FindFirstValue(ClaimConst.CLAINMACCOUNT);
            var tenantId = userContext?.FindFirstValue(ClaimConst.TENANTID);

            var ipAddress = NetHelper.Ip;
            var ipAddressName = await NetHelper.GetLocation(ipAddress);

            await _eventPublisher.PublishAsync(new LogEventSource("Log:CreateExLog", tenantId, new SysLogEntity
            {
                Id = SnowflakeIdHelper.NextId(),
                UserId = userId,
                UserName = string.Format("{0}/{1}", userName, userAccount),
                Type = 4,
                IPAddress = ipAddress,
                IPAddressName = ipAddressName,
                RequestURL = httpRequest.Path,
                RequestMethod = httpRequest.Method,
                Json = context.Exception.Message + "\n" + context.Exception.StackTrace + "\n" + context.Exception.TargetSite.GetParameters().ToString(),
                PlatForm = userAgent.OS.ToString(),
                Browser = userAgent.userAgent.ToString(),
                CreatorTime = DateTime.Now
            }));
        }
    }
}