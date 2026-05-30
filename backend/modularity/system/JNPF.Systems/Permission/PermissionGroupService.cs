using JNPF.Common.Const;
using JNPF.Extensions;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.PermissionGroup;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.VisualDev.Entitys;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 业务实现：权限组管理
/// 版 本：V3.5.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2023.06.29.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "PermissionGroup", Order = 163)]
[Route("api/Permission/[controller]")]
public class PermissionGroupService : IUserGroupService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<PermissionGroupEntity> _repository;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 用户信息.
    /// </summary>
    private readonly UsersService _usersService;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    /// <summary>
    /// 缓存管理器.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 初始化一个<see cref="PermissionGroupService"/>类型的新实例.
    /// </summary>
    public PermissionGroupService(
        ISqlSugarRepository<PermissionGroupEntity> repository,
        UsersService usersService,
        ICacheManager cacheManager,
        IUserManager userManager,
        IMHandler imHandler)
    {
        _repository = repository;
        _cacheManager = cacheManager;
        _userManager = userManager;
        _usersService = usersService;
        _imHandler = imHandler;
    }

    #region GET

    /// <summary>
    /// 获取列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] CommonInput input)
    {
        var dictionaryTypeEntity = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "groupType" && x.DeleteMark == null);
        SqlSugarPagedList<PermissionGroupListOutput>? data = await _repository.AsSugarClient().Queryable<PermissionGroupEntity>()
            .WhereIF(input.keyword.IsNotEmptyOrNull(), x => x.FullName.Contains(input.keyword) || x.EnCode.Contains(input.keyword))
            .WhereIF(input.enabledMark.IsNotEmptyOrNull(), a => a.EnabledMark.Equals(input.enabledMark))
            .Where(x => x.DeleteMark == null).OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc).OrderByIF(!input.keyword.IsNullOrEmpty(), x => x.LastModifyTime, OrderByType.Desc)
            .Select(x => new PermissionGroupListOutput
            {
                id = x.Id,
                fullName = x.FullName,
                enCode = x.EnCode,
                enabledMark = x.EnabledMark,
                creatorTime = x.CreatorTime,
                sortCode = x.SortCode,
                description = x.Description
            }).ToPagedListAsync(input.currentPage, input.pageSize);

        return PageResult<PermissionGroupListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        var entity = await _repository.GetSingleAsync(p => p.Id == id);
        return entity.Adapt<PermissionGroupUpInput>();
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="objectType">对象类型 组织、角色、岗位、分组、用户.</param>
    /// <returns></returns>
    [HttpGet("getPermissionGroup")]
    public async Task<dynamic> GetPermissionGroup([FromQuery] string id, string objectType)
    {
        var dataList = await _repository.AsQueryable().Where(p => p.EnabledMark.Equals(1) && p.DeleteMark == null && p.PermissionMember.Contains(id + "--")).ToListAsync();
        return new { list = dataList.Adapt<List<PermissionGroupInfoOutput>>() };
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="input">input.</param>
    /// <returns></returns>
    [HttpGet("getPermission")]
    public async Task<dynamic> GetPermission([FromQuery] PermissionInput input)
    {
        var res = new List<PermissionGroupTreeOutPut>();
        var authList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
            .Where(x => x.ObjectId.Equals(input.permissionId)).Select(x => x.ItemId).ToListAsync();

        var systemList = await _repository.AsSugarClient().Queryable<SystemEntity>()
            .Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1) && !x.EnCode.Equals("mainSystem"))
            .Where(x => authList.Contains(x.Id))
            .OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc).ToListAsync();

        var moduleList = await _repository.AsSugarClient().Queryable<ModuleEntity>()
            .Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1))
            .Where(x => authList.Contains(x.Id)).ToListAsync();

        if (input.itemType != "system" && input.itemType != "portalManage")
        {
            systemList.ForEach(it =>
            {
                if (moduleList.Any(x => x.SystemId != null && x.SystemId.Equals(it.Id) && x.Category.Equals("Web")))
                {
                    var webNewId = SnowflakeIdHelper.NextId();
                    moduleList.Where(x => x.SystemId != null && x.SystemId.Equals(it.Id) && x.Category.Equals("Web") && x.ParentId.Equals("-1")).ToList().ForEach(x =>
                    {
                        x.ParentId = webNewId;
                    });
                    moduleList.Add(new ModuleEntity()
                    {
                        Id = webNewId,
                        FullName = "WEB菜单",
                        Category = "Web",
                        Icon = "icon-ym icon-ym-pc",
                        SystemId = it.Id,
                        ParentId = it.Id
                    });
                }
                if (moduleList.Any(x => x.SystemId != null && x.SystemId.Equals(it.Id) && x.Category.Equals("App")))
                {
                    var appNewId = SnowflakeIdHelper.NextId();
                    moduleList.Where(x => x.SystemId != null && x.SystemId.Equals(it.Id) && x.Category.Equals("App") && x.ParentId.Equals("-1")).ToList().ForEach(x =>
                    {
                        x.ParentId = appNewId;
                    });
                    moduleList.Add(new ModuleEntity()
                    {
                        Id = appNewId,
                        FullName = "APP菜单",
                        Category = "App",
                        Icon = "icon-ym icon-ym-mobile",
                        SystemId = it.Id,
                        ParentId = it.Id
                    });
                }

                var addModuleList = moduleList.Where(x => x.SystemId != null && x.SystemId.Equals(it.Id)).Select(x => new PermissionGroupTreeOutPut
                {
                    id = x.Id,
                    parentId = x.ParentId.Equals("-1") ? x.SystemId : x.ParentId,
                    fullName = x.FullName,
                    icon = x.Icon
                }).ToList();

                res.AddRange(addModuleList);

                if (addModuleList.Any())
                {
                    res.Add(new PermissionGroupTreeOutPut
                    {
                        id = it.Id,
                        parentId = "-1",
                        fullName = it.FullName,
                        icon = it.Icon
                    });
                }
            });
        }

        switch (input.itemType)
        {
            case "system":
                res = await _repository.AsSugarClient().Queryable<SystemEntity>()
                    .Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1) && !x.EnCode.Equals("mainSystem"))
                    .Where(x => authList.Contains(x.Id))
                    .OrderBy(x => x.SortCode)
                    .OrderBy(x => x.CreatorTime, OrderByType.Desc)
                    .Select(x => new PermissionGroupTreeOutPut
                    {
                        id = x.Id,
                        fullName = x.FullName,
                        icon = x.Icon
                    }).ToListAsync();

                break;
            case "module":
                break;
            case "button":
                var btnres = new List<PermissionGroupTreeOutPut>();
                var btn = await _repository.AsSugarClient().Queryable<ModuleButtonEntity>()
                    .Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1))
                    .Where(x => authList.Contains(x.Id))
                    .Select(x => new PermissionGroupTreeOutPut
                    {
                        id = x.Id,
                        parentId = x.ModuleId,
                        fullName = x.FullName,
                        icon = x.Icon
                    }).ToListAsync();
                foreach (var item in res)
                {
                    if (btn.Any(it => it.parentId.Equals(item.id)))
                        btnres.AddRange(btn.FindAll(it => it.parentId.Equals(item.id)));
                }

                res = GetGroupTreeOutPut(res, btnres);
                break;
            case "column":
                var columnres = new List<PermissionGroupTreeOutPut>();
                var column = await _repository.AsSugarClient().Queryable<ModuleColumnEntity>()
                    .Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1))
                    .Where(x => authList.Contains(x.Id))
                    .Select(x => new PermissionGroupTreeOutPut
                    {
                        id = x.Id,
                        parentId = x.ModuleId,
                        fullName = x.FullName
                    }).ToListAsync();
                foreach (var item in res)
                {
                    if (column.Any(it => it.parentId.Equals(item.id)))
                        columnres.AddRange(column.FindAll(it => it.parentId.Equals(item.id)));
                }

                res = GetGroupTreeOutPut(res, columnres);
                break;
            case "form":
                var formres = new List<PermissionGroupTreeOutPut>();
                var form = await _repository.AsSugarClient().Queryable<ModuleFormEntity>()
                    .Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1))
                    .Where(x => authList.Contains(x.Id))
                    .Select(x => new PermissionGroupTreeOutPut
                    {
                        id = x.Id,
                        parentId = x.ModuleId,
                        fullName = x.FullName,
                    }).ToListAsync();
                foreach (var item in res)
                {
                    if (form.Any(it => it.parentId.Equals(item.id)))
                        formres.AddRange(form.FindAll(it => it.parentId.Equals(item.id)));
                }

                res = GetGroupTreeOutPut(res, formres);
                break;
            case "resource":
                var resourceres = new List<PermissionGroupTreeOutPut>();
                var resource = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>()
                    .Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1))
                    .Where(x => authList.Contains(x.Id))
                    .Select(x => new PermissionGroupTreeOutPut
                    {
                        id = x.Id,
                        parentId = x.ModuleId,
                        fullName = x.FullName,
                    }).ToListAsync();
                foreach (var item in res)
                {
                    if (resource.Any(it => it.parentId.Equals(item.id)))
                        resourceres.AddRange(resource.FindAll(it => it.parentId.Equals(item.id)));
                }

                res = GetGroupTreeOutPut(res, resourceres);
                break;
            case "portalManage":
                int i = 1;
                foreach (var item in systemList)
                {
                    // 系统下的所有门户
                    var sysPortalList = await _repository.AsSugarClient().Queryable<PortalManageEntity, PortalEntity>((pm, p) => new JoinQueryInfos(JoinType.Left, pm.PortalId == p.Id))
                        .Where((pm, p) => pm.EnabledMark == 1 && pm.DeleteMark == null && p.EnabledMark == 1 && p.DeleteMark == null)
                        .Where(pm => pm.SystemId.Equals(item.Id) && authList.Contains(pm.Id))
                        .Select((pm, p) => new PermissionGroupPortalTreeOutPut
                        {
                            id = pm.Id,
                            parentId = pm.SystemId,
                            fullName = p.FullName,
                            platform = pm.Platform
                        })
                        .ToListAsync();

                    if (sysPortalList.Any(it => it.platform.Equals("Web")))
                    {
                        var webId = i.ToString();
                        res.Add(new PermissionGroupPortalTreeOutPut
                        {
                            id = webId,
                            parentId = item.Id,
                            fullName = "WEB门户",
                            icon = "icon-ym icon-ym-pc"
                        });

                        foreach (var web in sysPortalList.Where(it => it.platform.Equals("Web") && it.parentId.Equals(item.Id)))
                            web.parentId = webId;
                        i++;
                    }
                    if (sysPortalList.Any(it => it.platform.Equals("App")))
                    {
                        var appId = i.ToString();
                        res.Add(new PermissionGroupPortalTreeOutPut
                        {
                            id = appId,
                            parentId = item.Id,
                            fullName = "APP门户",
                            icon = "icon-ym icon-ym-mobile"
                        });

                        foreach (var app in sysPortalList.Where(it => it.platform.Equals("App") && it.parentId.Equals(item.Id)))
                            app.parentId = appId;
                        i++;
                    }
                    res.AddRange(sysPortalList);
                    if (sysPortalList.Any())
                    {
                        res.Add(new PermissionGroupPortalTreeOutPut
                        {
                            id = item.Id,
                            parentId = "-1",
                            icon = item.Icon,
                            fullName = item.FullName
                        });
                    }
                }

                break;
        }

        return res.ToTree("-1");
    }

    private List<PermissionGroupTreeOutPut> GetGroupTreeOutPut(List<PermissionGroupTreeOutPut> module, List<PermissionGroupTreeOutPut> output)
    {
        var res = new List<PermissionGroupTreeOutPut>();

        foreach (var item in output)
        {
            // 寻找菜单上级
            foreach (var addItem in GetGroupTreeOutPut(item.parentId, module))
            {
                if (!res.Any(x => x.id.Equals(addItem.id)))
                {
                    res.Add(addItem);
                }
            }
        }

        res.AddRange(output);
        return res;
    }
    private List<PermissionGroupTreeOutPut> GetGroupTreeOutPut(string pId, List<PermissionGroupTreeOutPut> module)
    {
        var res = new List<PermissionGroupTreeOutPut>();

        // 寻找菜单上级
        var pItem = module.Find(x => x.id.Equals(pId));
        if (pItem != null)
        {
            res.Add(pItem);
            var addList = GetGroupTreeOutPut(pItem.parentId, module);
            res.AddRange(addList);
        }

        return res;
    }

    /// <summary>
    /// 权限成员.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("PermissionMember/{id}")]
    public async Task<dynamic> GetPermissionMember(string id)
    {
        var entity = await _repository.GetSingleAsync(p => p.Id == id);
        var idList = entity.PermissionMember?.Split(',').ToList();
        if (idList != null && idList.Any()) idList = idList.Distinct().ToList();
        return await _usersService.GetSelectedList(new Entitys.Dto.User.UserSelectedInput() { ids = idList, pagination = new PageInputBase() { currentPage = 1, pageSize = 999999 } });
    }

    /// <summary>
    /// 选择权限组.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetPermissionGroupSelector()
    {
        var list = await _repository.AsQueryable()
            .Where(it => it.DeleteMark == null && it.EnabledMark == 1)
            .Select(it => new PermissionGroupSelectorOutput
            {
                id = it.Id,
                fullName = it.FullName,
                enCode = it.EnCode,
                icon = "icon-ym icon-ym-authGroup"
            }).ToListAsync();
        return new { list = list };
    }

    #endregion

    #region POST

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] PermissionGroupCrInput input)
    {
        if (await _repository.IsAnyAsync(p => p.FullName == input.fullName && p.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2402);
        if (await _repository.IsAnyAsync(p => p.EnCode == input.enCode && p.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2401);
        PermissionGroupEntity? entity = input.Adapt<PermissionGroupEntity>();
        int isOk = await _repository.AsInsertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D2400);
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        PermissionGroupEntity? entity = await _repository.GetSingleAsync(p => p.Id == id && p.DeleteMark == null);
        int isOk = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D2503);

        await ForcedOffline(entity.PermissionMember);
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] PermissionGroupUpInput input)
    {
        PermissionGroupEntity oldEntity = await _repository.GetSingleAsync(it => it.Id == id);
        if (await _repository.IsAnyAsync(p => p.FullName == input.fullName && p.DeleteMark == null && p.Id != id))
            throw Oops.Oh(ErrorCode.D2502);
        if (await _repository.IsAnyAsync(p => p.EnCode == input.enCode && p.DeleteMark == null && p.Id != id))
            throw Oops.Oh(ErrorCode.D2501);

        PermissionGroupEntity entity = input.Adapt<PermissionGroupEntity>();
        int isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.D2504);
        if (oldEntity.EnabledMark == 1 && entity.EnabledMark.Equals(0)) await ForcedOffline(entity.PermissionMember);
    }

    /// <summary>
    /// 更新权限成员.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("PermissionMember/{id}")]
    public async Task UpdatePermissionMember([FromBody] PermissionMemberUpInput input)
    {
        PermissionGroupEntity oldEntity = await _repository.GetSingleAsync(it => it.Id == input.id);
        var permissionMember = oldEntity.PermissionMember.Copy();
        oldEntity.PermissionMember = string.Join(",", input.ids);
        int isOk = await _repository.AsUpdateable(oldEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D2504);
        if (permissionMember.IsNotEmptyOrNull() || input.ids.Any())
        {
            var pIdList = new List<string>();
            if (permissionMember.IsNotEmptyOrNull()) pIdList.AddRange(permissionMember.Split(",").ToList());
            if (input.ids.Any()) pIdList.AddRange(input.ids);
            permissionMember = string.Join(",", pIdList?.Distinct());
        }

        await ForcedOffline(permissionMember);
    }

    /// <summary>
    /// 复制.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/Copy")]
    public async Task ActionsCopy(string id)
    {
        string? random = new Random().NextLetterAndNumberString(5);
        var entity = await _repository.AsQueryable().FirstAsync(v => v.Id == id && v.DeleteMark == null);
        entity.Id = SnowflakeIdHelper.NextId();
        entity.FullName = entity.FullName + ".副本" + random;
        entity.EnCode += random;
        entity.EnabledMark = 0;
        entity.LastModifyTime = null;
        entity.LastModifyUserId = null;
        int isOk = await _repository.AsSugarClient().Insertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Create()).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D2504);

        var authorizeList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(x => x.ObjectType.Equals("Role") && x.ObjectId.Equals(id)).ToListAsync();
        if (authorizeList.Count > 0)
        {
            authorizeList.ForEach(x => x.ObjectId = entity.Id);
            await _repository.AsSugarClient().Insertable(authorizeList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync(); // 新增权限
        }
    }
    #endregion

    /// <summary>
    /// 强制权限组下的所有用户下线.
    /// </summary>
    /// <param name="permissionMember">权限组Id.</param>
    /// <returns></returns>
    private async Task ForcedOffline(string permissionMember)
    {
        var pIds = permissionMember?.Split(',').ToList();
        var pIdList = new List<string>();
        pIds?.ForEach(item => pIdList.Add(item.Split("--").FirstOrDefault()));

        // 查找该权限组下的所有成员id
        var userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => pIdList.Contains(x.ObjectId) || pIdList.Contains(x.UserId)).Select(x => x.UserId).ToListAsync();

        var onlineCacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, _userManager.TenantId);
        var list = await _cacheManager.GetAsync<List<UserOnlineModel>>(onlineCacheKey);

        if (list != null && userIds != null && userIds.Any())
        {
            // 过滤超管和分管用户,权限组的权限变动，超管和分管不受影响（即不被退出）
            var oIds = list.Select(x => x.userId).ToList();
            var uIdList = _repository.AsSugarClient().Queryable<UserEntity>().Where(x => oIds.Contains(x.Id) && x.IsAdministrator.Equals(0)).Select(x => x.Id).ToList();
            var orgAdminIds = _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Select(x => x.UserId).ToList();
            list = list.Where(x => uIdList.Except(orgAdminIds).Contains(x.userId)).ToList();

            var onlineUserList = list.Where(it => it.tenantId == _userManager.TenantId && userIds.Contains(it.userId)).ToList();
            if (onlineUserList != null && onlineUserList.Any())
            {
                foreach (var onlineUser in onlineUserList)
                {
                    await _imHandler.SendMessageAsync(onlineUser.connectionId, new { method = "logout", msg = "权限已变更，请重新登录！" }.ToJsonString());

                    // 删除在线用户ID
                    list.RemoveAll((x) => x.connectionId == onlineUser.connectionId);
                    await _cacheManager.SetAsync(onlineCacheKey, list);

                    // 删除用户登录信息缓存
                    var cacheKey = string.Format("{0}:{1}:{2}", _userManager.TenantId, CommonConst.CACHEKEYUSER, onlineUser.userId);
                    await _cacheManager.DelAsync(cacheKey);
                }
            }
        }
    }
}