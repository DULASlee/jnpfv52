using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.Schedule;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace JNPF.TaskScheduler.Listener;

[JobDetail("job_onlineUser", Description = "清理在线用户", GroupName = "default", Concurrent = false)]
[PeriodSeconds(1, TriggerId = "trigger_onlineUser", Description = "清理在线用户", MaxNumberOfRuns = 1, RunOnStart = true)]
public class OnlineUserJob : IJob
{
    private readonly IServiceProvider _serviceProvider;

    public OnlineUserJob(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync(JobExecutingContext context, CancellationToken stoppingToken)
    {
        using var serviceScope = _serviceProvider.CreateScope();

        var _cacheManager = serviceScope.ServiceProvider.GetService<ICacheManager>();
        var _imHandler = serviceScope.ServiceProvider.GetService<IMHandler>();

        var keys = _cacheManager.GetAllCacheKeys().FindAll(q => q.Contains(CommonConst.CACHEKEYONLINEUSER));

        if (keys.Any())
        {
            foreach (var key in keys)
            {
                var userList = await _cacheManager.GetAsync<List<UserOnlineModel>>(key);
                foreach (var userOnlineModel in userList)
                {
                    _imHandler.SendMessageAsync(userOnlineModel.connectionId, new { method = "logout", msg = "此账号已在其他地方登录" }.ToJsonString());
                }
                await _cacheManager.DelAsync(key);
            }

        }

        var originColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("【" + DateTime.Now + "】服务重启清空在线用户");
        Console.ForegroundColor = originColor;
    }
}