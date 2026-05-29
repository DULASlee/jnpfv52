using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models.InteAssistant;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Engine.Entity.Model.Integrate;
using JNPF.EventBus;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Engine;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Dto.InteAssistantNode;
using JNPF.InteAssistant.Entitys.Dto.InteAssistantQueue;
using JNPF.InteAssistant.Entitys.Dto.InteAssistantTask;
using JNPF.InteAssistant.Entitys.Entity;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// 业务实现：集成任务.
/// </summary>
[ApiDescriptionSettings(Tag = "InteAssistant", Name = "IntegrateTask", Order = 177)]
[Route("api/VisualDev/[controller]")]
public class IntegrateTaskService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<IntegrateTaskEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 事件总线.
    /// </summary>
    private readonly IEventPublisher _eventPublisher;

    /// <summary>
    /// 集成助手运行核心.
    /// </summary>
    private readonly InteAssistantRun _runService;

    /// <summary>
    /// 初始化一个<see cref="IntegrateTaskService"/>类型的新实例.
    /// </summary>
    public IntegrateTaskService(
        ISqlSugarRepository<IntegrateTaskEntity> repository,
        IUserManager userManager,
        ICacheManager cacheManager,
        InteAssistantRun runService,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _runService = runService;
        _eventPublisher = eventPublisher;
    }

    #region GET

    /// <summary>
    /// 获取任务节点信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var entity = await _repository.GetFirstAsync(it => it.Id.Equals(id));
        var data = await _repository.AsSugarClient().Queryable<IntegrateNodeEntity>().OrderBy(it => it.CreatorTime, OrderByType.Asc)
            .Where(it => it.TaskId.Equals(id))
            .Select(it => new InteAssistantNodeListOutput()
            {
                id = it.Id,
                taskId = it.TaskId,
                nodeCode = it.NodeCode,
                nodeType = it.NodeType,
                nodeName = it.NodeName,
                resultType = it.ResultType,
                errorMsg = it.ErrorMsg,
                startTime = it.StartTime,
                endTime = it.EndTime,
                parentId = it.ParentId,
                isRetry = SqlFunc.IIF(it.IsRetry == 1, true, false),
                type = SqlFunc.IIF(string.IsNullOrEmpty(it.ParentId) || it.ParentId.Equals("0"), 1, 0)
            }).ToListAsync();
        return new { data = entity.Data, list = data };
    }

    /// <summary>
    /// 任务队列列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("queueList")]
    public async Task<dynamic> GetQueueList()
    {
        var list = await _repository.AsSugarClient().Queryable<IntegrateQueueEntity>().Where(it => it.DeleteMark == null)
           .OrderBy(it => it.SortCode, OrderByType.Asc)
           .OrderBy(it => it.CreatorTime, OrderByType.Desc).ToListAsync();
        return list.Adapt<List<InteAssistantQueueListOutput>>();
    }

    /// <summary>
    /// 日记列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] InteAssistantTaskListQueryInput input)
    {
        var data = await _repository.AsQueryable()
            .WhereIF(input.endTime != null && input.startTime != null, it => SqlFunc.Between(it.CreatorTime, input.startTime, input.endTime))
            .Where(it => it.DeleteMark == null && it.IntegrateId.Equals(input.integrateId))
            .WhereIF(input.resultType == 1, it => it.ResultType.Equals(input.resultType))
            .WhereIF(input.resultType == 0, it => !it.ResultType.Equals(1))
            .OrderBy(it => it.ExecutionTime, OrderByType.Desc)
            .Select(it => new InteAssistantTaskListOutput()
            {
                id = it.Id,
                processId = it.ProcessId,
                parentId = it.ParentId,
                parentTime = it.ParentTime,
                isRetry = SqlFunc.IIF(string.IsNullOrEmpty(it.ParentId) || it.ParentId.Equals("0"), 0, 1),
                executionTime = it.ExecutionTime,
                resultType = it.ResultType,
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<InteAssistantTaskListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 节点重试.
    /// </summary>
    /// <param name="id">任务ID.</param>
    /// <param name="nodeId">节点ID.</param>
    /// <returns></returns>
    [HttpGet("{id}/nodeRetry")]
    public async Task nodeRetry(string id, string nodeId)
    {
        var cacheKey = string.Format("{0}:{1}", CommonConst.INTEASSISTANTRETRY, _userManager.TenantId);
        List<string> caCheIntegrateRetryList = await _cacheManager.GetAsync<List<string>>(cacheKey);
        caCheIntegrateRetryList ??= new List<string>();

        if (caCheIntegrateRetryList.Contains(id))
        {
            throw Oops.Oh(ErrorCode.D2600);
        }

        // 获取集成任务信息
        var entity = await _repository.GetSingleAsync(it => it.Id.Equals(id));

        // 获取任务节点信息
        var nodeInfo = await _repository.AsSugarClient().Queryable<IntegrateNodeEntity>().FirstAsync(it => it.Id.Equals(nodeId));

        // 获取集成助手信息
        var inteEntity = await _repository.AsSugarClient().Queryable<IntegrateEntity>().Where(it => it.Id.Equals(entity.IntegrateId)).FirstAsync();
        var templateJson = entity.TemplateJson.ToObject<DesignModel>();
        var dataList = entity.Data.ToObject<List<InteAssiDataModel>>();

        // 获取集成助手任务纲要
        var inteAssiTaskOutlineModel = await _runService.InteAssiTaskOutline(templateJson, entity.Type, entity.IntegrateId);

        var nodeRetry = inteAssiTaskOutlineModel.nodeAttributes;

        // 获取需要重试节点的纲要
        var retryNode = nodeRetry.Find(it => it.nodeId.Equals(nodeInfo.NodeCode));

        // 重试节点行数
        var numberRows = nodeRetry.IndexOf(retryNode);

        // 移除已成功节点纲要
        nodeRetry = nodeRetry.GetRange(numberRows, nodeRetry.Count - numberRows);

        var runModel = await _runService.GetIntegrateNodeList(entity.Id, inteEntity.FormId, entity.CreatorUserId, _userManager.TenantId, inteEntity.Type, inteEntity.FullName, templateJson, dataList, nodeRetry);

        var nodeResult = await _repository.AsSugarClient().Insertable(runModel.NodeEntity).ExecuteCommandAsync();

        if (runModel.NodeEntity.Any(it => it.NodeCode.Equals(NodeType.end.ToString())))
        {
            await _repository.AsSugarClient().Updateable<IntegrateTaskEntity>().SetColumns(it => new IntegrateTaskEntity()
            {
                ResultType = 1,
                LastModifyTime = SqlFunc.GetDate(),
                LastModifyUserId = _userManager.UserId,
            }).Where(it => it.Id.Equals(id) && it.DeleteMark == null).ExecuteCommandHasChangeAsync();
        }

        var result = await _repository.AsSugarClient().Updateable<IntegrateNodeEntity>().SetColumns(it => new IntegrateNodeEntity()
        {
            IsRetry = 0,
            LastModifyTime = SqlFunc.GetDate(),
            LastModifyUserId = _userManager.UserId,
        }).Where(it => it.Id.Equals(nodeId) && it.DeleteMark == null).ExecuteCommandHasChangeAsync();
    }

    #endregion

    #region POST

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [UnitOfWork]
    public async Task Delete(string id)
    {
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1007);

        string? triggerId = string.Format("{0}_trigger_schedule_{1}", _userManager.TenantId, id);

        var result = await _repository.AsUpdateable().SetColumns(it => new IntegrateTaskEntity()
        {
            DeleteMark = 1,
            DeleteTime = SqlFunc.GetDate(),
            DeleteUserId = _userManager.UserId,
        }).Where(it => it.Id.Equals(id) && it.DeleteMark == null).ExecuteCommandHasChangeAsync();

        var result1 = await _repository.AsSugarClient().Updateable<IntegrateNodeEntity>().SetColumns(it => new IntegrateNodeEntity()
        {
            DeleteMark = 1,
            DeleteTime = SqlFunc.GetDate(),
            DeleteUserId = _userManager.UserId,
        }).Where(it => it.TaskId.Equals(id) && it.DeleteMark == null).ExecuteCommandHasChangeAsync();

        if (!result && !result1)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 重试方法.
    /// </summary>
    /// <param name="id">自然主键.</param>
    /// <returns></returns>
    [HttpPut("{id}/retry")]
    public async Task Retry(string id)
    {
        // 获取任务重试缓存
        var cacheKey = string.Format("{0}:{1}", CommonConst.INTEASSISTANTRETRY, _userManager.TenantId);
        List<string> caCheIntegrateRetryList = await _cacheManager.GetAsync<List<string>>(cacheKey);
        caCheIntegrateRetryList ??= new List<string>();

        // 添加任务重试缓存
        caCheIntegrateRetryList.Add(id);
        await _cacheManager.SetAsync(cacheKey, caCheIntegrateRetryList);

        // 重试逻辑
        var entity = await _repository.GetSingleAsync(it => it.Id.Equals(id));
        var inteEntity = await _repository.AsSugarClient().Queryable<IntegrateEntity>().Where(it => it.Id.Equals(entity.IntegrateId)).FirstAsync();

        var templateJson = entity.TemplateJson.ToObject<DesignModel>();
        var dataList = entity.Data.ToObject<List<InteAssiDataModel>>();

        var inteAssiTaskOutlineModel = await _runService.InteAssiTaskOutline(templateJson, entity.Type, entity.IntegrateId);

        var taskId = SnowflakeIdHelper.NextId();
        var taskEntity = new IntegrateTaskEntity
        {
            Id = taskId,
            ProcessId = taskId,
            ParentTime = entity.ExecutionTime,
            ParentId = entity.Id,
            ExecutionTime = inteAssiTaskOutlineModel.startTime,
            TemplateJson = entity.TemplateJson,
            Data = entity.Data,
            DataId = entity.DataId,
            Type = entity.Type,
            IntegrateId = entity.IntegrateId,
            EnabledMark = 1,
            CreatorTime = DateTime.Now,
            CreatorUserId = entity.CreatorUserId,
        };
        var runModel = await _runService.GetIntegrateNodeList(taskId, inteEntity.FormId, entity.CreatorUserId, _userManager.TenantId, inteEntity.Type, inteEntity.FullName, templateJson, dataList, inteAssiTaskOutlineModel.nodeAttributes);

        // 为重试做准备
        taskEntity.Data = runModel.TaskData;

        if (runModel.NodeEntity.Any(it => it.NodeCode.Equals(NodeType.end.ToString())))
        {
            taskEntity.ResultType = 1;
        }

        var taskResult = await _repository.AsSugarClient().Insertable(taskEntity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        var nodeResult = await _repository.AsSugarClient().Insertable(runModel.NodeEntity).ExecuteCommandAsync();

        if (taskResult == 1)
        {
            caCheIntegrateRetryList.Remove(id);
            await _cacheManager.SetAsync(cacheKey, caCheIntegrateRetryList);
        }
    }

    #endregion
}