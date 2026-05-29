using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.System.PrintLog;
using JNPF.Systems.Entitys.Entity.System;
using JNPF.Systems.Entitys.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems.System;

/// <summary>
/// 打印模板日志
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "PrintLog", Order = 200)]
[Route("api/system/[controller]")]
public class PrintLogService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<PrintLogEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="PrintDevService"/>类型的新实例.
    /// </summary>
    public PrintLogService(
        ISqlSugarRepository<PrintLogEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region Get
    /// <summary>
    /// 列表(分页).
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetList(string id, [FromQuery] PrintLogQuery input)
    {
        var list = await _repository.AsSugarClient().Queryable<PrintLogEntity, UserEntity>((p, u) => new JoinQueryInfos(JoinType.Left, p.CreatorUserId == u.Id))
            .Where(p => p.DeleteMark == null && p.PrintId.Equals(id))
            .WhereIF(input.startTime.IsNotEmptyOrNull() && input.endTime.IsNotEmptyOrNull(), p => SqlFunc.Between(p.CreatorTime, input.startTime, input.endTime))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), (p, u) => p.PrintTitle.Contains(input.keyword) || SqlFunc.MergeString(u.RealName, "/", u.Account).Contains(input.keyword))
            .OrderBy(p => p.CreatorTime, OrderByType.Desc)
            .Select((p, u) => new PrintLogOutuut
            {
                id = p.Id,
                printId = p.PrintId,
                printMan = SqlFunc.MergeString(u.RealName, "/", u.Account),
                printNum = p.PrintNum,
                printTime = p.CreatorTime,
                printTitle = p.PrintTitle
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);

        return PageResult<PrintLogOutuut>.SqlSugarPageResult(list);
    }
    #endregion

    #region Post
    /// <summary>
    /// 新增.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("save")]
    public async Task Delete([FromBody] PrintLogOutuut input)
    {
        var entity = input.Adapt<PrintLogEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;
        var isOk = await _repository.AsInsertable(entity).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }
    #endregion
}
