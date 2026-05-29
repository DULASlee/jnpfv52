using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.VisualData.Entity;
using JNPF.VisualData.Entitys.Dto.ScreenComponent;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.VisualData;

/// <summary>
/// 业务实现：大屏组件.
/// </summary>
[ApiDescriptionSettings(Tag = "BladeVisual", Name = "component", Order = 160)]
[Route("api/blade-visual/[controller]")]
public class ScreenComponentService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualComponentEntity> _visualComponentRepository;

    /// <summary>
    /// 初始化一个<see cref="ScreenComponentService"/>类型的新实例.
    /// </summary>
    public ScreenComponentService(
        ISqlSugarRepository<VisualComponentEntity> visualComponentRepository)
    {
        _visualComponentRepository = visualComponentRepository;
    }

    #region Get

    /// <summary>
    /// 分页.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("list")]
    public async Task<dynamic> GetList([FromQuery] ScreenComponentListQueryInput input)
    {
        SqlSugarPagedList<ScreenComponentListOutput>? data = await _visualComponentRepository.AsQueryable()
            .Where(it => it.Type.Equals(input.type))
            .Select(v => new ScreenComponentListOutput
            {
                id = v.Id,
                name = v.Name,
                type = v.Type,
                img = v.Img
            }).ToPagedListAsync(input.current, input.size);
        return new { current = data.pagination.CurrentPage, pages = data.pagination.Total / data.pagination.PageSize, records = data.list, size = data.pagination.PageSize, total = data.pagination.Total };
    }

    /// <summary>
    /// 详情.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("detail")]
    public async Task<dynamic> GetInfo([FromQuery] string id)
    {
        VisualComponentEntity? entity = await _visualComponentRepository.AsQueryable().FirstAsync(v => v.Id == id);
        return entity.Adapt<ScreenComponentInfoOutput>();
    }

    #endregion

    #region Post

    /// <summary>
    /// 新增或修改.
    /// </summary>
    /// <returns></returns>
    [HttpPost("submit")]
    public async Task Submit([FromBody] ScreenComponentUpInput input)
    {
        VisualComponentEntity? entity = input.Adapt<VisualComponentEntity>();
        if (entity.Id.IsNotEmptyOrNull())
        {
            int isOk = await _visualComponentRepository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
            if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1001);
        }
        else
        {
            entity.Id = SnowflakeIdHelper.NextId();
            int isOk = await _visualComponentRepository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
            if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1000);
        }
    }

    /// <summary>
    /// 新增.
    /// </summary>
    /// <returns></returns>
    [HttpPost("save")]
    public async Task Create([FromBody] ScreenComponentCrInput input)
    {
        VisualComponentEntity? entity = input.Adapt<VisualComponentEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        int isOk = await _visualComponentRepository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <returns></returns>
    [HttpPost("update")]
    public async Task Update([FromBody] ScreenComponentUpInput input)
    {
        VisualComponentEntity? entity = input.Adapt<VisualComponentEntity>();
        int isOk = await _visualComponentRepository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <returns></returns>
    [HttpPost("remove")]
    public async Task Delete(string ids)
    {
        Task<VisualComponentEntity>? entity = _visualComponentRepository.AsQueryable().FirstAsync(v => v.Id == ids);
        _ = entity ?? throw Oops.Oh(ErrorCode.COM1005);
        int isOk = await _visualComponentRepository.AsDeleteable().Where(it => ids.Contains(it.Id)).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion
}
