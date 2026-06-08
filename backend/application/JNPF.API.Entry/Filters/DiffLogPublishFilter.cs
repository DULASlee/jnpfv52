using JNPF.Extras.DatabaseAccessor.SqlSugar.DiffLog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JNPF.API.Entry.Filters;

/// <summary>
/// Action Filter：在请求结束时，将收集的 DiffLog 数据通过 IDiffLogPublisher 发布。
/// 阶段 1-4：NoOp（空操作，数据被丢弃）。
/// 阶段 5：替换为 OutboxDiffLogPublisher 后，数据进入 Outbox 管道。
/// </summary>
public class DiffLogPublishFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var resultContext = await next();

        if (resultContext.Exception == null || resultContext.ExceptionHandled)
        {
            try
            {
                var collector = context.HttpContext.RequestServices
                    .GetService<IDiffLogCollector>();
                var publisher = context.HttpContext.RequestServices
                    .GetService<IDiffLogPublisher>();

                if (collector != null && publisher != null && collector.HasPendingData)
                {
                    var logs = collector.GetAndClear();
                    foreach (var log in logs)
                    {
                        await publisher.PublishAsync(log);
                    }
                }
            }
            catch
            {
                // DiffLog 发布失败不应影响业务响应
            }
        }
    }
}
