using JNPF.Apps.Entitys;
using JNPF.Apps.Entitys.Dto;
using JNPF.Extensions;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using JNPF.FriendlyException;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Module;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 菜单管理
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "Menu", Order = 212)]
[Route("api/system/[controller]")]
public class ModuleService : IModuleService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 系统功能表仓储.
    /// </summary>
    private readonly ISqlSugarRepository<ModuleEntity> _repository;

    /// <summary>
    /// 功能按钮服务.
    /// </summary>
    private readonly IModuleButtonService _moduleButtonService;

    /// <summary>
    /// 功能列表服务.
    /// </summary>
    private readonly IModuleColumnService _moduleColumnService;

    /// <summary>
    /// 功能数据资源服务.
    /// </summary>
    private readonly IModuleDataAuthorizeSchemeService _moduleDataAuthorizeSchemeService;

    /// <summary>
    /// 功能数据方案服务.
    /// </summary>
    private readonly IModuleDataAuthorizeService _moduleDataAuthorizeSerive;

    /// <summary>
    /// 功能表单服务.
    /// </summary>
    private readonly IModuleFormService _moduleFormSerive;

    /// <summary>
    /// 文件服务.
    /// </summary>
    private readonly IFileManager _fileManager;

    /// <summary>
    /// 用户管理器.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="ModuleService"/>类型的新实例.
    /// </summary>
    public ModuleService(
        ISqlSugarRepository<ModuleEntity> repository,
        IFileManager fileManager,
        ModuleButtonService moduleButtonService,
        IModuleColumnService moduleColumnService,
        IModuleFormService moduleFormSerive,
        IModuleDataAuthorizeService moduleDataAuthorizeSerive,
        IModuleDataAuthorizeSchemeService moduleDataAuthorizeSchemeService,
        IUserManager userManager)
    {
        _repository = repository;
        _fileManager = fileManager;
        _moduleButtonService = moduleButtonService;
        _moduleColumnService = moduleColumnService;
        _moduleFormSerive = moduleFormSerive;
        _moduleDataAuthorizeSchemeService = moduleDataAuthorizeSchemeService;
        _moduleDataAuthorizeSerive = moduleDataAuthorizeSerive;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 获取菜单列表.
    /// </summary>
    /// <param name="systemId">模块ID.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("ModuleBySystem/{systemId}")]
    public async Task<dynamic> GetList(string systemId, [FromQuery] ModuleListQuery input)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        var authorIds = new List<string>();
        if (_repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Any(x => x.UserId.Equals(_userManager.UserId)) && (_userManager.UserOrigin ?? "pc").Equals("pc") && (_userManager.DataScope?.Any(x => x.organizeType.IsNotEmptyOrNull()) == true))
            authorIds = _userManager.DataScope.Where(x => x.organizeType.IsNotEmptyOrNull()).Select(x => x.organizeId).ToList();
        else
            authorIds = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(x => x.ItemType.Equals("module") && x.ObjectType.Equals("Role") && _userManager.PermissionGroup.Contains(x.ObjectId)).Select(x => x.ItemId).ToListAsync();

        var data = await GetList(systemId);
        data.RemoveAll(it => it.EnCode.Equals("workFlow"));

        if (!_userManager.IsAdministrator)
            data = data.FindAll(x => authorIds.Contains(x.Id));
        if (!string.IsNullOrEmpty(input.category))
            data = data.FindAll(x => x.Category == input.category);
        if (ignoreIds.IsNotEmptyOrNull() && ignoreIds.Any())
            data = data.FindAll(x => !ignoreIds.Contains(x.Id));
        if (ignoreUrls.IsNotEmptyOrNull() && ignoreUrls.Any())
            data = data.FindAll(x => !ignoreUrls.Contains(x.UrlAddress));
        if (!string.IsNullOrEmpty(input.keyword))
            data = data.TreeWhere(t => t.FullName.Contains(input.keyword) || t.EnCode.Contains(input.keyword) || (t.UrlAddress.IsNotEmptyOrNull() && t.UrlAddress.Contains(input.keyword)), t => t.Id, t => t.ParentId);
        if (input.type.IsNotEmptyOrNull())
            data = data.TreeWhere(x => x.Type.Equals(input.type), t => t.Id, t => t.ParentId);
        if (input.enabledMark.IsNotEmptyOrNull())
            data = data.TreeWhere(x => x.EnabledMark.Equals(input.enabledMark), t => t.Id, t => t.ParentId);

        var treeList = data.Adapt<List<ModuleListOutput>>();
        return new { list = treeList.ToTree("-1") };
    }

    /// <summary>
    /// 获取菜单下拉框.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="category">菜单分类（参数有Web,App），默认显示所有分类.</param>
    /// <returns></returns>
    [HttpGet("Selector/{id}")]
    public async Task<dynamic> GetSelector(string id, string category)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        var data = await GetList(_userManager.User.SystemId);
        if (!string.IsNullOrEmpty(category))
            data = data.FindAll(x => x.Category == category && (x.Type == 1 || x.Type == 0));
        if (!id.Equals("0"))
            data.RemoveAll(x => x.Id == id);
        if (ignoreIds.IsNotEmptyOrNull() && ignoreIds.Any())
            data = data.FindAll(x => !ignoreIds.Contains(x.Id));
        if (ignoreUrls.IsNotEmptyOrNull() && ignoreUrls.Any())
            data = data.FindAll(x => !ignoreUrls.Contains(x.UrlAddress));
        var treeList = data.Where(x => x.EnabledMark.Equals(1)).ToList().Adapt<List<ModuleSelectorOutput>>();
        return new { list = treeList.ToTree("-1") };
    }

    /// <summary>
    /// 获取菜单下拉框.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <param name="systemId">模块ID.</param>
    /// <param name="category">菜单分类（参数有Web,App），默认显示所有分类.</param>
    /// <returns></returns>
    [HttpGet("Selector/{id}/{systemId}")]
    public async Task<dynamic> GetSelector(string id, string systemId, string category)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        var data = await GetList(systemId);
        data.RemoveAll(it => it.EnCode.Equals("mainSystem"));
        data.RemoveAll(it => it.EnCode.Equals("workFlow"));
        if (!string.IsNullOrEmpty(category))
            data = data.FindAll(x => x.Category == category && (x.Type == 1 || x.Type == 0));
        if (!id.Equals("0"))
            data.RemoveAll(x => x.Id == id);
        if (ignoreIds.IsNotEmptyOrNull() && ignoreIds.Any())
            data = data.FindAll(x => !ignoreIds.Contains(x.Id));
        if (ignoreUrls.IsNotEmptyOrNull() && ignoreUrls.Any())
            data = data.FindAll(x => !ignoreUrls.Contains(x.UrlAddress));
        var treeList = data.Where(x => x.EnabledMark.Equals(1)).ToList().Adapt<List<ModuleSelectorOutput>>();
        return new { list = treeList.ToTree("-1") };
    }

    /// <summary>
    /// 获取菜单列表（下拉框）.
    /// </summary>
    /// <param name="category">菜单分类（参数有Web,App）.</param>
    /// <returns></returns>
    [HttpGet("Selector/All")]
    public async Task<dynamic> GetSelectorAll(string category)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        var data = await _repository.AsQueryable()
            .Where(x => x.DeleteMark == null)
            .WhereIF(ignoreIds.IsNotEmptyOrNull() && ignoreIds.Any(), x => !ignoreIds.Contains(x.Id))
            .WhereIF(ignoreUrls.IsNotEmptyOrNull() && ignoreUrls.Any(), x => !ignoreUrls.Contains(x.UrlAddress))
            .OrderBy(o => o.SortCode).ToListAsync();
        if (!string.IsNullOrEmpty(category))
            data = data.FindAll(x => x.Category == category);

        var systemList = await _repository.AsSugarClient().Queryable<SystemEntity>()
            .Where(x => x.DeleteMark == null && x.EnabledMark == 1)
            .WhereIF(ignoreIds.IsNotEmptyOrNull() && ignoreIds.Any(), x => !ignoreIds.Contains(x.Id))
            .ToListAsync();

        var treeList = data.Adapt<List<ModuleSelectorAllOutput>>();
        foreach (var item in treeList)
        {
            if (item.type == "1")
            {
                item.hasModule = false;
            }

            if (item.parentId == "-1")
            {
                item.parentId = item.systemId;
            }
        }
        treeList = treeList.Union(systemList.Select(x => new ModuleSelectorAllOutput()
        {
            id = x.Id,
            parentId = "0",
            fullName = x.FullName,
            icon = x.Icon,
            type = "0",
            hasModule = false
        })).ToList();
        return new { list = treeList.ToTree() };
    }

    /// <summary>
    /// 获取菜单发布下拉框（功能设计、门户设计）.
    /// </summary>
    /// <returns></returns>
    [HttpGet("SelectorFilter/{moduleId}")]
    public async Task<dynamic> GetSelectorFilter(string moduleId, string category)
    {
        // 根据多租户返回结果moduleIdList :[菜单id] 过滤应用菜单
        var ignoreIds = _userManager.TenantIgnoreModuleIdList;
        var ignoreUrls = _userManager.TenantIgnoreUrlAddressList;

        var data = await GetList(_userManager.BizSystemId);
        //data.RemoveAll(it => it.EnCode.Equals("mainSystem"));
        data.RemoveAll(it => it.EnCode.Equals("workFlow"));
        //data = data.Where(aa => aa.SystemId == _userManager.BizSystemId);
        data = data.FindAll(x => x.EnabledMark.Equals(1) && x.Category == category && (x.Type == 1 || x.Type == 0));
        if (ignoreIds.IsNotEmptyOrNull() && ignoreIds.Any())
            data = data.FindAll(x => !ignoreIds.Contains(x.Id));
        if (ignoreUrls.IsNotEmptyOrNull() && ignoreUrls.Any())
            data = data.FindAll(x => !ignoreUrls.Contains(x.UrlAddress));

        var treeList = data.Adapt<List<ModuleSelectorFilterOutput>>();

        if (category == "App")
        {
            foreach (var item in treeList.FindAll(it => it.parentId.Equals("-1")))
                item.disabled = true;
        }

        // 已发布的菜单
        var moduleList = await _repository.AsQueryable().Where(it => it.DeleteMark == null && it.Category.Equals(category) && it.PropertyJson.Contains(moduleId)).ToListAsync();
        foreach (var item in moduleList)
        {
            if (item.ParentId.Equals("-1"))
            {
                var sys = treeList.Find(it => it.id.Equals(item.SystemId));
                if (sys.IsNotEmptyOrNull()) sys.disabled = true;
            }
            else
            {
                var mod = treeList.Find(it => it.id.Equals(item.ParentId));
                if (mod.IsNotEmptyOrNull()) mod.disabled = true;
            }
        }

        return new { list = treeList.ToTree("-1") };
    }

    /// <summary>
    /// 获取菜单信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo_Api(string id)
    {
        var data = await GetInfo(id);
        return data.Adapt<ModuleInfoOutput>();
    }

    /// <summary>
    /// 导出.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}/Actions/Export")]
    public async Task<dynamic> ActionsExport(string id)
    {
        var data = (await GetInfo(id)).Adapt<ModuleExportInput>();
        data.buttonEntityList = (await _moduleButtonService.GetList(id)).Adapt<List<ButtonEntityListItem>>();
        data.columnEntityList = (await _moduleColumnService.GetList(id)).Adapt<List<ColumnEntityListItem>>();
        data.authorizeEntityList = (await _moduleDataAuthorizeSerive.GetList(id)).Adapt<List<AuthorizeEntityListItem>>();
        data.schemeEntityList = (await _moduleDataAuthorizeSchemeService.GetList(id)).Adapt<List<SchemeEntityListItem>>();
        data.formEntityList = (await _moduleFormSerive.GetList(id)).Adapt<List<FromEntityListItem>>();
        var jsonStr = data.ToJsonString();
        return await _fileManager.Export(jsonStr, data.fullName, ExportFileType.bm);
    }

    /// <summary>
    /// 获取菜单权限返回权限组.
    /// </summary>
    /// <param name="id">主键id</param>
    /// <returns></returns>
    [HttpGet("getPermissionGroup/{id}")]
    public async Task<dynamic> GetPermissionGroup(string id)
    {
        // 获取当前菜单开启了哪些权限
        var entity = await GetInfo(id);
        if (entity.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.COM1005);

        // 权限：0-未开启、1-有权限、2-无权限
        var type = 0;
        var buttonAuthList = await _repository.AsSugarClient().Queryable<ModuleButtonEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .ToListAsync();
        var columnAuthList = await _repository.AsSugarClient().Queryable<ModuleColumnEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .ToListAsync();
        var fromAuthList = await _repository.AsSugarClient().Queryable<ModuleFormEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .ToListAsync();
        var dataAuthList = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .ToListAsync();

        var list = new List<ModulePermissionGroupListOutput>();
        if (buttonAuthList.Count > 0 || columnAuthList.Count > 0 || fromAuthList.Count > 0 || dataAuthList.Count > 0)
        {
            var allAuthList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
                .Where(it => it.ItemType.Equals("module") && it.ItemId.Equals(id))
                .Select(it => it.ObjectId)
                .ToListAsync();

            list = await _repository.AsSugarClient().Queryable<PermissionGroupEntity>()
                .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && allAuthList.Contains(it.Id))
                .Select(it => new ModulePermissionGroupListOutput
                {
                    id = it.Id,
                    fullName = it.FullName,
                    enCode = it.EnCode,
                    icon = "icon-ym icon-ym-authGroup"
                })
                .ToListAsync();

            if (list.Count > 0)
                type = 1;
            else
                type = 2;
        }

        return new { list = list, type = type };
    }

    /// <summary>
    /// 获取菜单权限返回权限组内容.
    /// </summary>
    /// <param name="id">主键id</param>
    /// <param name="permissionId">权限组id</param>
    /// <returns></returns>
    [HttpGet("getPermission/{id}/{permissionId}")]
    public async Task<dynamic> GetPermission(string id, string permissionId)
    {
        var modulePermission = new modulePermissionOutput();

        // 获取当前菜单开启了哪些权限
        var entity = await GetInfo(id);
        if (entity.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.COM1005);

        var permissionGroupEntity = await _repository.AsSugarClient().Queryable<PermissionGroupEntity>()
            .Where(it => it.DeleteMark == null && it.Id.Equals(permissionId))
            .FirstAsync();
        if (permissionGroupEntity.IsNullOrEmpty())
            throw Oops.Oh(ErrorCode.COM1005);

        // 权限组的权限
        var allAuthList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>()
            .Where(it => it.ObjectId.Equals(permissionId))
            .ToListAsync();

        var permissionMemberModel = new ModulePermissionModel();
        permissionMemberModel.fullName = "权限成员";
        permissionMemberModel.type = 1;
        if (permissionGroupEntity.IsNotEmptyOrNull())
        {
            var list = new List<ModulePermissionBaseModel>();
            if (permissionGroupEntity.PermissionMember.IsNotEmptyOrNull())
            {
                foreach (var item in permissionGroupEntity.PermissionMember.Split(",").ToList())
                {
                    var itemId = item.Split("--").FirstOrDefault();
                    var itemType = item.Split("--").LastOrDefault();
                    switch (itemType)
                    {
                        case "company":
                        case "department":
                            {
                                var data = await _repository.AsSugarClient().Queryable<OrganizeEntity>()
                                    .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.Id.Equals(itemId))
                                    .Select(it => new ModulePermissionBaseModel
                                    {
                                        id = it.Id,
                                        fullName = it.FullName
                                    })
                                    .FirstAsync();
                                if (data.IsNotEmptyOrNull()) list.Add(data);
                            }
                            break;
                        case "position":
                            {
                                var data = await _repository.AsSugarClient().Queryable<PositionEntity>()
                                    .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.Id.Equals(itemId))
                                    .Select(it => new ModulePermissionBaseModel
                                    {
                                        id = it.Id,
                                        fullName = it.FullName
                                    })
                                    .FirstAsync();
                                if (data.IsNotEmptyOrNull()) list.Add(data);
                            }
                            break;
                        case "role":
                            {
                                var data = await _repository.AsSugarClient().Queryable<RoleEntity>()
                                    .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.Id.Equals(itemId))
                                    .Select(it => new ModulePermissionBaseModel
                                    {
                                        id = it.Id,
                                        fullName = it.FullName
                                    })
                                    .FirstAsync();
                                if (data.IsNotEmptyOrNull()) list.Add(data);
                            }
                            break;
                        case "user":
                            {
                                var data = await _repository.AsSugarClient().Queryable<UserEntity>()
                                    .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.Id.Equals(itemId))
                                    .Select(it => new ModulePermissionBaseModel
                                    {
                                        id = it.Id,
                                        fullName = SqlFunc.MergeString(it.RealName, "/", it.Account)
                                    })
                                    .FirstAsync();
                                if (data.IsNotEmptyOrNull()) list.Add(data);
                            }
                            break;
                        case "group":
                            {
                                var data = await _repository.AsSugarClient().Queryable<GroupEntity>()
                                    .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.Id.Equals(itemId))
                                    .Select(it => new ModulePermissionBaseModel
                                    {
                                        id = it.Id,
                                        fullName = it.FullName
                                    })
                                    .FirstAsync();
                                if (data.IsNotEmptyOrNull()) list.Add(data);
                            }
                            break;
                    }
                }
            }
            permissionMemberModel.list = list;
        }
        modulePermission.permissionMember = permissionMemberModel;

        var moduleButtonPermissionModel = new ModulePermissionModel();
        moduleButtonPermissionModel.fullName = "按钮权限";
        var buttonAuthList = await _repository.AsSugarClient().Queryable<ModuleButtonEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .Select(it => new ModulePermissionBaseModel
            {
                id = it.Id,
                fullName = it.FullName
            })
            .ToListAsync();
        if (buttonAuthList.Count > 0)
        {
            moduleButtonPermissionModel.type = 2;
            moduleButtonPermissionModel.list = buttonAuthList.Where(it => allAuthList.Select(x => x.ItemId).Contains(it.id)).ToList();
            if (moduleButtonPermissionModel.list.Count > 0)
                moduleButtonPermissionModel.type = 1;
        }
        modulePermission.buttonAuthorize = moduleButtonPermissionModel;

        var moduleColumnPermissionModel = new ModulePermissionModel();
        moduleColumnPermissionModel.fullName = "列表权限";
        var columnAuthList = await _repository.AsSugarClient().Queryable<ModuleColumnEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .Select(it => new ModulePermissionBaseModel
            {
                id = it.Id,
                fullName = it.FullName
            })
            .ToListAsync();
        if (columnAuthList.Count > 0)
        {
            moduleColumnPermissionModel.type = 2;
            moduleColumnPermissionModel.list = columnAuthList.Where(it => allAuthList.Select(x => x.ItemId).Contains(it.id)).ToList();
            if (moduleColumnPermissionModel.list.Count > 0)
                moduleColumnPermissionModel.type = 1;
        }
        modulePermission.columnAuthorize = moduleColumnPermissionModel;

        var moduleFromPermissionModel = new ModulePermissionModel();
        moduleFromPermissionModel.fullName = "表单权限";
        var fromAuthList = await _repository.AsSugarClient().Queryable<ModuleFormEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .Select(it => new ModulePermissionBaseModel
            {
                id = it.Id,
                fullName = it.FullName
            })
            .ToListAsync();
        if (fromAuthList.Count > 0)
        {
            moduleFromPermissionModel.type = 2;
            moduleFromPermissionModel.list = fromAuthList.Where(it => allAuthList.Select(x => x.ItemId).Contains(it.id)).ToList();
            if (moduleFromPermissionModel.list.Count > 0)
                moduleFromPermissionModel.type = 1;
        }
        modulePermission.formAuthorize = moduleFromPermissionModel;

        var moduleDataPermissionModel = new ModulePermissionModel();
        moduleDataPermissionModel.fullName = "数据权限";
        var dataAuthList = await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>()
            .Where(it => it.DeleteMark == null && it.EnabledMark.Equals(1) && it.ModuleId.Equals(id))
            .Select(it => new ModulePermissionBaseModel
            {
                id = it.Id,
                fullName = it.FullName
            })
            .ToListAsync();
        if (dataAuthList.Count > 0)
        {
            moduleDataPermissionModel.type = 2;
            moduleDataPermissionModel.list = dataAuthList.Where(it => allAuthList.Select(x => x.ItemId).Contains(it.id)).ToList();
            if (moduleDataPermissionModel.list.Count > 0)
                moduleDataPermissionModel.type = 1;
        }
        modulePermission.dataAuthorize = moduleDataPermissionModel;

        return modulePermission;
    }

    #endregion

    #region Post

    /// <summary>
    /// 添加菜单.
    /// </summary>
    /// <param name="input">请求参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    [UnitOfWork]
    public async Task Creater([FromBody] ModuleCrInput input)
    {
        if (await _repository.IsAnyAsync(x => x.EnCode == input.enCode && x.DeleteMark == null && x.Category == input.category))
            throw Oops.Oh(ErrorCode.COM1025);
        if (await _repository.IsAnyAsync(x => x.FullName == input.fullName && x.DeleteMark == null && x.Category == input.category && input.parentId == x.ParentId && x.SystemId == input.systemId))
            throw Oops.Oh(ErrorCode.COM1024);
        var entity = input.Adapt<ModuleEntity>();

        // 添加字典菜单按钮
        if (entity.Type == 4)
        {
            foreach (var item in await _moduleButtonService.GetList())
            {
                if (item.ModuleId == "-1")
                {
                    item.ModuleId = entity.Id;
                    await _moduleButtonService.Create(item);
                }
            }
        }

        var isOk = await _repository.AsInsertable(entity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        if (isOk < 1)
            throw Oops.Oh(ErrorCode.COM1000);
    }

    /// <summary>
    /// 修改菜单.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] ModuleUpInput input)
    {
        var info = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (await _repository.IsAnyAsync(x => x.Id != id && x.EnCode == input.enCode && x.DeleteMark == null && x.Category == input.category))
            throw Oops.Oh(ErrorCode.COM1004);
        if (await _repository.IsAnyAsync(x => x.Id != id && (x.FullName == input.fullName) && x.DeleteMark == null && x.Category == input.category && input.parentId == x.ParentId && x.SystemId == input.systemId))
            throw Oops.Oh(ErrorCode.COM1004);
        if (await _repository.IsAnyAsync(x => x.ParentId == id && x.DeleteMark == null && x.SystemId == input.systemId) && info.Type != input.type)
            throw Oops.Oh(ErrorCode.D4008);
        var entity = input.Adapt<ModuleEntity>();
        if (entity.Category.Equals("App"))
        {
            var appData = entity.Adapt<AppDataListAllOutput>();
            appData.isData = _repository.AsSugarClient().Queryable<AppDataEntity>().Any(x => x.ObjectType == "2" && x.CreatorUserId == _userManager.UserId && x.ObjectId == appData.id && x.DeleteMark == null);
            var objData = appData.ToJsonString();
            await _repository.AsSugarClient().Updateable<AppDataEntity>().SetColumns(it => new AppDataEntity()
            {
                ObjectData = objData
            }).Where(it => it.ObjectId == id).ExecuteCommandAsync();
        }
        var isOk = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1001);
    }

    /// <summary>
    /// 删除菜单.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [UnitOfWork]
    public async Task Delete(string id)
    {
        var entity = await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
        if (entity == null || await _repository.IsAnyAsync(x => x.ParentId == id && x.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D1039);

        try
        {
            // 删除app菜单的常用功能
            if (entity.Category.Equals("App"))
            {
                await _repository.AsSugarClient().Updateable<AppDataEntity>().SetColumns(it => new AppDataEntity()
                {
                    DeleteMark = 1,
                    DeleteUserId = _userManager.UserId,
                    DeleteTime = SqlFunc.GetDate()
                }).Where(it => it.ObjectId == id && it.CreatorUserId == _userManager.UserId).ExecuteCommandAsync();
            }

            await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandHasChangeAsync();

            // 删除菜单相关权限
            await _repository.AsSugarClient().Updateable<ModuleButtonEntity>().SetColumns(it => new ModuleButtonEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).Where(it => it.ModuleId == id && it.DeleteMark == null).ExecuteCommandAsync();
            await _repository.AsSugarClient().Updateable<ModuleColumnEntity>().SetColumns(it => new ModuleColumnEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).Where(it => it.ModuleId == id && it.DeleteMark == null).ExecuteCommandAsync();
            await _repository.AsSugarClient().Updateable<ModuleFormEntity>().SetColumns(it => new ModuleFormEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).Where(it => it.ModuleId == id && it.DeleteMark == null).ExecuteCommandAsync();
            await _repository.AsSugarClient().Updateable<ModuleDataAuthorizeEntity>().SetColumns(it => new ModuleDataAuthorizeEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).Where(it => it.ModuleId == id && it.DeleteMark == null).ExecuteCommandAsync();
            await _repository.AsSugarClient().Updateable<ModuleDataAuthorizeSchemeEntity>().SetColumns(it => new ModuleDataAuthorizeSchemeEntity()
            {
                DeleteMark = 1,
                DeleteUserId = _userManager.UserId,
                DeleteTime = SqlFunc.GetDate()
            }).Where(it => it.ModuleId == id && it.DeleteMark == null).ExecuteCommandAsync();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.COM1002);
        }
    }

    /// <summary>
    /// 更新菜单状态.
    /// </summary>
    /// <param name="id">菜单id.</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/State")]
    public async Task ActionsState(string id)
    {
        var isOk = await _repository.AsUpdateable().SetColumns(it => new ModuleEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 1, 0, 1),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandHasChangeAsync();
        if (!isOk)
            throw Oops.Oh(ErrorCode.COM1003);
    }

    /// <summary>
    /// 导入.
    /// </summary>
    /// <param name="systemId"></param>
    /// <param name="file"></param>
    /// <param name="parentId"></param>
    /// <param name="category"></param>
    /// <param name="type">识别重复（0：跳过，1：追加）.</param>
    /// <returns></returns>
    [HttpPost("{systemId}/Actions/Import")]
    [UnitOfWork]
    public async Task ActionsImport(string systemId, IFormFile file, string parentId, string category, int type)
    {
        var fileType = Path.GetExtension(file.FileName).Replace(".", string.Empty);
        if (!fileType.ToLower().Equals(ExportFileType.bm.ToString()))
            throw Oops.Oh(ErrorCode.D3006);
        var josn = _fileManager.Import(file);
        ModuleExportInput? moduleModel;
        try
        {
            moduleModel = josn.ToObject<ModuleExportInput>();
        }
        catch
        {
            throw Oops.Oh(ErrorCode.D3006);
        }
        if (moduleModel == null) throw Oops.Oh(ErrorCode.D3006);

        var errorMsgList = new List<string>();
        var errorList = new List<string>();
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(moduleModel.id))) errorList.Add("ID");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Category.Equals(category) && it.EnCode.Equals(moduleModel.enCode))) errorList.Add("编码");
        if (await _repository.AsQueryable().AnyAsync(it => it.DeleteMark == null && it.Category.Equals(category) && it.FullName.Equals(moduleModel.fullName) && it.SystemId.Equals(systemId) && it.ParentId.Equals(parentId))) errorList.Add("名称");
        moduleModel.moduleId = null;
        moduleModel.parentId = parentId;
        moduleModel.systemId = systemId;

        if (errorList.Any())
        {
            if (type.Equals(0))
            {
                var error = string.Join("、", errorList);
                errorMsgList.Add(string.Format("{0}重复", error));
            }
            else
            {
                var random = new Random().NextLetterAndNumberString(5);
                moduleModel.id = SnowflakeIdHelper.NextId();
                moduleModel.fullName = string.Format("{0}.副本{1}", moduleModel.fullName, random);

                var oldCode = moduleModel.enCode;
                moduleModel.enCode += random;
                if (moduleModel.type == 3)
                    moduleModel.urlAddress = moduleModel.urlAddress.Replace(oldCode, moduleModel.enCode);
            }
        }
        await ImportData(moduleModel, type, errorMsgList);

        if (errorMsgList.Any() && type.Equals(0)) throw Oops.Oh(ErrorCode.COM1018, string.Join("；", errorMsgList));
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<ModuleEntity>> GetList(string systemId)
    {
        if (systemId.IsNullOrEmpty() || systemId.Equals("0"))
        {
            var systemList = await _repository.AsSugarClient().Queryable<SystemEntity>()
                .Where(x => x.DeleteMark == null)
                .OrderBy(x => x.SortCode)
                .OrderBy(x => x.CreatorTime, OrderByType.Desc)
                .Select(x => new ModuleEntity
                {
                    Id = x.Id,
                    ParentId = "-1",
                    EnabledMark = x.EnabledMark,
                    Type = 0,
                    Category = "Web",
                    FullName = x.FullName,
                    Icon = x.Icon,
                    EnCode = x.EnCode,
                    SystemId = x.Id
                }).ToListAsync();

            var moduleList = await _repository.AsQueryable().Where(x => x.DeleteMark == null).OrderBy(x => x.SortCode).OrderBy(x => x.CreatorTime, OrderByType.Desc).ToListAsync();
            moduleList.Where(x => x.ParentId.Equals("-1")).ToList().ForEach(it => it.ParentId = it.SystemId);
            moduleList.AddRange(systemList);
            var appSystemList = systemList.Copy();
            appSystemList.ForEach(item => item.Category = "App");
            moduleList.AddRange(appSystemList);
            return moduleList;
        }

        return await _repository.AsQueryable().Where(x => x.DeleteMark == null && x.SystemId == systemId).OrderBy(o => o.SortCode).OrderBy(o => o.CreatorTime, OrderByType.Desc).ToListAsync();
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<ModuleEntity> GetInfo(string id)
    {
        return await _repository.GetFirstAsync(x => x.Id == id && x.DeleteMark == null);
    }

    /// <summary>
    /// 获取用户树形模块功能列表.
    /// </summary>
    /// <param name="type">登录类型.</param>
    /// <param name="systemId">SystemId.</param>
    [NonAction]
    public async Task<List<ModuleNodeOutput>> GetUserTreeModuleList(string type, string systemId = "")
    {
        return (await GetUserModuleList(type, systemId)).ToTree("-1");
    }

    /// <summary>
    /// 获取用户树形模块功能列表.
    /// </summary>
    /// <param name="type">登录类型.</param>
    /// <param name="systemId">SystemId.</param>
    /// <param name="mIds">指定过滤Ids.</param>
    [NonAction]
    public async Task<List<ModuleNodeOutput>> GetUserModuleListByIds(string type, string systemId = "", List<string> mIds = null, List<string> mUrls = null)
    {
        var result = await GetUserModuleList(type, systemId);
        if (mIds == null) mIds = new List<string>();
        if (mUrls == null) mUrls = new List<string>();

        var rtnList =  result.Where(x => !mIds.Contains(x.id) && !mUrls.Contains(x.urlAddress)).ToList();

        return rtnList;
    }

    /// <summary>
    /// 获取用户菜单列表（使用预取的授权菜单 ID，避免重复查询 AuthorizeEntity）.
    /// </summary>
    [NonAction]
    public async Task<List<ModuleNodeOutput>> GetUserModuleListWithAuthIds(string type, List<string> authorizeModuleIds, List<string> mIds = null, List<string> mUrls = null)
    {
        var result = await GetUserModuleListInternal(type, "", authorizeModuleIds);
        if (mIds == null) mIds = new List<string>();
        if (mUrls == null) mUrls = new List<string>();
        return result.Where(x => !mIds.Contains(x.id) && !mUrls.Contains(x.urlAddress)).ToList();
    }

    /// <summary>
    /// 获取用户树形模块功能列表（内部实现，支持传入预取的授权 ID）.
    /// </summary>
    [NonAction]
    private async Task<List<ModuleNodeOutput>> GetUserModuleListInternal(string type, string systemId, List<string> authorizeModuleIds)
    {
        var output = new List<ModuleNodeOutput>();
        var userSystemId = _userManager.UserOrigin.Equals("pc") ? _userManager.User.SystemId : _userManager.User.AppSystemId;
        if (systemId.IsNotEmptyOrNull()) userSystemId = systemId;
        if (!_userManager.IsAdministrator)
        {
            var dataScop = _userManager.DataScope;
            var objectIdList = dataScop.Where(x => x.organizeType != null && (x.organizeType.Equals("System") || x.organizeType.Equals("Module"))).Select(x => x.organizeId).ToList();

            if (objectIdList.Any(x => x.Equals(userSystemId)))
            {
                if (await _repository.AsSugarClient().Queryable<SystemEntity>().AnyAsync(a => a.Id.Equals(userSystemId) && a.EnCode.Equals("mainSystem")))
                {
                    var menus = await _repository.AsQueryable()
                        .Where(a => (a.SystemId.Equals(userSystemId) && objectIdList.Contains(a.Id) && a.EnabledMark == 1 && a.Category.Equals(type) && a.DeleteMark == null)
                        || _userManager.CommonModuleEnCodeList.Contains(a.EnCode))
                        .OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).ToListAsync();
                    output = menus.Adapt<List<ModuleNodeOutput>>();
                }
                else
                {
                    var menus = await _repository.AsQueryable()
                        .Where(a => (a.SystemId.Equals(userSystemId) && a.EnabledMark == 1 && a.Category.Equals(type) && a.DeleteMark == null)
                        || _userManager.CommonModuleEnCodeList.Contains(a.EnCode))
                        .OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).ToListAsync();
                    output = menus.Adapt<List<ModuleNodeOutput>>();
                }
            }
            else
            {
                // 使用预取的授权 ID，不再查询 AuthorizeEntity
                if (authorizeModuleIds != null && authorizeModuleIds.Any() && userSystemId.IsNotEmptyOrNull())
                {
                    var menus = await _repository.AsQueryable()
                        .Where(a => ((a.SystemId.Equals(userSystemId) || a.SystemId.Equals("workFlow")) && authorizeModuleIds.Contains(a.Id) && a.EnabledMark == 1 && a.Category.Equals(type) && a.DeleteMark == null)
                       || _userManager.CommonModuleEnCodeList.Contains(a.EnCode)).ClearFilter<IZxSystemFilter>()
                        .OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).ToListAsync();
                    output = menus.Adapt<List<ModuleNodeOutput>>();
                }
            }
        }
        else
        {
            var menus = await _repository.AsQueryable()
                .Where(a => (a.SystemId.Equals(userSystemId) && a.EnabledMark == 1 && a.Category.Equals(type) && a.DeleteMark == null)
                || _userManager.CommonModuleEnCodeList.Contains(a.EnCode))
                .ClearFilter<IZxSystemFilter>()
                .OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).ToListAsync();

            output = menus.Adapt<List<ModuleNodeOutput>>();
        }

        if (output.Any())
        {
            output.ForEach(x =>
            {
                x.systemId = userSystemId;
            });
        }

        var workflowEnabled = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(it => it.Id.Equals(userSystemId) && it.DeleteMark == null).Select(it => it.WorkflowEnabled).FirstAsync();
        var workFlow = output.Where(it => it.enCode.Equals("workFlow")).FirstOrDefault();
        if (workFlow.IsNotEmptyOrNull())
        {
            output.Remove(workFlow);
            if (workflowEnabled.IsNotEmptyOrNull() && workflowEnabled.Equals(1))
                output.Insert(0, workFlow);
        }

        return output;
    }

    /// <summary>
    /// 获取用户树形模块功能列表.
    /// </summary>
    /// <param name="type">登录类型.</param>
    /// <param name="systemId">SystemId.</param>
    [NonAction]
    public async Task<List<ModuleNodeOutput>> GetUserModuleList(string type, string systemId = "")
    {
        var output = new List<ModuleNodeOutput>();
        var userSystemId = _userManager.UserOrigin.Equals("pc") ? _userManager.User.SystemId : _userManager.User.AppSystemId;
        if (systemId.IsNotEmptyOrNull()) userSystemId = systemId;
        if (!_userManager.IsAdministrator)
        {
            // 获取所有分管系统Ids
            var dataScop = _userManager.DataScope;
            var objectIdList = dataScop.Where(x => x.organizeType != null && (x.organizeType.Equals("System") || x.organizeType.Equals("Module"))).Select(x => x.organizeId).ToList();

            // 当前系统在分管范围内
            if (objectIdList.Any(x => x.Equals(userSystemId)))
            {
                if (await _repository.AsSugarClient().Queryable<SystemEntity>().AnyAsync(a => a.Id.Equals(userSystemId) && a.EnCode.Equals("mainSystem")))
                {
                    // 当前系统的有分管权限的菜单
                    var menus = await _repository.AsQueryable()
                        .Where(a => (a.SystemId.Equals(userSystemId) && objectIdList.Contains(a.Id) && a.EnabledMark == 1 && a.Category.Equals(type) && a.DeleteMark == null)
                        || _userManager.CommonModuleEnCodeList.Contains(a.EnCode))
                        .OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).ToListAsync();
                    output = menus.Adapt<List<ModuleNodeOutput>>();
                }
                else
                {
                    // 当前系统的所有菜单
                    var menus = await _repository.AsQueryable()
                        .Where(a => (a.SystemId.Equals(userSystemId) && a.EnabledMark == 1 && a.Category.Equals(type) && a.DeleteMark == null)
                        || _userManager.CommonModuleEnCodeList.Contains(a.EnCode))
                        .OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).ToListAsync();
                    output = menus.Adapt<List<ModuleNodeOutput>>();
                }
            }
            else
            {
                var pIds = _userManager.PermissionGroup;
                if (pIds.Any() && userSystemId.IsNotEmptyOrNull())
                {
                    var mIdList = await _repository.AsSugarClient().Queryable<AuthorizeEntity>().Where(a => pIds.Contains(a.ObjectId)).Where(a => a.ItemType == "module").Select(a => a.ItemId).ToListAsync();

                    // 当前系统的有权限的菜单
                    var menus = await _repository.AsQueryable()
                        .Where(a => ((a.SystemId.Equals(userSystemId)  || a.SystemId.Equals("workFlow")  ) && mIdList.Contains(a.Id) && a.EnabledMark == 1 && a.Category.Equals(type) && a.DeleteMark == null)
                       || _userManager.CommonModuleEnCodeList.Contains(a.EnCode)).ClearFilter<IZxSystemFilter>()
                        .OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).ToListAsync();
                    output = menus.Adapt<List<ModuleNodeOutput>>();
                }
            }
        }
        else
        {
            var menus = await _repository.AsQueryable()
                .Where(a => (a.SystemId.Equals(userSystemId) && a.EnabledMark == 1 && a.Category.Equals(type) && a.DeleteMark == null)
                || _userManager.CommonModuleEnCodeList.Contains(a.EnCode))
                .ClearFilter<IZxSystemFilter>()
                .OrderBy(q => q.ParentId).OrderBy(q => q.SortCode).ToListAsync();

            output = menus.Adapt<List<ModuleNodeOutput>>();
        }

        if (output.Any())
        {
            output.ForEach(x =>
            {
                x.systemId = userSystemId;
                //if (x.parentId != "-1" && !output.Any(xx => xx.id.Equals(x.parentId))) x.parentId = "-1";
            });
        }

        // 工作流程
        var workflowEnabled = await _repository.AsSugarClient().Queryable<SystemEntity>().Where(it => it.Id.Equals(userSystemId) && it.DeleteMark == null).Select(it => it.WorkflowEnabled).FirstAsync();
        var workFlow = output.Where(it => it.enCode.Equals("workFlow")).FirstOrDefault();
        if (workFlow.IsNotEmptyOrNull())
        {
            output.Remove(workFlow);
            if (workflowEnabled.IsNotEmptyOrNull() && workflowEnabled.Equals(1))
                output.Insert(0, workFlow);
        }

        return output;
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 导入数据.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <param name="errorMsg"></param>
    /// <returns></returns>
    private async Task ImportData(ModuleExportInput data, int type, List<string> errorMsg)
    {
        try
        {
            if (data.buttonEntityList.Any())
            {
                var button = data.buttonEntityList.Adapt<List<ModuleButtonEntity>>();
                var buttonDic = new Dictionary<string, string>();
                foreach (var item in button)
                {
                    var isExist = false; // 是否出现重复
                    var random = new Random().NextLetterAndNumberString(5);
                    item.ModuleId = data.id;
                    item.Create();
                    item.CreatorUserId = _userManager.UserId;
                    item.LastModifyTime = null;
                    item.LastModifyUserId = null;

                    if (type.Equals(0))
                    {
                        if (await _repository.AsSugarClient().Queryable<ModuleButtonEntity>().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(item.Id)))
                        {
                            if (buttonDic.ContainsKey("ID"))
                                buttonDic["ID"] = string.Format("{0}、{1}", buttonDic["ID"], item.Id);
                            else
                                buttonDic.Add("ID", item.Id);
                            isExist = true;
                        }
                    }
                    else
                    {
                        item.Id = SnowflakeIdHelper.NextId();
                    }
                    if (await _repository.AsSugarClient().Queryable<ModuleButtonEntity>().AnyAsync(it => it.DeleteMark == null && it.ModuleId.Equals(data.id) && it.EnCode.Equals(item.EnCode)))
                    {
                        if (buttonDic.ContainsKey("编码"))
                            buttonDic["编码"] = string.Format("{0}、{1}", buttonDic["编码"], item.EnCode);
                        else
                            buttonDic.Add("编码", item.EnCode);
                        isExist = true;
                    }
                    if (await _repository.AsSugarClient().Queryable<ModuleButtonEntity>().AnyAsync(it => it.DeleteMark == null && it.ModuleId.Equals(data.id) && it.FullName.Equals(item.FullName)))
                    {
                        if (buttonDic.ContainsKey("名称"))
                            buttonDic["名称"] = string.Format("{0}、{1}", buttonDic["名称"], item.FullName);
                        else
                            buttonDic.Add("名称", item.FullName);
                        isExist = true;
                    }

                    if (!isExist) // 子表不重复
                    {
                        var storModuleModel = _repository.AsSugarClient().Storageable(item).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                        await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                        await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                    }
                    else if (isExist && type.Equals(1)) // 追加时，子表重复
                    {
                        item.FullName = string.Format("{0}.副本{1}", item.FullName, random);
                        item.EnCode += random;
                        await _repository.AsSugarClient().Insertable(item).ExecuteCommandAsync();
                    }
                }

                // 组装子表提示语
                if (type.Equals(0) && buttonDic.Any())
                {
                    var buttonMsg = new List<string>();
                    foreach (var item in buttonDic)
                        buttonMsg.Add(string.Format("{0}({1})", item.Key, item.Value));

                    errorMsg.Add(string.Format("buttonEntityList：{0}重复", string.Join("、", buttonMsg)));
                }
            }
            if (data.columnEntityList.Any())
            {
                var colum = data.columnEntityList.Adapt<List<ModuleColumnEntity>>();
                var columDic = new Dictionary<string, string>();
                foreach (var item in colum)
                {
                    var isExist = false; // 是否出现重复
                    var random = new Random().NextLetterAndNumberString(5);
                    item.ModuleId = data.id;
                    item.Create();
                    item.CreatorUserId = _userManager.UserId;
                    item.LastModifyTime = null;
                    item.LastModifyUserId = null;

                    if (type.Equals(0))
                    {
                        if (await _repository.AsSugarClient().Queryable<ModuleColumnEntity>().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(item.Id)))
                        {
                            if (columDic.ContainsKey("ID"))
                                columDic["ID"] = string.Format("{0}、{1}", columDic["ID"], item.Id);
                            else
                                columDic.Add("ID", item.Id);
                            isExist = true;
                        }
                    }
                    else
                    {
                        item.Id = SnowflakeIdHelper.NextId();
                    }
                    if (await _repository.AsSugarClient().Queryable<ModuleColumnEntity>().AnyAsync(it => it.DeleteMark == null && it.ModuleId.Equals(data.id) && it.FullName.Equals(item.FullName)))
                    {
                        if (columDic.ContainsKey("名称"))
                            columDic["名称"] = string.Format("{0}、{1}", columDic["名称"], item.FullName);
                        else
                            columDic.Add("名称", item.FullName);
                        isExist = true;
                    }

                    if (!isExist) // 子表不重复
                    {
                        var storModuleModel = _repository.AsSugarClient().Storageable(item).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                        await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                        await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                    }
                    else if (isExist && type.Equals(1)) // 追加时，子表重复
                    {
                        item.FullName = string.Format("{0}.副本{1}", item.FullName, random);
                        item.EnCode += random;
                        await _repository.AsSugarClient().Insertable(item).ExecuteCommandAsync();
                    }
                }

                // 组装子表提示语
                if (type.Equals(0) && columDic.Any())
                {
                    var columMsg = new List<string>();
                    foreach (var item in columDic)
                        columMsg.Add(string.Format("{0}({1})", item.Key, item.Value));

                    errorMsg.Add(string.Format("columnEntityList：{0}重复", string.Join("、", columMsg)));
                }
            }
            if (data.formEntityList.Any())
            {
                var form = data.formEntityList.Adapt<List<ModuleFormEntity>>();
                var formDic = new Dictionary<string, string>();
                foreach (var item in form)
                {
                    var isExist = false; // 是否出现重复
                    var random = new Random().NextLetterAndNumberString(5);
                    item.ModuleId = data.id;
                    item.Create();
                    item.CreatorUserId = _userManager.UserId;
                    item.LastModifyTime = null;
                    item.LastModifyUserId = null;

                    if (type.Equals(0))
                    {
                        if (await _repository.AsSugarClient().Queryable<ModuleFormEntity>().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(item.Id)))
                        {
                            if (formDic.ContainsKey("ID"))
                                formDic["ID"] = string.Format("{0}、{1}", formDic["ID"], item.Id);
                            else
                                formDic.Add("ID", item.Id);
                            isExist = true;
                        }
                    }
                    else
                    {
                        item.Id = SnowflakeIdHelper.NextId();
                    }
                    if (await _repository.AsSugarClient().Queryable<ModuleFormEntity>().AnyAsync(it => it.DeleteMark == null && it.ModuleId.Equals(data.id) && it.FullName.Equals(item.FullName)))
                    {
                        if (formDic.ContainsKey("名称"))
                            formDic["名称"] = string.Format("{0}、{1}", formDic["名称"], item.FullName);
                        else
                            formDic.Add("名称", item.FullName);
                        isExist = true;
                    }

                    if (!isExist) // 子表不重复
                    {
                        var storModuleModel = _repository.AsSugarClient().Storageable(item).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                        await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                        await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                    }
                    else if (isExist && type.Equals(1)) // 追加时，子表重复
                    {
                        item.FullName = string.Format("{0}.副本{1}", item.FullName, random);
                        item.EnCode += random;
                        await _repository.AsSugarClient().Insertable(item).ExecuteCommandAsync();
                    }
                }

                // 组装子表提示语
                if (type.Equals(0) && formDic.Any())
                {
                    var formMsg = new List<string>();
                    foreach (var item in formDic)
                        formMsg.Add(string.Format("{0}({1})", item.Key, item.Value));

                    errorMsg.Add(string.Format("formEntityList：{0}重复", string.Join("、", formMsg)));
                }
            }

            var dic = new Dictionary<string, string>();
            if (data.authorizeEntityList.Any())
            {
                var dataAuthorize = data.authorizeEntityList.Adapt<List<ModuleDataAuthorizeEntity>>();
                var dataAuthorizeDic = new Dictionary<string, string>();
                foreach (var item in dataAuthorize)
                {
                    var isExist = false; // 是否出现重复
                    var random = new Random().NextLetterAndNumberString(5);
                    item.ModuleId = data.id;
                    item.Create();
                    item.CreatorUserId = _userManager.UserId;
                    item.LastModifyTime = null;
                    item.LastModifyUserId = null;

                    if (type.Equals(0))
                    {
                        if (await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeEntity>().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(item.Id)))
                        {
                            if (dataAuthorizeDic.ContainsKey("ID"))
                                dataAuthorizeDic["ID"] = string.Format("{0}、{1}", dataAuthorizeDic["ID"], item.Id);
                            else
                                dataAuthorizeDic.Add("ID", item.Id);
                            isExist = true;
                        }
                    }
                    else
                    {
                        var id = SnowflakeIdHelper.NextId();
                        dic[item.Id] = id;
                        item.Id = id;
                    }
                    if (await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeEntity>().AnyAsync(it => it.DeleteMark == null && it.ModuleId.Equals(data.id) && it.FullName.Equals(item.FullName)))
                    {
                        if (dataAuthorizeDic.ContainsKey("名称"))
                            dataAuthorizeDic["名称"] = string.Format("{0}、{1}", dataAuthorizeDic["名称"], item.FullName);
                        else
                            dataAuthorizeDic.Add("名称", item.FullName);
                        isExist = true;
                    }

                    if (!isExist) // 子表不重复
                    {
                        var storModuleModel = _repository.AsSugarClient().Storageable(item).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                        await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                        await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                    }
                    else if (isExist && type.Equals(1)) // 追加时，子表重复
                    {
                        item.FullName = string.Format("{0}.副本{1}", item.FullName, random);
                        item.EnCode += random;
                        await _repository.AsSugarClient().Insertable(item).ExecuteCommandAsync();
                    }
                }

                // 组装子表提示语
                if (type.Equals(0) && dataAuthorizeDic.Any())
                {
                    var dataAuthorizeMsg = new List<string>();
                    foreach (var item in dataAuthorizeDic)
                        dataAuthorizeMsg.Add(string.Format("{0}({1})", item.Key, item.Value));

                    errorMsg.Add(string.Format("authorizeEntityList：{0}重复", string.Join("、", dataAuthorizeMsg)));
                }
            }
            if (data.schemeEntityList.Any())
            {
                var dataAuthorizeScheme = data.schemeEntityList.Adapt<List<ModuleDataAuthorizeSchemeEntity>>();
                var dataAuthorizeSchemeDic = new Dictionary<string, string>();
                foreach (var item in dataAuthorizeScheme)
                {
                    var isExist = false; // 是否出现重复
                    var random = new Random().NextLetterAndNumberString(5);
                    item.ModuleId = data.id;
                    item.Create();
                    item.CreatorUserId = _userManager.UserId;
                    item.LastModifyTime = null;
                    item.LastModifyUserId = null;

                    if (type.Equals(0))
                    {
                        if (await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().AnyAsync(it => it.DeleteMark == null && it.Id.Equals(item.Id)))
                        {
                            if (dataAuthorizeSchemeDic.ContainsKey("ID"))
                                dataAuthorizeSchemeDic["ID"] = string.Format("{0}、{1}", dataAuthorizeSchemeDic["ID"], item.Id);
                            else
                                dataAuthorizeSchemeDic.Add("ID", item.Id);
                            isExist = true;
                        }
                    }
                    else
                    {
                        item.Id = SnowflakeIdHelper.NextId();
                    }
                    if (await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().AnyAsync(it => it.DeleteMark == null && it.ModuleId.Equals(data.id) && it.EnCode.Equals(item.EnCode)))
                    {
                        if (dataAuthorizeSchemeDic.ContainsKey("编码"))
                            dataAuthorizeSchemeDic["编码"] = string.Format("{0}、{1}", dataAuthorizeSchemeDic["编码"], item.EnCode);
                        else
                            dataAuthorizeSchemeDic.Add("编码", item.EnCode);
                        isExist = true;
                    }
                    if (await _repository.AsSugarClient().Queryable<ModuleDataAuthorizeSchemeEntity>().AnyAsync(it => it.DeleteMark == null && it.ModuleId.Equals(data.id) && it.FullName.Equals(item.FullName)))
                    {
                        if (dataAuthorizeSchemeDic.ContainsKey("名称"))
                            dataAuthorizeSchemeDic["名称"] = string.Format("{0}、{1}", dataAuthorizeSchemeDic["名称"], item.FullName);
                        else
                            dataAuthorizeSchemeDic.Add("名称", item.FullName);
                        isExist = true;
                    }

                    if (!isExist) // 子表不重复
                    {
                        var storModuleModel = _repository.AsSugarClient().Storageable(item).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
                        await storModuleModel.AsInsertable.ExecuteCommandAsync(); // 执行插入
                        await storModuleModel.AsUpdateable.ExecuteCommandAsync(); // 执行更新
                    }
                    else if (isExist && type.Equals(1)) // 追加时，子表重复
                    {
                        item.FullName = string.Format("{0}.副本{1}", item.FullName, random);
                        item.EnCode += random;
                        if (item.ConditionJson.IsNotEmptyOrNull())
                        {
                            foreach (var key in dic.Keys)
                            {
                                item.ConditionJson = item.ConditionJson.Replace(key, dic[key]);
                            }
                        }
                        await _repository.AsSugarClient().Insertable(item).ExecuteCommandAsync();
                    }
                }

                // 组装子表提示语
                if (type.Equals(0) && dataAuthorizeSchemeDic.Any())
                {
                    var dataAuthorizeSchemeMsg = new List<string>();
                    foreach (var item in dataAuthorizeSchemeDic)
                        dataAuthorizeSchemeMsg.Add(string.Format("{0}({1})", item.Key, item.Value));

                    errorMsg.Add(string.Format("schemeEntityList：{0}重复", string.Join("、", dataAuthorizeSchemeMsg)));
                }
            }

            var module = data.Adapt<ModuleEntity>();
            module.Create();
            module.CreatorUserId = _userManager.UserId;
            module.LastModifyTime = null;
            module.LastModifyUserId = null;
            var stor = _repository.AsSugarClient().Storageable(module).Saveable().ToStorage(); // 存在更新不存在插入 根据主键
            await stor.AsInsertable.ExecuteCommandAsync(); // 执行插入
            await stor.AsUpdateable.ExecuteCommandAsync(); // 执行更新
        }
        catch (Exception ex)
        {
            throw Oops.Oh(ErrorCode.COM1020, ex.Message);
        }
    }

    #endregion
}
