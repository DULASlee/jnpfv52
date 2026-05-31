using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.EventBus;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.EventHandler;

/// <summary>
/// 日记事件订阅.
/// </summary>
public class LogEventSubscriber : IEventSubscriber, ISingleton
{
    private readonly ISqlSugarClient _sqlSugarClient;
    private readonly ITenantManager _tenantManager;
    private readonly IUserManager _userManager;
    private readonly ILogger<LogEventSubscriber> _logger;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public LogEventSubscriber(
        ISqlSugarClient sqlSugarClient,
        IUserManager userManager,
        ITenantManager tenantManager,
        ILogger<LogEventSubscriber> logger)
    {
        _sqlSugarClient = sqlSugarClient;
        _userManager = userManager;
        _tenantManager = tenantManager;
        _logger = logger;
    }

    /// <summary>
    /// 创建日记.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    [EventSubscribe("Log:CreateReLog")]
    [EventSubscribe("Log:CreateExLog")]
    [EventSubscribe("Log:CreateVisLog")]
    [EventSubscribe("Log:CreateOpLog")]
    public async Task CreateLog(EventHandlerExecutingContext context)
    {
        var log = (LogEventSource)context.Source;

        try
        {
            // CopyNew() MUST be called BEFORE ChangTenant to isolate tenant state.
            // ChangTenant mutates the SqlSugarScope's internal connections/filters;
            // calling it on the shared singleton would leak tenant state across requests.
            var db = _sqlSugarClient.CopyNew();

            if (log.TenantId.IsNotEmptyOrNull())
            {
                await _tenantManager.ChangTenant(db, log.TenantId);
            }

            await db.Insertable(log.Entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Operation log write failed, TraceId={TraceId}, TenantId={TenantId}",
                log.Entity.TraceId, log.TenantId);
        }
    }
}