using JNPF.ClayObject;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Dtos.Message;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models.InteAssistant;
using JNPF.Common.Models.Job;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Engine.Entity.Model.Integrate;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Engine.Dto;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.Message.Interfaces;
using JNPF.Schedule;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using JNPF.TimeCrontab;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// 业务实现：集成助手.
/// </summary>
[ApiDescriptionSettings(Tag = "InteAssistant", Name = "Integrate", Order = 176)]
[Route("api/VisualDev/[controller]")]
public class IntegrateService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<IntegrateEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 调度管理.
    /// </summary>
    private readonly IJobManager _jobManager;

    /// <summary>
    /// 作业计划工厂服务.
    /// </summary>
    private readonly ISchedulerFactory _schedulerFactory;

    /// <summary>
    /// 信息管理.
    /// </summary>
    private readonly IMessageManager _messageManager;

    /// <summary>
    /// 用户关系服务.
    /// </summary>
    private readonly IUserRelationService _userRelationService;

    /// <summary>
    /// 初始化一个<see cref="IntegrateService"/>类型的新实例.
    /// </summary>
    public IntegrateService(
        ISqlSugarRepository<IntegrateEntity> repository,
        IUserManager userManager,
        IFileManager fileManager,
        IJobManager jobManager,
        ISchedulerFactory schedulerFactory,
        IMessageManager messageManager,
        IUserRelationService userRelationService)
    {
        _repository = repository;
        _userManager = userManager;
        _fileManager = fileManager;
        _jobManager = jobManager;
        _schedulerFactory = schedulerFactory;
        _messageManager = messageManager;
        _userRelationService = userRelationService;
    }

    #region GET

    /// <summary>
    /// 获取功能信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var data = await _repository.AsQueryable().FirstAsync(it => it.Id == id && it.DeleteMark == null);
        return data.Adapt<InteAssistantInfoOutput>();
    }

    /// <summary>
    /// 获取功能列表.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] InteAssistantListQueryInput input)
    {
        var data = await _repository.AsQueryable()
            .WhereIF(input.type != null, it => it.Type.Equals(input.type))
            .WhereIF(!string.IsNullOrEmpty(input.keyword), it => it.FullName.Contains(input.keyword) || it.EnCode.Contains(input.keyword))
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), it => it.EnabledMark.Equals(input.enabledMark))
            .Where(it => it.DeleteMark == null)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .OrderBy(it => it.SortCode, OrderByType.Asc)
            .Select(it => new InteAssistantListOutput
            {
                id = it.Id,
                type = it.Type,
                fullName = it.FullName,
                enCode = it.EnCode,
                enabledMark = (int)it.EnabledMark,
                creatorTime = it.CreatorTime,
                creatorUser = SqlFunc.Subqueryable<UserEntity>().Where(u => u.Id.Equals(it.CreatorUserId)).Select(u => SqlFunc.MergeString(u.RealName, "/", u.Account)),
                lastModifyTime = it.LastModifyTime,
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<InteAssistantListOutput>.SqlSugarPageResult(data);
    }

    #endregion

    #region POST

    /// <summary>
    /// 新建功能信息.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task<dynamic> Create([FromBody] InteAssistantCrInput input)
    {
        var entity = input.Adapt<IntegrateEntity>();

        // 验证名称和编码是否重复
        if (await _repository.IsAnyAsync(it => it.DeleteMark == null && it.Type == input.type && (it.FullName == input.fullName || it.EnCode == input.enCode))) throw Oops.Oh(ErrorCode.D1406);

        entity.Id = SnowflakeIdHelper.NextId();
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;

        // 添加功能
        var result = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        if (result < 1)
            throw Oops.Oh(ErrorCode.COM1000);
        return entity.Id;
    }

    /// <summary>
    /// 修改接口.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    [UnitOfWork]
    public async Task<dynamic> Update(string id, [FromBody] InteAssistantUpInput input)
    {
        var entityOld = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entityOld.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.COM1007);

        var templateJson = input.templateJson?.ToObject<DesignModel>();

        if (templateJson != null)
        {
            // 集成模版是否改动
            switch (input.enabledMark)
            {
                case 1:
                    switch (input.templateJson.Equals(entityOld.TemplateJson))
                    {
                        // 有改动 清空队列任务
                        case false:
                            var triggerId = string.Format("{0}_trigger_schedule_{1}", _userManager.TenantId, id);

                            // 定时触发 需要先停再删
                            switch (input.type)
                            {
                                case 2:
                                    // 获取单个任务
                                    var scheduleResult = _schedulerFactory.TryGetJob("job_builtIn_IntegrateTiming", out var scheduler);
                                    if (scheduler.ContainsTrigger(triggerId))
                                    {
                                        // 先暂停触发器 在删除触发器
                                        bool succeed = scheduler.PauseTrigger(triggerId);
                                        if (succeed)
                                            scheduler.RemoveTrigger(triggerId);
                                    }

                                    break;
                            }

                            // 删除执行队列
                            await _repository.AsSugarClient().Updateable<IntegrateQueueEntity>().SetColumns(it => new IntegrateQueueEntity()
                            {
                                DeleteMark = 1,
                                DeleteTime = SqlFunc.GetDate(),
                                DeleteUserId = _userManager.UserId,
                            }).Where(it => it.IntegrateId.Equals(id) && it.DeleteMark == null).ExecuteCommandAsync();

                            // 定时触发 需要创建系统调度
                            switch (input.type)
                            {
                                case 2:

                                    // 重新生成一个触发器
                                    DateTime? startTime = templateJson?.properties.startTime;
                                    DateTime? endTime = templateJson?.properties.endTime;

                                    TriggerBuilder? triggerBuilder = _jobManager.ObtainTriggerBuilder(new JobTriggerModel
                                    {
                                        TriggreId = triggerId,
                                        Description = string.Format("租户:`{0}`,定时触发:`{1}`定时触发器", _userManager.TenantId, input.fullName),
                                        StartTime = startTime,
                                        EndTime = endTime,
                                    });

                                    // 触发结束时间类型
                                    switch (templateJson?.properties.endTimeType)
                                    {
                                        // 触发次数
                                        case 1:
                                            triggerBuilder.SetMaxNumberOfRuns(templateJson.properties.endLimit);
                                            break;
                                    }

                                    // corn 规则
                                    switch (templateJson?.properties.cron.Split(" ").Length)
                                    {
                                        case 7:
                                            triggerBuilder.AlterToCron(templateJson.properties.cron, CronStringFormat.WithSecondsAndYears);
                                            break;
                                        case 6:
                                            triggerBuilder.AlterToCron(templateJson.properties.cron, CronStringFormat.WithSeconds);
                                            break;
                                    }

                                    // 获取单个任务
                                    var scheduleResult = _schedulerFactory.TryGetJob("job_builtIn_IntegrateTiming", out var scheduler);
                                    scheduler.AddTrigger(triggerBuilder);
                                    break;
                            }

                            break;
                    }
                    break;
            }
        }

        var entity = input.Adapt<IntegrateEntity>();
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;

        switch (input.type)
        {
            case 1:
                entity.FormId = templateJson?.properties?.formId;
                entity.TriggerType = (templateJson?.properties?.triggerEvent).ParseToInt();
                break;
            case 2:
                entity.FormId = templateJson?.childNode?.properties?.formId;
                break;
        }

        var result = await _repository.AsUpdateable(entity).UpdateColumns(it => new {
            it.Description,
            it.EnCode,
            it.EnabledMark,
            it.FullName,
            it.TemplateJson,
            it.TriggerType,
            it.FormId,
            it.Type,
            it.LastModifyTime,
            it.LastModifyUserId
        }).ExecuteCommandHasChangeAsync();
        if (!result)
            throw Oops.Oh(ErrorCode.COM1001);
        return id;
    }

    /// <summary>
    /// 更新集成助手状态.
    /// </summary>
    /// <param name="id">id.</param>
    [HttpPut("{id}/Actions/State")]
    [UnitOfWork]
    public async Task UpdateState(string id)
    {
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1007);
        string? triggerId = string.Format("{0}_trigger_schedule_{1}", _userManager.TenantId, id);

        // 先处理业务逻辑在修改状态
        switch (await _repository.IsAnyAsync(it => it.Id.Equals(id) && it.EnabledMark.Equals(1)))
        {
            // 禁用
            case true:
                {
                    // 获取单个任务
                    var scheduleResult = _schedulerFactory.TryGetJob("job_builtIn_IntegrateTiming", out var scheduler);
                    if (scheduler.ContainsTrigger(triggerId))
                    {
                        // 先暂停触发器
                        scheduler.PauseTrigger(triggerId);
                    }

                    // 删除执行队列
                    await _repository.AsSugarClient().Updateable<IntegrateQueueEntity>().SetColumns(it => new IntegrateQueueEntity()
                    {
                        DeleteMark = 1,
                        DeleteTime = SqlFunc.GetDate(),
                        DeleteUserId = _userManager.UserId,
                    }).Where(it => it.IntegrateId.Equals(id) && it.DeleteMark == null).ExecuteCommandAsync();
                }
                break;

            // 启用
            case false:
                {
                    // 获取单个任务
                    var scheduleResult = _schedulerFactory.TryGetJob("job_builtIn_IntegrateTiming", out var scheduler);
                    if (scheduler.ContainsTrigger(triggerId))
                    {
                        // 启动触发器
                        scheduler.StartTrigger(triggerId);
                    }
                    else
                    {
                        var entity = await _repository.AsQueryable().FirstAsync(it => it.Id.Equals(id) && it.DeleteMark == null);

                        switch (entity.Type.Equals(2) && !string.IsNullOrEmpty(entity.TemplateJson))
                        {
                            case true:
                                // 重新生成一个触发器
                                var templateJson = entity.TemplateJson?.ToObject<DesignModel>();
                                DateTime? startTime = templateJson?.properties?.startTime;
                                DateTime? endTime = templateJson?.properties?.endTime;

                                TriggerBuilder? triggerBuilder = _jobManager.ObtainTriggerBuilder(new JobTriggerModel
                                {
                                    TriggreId = triggerId,
                                    Description = string.Format("租户:`{0}`,定时触发:`{1}`定时触发器", _userManager.TenantId, entity.FullName),
                                    StartTime = startTime,
                                    EndTime = endTime,
                                });

                                // 触发结束时间类型
                                switch (templateJson?.properties?.endTimeType)
                                {
                                    // 触发次数
                                    case 1:
                                        triggerBuilder.SetMaxNumberOfRuns(templateJson.properties.endLimit);
                                        break;
                                }

                                // corn 规则
                                switch (templateJson?.properties?.cron.Split(" ").Length)
                                {
                                    case 7:
                                        triggerBuilder.AlterToCron(templateJson.properties.cron, CronStringFormat.WithSecondsAndYears);
                                        break;
                                    case 6:
                                        triggerBuilder.AlterToCron(templateJson?.properties?.cron, CronStringFormat.WithSeconds);
                                        break;
                                }

                                scheduler.AddTrigger(triggerBuilder);
                                break;
                        }
                    }
                }

                break;
        }

        var result = await _repository.AsUpdateable().SetColumns(it => new IntegrateEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 1, 0, 1),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
        if (!result)
            throw Oops.Oh(ErrorCode.COM1003);
    }

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

        // 获取单个任务 删除`定时触发` 需要将对应的触发器一起删除
        var scheduleResult = _schedulerFactory.TryGetJob("job_builtIn_IntegrateTiming", out var scheduler);
        if (scheduler.ContainsTrigger(triggerId))
        {
            // 删除触发器
            scheduler.RemoveTrigger(triggerId);
        }

        // 删除执行队列
        await _repository.AsSugarClient().Updateable<IntegrateQueueEntity>().SetColumns(it => new IntegrateQueueEntity()
        {
            DeleteMark = 1,
            DeleteTime = SqlFunc.GetDate(),
            DeleteUserId = _userManager.UserId,
        }).Where(it => it.IntegrateId.Equals(id) && it.DeleteMark == null).ExecuteCommandAsync();

        var result = await _repository.AsUpdateable().SetColumns(it => new IntegrateEntity()
        {
            DeleteMark = 1,
            DeleteTime = SqlFunc.GetDate(),
            DeleteUserId = _userManager.UserId,
        }).Where(it => it.Id.Equals(id) && it.DeleteMark == null).ExecuteCommandHasChangeAsync();
        if (!result)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Copy")]
    public async Task ActionsCopy(string id)
    {
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1007);
        var random = new Random().NextLetterAndNumberString(5);
        var entity = await _repository.AsQueryable().FirstAsync(it => it.Id.Equals(id) && it.DeleteMark == null);
        entity.Id = SnowflakeIdHelper.NextId();
        entity.FullName = string.Format("{0}.副本{1}", entity.FullName, random);
        entity.EnCode = string.Format("{0}{1}", entity.EnCode, random);
        entity.EnabledMark = 0;
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;
        var result = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        if (result < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1007);
        var entity = await _repository.AsQueryable().FirstAsync(it => it.Id.Equals(id) && it.DeleteMark == null);
        return await _fileManager.Export(entity.ToJsonString(), entity.FullName, ExportFileType.bi);
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    [HttpPost("Actions/Import")]
    public async Task ActionsImport(IFormFile file, int type)
    {
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.bi.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        IntegrateEntity? data;
        try
        {
            data = josn.ToObject<IntegrateEntity>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }
        if (data == null)
            throw Oops.Oh(ErrorCode.D3006);

        var errorMsgList = new List<string>();
        var errorList = new List<string>();
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(data.Id))) errorList.Add("ID");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.EnCode.Equals(data.EnCode))) errorList.Add("编码");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.FullName.Equals(data.FullName))) errorList.Add("名称");

        if (errorList.Any())
        {
            if (type.Equals(0))
            {
                var error = string.Join("、", errorList);
                errorMsgList.Add(string.Format("{0}重复", error));
            }
            else
            {
                var random = new Random().NextLetterAndNumberString(5);
                data.Id = SnowflakeIdHelper.NextId();
                data.FullName = string.Format("{0}.副本{1}", data.FullName, random);
                data.EnCode += random;
            }
        }
        if (errorMsgList.Any() && type.Equals(0)) throw Oops.Oh(ErrorCode.COM1018, string.Join(";", errorMsgList));

        data.Create();
        data.EnabledMark = 0;
        data.CreatorUserId = _userManager.UserId;
        data.LastModifyTime = null;
        data.LastModifyUserId = null;
        try
        {
            var storModuleModel = _repository.AsSugarClient().Storageable(data).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
            await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
            await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.COM1020, ex.Message);
        }
    }

    /// <summary>
    /// 执行任务过程通知.
    /// </summary>
    /// <param name="input">输入参数.</param>
    /// <returns></returns>
    [HttpPost("ExecuteNotice")]
    public async Task ExecuteNotice([FromBody] InteAssistantExecuteNoticeInput input)
    {
        List<string> toUserIds = new List<string>();
        input.msgUserType.ForEach(async item =>
        {
            switch (item)
            {
                case 1:
                    toUserIds.Add(input.creatorUserId);
                    break;
                case 2:
                    var userEntity = await _repository.AsSugarClient().Queryable<UserEntity>().FirstAsync(it => it.Account.Equals("admin"));
                    toUserIds.Add(userEntity.Id);
                    break;
                case 3:
                    toUserIds.AddRange(_userRelationService.GetUserId(input.msgUserIds));
                    break;
            }
        });
        switch (input.mesConfig.on)
        {
            case 3:
                var paramsDic = new Dictionary<string, string>
                {
                    { "@Title", input.defaultTitle }
                };
                var messageList = _messageManager.GetMessageList(input.msgEnCode, toUserIds, paramsDic, 3);
                await _messageManager.SendDefaultMsg(toUserIds, messageList);
                break;
            case 1:
            case 2:
                var messageSendModelList = await _messageManager.GetMessageSendModels(input.mesConfig.msgId);
                foreach (var item in messageSendModelList)
                {
                    item.toUser = toUserIds;
                    item.paramJson.Clear();
                    item.paramJson.Add(new MessageSendParam
                    {
                        field = "@Title",
                        value = input.mesConfig.msgName
                    });
                    item.paramJson.Add(new MessageSendParam
                    {
                        field = "@CreatorUserName",
                        value = _userManager.GetUserName(input.creatorUserId)
                    });
                    await _messageManager.SendDefinedMsg(
                        item,
                        new Dictionary<string, object>(),
                        input.taskEntity is null
                            ? null
                            : IntegrateTaskMessageDto.From(input.taskEntity.Data, input.taskEntity.TemplateJson));
                }
                break;
        }
    }

    /// <summary>
    /// 消息通知.
    /// </summary>
    /// <param name="input">输入参数.</param>
    /// <returns></returns>
    [HttpPost("MessageNotice")]
    public async Task MessageNotice([FromBody] InteAssistantMessageNoticeInput input)
    {
        var list = new List<string>();
        foreach (var template in input.templateJson)
        {
            var clay = Clay.Parse(input.data);
            foreach (var param in template.paramJson)
            {
                switch (param.relationField.Equals("@formId"))
                {
                    case true:
                        param.value = clay["id"]?.ToString();
                        break;
                    default:
                        param.value = clay[param.relationField]?.ToString();
                        break;
                }
            }
        }

        List<string> toUserIds = new List<string>();
        toUserIds.AddRange(_userRelationService.GetUserId(input.msgUserIds));
        foreach (var item in input.templateJson)
        {
            item.toUser = toUserIds;
            await _messageManager.SendDefinedMsg(item, new Dictionary<string, object>());
        }
    }

    #endregion
}