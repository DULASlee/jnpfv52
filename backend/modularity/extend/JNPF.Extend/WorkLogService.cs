using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extend.Entitys;
using JNPF.Extend.Entitys.Dto.WoekLog;
using JNPF.Extend.Entitys.Dto.WorkLog;
using JNPF.FriendlyException;
using JNPF.Systems.Interfaces.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Extend;

/// <summary>
/// 工作日志
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[ApiDescriptionSettings(Tag = "Extend", Name = "WorkLog", Order = 600)]
[Route("api/extend/[controller]")]
public class WorkLogService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<WorkLogEntity> _repository;
    private readonly IUsersService _usersService;
    private readonly IUserManager _userManager;
    private readonly ITenant _db;

    public WorkLogService(ISqlSugarRepository<WorkLogEntity> repository, IUsersService usersService, IUserManager userManager, ISqlSugarClient context)
    {
        _repository = repository;
        _usersService = usersService;
        _userManager = userManager;
        _db = context.AsTenant();
    }

    #region Get
    /// <summary>
    /// 列表(我发出的)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("Send")]
    public async Task<dynamic> GetSendList([FromQuery] PageInputBase input)
    {
        var list = await _repository.AsQueryable().Where(x => x.CreatorUserId == _userManager.UserId && x.DeleteMark == null)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), m => m.Title.Contains(input.keyword) || m.Description.Contains(input.keyword))
            .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.LastModifyTime, OrderByType.Desc).ToPagedListAsync(input.currentPage, input.pageSize);
        var pageList = new SqlSugarPagedList<WorkLogListOutput>()
        {
            list = list.list.Adapt<List<WorkLogListOutput>>(),
            pagination = list.pagination
        };
        return PageResult<WorkLogListOutput>.SqlSugarPageResult(pageList);
    }

    /// <summary>
    /// 列表(我收到的)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("Receive")]
    public async Task<dynamic> GetReceiveList([FromQuery] PageInputBase input)
    {
        var list = await _repository.AsSugarClient().Queryable<WorkLogEntity, WorkLogShareEntity>(
            (a, b) => new JoinQueryInfos(JoinType.Left, a.Id == b.WorkLogId))
            .Where((a, b) => a.DeleteMark == null && b.ShareUserId == _userManager.UserId)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.Title.Contains(input.keyword))
            .Select(a => new WorkLogListOutput()
            {
                id = a.Id,
                title = a.Title,
                question = a.Question,
                creatorTime = a.CreatorTime,
                todayContent = a.TodayContent,
                tomorrowContent = a.TomorrowContent,
                toUserId = a.ToUserId,
                sortCode = a.SortCode,
                lastModifyTime = a.LastModifyTime
            }).MergeTable()
            .OrderBy(a => a.sortCode).OrderBy(a => a.creatorTime, OrderByType.Desc)
            .OrderByIF(!string.IsNullOrEmpty(input.keyword), t => t.lastModifyTime, OrderByType.Desc)
            .ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<WorkLogListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 信息
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var output = (await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null)).Adapt<WorkLogInfoOutput>();
        output.userIds = output.toUserId;
        var userNameList = new List<string>();
        foreach (var item in output.toUserId.Split(","))
        {
            userNameList.Add(await _usersService.GetUserName(item));
        }
        output.toUserId = string.Join(",", userNameList);
        return output;
    }
    #endregion

    #region Post

    /// <summary>
    /// 添加.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] WorkLogCrInput input)
    {
        try
        {
            var entity = input.Adapt<WorkLogEntity>();
            entity.ToUserId = string.Join(",", input.userIds);
            entity.Id = SnowflakeIdHelper.NextId();
            entity.ToUserId = string.Join(",", input.userIds);
            List<WorkLogShareEntity> workLogShareList = entity.ToUserId.Split(',').Select(x => new WorkLogShareEntity()
            {
                Id = SnowflakeIdHelper.NextId(),
                ShareTime = DateTime.Now,
                WorkLogId = entity.Id,
                ShareUserId = x
            }).ToList();
            _db.BeginTran();
            _repository.AsSugarClient().Insertable(workLogShareList).ExecuteCommand();
            var isOk = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
            if (isOk < 1)
                throw Oops.Oh(ErrorCode.COM1000);
            _db.CommitTran();
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw Oops.Oh(ErrorCode.COM1000);
        }
    }

    /// <summary>
    /// 修改
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] WorkLogUpInput input)
    {
        try
        {
            var entity = input.Adapt<WorkLogEntity>();
            entity.ToUserId = string.Join(",", input.userIds);
            List<WorkLogShareEntity> workLogShareList = entity.ToUserId.Split(',').Select(x => new WorkLogShareEntity()
            {
                Id = SnowflakeIdHelper.NextId(),
                ShareTime = DateTime.Now,
                WorkLogId = entity.Id,
                ShareUserId = x
            }).ToList();
            _db.BeginTran();
            _repository.AsSugarClient().Deleteable<WorkLogShareEntity>().Where(x => x.WorkLogId == id).ExecuteCommand();
            _repository.AsSugarClient().Insertable(workLogShareList).ExecuteCommand();
            var isOk = await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
            if (isOk < 1)
                throw Oops.Oh(ErrorCode.COM1001);
            _db.CommitTran();
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw Oops.Oh(ErrorCode.COM1001);
        }
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        try
        {
            var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
            if (entity == null)
                throw Oops.Oh(ErrorCode.COM1005);
            _db.BeginTran();
            _repository.AsSugarClient().Deleteable<WorkLogShareEntity>(x => x.WorkLogId == id).ExecuteCommand();
            var isOk = await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.Delete()).ExecuteCommandAsync();
            if (isOk < 1)
                throw Oops.Oh(ErrorCode.COM1002);
            _db.CommitTran();
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw Oops.Oh(ErrorCode.COM1002);
        }
    }
    #endregion
}