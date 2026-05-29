using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Systems.Entitys.Dto.Authorize;
using JNPF.Systems.Entitys.Model.Authorize;
using JNPF.Systems.Entitys.Model.Menu;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.VisualDev.Entitys;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 业务实现：操作权限.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Authority", Order = 170)]
[Route("api/permission/[controller]")]
public class AuthorizeService : IAuthorizeService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 权限操作表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<AuthorizeEntity> _authorizeRepository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 缓存管理器.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    /// <summary>
    /// 初始化一个<see cref="AuthorizeService"/>类型的新实例.
    /// </summary>
    public AuthorizeService(
        ISqlSugarRepository<AuthorizeEntity> authorizeRepository,
        ICacheManager cacheManager,
        IUserManager userManager,
        IMHandler imHandler)
    {
        _authorizeRepository = authorizeRepository;
        _cacheManager = cacheManager;
        _userManager = userManager;
        _imHandler = imHandler;
    }

    #region Get

    /// <summary>
    /// 获取功能权限数据.
    /// </summary>
    /// <param name="itemId">模块ID.</param>
    /// <param name="objectType">对象类型.</param>
    /// <returns></returns>
    [HttpGet("Model/{itemId}/{objectType}")]
    public async Task<dynamic> GetModelList(string itemId, string objectType)
    {
        IEnumerable<string> ids = await _authorizeRepository.AsQueryable().Where(a => a.ItemId == itemId && a.ObjectType == objectType).Select(s => s.ObjectId).ToListAsync();
        return new { ids };
    }

    /// <summary>
    /// 获取模块列表展示字段权限.
    /// </summary>
    /// <param name="moduleId">模块主键.</param>
    /// <returns></returns>
    [HttpGet("GetColumnsByModuleId/{moduleId}")]
    public async Task<dynamic> GetColumnsByModuleId(string moduleId)
    {
        string? data = await _authorizeRepository.AsSugarClient().Queryable<ColumnsPurviewEntity>().Where(x => x.ModuleId == moduleId).Select(x => x.FieldList).FirstAsync();
        if (!string.IsNullOrEmpty(data)) return data.ToObject<List<ListDisplayFieldOutput>>();
        else return new List<ListDisplayFieldOutput>();
    }

    /// <summary>
    /// 获取门户权限数据.
    /// </summary>
    /// <param name="id">权限组id.</param>
    /// <returns></returns>
    [HttpGet("Portal/{id}")]
    public async Task<dynamic> GetPortalAuthorizeList(string id)
    {
        var authorizeSysIds = await _authorizeRepository.AsSugarClient().Queryable<AuthorizeEntity>()
            .Where(it => it.ItemType.Equals("system") && it.ObjectId.Equals(id))
            .Select(it => it.ItemId).ToListAsync();

        var systemList = await _authorizeRepository.AsSugarClient().Queryable<SystemEntity>()
            .Where(it => it.EnabledMark == 1 && it.DeleteMark == null && authorizeSysIds.Contains(it.Id))
            .OrderBy(it => it.SortCode).OrderByDescending(it => it.CreatorTime)
            .Select(it => new AuthorizePortalOutput
            {
                id = it.Id,
                parentId = "-1",
                fullName = it.FullName,
                platform = string.Empty,
                systemId = string.Empty,
                icon = it.Icon
            }).ToListAsync();

        var portalList = new List<AuthorizePortalOutput>();
        var ids = new List<string>();
        foreach (var item in systemList)
        {
            // 系统下的所有门户
            var sysPortalList = await _authorizeRepository.AsSugarClient().Queryable<PortalManageEntity, PortalEntity>((pm, p) => new JoinQueryInfos(JoinType.Left, pm.PortalId == p.Id))
                .Where((pm, p) => pm.EnabledMark == 1 && pm.DeleteMark == null && p.EnabledMark == 1 && p.DeleteMark == null)
                .Where(pm => pm.SystemId.Equals(item.id))
                .OrderBy(pm => pm.SortCode)
                .OrderBy(pm => pm.CreatorTime, OrderByType.Desc)
                .Select((pm, p) => new AuthorizePortalOutput
                {
                    id = pm.Id,
                    parentId = pm.SystemId,
                    fullName = p.FullName,
                    platform = pm.Platform,
                    systemId = pm.SystemId
                })
                .ToListAsync();

            if (sysPortalList.Any(it => it.platform.Equals("Web")))
            {
                var webId = string.Format("{0}1", item.id);
                portalList.Add(new AuthorizePortalOutput
                {
                    id = webId,
                    parentId = item.id,
                    fullName = "WEB门户",
                    icon = "icon-ym icon-ym-pc",
                    platform = "Web",
                    systemId = string.Empty
                });

                foreach (var web in sysPortalList.Where(it => it.platform.Equals("Web") && it.parentId.Equals(item.id)))
                    web.parentId = webId;
            }
            if (sysPortalList.Any(it => it.platform.Equals("App")))
            {
                var appId = string.Format("{0}2", item.id);
                portalList.Add(new AuthorizePortalOutput
                {
                    id = appId,
                    parentId = item.id,
                    fullName = "APP门户",
                    icon = "icon-ym icon-ym-mobile",
                    platform = "App",
                    systemId = string.Empty
                });

                foreach (var app in sysPortalList.Where(it => it.platform.Equals("App") && it.parentId.Equals(item.id)))
                    app.parentId = appId;
            }
            portalList.AddRange(sysPortalList);
            if (sysPortalList.Any()) portalList.Add(item);
        }

        var allIds = portalList.Select(it => it.id).ToList();

        // 勾选的授权Id
        var authorizeIds = await _authorizeRepository.AsQueryable()
            .Where(it => it.ItemType.Equals("portalManage") && it.ObjectId.Equals(id))
            .Select(it => it.ItemId)
            .ToListAsync();
        ids.AddRange(authorizeIds);
        foreach (var item in authorizeIds)
        {
            var portal = portalList.Find(it => it.id.Equals(item));
            if (portal.IsNotEmptyOrNull())
            {
                var portalParentIds = portalList.Where(it => it.id.Equals(portal.systemId) || (it.parentId.Equals(portal.systemId) && it.platform.Equals(portal.platform))).Select(it => it.id).ToList();
                ids.AddRange(portalParentIds);
            }
        }

        return new { list = portalList.ToTree("-1"), all = allIds, ids = ids };
    }

    #endregion

    #region Post

    /// <summary>
    /// 权限数据.
    /// </summary>
    /// <param name="objectId">对象主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("Data/{objectId}/Values")]
    public async Task<dynamic> GetDataValues(string objectId, [FromBody] AuthorizeDataQuery input)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        AuthorizeDataOutput? output = new AuthorizeDataOutput() { all = new List<string>(), ids = new List<string>(), list = new List<AuthorizeDataModelOutput>() };
        AuthorizeModel? authorizeData = new AuthorizeModel();
        var systemIds = input.moduleIds.Split(",").ToList();
        var orgAdminList = _userManager.DataScope.Select(x => x.organizeId).Distinct().ToList();
        var noAuthList = new List<string>();
        if (!_userManager.IsAdministrator)
        {
            noAuthList = await _authorizeRepository.AsQueryable().Where(x => x.ObjectId.Equals(objectId) && x.ItemType.Equals("system")).Select(x => x.ItemId).ToListAsync();
            orgAdminList.ForEach(it => noAuthList.Remove(it));
            systemIds.ForEach(item => { if (!orgAdminList.Any(x => x == item)) noAuthList.Add(item); });
        }

        var roleIds = _userManager.PermissionGroup.ToArray();
        string? userId = _userManager.UserId;
        bool isAdmin = _userManager.IsAdministrator;

        List<ModuleEntity>? menuList = await GetCurrentUserModuleAuthorize(userId, systemIds.ToArray());
        if (ignoreIds != null) menuList = menuList.Where(x => !ignoreIds.Contains(x.Id)).ToList();
        if (ignoreUrls != null) menuList = menuList.Where(x => !ignoreUrls.Contains(x.UrlAddress)).ToList();
        var systemList = await _authorizeRepository.AsSugarClient().Queryable<SystemEntity>()
            .Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1) && systemIds.Contains(x.Id))
            .WhereIF(ignoreIds != null, x => !ignoreIds.Contains(x.Id))
            .OrderBy(x => x.SortCode)
            .OrderBy(x => x.CreatorTime, OrderByType.Desc)
            .Select(x => new ModuleEntity()
            {
                Id = x.Id,
                ParentId = "-1",
                FullName = x.FullName,
                Icon = x.Icon,
                SystemId = "-1",
                SortCode = x.SortCode,
            }).ToListAsync();
        systemList.ForEach(item =>
        {
            if (menuList.Any(it => it.Category.Equals("Web") && it.SystemId.Equals(item.Id)))
            {
                var pId = string.Format("{0}1", item.Id);
                menuList.Where(it => it.Category.Equals("Web") && it.ParentId.Equals("-1") && it.SystemId.Equals(item.Id)).ToList().ForEach(it =>
                {
                    it.ParentId = pId;
                });
                menuList.Add(new ModuleEntity()
                {
                    Id = pId,
                    FullName = "WEB菜单",
                    Icon = "icon-ym icon-ym-pc",
                    ParentId = item.Id,
                    Category = "Web",
                    EnCode = "WEB菜单",
                    Type = 1,
                    SystemId = item.Id,
                    SortCode = 99998
                });
            }

            if (menuList.Any(it => it.Category.Equals("App") && it.SystemId.Equals(item.Id)))
            {
                var rId = string.Format("{0}2", item.Id);
                menuList.Where(it => it.Category.Equals("App") && it.ParentId.Equals("-1") && it.SystemId.Equals(item.Id)).ToList().ForEach(it =>
                {
                    it.ParentId = rId;
                });
                menuList.Add(new ModuleEntity()
                {
                    Id = rId,
                    FullName = "APP菜单",
                    Icon = "icon-ym icon-ym-mobile",
                    ParentId = item.Id,
                    Category = "App",
                    EnCode = "APP菜单",
                    Type = 1,
                    SystemId = item.Id,
                    SortCode = 99999
                });
            }
        });
        menuList.AddRange(systemList);

        List<ModuleButtonEntity>? moduleButtonList = await GetCurrentUserButtonAuthorize(userId);
        List<ModuleColumnEntity>? moduleColumnList = await GetCurrentUserColumnAuthorize(userId);
        List<ModuleFormEntity>? moduleFormList = await GetCurrentUserFormAuthorize(userId);
        List<ModuleDataAuthorizeSchemeEntity>? moduleDataSchemeList = await GetCurrentUserResourceAuthorize(userId);

        authorizeData.FunctionList = menuList.Adapt<List<FunctionalModel>>();
        authorizeData.ButtonList = moduleButtonList.Adapt<List<FunctionalButtonModel>>();
        authorizeData.ColumnList = moduleColumnList.Adapt<List<FunctionalViewModel>>();
        authorizeData.FormList = moduleFormList.Adapt<List<FunctionalFormModel>>();
        authorizeData.ResourceList = moduleDataSchemeList.Adapt<List<FunctionalResourceModel>>();

        #region 已勾选的权限id

        List<AuthorizeEntity>? authorizeList = await this.GetAuthorizeListByObjectId(objectId);
        List<string>? checkSystemList = authorizeList.Where(o => o.ItemType.Equals("system")).Select(m => m.ItemId).ToList();
        List<string>? checkModuleList = authorizeList.Where(o => o.ItemType.Equals("module")).Select(m => m.ItemId).ToList();
        List<string>? checkButtonList = authorizeList.Where(o => o.ItemType.Equals("button")).Select(m => m.ItemId).ToList();
        List<string>? checkColumnList = authorizeList.Where(o => o.ItemType.Equals("column")).Select(m => m.ItemId).ToList();
        List<string>? checkFormList = authorizeList.Where(o => o.ItemType.Equals("form")).Select(m => m.ItemId).ToList();
        List<string>? checkResourceList = authorizeList.Where(o => o.ItemType.Equals("resource")).Select(m => m.ItemId).ToList();

        #endregion

        List<ModuleEntity>? moduleList = new List<ModuleEntity>();
        List<string>? childNodesIds = new List<string>();
        switch (input.type)
        {
            case "system":
                var resList = await _authorizeRepository.AsSugarClient().Queryable<SystemEntity>()
                    .Where(x => x.DeleteMark == null && x.EnabledMark.Equals(1) && !x.EnCode.Equals("mainSystem"))
                    .WhereIF(!isAdmin, x => orgAdminList.Contains(x.Id) || noAuthList.Contains(x.Id))
                    .WhereIF(ignoreIds != null, x => !ignoreIds.Contains(x.Id))
                    .OrderBy(x => x.SortCode)
                    .OrderBy(x => x.CreatorTime, OrderByType.Desc)
                    .Select(x => new AuthorizeSystemDataModelOutput
                    {
                        id = x.Id,
                        fullName = x.FullName,
                        icon = x.Icon,
                        sortCode = x.SortCode,
                        disabled = false
                    }).ToListAsync();
                var resAll = resList.Select(x => x.id).ToList();
                if (!isAdmin) resList.ForEach(it => { if (noAuthList.Contains(it.id)) it.disabled = true; });
                return new { list = resList, all = resAll, ids = checkSystemList };
            case "module":
                List<AuthorizeDataModelOutput>? authorizeDataModuleList = authorizeData.FunctionList.Adapt<List<AuthorizeDataModelOutput>>();
                GetOutPutResult(ref output, authorizeDataModuleList, checkModuleList);
                return GetResult(output, noAuthList);
            case "button":
                if (string.IsNullOrEmpty(input.moduleIds))
                {
                    return output;
                }
                else
                {
                    List<string>? moduleIdList = new List<string>(input.moduleIds.Split(","));
                    moduleIdList.ForEach(ids =>
                    {
                        ModuleEntity? moduleEntity = menuList.Find(m => m.Id == ids);
                        if (moduleEntity != null) moduleList.Add(moduleEntity);
                    });

                    // 勾选的菜单末级节点菜单id集合
                    childNodesIds = GetChildNodesId(moduleList);
                }
                moduleList = await GetModuleAndSystemScheme(moduleList, menuList);
                checkButtonList.AddRange(checkSystemList);
                checkButtonList.AddRange(checkModuleList);
                output = GetButton(moduleList, moduleButtonList, childNodesIds, checkButtonList);
                return GetResult(output, noAuthList);
            case "column":
                if (string.IsNullOrEmpty(input.moduleIds))
                {
                    return output;
                }
                else
                {
                    List<string>? moduleIdList = new List<string>(input.moduleIds.Split(","));
                    moduleIdList.ForEach(ids =>
                    {
                        ModuleEntity? moduleEntity = menuList.Find(m => m.Id == ids);
                        if (moduleEntity != null) moduleList.Add(moduleEntity);
                    });

                    // 子节点菜单id集合
                    childNodesIds = GetChildNodesId(moduleList);
                }
                moduleList = await GetModuleAndSystemScheme(moduleList, menuList);
                checkColumnList.AddRange(checkSystemList);
                checkColumnList.AddRange(checkModuleList);
                output = GetColumn(moduleList, moduleColumnList, childNodesIds, checkColumnList);
                return GetResult(output, noAuthList);
            case "form":
                if (string.IsNullOrEmpty(input.moduleIds))
                {
                    return output;
                }
                else
                {
                    List<string>? moduleIdList = new List<string>(input.moduleIds.Split(","));
                    moduleIdList.ForEach(ids =>
                    {
                        ModuleEntity? moduleEntity = menuList.Find(m => m.Id == ids);
                        if (moduleEntity != null) moduleList.Add(moduleEntity);
                    });

                    // 子节点菜单id集合
                    childNodesIds = GetChildNodesId(moduleList);
                }

                moduleList = await GetModuleAndSystemScheme(moduleList, menuList);
                checkFormList.AddRange(checkSystemList);
                checkFormList.AddRange(checkModuleList);
                output = GetForm(moduleList, moduleFormList, childNodesIds, checkFormList);
                return GetResult(output, noAuthList);
            case "resource":
                if (string.IsNullOrEmpty(input.moduleIds))
                {
                    return output;
                }
                else
                {
                    List<string>? moduleIdList = new List<string>(input.moduleIds.Split(","));
                    moduleIdList.ForEach(ids =>
                    {
                        ModuleEntity? moduleEntity = menuList.Find(m => m.Id == ids);
                        if (moduleEntity != null) moduleList.Add(moduleEntity);
                    });

                    // 子节点菜单id集合
                    childNodesIds = GetChildNodesId(moduleList);
                }

                moduleList = await GetModuleAndSystemScheme(moduleList, menuList);
                checkResourceList.AddRange(checkSystemList);
                checkResourceList.AddRange(checkModuleList);
                output = GetResource(moduleList, moduleDataSchemeList, childNodesIds, checkResourceList);
                return GetResult(output, noAuthList);
            default:
                return output;
        }
    }

    /// <summary>
    /// 设置或更新岗位/角色/用户权限.
    /// </summary>
    /// <param name="objectId">参数.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("Data/{objectId}")]
    public async Task UpdateData(string objectId, [FromBody] AuthorizeDataUpInput input)
    {
        input.button = input.button.Except(input.module).ToList();
        input.column = input.column.Except(input.module).ToList();
        input.form = input.form.Except(input.module).ToList();
        input.resource = input.resource.Except(input.module).ToList();
        input.objectType = input.objectType.Equals("Batch") ? "Role" : input.objectType;
        List<AuthorizeEntity>? authorizeList = new List<AuthorizeEntity>();
        AddAuthorizeEntity(ref authorizeList, input.systemIds, objectId, input.objectType, "system");
        AddAuthorizeEntity(ref authorizeList, input.module, objectId, input.objectType, "module");
        AddAuthorizeEntity(ref authorizeList, input.button, objectId, input.objectType, "button");
        AddAuthorizeEntity(ref authorizeList, input.column, objectId, input.objectType, "column");
        AddAuthorizeEntity(ref authorizeList, input.form, objectId, input.objectType, "form");
        AddAuthorizeEntity(ref authorizeList, input.resource, objectId, input.objectType, "resource");

        // 删除除了门户外的相关权限
        await _authorizeRepository.DeleteAsync(a => a.ObjectId == objectId && !a.ItemType.Equals("portalManage"));

        if (authorizeList.Count > 0)
        {
            // 新增权限
            await _authorizeRepository.AsSugarClient().Insertable(authorizeList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }

        // 刷新权限的在线用户
        await ForcedRefresh(new List<string>() { objectId });
    }

    /// <summary>
    /// 批量设置权限.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("Data/Batch")]
    public async Task BatchData([FromBody] AuthorizeDataBatchInput input)
    {
        // 计算按钮、列表、资源三个集合内不包含菜单ID的差
        input.button = input.button.Except(input.module).ToList();
        input.column = input.column.Except(input.module).ToList();
        input.form = input.form.Except(input.module).ToList();
        input.resource = input.resource.Except(input.module).ToList();

        // 拼装权限集合
        List<AuthorizeEntity>? authorizeItemList = new List<AuthorizeEntity>();
        List<AuthorizeEntity>? authorizeObejctList = new List<AuthorizeEntity>();
        BatchAddAuthorizeEntity(ref authorizeItemList, input.systemIds, "system", true);
        BatchAddAuthorizeEntity(ref authorizeItemList, input.module, "module", true);
        BatchAddAuthorizeEntity(ref authorizeItemList, input.button, "button", true);
        BatchAddAuthorizeEntity(ref authorizeItemList, input.column, "column", true);
        BatchAddAuthorizeEntity(ref authorizeItemList, input.form, "form", true);
        BatchAddAuthorizeEntity(ref authorizeItemList, input.resource, "resource", true);
        BatchAddAuthorizeEntity(ref authorizeObejctList, new List<string>() { input.permissionGroupId }, "Role", false);
        List<AuthorizeEntity>? data = new List<AuthorizeEntity>();
        SeveBatch(ref data, authorizeObejctList, authorizeItemList);

        // 获取已有权限集合
        List<AuthorizeEntity>? existingRoleData = await _authorizeRepository.AsQueryable().Where(x => input.permissionGroupId.Equals(x.ObjectId) && x.ObjectType.Equals("Role")).ToListAsync();

        // 计算新增菜单集合与已有权限集合差
        data = data.Except(existingRoleData).ToList();

        // 数据不为空添加
        if (data.Count > 0)
        {
            // 新增权限
            int num = await _authorizeRepository.AsSugarClient().Insertable(data).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }

        // 刷新权限的在线用户
        await ForcedRefresh(new List<string>() { input.permissionGroupId });
    }

    /// <summary>
    /// 设置/更新功能权限.
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("Model/{itemId}")]
    public async Task UpdateModel(string itemId, [FromBody] AuthorizeModelInput input)
    {
        List<AuthorizeEntity>? authorizeList = new List<AuthorizeEntity>();
        List<PortalDataEntity>? portalList = new List<PortalDataEntity>();

        try
        {
            _authorizeRepository.AsSugarClient().Ado.BeginTran();

            // 权限组ID不为空
            if (input.objectId.Count > 0)
            {
                input.objectId.ForEach(item =>
                {
                    AuthorizeEntity? entity = new AuthorizeEntity();
                    entity.Id = SnowflakeIdHelper.NextId();
                    entity.CreatorTime = DateTime.Now;
                    entity.CreatorUserId = _userManager.UserId;
                    entity.ItemId = itemId;
                    entity.ItemType = input.itemType;
                    entity.ObjectId = item;
                    entity.ObjectType = input.objectType;
                    entity.SortCode = input.objectId.IndexOf(item);
                    authorizeList.Add(entity);
                });

                // 删除除了门户外的相关权限
                await _authorizeRepository.DeleteAsync(a => a.ItemId == itemId);

                // 新增权限
                await _authorizeRepository.AsSugarClient().Insertable(authorizeList).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
            }
            else
            {
                // 删除除了门户外的相关权限
                await _authorizeRepository.DeleteAsync(a => a.ItemId == itemId);
            }

            _authorizeRepository.AsSugarClient().Ado.CommitTran();
        }
        catch
        {
            _authorizeRepository.AsSugarClient().Ado.RollbackTran();
        }

        if (input.objectId.Any() && !input.itemType.Equals("portalManage")) await ForcedRefresh(input.objectId); // 刷新权限的在线用户
    }

    /// <summary>
    /// 设置模块列表展示字段权限.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("SetColumnsByModuleId")]
    public async Task SetColumnsByModuleId([FromBody] ColumnsPurviewDataUpInput input)
    {
        ColumnsPurviewEntity? entity = await _authorizeRepository.AsSugarClient().Queryable<ColumnsPurviewEntity>().Where(x => x.ModuleId == input.moduleId).FirstAsync();
        if (entity == null) entity = new ColumnsPurviewEntity();
        entity.FieldList = input.fieldList;
        entity.ModuleId = input.moduleId;

        if (entity.Id.IsNotEmptyOrNull())
        {
            // 更新
            int newEntity = await _authorizeRepository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        }
        else
        {
            entity.Id = SnowflakeIdHelper.NextId();
            entity.CreatorTime = DateTime.Now;
            entity.CreatorUserId = _userManager.UserId;
            await _authorizeRepository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 保存门户权限.
    /// </summary>
    /// <param name="id">权限组id.</param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Portal/{id}")]
    [UnitOfWork]
    public async Task SavePortalAuthorize(string id, [FromBody] AuthorizePortalSaveInput input)
    {
        var portalManageIdList = await _authorizeRepository.AsSugarClient().Queryable<PortalManageEntity>()
            .Where(it => it.EnabledMark == 1 && it.DeleteMark == null)
            .Select(it => it.Id)
            .ToListAsync();

        var authorizeList = new List<AuthorizeEntity>();
        foreach (var item in input.ids)
        {
            if (portalManageIdList.Contains(item))
            {
                authorizeList.Add(new AuthorizeEntity
                {
                    Id = SnowflakeIdHelper.NextId(),
                    ItemType = "portalManage",
                    ItemId = item,
                    ObjectType = "Role",
                    ObjectId = id,
                    SortCode = 0,
                    CreatorTime = DateTime.Now,
                    CreatorUserId = _userManager.UserId
                });
            }
        }

        await _authorizeRepository.AsDeleteable().Where(it => it.ObjectType.Equals("Role") && it.ObjectId.Equals(id) && it.ItemType.Equals("portalManage")).ExecuteCommandAsync();
        await _authorizeRepository.AsInsertable(authorizeList).ExecuteCommandAsync();
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 添加权限接口参数组装.
    /// </summary>
    /// <param name="list">返回参数.</param>
    /// <param name="itemIds">权限数据id.</param>
    /// <param name="objectId">对象ID.</param>
    /// <param name="objectType">分类.</param>
    /// <param name="itemType">权限分类.</param>
    private void AddAuthorizeEntity(ref List<AuthorizeEntity> list, List<string> itemIds, string objectId, string objectType, string itemType)
    {
        foreach (string? item in itemIds)
        {
            AuthorizeEntity? entity = new AuthorizeEntity();
            entity.Id = SnowflakeIdHelper.NextId();
            entity.CreatorTime = DateTime.Now;
            entity.CreatorUserId = _userManager.UserId;
            entity.ItemId = item;
            entity.ObjectId = objectId;
            entity.ItemType = itemType;
            entity.ObjectType = objectType;
            entity.SortCode = itemIds.IndexOf(item);
            list.Add(entity);
        }
    }

    /// <summary>
    /// 批量添加权限接口参数组装.
    /// </summary>
    /// <param name="list">返回参数.</param>
    /// <param name="ids">来源数据.</param>
    /// <param name="type">来源类型.</param>
    /// <param name="isData">是否是权限数据.</param>
    private void BatchAddAuthorizeEntity(ref List<AuthorizeEntity> list, List<string> ids, string type, bool isData)
    {
        if (ids != null && ids.Count != 0)
        {
            if (isData)
            {
                foreach (string? item in ids)
                {
                    AuthorizeEntity? entity = new AuthorizeEntity();
                    entity.ItemId = item;
                    entity.ItemType = type;
                    list.Add(entity);
                }
            }
            else
            {
                foreach (string? item in ids)
                {
                    AuthorizeEntity? entity = new AuthorizeEntity();
                    entity.ObjectId = item;
                    entity.ObjectType = type;
                    list.Add(entity);
                }
            }
        }
    }

    /// <summary>
    /// 保存批量权限.
    /// </summary>
    /// <param name="list">返回list.</param>
    /// <param name="objectList">对象数据.</param>
    /// <param name="authorizeList">权限数据.</param>
    private void SeveBatch(ref List<AuthorizeEntity> list, List<AuthorizeEntity> objectList, List<AuthorizeEntity> authorizeList)
    {
        foreach (AuthorizeEntity? objectItem in objectList)
        {
            foreach (AuthorizeEntity entityItem in authorizeList)
            {
                AuthorizeEntity? entity = new AuthorizeEntity();
                entity.Id = SnowflakeIdHelper.NextId();
                entity.CreatorTime = DateTime.Now;
                entity.CreatorUserId = _userManager.UserId;
                entity.ItemId = entityItem.ItemId;
                entity.ItemType = entityItem.ItemType;
                entity.ObjectId = objectItem.ObjectId;
                entity.ObjectType = objectItem.ObjectType;
                entity.SortCode = authorizeList.IndexOf(entityItem);
                list.Add(entity);
            }
        }
    }

    /// <summary>
    /// 返回参数处理.
    /// </summary>
    /// <param name="output">返回参数.</param>
    /// <param name="list">返回参数数据.</param>
    /// <param name="checkList">已勾选的id.</param>
    /// <param name="parentId"></param>
    private void GetOutPutResult(ref AuthorizeDataOutput output, List<AuthorizeDataModelOutput> list, List<string> checkList, string parentId = "-1")
    {
        output.all = list.Select(l => l.id).ToList();
        output.ids = checkList.Intersect(output.all).ToList();
        output.list = list.OrderBy(x => x.sortCode).ToList().ToTree(parentId);
    }

    /// <summary>
    /// 获取子节点菜单id.
    /// </summary>
    /// <param name="moduleEntitiesList"></param>
    /// <returns></returns>
    private List<string> GetChildNodesId(List<ModuleEntity> moduleEntitiesList)
    {
        List<string>? ids = moduleEntitiesList.Select(m => m.Id).ToList();
        List<string>? pids = moduleEntitiesList.Select(m => m.ParentId).ToList();
        List<string>? childNodesIds = ids.Where(x => !pids.Contains(x) && moduleEntitiesList.Find(m => m.Id == x).ParentId != "-1").ToList();
        return childNodesIds.Union(ids).ToList();
    }

    /// <summary>
    /// 过滤菜单权限数据.
    /// </summary>
    /// <param name="childNodesIds">其他权限数据菜单id集合.</param>
    /// <param name="moduleList">勾选菜单权限数据.</param>
    /// <param name="output">返回值.</param>
    private void GetParentsModuleList(List<string> childNodesIds, List<ModuleEntity> moduleList, ref List<AuthorizeDataModelOutput> output)
    {
        // 获取有其他权限的菜单末级节点id
        List<AuthorizeDataModelOutput>? authorizeModuleData = moduleList.Adapt<List<AuthorizeDataModelOutput>>();
        foreach (string? item in childNodesIds)
        {
            GteModuleListById(item, authorizeModuleData, output);
        }

        output = output.Distinct().ToList();
    }

    /// <summary>
    /// 根据菜单id递归获取authorizeDataOutputModel的父级菜单.
    /// </summary>
    /// <param name="id">菜单id.</param>
    /// <param name="authorizeDataOutputModel">选中菜单集合.</param>
    /// <param name="output">返回数据.</param>
    private void GteModuleListById(string id, List<AuthorizeDataModelOutput> authorizeDataOutputModel, List<AuthorizeDataModelOutput> output)
    {
        AuthorizeDataModelOutput? data = authorizeDataOutputModel.Find(l => l.id == id);
        if (data != null)
        {
            if (data.parentId != "-1")
            {
                if (!output.Contains(data)) output.Add(data);
                GteModuleListById(data.parentId, authorizeDataOutputModel, output);
            }
            else
            {
                if (!output.Contains(data)) output.Add(data);
            }
        }
    }

    /// <summary>
    /// 按钮权限.
    /// </summary>
    /// <param name="moduleList">选中的菜单.</param>
    /// <param name="moduleButtonList">所有的按钮.</param>
    /// <param name="childNodesIds"></param>
    /// <param name="checkList"></param>
    /// <returns></returns>
    private AuthorizeDataOutput GetButton(List<ModuleEntity> moduleList, List<ModuleButtonEntity> moduleButtonList, List<string> childNodesIds, List<string> checkList)
    {
        AuthorizeDataOutput? output = new AuthorizeDataOutput();
        List<ModuleButtonEntity>? buttonList = new List<ModuleButtonEntity>();
        childNodesIds.ForEach(ids =>
        {
            List<ModuleButtonEntity>? buttonEntity = moduleButtonList.FindAll(m => m.ModuleId == ids);
            if (buttonEntity.Count != 0)
            {
                buttonEntity.ForEach(bt =>
                {
                    bt.Icon = string.Empty;
                    if (bt.ParentId.Equals("-1"))
                    {
                        bt.ParentId = ids;
                    }
                });
                buttonList = buttonList.Union(buttonEntity).ToList();
            }
        });
        List<AuthorizeDataModelOutput>? authorizeDataButtonList = buttonList.Adapt<List<AuthorizeDataModelOutput>>();
        List<AuthorizeDataModelOutput>? authorizeDataModuleList = new List<AuthorizeDataModelOutput>();

        // 末级菜单id集合
        List<string>? moduleIds = buttonList.Select(b => b.ModuleId).ToList().Distinct().ToList();
        GetParentsModuleList(moduleIds, moduleList, ref authorizeDataModuleList);
        List<AuthorizeDataModelOutput>? list = authorizeDataModuleList.Union(authorizeDataButtonList).ToList();
        GetOutPutResult(ref output, list, checkList);
        return output;
    }

    /// <summary>
    /// 列表权限.
    /// </summary>
    /// <param name="moduleList">选中的菜单.</param>
    /// <param name="moduleColumnEntity">所有的列表.</param>
    /// <param name="childNodesIds"></param>
    /// <param name="checkList"></param>
    /// <returns></returns>
    private AuthorizeDataOutput GetColumn(List<ModuleEntity> moduleList, List<ModuleColumnEntity> moduleColumnEntity, List<string> childNodesIds, List<string> checkList)
    {
        AuthorizeDataOutput? output = new AuthorizeDataOutput();
        List<ModuleColumnEntity>? columnList = new List<ModuleColumnEntity>();
        childNodesIds.ForEach(ids =>
        {
            List<ModuleColumnEntity>? columnEntity = moduleColumnEntity.FindAll(m => m.ModuleId == ids);
            if (columnEntity.Count != 0)
            {
                columnEntity.ForEach(bt =>
                {
                    bt.ParentId = ids;
                });
                columnList = columnList.Union(columnEntity).ToList();
            }
        });
        List<AuthorizeDataModelOutput>? authorizeDataColumnList = columnList.Adapt<List<AuthorizeDataModelOutput>>();
        List<AuthorizeDataModelOutput>? authorizeDataModuleList = new List<AuthorizeDataModelOutput>();
        List<string>? moduleIds = columnList.Select(b => b.ModuleId).ToList().Distinct().ToList();
        GetParentsModuleList(moduleIds, moduleList, ref authorizeDataModuleList);
        List<AuthorizeDataModelOutput>? list = authorizeDataModuleList.Union(authorizeDataColumnList).ToList();
        GetOutPutResult(ref output, list, checkList);
        return output;
    }

    /// <summary>
    /// 表单权限.
    /// </summary>
    /// <returns></returns>
    private AuthorizeDataOutput GetForm(List<ModuleEntity> moduleList, List<ModuleFormEntity> moduleFormEntity, List<string> childNodesIds, List<string> checkList)
    {
        AuthorizeDataOutput? output = new AuthorizeDataOutput();
        List<ModuleFormEntity>? formList = new List<ModuleFormEntity>();
        childNodesIds.ForEach(ids =>
        {
            List<ModuleFormEntity>? formEntity = moduleFormEntity.FindAll(m => m.ModuleId == ids);
            if (formEntity.Count != 0)
            {
                formEntity.ForEach(bt =>
                {
                    bt.ParentId = ids;
                });
                formList = formList.Union(formEntity).ToList();
            }
        });
        List<AuthorizeDataModelOutput>? authorizeDataFormList = formList.Adapt<List<AuthorizeDataModelOutput>>();
        List<AuthorizeDataModelOutput>? authorizeDataModuleList = new List<AuthorizeDataModelOutput>();
        List<string>? moduleIds = formList.Select(b => b.ModuleId).ToList().Distinct().ToList();
        GetParentsModuleList(moduleIds, moduleList, ref authorizeDataModuleList);
        List<AuthorizeDataModelOutput>? list = authorizeDataModuleList.Union(authorizeDataFormList).ToList();
        GetOutPutResult(ref output, list, checkList);
        return output;
    }

    /// <summary>
    /// 数据权限.
    /// </summary>
    /// <param name="moduleList"></param>
    /// <param name="moduleResourceEntity"></param>
    /// <param name="childNodesIds"></param>
    /// <param name="checkList"></param>
    /// <returns></returns>
    private AuthorizeDataOutput GetResource(List<ModuleEntity> moduleList, List<ModuleDataAuthorizeSchemeEntity> moduleResourceEntity, List<string> childNodesIds, List<string> checkList)
    {
        List<string>? moduleIds = new List<string>();
        AuthorizeDataOutput? output = new AuthorizeDataOutput();
        List<AuthorizeDataModelOutput>? authorizeDataResourceList = new List<AuthorizeDataModelOutput>();
        childNodesIds.ForEach(ids =>
        {
            List<ModuleDataAuthorizeSchemeEntity>? resourceEntity = moduleResourceEntity.FindAll(m => m.ModuleId == ids);
            if (resourceEntity.Count != 0)
            {
                moduleIds.Add(ids);
                List<AuthorizeDataModelOutput>? entity = resourceEntity.Adapt<List<AuthorizeDataModelOutput>>();

                entity.ForEach(e => e.parentId = ids);
                authorizeDataResourceList = authorizeDataResourceList.Union(entity).ToList();
            }
        });
        List<AuthorizeDataModelOutput>? authorizeDataModuleList = new List<AuthorizeDataModelOutput>();
        GetParentsModuleList(moduleIds, moduleList, ref authorizeDataModuleList);
        List<AuthorizeDataModelOutput>? list = authorizeDataModuleList.Union(authorizeDataResourceList).ToList();
        GetOutPutResult(ref output, list, checkList);
        return output;
    }

    /// <summary>
    /// 强制权限组下的所有用户刷新页面.
    /// </summary>
    /// <param name="permissionGroupId">权限组Id.</param>
    /// <returns></returns>
    private async Task ForcedRefresh(List<string> permissionGroupId)
    {
        var entity = await _authorizeRepository.AsSugarClient().Queryable<PermissionGroupEntity>().FirstAsync(p => permissionGroupId.Contains(p.Id) && p.DeleteMark == null);
        var pIds = entity.PermissionMember?.Split(',').ToList();
        var pIdList = new List<string>();
        pIds?.ForEach(item => pIdList.Add(item.Split("--").FirstOrDefault()));

        // 查找该权限组下的所有成员id
        var userIds = await _authorizeRepository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => pIdList.Contains(x.ObjectId) || pIdList.Contains(x.UserId)).Select(x => x.UserId).ToListAsync();

        var onlineCacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, _userManager.TenantId);
        var list = await _cacheManager.GetAsync<List<UserOnlineModel>>(onlineCacheKey);

        if (userIds.Any())
        {
            // 过滤超管和分管用户,权限组的权限变动，超管和分管不受影响（即不被退出）
            var oIds = list.Select(x => x.userId).ToList();
            var uIdList = _authorizeRepository.AsSugarClient().Queryable<UserEntity>().Where(x => oIds.Contains(x.Id) && x.IsAdministrator.Equals(0)).Select(x => x.Id).ToList();
            var orgAdminIds = _authorizeRepository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Select(x => x.UserId).ToList();
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
    #endregion

    #region PublicMethod

    /// <summary>
    /// 当前用户模块权限.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <param name="systemIds">当前系统Ids .</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<ModuleEntity>> GetCurrentUserModuleAuthorize(string userId, string[] systemIds)
    {
        return await _authorizeRepository.AsSugarClient().Queryable<ModuleEntity>().Where(a => a.EnabledMark == 1 && a.DeleteMark == null)
                .Where(a => systemIds.Contains(a.SystemId)).OrderBy(o => o.SortCode).ToListAsync();
    }

    /// <summary>
    /// 当前用户模块按钮权限.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<ModuleButtonEntity>> GetCurrentUserButtonAuthorize(string userId)
    {
        return await _authorizeRepository.AsSugarClient().Queryable<ModuleButtonEntity>().Where(a => a.EnabledMark == 1 && a.DeleteMark == null).OrderBy(o => o.SortCode)
                .Mapper(a =>
                {
                    a.ParentId = a.ParentId.Equals("-1") ? a.ModuleId : a.ParentId;
                }).ToListAsync();
    }

    /// <summary>
    /// 当前用户模块列权限.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<ModuleColumnEntity>> GetCurrentUserColumnAuthorize(string userId)
    {
        return await _authorizeRepository.AsSugarClient().Queryable<ModuleColumnEntity>().Where(a => a.EnabledMark == 1 && a.DeleteMark == null).OrderBy(o => o.SortCode)
                .Mapper(a =>
                {
                    a.ParentId = a.ModuleId;
                }).ToListAsync();
    }

    /// <summary>
    /// 当前用户模块表单权限.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<ModuleFormEntity>> GetCurrentUserFormAuthorize(string userId)
    {
        return await _authorizeRepository.AsSugarClient().Queryable<ModuleFormEntity>().Where(a => a.EnabledMark == 1 && a.DeleteMark == null).OrderBy(o => o.SortCode)
                .Mapper(a =>
                {
                    a.ParentId = a.ModuleId;
                }).ToListAsync();
    }

    /// <summary>
    /// 当前用户模块权限资源.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <param name="isAdmin">是否超管.</param>
    /// <param name="roleIds">用户角色Ids.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<ModuleDataAuthorizeSchemeEntity>> GetCurrentUserResourceAuthorize(string userId)
    {
        return await _authorizeRepository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().Where(a => a.EnabledMark == 1 && a.DeleteMark == null).OrderBy(o => o.SortCode).ToListAsync();
    }

    /// <summary>
    /// 获取权限项ids.
    /// </summary>
    /// <param name="roleId">角色id.</param>
    /// <param name="itemType">项类型.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<string>> GetAuthorizeItemIds(string roleId, string itemType)
    {
        var data = await _authorizeRepository.AsQueryable().Where(a => a.ObjectId == roleId && a.ItemType == itemType).GroupBy(it => new { it.ItemId }).Select(it => new { it.ItemId }).ToListAsync();
        return data.Select(it => it.ItemId).ToList();
    }

    /// <summary>
    /// 是否存在权限资源.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<bool> GetIsExistModuleDataAuthorizeScheme(string[] ids)
    {
        return await _authorizeRepository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().AnyAsync(m => ids.Contains(m.Id) && m.DeleteMark == null);
    }

    /// <summary>
    /// 获取权限列表.
    /// </summary>
    /// <param name="objectId">对象主键.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<AuthorizeEntity>> GetAuthorizeListByObjectId(string objectId)
    {
        return await _authorizeRepository.AsQueryable().Where(a => a.ObjectId == objectId).ToListAsync();
    }

    /// <summary>
    /// 处理菜单+系统.
    /// </summary>
    /// <param name="moduleList"></param>
    /// <param name="systemList"></param>
    /// <returns></returns>
    public async Task<List<ModuleEntity>> GetModuleAndSystemScheme(List<ModuleEntity> moduleList, List<ModuleEntity> systemList)
    {
        var moduleSystemList = systemList.Where(x => x.SystemId != null && x.SystemId.Equals("-1")).ToList();

        moduleSystemList.ForEach(item =>
        {
            if (moduleList.Any(it => it.Category != null && it.Category.Equals("Web") && it.SystemId.Equals(item.Id)))
            {
                var webMenu = systemList.FirstOrDefault(it => it.ParentId.Equals(item.Id) && it.Category.Equals("Web"));
                moduleList.Where(it => it.Category != null && it.Category.Equals("Web") && it.ParentId.Equals("-1") && it.SystemId.Equals(item.Id)).ToList().ForEach(it =>
                {
                    it.ParentId = webMenu.Id;
                });
                moduleList.Add(webMenu);
            }
            if (moduleList.Any(it => it.Category != null && it.Category.Equals("App") && it.SystemId.Equals(item.Id)))
            {
                var appMenu = systemList.FirstOrDefault(it => it.ParentId.Equals(item.Id) && it.Category.Equals("App"));
                moduleList.Where(it => it.Category != null && it.Category.Equals("App") && it.ParentId.Equals("-1") && it.SystemId.Equals(item.Id)).ToList().ForEach(it =>
                {
                    it.ParentId = appMenu.Id;
                });
                moduleList.Add(appMenu);
            }
        });

        moduleList.Where(x => x.ParentId.Equals("-1")).ToList().ForEach(item => item.ParentId = item.SystemId);
        moduleList.AddRange(moduleSystemList);

        return moduleList;
    }

    /// <summary>
    /// 处理app菜单 勾选问题，返回最终结果.
    /// </summary>
    /// <param name="output"></param>
    /// <returns></returns>
    public AuthorizeDataOutput GetResult(AuthorizeDataOutput output, List<string> noAuthList)
    {
        if (output.list.Any())
        {
            output.list.ForEach(item =>
            {
                if (noAuthList.Contains(item.id)) item.disabled = true;
                var appItem = item.children?.Adapt<List<AuthorizeDataModelOutput>>().FirstOrDefault(x => !output.ids.Contains(x.id) && x.fullName.Equals("app菜单"));
                if (appItem != null)
                {
                    foreach (var it in output.ids)
                    {
                        if (appItem.ToJsonString().Contains(it))
                        {
                            output.ids.Add(appItem.id);
                            break;
                        }
                    }
                }
            });
            output.list.Where(x => x.disabled).ToList().ForEach(item =>
            {
                if (item.children != null && item.children.Any()) item.children = SetDisabled(item.children, output.ids);
            });
        }

        return output;
    }
    private List<object> SetDisabled(List<object> data, List<string> checkIds)
    {
        if (data != null && data.Any())
        {
            var newData = data.ToObject<List<AuthorizeDataModelOutput>>().Where(x => checkIds.Contains(x.id));
            foreach (var item in newData)
            {
                item.disabled = true;
                if (item.children != null && item.children.Any()) item.children = SetDisabled(item.children, checkIds);
            }

            data = newData.ToObject<List<object>>();
        }

        return data;
    }

    /// <summary>
    /// 获取在线用户列表.
    /// </summary>
    /// <param name="tenantId">租户ID.</param>
    /// <returns></returns>
    public async Task<List<UserOnlineModel>> GetOnlineUserList(string tenantId)
    {
        var cacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, tenantId);
        return await _cacheManager.GetAsync<List<UserOnlineModel>>(cacheKey);
    }

    /// <summary>
    /// 删除在线用户ID.
    /// </summary>
    /// <param name="tenantId">租户ID.</param>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    private async Task<bool> DelOnlineUser(string tenantId, string userId)
    {
        var cacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, tenantId);
        var list = await _cacheManager.GetAsync<List<UserOnlineModel>>(cacheKey);
        var online = list.Find(it => it.userId == userId);
        list.RemoveAll((x) => x.connectionId == online.connectionId);
        return await _cacheManager.SetAsync(cacheKey, list);
    }

    /// <summary>
    /// 删除用户登录信息缓存.
    /// </summary>
    /// <param name="tenantId">租户ID.</param>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    private async Task<bool> DelUserInfo(string tenantId, string userId)
    {
        var cacheKey = string.Format("{0}:{1}:{2}", tenantId, CommonConst.CACHEKEYUSER, userId);
        return await _cacheManager.DelAsync(cacheKey);
    }

    #endregion
}