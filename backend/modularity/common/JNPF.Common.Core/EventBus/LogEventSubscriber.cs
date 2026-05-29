using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.EventBus;
using SqlSugar;

namespace JNPF.EventHandler;

/// <summary>
/// 日记事件订阅.
/// </summary>
public class LogEventSubscriber : IEventSubscriber, ISingleton
{
    /// <summary>
    /// 初始化客户端.
    /// </summary>
    private static SqlSugarScope? _sqlSugarClient;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public LogEventSubscriber(ISqlSugarClient sqlSugarClient, IUserManager userManager, TenantManager tenantManager)
    {
        _sqlSugarClient = (SqlSugarScope)sqlSugarClient;
        _userManager = userManager;
        _tenantManager = tenantManager;
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
         
        if (log.TenantId.IsNotEmptyOrNull())
        {
            await _tenantManager.ChangTenant(_sqlSugarClient, log.TenantId);
        }

        await _sqlSugarClient.CopyNew().Insertable(log.Entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
    }
}