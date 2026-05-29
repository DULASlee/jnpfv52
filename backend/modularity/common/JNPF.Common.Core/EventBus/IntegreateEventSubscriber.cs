using JNPF.ClayObject;
using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Manager;
using JNPF.Common.Models.InteAssistant;
using JNPF.Common.Models.Job;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Engine.Entity.Model.Integrate;
using JNPF.EventBus;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.JsonSerialization;
using JNPF.RemoteRequest.Extensions;
using JNPF.Schedule;
using JNPF.Systems.Entitys.Permission;
using JNPF.UnifyResult;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace JNPF.EventHandler;

/// <summary>
/// 集成事件订阅.
/// </summary>
public class IntegreateEventSubscriber : IEventSubscriber, ISingleton, IDisposable
{
    /// <summary>
    /// 初始化客户端.
    /// </summary>
    private static ISqlSugarClient? _sqlSugarClient;

    /// <summary>
    /// 服务提供器.
    /// </summary>
    private readonly IServiceScope _serviceScope;

    /// <summary>
    /// 作业计划工厂服务.
    /// </summary>
    private readonly ISchedulerFactory _schedulerFactory;

    /// <summary>
    /// 调度管理.
    /// </summary>
    private readonly IJobManager _jobManager;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 构造函数.
    /// </summary>
    public IntegreateEventSubscriber(
        IServiceScopeFactory serviceScopeFactory,
        ISqlSugarClient sqlSugarClient,
        IJobManager jobManager,
        ITenantManager tenantManager,
        ISchedulerFactory schedulerFactory)
    {
        _serviceScope = serviceScopeFactory.CreateScope();
        _sqlSugarClient = sqlSugarClient;
        _jobManager = jobManager;
        _schedulerFactory = schedulerFactory;
        _tenantManager = tenantManager;
    }

    /// <summary>
    /// 创建触发集成.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    [EventSubscribe("Inte:CreateInte")]
    public async Task CreateInte(EventHandlerExecutingContext context)
    {
        var inte = (InteEventSource)context.Source;
        string cacheKey = string.Empty;

        var _cacheManager = _serviceScope.ServiceProvider.GetService<ICacheManager>();

        if (KeyVariable.MultiTenancy && !string.IsNullOrEmpty(inte.TenantId) && !_sqlSugarClient.AsTenant().IsAnyConnection(inte.TenantId))
        {
            await _tenantManager.ChangTenant(_sqlSugarClient, inte.TenantId);
        }

        _sqlSugarClient = _sqlSugarClient.CopyNew();

        // 集成助手表单ID一致 且 未删除 且 状态启用 且 触发事件作为 一致的数据
        List<IntegrateEntity>? inteAssiEntity = new List<IntegrateEntity>();
        switch (inte.Model.TriggerType)
        {
            case 4:
                inteAssiEntity = await _sqlSugarClient.Queryable<IntegrateEntity>().Where(it => it.Type.Equals(1) && it.FormId.Equals(inte.Model.ModelId) && it.DeleteMark == null && it.EnabledMark.Equals(1) && it.TriggerType.Equals(1)).ToListAsync();
                break;
            case 5:
                inteAssiEntity = await _sqlSugarClient.Queryable<IntegrateEntity>().Where(it => it.Type.Equals(1) && it.FormId.Equals(inte.Model.ModelId) && it.DeleteMark == null && it.EnabledMark.Equals(1) && it.TriggerType.Equals(3)).ToListAsync();
                break;
            default:
                inteAssiEntity = await _sqlSugarClient.Queryable<IntegrateEntity>().Where(it => it.Type.Equals(1) && it.FormId.Equals(inte.Model.ModelId) && it.DeleteMark == null && it.EnabledMark.Equals(1) && it.TriggerType.Equals(inte.Model.TriggerType)).ToListAsync();
                break;
        }

        // 有集成助手记录
        switch (inteAssiEntity.Count > 0)
        {
            case true:
                var triggerType = string.Empty;
                List<IntegrateQueueEntity> inteQueueList = new List<IntegrateQueueEntity>();
                RESTfulResult<object> result = new RESTfulResult<object>();

                switch (inte.Model.TriggerType)
                {
                    case 4:
                    case 5:
                        break;
                    default:
                        var value = inte.Model.Data.ToObject<Dictionary<string, object>>();
                        value["id"] = inte.Model.DataId;
                        inte.Model.Data = value.ToJsonString();
                        break;
                }

                switch (inte.Model.TriggerType)
                {
                    case 4:
                        triggerType = "批量新增";
                        break;
                    case 5:
                        triggerType = "批量删除";
                        List<string> ids = inte.Model.Data.ToObject<List<string>>();
                        ids.ForEach(id =>
                        {
                            var data = new InteAssiDataModel { DataId = inte.Model.DataId, Data = inte.Model.Data }.ToJsonString();
                            inteAssiEntity.ForEach(item => inteQueueList.Add(GetAddIntegrateQueueEntity(item.FullName, item.Id, 0, inte.Model.Data, inte.UserId)));
                        });
                        break;
                    case 3:
                        {
                            var data = new InteAssiDataModel { DataId = inte.Model.DataId, Data = inte.Model.Data }.ToJsonString();
                            foreach (var item in inteAssiEntity)
                            {
                                var templateJson = item.TemplateJson?.ToObject<DesignModel>();

                                // 集成模版不能为空
                                if (templateJson != null)
                                {
                                    var ruleMatchLogic = templateJson?.properties.ruleMatchLogic;
                                    var ruleList = templateJson?.properties.ruleList;
                                    if (!string.IsNullOrEmpty(ruleMatchLogic) && ruleList?.Count > 0)
                                    {
                                        // 新增一条集成助手数据
                                        var clay = Clay.Parse(inte.Model.Data);
                                        clay.f_inte_assistant = 1;
                                        string parameter = new { id = string.Empty, data = clay.ToString(), isInteAssis = false }.ToJsonString();
                                        var requestAddress = string.Format("/api/visualdev/OnlineDev/{0}", templateJson.properties.formId);

                                        result = await InteAssistantHttpClient(1, requestAddress, parameter, inte.UserId, inte.TenantId);

                                        // 查询列表
                                        string superQuery = new { matchLogic = ruleMatchLogic, conditionList = ruleList }.ToJsonString();
                                        parameter = new { currentPage = 1, modelId = templateJson.properties.formId, pageSize = 999999, superQueryJson = superQuery, isOnlyId = 1, isInteAssisData = 1 }.ToJsonString();
                                        requestAddress = string.Format("/api/visualdev/OnlineDev/{0}/List", templateJson.properties.formId);

                                        result = await InteAssistantHttpClient(1, requestAddress, parameter, inte.UserId, inte.TenantId);
                                        var resultData = Clay.Parse(result.data.ToString());
                                        List<object> idList = resultData.list.Deserialize<List<object>>();
                                        switch (idList.Count > 0)
                                        {
                                            case true:
                                                inteQueueList.Add(GetAddIntegrateQueueEntity(item.FullName, item.Id, 0, data, inte.UserId));
                                                break;
                                        }

                                        // 删除
                                        requestAddress = string.Format("/api/visualdev/OnlineDev/DelInteAssistant/{0}", templateJson.properties.formId);
                                        result = await InteAssistantHttpClient(3, requestAddress, null, inte.UserId, inte.TenantId);
                                    }
                                    else
                                    {
                                        inteQueueList.Add(GetAddIntegrateQueueEntity(item.FullName, item.Id, 0, data, inte.UserId));
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        {
                            var data = new InteAssiDataModel { DataId = inte.Model.DataId, Data = inte.Model.Data }.ToJsonString();
                            foreach (var item in inteAssiEntity)
                            {
                                var templateJson = item.TemplateJson?.ToObject<DesignModel>();

                                // 集成模版不能为空
                                if (templateJson != null)
                                {
                                    var ruleMatchLogic = templateJson?.properties.ruleMatchLogic;
                                    var ruleList = templateJson?.properties.ruleList;
                                    if (!string.IsNullOrEmpty(ruleMatchLogic) && ruleList?.Count > 0)
                                    {
                                        var dicHerader = new Dictionary<string, object>();

                                        var user = await _sqlSugarClient.Queryable<UserEntity>().FirstAsync(it => it.Id.Equals(inte.UserId));

                                        // 生成实时token
                                        var toKen = NetHelper.GetToken(user.Id, user.Account, user.RealName, user.IsAdministrator, inte.TenantId);
                                        dicHerader.Add("Authorization", toKen);

                                        string superQuery = new { matchLogic = ruleMatchLogic, conditionList = templateJson?.properties.ruleList }.ToJsonString();
                                        string parameter = new { currentPage = 1, modelId = templateJson?.properties.formId, pageSize = 999999, superQueryJson = superQuery, isOnlyId = 1 }.ToJsonString();
                                        var localAddress = GetLocalAddress();
                                        var path = string.Format("{0}/api/visualdev/OnlineDev/{1}/List", localAddress, templateJson.properties.formId);

                                        result = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(parameter).PostAsStringAsync()).ToObject<RESTfulResult<object>>();
                                        if (result.data.ToJsonString().Contains(inte.Model.DataId))
                                        {
                                            inteQueueList.Add(GetAddIntegrateQueueEntity(item.FullName, item.Id, 0, data, inte.UserId));
                                        }
                                    }
                                    else
                                    {
                                        inteQueueList.Add(GetAddIntegrateQueueEntity(item.FullName, item.Id, 0, data, inte.UserId));
                                    }
                                }
                            }
                        }
                        break;
                }

                var result1 = await _sqlSugarClient.Insertable(inteQueueList).ExecuteCommandAsync();

                // 添加队列的数量与实际添加的数量一致
                if (result1 == inteQueueList.Count)
                {
                    // 添加Redis 缓存避免执行多余的逻辑
                    cacheKey = string.Format("{0}:{1}", CommonConst.INTEASSISTANT, inte.TenantId);
                    List<string> caCheIntegrateList = await _cacheManager.GetAsync<List<string>>(cacheKey);
                    caCheIntegrateList ??= new List<string>();
                    caCheIntegrateList?.AddRange(inteQueueList.Select(it => it.Id).ToList());
                    await _cacheManager.SetAsync(cacheKey, caCheIntegrateList);

                    var triggerId = string.Format("Integrate_trigger_schedule_{0}", inte.TenantId);

                    // 判断该租户的集成助手执行队列调度器是否存在
                    var scheduleResult = _schedulerFactory.TryGetJob("job_builtIn_ExecutionQueue", out var scheduler);

                    // 集成助手-执行队列 触发器只有在用户创建定时触发或者触发`事件触发`后创建
                    if (!scheduler.ContainsTrigger(triggerId))
                    {
                        TriggerBuilder? triggerBuilder = _jobManager.ObtainTriggerBuilder(new JobTriggerModel
                        {
                            TriggreId = triggerId,
                            Description = string.Format("租户`{0}`集成助手-执行队列调度器", inte.TenantId),
                            StartTime = DateTime.Now,
                            EndTime = null,
                        });

                        triggerBuilder.AlterToSecondly();

                        scheduler.AddTrigger(triggerBuilder);
                    }
                    else
                    {
                        scheduler.StartTrigger(triggerId);
                    }
                }

                break;
        }
    }

    public void Dispose()
    {
        _serviceScope.Dispose();
    }

    /// <summary>
    /// 获取新增集成助手队列信息.
    /// </summary>
    /// <param name="fullName"></param>
    /// <param name="integrateId"></param>
    /// <param name="state"></param>
    /// <param name="description"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public IntegrateQueueEntity GetAddIntegrateQueueEntity(string fullName, string integrateId, int state, string description, string userId)
    {
        return new IntegrateQueueEntity()
        {
            Id = SnowflakeIdHelper.NextId(),
            FullName = fullName,
            IntegrateId = integrateId,
            ExecutionTime = null,
            State = state,
            Description = description,
            CreatorTime = DateTime.Now,
            CreatorUserId = userId,
            EnabledMark = 1,
        };
    }

    /// <summary>
    /// 获取当前进程地址.
    /// </summary>
    /// <returns></returns>
    private string GetLocalAddress()
    {
        var server = _serviceScope.ServiceProvider.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses;
        return addresses?.FirstOrDefault()?.Replace("[::]", "localhost");
    }

    /// <summary>
    /// 集成助手远程请求客户端.
    /// </summary>
    /// <param name="requestModeType">请求方式.</param>
    /// <param name="requestAddress">请求地址.</param>
    /// <param name="parameter">请求参数.</param>
    /// <param name="userId">token用户ID.</param>
    /// <param name="tenantId">token租户.</param>
    /// <returns></returns>
    private async Task<RESTfulResult<object>> InteAssistantHttpClient(int requestModeType, string requestAddress, object parameter, string userId, string tenantId)
    {
        var response = new RESTfulResult<object>();

        var dicHerader = new Dictionary<string, object>();
        var user = await _sqlSugarClient.Queryable<UserEntity>().FirstAsync(it => it.Id.Equals(userId));

        // 生成实时token
        var toKen = NetHelper.GetToken(user.Id, user.Account, user.RealName, user.IsAdministrator, tenantId);
        dicHerader.Add("Authorization", toKen);

        var localAddress = GetLocalAddress();

        var path = string.Format("{0}{1}", localAddress, requestAddress);

        switch (requestModeType)
        {
            // post
            case 1:
                response = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(parameter).PostAsStringAsync()).ToObject<RESTfulResult<object>>();
                break;

            // put
            case 2:
                response = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(parameter).PutAsStringAsync()).ToObject<RESTfulResult<object>>();
                break;

            //Delete
            case 3:
                response = (await path.SetJsonSerialization<NewtonsoftJsonSerializerProvider>().SetContentType("application/json").SetHeaders(dicHerader).SetBody(parameter).DeleteAsStringAsync()).ToObject<RESTfulResult<object>>();
                break;
        }

        return response;
    }
}