using JNPF.Common.Enums;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.VisualData.Entity;
using JNPF.VisualData.Entitys.Dto.ScreenGlobal;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.VisualData;

/// <summary>
/// 业务实现：全局变量.
/// </summary>
[ApiDescriptionSettings(Tag = "BladeVisual", Name = "visual-global", Order = 160)]
[Route("api/blade-visual/[controller]")]
public class ScreenGlobalService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualGlobalEntity> _repository;

    /// <summary>
    /// 初始化一个<see cref="ScreenGlobalService"/>类型的新实例.
    /// </summary>
    public ScreenGlobalService(ISqlSugarRepository<VisualGlobalEntity> repository)
    {
        _repository = repository;
    }

    #region Get

    /// <summary>
    /// 获取大屏全局变量列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("list")]
    public async Task<dynamic> GetList([FromQuery] ScreenGlobalListQueryInput input)
    {
        var list = await _repository.AsQueryable()
            .Select(it => new ScreenGlobalListOutput
            {
                id = it.Id,
                globalName = it.GlobalName,
                globalKey = it.GlobalKey,
                globalValue = it.GlobalValue,
            })
            .ToPagedListAsync(input.current, input.size);
        return new { current = list.pagination.CurrentPage, pages = list.pagination.Total / list.pagination.PageSize, records = list.list, size = list.pagination.PageSize, total = list.pagination.Total };
    }

    /// <summary>
    /// 详情.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("detail")]
    public async Task<dynamic> GetInfo(string id)
    {
        var entity = await _repository.AsQueryable().FirstAsync(it => it.Id == id);
        return entity.Adapt<ScreenGlobalInfoOutput>();
    }

    #endregion

    #region Post

    /// <summary>
    /// 新增.
    /// </summary>
    /// <returns></returns>
    [HttpPost("save")]
    public async Task Create([FromBody] ScreenGlobalCrInput input)
    {
        var entity = input.Adapt<VisualGlobalEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        int isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <returns></returns>
    [HttpPost("update")]
    public async Task Update([FromBody] ScreenGlobalUpInput input)
    {
        var entity = input.Adapt<VisualGlobalEntity>();
        int isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <returns></returns>
    [HttpPost("remove")]
    public async Task Delete(string ids)
    {
        var entity = _repository.AsQueryable().FirstAsync(it => it.Id == ids);
        _ = entity ?? throw Oops.Oh(ErrorCode.COM1005);
        int isOk = await _repository.AsDeleteable().Where(it => it.Id == ids).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion
}
