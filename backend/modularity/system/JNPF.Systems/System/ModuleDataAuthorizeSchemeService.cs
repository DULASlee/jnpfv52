using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Filter;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.ModuleDataAuthorizeScheme;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 数据权限
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "ModuleDataAuthorizeScheme", Order = 214)]
[Route("api/system/[controller]")]
public class ModuleDataAuthorizeSchemeService : IModuleDataAuthorizeSchemeService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ModuleDataAuthorizeSchemeEntity> _repository;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="ModuleDataAuthorizeSchemeService"/>类型的新实例.
    /// </summary>
    /// <param name="moduleDataAuthorizeSchemeRepository"></param>
    /// <param name="userManager"></param>
    public ModuleDataAuthorizeSchemeService(
        ISqlSugarRepository<ModuleDataAuthorizeSchemeEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="moduleId">功能主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("{moduleId}/List")]
    public async Task<dynamic> GetList(string moduleId, [FromQuery] KeywordInput input)
    {
        var list = await _repository.AsQueryable()
            .Where(x => x.DeleteMark == null && x.ModuleId == moduleId)
            .OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .OrderBy(a => a.SortCode)
            .ToListAsync();
        if (!string.IsNullOrEmpty(input.keyword))
            list = list.FindAll(t => t.EnCode.Contains(input.keyword) || t.FullName.Contains(input.keyword));
        return new { list = list.Adapt<List<ModuleDataAuthorizeSchemeListOutput>>() };
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo_Api(string id)
    {
        var data = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        return data.Adapt<ModuleDataAuthorizeSchemeInfoOutput>();
    }

    #endregion

    #region POST

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        if (!await _repository.IsAnyAsync(x => x.Id == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.COM1005);
        var isOk = await _repository.AsUpdateable().SetColumns(it => new ModuleDataAuthorizeSchemeEntity()
        {
            DeleteMark = 1,
            DeleteUserId = _userManager.UserId,
            DeleteTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1002);
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] ModuleDataAuthorizeSchemeCrInput input)
    {
        if (await _repository.IsAnyAsync(x => (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null && x.ModuleId == input.moduleId))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<ModuleDataAuthorizeSchemeEntity>();
        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 编辑.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] ModuleDataAuthorizeSchemeUpInput input)
    {
        if (await _repository.IsAnyAsync(x => (x.EnCode == input.enCode || x.FullName == input.fullName) && x.DeleteMark == null && x.ModuleId == input.moduleId && x.Id != id))
            throw Oops.Oh(ErrorCode.COM1004);
        var entity = input.Adapt<ModuleDataAuthorizeSchemeEntity>();
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="moduleId">功能主键.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<ModuleDataAuthorizeSchemeEntity>> GetList(string moduleId)
    {
        return await _repository.AsQueryable().Where(x => x.DeleteMark == null && x.ModuleId == moduleId).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();
    }

    /// <summary>
    /// 获取用户资源列表.
    /// <param name="moduleIdList">功能ids.</param>
    /// </summary>
    [NonAction]
    public async Task<List<ModuleDataAuthorizeSchemeOutput>> GetResourceList(List<string> moduleIdList)
    {
        var output = new List<ModuleDataAuthorizeSchemeOutput>();
        if (!_userManager.IsAdministrator)
        {
            if (moduleIdList != null && moduleIdList.Any())
            {
                var buttons = await _repository.AsQueryable().Where(a => moduleIdList.Contains(a.ModuleId)).Where(a => a.EnabledMark == 1 && a.DeleteMark == null).OrderBy(q => q.SortCode).ToListAsync();
                output.AddRange(buttons.Adapt<List<ModuleDataAuthorizeSchemeOutput>>());
            }

            var roles = _userManager.PermissionGroup;
            if (roles.Any())
            {
                var items = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().In(a => a.ObjectId, roles).Where(a => a.ItemType == "resource").Select(a => a.ItemId).ToListAsync();
                var buttons = await _repository.AsQueryable().Where(a => items.Contains(a.Id)).Where(a => a.EnabledMark == 1 && a.DeleteMark == null).Select<ModuleDataAuthorizeSchemeEntity>().OrderBy(q => q.SortCode).ToListAsync();
                output.AddRange(buttons.Adapt<List<ModuleDataAuthorizeSchemeOutput>>());
            }
        }
        else
        {
            var buttons = await _repository.AsQueryable().Where(a => a.EnabledMark == 1 && a.DeleteMark == null).Select<ModuleDataAuthorizeSchemeEntity>().OrderBy(q => q.SortCode).ToListAsync();
            output = buttons.Adapt<List<ModuleDataAuthorizeSchemeOutput>>();
        }

        return output;
    }

    #endregion
}