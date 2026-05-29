using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.System.PortalManage;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using JNPF.VisualDev.Entitys;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using SqlSugar;

namespace JNPF.Systems.System;

/// <summary>
/// 门户管理.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "PortalManage", Order = 212)]
[Route("api/system/[controller]")]
public class PortalManageService : IPortalManageService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 系统功能表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<PortalManageEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="PortalManageService"/>类型的新实例.
    /// </summary>
    public PortalManageService(
        ISqlSugarRepository<PortalManageEntity> repository,
        IUserManager userManager)
    {
        _repository = repository;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 获取门户管理列表.
    /// </summary>
    /// <param name="systemId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("list/{systemId}")]
    public async Task<dynamic> GetList(string systemId, [FromQuery] PortalManageListQueryInput input)
    {
        var data = await _repository.AsSugarClient().Queryable<PortalManageEntity, PortalEntity, DictionaryDataEntity, UserEntity>((pm, p, d, u) => new JoinQueryInfos(JoinType.Left, pm.PortalId == p.Id, JoinType.Left, p.Category == d.Id, JoinType.Left, pm.CreatorUserId == u.Id))
            .Where(pm => pm.DeleteMark == null && pm.SystemId.Equals(systemId) && pm.Platform.Equals(input.platform))
            .WhereIF(input.keyword.IsNotEmptyOrNull(), (pm, p) => pm.Description.Contains(input.keyword) || p.FullName.Contains(input.keyword))
            .WhereIF(input.category.IsNotEmptyOrNull(), (pm, p) => p.Category.Equals(input.category))
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), (pm, p) => pm.EnabledMark.Equals(input.enabledMark))
            .OrderBy(pm => pm.SortCode)
            .OrderBy(pm => pm.CreatorTime, OrderByType.Desc)
            .OrderByIF(!input.keyword.IsNullOrEmpty(), pm => pm.LastModifyTime, OrderByType.Desc)
            .Select((pm, p, d, u) => new PortalManageListOutput
            {
                creatorUser = SqlFunc.MergeString(u.RealName, "/", u.Account),
                creatorTime = pm.CreatorTime,
                lastModifyUser = SqlFunc.MergeString(u.RealName, "/", u.Account),
                lastModifyTime = pm.LastModifyTime,
                id = pm.Id,
                fullName = p.FullName,
                categoryId = p.Category,
                categoryName = d.FullName,
                description = pm.Description,
                enabledMark = pm.EnabledMark,
                sortCode = pm.SortCode,
                systemId = pm.SystemId,
                portalId = pm.PortalId
            })
            .ToPagedListAsync(input.currentPage, input.pageSize);

        return PageResult<PortalManageListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取门户管理.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        return await _repository.AsSugarClient().Queryable<PortalManageEntity, PortalEntity, DictionaryDataEntity>((pm, p, d) => new JoinQueryInfos(JoinType.Left, pm.PortalId == p.Id, JoinType.Left, p.Category == d.Id))
            .Where(pm => pm.DeleteMark == null && pm.Id.Equals(id))
            .Select((pm, p, d) => new PortalManageInfoOutput
            {
                id = pm.Id,
                portalId = pm.PortalId,
                systemId = pm.SystemId,
                fullName = p.FullName,
                categoryId = p.Category,
                categoryName = d.FullName,
                description = pm.Description,
                enabledMark = pm.EnabledMark,
                sortCode = pm.SortCode,
            })
            .FirstAsync();
    }

    #endregion

    #region POST

    /// <summary>
    /// 新建门户信息.
    /// </summary>
    /// <param name="input">实体对象.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] PortalManageCrInput input)
    {
        var entity = input.Adapt<PortalManageEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;

        var isOk = await _repository.AsSugarClient().Insertable(entity).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.COM1000);

        var portalState = await _repository.AsSugarClient().Queryable<PortalEntity>()
            .Where(it => it.DeleteMark == null && it.Id.Equals(entity.PortalId))
            .Select(it => it.State)
            .FirstAsync();
        if (portalState.Equals(0))
        {
            await _repository.AsSugarClient().Updateable<PortalEntity>()
                .SetColumns(it => new PortalEntity
                {
                    State = 1
                })
                .Where(it => it.Id.Equals(entity.PortalId))
                .ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 修改门户信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] PortalManageUpInput input)
    {
        var entity = input.Adapt<PortalManageEntity>();
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;

        var isOk = await _repository.AsUpdateable(entity)
            .UpdateColumns(it => new {
                it.PortalId,
                it.EnabledMark,
                it.Description,
                it.SortCode,
                it.LastModifyUserId,
                it.LastModifyTime
            }).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除门户信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        var isOk = await _repository.AsUpdateable()
            .Where(it => it.Id.Equals(id))
            .SetColumns(it => new PortalManageEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.COM1002);
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 获取用户授权的门户.
    /// </summary>
    /// <param name="systemId"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<SysPortalManageOutput>> GetSysPortalManageList(string systemId)
    {
        var output = new List<SysPortalManageOutput>();
        if (!_userManager.IsAdministrator)
        {
            var roles = _userManager.PermissionGroup;
            if (roles.Any())
            {
                var itemList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().In(a => a.ObjectId, roles).Where(a => a.ItemType == "portalManage").GroupBy(it => new { it.ItemId }).Select(a => a.ItemId).ToListAsync();
                var portalManageList = await _repository.AsSugarClient().Queryable<PortalManageEntity, PortalEntity>((pm, p) => new JoinQueryInfos(JoinType.Left, pm.PortalId == p.Id))
                    .Where(pm => pm.DeleteMark == null && pm.EnabledMark == 1 && itemList.Contains(pm.Id))
                    .WhereIF(systemId.IsNotEmptyOrNull(), pm => pm.SystemId.Equals(systemId))
                    .OrderBy(pm => pm.SortCode)
                    .OrderBy(pm => pm.CreatorTime, OrderByType.Desc)
                    .Select((pm, p) => new SysPortalManageOutput
                    {
                        id = pm.Id,
                        fullName = p.FullName,
                        platform = pm.Platform
                    })
                    .ToListAsync();
                output.AddRange(portalManageList);
            }
        }
        else
        {
            var portalManageList = await _repository.AsSugarClient().Queryable<PortalManageEntity, PortalEntity>((pm, p) => new JoinQueryInfos(JoinType.Left, pm.PortalId == p.Id))
                    .Where(pm => pm.DeleteMark == null && pm.EnabledMark == 1)
                    .WhereIF(systemId.IsNotEmptyOrNull(), pm => pm.SystemId.Equals(systemId))
                    .OrderBy(pm => pm.SortCode)
                    .OrderBy(pm => pm.CreatorTime, OrderByType.Desc)
                    .Select((pm, p) => new SysPortalManageOutput
                    {
                        id = pm.Id,
                        fullName = p.FullName,
                        platform = pm.Platform
                    })
                    .ToListAsync();
            output.AddRange(portalManageList);
        }

        return output;
    }

    #endregion
}
