using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Systems.Entitys.Dto.DataInterfaceLog;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 数据接口日志
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "DataInterfaceLog", Order = 204)]
[Route("api/system/[controller]")]
public class DataInterfaceLogService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<DataInterfaceLogEntity> _repository;

    /// <summary>
    /// 初始化一个<see cref="DataInterfaceLogService"/>类型的新实例.
    /// </summary>
    public DataInterfaceLogService(
        ISqlSugarRepository<DataInterfaceLogEntity> repository)
    {
        _repository = repository;
    }

    #region Get

    [HttpGet("{id}")]
    public async Task<dynamic> GetList(string id, [FromQuery] PageInputBase input)
    {
        var list = await _repository.AsSugarClient().Queryable<DataInterfaceLogEntity, UserEntity>((a, b) =>
        new JoinQueryInfos(JoinType.Left, b.Id == a.UserId))
             .Where(a => a.InvokId == id)
             .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.UserId.Contains(input.keyword) || a.InvokIp.Contains(input.keyword)).OrderBy(a => a.InvokTime, OrderByType.Desc)
            .Select((a, b) => new DataInterfaceLogListOutput
            {
                id = a.Id,
                invokDevice = a.InvokDevice,
                invokIp = a.InvokIp,
                userId = SqlFunc.MergeString(b.RealName, "/", b.Account),
                invokTime = a.InvokTime,
                invokType = a.InvokType,
                invokWasteTime = a.InvokWasteTime
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<DataInterfaceLogListOutput>.SqlSugarPageResult(list);
    }

    #endregion
}