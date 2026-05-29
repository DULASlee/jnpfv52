using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.VisualData.Entity;
using JNPF.VisualData.Entitys.Dto.ScreenRecord;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.VisualData;

/// <summary>
/// 业务实现：大屏数据源.
/// </summary>
[ApiDescriptionSettings(Tag = "BladeVisual", Name = "record", Order = 160)]
[Route("api/blade-visual/[controller]")]
public class ScreenRecordService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<VisualRecordEntity> _repository;

    /// <summary>
    /// 初始化一个<see cref="ScreenRecordService"/>类型的新实例.
    /// </summary>
    public ScreenRecordService(
        ISqlSugarRepository<VisualRecordEntity> visualRecordRepository)
    {
        _repository = visualRecordRepository;
    }

    #region Get

    /// <summary>
    /// 分页.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("list")]
    public async Task<dynamic> GetList([FromQuery] ScreenRecordListQueryInput input)
    {
        SqlSugarPagedList<ScreenRecordListOutput>? data = await _repository.AsQueryable()
            .Select(v => new ScreenRecordListOutput
            {
                id = v.Id,
                name = v.Name,
                dataType = v.DataType,
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
        VisualRecordEntity? entity = await _repository.AsQueryable().FirstAsync(v => v.Id == id);
        var data = entity.Adapt<ScreenRecordInfoOutput>();
        data.proxy = entity.Proxy.Equals(1) ? true : false;
        return data;
    }

    #endregion

    #region Post

    /// <summary>
    /// 新增或修改.
    /// </summary>
    /// <returns></returns>
    [HttpPost("submit")]
    public async Task Submit([FromBody] ScreenRecordUpInput input)
    {
        VisualRecordEntity? entity = input.Adapt<VisualRecordEntity>();
        if (entity.Id.IsNotEmptyOrNull())
        {
            int isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
            if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1001);
        }
        else
        {
            entity.Id = SnowflakeIdHelper.NextId();
            int isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
            if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1000);
        }
    }

    /// <summary>
    /// 新增.
    /// </summary>
    /// <returns></returns>
    [HttpPost("save")]
    public async Task Create([FromBody] ScreenRecordCrInput input)
    {
        VisualRecordEntity? entity = input.Adapt<VisualRecordEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        entity.Proxy = input.proxy ? 1 : 0;
        int isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改.
    /// </summary>
    /// <returns></returns>
    [HttpPost("update")]
    public async Task Update([FromBody] ScreenRecordUpInput input)
    {
        VisualRecordEntity? entity = input.Adapt<VisualRecordEntity>();
        entity.Proxy = input.proxy ? 1 : 0;
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
        Task<VisualRecordEntity>? entity = _repository.AsQueryable().FirstAsync(v => v.Id == ids);
        _ = entity ?? throw Oops.Oh(ErrorCode.COM1005);
        int isOk = await _repository.AsDeleteable().Where(it => ids.Contains(it.Id)).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion
}
