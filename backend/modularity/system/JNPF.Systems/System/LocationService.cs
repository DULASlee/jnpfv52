using JNPF.Common.Enums;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Dto.System.Location;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace JNPF.Systems.System;

/// <summary>
/// 定位.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "Location", Order = 206)]
[Route("api/system/[controller]")]
public class LocationService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 查询附近数据.
    /// </summary>
    /// <returns></returns>
    [HttpGet("around")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> GetAroundList([FromQuery] LocationAroundListInput input)
    {
        var url = "https://restapi.amap.com/v3/place/around";

        var query = new Dictionary<string, object>();
        query.Add("key", input.key);
        query.Add("location", input.location);
        query.Add("radius", input.radius);
        query.Add("offset", input.offset);
        query.Add("page", input.page);

        var response = (await url.SetQueries(query).GetAsStringAsync()).ToObject<JObject>();
        if (response == null)
            throw Oops.Oh(ErrorCode.D1904);
        return response;
    }

    /// <summary>
    /// 根据关键字查询附近数据.
    /// </summary>
    /// <returns></returns>
    [HttpGet("text")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> GetTextList([FromQuery] LocationTextListInput input)
    {
        var url = "https://restapi.amap.com/v3/place/text";

        var query = new Dictionary<string, object>();
        query.Add("key", input.key);
        query.Add("keywords", input.keywords);
        query.Add("radius", input.radius);
        query.Add("offset", input.offset);
        query.Add("page", input.page);

        var response = (await url.SetQueries(query).GetAsStringAsync()).ToObject<JObject>();
        if (response == null)
            throw Oops.Oh(ErrorCode.D1904);
        return response;
    }

    /// <summary>
    /// 输入提示.
    /// </summary>
    /// <returns></returns>
    [HttpGet("inputtips")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> GetInputtips([FromQuery] LocationInputtipsInput input)
    {
        var url = "https://restapi.amap.com/v3/assistant/inputtips";

        var query = new Dictionary<string, object>();
        query.Add("key", input.key);
        query.Add("keywords", input.keywords);

        var response = (await url.SetQueries(query).GetAsStringAsync()).ToObject<JObject>();
        if (response == null)
            throw Oops.Oh(ErrorCode.D1904);
        return response;
    }

    [HttpGet("regeo")]
    [AllowAnonymous]
    [IgnoreLog]
    public async Task<dynamic> GetRegeo([FromQuery] LocationRegeoInput input)
    {
        var url = "https://restapi.amap.com/v3/geocode/regeo";

        var query = new Dictionary<string, object>();
        query.Add("key", input.key);
        query.Add("location", input.location);

        var response = (await url.SetQueries(query).GetAsStringAsync()).ToObject<JObject>();
        if (response == null)
            throw Oops.Oh(ErrorCode.D1904);
        return response;
    }

    /// <summary>
    /// 获取静态图.
    /// </summary>
    /// <returns></returns>
    [HttpGet("staticmap")]
    [AllowAnonymous]
    [IgnoreLog]
    [NonUnify]
    public async Task<IActionResult> GetStaticmap([FromQuery] LocationStaticmap input)
    {
        var url = "https://restapi.amap.com/v3/staticmap";

        try
        {
            var query = new Dictionary<string, object>();
            query.Add("key", input.key);
            query.Add("location", input.location);
            query.Add("zoom", input.zoom);
            query.Add("size", input.size);

            var response = await url.SetQueries(query).GetAsByteArrayAsync();
            if (response == null)
                throw Oops.Oh(ErrorCode.D1904);

            return new FileContentResult(response, "image/jpeg");
        }
        catch (Exception)
        {
            throw Oops.Oh(ErrorCode.D1905);
        }
    }
}
