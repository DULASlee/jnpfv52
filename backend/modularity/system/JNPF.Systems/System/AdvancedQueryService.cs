using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.System.AdvancedQuery;
using JNPF.Systems.Entitys.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 高级查询方案管理
/// 版 本：V3.4
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2022-06-07.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "AdvancedQuery", Order = 202)]
[Route("api/system/[controller]")]
public class AdvancedQueryService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<AdvancedQuerySchemeEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="AdvancedQueryService"/>类型的新实例.
    /// </summary>
    public AdvancedQueryService(
        ISqlSugarRepository<AdvancedQuerySchemeEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="moduleId">菜单Id.</param>
    /// <returns></returns>
    [HttpGet("{moduleId}/List")]
    public async Task<dynamic> GetList(string moduleId)
    {
        var data = await _repository.AsQueryable().Where(x => x.ModuleId.Equals(moduleId) && x.CreatorUserId.Equals(_userManager.UserId) && x.DeleteMark == null).ToListAsync();
        return new { list = data.Adapt<List<AdvancedQuerySchemeListOutput>>() };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">请求参数.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var data = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        return data.Adapt<AdvancedQuerySchemeInfoOutput>();
    }

    #endregion

    #region Post

    /// <summary>
    /// 新增.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] AdvancedQuerySchemeCrInput input)
    {
        var entity = input.Adapt<AdvancedQuerySchemeEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        entity.CreatorUserId = _userManager.UserId;
        entity.CreatorTime = DateTime.Now;
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 保存.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Save(string id, [FromBody] AdvancedQuerySchemeCrInput input)
    {
        var entity = input.Adapt<AdvancedQuerySchemeEntity>();
        if (await _repository.IsAnyAsync(x => x.Id.Equals(id)))
        {
            entity.Id = id;
            var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).ExecuteCommandAsync();
            if (isOk < 1) throw Oops.Oh(ErrorCode.COM1000);
        }
        else
        {
            entity.Id = SnowflakeIdHelper.NextId();
            entity.CreatorUserId = _userManager.UserId;
            entity.CreatorTime = DateTime.Now;
            var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteCommandAsync();
            if (isOk < 1) throw Oops.Oh(ErrorCode.COM1000);
        }
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        entity.DeleteMark = 1;
        entity.DeleteTime = DateTime.Now;
        if (entity == null)
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsUpdateable(entity).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime }).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion
}