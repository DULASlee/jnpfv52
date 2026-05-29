using JNPF.Common.Configuration;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Systems.Entitys.System;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Apps;

/// <summary>
/// App版本信息
/// 版 本：V3.3
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2022-04-07.
/// </summary>
[ApiDescriptionSettings(Tag = "App", Name = "Version", Order = 806)]
[Route("api/App/[controller]")]
public class AppVersion : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SysConfigEntity> _repository; // 系统设置

    /// <summary>
    /// 原始数据库.
    /// </summary>
    private readonly SqlSugarScope _db;

    /// <summary>
    /// 构造.
    /// </summary>
    /// <param name="sysConfigRepository"></param>
    /// <param name="context"></param>
    public AppVersion(
        ISqlSugarRepository<SysConfigEntity> repository,
        ISqlSugarClient context)
    {
        _repository = repository;
        _db = (SqlSugarScope)context;
    }

    #region Get

    /// <summary>
    /// 版本信息.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetInfo()
    {
        SysConfigEntity? data = new SysConfigEntity();

        if (KeyVariable.MultiTenancy)
        {
            data = await _db.Queryable<SysConfigEntity>().Where(x => x.Category.Equals("SysConfig") && x.Key == "sysVersion").FirstAsync();
        }
        else
        {
            data = await _repository.AsQueryable().Where(x => x.Category.Equals("SysConfig") && x.Key == "sysVersion").FirstAsync();
        }

        return new { sysVersion = data.Value };
    }

    #endregion
}