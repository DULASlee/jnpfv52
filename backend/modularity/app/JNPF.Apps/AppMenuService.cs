using JNPF.Apps.Entitys.Dto;
using JNPF.Apps.Interfaces;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Apps;

/// <summary>
/// App菜单
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[ApiDescriptionSettings(Tag = "App", Name = "Menu", Order = 800)]
[Route("api/App/[controller]")]
public class AppMenuService : IDynamicApiController, ITransient
{
    /// <summary>
    /// App常用数据.
    /// </summary>
    private readonly IAppDataService _appDataService;

    /// <summary>
    /// 初始化一个<see cref="AppMenuService"/>类型的新实例.
    /// </summary>
    public AppMenuService(IAppDataService appDataService)
    {
        _appDataService = appDataService;
    }

    #region Get

    /// <summary>
    /// 获取菜单列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList(string keyword)
    {
        List<AppMenuListOutput>? list = (await _appDataService.GetAppMenuList(keyword)).Adapt<List<AppMenuListOutput>>();
        return new { list = list.ToTree("-1") };
    }

    #endregion
}