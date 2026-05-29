using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Dtos.Message;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.CollectiveOAuth.Utils;
using JNPF.FriendlyException;
using JNPF.Message.Interfaces;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Dto.Schedule;
using JNPF.Systems.Entitys.Entity.System;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.TaskQueue;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace JNPF.Systems.System;

/// <summary>
/// 业务实现：门户日程.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "Schedule", Order = 206)]
[Route("api/system/[controller]")]
public class ScheduleService : IScheduleService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 系统功能表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ScheduleEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 任务队列.
    /// </summary>
    private readonly ITaskQueue _taskQueue;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 消息通知.
    /// </summary>
    private readonly IMessageManager _messageManager;

    /// <summary>
    /// 服务提供器.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化一个<see cref="ScheduleService"/>类型的新实例.
    /// </summary>
    public ScheduleService(
        ISqlSugarRepository<ScheduleEntity> repository,
        IUserManager userManager,
        ITaskQueue taskQueue,
        ICacheManager cacheManager,
        IMessageManager messageManager,
        IServiceProvider serviceProvider)
    {
        _repository = repository;
        _userManager = userManager;
        _taskQueue = taskQueue;
        _cacheManager = cacheManager;
        _messageManager = messageManager;
        _serviceProvider = serviceProvider;
    }

    #region Get

    /// <summary>
    /// 日程列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] ScheduleListInput input)
    {
        var list = await _repository.AsSugarClient().Queryable<ScheduleEntity, ScheduleUserEntity>((s, su) => new JoinQueryInfos(JoinType.Left, s.Id == su.ScheduleId))
            .WhereIF(!input.startTime.IsNullOrEmpty() && !input.endTime.IsNullOrEmpty(), s => (s.StartDay >= input.startTime && s.StartDay <= input.endTime) || SqlFunc.Between(input.startTime, s.StartDay, s.EndDay))
            .Where(s => s.DeleteMark == null)
            .Where((s, su) => su.DeleteMark == null && su.EnabledMark == 1 && su.ToUserId.Equals(_userManager.UserId))
            .OrderBy(s => s.AllDay, OrderByType.Desc)
            .OrderBy(s => s.StartDay)
            .OrderBy(s => s.EndDay)
            .OrderBy(s => s.CreatorTime, OrderByType.Desc)
            .Select(s => new ScheduleListOutput
            {
                id = s.Id,
                category = s.Category,
                urgent = s.Urgent.ToString(),
                title = s.Title,
                content = s.Content,
                allDay = s.AllDay,
                startDay = s.StartDay,
                startTime = s.StartTime,
                endDay = SqlFunc.IIF(s.AllDay == 1, s.EndDay.AddSeconds(1), s.EndDay),
                endTime = s.EndTime,
                duration = s.Duration,
                color = s.Color,
                reminderTime = s.ReminderTime,
                reminderType = s.ReminderType,
                send = s.Send,
                sendName = s.SendName,
                repetition = s.Repetition,
                repeatTime = s.RepeatTime,
                creatorUserId = s.CreatorUserId,
                groupId = s.GroupId,
            }).ToListAsync();

        return new { list = list };
    }

    /// <summary>
    /// App日程列表.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("AppList")]
    public async Task<dynamic> GetAppList([FromQuery] ScheduleAppListInput input)
    {
        var signList = new Dictionary<string, int>();
        while (input.startTime <= input.endTime)
        {
            var count = await _repository.AsSugarClient().Queryable<ScheduleEntity, ScheduleUserEntity>((s, su) => new JoinQueryInfos(JoinType.Left, s.Id == su.ScheduleId))
                .Where(s => s.DeleteMark == null && s.StartDay.Date <= input.startTime && s.EndDay.Date >= input.startTime)
                .Where((s, su) => su.DeleteMark == null && su.ToUserId.Equals(_userManager.UserId))
                .CountAsync();
            signList.Add(string.Format("{0:yyyyMMdd}", input.startTime), count);
            input.startTime = input.startTime.AddDays(1);
        }

        if (input.dateTime.IsNullOrEmpty())
            input.dateTime = DateTime.Now.Date;
        var todayList = await _repository.AsSugarClient().Queryable<ScheduleEntity, ScheduleUserEntity>((s, su) => new JoinQueryInfos(JoinType.Left, s.Id == su.ScheduleId))
            .Where(s => s.DeleteMark == null && s.StartDay.Date <= input.dateTime && s.EndDay.Date >= input.dateTime)
            .Where((s, su) => su.DeleteMark == null && su.ToUserId.Equals(_userManager.UserId))
            .OrderBy(s => s.AllDay, OrderByType.Desc)
            .OrderBy(s => s.StartDay)
            .OrderBy(s => s.EndDay)
            .OrderBy(s => s.CreatorTime, OrderByType.Desc)
            .Select(s => new ScheduleListOutput()
            {
                id = s.Id,
                category = s.Category,
                urgent = s.Urgent.ToString(),
                title = s.Title,
                content = s.Content,
                allDay = s.AllDay,
                startDay = s.StartDay,
                startTime = s.StartTime,
                endDay = s.EndDay,
                endTime = s.EndTime,
                duration = s.Duration,
                color = s.Color,
                reminderTime = s.ReminderTime,
                reminderType = s.ReminderType,
                send = s.Send,
                sendName = s.SendName,
                repetition = s.Repetition,
                repeatTime = s.RepeatTime,
                creatorUserId = s.CreatorUserId,
                groupId = s.GroupId
            }).ToListAsync();

        return new { signList, todayList };
    }

    /// <summary>
    /// 获取日程信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return await _repository.AsQueryable()
            .Includes(it => it.ScheduleUser)
            .Where(it => it.DeleteMark == null && it.Id.Equals(id))
            .Select(it => new ScheduleInfoOutput
            {
                id = it.Id,
                category = it.Category,
                urgent = it.Urgent.ToString(),
                title = it.Title,
                content = it.Content,
                allDay = it.AllDay,
                startDay = it.StartDay,
                startTime = it.StartTime,
                endDay = it.EndDay,
                endTime = it.EndTime,
                duration = it.Duration,
                color = it.Color,
                reminderTime = it.ReminderTime,
                reminderType = it.ReminderType,
                send = it.Send,
                sendName = it.SendName,
                repetition = it.Repetition,
                repeatTime = it.RepeatTime,
                creatorUserId = it.CreatorUserId,
                toUserIds = it.ScheduleUser.Where(x => x.DeleteMark == null && x.Type.Equals(2)).Select(x => x.ToUserId).ToList(),
                files = it.Files
            })
            .FirstAsync();
    }

    /// <summary>
    /// 日程信息.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("detail")]
    public async Task<dynamic> GetDetalInfo([FromQuery] ScheduleDetailInput input)
    {
        if (!await _repository.IsAnyAsync(it => it.DeleteMark == null && it.Id.Equals(input.id)))
        {
            throw Oops.Oh(ErrorCode.D1918);
        }

        var data = await _repository.AsQueryable()
            .Includes(it => it.ScheduleUser)
            .Where(it => it.DeleteMark == null && it.Id.Equals(input.id))
            .Select(it => new ScheduleDetailOutput
            {
                id = it.Id,
                category = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(x => x.Id.Equals(it.Category)).Select(x => x.FullName),
                allDay = it.AllDay,
                color = it.Color,
                content = it.Content,
                creatorUserId = SqlFunc.Subqueryable<UserEntity>().Where(x => x.Id.Equals(it.CreatorUserId)).Select(x => SqlFunc.MergeString(x.RealName, "/", x.Account)),
                duration = it.Duration,
                startDay = it.StartDay,
                startTime = it.StartTime,
                endDay = it.EndDay,
                endTime = it.EndTime,
                reminderTime = it.ReminderTime,
                reminderType = it.ReminderType,
                send = it.Send,
                sendName = it.SendName,
                title = it.Title,
                urgent = it.Urgent.ToString(),
                repeatTime = it.RepeatTime,
                repetition = it.Repetition,
                toUserIdList = it.ScheduleUser.Where(x => x.DeleteMark == null && x.Type.Equals(2)).Select(x => x.ToUserId).ToList(),
                files = it.Files
            }).FirstAsync();

        data.toUserIds = string.Join(",", await _repository.AsSugarClient().Queryable<UserEntity>().Where(it => data.toUserIdList.Contains(it.Id)).Select(it => SqlFunc.MergeString(it.RealName, "/", it.Account)).ToListAsync());
        switch (data.urgent)
        {
            case "1":
                data.urgent = "普通";
                break;
            case "2":
                data.urgent = "重要";
                break;
            case "3":
                data.urgent = "紧急";
                break;
        }

        return data;
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建日程.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] SysScheduleCrInput input)
    {
        var entityList = new List<ScheduleEntity>();
        var entityLogList = new List<ScheduleLogEntity>();

        if (!"1".Equals(input.repetition) && input.repeatTime.IsNotEmptyOrNull())
            input.repeatTime = new DateTime(input.repeatTime.Value.Year, input.repeatTime.Value.Month, input.repeatTime.Value.Day, 23, 59, 59);

        var oriEntity = AddSchedule(input, 0, "1");
        entityList.Add(oriEntity);

        // 添加参与人
        await AddScheduleUser(oriEntity.Id, input.toUserIds, oriEntity.CreatorUserId);

        var entityLog = AddScheduleLog(oriEntity, string.Join(",", input.toUserIds), "1");
        entityLogList.Add(entityLog);

        switch (input.repetition)
        {
            case "1": // 不重复
                break;
            case "2": // 每天重复
                {
                    var repeatTime = string.Format("{0:yyyy-MM-dd} {1:HH:mm:ss}", input.repeatTime, oriEntity.StartTime).ParseToDateTime() - oriEntity.StartDay;
                    int repeatDay = repeatTime.Days;
                    for (int i = 1; i <= repeatDay; i++)
                    {
                        var repeatEntity = AddSchedule(input, i, input.repetition, oriEntity.GroupId);
                        entityList.Add(repeatEntity);

                        await AddScheduleUser(repeatEntity.Id, input.toUserIds, repeatEntity.CreatorUserId);

                        var repeatLog = AddScheduleLog(repeatEntity, string.Join(",", input.toUserIds), "1");
                        entityLogList.Add(repeatLog);
                    }
                }

                break;
            case "3": // 每周重复
                {
                    var repeatTime = string.Format("{0:yyyy-MM-dd} {1:HH:mm:ss}", input.repeatTime, oriEntity.StartTime).ParseToDateTime() - oriEntity.StartDay;
                    int repeatDay = repeatTime.Days / 7;
                    for (int i = 1; i <= repeatDay; i++)
                    {
                        var repeatEntity = AddSchedule(input, i, input.repetition, oriEntity.GroupId);
                        entityList.Add(repeatEntity);

                        await AddScheduleUser(repeatEntity.Id, input.toUserIds, repeatEntity.CreatorUserId);

                        var repeatLog = AddScheduleLog(repeatEntity, string.Join(",", input.toUserIds), "1");
                        entityLogList.Add(repeatLog);
                    }
                }

                break;
            case "4": // 每月重复
                {
                    var repeatTime = string.Format("{0:yyyy-MM-dd} {1:HH:mm:ss}", input.repeatTime, oriEntity.StartTime).ParseToDateTime();
                    var startMonth = oriEntity.StartDay.AddMonths(1);
                    int repeatNum = 1;
                    for (DateTime i = startMonth; i < repeatTime; i = i.AddMonths(1))
                    {
                        var repeatEntity = AddSchedule(input, repeatNum, input.repetition, oriEntity.GroupId);
                        entityList.Add(repeatEntity);

                        await AddScheduleUser(repeatEntity.Id, input.toUserIds, repeatEntity.CreatorUserId);

                        var repeatLog = AddScheduleLog(repeatEntity, string.Join(",", input.toUserIds), "1");
                        entityLogList.Add(repeatLog);

                        repeatNum++;
                    }
                }

                break;
            case "5": // 每年重复
                {
                    var repeatTime = string.Format("{0:yyyy-MM-dd} {1:HH:mm:ss}", input.repeatTime, oriEntity.StartTime).ParseToDateTime();
                    var startYear = oriEntity.StartDay.AddYears(1);
                    int repeatNum = 1;
                    for (DateTime i = startYear; i < repeatTime; i = i.AddYears(1))
                    {
                        var repeatEntity = AddSchedule(input, repeatNum, input.repetition, oriEntity.GroupId);
                        entityList.Add(repeatEntity);

                        await AddScheduleUser(repeatEntity.Id, input.toUserIds, repeatEntity.CreatorUserId);

                        var repeatLog = AddScheduleLog(repeatEntity, string.Join(",", input.toUserIds), "1");
                        entityLogList.Add(repeatLog);

                        repeatNum++;
                    }
                }

                break;
        }

        var isOk = await _repository.AsInsertable(entityList).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.D1908);
        var logIsOk = await _repository.AsSugarClient().Insertable(entityLogList).ExecuteCommandAsync();
        if (!(logIsOk > 0))
            throw Oops.Oh(ErrorCode.D1909);

        var startTime = DateTime.Now;
        var nextDayTime = DateTime.Now.AddDays(1);
        var endTime = new DateTime(nextDayTime.Year, nextDayTime.Month, nextDayTime.Day, 0, 5, 0);
        var pushList = entityList.FindAll(it => it.PushTime >= startTime && it.PushTime < endTime && it.ReminderTime != -2);
        foreach (var item in pushList)
        {
            // 添加日程任务队列
            await AddPushTaskQueue(item, "MBXTRC001", "1", _userManager.TenantId);
            await _cacheManager.SetAsync(string.Format("{0}:{1}:{2}", CommonConst.CACHEKEYSCHEDULE, _userManager.TenantId, item.Id), item.PushTime.ToString(), TimeSpan.FromDays(1));
        }

        var oriEntityUserList = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
            .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(oriEntity.Id))
            .Select(it => it.ToUserId)
            .ToListAsync();
        await SendScheduleMsg(oriEntity, oriEntityUserList, "1", "MBXTRC001");
    }

    /// <summary>
    /// 修改日程信息.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="type">修改类型（1：此日程，2：此日程及后续）.</param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("{id}/{type}")]
    public async Task Update(string id, int type, [FromBody] SysScheduleUpInput input)
    {
        var data = await _repository.AsQueryable()
            .Where(it => it.Id.Equals(id))
            .Select(it => new { it.GroupId, it.StartDay, it.PushTime })
            .FirstAsync();

        // 获取同一分组的后续日程数据.
        var dataList = await _repository.AsQueryable()
            .Where(it => it.EnabledMark == 1 && it.DeleteMark == null && it.GroupId.Equals(data.GroupId) && it.StartDay > data.StartDay)
            .Select(it => it.Id)
            .ToListAsync();

        var addEntityList = new List<ScheduleEntity>();
        var upEntityList = new List<ScheduleEntity>();
        var entityLogList = new List<ScheduleLogEntity>();

        // 用于加入任务队列
        var scheduleJobList = new List<ScheduleEntity>();

        if (!"1".Equals(input.repetition) && input.repeatTime.IsNotEmptyOrNull())
            input.repeatTime = new DateTime(input.repeatTime.Value.Year, input.repeatTime.Value.Month, input.repeatTime.Value.Day, 23, 59, 59);

        var newEntity = UpdateSchedule(input, id, 0, "1");

        // 修改此日程时清空重复提醒.
        if (type == 1)
        {
            newEntity.Repetition = 1;
            newEntity.RepeatTime = null;
        }

        upEntityList.Add(newEntity);

        await AddScheduleUser(newEntity.Id, input.toUserIds, newEntity.CreatorUserId);

        var entityLog = AddScheduleLog(newEntity, string.Join(",", input.toUserIds), "1");
        entityLogList.Add(entityLog);

        scheduleJobList.Add(newEntity);

        if (type != 1)
        {
            var beforeSchedule = await _repository.AsQueryable()
                .Where(it => it.DeleteMark == null && it.GroupId.Equals(data.GroupId) && it.StartDay < data.StartDay)
                .OrderBy(it => it.StartDay, OrderByType.Desc)
                .ToListAsync();

            // 修改之前日程的重复提醒时间
            if (beforeSchedule.Count > 0)
            {
                var maxStartDay = beforeSchedule.FirstOrDefault().StartDay;
                var repeatTime = new DateTime(maxStartDay.Year, maxStartDay.Month, maxStartDay.Day, 23, 59, 59);
                for (int i = 0; i < beforeSchedule.Count; i++)
                {
                    beforeSchedule[i].RepeatTime = repeatTime;
                    upEntityList.Add(beforeSchedule[i]);
                }
            }

            switch (input.repetition)
            {
                case "1": // 不重复
                    break;
                case "2": // 每天重复
                    {
                        var repeatTime = string.Format("{0:yyyy-MM-dd} {1:HH:mm:ss}", input.repeatTime, newEntity.StartTime).ParseToDateTime() - newEntity.StartDay;
                        int repeatDay = repeatTime.Days;
                        var oldIdCount = dataList.Count;

                        for (int i = 1; i <= repeatDay; i++)
                        {
                            var oldEntityId = string.Empty;
                            if (oldIdCount >= i)
                            {
                                oldEntityId = dataList.FirstOrDefault();
                                dataList.Remove(oldEntityId);

                                var repeatEntity = UpdateSchedule(input, oldEntityId, i, input.repetition, newEntity.GroupId);
                                upEntityList.Add(repeatEntity);

                                await AddScheduleUser(repeatEntity.Id, input.toUserIds, repeatEntity.CreatorUserId);

                                var repeatLogEntity = AddScheduleLog(repeatEntity, string.Join(",", input.toUserIds), "2");
                                entityLogList.Add(repeatLogEntity);

                                scheduleJobList.Add(repeatEntity);
                            }
                            else
                            {
                                var repeatEntity = AddSchedule(input, i, input.repetition, newEntity.GroupId);
                                addEntityList.Add(repeatEntity);

                                await AddScheduleUser(repeatEntity.Id, input.toUserIds, repeatEntity.CreatorUserId);

                                var repeatLogEntity = AddScheduleLog(repeatEntity, string.Join(",", input.toUserIds), "2");
                                entityLogList.Add(repeatLogEntity);

                                scheduleJobList.Add(repeatEntity);
                            }
                        }
                    }

                    break;
                case "3": // 每周重复
                    {
                        var repeatTime = string.Format("{0:yyyy-MM-dd} {1:HH:mm:ss}", input.repeatTime, newEntity.StartTime).ParseToDateTime() - newEntity.StartDay;
                        int repeatDay = repeatTime.Days / 7;
                        var oldIdCount = dataList.Count;
                        for (int i = 1; i <= repeatDay; i++)
                        {
                            var oldEntityId = string.Empty;
                            if (oldIdCount >= i)
                            {
                                oldEntityId = dataList.FirstOrDefault();
                                dataList.Remove(oldEntityId);

                                var repeatEntity = UpdateSchedule(input, oldEntityId, i, input.repetition, newEntity.GroupId);
                                upEntityList.Add(repeatEntity);

                                await AddScheduleUser(repeatEntity.Id, input.toUserIds, repeatEntity.CreatorUserId);

                                var repeatLogEntity = AddScheduleLog(repeatEntity, string.Join(",", input.toUserIds), "2");
                                entityLogList.Add(repeatLogEntity);

                                scheduleJobList.Add(repeatEntity);
                            }
                            else
                            {
                                var repeatEntity = AddSchedule(input, i, input.repetition, newEntity.GroupId);
                                addEntityList.Add(repeatEntity);

                                await AddScheduleUser(repeatEntity.Id, input.toUserIds, repeatEntity.CreatorUserId);

                                var repeatLogEntity = AddScheduleLog(repeatEntity, string.Join(",", input.toUserIds), "2");
                                entityLogList.Add(repeatLogEntity);

                                scheduleJobList.Add(repeatEntity);
                            }
                        }
                    }

                    break;
                case "4": // 每月重复
                    {
                        var repeatTime = string.Format("{0:yyyy-MM-dd} {1:HH:mm:ss}", input.repeatTime, newEntity.StartTime).ParseToDateTime();
                        var startMonth = newEntity.StartDay.AddMonths(1);
                        int repeatNum = 1;
                        var oldIdCount = dataList.Count;
                        for (DateTime i = startMonth; i < repeatTime; i = i.AddMonths(1))
                        {
                            var oldEntityId = string.Empty;
                            if (oldIdCount > repeatNum)
                            {
                                oldEntityId = dataList.FirstOrDefault();
                                dataList.Remove(oldEntityId);

                                var repeatEntity = UpdateSchedule(input, oldEntityId, repeatNum, input.repetition, newEntity.GroupId);
                                upEntityList.Add(repeatEntity);

                                await AddScheduleUser(repeatEntity.Id, input.toUserIds, repeatEntity.CreatorUserId);

                                var repeatLogEntity = AddScheduleLog(repeatEntity, string.Join(",", input.toUserIds), "2");
                                entityLogList.Add(repeatLogEntity);

                                scheduleJobList.Add(repeatEntity);
                            }
                            else
                            {
                                var repeatEntity = AddSchedule(input, repeatNum, input.repetition, newEntity.GroupId);
                                addEntityList.Add(repeatEntity);

                                await AddScheduleUser(repeatEntity.Id, input.toUserIds, repeatEntity.CreatorUserId);

                                var repeatLogEntity = AddScheduleLog(repeatEntity, string.Join(",", input.toUserIds), "2");
                                entityLogList.Add(repeatLogEntity);

                                scheduleJobList.Add(repeatEntity);
                            }

                            repeatNum++;
                        }
                    }

                    break;
                case "5": // 每年重复
                    {
                        var repeatTime = string.Format("{0:yyyy-MM-dd} {1:HH:mm:ss}", input.repeatTime, newEntity.StartTime).ParseToDateTime();
                        var startYear = newEntity.StartDay.AddYears(1);
                        int repeatNum = 1;
                        var oldIdCount = dataList.Count;
                        for (DateTime i = startYear; i < repeatTime; i = i.AddYears(1))
                        {
                            var oldEntityId = string.Empty;
                            if (oldIdCount > repeatNum)
                            {
                                oldEntityId = dataList.FirstOrDefault();
                                dataList.Remove(oldEntityId);

                                var repeatEntity = UpdateSchedule(input, oldEntityId, repeatNum, input.repetition, newEntity.GroupId);
                                upEntityList.Add(repeatEntity);

                                await AddScheduleUser(repeatEntity.Id, input.toUserIds, repeatEntity.CreatorUserId);

                                var repeatLogEntity = AddScheduleLog(repeatEntity, string.Join(",", input.toUserIds), "2");
                                entityLogList.Add(repeatLogEntity);

                                scheduleJobList.Add(repeatEntity);
                            }
                            else
                            {
                                var repeatEntity = AddSchedule(input, repeatNum, input.repetition, newEntity.GroupId);
                                addEntityList.Add(repeatEntity);

                                await AddScheduleUser(repeatEntity.Id, input.toUserIds, repeatEntity.CreatorUserId);

                                var repeatLogEntity = AddScheduleLog(repeatEntity, string.Join(",", input.toUserIds), "2");
                                entityLogList.Add(repeatLogEntity);

                                scheduleJobList.Add(repeatEntity);
                            }

                            repeatNum++;
                        }
                    }

                    break;
            }

            // 多余数据操作
            if (dataList.Count > 0)
            {
                // 同组ID下的多余信息删除(ScheduleEntity、ScheduleUser)
                var delSchedule = await _repository.AsSugarClient().Updateable<ScheduleEntity>()
                    .SetColumns(it => new ScheduleEntity()
                    {
                        DeleteMark = 1,
                        DeleteTime = SqlFunc.GetDate(),
                        DeleteUserId = _userManager.UserId
                    })
                    .Where(it => dataList.Contains(it.Id))
                    .ExecuteCommandAsync();

                var delScheduleUser = await _repository.AsSugarClient().Updateable<ScheduleUserEntity>()
                    .SetColumns(it => new ScheduleUserEntity()
                    {
                        DeleteMark = 1,
                        DeleteTime = SqlFunc.GetDate(),
                        DeleteUserId = _userManager.UserId
                    })
                    .Where(it => dataList.Contains(it.ScheduleId))
                    .ExecuteCommandAsync();
            }
        }

        if (upEntityList.Count > 0)
        {
            var upIsOk = await _repository.AsUpdateable(upEntityList)
                .UpdateColumns(it => new {
                    it.AllDay,
                    it.Color,
                    it.Content,
                    it.Duration,
                    it.StartDay,
                    it.StartTime,
                    it.EndDay,
                    it.EndTime,
                    it.ReminderTime,
                    it.ReminderType,
                    it.RepeatTime,
                    it.Repetition,
                    it.PushTime,
                    it.Send,
                    it.SendName,
                    it.Title,
                    it.Category,
                    it.Urgent,
                    it.GroupId,
                    it.LastModifyTime,
                    it.LastModifyUserId,
                    it.Files
                })
                .ExecuteCommandAsync();
            if (!(upIsOk > 0))
                throw Oops.Oh(ErrorCode.D1910);
        }

        if (addEntityList.Count > 0)
        {
            var addIsOk = await _repository.AsInsertable(addEntityList).ExecuteCommandAsync();
            if (!(addIsOk > 0))
                throw Oops.Oh(ErrorCode.D1910);
        }

        var logIsOk = await _repository.AsSugarClient().Insertable(entityLogList).ExecuteCommandAsync();
        if (!(logIsOk > 0))
            throw Oops.Oh(ErrorCode.D1911);

        var startTime = DateTime.Now;
        var nextDayTime = DateTime.Now.AddDays(1);
        var endTime = new DateTime(nextDayTime.Year, nextDayTime.Month, nextDayTime.Day, 0, 5, 0);
        var pushList = scheduleJobList.FindAll(it => it.PushTime >= startTime && it.PushTime < endTime && it.ReminderTime != -2);
        foreach (var item in pushList)
        {
            // 添加日程任务队列
            await AddPushTaskQueue(newEntity, "MBXTRC001", "1", _userManager.TenantId);
            await _cacheManager.SetAsync(string.Format("{0}:{1}:{2}", CommonConst.CACHEKEYSCHEDULE, _userManager.TenantId, id), newEntity.PushTime.ToString(), TimeSpan.FromDays(1));
        }

        var toUserIds = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
            .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(newEntity.Id))
            .Select(it => it.ToUserId)
            .ToListAsync();
        newEntity.Send = string.Empty;
        await SendScheduleMsg(newEntity, toUserIds, "2", "MBXTRC002");
    }

    /// <summary>
    /// 删除日程信息.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="type">删除类型（1：此日程，2：此日程及后续，3：所有日程）.</param>
    /// <returns></returns>
    [HttpDelete("{id}/{type}")]
    public async Task Delete(string id, int type)
    {
        List<ScheduleEntity> scheduleList = new List<ScheduleEntity>();
        List<ScheduleEntity> upScheduleList = new List<ScheduleEntity>();
        List<ScheduleUserEntity> scheduleParticipantsList = new List<ScheduleUserEntity>();
        List<ScheduleLogEntity> scheduleLogList = new List<ScheduleLogEntity>();

        var data = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.Id.Equals(id))
            .FirstAsync();

        var dataUserList = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
            .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(id))
            .ToListAsync();

        switch (type)
        {
            // 当前日程
            case 1:
                {
                    if (data.CreatorUserId == _userManager.UserId)
                    {
                        scheduleList.Add(data);
                        scheduleLogList.Add(AddScheduleLog(data, string.Join(",", dataUserList), "3"));
                    }
                    else
                    {
                        var dataUser = dataUserList.Where(it => it.ToUserId.Equals(_userManager.UserId)).First();
                        scheduleParticipantsList.Add(dataUser);

                        scheduleLogList.Add(AddScheduleLog(data, _userManager.UserId, "4"));
                    }
                }

                break;

            // 当前日程及后续
            case 2:
                {
                    var beforeSchedule = await _repository.AsQueryable()
                        .Where(it => it.DeleteMark == null && it.GroupId.Equals(data.GroupId) && it.StartDay < data.StartDay && it.CreatorUserId.Equals(_userManager.UserId))
                        .OrderBy(it => it.StartDay, OrderByType.Desc)
                        .ToListAsync();

                    // 修改之前日程的重复提醒时间
                    if (beforeSchedule.Count > 0)
                    {
                        var maxStartDay = beforeSchedule.FirstOrDefault().StartDay;
                        var repeatTime = new DateTime(maxStartDay.Year, maxStartDay.Month, maxStartDay.Day, 23, 59, 59);
                        for (int i = 0; i < beforeSchedule.Count; i++)
                        {
                            beforeSchedule[i].RepeatTime = repeatTime;
                            upScheduleList.Add(beforeSchedule[i]);
                        }
                    }

                    var dataList = await _repository.AsQueryable()
                        .Where(it => it.DeleteMark == null && it.GroupId.Equals(data.GroupId) && it.StartDay >= data.StartDay)
                        .ToListAsync();

                    if (data.CreatorUserId == _userManager.UserId)
                    {
                        scheduleList.AddRange(dataList);
                        foreach (var item in dataList)
                        {
                            var dataUser = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
                                .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(item.Id))
                                .ToListAsync();
                            scheduleLogList.Add(AddScheduleLog(item, string.Join(",", dataUser), "3"));
                        }
                    }
                    else
                    {
                        var dataUser = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
                            .Where(it => it.DeleteMark == null && it.ToUserId.Equals(_userManager.UserId) && dataList.Select(s => s.Id).ToList().Contains(it.ScheduleId))
                            .ToListAsync();
                        scheduleParticipantsList.AddRange(dataUser);

                        foreach (var item in dataList)
                        {
                            scheduleLogList.Add(AddScheduleLog(item, _userManager.UserId, "4"));
                        }
                    }
                }

                break;

            // 参与人所有日程
            case 3:
                {
                    var allDataList = await _repository.AsQueryable()
                        .Where(it => it.DeleteMark == null && it.GroupId.Equals(data.GroupId))
                        .ToListAsync();
                    if (data.CreatorUserId.Equals(_userManager.UserId))
                    {
                        foreach (var item in allDataList)
                        {
                            var userList = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
                                .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(item.Id))
                                .ToListAsync();
                            scheduleList.Add(item);
                            scheduleLogList.Add(AddScheduleLog(item, string.Join(",", userList), "3"));
                        }
                    }
                    else
                    {
                        foreach (var item in allDataList)
                        {
                            var user = await _repository.AsSugarClient().Queryable<ScheduleUserEntity>()
                                .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(item.Id) && it.ToUserId.Equals(_userManager.UserId))
                                .FirstAsync();
                            if (user.IsNotEmptyOrNull())
                            {
                                scheduleParticipantsList.Add(user);
                                scheduleLogList.Add(AddScheduleLog(item, user.ToUserId, "4"));
                            }
                        }
                    }
                }

                break;
        }

        await _repository.AsUpdateable(upScheduleList).UpdateColumns(it => it.RepeatTime).ExecuteCommandAsync();

        if (scheduleList.Count > 0)
        {
            var isOk = await _repository.AsUpdateable()
                .SetColumns(it => new ScheduleEntity()
                {
                    DeleteMark = 1,
                    DeleteTime = SqlFunc.GetDate(),
                    DeleteUserId = _userManager.UserId
                })
                .Where(it => it.DeleteMark == null && scheduleList.Select(s => s.Id).Contains(it.Id))
                .ExecuteCommandAsync();
            if (!(isOk > 0))
                throw Oops.Oh(ErrorCode.D1912);

            await _repository.AsSugarClient().Deleteable<ScheduleUserEntity>().Where(it => scheduleList.Select(s => s.Id).ToList().Contains(it.ScheduleId)).ExecuteCommandAsync();
        }

        await _repository.AsSugarClient().Updateable<ScheduleUserEntity>()
            .SetColumns(it => new ScheduleUserEntity()
            {
                EnabledMark = 0,
                LastModifyTime = SqlFunc.GetDate(),
                LastModifyUserId = _userManager.UserId
            }).Where(it => scheduleParticipantsList.Select(x => x.Id).ToList().Contains(it.Id)).ExecuteCommandAsync();

        var logIsOk = await _repository.AsSugarClient().Insertable(scheduleLogList).ExecuteCommandAsync();
        if (!(logIsOk > 0))
            throw Oops.Oh(ErrorCode.D1914);

        if (data.CreatorUserId == _userManager.UserId)
        {
            // 日程删除通知
            data.Send = string.Empty;
            await SendScheduleMsg(data, dataUserList.Select(it => it.ToUserId).ToList(), "3", "MBXTRC003");

            // 删除缓存
            await _cacheManager.DelAsync(string.Format("{0}:{1}:{2}", CommonConst.CACHEKEYSCHEDULE, _userManager.TenantId, id));
        }
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 日程当天推送列表.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<ScheduleEntity>> GetCalendarDayPushList(string tenantId)
    {
        var nextDayTime = DateTime.Now.AddDays(1);
        var endTime = new DateTime(nextDayTime.Year, nextDayTime.Month, nextDayTime.Day, 0, 5, 0);

        using var scoped = _serviceProvider.CreateScope();
        var sqlSugarClient = scoped.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var dataBaseManager = scoped.ServiceProvider.GetService<IDataBaseManager>();

        if (sqlSugarClient.CurrentConnectionConfig.ConfigId.ToString() != tenantId)
        {
            sqlSugarClient = dataBaseManager.GetTenantSqlSugarClient(tenantId);
        }

        var entityList = await sqlSugarClient.Queryable<ScheduleEntity>()
            .Where(it => it.DeleteMark == null && it.PushTime >= DateTime.Now && it.PushTime < endTime && it.ReminderTime != -2)
            .ToListAsync();

        return entityList;
    }

    /// <summary>
    /// 添加推送任务队列.
    /// </summary>
    /// <param name="entity">日程实体.</param>
    /// <param name="enCode">消息编码.</param>
    /// <param name="type">消息类型.</param>
    /// <param name="tenantId">租户ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task AddPushTaskQueue(ScheduleEntity entity, string enCode, string type, string tenantId)
    {
        TimeSpan ts = entity.PushTime.ParseToDateTime().Subtract(string.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now).ParseToDateTime());
        await _taskQueue.EnqueueAsync(
            async (provider, token) =>
            {
                using var scoped = provider.CreateScope();
                var sqlSugarClient = scoped.ServiceProvider.GetRequiredService<ISqlSugarClient>();
                var dataBaseManager = scoped.ServiceProvider.GetService<IDataBaseManager>();

                var server = scoped.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
                var addressesFeature = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
                var addresses = addressesFeature?.Addresses;

                if (sqlSugarClient.CurrentConnectionConfig.ConfigId.ToString() != tenantId)
                {
                    sqlSugarClient = dataBaseManager.GetTenantSqlSugarClient(tenantId);
                }
                var schedule = await sqlSugarClient.Queryable<ScheduleEntity>()
                  .Where(it => it.Id.Equals(entity.Id) && it.DeleteMark == null && it.PushTime.Equals(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
                  .FirstAsync();

                if (schedule.IsNotEmptyOrNull())
                {
                    var userIdList = await sqlSugarClient.Queryable<ScheduleUserEntity>()
                        .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(schedule.Id))
                        .Select(it => it.ToUserId)
                        .ToListAsync();

                    var cacheKey = string.Format("{0}:{1}:{2}", CommonConst.CACHEKEYSCHEDULE, tenantId, schedule.Id);
                    if (await _cacheManager.ExistsAsync(cacheKey) && (await _cacheManager.GetAsync(cacheKey)).Equals(schedule.PushTime.ParseToDateTime().ToString()))
                    {
                        var userEntity = await sqlSugarClient.Queryable<UserEntity>().FirstAsync(x => x.Id == schedule.CreatorUserId && x.DeleteMark == null);
                        var newToken = NetHelper.GetToken(userEntity.Id, userEntity.Account, userEntity.RealName, userEntity.IsAdministrator, tenantId);
                        var heraderDic = new Dictionary<string, object>();
                        heraderDic.Add("Authorization", newToken);
                        var scheduleTaskModel = new ScheduleTaskModel();
                        scheduleTaskModel.taskParams.Add("entity", schedule);
                        scheduleTaskModel.taskParams.Add("userList", userIdList);
                        scheduleTaskModel.taskParams.Add("type", "1");
                        scheduleTaskModel.taskParams.Add("enCode", "MBXTRC001");
                        var localAddress = addresses.FirstOrDefault().Replace("[::]", "localhost");
                        var path = string.Format("{0}/ScheduleTask/schedule", localAddress);
                        var result = await path.SetHeaders(heraderDic).SetBody(scheduleTaskModel).PostAsStringAsync();

                        await _cacheManager.DelAsync(cacheKey);
                    }
                }

                await ValueTask.CompletedTask;
            }, (int)ts.TotalMilliseconds);
    }

    /// <summary>
    /// 发送日程消息.
    /// </summary>
    /// <param name="entity">日程实例.</param>
    /// <param name="userList">接收用户.</param>
    /// <param name="type">消息类型.</param>
    /// <param name="enCode">消息编码.</param>
    /// <returns></returns>
    public async Task SendScheduleMsg(ScheduleEntity entity, List<string> userList, string type, string enCode)
    {
        #region 组装跳转参数
        var bodyDic = new Dictionary<string, object>();
        var dic = new Dictionary<string, object>();
        dic.Add("id", entity.Id);
        dic.Add("groupId", entity.GroupId);
        dic.Add("type", type);
        foreach (var item in userList) { bodyDic.Add(item, dic); }
        #endregion

        if (entity.Send.IsNotEmptyOrNull())
        {
            var sendModelList = await _messageManager.GetMessageSendModels(entity.Send);
            var creatorUserName = await _userManager.GetUserNameAsync(entity.CreatorUserId);
            foreach (var item in sendModelList)
            {
                item.toUser = userList;
                item.paramJson.Clear();
                item.paramJson.Add(new MessageSendParam
                {
                    field = "@Title",
                    value = entity.Title
                });
                item.paramJson.Add(new MessageSendParam
                {
                    field = "@Content",
                    value = entity.Content
                });
                item.paramJson.Add(new MessageSendParam
                {
                    field = "@StartDate",
                    value = entity.StartDay.ParseToString()
                });
                item.paramJson.Add(new MessageSendParam
                {
                    field = "@StartTime",
                    value = entity.StartTime
                });
                item.paramJson.Add(new MessageSendParam
                {
                    field = "@EndDate",
                    value = entity.EndDay.ParseToString()
                });
                item.paramJson.Add(new MessageSendParam
                {
                    field = "@EndTime",
                    value = entity.EndTime
                });
                item.paramJson.Add(new MessageSendParam
                {
                    field = "@CreatorUserName",
                    value = creatorUserName
                });
                await _messageManager.SendDefinedMsg(item, bodyDic);
            }
        }
        else
        {
            var paramsDic = new Dictionary<string, string>();
            var creatorUserName = await _userManager.GetUserNameAsync(entity.CreatorUserId);
            paramsDic.Add("@Title", entity.Title);
            paramsDic.Add("@Content", entity.Content);
            paramsDic.Add("@StartDate", entity.StartDay.ParseToString());
            paramsDic.Add("@StartTime", entity.StartTime);
            paramsDic.Add("@EndDate", entity.EndDay.ParseToString());
            paramsDic.Add("@EndTime", entity.EndTime);
            paramsDic.Add("@CreatorUserName", creatorUserName);
            var messageList = _messageManager.GetMessageList(enCode, userList, paramsDic, 4, bodyDic);
            await _messageManager.SendDefaultMsg(userList, messageList);
        }
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 获取提醒时间.
    /// </summary>
    /// <param name="reminderTime">提前时间提醒.</param>
    /// <param name="startDay">开始时间.</param>
    /// <param name="allDay">是否全天.</param>
    /// <returns></returns>
    private static DateTime? GetPushTime(int reminderTime, DateTime startDay, int allDay)
    {
        DateTime? pushTime = null;

        if (allDay.Equals(1))
        {
            var time = new DateTime(startDay.Year, startDay.Month, startDay.Day, 8, 0, 0);
            switch (reminderTime)
            {
                case 1: // 当天8:00
                    pushTime = time;
                    break;
                case 2: // 当天9:00
                    pushTime = time.AddHours(1);
                    break;
                case 3: // 当天10:00
                    pushTime = time.AddHours(2);
                    break;
                case 4: // 1天前8:00
                    pushTime = time.AddDays(-1);
                    break;
                case 5: // 1天前9:00
                    pushTime = time.AddHours(1).AddDays(-1);
                    break;
                case 6: // 1天前10:00
                    pushTime = time.AddHours(2).AddDays(-1);
                    break;
                case 7: // 2天前8:00
                    pushTime = time.AddDays(-2);
                    break;
                case 8: // 2天前9:00
                    pushTime = time.AddHours(1).AddDays(-2);
                    break;
                case 9: // 2天前10:00
                    pushTime = time.AddHours(2).AddDays(-2);
                    break;
                case 10: // 1周前8:00
                    pushTime = time.AddDays(-7);
                    break;
                case 11: // 1周前9:00
                    pushTime = time.AddHours(1).AddDays(-7);
                    break;
                case 12: // 1周前10:00
                    pushTime = time.AddHours(2).AddDays(-7);
                    break;
            }
        }
        else
        {
            switch (reminderTime)
            {
                case -1: // 开始时
                    pushTime = startDay;
                    break;
                case 5: // 提前5分钟
                    pushTime = startDay.AddMinutes(-5);
                    break;
                case 10: // 提前10分钟
                    pushTime = startDay.AddMinutes(-10);
                    break;
                case 15: // 提前15分钟
                    pushTime = startDay.AddMinutes(-15);
                    break;
                case 30: // 提前30分钟
                    pushTime = startDay.AddMinutes(-30);
                    break;
                case 60: // 提前1小时
                    pushTime = startDay.AddHours(-1);
                    break;
                case 120: // 提前2小时
                    pushTime = startDay.AddHours(-2);
                    break;
                case 1440: // 提前1天
                    pushTime = startDay.AddDays(-1);
                    break;
                case 2880: // 提前2天
                    pushTime = startDay.AddDays(-2);
                    break;
                case 10080: // 提前1周
                    pushTime = startDay.AddDays(-7);
                    break;
            }
        }

        return pushTime;
    }

    /// <summary>
    /// 获取开始时间.
    /// </summary>
    /// <param name="startDay">开始日期.</param>
    /// <param name="time">开始时间（时：分）.</param>
    /// <param name="num"></param>
    /// <param name="repetition">2-每日,3-每周,4-每月,5-每年.</param>
    /// <returns></returns>
    private DateTime GetStartTime(DateTime startDay, string time, int num, string repetition)
    {
        var startTime = startDay;
        if (time.IsNotEmptyOrNull())
        {
            // 开始时间
            startTime = string.Format("{0} {1}", string.Format("{0:yyyy-MM-dd}", startDay), time).ParseToDateTime();
        }

        switch (repetition)
        {
            case "1": // 不重复
                break;
            case "2": // 每天重复
                startTime = startTime.AddDays(num);
                break;
            case "3": // 每周重复
                startTime = startTime.AddDays(num * 7);
                break;
            case "4": // 每月重复
                startTime = startTime.AddMonths(num);
                break;
            case "5": // 每年重复
                startTime = startTime.AddYears(num);
                break;
        }

        return startTime;
    }

    /// <summary>
    /// 获取结束时间.
    /// </summary>
    /// <param name="startTime">开始时间.</param>
    /// <param name="duration">时长.</param>
    /// <param name="endDay">结束日期.</param>
    /// <param name="endTime">结束时间（时：分）.</param>
    /// <param name="num">重复提醒循环次数.</param>
    /// <param name="repetition">重复提醒（1-不重复,2-每日,3-每周,4-每月,5-每年）.</param>
    /// <returns></returns>
    private DateTime GetEndTime(DateTime startTime, int duration, DateTime endDay, string endTime, int num, string repetition)
    {
        // 结束时间
        DateTime newEndDay = startTime;

        switch (duration)
        {
            case 30: // 30分钟
                newEndDay = newEndDay.AddMinutes(30);
                break;
            case 60: // 1小时
                newEndDay = newEndDay.AddHours(1);
                break;
            case 90: // 1.5小时
                newEndDay = newEndDay.AddHours(1).AddMinutes(30);
                break;
            case 120: // 2小时
                newEndDay = newEndDay.AddHours(2);
                break;
            case 180: // 3小时
                newEndDay = newEndDay.AddHours(3);
                break;
            case -1: // 自定义
                newEndDay = string.Format("{0} {1}", string.Format("{0:yyyy-MM-dd}", endDay), endTime).ParseToDateTime();
                switch (repetition)
                {
                    case "1": // 不重复
                        break;
                    case "2": // 每天重复
                        newEndDay = newEndDay.AddDays(num);
                        break;
                    case "3": // 每周重复
                        newEndDay = newEndDay.AddDays(num * 7);
                        break;
                    case "4": // 每月重复
                        newEndDay = newEndDay.AddMonths(num);
                        break;
                    case "5": // 每年重复
                        newEndDay = newEndDay.AddYears(num);
                        break;
                }

                break;

            default:
                newEndDay = endDay;
                switch (repetition)
                {
                    case "1": // 不重复
                        break;
                    case "2": // 每天重复
                        newEndDay = newEndDay.AddDays(num);
                        break;
                    case "3": // 每周重复
                        newEndDay = newEndDay.AddDays(num * 7);
                        break;
                    case "4": // 每月重复
                        newEndDay = newEndDay.AddMonths(num);
                        break;
                    case "5": // 每年重复
                        newEndDay = newEndDay.AddYears(num);
                        break;
                }

                break;
        }

        return newEndDay;
    }

    /// <summary>
    /// 添加日程.
    /// </summary>
    /// <param name="input">创建日程输入.</param>
    /// <param name="num">重复提醒循环次数.</param>
    /// <param name="repetition">重复提醒（1-不重复,2-每日,3-每周,4-每月,5-每年）.</param>
    /// <param name="groupId">同分组ID.</param>
    /// <returns></returns>
    private ScheduleEntity AddSchedule(SysScheduleCrInput input, int num, string repetition, string groupId = "")
    {
        var entity = input.Adapt<ScheduleEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;
        entity.GroupId = string.IsNullOrEmpty(groupId) ? SnowflakeIdHelper.NextId() : groupId;
        entity.EnabledMark = 1;
        if (entity.AllDay == 1)
        {
            var startDay = new DateTime(entity.StartDay.Year, entity.StartDay.Month, entity.StartDay.Day, 0, 0, 0);
            var startTime = string.Empty;
            var endDay = new DateTime(entity.EndDay.Year, entity.EndDay.Month, entity.EndDay.Day, 23, 59, 59);
            var endTime = string.Empty;
            entity.Duration = 0;
            entity.StartDay = GetStartTime(startDay, startTime, num, repetition);
            entity.EndDay = GetEndTime(entity.StartDay, entity.Duration, endDay, endTime, num, repetition);
        }
        else
        {
            entity.StartDay = GetStartTime(input.startDay, input.startTime, num, repetition);
            entity.EndDay = GetEndTime(entity.StartDay, input.duration, input.endDay, input.endTime, num, repetition);
        }

        entity.PushTime = input.reminderTime == -2 ? null : GetPushTime(input.reminderTime, entity.StartDay, input.allDay);

        return entity;
    }

    /// <summary>
    /// 修改日程.
    /// </summary>
    /// <param name="input">修改日程输入.</param>
    /// <param name="id">日程ID.</param>
    /// <param name="num">重复提醒循环次数.</param>
    /// <param name="repetition">重复提醒（1-不重复,2-每日,3-每周,4-每月,5-每年）.</param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    private ScheduleEntity UpdateSchedule(SysScheduleUpInput input, string id, int num, string repetition, string groupId = "")
    {
        var entity = input.Adapt<ScheduleEntity>();
        entity.Id = id;
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;
        entity.GroupId = string.IsNullOrEmpty(groupId) ? SnowflakeIdHelper.NextId() : groupId;
        if (entity.AllDay == 1)
        {
            var startDay = new DateTime(entity.StartDay.Year, entity.StartDay.Month, entity.StartDay.Day, 0, 0, 0);
            var startTime = string.Empty;
            var endDay = new DateTime(entity.EndDay.Year, entity.EndDay.Month, entity.EndDay.Day, 23, 59, 59);
            var endTime = string.Empty;
            entity.Duration = 0;
            entity.StartDay = GetStartTime(startDay, startTime, num, repetition);
            entity.EndDay = GetEndTime(entity.StartDay, entity.Duration, endDay, endTime, num, repetition);
        }
        else
        {
            entity.StartDay = GetStartTime(input.startDay, input.startTime, num, repetition);
            entity.EndDay = GetEndTime(entity.StartDay, input.duration, input.endDay, input.endTime, num, repetition);
        }

        entity.PushTime = input.reminderTime == -2 ? null : GetPushTime(input.reminderTime, entity.StartDay, input.allDay);

        return entity;
    }

    /// <summary>
    /// 保存日程参与人.
    /// </summary>
    /// <param name="scheduleId">日程ID.</param>
    /// <param name="toUserIds">参与人ID集合.</param>
    /// <param name="creatorUserId">创建人ID.</param>
    /// <returns></returns>
    private async Task AddScheduleUser(string scheduleId, List<string> toUserIds, string creatorUserId)
    {
        List<ScheduleUserEntity> addScheduleUserList = new List<ScheduleUserEntity>();

        // 创建人默认为参与人
        if (!await _repository.AsSugarClient().Queryable<ScheduleUserEntity>().Where(it => it.DeleteMark == null && it.ScheduleId.Equals(scheduleId) && it.ToUserId.Equals(creatorUserId)).AnyAsync())
        {
            var creator = new ScheduleUserEntity()
            {
                Id = SnowflakeIdHelper.NextId(),
                ScheduleId = scheduleId,
                ToUserId = creatorUserId,
                EnabledMark = 1,
                CreatorTime = DateTime.Now,
                CreatorUserId = _userManager.UserId,
                Type = 1
            };
            await _repository.AsSugarClient().Insertable(creator).ExecuteCommandAsync();
        }

        if (toUserIds.Contains(creatorUserId))
        {
            await _repository.AsSugarClient().Updateable<ScheduleUserEntity>()
                .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(scheduleId) && it.ToUserId.Equals(creatorUserId))
                .SetColumns(it => new ScheduleUserEntity()
                {
                    Type = 2,
                    LastModifyTime = SqlFunc.GetDate(),
                    LastModifyUserId = _userManager.UserId
                }).ExecuteCommandAsync();
        }
        else
        {
            await _repository.AsSugarClient().Updateable<ScheduleUserEntity>()
                .Where(it => it.DeleteMark == null && it.ScheduleId.Equals(scheduleId) && it.ToUserId.Equals(creatorUserId))
                .SetColumns(it => new ScheduleUserEntity()
                {
                    Type = 1,
                    LastModifyTime = SqlFunc.GetDate(),
                    LastModifyUserId = _userManager.UserId
                }).ExecuteCommandAsync();
        }

        await _repository.AsSugarClient().Deleteable<ScheduleUserEntity>().Where(it => it.ScheduleId.Equals(scheduleId) && !it.ToUserId.Equals(creatorUserId)).ExecuteCommandAsync();

        foreach (var item in toUserIds.Where(it => !it.Equals(creatorUserId)).ToList())
        {
            addScheduleUserList.Add(new ScheduleUserEntity()
            {
                Id = SnowflakeIdHelper.NextId(),
                ScheduleId = scheduleId,
                ToUserId = item,
                EnabledMark = 1,
                CreatorTime = DateTime.Now,
                CreatorUserId = _userManager.UserId,
                Type = 2
            });
        }
        await _repository.AsSugarClient().Insertable(addScheduleUserList).ExecuteCommandAsync();
    }

    /// <summary>
    /// 保存日程日志.
    /// </summary>
    /// <param name="entity">日程实体.</param>
    /// <param name="toUserIds">参与人ID.</param>
    /// <param name="type">操作类型(1:新增，2：修改，3：删除，4：删除参与人).</param>
    /// <returns></returns>
    private ScheduleLogEntity AddScheduleLog(ScheduleEntity entity, string toUserIds, string type)
    {
        var logEntity = entity.Adapt<ScheduleLogEntity>();
        logEntity.Id = SnowflakeIdHelper.NextId();
        logEntity.OperationType = type;
        logEntity.ScheduleId = entity.Id;
        logEntity.EnabledMark = 1;
        logEntity.CreatorTime = DateTime.Now;
        logEntity.CreatorUserId = _userManager.UserId;

        if (toUserIds.IsNotEmptyOrNull())
        {
            logEntity.UserId = toUserIds;
        }
        else
        {
            logEntity.UserId = _userManager.UserId;
        }

        return logEntity;
    }
    #endregion
}