using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.Department;
using JNPF.Systems.Entitys.Dto.Organize;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 业务实现：部门管理.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Organize", Order = 166)]
[Route("api/permission/[controller]")]
public class DepartmentService : IDepartmentService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 部门管理仓储.
    /// </summary>
    private readonly ISqlSugarRepository<OrganizeEntity> _repository;

    /// <summary>
    /// 系统配置.
    /// </summary>
    private readonly ISysConfigService _sysConfigService;

    /// <summary>
    /// 组织管理.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 第三方同步.
    /// </summary>
    private readonly ISynThirdInfoService _synThirdInfoService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="DepartmentService"/>类型的新实例.
    /// </summary>
    public DepartmentService(
        ISqlSugarRepository<OrganizeEntity> repository,
        ISysConfigService sysConfigService,
        IOrganizeService organizeService,
        ISynThirdInfoService synThirdInfoService,
        IUserManager userManager)
    {
        _repository = repository;
        _sysConfigService = sysConfigService;
        _organizeService = organizeService;
        _synThirdInfoService = synThirdInfoService;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="companyId">公司主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("{companyId}/Department")]
    public async Task<dynamic> GetList(string companyId, [FromQuery] KeywordInput input)
    {
        List<DepartmentListOutput>? data = new List<DepartmentListOutput>();

        // 全部部门数据
        List<DepartmentListOutput>? departmentAllList = await _repository.AsSugarClient().Queryable<OrganizeEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.ManagerId))
            .Where(a => a.ParentId == companyId && a.Category.Equals("department") && a.DeleteMark == null)
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .Select((a, b) => new DepartmentListOutput
            {
                id = a.Id,
                parentId = a.ParentId,
                fullName = a.FullName,
                enCode = a.EnCode,
                description = a.Description,
                enabledMark = a.EnabledMark,
                creatorTime = a.CreatorTime,
                manager = SqlFunc.MergeString(b.RealName, "/", b.Account),
                sortCode = a.SortCode
            }).ToListAsync();

        // 当前公司部门
        List<OrganizeEntity>? departmentList = await _repository.AsQueryable().WhereIF(!string.IsNullOrEmpty(input.keyword), d => d.FullName.Contains(input.keyword) || d.EnCode.Contains(input.keyword))
            .Where(t => t.ParentId == companyId && t.Category.Equals("department") && t.DeleteMark == null)
            .OrderBy(a => a.SortCode)
            .OrderBy(a => a.CreatorTime, OrderByType.Desc)
            .ToListAsync();
        departmentList.ForEach(item =>
        {
            item.ParentId = "0";
            data.AddRange(departmentAllList.TreeChildNode(item.Id, t => t.id, t => t.parentId));
        });
        return new { list = data.OrderBy(x => x.sortCode).ToList() };
    }

    /// <summary>
    /// 获取下拉框.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Department/Selector/{id}")]
    public async Task<dynamic> GetSelector(string id)
    {
        // 获取组织树
        var orgTree = _organizeService.GetOrgListTreeName();

        List<OrganizeEntity>? data = await _repository.AsQueryable().Where(t => t.DeleteMark == null).OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();
        if (!"0".Equals(id)) data.RemoveAll(it => it.Id == id);

        List<DepartmentSelectorOutput>? treeList = data.Adapt<List<DepartmentSelectorOutput>>();
        treeList.ForEach(item =>
        {
            if (item.type != null && item.type.Equals("company")) item.icon = "icon-ym icon-ym-tree-organization3";
            item.organize = orgTree.FirstOrDefault(x => x.Id.Equals(item.id))?.Description;
            item.organizeIds = item.organizeIdTree.Split(",").ToList();
        });
        return new { list = treeList.OrderBy(x => x.sortCode).ToList().ToTree("-1") };
    }

    /// <summary>
    /// 获取下拉框根据权限.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Department/SelectorByAuth/{id}")]
    public async Task<dynamic> GetSelectorByAuth(string id)
    {
        // 获取组织树
        var orgTree = _organizeService.GetOrgListTreeName();

        // 获取分级管理组织
        var dataScope = _userManager.DataScope.Where(x => x.Select).Select(x => x.organizeId).Distinct().ToList();

        List<OrganizeEntity>? data = await _repository.AsQueryable().Where(t => t.DeleteMark == null)
            .WhereIF(!_userManager.IsAdministrator, x => dataScope.Contains(x.Id))
            .OrderBy(o => o.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();

        List<DepartmentSelectorOutput>? treeList = data.Adapt<List<DepartmentSelectorOutput>>();
        treeList.ForEach(item =>
        {
            if (item.type != null && item.type.Equals("company")) item.icon = "icon-ym icon-ym-tree-organization3";
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
                    var addItem = pItem.Adapt<DepartmentSelectorOutput>();

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
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("Department/{id}")]
    public async Task<dynamic> GetInfo(string id)
    {
        OrganizeEntity? entity = await _repository.GetSingleAsync(d => d.Id == id);
        var res = entity.Adapt<DepartmentInfoOutput>();
        if (entity.ParentId.Equals("-1")) res.organizeIdTree = new List<string>() { res.id };
        else res.organizeIdTree = (await _repository.GetSingleAsync(p => p.Id == entity.ParentId)).OrganizeIdTree.Split(",").ToList();
        return res;
    }

    #endregion

    #region POST

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("Department")]
    public async Task Create([FromBody] DepartmentCrInput input)
    {
        if (!_userManager.DataScope.Any(it => it.organizeId == input.parentId && it.Add) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);
        if (await _repository.IsAnyAsync(o => o.EnCode.Equals(input.enCode) && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2014);
        if (await _repository.IsAnyAsync(o => o.ParentId == input.parentId && o.FullName == input.fullName && o.Category == "department" && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2019);
        OrganizeEntity? entity = input.Adapt<OrganizeEntity>();
        entity.Category = "department";
        entity.Id = SnowflakeIdHelper.NextId();
        entity.EnabledMark = 1;
        entity.CreatorTime = DateTime.Now;
        entity.CreatorUserId = _userManager.UserId;

        #region  处理 上级ID列表 存储

        List<string>? idList = new List<string>();
        idList.Add(entity.Id);
        if (entity.ParentId != "-1")
        {
            List<string>? ids = _repository.AsQueryable().ToParentList(it => it.ParentId, entity.ParentId).Select(x => x.Id).ToList();
            idList.AddRange(ids);
        }

        idList.Reverse();
        entity.OrganizeIdTree = string.Join(",", idList);

        #endregion

        OrganizeEntity? newEntity = await _repository.AsInsertable(entity).CallEntityMethod(m => m.Create()).ExecuteReturnEntityAsync();
        _ = newEntity ?? throw Oops.Oh(ErrorCode.D2015);

        #region 默认赋予分级管理权限
        var adminlist = new List<OrganizeAdministratorEntity>();
        if (!_userManager.IsAdministrator)
        {
            adminlist.Add(new OrganizeAdministratorEntity()
            {
                UserId = _userManager.UserId,
                OrganizeId = newEntity.Id,
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
                OrganizeId = newEntity.Id,
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
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("Department/{id}")]
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
        if (await _repository.AsSugarClient().Queryable<UserRelationEntity>().AnyAsync(x => x.ObjectType == "Organize" && x.ObjectId == id))
            throw Oops.Oh(ErrorCode.D2004);

        // 该机构下有角色，则不能删
        if (await _repository.AsSugarClient().Queryable<OrganizeRelationEntity>().AnyAsync(x => x.OrganizeId == id && x.ObjectType == "Role"))
            throw Oops.Oh(ErrorCode.D2020);
        OrganizeEntity? entity = await _repository.GetSingleAsync(o => o.Id == id && o.DeleteMark == null);
        _ = entity ?? throw Oops.Oh(ErrorCode.D2002);

        int isOK = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId }).ExecuteCommandAsync();
        if (!(isOK > 0))
        {
            throw Oops.Oh(ErrorCode.D2017);
        }
        else
        {
            // 删除该组织和角色关联数据
            await _repository.AsSugarClient().Deleteable<OrganizeRelationEntity>().Where(x => x.OrganizeId == id && x.ObjectType == "Role").ExecuteCommandAsync();
        }

        #region 第三方数据删除
        try
        {
            SysConfigOutput? sysConfig = await _sysConfigService.GetInfo();
            if (sysConfig.dingSynIsSynOrg) await _synThirdInfoService.DelSynData(2, 1, sysConfig, id);
            if (sysConfig.qyhIsSynOrg) await _synThirdInfoService.DelSynData(1, 1, sysConfig, id);
        }
        catch (Exception)
        {
        }
        #endregion
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPut("Department/{id}")]
    public async Task Update(string id, [FromBody] DepartmentUpInput input)
    {
        OrganizeEntity? oldEntity = await _repository.GetSingleAsync(it => it.Id == id);
        if (oldEntity.ParentId != input.parentId && !_userManager.DataScope.Any(it => it.organizeId == oldEntity.ParentId && it.Edit) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        if (oldEntity.ParentId != input.parentId && !_userManager.DataScope.Any(it => it.organizeId == input.parentId && it.Edit) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        if (!_userManager.DataScope.Any(it => it.organizeId == id && it.Edit) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        if (input.parentId.Equals(id))
            throw Oops.Oh(ErrorCode.D2001);

        // 父id不能为自己的子节点
        List<string>? childIdListById = await _organizeService.GetChildIdListWithSelfById(id);
        if (childIdListById.Contains(input.parentId))
            throw Oops.Oh(ErrorCode.D2001);
        if (await _repository.IsAnyAsync(o => o.EnCode == input.enCode && o.Id != id && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2014);
        if (await _repository.IsAnyAsync(o => o.ParentId == input.parentId && o.FullName == input.fullName && o.Id != id && o.Category == "department" && o.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D2019);
        OrganizeEntity? entity = input.Adapt<OrganizeEntity>();
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;

        #region 处理 上级ID列表 存储
        if (string.IsNullOrWhiteSpace(oldEntity.OrganizeIdTree) || entity.ParentId != oldEntity.ParentId)
        {
            List<string>? idList = new List<string>();
            idList.Add(entity.Id);
            if (entity.ParentId != "-1")
            {
                List<string>? ids = _repository.AsQueryable().ToParentList(it => it.ParentId, entity.ParentId).Select(x => x.Id).ToList();
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

                await _repository.AsUpdateable(oldEntityList).UpdateColumns(x => x.OrganizeIdTree).ExecuteCommandAsync(); // 批量修改 父级组织
            }
        }
        #endregion

        int isOK = await _repository.AsUpdateable(entity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();
        if (!(isOK > 0)) throw Oops.Oh(ErrorCode.D2018);

        #region 第三方同步
        try
        {
            SysConfigOutput? sysConfig = await _sysConfigService.GetInfo();
            List<OrganizeListOutput>? orgList = new List<OrganizeListOutput>();
            entity.Category = "department";
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
    /// 更新状态.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpPut("Department/{id}/Actions/State")]
    public async Task UpdateState(string id)
    {
        if (!_userManager.DataScope.Any(it => it.organizeId == id && it.Edit == true) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);
        OrganizeEntity? entity = await _repository.GetFirstAsync(o => o.Id == id);
        _ = entity.EnabledMark == 1 ? 0 : 1;
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;

        int isOk = await _repository.AsUpdateable(entity).UpdateColumns(o => new { o.EnabledMark, o.LastModifyTime, o.LastModifyUserId }).ExecuteCommandAsync();
        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D2016);
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 获取部门列表(其他服务使用).
    /// </summary>
    /// <returns></returns>
    [NonAction]
    public async Task<List<OrganizeEntity>> GetListAsync()
    {
        return await _repository.AsQueryable().Where(t => t.Category.Equals("department") && t.EnabledMark == 1 && t.DeleteMark == null).OrderBy(o => o.SortCode).ToListAsync();
    }

    /// <summary>
    /// 部门名称.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public string GetDepName(string id)
    {
        OrganizeEntity? entity = _repository.GetFirst(x => x.Id == id && x.Category == "department" && x.EnabledMark == 1 && x.DeleteMark == null);
        return entity == null ? string.Empty : entity.FullName;
    }

    /// <summary>
    /// 公司名称.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public string GetComName(string id)
    {
        string? name = string.Empty;
        OrganizeEntity? entity = _repository.GetFirst(x => x.Id == id && x.EnabledMark == 1 && x.DeleteMark == null);
        if (entity == null)
        {
            return name;
        }
        else
        {
            if (entity.Category == "company")
            {
                return entity.FullName;
            }
            else
            {
                OrganizeEntity? pEntity = _repository.GetFirst(x => x.Id == entity.ParentId && x.EnabledMark == 1 && x.DeleteMark == null);
                return GetComName(pEntity.Id);
            }
        }
    }

    /// <summary>
    /// 公司结构名称树.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public string GetOrganizeNameTree(string id)
    {
        string? names = string.Empty;

        // 组织结构
        List<string>? olist = _repository.AsQueryable().ToParentList(it => it.ParentId, id).Select(x => x.FullName).ToList();
        olist.Reverse();
        names = string.Join("/", olist);

        return names;
    }

    /// <summary>
    /// 公司id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public string GetCompanyId(string id)
    {
        OrganizeEntity? entity = _repository.GetFirst(x => x.Id == id && x.EnabledMark == 1 && x.DeleteMark == null);
        if (entity == null)
        {
            return string.Empty;
        }
        else
        {
            if (entity.Category == "company")
            {
                return entity.Id;
            }
            else
            {
                OrganizeEntity? pEntity = _repository.GetFirst(x => x.Id == entity.ParentId && x.EnabledMark == 1 && x.DeleteMark == null);
                return GetCompanyId(pEntity.Id);
            }
        }
    }

    /// <summary>
    /// 获取公司下所有部门.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<List<OrganizeEntity>> GetCompanyAllDep(string id)
    {
        return await _repository.GetListAsync(x => x.OrganizeIdTree.Contains(id) && x.Category == "department" && x.EnabledMark == 1 && x.DeleteMark == null);
    }
    #endregion
}