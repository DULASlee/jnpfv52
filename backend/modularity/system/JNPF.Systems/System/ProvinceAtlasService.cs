using JNPF.Common.Enums;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Dto.ProvinceAtlas;
using JNPF.Systems.Entitys.Entity.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace JNPF.Systems.System;

/// <summary>
/// 行政区划：地图.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "Atlas", Order = 206)]
[Route("api/system/[controller]")]
public class ProvinceAtlasService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 系统功能表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ProvinceAtlasEntity> _repository;

    /// <summary>
    /// 初始化一个<see cref="ProvinceAtlasService"/>类型的新实例.
    /// </summary>
    public ProvinceAtlasService(ISqlSugarRepository<ProvinceAtlasEntity> repository)
    {
        _repository = repository;
    }

    private static string atlasUrl = "https://geo.datav.aliyun.com/areas_v3/bound/geojson?code=";

    #region Get

    /// <summary>
    /// 获取所有列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList()
    {
        var data = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null)
            .OrderBy(it => it.SortCode)
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .ToListAsync();
        var output = data.Adapt<List<ProvinceAtlasListOutput>>();
        return output.ToTree("-1");
    }

    /// <summary>
    /// 获取地图json.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("geojson")]
    public async Task<dynamic> Geojson([FromQuery] ProvinceAtlasGeojsonInput input)
    {
        string url = atlasUrl + input.code;

        var dataId = await _repository.AsQueryable()
            .Where(it => it.EnCode.Equals(input.code) && it.DeleteMark == null)
            .Select(it => it.Id)
            .FirstAsync();
        if (await _repository.AsQueryable().AnyAsync(it => it.ParentId.Equals(dataId) && it.DeleteMark == null))
            url += "_full";

        try
        {
            var response = (await url.GetAsStringAsync()).ToObject<JObject>();
            if (response == null)
                throw Oops.Oh(ErrorCode.D1904);
            return response;
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.D1905);
        }
    }

    #endregion
}
