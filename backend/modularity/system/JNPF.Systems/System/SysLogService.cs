using JNPF.Common.Core.Manager;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.LinqBuilder;
using JNPF.Logging.Attributes;
using JNPF.Systems.Entitys.Dto.SysLog;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Reflection;

namespace JNPF.Systems;

/// <summary>
/// 系统日志
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "Log", Order = 211)]
[Route("api/system/[controller]")]
public class SysLogService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SysLogEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="SysLogService"/>类型的新实例.
    /// </summary>
    public SysLogService(
        ISqlSugarRepository<SysLogEntity> repository, IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 获取系统日志列表-登录日志（带分页）.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [IgnoreLog]
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] LogListQuery input)
    {
        var whereLambda = LinqExpression.And<SysLogEntity>();
        whereLambda = whereLambda.And(x => x.Type == input.category);

        if (input.keyword.IsNotEmptyOrNull())
        {
            switch (input.category)
            {
                case 1: // 登录日志 关键字（用户、IP地址）
                    whereLambda = whereLambda.And(m => m.UserName.Contains(input.keyword) || m.IPAddress.Contains(input.keyword));
                    break;
                case 3: // 操作日志 关键字（用户、IP地址、操作模块、请求地址）
                    whereLambda = whereLambda.And(m => m.UserName.Contains(input.keyword) || m.IPAddress.Contains(input.keyword) || m.ModuleName.Contains(input.keyword) || m.RequestURL.Contains(input.keyword));
                    break;
                case 4: // 异常日志
                case 5: // 请求日志 关键字（用户、IP地址、请求地址）
                    whereLambda = whereLambda.And(m => m.UserName.Contains(input.keyword) || m.IPAddress.Contains(input.keyword) || m.RequestURL.Contains(input.keyword));
                    break;
            }
        }

        var list = await _repository.AsQueryable().ClearFilter<IZxSystemFilter>()
            .Where(whereLambda)
            .WhereIF(input.loginType.IsNotEmptyOrNull(), it => it.LoginType.Equals(input.loginType))
            .WhereIF(input.loginMark.IsNotEmptyOrNull(), it => it.LoginMark.Equals(input.loginMark))
            .WhereIF(input.requestMethod.IsNotEmptyOrNull(), it => it.RequestMethod.Equals(input.requestMethod))
            .WhereIF(input.endTime.IsNotEmptyOrNull() && input.startTime.IsNotEmptyOrNull(), it => SqlFunc.Between(it.CreatorTime, input.startTime, input.endTime))
            .OrderBy(it => it.CreatorTime, OrderByType.Desc)
            .Select(it => new LogListOutput
            {
                id = it.Id,
                creatorTime = it.CreatorTime,
                userName = it.UserName,
                ipAddress = it.IPAddress,
                ipAddressName = it.IPAddressName,
                browser = it.Browser,
                platForm = it.PlatForm,
                requestDuration = it.RequestDuration,
                requestMethod = it.RequestMethod,
                requestUrl = it.RequestURL,
                moduleName = it.ModuleName,
                loginType = it.LoginType,
                loginMark = it.LoginMark,
                abstracts = it.Description
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);

        return PageResult<LogListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取系统日志详情.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var data = await _repository.AsQueryable().FirstAsync(it => it.Id.Equals(id));
        return data.Adapt<LogInfoOutput>();
    }

    /// <summary>
    /// 操作模块.
    /// </summary>
    /// <returns></returns>
    [HttpGet("ModuleName")]
    public async Task<dynamic> ModuleNameSelector()
    {
        return App.EffectiveTypes
                .Where(u => u.IsClass && !u.IsInterface && !u.IsAbstract && typeof(IDynamicApiController).IsAssignableFrom(u))
                .SelectMany(u => u.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Where(x => x.IsDefined(typeof(OperateLogAttribute), false))
                .Select(x => new { moduleName = x.GetCustomAttribute<OperateLogAttribute>().ModuleName }).Distinct();
    }

    #endregion

    #region POST

    /// <summary>
    /// 批量删除.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpDelete]
    [UnitOfWork]
    public async Task Delete([FromBody] LogDelInput input)
    {
        await _repository.AsDeleteable().In(it => it.Id, input.ids).ExecuteCommandAsync();
    }

    /// <summary>
    /// 一键删除.
    /// </summary>
    /// <param name="category">请求参数.</param>
    /// <returns></returns>
    [HttpDelete("{category}")]
    [UnitOfWork]
    public async Task Delete(int category)
    {
        await _repository.DeleteAsync(x => x.Type == category);
    }

    /// <summary>
    /// 一键删除登录日志.
    /// </summary>
    /// <param name="category">请求参数.</param>
    /// <returns></returns>
    [HttpDelete("DeleteLoginLog")]
    [UnitOfWork]
    public async Task DeleteLoginLog()
    {
        await _repository.DeleteAsync(x => x.UserId.Equals(_userManager.UserId) && x.Type.Equals(1));
    }

    #endregion
}