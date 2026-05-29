using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.JsonSerialization;
using JNPF.LinqBuilder;
using JNPF.Systems.Entitys.Dto.Organize;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 机构管理.
/// 组织架构：公司》部门》岗位》用户
/// 版 本：V3.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021.06.07.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Organize", Order = 165)]
[Route("api/permission/[controller]")]
public class OrganizeService : IOrganizeService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<OrganizeEntity> _repository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 系统配置.
    /// </summary>
    private readonly ISysConfigService _sysConfigService;

    /// <summary>
    /// 第三方同步.
    /// </summary>
    private readonly ISynThirdInfoService _synThirdInfoService;

    /// <summary>
    /// 多租户事务.
    /// </summary>
    private readonly ITenant _db;

    /// <summary>
    /// 初始化一个<see cref="OrganizeService"/>类型的新实例.
    /// </summary>
    public OrganizeService(
        ISqlSugarRepository<OrganizeEntity> repository,
        ISysConfigService sysConfigService,
        ISynThirdInfoService synThirdInfoService,
        IUserManager userManager,
        ISqlSugarClient context)
    {
        _repository = repository;
        _sysConfigService = sysConfigService;
        _synThirdInfoService = synThirdInfoService;
        _userManager = userManager;
        _db = context.AsTenant();
    }

    #region GET

    /// <summary>
    /// 获取机构列表.
    /// </summary>
    /// <param name="input">关键字参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] CommonInput input)
    {
        StructureOrganizeIdTree(); // 构造组织树 id.

        // 获取分级管理组织
        var dataScope = _userManager.DataScope.Where(x => x.Select).Select(x => x.organizeId).Distinct().ToList();

        List<OrganizeListOutput>? data = await _repository.AsQueryable().Where(t => t.DeleteMark == null)
            .WhereIF(!_userManager.IsAdministrator, a => dataScope.Contains(a.Id))
            .WhereIF(input.type.IsNotEmptyOrNull(), a => a.Category.Equals(input.type))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select(x => new OrganizeListOutput()
            {
                category = x.Category,
                creatorTime = x.CreatorTime,
                enCode = x.EnCode,
                enabledMark = x.EnabledMark,
                fullName = x.FullName,
                id = x.Id,
                organizeIdTree = x.OrganizeIdTree,
                parentId = x.ParentId,
                sortCode = x.SortCode,
                icon = SqlFunc.IIF(x.Category.Equals("company"), "icon-ym icon-ym-tree-organization3", "icon-ym icon-ym-tree-department1"),
                type = x.Category
            }).ToListAsync();

        if (!string.IsNullOrEmpty(input.keyword))
            data = data.TreeWhere(t => t.fullName.Contains(input.keyword) || t.enCode.Contains(input.keyword), t => t.id, t => t.parentId);
        data.ForEach(item =>
        {
            if (!data.Any(x => x.id.Equals(item.parentId)))
                item.parentId = item.parentId.Equals("-1") ? "-1" : "0";
        });

        // 获取组织树
        var orgTree = GetOrgListTreeName();

        data.ForEach(item =>
        {
            item.fullName = orgTree.FirstOrDefault(x => x.Id.Equals(item.id))?.Description;
            item.organizeIds = item.organizeIdTree.Split(",").ToList();
        });

        // 组织断层处理
        data.OrderByDescending(x => x.organizeIdTree.Length).ToList().ForEach(item =>
        {
            if (!data.Any(x => x.id.Equals(item.parentId)))
            {
                var pItem = data.Find(x => x.id != item.id && item.organizeIdTree.Contains(x.organizeIdTree));
                if (pItem != null)
                {
                    item.parentId = pItem.id;
                    item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
                }
                else
                {
                    item.parentId = item.parentId.Equals("-1") ? "-1" : "0";
                }
            }
            else
            {
                var pItem = data.Find(x => x.id.Equals(item.parentId));
                item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
            }
        });

        return data.Any(x => x.parentId.Equals("-1")) ? new { list = data.OrderBy(x => x.sortCode).ToList().ToTree("-1") } : new { list = data.OrderBy(x => x.sortCode).ToList().ToTree("0") };
    }

    /// <summary>
    /// 获取下拉框.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector/{id}")]
    public async Task<dynamic> GetSelector(string id)
    {
        // 获取组织树
        var orgTree = GetOrgListTreeName();

        List<OrganizeEntity>? data = await _repository.AsQueryable().Where(t => t.Category.Equals("company") && t.DeleteMark == null).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();
        if (!"0".Equals(id))
        {
            OrganizeEntity? info = data.Find(it => it.Id == id);
            data.Remove(info);
        }

        List<OrganizeListOutput>? treeList = data.Adapt<List<OrganizeListOutput>>();
        treeList.ForEach(item =>
        {
            item.type = item.category;
            if (item != null && item.category.Equals("company")) item.icon = "icon-ym icon-ym-tree-organization3";
            item.organize = orgTree.FirstOrDefault(x => x.Id.Equals(item.id))?.Description;
            item.organizeIds = item.organizeIdTree.Split(",").ToList();
        });
        return new { list = treeList.OrderBy(x => x.sortCode).ToList().ToTree("-1") };
    }

    /// <summary>
    /// 获取下拉框根据权限.
    /// </summary>
    /// <returns></returns>
    [HttpGet("SelectorByAuth/{id}")]
    public async Task<dynamic> GetSelectorByAuth(string id)
    {
        // 获取组织树
        var orgTree = GetOrgListTreeName();

        // 获取分级管理组织
        var dataScope = _userManager.DataScope.Where(x => x.Select).Select(x => x.organizeId).Distinct().ToList();

        List<OrganizeEntity>? data = await _repository.AsQueryable().Where(t => t.Category.Equals("company") && t.DeleteMark == null)
            .WhereIF(!_userManager.IsAdministrator, x => dataScope.Contains(x.Id))
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();

        //if (!"0".Equals(id))
        //{
        //    OrganizeEntity? info = data.Find(it => it.Id == id);
        //    data.Remove(info);
        //}

        List<OrganizeSelectorOutput>? treeList = data.Adapt<List<OrganizeSelectorOutput>>();
        treeList.ForEach(item =>
        {
            item.type = item.category;
            if (item != null && item.category.Equals("company")) item.icon = "icon-ym icon-ym-tree-organization3";
            item.fullName = orgTree.FirstOrDefault(x => x.Id.Equals(item.id))?.Description;
            item.organize = item.fullName;
            item.organizeIds = item.organizeIdTree.Split(",").ToList();
        });

        if (!await _repository.AsSugarClient().Queryable<OrganizeEntity>().AnyAsync(x => treeList.Select(xx => xx.id).Contains(x.ParentId) && x.Id.Equals(id)))
        {
            if (!id.Equals("0") && id.IsNotEmptyOrNull())
            {
                var entity = _repository.GetById(id);
                var pItem = _repository.GetById(entity.ParentId);
                if (pItem != null)
                {
                    var addItem = pItem.Adapt<OrganizeSelectorOutput>();

                    if (addItem.type != null && addItem.type.Equals("company")) addItem.icon = "icon-ym icon-ym-tree-organization3";
                    addItem.fullName = orgTree.FirstOrDefault(x => x.Id.Equals(addItem.id))?.Description;
                    addItem.organize = addItem.fullName;
                    addItem.organizeIds = addItem.organizeIdTree.Split(",").ToList();
                    addItem.disabled = true;
                    addItem.sortCode = 0;
                    if (!treeList.Any(x => x.id.Equals(addItem.id))) treeList.Add(addItem);
                }
            }
        }

        // 组织断层处理
        treeList.Where(x => x.parentId != "-1").OrderByDescending(x => x.organizeIdTree.Length).ToList().ForEach(item =>
        {
            if (!treeList.Any(x => x.id.Equals(item.parentId)))
            {
                var pItem = treeList.Find(x => x.id != item.id && item.organizeIdTree.Contains(x.organizeIdTree));
                if (pItem != null)
                {
                    item.parentId = pItem.id;
                    item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
                }
                else
                {
                    item.parentId = "-1";
                }
            }
            else
            {
                var pItem = treeList.Find(x => x.id.Equals(item.parentId));
                item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
            }
        });

        return new { list = treeList.OrderBy(x => x.sortCode).ToList().ToTree("-1") };
    }

    /// <summary>
    /// 获取树形.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Tree")]
    public async Task<dynamic> GetTree()
    {
        List<OrganizeEntity>? data = await _repository.AsQueryable().Where(t => t.Category.Equals("company") && t.DeleteMark == null).OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();
        List<OrganizeTreeOutput>? treeList = data.Adapt<List<OrganizeTreeOutput>>();
        return new { list = treeList.OrderBy(x => x.sortCode).ToList().ToTree("-1") };
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        OrganizeEntity? entity = await _repository.GetSingleAsync(p => p.Id == id);
        var res = entity.Adapt<OrganizeInfoOutput>();
        if (entity.ParentId.Equals("-1")) res.organizeIdTree = new List<string>() { "-1" };
        else res.organizeIdTree = (await _repository.GetSingleAsync(p => p.Id == entity.ParentId)).OrganizeIdTree.Split(",").ToList();
        return res;
    }

    #endregion

    #region POST

    /// <summary>
    /// 根据组织Id List 获取当前所属组织(部门).
    /// </summary>
    /// <returns></returns>
    [HttpPost("getDefaultCurrentValueDepartmentId")]
    public async Task<dynamic> GetDefaultCurrentValueDepartmentId([FromBody] GetDefaultCurrentValueInput input)
    {
        var depId = _repository.AsSugarClient().Queryable<UserEntity, OrganizeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.OrganizeId))
            .Where((a, b) => a.Id.Equals(_userManager.UserId) && b.Category.Equals("department")).Select((a, b) => a.OrganizeId).First();

        if (input.DepartIds == null || !input.DepartIds.Any()) return new { departmentId = depId };
        var userRelationList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => input.DepartIds.Contains(x.ObjectId))
            .Select(x => x.UserId).ToList();

        if (userRelationList.Contains(_userManager.UserId)) return new { userId = depId };
        else return new { departmentId = string.Empty };
    }

    /// <summary>
    /// 通过部门id获取部门列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("OrganizeCondition")]
    public async Task<dynamic> OrganizeCondition([FromBody] OrganizeConditionInput input)
    {
        // 获取所有组织
        List<OrganizeEntity>? allOrgList = GetOrgListTreeName();

        if (input.departIds.Contains("@currentOrg"))
        {
            input.departIds.Add(_userManager.User.OrganizeId);
            input.departIds.Remove("@currentOrg");
        }
        if (input.departIds.Contains("@currentOrgAndSubOrg"))
        {
            input.departIds.AddRange(allOrgList.TreeChildNode(_userManager.User.OrganizeId, t => t.Id, t => t.ParentId).Select(it => it.Id).ToList());
            input.departIds.Remove("@currentOrgAndSubOrg");
        }
        if (input.departIds.Contains("@currentGradeOrg"))
        {
            if (_userManager.IsAdministrator)
            {
                input.departIds.AddRange(allOrgList.Select(it => it.Id).ToList());
            }
            else
            {
                input.departIds.AddRange(_userManager.DataScope.Select(x => x.organizeId).ToList());
            }
            input.departIds.Remove("@currentGradeOrg");
        }

        var queryWhere = LinqExpression.Or<OrganizeEntity>();
        input.departIds.ForEach(id => queryWhere = queryWhere.Or(x => x.Id.Contains(id)));
        queryWhere = queryWhere.And(x => x.DeleteMark == null);
        List<OrganizeListOutput>? data = await _repository.AsQueryable().Where(queryWhere)
            .WhereIF(input.keyword.IsNotEmptyOrNull(), a => a.FullName.Contains(input.keyword) || a.EnCode.Contains(input.keyword)).Select(a => new OrganizeListOutput
            {
                id = a.Id,
                organizeIdTree = a.OrganizeIdTree,
                type = a.Category,
                parentId = a.ParentId,
                lastFullName = a.FullName,
                fullName = a.FullName,
                enabledMark = a.EnabledMark,
                creatorTime = a.CreatorTime,
                icon = a.Category.Equals("company") ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                sortCode = a.SortCode
            }).ToListAsync();

        data.ForEach(item =>
        {
            var description = allOrgList.FirstOrDefault(x => x.Id == item.id)?.Description;
            if (!data.Any(x => x.id.Equals(item.parentId)))
            {
                item.fullName = description;
                item.parentId = "0";
            }
            var ids = new List<string>();
            ids.AddRange(item.organizeIdTree.Split(",").ToList());
            item.organizeIds = ids;
            item.organize = description;
        });

        // 组织断层处理
        data.OrderByDescending(x => x.organizeIdTree.Length).ToList().ForEach(item =>
        {
            if (!data.Any(x => x.id.Equals(item.parentId)))
            {
                var pItem = data.Find(x => x.id != item.id && item.organizeIdTree.Contains(x.organizeIdTree));
                if (pItem != null)
                {
                    item.parentId = pItem.id;
                    item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
                }
                else
                {
                    item.parentId = "0";
                }
            }
            else
            {
                var pItem = data.Find(x => x.id.Equals(item.parentId));
                item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
            }
        });

        return new { list = data.ToTree("0") };
    }

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task Create([FromBody] OrganizeCrInput input)
    {
        if (!_userManager.DataScope.Any(it => it.organizeId == input.parentId && it.Add) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);
        if (await _repository.IsAnyAsync(o => o.EnCode == input.enCode && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2008);
        if (await _repository.IsAnyAsync(o => o.ParentId == input.parentId && o.FullName == input.fullName && o.Category == "company" && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2009);
        OrganizeEntity? entity = input.Adapt<OrganizeEntity>();
        entity.Id = SnowflakeIdHelper.NextId();
        entity.EnabledMark = input.enabledMark;
        entity.Category = "company";
        entity.PropertyJson = JSON.Serialize(input.propertyJson);

        #region 处理 上级ID列表 存储

        List<string>? idList = new List<string>();
        idList.Add(entity.Id);
        if (entity.ParentId != "-1")
        {
            List<string>? ids = _repository.AsSugarClient().Queryable<OrganizeEntity>().ToParentList(it => it.ParentId, entity.ParentId).Select(x => x.Id).ToList();
            idList.AddRange(ids);
        }

        idList.Reverse();
        entity.OrganizeIdTree = string.Join(",", idList);
        #endregion

        OrganizeEntity? isOk = await _repository.AsSugarClient().Insertable(entity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
        _ = isOk ?? throw Oops.Oh(ErrorCode.D2012);

        #region 默认赋予分级管理权限
        var adminlist = new List<OrganizeAdministratorEntity>();
        if (!_userManager.IsAdministrator)
        {
            adminlist.Add(new OrganizeAdministratorEntity()
            {
                UserId = _userManager.UserId,
                OrganizeId = isOk.Id,
                ThisLayerAdd = 1,
                ThisLayerDelete = 1,
                ThisLayerEdit = 1,
                ThisLayerSelect = 1,
                SubLayerAdd = 0,
                SubLayerDelete = 0,
                SubLayerEdit = 0,
                SubLayerSelect = 0
            });
        }

        var adminUserIds = _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Where(x => !x.UserId.Equals(_userManager.UserId)).Select(x => x.UserId).Distinct().ToList();
        adminUserIds.ForEach(userid =>
        {
            adminlist.Add(new OrganizeAdministratorEntity()
            {
                UserId = userid,
                OrganizeId = isOk.Id,
                ThisLayerAdd = 0,
                ThisLayerDelete = 0,
                ThisLayerEdit = 0,
                ThisLayerSelect = 0,
                SubLayerAdd = 0,
                SubLayerDelete = 0,
                SubLayerEdit = 0,
                SubLayerSelect = 0
            });
        });

        if (adminlist.Any()) await _repository.AsSugarClient().Insertable(adminlist).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
        #endregion

        #region 第三方同步

        try
        {
            SysConfigOutput? sysConfig = await _sysConfigService.GetInfo();
            List<OrganizeListOutput>? orgList = new List<OrganizeListOutput>();
            orgList.Add(entity.Adapt<OrganizeListOutput>());
            if (sysConfig.dingSynIsSynOrg)
                await _synThirdInfoService.SynDep(2, 1, sysConfig, orgList);
            if (sysConfig.qyhIsSynOrg)
                await _synThirdInfoService.SynDep(1, 1, sysConfig, orgList);
        }
        catch (Exception)
        {
        }

        #endregion
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="input">参数.</param>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] OrganizeUpInput input)
    {
        OrganizeEntity? oldEntity = await _repository.GetSingleAsync(it => it.Id == id);
        if (oldEntity.ParentId != input.parentId && !_userManager.DataScope.Any(it => it.organizeId == oldEntity.ParentId && it.Edit) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        if (oldEntity.ParentId != input.parentId && !_userManager.DataScope.Any(it => it.organizeId == input.parentId && it.Edit) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        if (!_userManager.DataScope.Any(it => it.organizeId == id && it.Edit == true) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        if (input.parentId.Equals(id))
            throw Oops.Oh(ErrorCode.D2001);
        if (input.parentId.Equals("-1") && !oldEntity.ParentId.Equals("-1") && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        // 父id不能为自己的子节点
        List<string>? childIdListById = await GetChildIdListWithSelfById(id);
        if (childIdListById.Contains(input.parentId))
            throw Oops.Oh(ErrorCode.D2001);
        if (await _repository.IsAnyAsync(o => o.EnCode == input.enCode && o.Id != id && o.DeleteMark == null && o.Id != id))
            throw Oops.Oh(ErrorCode.D2008);
        if (await _repository.IsAnyAsync(o => o.ParentId == input.parentId && o.FullName == input.fullName && o.Id != id && o.Category == "company" && o.DeleteMark == null && o.Id != id))
            throw Oops.Oh(ErrorCode.D2009);
        OrganizeEntity? entity = input.Adapt<OrganizeEntity>();
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;
        entity.PropertyJson = JSON.Serialize(input.propertyJson);

        try
        {
            // 开启事务
            _db.BeginTran();

            #region 处理 上级ID列表 存储

            if (string.IsNullOrWhiteSpace(oldEntity.OrganizeIdTree) || entity.ParentId != oldEntity.ParentId)
            {
                List<string>? idList = new List<string>();
                idList.Add(entity.Id);
                if (entity.ParentId != "-1")
                {
                    List<string>? ids = _repository.AsSugarClient().Queryable<OrganizeEntity>().ToParentList(it => it.ParentId, entity.ParentId).Select(x => x.Id).ToList();
                    idList.AddRange(ids);
                }

                idList.Reverse();
                entity.OrganizeIdTree = string.Join(",", idList);

                // 如果上级结构 变动 ，需要更改所有包含 该组织的id 的结构
                if (entity.OrganizeIdTree != oldEntity.OrganizeIdTree)
                {
                    List<OrganizeEntity>? oldEntityList = await _repository.AsQueryable().Where(x => x.OrganizeIdTree.Contains(oldEntity.OrganizeIdTree + ",") && x.Id != oldEntity.Id).ToListAsync();
                    oldEntityList.Add(oldEntity);
                    oldEntityList.ForEach(item =>
                    {
                        string? childList = item.OrganizeIdTree.Split(oldEntity.Id).LastOrDefault();
                        item.OrganizeIdTree = entity.OrganizeIdTree + childList;
                    });

                    await _repository.AsSugarClient().Updateable(oldEntityList).UpdateColumns(x => x.OrganizeIdTree).ExecuteCommandAsync(); // 批量修改 父级组织
                }
            }

            #endregion

            await _repository.AsSugarClient().Updateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();

            _db.CommitTran();
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw Oops.Oh(ErrorCode.D2010);
        }

        #region 第三方同步

        try
        {
            SysConfigOutput? sysConfig = await _sysConfigService.GetInfo();
            List<OrganizeListOutput>? orgList = new List<OrganizeListOutput>();
            entity.Category = "company";
            orgList.Add(entity.Adapt<OrganizeListOutput>());
            if (sysConfig.dingSynIsSynOrg) await _synThirdInfoService.SynDep(2, 1, sysConfig, orgList);
            if (sysConfig.qyhIsSynOrg) await _synThirdInfoService.SynDep(1, 1, sysConfig, orgList);
        }
        catch (Exception)
        {
        }

        #endregion
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id)
    {
        if (!_userManager.DataScope.Any(it => it.organizeId == id && it.Delete == true) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        // 该机构下有机构，则不能删
        if (await _repository.IsAnyAsync(o => o.ParentId.Equals(id) && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2005);

        // 该机构下有岗位，则不能删
        if (await _repository.AsSugarClient().Queryable<PositionEntity>().AnyAsync(p => p.OrganizeId.Equals(id) && p.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2006);

        // 该机构下有用户，则不能删
        if (await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType == "Organize" && x.ObjectId == id).AnyAsync())
            throw Oops.Oh(ErrorCode.D2004);

        // 该机构下有角色，则不能删
        if (await _repository.AsSugarClient().Queryable<OrganizeRelationEntity>().Where(x => x.OrganizeId == id && x.ObjectType == "Role").AnyAsync())
            throw Oops.Oh(ErrorCode.D2020);

        try
        {
            // 开启事务
            _db.BeginTran();

            await _repository.AsSugarClient().Updateable<OrganizeEntity>().SetColumns(it => new OrganizeEntity()
            {
                DeleteMark = 1,
                DeleteTime = SqlFunc.GetDate(),
                DeleteUserId = _userManager.UserId,
            }).Where(it => it.Id == id && it.DeleteMark == null).ExecuteCommandAsync();

            // 删除该组织和角色关联数据
            await _repository.AsSugarClient().Deleteable<OrganizeRelationEntity>().Where(x => x.OrganizeId == id && x.ObjectType == "Role").ExecuteCommandAsync();

            _db.CommitTran();
        }
        catch (Exception)
        {
            _db.RollbackTran();
            throw Oops.Oh(ErrorCode.D2012);
        }

        #region 第三方同步
        try
        {
            SysConfigOutput? sysConfig = await _sysConfigService.GetInfo();
            if (sysConfig.dingSynIsSynOrg)
                await _synThirdInfoService.DelSynData(2, 1, sysConfig, id);

            if (sysConfig.qyhIsSynOrg)
                await _synThirdInfoService.DelSynData(1, 1, sysConfig, id);

        }
        catch (Exception)
        {
        }
        #endregion
    }

    /// <summary>
    /// 更新状态.
    /// </summary>
    /// <param name="id">主键</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/State")]
    public async Task UpdateState(string id)
    {
        if (!_userManager.DataScope.Any(it => it.organizeId == id && it.Edit == true) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        if (!await _repository.IsAnyAsync(u => u.Id == id && u.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2002);

        int isOk = await _repository.AsSugarClient().Updateable<OrganizeEntity>().SetColumns(it => new OrganizeEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 1, 0, 1),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandAsync();

        if (!(isOk > 0))
            throw Oops.Oh(ErrorCode.D2011);
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 是否机构主管.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<bool> GetIsManagerByUserId(string userId)
    {
        return await _repository.IsAnyAsync(o => o.EnabledMark == 1 && o.DeleteMark == null && o.ManagerId == userId);
    }

    /// <summary>
    /// 获取机构列表(其他服务使用).
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<OrganizeEntity>> GetListAsync()
    {
        return await _repository.AsQueryable().Where(t => t.EnabledMark == 1 && t.DeleteMark == null).OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();
    }

    /// <summary>
    /// 获取公司列表(其他服务使用).
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<OrganizeEntity>> GetCompanyListAsync()
    {
        return await _repository.AsQueryable().Where(t => t.Category.Equals("company") && t.EnabledMark == 1 && t.DeleteMark == null).OrderBy(o => o.SortCode).ToListAsync();
    }

    /// <summary>
    /// 下属机构.
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <param name="isAdmin">是否管理员.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<string[]> GetSubsidiary(string organizeId, bool isAdmin)
    {
        List<OrganizeEntity>? data = await _repository.AsQueryable().Where(o => o.DeleteMark == null && o.EnabledMark == 1).OrderBy(o => o.SortCode).ToListAsync();
        if (!isAdmin)
            data = data.TreeChildNode(organizeId, t => t.Id, t => t.ParentId);
        return data.Select(m => m.Id).ToArray();
    }

    /// <summary>
    /// 下属机构.
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<string>> GetSubsidiary(string organizeId)
    {
        List<OrganizeEntity>? data = await _repository.AsQueryable().Where(o => o.DeleteMark == null && o.EnabledMark == 1).OrderBy(o => o.SortCode).ToListAsync();
        data = data.TreeChildNode(organizeId, t => t.Id, t => t.ParentId);
        return data.Select(m => m.Id).ToList();
    }

    /// <summary>
    /// 根据节点Id获取所有子节点Id集合，包含自己.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<string>> GetChildIdListWithSelfById(string id)
    {
        List<string>? childIdList = await _repository.AsQueryable().Where(u => u.ParentId.Contains(id) && u.DeleteMark == null).Select(u => u.Id).ToListAsync();
        childIdList.Add(id);
        return childIdList;
    }

    /// <summary>
    /// 获取机构成员列表 .
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<OrganizeMemberListOutput>> GetOrganizeMemberList(string organizeId)
    {
        List<OrganizeMemberListOutput>? output = new List<OrganizeMemberListOutput>();
        if (organizeId.Equals("0"))
        {
            List<OrganizeEntity>? data = await _repository.AsQueryable()
                .Where(o => o.DeleteMark == null && o.EnabledMark == 1 && o.ParentId.Equals("-1")).OrderBy(o => o.SortCode).ToListAsync();
            data.ForEach(o =>
            {
                output.Add(new OrganizeMemberListOutput
                {
                    id = o.Id,
                    parentId = o.ParentId,
                    fullName = o.FullName,
                    enabledMark = o.EnabledMark,
                    type = o.Category,
                    organize = o.FullName,
                    icon = o.Category.Equals("company") ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                    organizeIdTree = o.OrganizeIdTree,
                    hasChildren = true,
                    isLeaf = false
                });
            });
        }
        else
        {
            var relationList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType.Equals("Organize") && x.ObjectId.Equals(organizeId)).Select(x => x.UserId).ToListAsync();
            List<UserEntity>? userList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(u => relationList.Contains(u.Id) && u.EnabledMark > 0 && u.DeleteMark == null).OrderBy(o => o.SortCode).ToListAsync();
            userList.ForEach(u =>
            {
                output.Add(new OrganizeMemberListOutput()
                {
                    id = u.Id,
                    fullName = u.RealName + "/" + u.Account,
                    enabledMark = u.EnabledMark,
                    type = "user",
                    icon = "icon-ym icon-ym-tree-user2",
                    headIcon = "/api/File/Image/userAvatar/" + u.HeadIcon,
                    hasChildren = false,
                    isLeaf = true,
                    isAdministrator = u.IsAdministrator,
                });
            });
            List<OrganizeEntity>? departmentList = await _repository.AsQueryable()
                .Where(o => o.ParentId.Equals(organizeId) && o.DeleteMark == null && o.EnabledMark == 1).OrderBy(o => o.SortCode).ToListAsync();
            departmentList.ForEach(o =>
            {
                output.Add(new OrganizeMemberListOutput()
                {
                    id = o.Id,
                    parentId = o.ParentId,
                    fullName = o.FullName,
                    enabledMark = o.EnabledMark,
                    type = o.Category,
                    icon = o.Category.Equals("company") ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                    hasChildren = true,
                    organizeIdTree = o.OrganizeIdTree,
                    isLeaf = false
                });
            });
        }

        // 获取组织树
        var orgTree = GetOrgListTreeName();

        // 组织断层处理
        output.Where(x => x.parentId != "-1" && x.organizeIdTree.IsNotEmptyOrNull()).OrderByDescending(x => x.organizeIdTree.Length).ToList().ForEach(item =>
        {
            item.fullName = orgTree.FirstOrDefault(x => x.Id.Equals(item.id))?.Description;
            item.organize = item.fullName;
            if (!output.Any(x => x.id.Equals(item.parentId)))
            {
                var pItem = output.Find(x => x.organizeIdTree.IsNotEmptyOrNull() && x.id != item.id && item.organizeIdTree.Contains(x.organizeIdTree));
                if (pItem != null)
                {
                    item.parentId = pItem.id;
                    item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
                }
                else
                {
                    item.parentId = "-1";
                }
            }
            else
            {
                var pItem = output.Find(x => x.id.Equals(item.parentId));
                item.fullName = item.fullName.Replace(pItem.fullName + "/", string.Empty);
            }
        });

        if (!organizeId.Equals("0"))
        {
            var pOrgTreeName = orgTree.Find(x => x.Id.Equals(organizeId)).Description;
            output.ForEach(item => item.fullName = item.fullName.Replace(pOrgTreeName + "/", string.Empty));
        }

        return output;
    }

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<OrganizeEntity> GetInfoById(string Id)
    {
        return await _repository.GetSingleAsync(p => p.Id == Id);
    }

    /// <summary>
    /// 获取组织下所有子组织.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<List<string>> GetChildOrgId(string id)
    {
        return (await _repository.GetListAsync(x => x.OrganizeIdTree.Contains(id) && x.EnabledMark == 1 && x.DeleteMark == null)).Select(x => x.Id).ToList();
    }

    /// <summary>
    /// 处理组织树 名称.
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public List<OrganizeEntity> GetOrgListTreeName()
    {
        List<OrganizeEntity>? orgTreeNameList = new List<OrganizeEntity>();
        List<OrganizeEntity>? orgList = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.DeleteMark == null && x.EnabledMark == 1).ToList();
        orgList.ForEach(item =>
        {
            if (item.OrganizeIdTree.IsNullOrEmpty()) item.OrganizeIdTree = item.Id;
            OrganizeEntity? newItem = item.Adapt<OrganizeEntity>();
            newItem.Id = item.Id;
            var orgNameList = new List<string>();
            item.OrganizeIdTree.Split(",").ToList().ForEach(it =>
            {
                var org = orgList.Find(x => x.Id == it);
                if (org != null) orgNameList.Add(org.FullName);
            });
            newItem.Description = string.Join("/", orgNameList);
            orgTreeNameList.Add(newItem);
        });

        return orgTreeNameList;
    }

    /// <summary>
    /// 构造 OrganizeIdTree.
    /// </summary>
    private void StructureOrganizeIdTree()
    {
        if (_repository.IsAny(x => SqlFunc.IsNullOrEmpty(SqlFunc.ToString(x.OrganizeIdTree))))
        {
            var orgList = _repository.GetList(x => SqlFunc.IsNullOrEmpty(x.OrganizeIdTree));

            orgList.ForEach(item =>
            {
                if (item.ParentId == "-1")
                {
                    item.OrganizeIdTree = item.Id;
                }
                else
                {
                    var plist = _repository.AsQueryable().ToParentList(it => it.ParentId, item.Id).Select(x => x.Id);
                    plist = plist.Reverse();
                    item.OrganizeIdTree = string.Join(",", plist);
                }
            });
            _repository.AsUpdateable(orgList).ExecuteCommand();
        }
    }

    #endregion
}