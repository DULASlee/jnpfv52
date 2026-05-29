using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Models.InteAssistant;
using JNPF.Common.Models.Job;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.Logging.Attributes;
using JNPF.Schedule;
using JNPF.VisualDev.Entitys.Dto.WebHook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System.Text;

namespace JNPF.InteAssistant;

/// <summary>
/// 可视化开发基础 .
/// </summary>
[ApiDescriptionSettings(Tag = "VisualDev", Name = "Hooks", Order = 172)]
[Route("api/visualdev/[controller]")]
public class WebHookService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 解析服务作用域工厂服务.
    /// </summary>
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<IntegrateEntity> _repository;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 作业计划工厂服务.
    /// </summary>
    private readonly ISchedulerFactory _schedulerFactory;

    /// <summary>
    /// 调度管理.
    /// </summary>
    private readonly IJobManager _jobManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// 初始化一个<see cref="WebHookService"/>类型的新实例.
    /// </summary>
    public WebHookService(
        IServiceScopeFactory serviceScopeFactory,
        ICacheManager cacheManager,
        ISqlSugarRepository<IntegrateEntity> repository,
        IJobManager jobManager,
        IUserManager userManager,
        ITenantManager tenantManager,
        ISchedulerFactory schedulerFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _cacheManager = cacheManager;
        _repository = repository;
        _jobManager = jobManager;
        _userManager = userManager;
        _schedulerFactory = schedulerFactory;
        _tenantManager = tenantManager;
    }

    #region Get

    /// <summary>
    /// 获取webhookUrl.
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <returns></returns>
    [HttpGet("getUrl")]
    public async Task<GetWebHookUrlOutput> GetWebhookUrl([FromQuery] string id)
    {
        var output = new GetWebHookUrlOutput();
        Random codeRandom = new Random();
        output.enCodeStr = Convert.ToBase64String(Encoding.GetEncoding("utf-8").GetBytes(id));
        output.randomStr = codeRandom.NextLetterAndNumberString(5);
        output.requestUrl = string.Format("/api/visualdev/Hooks/{0}/params/{1}", output.enCodeStr, output.randomStr);
        output.webhookUrl = string.Format("/api/visualdev/Hooks/{0}", output.enCodeStr);
        return output;
    }

    /// <summary>
    /// .
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <param name="randomStr">随机值字符串.</param>
    /// <returns></returns>
    [HttpGet("{id}/start/{randomStr}")]
    public async Task GetWebHookStart(string id, string randomStr)
    {
        await _cacheManager.DelAsync(string.Format("{0}:{1}", CommonConst.INTEGRATEWEBHOOK, randomStr));
        await _cacheManager.SetAsync(string.Format("{0}:{1}:{2}", CommonConst.INTEGRATEWEBHOOK, id, randomStr), _userManager.TenantId, TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 获取缓存的接口参数.
    /// </summary>
    /// <param name="randomStr">随机值字符串.</param>
    /// <returns></returns>
    [HttpGet("getParams/{randomStr}")]
    public async Task<dynamic> GetRedisParams(string randomStr)
    {
        Dictionary<string, string> resultMap = new Dictionary<string, string>();
        var key = string.Format("{0}:{1}", CommonConst.INTEGRATEWEBHOOK, randomStr);
        if (_cacheManager.Exists(key))
            resultMap = await _cacheManager.GetAsync<Dictionary<string, string>>(key);
        List<StaticData> list = new List<StaticData>();
        foreach (var item in resultMap)
        {
            list.Add(new StaticData
            {
                id = item.Key,
                fullName = item.Value,
            });
        }

        return list;
    }

    /// <summary>
    /// WebHook接口参数获取.
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <param name="randomStr">随机值字符串.</param>
    /// <returns></returns>
    [AllowAnonymous]
    [IgnoreLog]
    [HttpGet("{id}/params/{randomStr}")]
    public async Task GetWebhookParams(string id, string randomStr)
    {
        await insertRedis(id, randomStr);
    }

    #endregion

    #region Post

    /// <summary>
    /// 获取WebHook触发.
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <param name="tenantId">租户ID.</param>
    /// <param name="parameter">参数.</param>
    /// <returns></returns>
    [AllowAnonymous]
    [IgnoreLog]
    [HttpPost("{id}")]
    public async Task GetWebHookTrigger(string id, [FromQuery] string tenantId, [FromBody] Dictionary<string, string>? parameter)
    {
        tenantId ??= "default";
        var inteId = Encoding.UTF8.GetString(Convert.FromBase64String(id));

        using var scope = _serviceScopeFactory.CreateScope();
        var sqlSugarClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var cacheKey = string.Empty;

        if (KeyVariable.MultiTenancy)
        {
            await _tenantManager.ChangTenant(sqlSugarClient, tenantId);
        }

        sqlSugarClient = sqlSugarClient.CopyNew();

        if (await sqlSugarClient.Queryable<IntegrateEntity>().AnyAsync(it => it.Id == inteId && it.EnabledMark == 0))
        {
            throw Oops.Oh(ErrorCode.D2602);
        }

        var inteQueueId = SnowflakeIdHelper.NextId();
        var data = new InteAssiDataModel
        {
            Data = parameter?.ToJsonString()
        }.ToJsonString();
        sqlSugarClient.Queryable<IntegrateEntity>().Where(it => it.Id.Equals(inteId)).Select(it => new { F_ID = inteQueueId, F_INTEGRATE_ID = it.Id, F_FULL_NAME = it.FullName, F_STATE = 0, F_ENABLED_MARK = 1, F_DESCRIPTION = data, F_TENANT_ID = it.TenantId }).IntoTable<IntegrateQueueEntity>();

        cacheKey = string.Format("{0}:{1}", CommonConst.INTEASSISTANT, tenantId);
        List<string> caCheIntegrateList = await _cacheManager.GetAsync<List<string>>(cacheKey);
        caCheIntegrateList ??= new List<string>();
        caCheIntegrateList?.Add(inteQueueId);
        await _cacheManager.SetAsync(cacheKey, caCheIntegrateList);

        // 判断该租户的集成助手执行队列调度器是否存在
        var triggerId = string.Format("Integrate_trigger_schedule_{0}", tenantId);

        // 判断该租户的集成助手执行队列调度器是否存在
        var scheduleResult = _schedulerFactory.TryGetJob("job_builtIn_ExecutionQueue", out var scheduler);

        // 集成助手-执行队列 触发器只有在用户创建定时触发或者触发`事件触发`后创建
        if (!scheduler.ContainsTrigger(triggerId))
        {
            TriggerBuilder? triggerBuilder = _jobManager.ObtainTriggerBuilder(new JobTriggerModel
            {
                TriggreId = triggerId,
                Description = string.Format("租户`{0}`集成助手-执行队列调度器", tenantId),
                StartTime = DateTime.Now,
                EndTime = null,
            });

            triggerBuilder.AlterToSecondly();

            scheduler.AddTrigger(triggerBuilder);
        }
    }

    /// <summary>
    /// WebHook接口参数获取.
    /// </summary>
    /// <param name="id">主键ID.</param>
    /// <param name="randomStr">随机值字符串.</param>
    /// <param name="parameter">参数.</param>
    /// <returns></returns>
    [AllowAnonymous]
    [IgnoreLog]
    [HttpPost("{id}/params/{randomStr}")]
    public async Task PostWebhookParams(string id, string randomStr, [FromBody] Dictionary<string, string>? parameter)
    {
        await insertRedis(id, randomStr, parameter);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 创建缓存.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="randomStr"></param>
    /// <param name="resultMap"></param>
    private async Task insertRedis(string id, string randomStr, Dictionary<string, string>? resultMap = default)
    {
        var inteId = Encoding.UTF8.GetString(Convert.FromBase64String(id));
        var key = string.Format("{0}:{1}:{2}", CommonConst.INTEGRATEWEBHOOK, inteId, randomStr);
        if (!_cacheManager.Exists(key))
            throw Oops.Oh(ErrorCode.D2601);

        var entity = await _repository.GetByIdAsync(inteId);
        if (entity.EnabledMark == 0)
            throw Oops.Oh(ErrorCode.D2602);
        var requestParams = App.HttpContext.Request.Query;
        if (requestParams != null && requestParams.Count > 0)
        {
            resultMap ??= new Dictionary<string, string>();
            foreach (var param in requestParams)
            {
                resultMap.Add(param.Key, param.Value);
            }
        }
        if (resultMap != null && resultMap.Count > 0)
        {
            await _cacheManager.DelAsync(key);
            await _cacheManager.SetAsync(string.Format("{0}:{1}", CommonConst.INTEGRATEWEBHOOK, randomStr), resultMap);
        }
    }

    #endregion
}