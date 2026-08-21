using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Systems.Entitys.Dto.Organize;
using JNPF.Systems.Entitys.Dto.Permission.User;
using JNPF.Systems.Entitys.Dto.Role;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Dto.UserRelation;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Entitys.Enum;
using JNPF.Systems.Interfaces.Permission;
using JNPF.WorkFlow.Interfaces.Repository;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Threading;

namespace JNPF.Systems;

/// <summary>
///  业务实现：用户信息查询（CR-20260819-01 阶段 3 自 UsersService 剥离）.
///  路由契约：类级特性与 UsersService 完全一致，[controller] 仍解析为 users，
///  端点路径 /api/permission/users/* 逐条不变（仅移动，行为零变更）.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Users", Order = 163)]
[Route("api/permission/[controller]")]
public class UsersQueryService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<UserEntity> _repository;  // 用户表仓储

    /// <summary>
    /// 机构表服务.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 用户关系表服务.
    /// </summary>
    private readonly IUserRelationService _userRelationService;

    /// <summary>
    /// 流程相关.
    /// </summary>
    private readonly IFlowTaskRepository _flowTaskRepository;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 初始化一个<see cref="UsersQueryService"/>类型的新实例.
    /// </summary>
    public UsersQueryService(
        ISqlSugarRepository<UserEntity> userRepository,
        IOrganizeService organizeService,
        IUserRelationService userRelationService,
        IFlowTaskRepository flowTaskRepository,
        IUserManager userManager)
    {
        _repository = userRepository;
        _organizeService = organizeService;
        _userRelationService = userRelationService;
        _flowTaskRepository = flowTaskRepository;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 获取列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<PageResult<UserListOutput>> GetList([FromQuery] UserListQuery input, CancellationToken cancellationToken = default)
    {
        // 获取分级管理组织
        var dataScope = _userManager.DataScope.Where(x => x.Select).Select(x => x.organizeId).Distinct().ToList();

        PageInputBase? pageInput = input.Adapt<PageInputBase>();

        // 处理组织树 名称
        List<OrganizeEntity>? orgTreeNameList = _organizeService.GetOrgListTreeName();

        #region 获取组织层级

        List<string>? childOrgIds = new List<string>();
        if (input.organizeId.IsNotEmptyOrNull())
        {
            childOrgIds.Add(input.organizeId);

            // 根据组织Id 获取所有子组织Id集合
            childOrgIds.AddRange(orgTreeNameList.Where(x => x.OrganizeIdTree.Contains(input.organizeId)).Select(x => x.Id).ToList());
            childOrgIds = childOrgIds.Distinct().ToList();
        }

        #endregion

        // 获取配置文件 账号锁定类型
        SysConfigEntity? config = await _repository.AsSugarClient().Queryable<SysConfigEntity>().Where(x => x.Key.Equals("lockType") && x.Category.Equals("SysConfig")).FirstAsync();
        ErrorStrategy configLockType = (ErrorStrategy)Enum.Parse(typeof(ErrorStrategy), config?.Value);

        SqlSugarPagedList<UserListOutput>? data = new SqlSugarPagedList<UserListOutput>();

        // 性别字典类型
        var dictionaryTypeEntity2 = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "sex" && x.DeleteMark == null && x.EnabledMark == 1);

        data = await _repository.AsQueryable()
            .Where(a => a.DeleteMark == null && !a.Account.Equals("admin"))
            .WhereIF(input.enabledMark != null, a => a.EnabledMark.Equals(input.enabledMark))
            .WhereIF(input.gender != null, a => a.Gender.Equals(input.gender))
            .WhereIF(childOrgIds.Any(), a => SqlFunc.Subqueryable<UserRelationEntity>().Where(x => childOrgIds.Contains(x.ObjectId) && x.UserId.Equals(a.Id)).Any())
            .WhereIF(!input.keyword.IsNullOrEmpty(), a => a.Account.Contains(input.keyword) || a.RealName.Contains(input.keyword) || a.MobilePhone.Contains(input.keyword))
            .WhereIF(!_userManager.IsAdministrator, a => SqlFunc.Subqueryable<UserRelationEntity>().Where(x => dataScope.Contains(x.ObjectId) && x.UserId.Equals(a.Id)).Any())
            .OrderBy(a => a.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).OrderByIF(!input.keyword.IsNullOrEmpty(), a => a.LastModifyTime, OrderByType.Desc)
            .Select(a => new UserListOutput
            {
                id = a.Id,
                account = a.Account,
                realName = a.RealName,
                headIcon = SqlFunc.Subqueryable<UserEntity>().Where(e => e.Id == a.Id).Select(e => SqlFunc.MergeString("/api/File/Image/userAvatar/", e.HeadIcon)),
                creatorTime = a.CreatorTime,
                gender = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(d => d.DictionaryTypeId == dictionaryTypeEntity2.Id && d.EnCode == a.Gender && d.DeleteMark == null && d.EnabledMark == 1).Select(d => d.FullName),
                mobilePhone = a.MobilePhone,
                sortCode = a.SortCode,
                isAdministrator = a.IsAdministrator,
                enabledMark = SqlFunc.IIF(configLockType == ErrorStrategy.Delay && a.EnabledMark == 2 && a.UnLockTime < DateTime.Now, 1, a.EnabledMark),
                handoverMark = SqlFunc.IIF(SqlFunc.IsNullOrEmpty(a.HandoverUserId), 0, 1)
            }).ToPagedListAsync(input.currentPage, input.pageSize);

        #region 处理 用户 多组织

        List<UserRelationEntity>? orgUserIdAll = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => data.list.Select(u => u.id).Contains(x.UserId)).ToListAsync();
        foreach (UserListOutput? item in data.list)
        {
            // 获取用户组织集合
            item.organizeList = orgUserIdAll.Where(x => x.UserId == item.id).Select(x => x.ObjectId).ToList();
            item.organize = string.Join(" ; ", orgTreeNameList.Where(x => item.organizeList.Contains(x.Id)).Select(x => x.Description));
        }

        #endregion

        return new PageResult<UserListOutput>() { list = data.list.ToList(), pagination = data.pagination.Adapt<PageResult>() };
    }
    /// <summary>
    /// 获取全部用户.
    /// </summary>
    /// <returns></returns>
    [HttpGet("All")]
    public async Task<dynamic> GetUserAllList(CancellationToken cancellationToken = default)
    {
        // 多表查询 Where/OrderBy 必须显式指向第一表 a（UserEntity）；原代码用 p 触发 SqlSugar 别名不一致 500（存量缺陷，用户批复修复）
        return await _repository.AsSugarClient().Queryable<UserEntity, OrganizeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.OrganizeId))
            .Where((a, b) => a.EnabledMark == 1 && a.DeleteMark == null).OrderBy((a, b) => a.SortCode)
            .Select((a, b) => new UserListAllOutput
            {
                id = a.Id,
                account = a.Account,
                realName = a.RealName,
                headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", a.HeadIcon),
                gender = a.Gender,
                department = b.FullName,
                sortCode = a.SortCode,
                quickQuery = a.QuickQuery,
            }).ToListAsync();
    }

    /// <summary>
    /// 获取用户数据分页 根据角色Id.
    /// </summary>
    /// <returns></returns>
    [HttpGet("getUsersByRoleId")]
    public async Task<dynamic> GetUsersByRoleId([FromQuery] RoleListInput input, CancellationToken cancellationToken = default)
    {
        RoleEntity? roleInfo = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.Id == input.roleId).FirstAsync();

        // 查询全部用户 (全局角色)
        if (roleInfo.GlobalMark == 1)
        {
            SqlSugarPagedList<UserListAllOutput>? list = await _repository.AsQueryable()
                .WhereIF(!input.keyword.IsNullOrEmpty(), a => a.Account.Contains(input.keyword) || a.RealName.Contains(input.keyword))
                .Where(p => p.EnabledMark == 1 && p.DeleteMark == null).OrderBy(p => p.SortCode)
                .Select(a => new UserListAllOutput
                {
                    id = a.Id,
                    account = a.Account,
                    realName = a.RealName,
                    gender = a.Gender,
                    sortCode = a.SortCode,
                    quickQuery = a.QuickQuery
                }).ToPagedListAsync(input.currentPage, input.pageSize);

            return PageResult<UserListAllOutput>.SqlSugarPageResult(list);
        }

        // 查询角色 所属 所有组织 用户
        else
        {
            // 查询角色 所有所属组织
            List<string>? orgList = await _repository.AsSugarClient().Queryable<OrganizeRelationEntity>().Where(x => x.ObjectType == "Role" && x.ObjectId == roleInfo.Id).Select(x => x.OrganizeId).ToListAsync();

            List<string>? userIdList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType == "Organize" && orgList.Contains(x.ObjectId)).Select(x => x.UserId).Distinct().ToListAsync();

            SqlSugarPagedList<UserListAllOutput>? list = await _repository.AsQueryable()
                .Where(a => userIdList.Contains(a.Id))
                .Where(p => p.EnabledMark == 1 && p.DeleteMark == null).OrderBy(p => p.SortCode)
                .WhereIF(!input.keyword.IsNullOrEmpty(), a => a.Account.Contains(input.keyword) || a.RealName.Contains(input.keyword))
                .Select(a => new UserListAllOutput
                {
                    id = a.Id,
                    account = a.Account,
                    realName = a.RealName,
                    gender = a.Gender,
                    sortCode = a.SortCode,
                    quickQuery = a.QuickQuery,
                }).ToPagedListAsync(input.currentPage, input.pageSize);

            return PageResult<UserListAllOutput>.SqlSugarPageResult(list);
        }
    }

    /// <summary>
    /// 获取用户数据 根据角色所属组织.
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetUsersByRoleOrgId")]
    public async Task<dynamic> GetUsersByRoleOrgId([FromQuery] RoleListInput input, CancellationToken cancellationToken = default)
    {
        RoleEntity? roleInfo = await _repository.AsSugarClient().Queryable<RoleEntity>().Where(x => x.Id == input.roleId).FirstAsync();
        input.organizeId = input.organizeId == null ? "0" : input.organizeId;

        // 获取角色所属组织集合
        List<string>? orgList = await _repository.AsSugarClient().Queryable<OrganizeRelationEntity>().Where(x => x.ObjectType == "Role" && x.ObjectId == roleInfo.Id).Select(x => x.OrganizeId).ToListAsync();

        var orgTreeNameList = _organizeService.GetOrgListTreeName();

        List<OrganizeMemberListOutput>? output = new List<OrganizeMemberListOutput>();
        if (input.organizeId.Equals("0"))
        {
            if (input.keyword.IsNotEmptyOrNull())
            {
                // 获取角色所属组织 成员id
                var res = await _repository.AsSugarClient().Queryable<UserEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.UserId == a.Id))
                .Where((a, b) => b.ObjectType == "Organize" && orgList.Contains(b.ObjectId)).Where((a, b) => a.EnabledMark == 1 && a.DeleteMark == null)
                .Where((a, b) => a.RealName.Contains(input.keyword) || a.Account.Contains(input.keyword))
                .GroupBy((a, b) => new { a.Id, a.RealName, a.Account, a.EnabledMark })
                .Select((a, b) => new {
                    id = a.Id,
                    fullName = SqlFunc.MergeString(a.RealName, "/", a.Account),
                    enabledMark = a.EnabledMark,
                    type = "user",
                    icon = "icon-ym icon-ym-tree-user2",
                    hasChildren = false,
                    isLeaf = true
                }).ToListAsync();
                output.AddRange(res.Adapt<List<OrganizeMemberListOutput>>());
            }
            else
            {
                List<OrganizeEntity>? allOrg = _organizeService.GetOrgListTreeName();

                List<OrganizeEntity>? data = await _repository.AsSugarClient().Queryable<OrganizeEntity>()
                    .Where(o => orgList.Contains(o.Id) && o.DeleteMark == null && o.EnabledMark == 1)
                    .OrderBy(o => o.SortCode).ToListAsync();

                foreach (OrganizeEntity? o in data)
                {
                    if (o.OrganizeIdTree.IsNullOrEmpty()) o.OrganizeIdTree = o.Id;
                    if (!data.Where(x => x.Id != o.Id && o.OrganizeIdTree.Contains(x.OrganizeIdTree)).Any())
                    {
                        output.Add(new OrganizeMemberListOutput
                        {
                            id = o.Id,
                            fullName = allOrg.FirstOrDefault(x => x.Id.Equals(o.Id))?.Description,
                            enabledMark = o.EnabledMark,
                            type = o.Category,
                            icon = "icon-ym icon-ym-tree-organization3",
                            hasChildren = true,
                            isLeaf = false
                        });
                    }
                }
            }
        }
        else
        {
            List<OrganizeEntity>? allOrg = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(o => o.DeleteMark == null && o.EnabledMark == 1).OrderBy(o => o.ParentId).ToListAsync();

            var res = await _repository.AsSugarClient().Queryable<UserEntity, UserRelationEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.UserId == a.Id))
                .Where((a, b) => b.ObjectType == "Organize" && b.ObjectId == input.organizeId).Where((a, b) => a.EnabledMark == 1 && a.DeleteMark == null)
                .GroupBy((a, b) => new { a.Id, a.RealName, a.Account, a.EnabledMark })
                .Select((a, b) => new {
                    id = a.Id,
                    fullName = SqlFunc.MergeString(a.RealName, "/", a.Account),
                    enabledMark = a.EnabledMark,
                    type = "user",
                    icon = "icon-ym icon-ym-tree-user2",
                    hasChildren = false,
                    isLeaf = true
                }).ToListAsync();
            output.AddRange(res.Adapt<List<OrganizeMemberListOutput>>());
            var departmentList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(o => o.OrganizeIdTree.Contains(input.organizeId) && orgList.Contains(o.Id)).ToListAsync();

            departmentList.OrderBy(x => x.OrganizeIdTree.Length).ToList().ForEach(o =>
            {
                o.FullName = orgTreeNameList.FirstOrDefault(x => x.Id.Equals(o.Id)).Description;

                if (o.Id != input.organizeId && !output.Any(x => o.FullName.Contains(x.fullName)))
                {
                    var pName = string.Empty;
                    if (!departmentList.Any(x => x.Id == o.ParentId)) pName = orgTreeNameList.FirstOrDefault(x => x.Id.Equals(input.organizeId)).Description;
                    else pName = orgTreeNameList.FirstOrDefault(x => x.Id.Equals(o.ParentId)).Description;
                    output.Add(new OrganizeMemberListOutput()
                    {
                        id = o.Id,
                        fullName = o.FullName.Replace(pName + "/", string.Empty),
                        enabledMark = o.EnabledMark,
                        type = o.Category,
                        icon = o.Category.Equals("company") ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                        hasChildren = true,
                        isLeaf = false
                    });
                }
            });
        }

        // 获取 所属组织的所有成员
        List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.ObjectType == "Organize" && output.Select(x => x.id).Contains(x.UserId)).ToListAsync();

        // 用户头像
        var usersHead = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => output.Select(xx => xx.id).Contains(x.Id)).Select(x => new { id = x.Id, headIcon = x.HeadIcon }).ToListAsync();

        // 处理组织树
        output.ForEach(item =>
        {
            if (item.type.Equals("user"))
            {
                var head = usersHead.Find(x => x.id.Equals(item.id)).headIcon;
                item.headIcon = head.IsNullOrEmpty() ? "/api/file/Image/userAvatar/001.png" : "/api/file/Image/userAvatar/" + head;
            }
            var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
            var oTree = orgTreeNameList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
            item.organize = string.Join(",", oTree);
        });
        return output;
    }

    /// <summary>
    /// 获取IM用户列表.
    /// </summary>
    /// <returns></returns>
    [HttpGet("ImUser")]
    public async Task<dynamic> GetImUserList([FromQuery] PageInputBase input, CancellationToken cancellationToken = default)
    {
        SqlSugarPagedList<IMUserListOutput>? list = await _repository.AsSugarClient().Queryable<UserEntity, OrganizeEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.OrganizeId))
            .WhereIF(!input.keyword.IsNullOrEmpty(), a => a.Account.Contains(input.keyword) || a.RealName.Contains(input.keyword))
            .Where(a => a.Id != _userManager.UserId && a.EnabledMark == 1 && a.DeleteMark == null).OrderBy(a => a.SortCode)
            .Select((a, b) => new IMUserListOutput
            {
                id = a.Id,
                account = a.Account,
                realName = a.RealName,
                headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", a.HeadIcon),
                department = b.FullName,
            }).ToPagedListAsync(input.currentPage, input.pageSize);
        return PageResult<IMUserListOutput>.SqlSugarPageResult(list);
    }

    /// <summary>
    /// 获取下拉框（公司+部门+用户）.
    /// </summary>
    /// <returns></returns>
    [HttpGet("Selector")]
    public async Task<dynamic> GetSelector(CancellationToken cancellationToken = default)
    {
        List<OrganizeEntity>? organizeList = await _organizeService.GetListAsync();
        List<UserEntity>? userList = await _repository.AsQueryable().Where(t => t.EnabledMark == 1 && t.DeleteMark == null).OrderBy(u => u.SortCode).OrderBy(a => a.CreatorTime, OrderByType.Desc).ToListAsync();
        List<UserSelectorOutput>? organizeTreeList = organizeList.Adapt<List<UserSelectorOutput>>();
        List<UserSelectorOutput>? treeList = userList.Adapt<List<UserSelectorOutput>>();
        treeList = treeList.Concat(organizeTreeList).ToList();
        return new { list = treeList.OrderBy(x => x.sortCode).ToList().ToTree("-1") };
    }

    /// <summary>
    /// 获取信息.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    public async Task<dynamic> GetInfo(string id, CancellationToken cancellationToken = default)
    {
        UserEntity? entity = await _repository.GetFirstAsync(u => u.Id == id);
        SysConfigEntity? config = await _repository.AsSugarClient().Queryable<SysConfigEntity>().Where(x => x.Key.Equals("lockType") && x.Category.Equals("SysConfig")).FirstAsync();
        string? configLockType = config?.Value;
        entity.EnabledMark = configLockType.IsNotEmptyOrNull() && configLockType == "2" && entity.EnabledMark == 2 && entity.UnLockTime < DateTime.Now ? 1 : entity.EnabledMark;
        UserInfoOutput? output = entity.Adapt<UserInfoOutput>();
        if (output.headIcon == "/api/File/Image/userAvatar/") output.headIcon = string.Empty;
        if (entity != null)
        {
            List<UserRelationEntity>? allRelationList = await _userRelationService.GetListByUserId(id);
            var relationIds = allRelationList.Where(x => x.ObjectType == "Organize" || x.ObjectType == "Position").Select(x => new { x.ObjectId, x.ObjectType }).ToList();
            List<OrganizeEntity>? oList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => relationIds.Where(x => x.ObjectType == "Organize").Select(x => x.ObjectId).Contains(x.Id)).ToListAsync();
            output.organizeIdTree = new List<List<string>>();
            oList.ForEach(item =>
            {
                if (item.OrganizeIdTree.IsNotEmptyOrNull()) output.organizeIdTree.Add(item.OrganizeIdTree.Split(",").ToList());
            });
            output.organizeId = string.Join(",", relationIds.Where(x => x.ObjectType == "Organize").Select(x => x.ObjectId));
            output.positionId = string.Join(",", relationIds.Where(x => x.ObjectType == "Position").Select(x => x.ObjectId));
        }

        return output;
    }

    /// <summary>
    /// 获取当前用户所属机构下属成员.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("getOrganization")]
    public async Task<dynamic> GetOrganizeMember([FromQuery] UserListQuery input, CancellationToken cancellationToken = default)
    {
        if (input.organizeId.IsNotEmptyOrNull() && input.organizeId != "0") input.organizeId = input.organizeId.Split(",").LastOrDefault();
        else input.organizeId = _userManager.User.OrganizeId;

        // 获取所属组织的所有成员
        List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.ObjectType == "Organize").ToListAsync();

        var res = await _repository.AsQueryable()
                .WhereIF(!input.keyword.IsNullOrEmpty(), u => u.Account.Contains(input.keyword) || u.RealName.Contains(input.keyword))
                .Where(u => u.EnabledMark == 1 && u.DeleteMark == null && userList.Where(x => x.ObjectId == input.organizeId).Select(x => x.UserId).Contains(u.Id)).OrderBy(o => o.SortCode)
                .Select(u => new OrganizeMemberListOutput
                {
                    id = u.Id,
                    fullName = SqlFunc.MergeString(u.RealName, "/", u.Account),
                    enabledMark = u.EnabledMark,
                    icon = "icon-ym icon-ym-tree-user2",
                    headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", u.HeadIcon),
                    isLeaf = true,
                    hasChildren = false,
                    type = "user",
                }).ToListAsync();

        if (res.Any())
        {
            var orgList = _organizeService.GetOrgListTreeName();

            // 处理组织树
            res.ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });
        }

        return res;
    }

    /// <summary>
    /// 获取工作交接内容.
    /// </summary>
    /// <param name="fromId">移交人Id.</param>
    /// <returns></returns>
    [HttpGet("getWorkByUser")]
    public async Task<dynamic> GetWorkByUser([FromQuery] string fromId, CancellationToken cancellationToken = default)
    {
        var res = new UserWorkHandoverModel();
        res.permission = await _repository.AsSugarClient().Queryable<PermissionGroupEntity>().Where(x => x.PermissionMember.Contains(fromId) && x.DeleteMark == null)
            .Select(x => new PermissionGroupListSelector() { id = x.Id, enCode = x.EnCode, fullName = x.FullName, icon = "icon-ym icon-ym-authGroup" }).ToListAsync();

        res.wait = await _flowTaskRepository.GetWorkHandover(fromId, 1);
        res.flow = await _flowTaskRepository.GetWorkHandover(fromId, 2);

        return res;
    }

    #endregion

    #region POST

    /// <summary>
    /// 根据用户Id List 获取当前用户Id.
    /// </summary>
    /// <returns></returns>
    [HttpPost("getDefaultCurrentValueUserId")]
    public async Task<dynamic> GetDefaultCurrentValueUserId([FromBody] GetDefaultCurrentValueInput input, CancellationToken cancellationToken = default)
    {
        if ((input.UserIds == null || !input.UserIds.Any()) && (input.DepartIds == null || !input.DepartIds.Any()) && (input.PositionIds == null || !input.PositionIds.Any())
            && (input.RoleIds == null || !input.RoleIds.Any()) && (input.GroupIds == null || !input.GroupIds.Any())) return new { userId = _userManager.UserId };

        var userRelationList = _repository.AsSugarClient().Queryable<UserRelationEntity>().Select(x => new UserRelationEntity() { UserId = x.UserId, ObjectId = x.ObjectId }).ToList();
        var userIdList = userRelationList.Where(x => input.UserIds.Contains(x.UserId) || input.DepartIds.Contains(x.ObjectId)
            || input.PositionIds.Contains(x.ObjectId) || input.RoleIds.Contains(x.ObjectId) || input.GroupIds.Contains(x.ObjectId)).Select(x => x.UserId).ToList();

        if (userIdList.Contains(_userManager.UserId)) return new { userId = _userManager.UserId };
        else return new { userId = string.Empty };
    }

    /// <summary>
    /// 获取.
    /// </summary>
    /// <returns></returns>
    [HttpPost("GetUserList")]
    public async Task<dynamic> GetUserList([FromBody] UserRelationInput input, CancellationToken cancellationToken = default)
    {
        var data = await _repository.AsQueryable().Where(it => it.EnabledMark > 0 && it.DeleteMark == null)
            .Where(it => input.ids.Contains(it.Id))
            .Select(it => new OrganizeMemberListOutput()
            {
                id = it.Id,
                fullName = SqlFunc.MergeString(it.RealName, "/", it.Account),
                headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", it.HeadIcon),
                enabledMark = it.EnabledMark,
                isAdministrator = it.IsAdministrator,
            }).ToListAsync();

        data = data.OrderBy(x => input.ids.IndexOf(x.id)).ToList();
        if (data.Any())
        {
            var orgList = _organizeService.GetOrgListTreeName();

            // 获取 所属组织的所有成员
            List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.ObjectType == "Organize" && data.Select(x => x.id).Contains(x.UserId)).ToListAsync();

            // 处理组织树
            data.ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });
        }

        return new { list = data };
    }

    /// <summary>
    /// 获取机构成员列表.
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("ImUser/Selector/{organizeId}")]
    public async Task<dynamic> GetOrganizeMemberList(string organizeId, [FromBody] PageInputBase input, CancellationToken cancellationToken = default)
    {
        List<OrganizeMemberListOutput>? output = new List<OrganizeMemberListOutput>();
        var orgList = _organizeService.GetOrgListTreeName();
        if (!input.keyword.IsNullOrEmpty())
        {
            var outList = await _repository.AsQueryable()
                .WhereIF(!input.keyword.IsNullOrEmpty(), u => u.Account.Contains(input.keyword) || u.RealName.Contains(input.keyword))
                .Where(u => u.EnabledMark > 0 && u.DeleteMark == null).OrderBy(o => o.SortCode)
                .Select(u => new OrganizeMemberListOutput
                {
                    id = u.Id,
                    fullName = SqlFunc.MergeString(u.RealName, "/", u.Account),
                    enabledMark = SqlFunc.IIF(u.EnabledMark == 2 && u.UnLockTime < DateTime.Now, 1, u.EnabledMark),
                    icon = "icon-ym icon-ym-tree-user2",
                    headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", u.HeadIcon),
                    isLeaf = true,
                    hasChildren = false,
                    type = "user",
                    isAdministrator = u.IsAdministrator,
                }).ToPagedListAsync(input.currentPage, input.pageSize);

            if (outList.list.Any())
            {
                // 获取 所属组织的所有成员
                List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
                    .Where(x => x.ObjectType == "Organize" && outList.list.Select(x => x.id).Contains(x.UserId)).ToListAsync();

                // 处理组织树
                outList.list.Where(x => x.type.Equals("user")).ToList().ForEach(item =>
                {
                    var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                    var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                    item.organize = string.Join(",", oTree);
                });
            }

            return PageResult<OrganizeMemberListOutput>.SqlSugarPageResult(outList);
        }
        else
        {
            var pOrganize = orgList.FirstOrDefault(x => x.Id.Equals(organizeId));

            output = await _organizeService.GetOrganizeMemberList(organizeId);
            if (pOrganize != null) output.ForEach(item => item.fullName = item.fullName.Replace(pOrganize.FullName + "/", string.Empty));
        }

        if (output.Any())
        {
            // 获取 所属组织的所有成员
            List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.ObjectType == "Organize" && output.Select(x => x.id).Contains(x.UserId)).ToListAsync();

            // 处理组织树
            output.Where(x => x.type.Equals("user")).ToList().ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });
        }

        return new { list = output };
    }

    /// <summary>
    /// 获取下拉框 根据权限.
    /// </summary>
    /// <returns></returns>
    [HttpPost("GetListByAuthorize/{organizeId}")]
    public async Task<dynamic> GetListByAuthorize(string organizeId, [FromBody] KeywordInput input, CancellationToken cancellationToken = default)
    {
        List<OrganizeMemberListOutput>? output = new List<OrganizeMemberListOutput>();
        if (!input.keyword.IsNullOrEmpty())
        {
            var dataScope = _userManager.DataScope.Where(x => x.Select).Select(x => x.organizeId).ToList();
            var userIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => dataScope.Contains(x.ObjectId)).Select(x => x.UserId).ToListAsync();

            output = await _repository.AsQueryable()
                .Where(u => u.Account.Contains(input.keyword) || u.RealName.Contains(input.keyword))
                .Where(u => u.EnabledMark > 0 && u.DeleteMark == null && userIds.Contains(u.Id)).OrderBy(o => o.SortCode)
                .Select(u => new OrganizeMemberListOutput
                {
                    id = u.Id,
                    fullName = SqlFunc.MergeString(u.RealName, "/", u.Account),
                    enabledMark = SqlFunc.IIF(u.EnabledMark == 2 && u.UnLockTime < DateTime.Now, 1, u.EnabledMark),
                    icon = "icon-ym icon-ym-tree-user2",
                    headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", u.HeadIcon),
                    isLeaf = true,
                    hasChildren = false,
                    type = "user",
                }).Take(50).ToListAsync();

        }
        else
        {
            output = await GetOrganizeMemberList(organizeId);
        }

        if (output.Any())
        {
            var orgList = _organizeService.GetOrgListTreeName();
            // 获取所属组织的所有成员
            List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.ObjectType == "Organize" && output.Select(x => x.id).Contains(x.UserId)).ToListAsync();

            // 处理组织树
            output.Where(x => x.type.Equals("user")).ToList().ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });
        }

        return new { list = output.DistinctBy(x => x.id).ToList() };
    }

    /// <summary>
    /// 获取当前用户下属成员.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("getSubordinates")]
    public async Task<dynamic> GetSubordinate([FromBody] KeywordInput input, CancellationToken cancellationToken = default)
    {
        var res = await _repository.AsQueryable()
                   .WhereIF(!input.keyword.IsNullOrEmpty(), u => u.Account.Contains(input.keyword) || u.RealName.Contains(input.keyword))
                   .Where(u => u.EnabledMark == 1 && u.DeleteMark == null && u.ManagerId == _userManager.UserId).OrderBy(o => o.SortCode)
                   .Select(u => new OrganizeMemberListOutput
                   {
                       id = u.Id,
                       fullName = SqlFunc.MergeString(u.RealName, "/", u.Account),
                       enabledMark = u.EnabledMark,
                       icon = "icon-ym icon-ym-tree-user2",
                       headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", u.HeadIcon),
                       isLeaf = true,
                       hasChildren = false,
                       type = "user",
                   }).ToListAsync();

        // 获取所属组织的所有成员（原文此处为 GBK 损坏乱码注释，随迁时恢复原义）
        List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => res.Select(xx => xx.id).Contains(x.UserId)).ToListAsync();

        if (res.Any())
        {
            var orgList = _organizeService.GetOrgListTreeName();

            // 处理组织树（原文此处为 GBK 损坏乱码注释，随迁时恢复原义）
            res.ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });
        }

        return res;
    }

    /// <summary>
    /// 获取当前用户所属机构下属成员.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("GetUsersByPositionId")]
    public async Task<dynamic> GetUsersByPositionId([FromQuery] UserListQuery input, CancellationToken cancellationToken = default)
    {
        List<OrganizeMemberListOutput>? outData = new List<OrganizeMemberListOutput>();
        UserEntity? user = _userManager.User;

        // 获取岗位所属组织信息
        OrganizeMemberListOutput? orgInfo = await _repository.AsSugarClient().Queryable<PositionEntity, OrganizeEntity>((a, b) =>
                new JoinQueryInfos(JoinType.Left, b.Id == a.OrganizeId && b.EnabledMark == 1 && b.DeleteMark == null))
            .Where((a, b) => a.Id == input.positionId).Select((a, b) => new OrganizeMemberListOutput
            {
                id = b.Id,
                fullName = b.FullName,
                enabledMark = b.EnabledMark,
                type = b.Category,
                parentId = "0",
                organize = b.Id,
                icon = b.Category.Equals("company") ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                hasChildren = true,
                isLeaf = false
            }).FirstAsync();

        var orgList = _organizeService.GetOrgListTreeName();

        // 处理组织树
        if (orgInfo.organize.IsNotEmptyOrNull())
        {
            orgInfo.fullName = orgList.FirstOrDefault(x => x.Id.Equals(orgInfo.organize))?.Description;
        }

        outData.Add(orgInfo);

        // 获取岗位所属组织的所有成员
        List<OrganizeMemberListOutput>? userData = await _repository.AsSugarClient().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.UserId))
            .Where((a, b) => a.ObjectType == "Organize" && a.ObjectId == orgInfo.id && b.EnabledMark == 1 && b.DeleteMark == null)
            .WhereIF(!input.keyword.IsNullOrEmpty(), (a, b) => b.Account.Contains(input.keyword) || b.RealName.Contains(input.keyword))
            .Select((a, b) => new OrganizeMemberListOutput
            {
                id = b.Id,
                parentId = orgInfo.id,
                fullName = SqlFunc.MergeString(b.RealName, "/", b.Account),
                enabledMark = b.EnabledMark,
                icon = "icon-ym icon-ym-tree-user2",
                headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", b.HeadIcon),
                isLeaf = true,
                hasChildren = false,
                type = "user"
            }).ToListAsync();

        // 获取 所属组织的所有成员
        List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
            .Where(x => x.ObjectType == "Organize" && userData.Select(x => x.id).Contains(x.UserId)).ToListAsync();

        // 处理组织树
        userData.ForEach(item =>
        {
            var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
            var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
            item.organize = string.Join(",", oTree);
        });
        outData.AddRange(userData);

        return outData.ToTree("0");
    }

    /// <summary>
    /// 通过部门、岗位、用户、角色、分组id获取用户列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("UserCondition")]
    public async Task<dynamic> UserCondition([FromBody] UserConditionInput input, CancellationToken cancellationToken = default)
    {
        SqlSugarPagedList<UserListOutput>? data = new SqlSugarPagedList<UserListOutput>();

        if (input.departIds == null) input.departIds = new List<string>();
        if (input.positionIds != null) input.departIds.AddRange(input.positionIds);
        if (input.roleIds != null) input.departIds.AddRange(input.roleIds);
        if (input.groupIds != null) input.departIds.AddRange(input.groupIds);
        if (data.list == null) data.list = new List<UserListOutput>();
        if (!input.departIds.Any()) return PageResult<UserListOutput>.SqlSugarPageResult(data);
        var ids = await _repository.AsSugarClient().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.UserId))
            .Where((a, b) => b.DeleteMark == null)
            .WhereIF(input.departIds.Any() || input.userIds.Any(), (a, b) => input.departIds.Contains(a.ObjectId) || input.userIds.Contains(b.Id))
            .WhereIF(input.pagination.keyword.IsNotEmptyOrNull(), (a, b) => b.Account.Contains(input.pagination.keyword) || b.RealName.Contains(input.pagination.keyword))
            .Select((a, b) => b.Id).Distinct().ToListAsync();
        data = await _repository.AsQueryable().Where(x => ids.Contains(x.Id)).Select(x => new UserListOutput()
        {
            id = x.Id,
            organizeId = x.OrganizeId,
            account = x.Account,
            fullName = SqlFunc.MergeString(x.RealName, "/", x.Account),
            headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", x.HeadIcon),
            gender = x.Gender,
            mobilePhone = x.MobilePhone
        }).ToPagedListAsync(input.pagination.currentPage, input.pagination.pageSize);
        if (data.list.Any())
        {
            var orgList = _organizeService.GetOrgListTreeName();

            // 获取所属组织的所有成员
            List<UserRelationEntity>? userList = await _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.ObjectType == "Organize" && data.list.Select(x => x.id).Contains(x.UserId)).ToListAsync();

            // 处理组织树
            data.list.ToList().ForEach(item =>
            {
                var oids = userList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).ToList();
                var oTree = orgList.Where(x => oids.Contains(x.Id)).Select(x => x.Description).ToList();
                item.organize = string.Join(",", oTree);
            });

        }

        return PageResult<UserListOutput>.SqlSugarPageResult(data);
    }

    /// <summary>
    /// 获取选中组织、岗位、角色、分组、用户基本信息.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("GetSelectedList")]
    public async Task<dynamic> GetSelectedList([FromBody] UserSelectedInput input, CancellationToken cancellationToken = default)
    {
        if (input.ids == null) return new { list = new List<UserSelectedOutput>() };

        var objIds = new List<string>();
        input.ids.Where(x => x.IsNotEmptyOrNull()).ToList().ForEach(item => objIds.Add(item.Split("--").First()));
        var orgInfoList = _organizeService.GetOrgListTreeName();

        var orgList = new List<OrganizeEntity>();
        var posList = new List<PositionEntity>();
        var roleList = new List<RoleEntity>();
        var groupList = new List<GroupEntity>();
        var userList = new List<UserEntity>();
        foreach (var item in objIds)
        {
            var org = orgInfoList.FirstOrDefault(x => item.Equals(x.Id));
            if (org.IsNotEmptyOrNull()) orgList.Add(org);
            var pos = await _repository.AsSugarClient().Queryable<PositionEntity>().FirstAsync(x => item.Equals(x.Id) && x.EnabledMark == 1 && x.DeleteMark == null);
            if (pos.IsNotEmptyOrNull()) posList.Add(pos);
            var role = await _repository.AsSugarClient().Queryable<RoleEntity>().FirstAsync(x => item.Equals(x.Id) && x.EnabledMark == 1 && x.DeleteMark == null);
            if (role.IsNotEmptyOrNull()) roleList.Add(role);
            var group = await _repository.AsSugarClient().Queryable<GroupEntity>().FirstAsync(x => item.Equals(x.Id) && x.EnabledMark == 1 && x.DeleteMark == null);
            if (group.IsNotEmptyOrNull()) groupList.Add(group);
            var user = await _repository.AsSugarClient().Queryable<UserEntity>().FirstAsync(x => item.Equals(x.Id) && x.EnabledMark > 0 && x.DeleteMark == null);
            if (user.IsNotEmptyOrNull()) userList.Add(user);
        }
        var resList = new List<UserSelectedOutput>();

        orgList.ForEach(item =>
        {
            resList.Add(new UserSelectedOutput()
            {
                id = item.Id,
                fullName = item.FullName,
                type = item.Category,
                icon = item.Category.Equals("company") ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                organize = item.Description,
                organizeIds = new List<string> { item.OrganizeIdTree },
            });
        });

        posList.ForEach(item =>
        {
            resList.Add(new UserSelectedOutput()
            {
                id = item.Id,
                fullName = item.FullName,
                type = "position",
                icon = "icon-ym icon-ym-tree-position1",
                organize = orgInfoList.Find(x => x.Id.Equals(item.OrganizeId)).Description,
                organizeIds = new List<string> { orgInfoList.Find(x => x.Id.Equals(item.OrganizeId)).OrganizeIdTree },
            });
        });

        var roleOrgList = await _repository.AsSugarClient().Queryable<OrganizeRelationEntity>().Where(x => roleList.Select(xx => xx.Id).Contains(x.ObjectId)).Select(x => new { x.ObjectId, x.OrganizeId }).ToListAsync();
        roleList.ForEach(item =>
        {
            resList.Add(new UserSelectedOutput()
            {
                id = item.Id,
                fullName = item.FullName,
                type = "role",
                organize = SqlFunc.IIF(item.GlobalMark == 1, "", string.Join(",", orgInfoList.Where(o => roleOrgList.Where(x => x.ObjectId.Equals(item.Id)).Select(x => x.OrganizeId).Contains(o.Id)).Select(x => x.Description))),
                icon = "icon-ym icon-ym-generator-role",
                organizeIds = orgInfoList.Where(o => roleOrgList.Where(x => x.ObjectId.Equals(item.Id)).Select(x => x.OrganizeId).Contains(o.Id)).Select(x => x.OrganizeIdTree).ToList(),
            });
        });

        groupList.ForEach(item =>
        {
            resList.Add(new UserSelectedOutput()
            {
                id = item.Id,
                fullName = item.FullName,
                type = "group",
                icon = "icon-ym icon-ym-generator-group1"
            });
        });

        var userOrgList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => userList.Select(xx => xx.Id).Contains(x.UserId) && x.ObjectType.Equals("Organize")).Select(x => new { x.ObjectId, x.UserId }).ToListAsync();
        userList.ForEach(item =>
        {
            resList.Add(new UserSelectedOutput()
            {
                id = item.Id,
                fullName = item.RealName + "/" + item.Account,
                type = "user",
                icon = "icon-ym icon-ym-tree-user2",
                headIcon = "/api/File/Image/userAvatar/" + item.HeadIcon,
                organize = string.Join(",", orgInfoList.Where(o => userOrgList.Where(x => x.UserId.Equals(item.Id)).Select(x => x.ObjectId).Contains(o.Id)).Select(x => x.Description)),
                organizeIds = orgInfoList.Where(o => userOrgList.Where(x => x.UserId.Equals(item.Id)).Select(x => x.ObjectId).Contains(o.Id)).Select(x => x.OrganizeIdTree).ToList(),
            });
        });

        if (objIds.Contains("@currentOrg"))
        {
            resList.Add(new UserSelectedOutput()
            {
                id = "@currentOrg",
                fullName = "当前组织",
                type = "system"
            });
        }
        if (objIds.Contains("@currentOrgAndSubOrg"))
        {
            resList.Add(new UserSelectedOutput()
            {
                id = "@currentOrgAndSubOrg",
                fullName = "当前组织及子组织",
                type = "system"
            });
        }
        if (objIds.Contains("@currentGradeOrg"))
        {
            resList.Add(new UserSelectedOutput()
            {
                id = "@currentGradeOrg",
                fullName = "当前分管组织",
                type = "system"
            });
        }

        return new { list = resList.OrderBy(x => objIds.IndexOf(x.id)) };
    }

    /// <summary>
    /// 获取用户基本信息.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("GetSelectedUserList")]
    public async Task<dynamic> GetSelectedUserList([FromBody] UserSelectedInput input, CancellationToken cancellationToken = default)
    {
        var userId = new List<string>();
        input.ids.ForEach(item => userId.Add(item.Split("--").First()));
        var orgInfoList = _organizeService.GetOrgListTreeName();

        if (userId.Contains("@currentOrg"))
        {
            userId.Add(_userManager.User.OrganizeId);
            userId.Remove("@currentOrg");
        }
        if (userId.Contains("@currentOrgAndSubOrg"))
        {
            userId.AddRange(orgInfoList.TreeChildNode(_userManager.User.OrganizeId, t => t.Id, t => t.ParentId).Select(it => it.Id).ToList());
            userId.Remove("@currentOrgAndSubOrg");
        }
        if (userId.Contains("@currentGradeOrg"))
        {
            if (_userManager.IsAdministrator)
            {
                userId.AddRange(orgInfoList.Select(it => it.Id).ToList());
            }
            else
            {
                userId.AddRange(_userManager.DataScope.Select(x => x.organizeId).ToList());
            }
            userId.Remove("@currentGradeOrg");
        }

        var userIdList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => userId.Contains(x.ObjectId) || userId.Contains(x.UserId)).Select(x => x.UserId).Distinct().ToListAsync();

        // 子组织
        //var childOrgIdList = new List<string>();
        //relIdList.Where(x => x.ObjectType.Equals("Organize")).Select(x => x.ObjectId).ToList().ForEach(item => childOrgIdList.AddRange(orgInfoList.Where(x => x.OrganizeIdTree.Contains(item)).Select(x => x.Id)));
        //userIdList.AddRange(await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => childOrgIdList.Contains(x.ObjectId) && x.ObjectType.Equals("Organize")).Select(x => x.UserId).ToListAsync());
        //userIdList.AddRange(userId);
        var userOrgList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => userIdList.Contains(x.UserId) && x.ObjectType.Equals("Organize")).Select(x => new { x.ObjectId, x.UserId }).ToListAsync();
        var userInfoList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => userIdList.Contains(x.Id) && x.DeleteMark == null && x.EnabledMark > 0)
            .WhereIF(input.pagination.keyword.IsNotEmptyOrNull(), x => x.RealName.Contains(input.pagination.keyword) || x.Account.Contains(input.pagination.keyword))
            .Select(x => new UserSelectedOutput
            {
                fullName = SqlFunc.MergeString(x.RealName, "/", x.Account),
                icon = "icon-ym icon-ym-tree-user2",
                headIcon = SqlFunc.MergeString("/api/File/Image/userAvatar/", x.HeadIcon),
                id = x.Id,
                type = "user",
                gender = x.Gender,
                mobilePhone = x.MobilePhone
            }).ToPagedListAsync(input.pagination.currentPage, input.pagination.pageSize);

        userInfoList.list.ToList().ForEach(item =>
        {
            item.organize = string.Join(",", orgInfoList.Where(o => userOrgList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).Contains(o.Id)).Select(x => x.Description));
            item.organizeIds = orgInfoList.Where(o => userOrgList.Where(x => x.UserId.Equals(item.id)).Select(x => x.ObjectId).Contains(o.Id)).Select(x => x.OrganizeIdTree).ToList();
        });

        return PageResult<UserSelectedOutput>.SqlSugarPageResult(userInfoList);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 获取集合中的组织 树,根据上级ID.
    /// </summary>
    /// <param name="list">组织 集合.</param>
    /// <param name="parentId">上级ID.</param>
    /// <param name="addList">返回.</param>
    /// <returns></returns>
    private List<string> GetOrganizeParentName(List<OrganizeEntity> list, string parentId, List<string> addList)
    {
        OrganizeEntity? entity = list.Find(x => x.Id == parentId);

        if (entity.ParentId != "-1") GetOrganizeParentName(list, entity.ParentId, addList);
        else addList.Add(entity.FullName);

        return addList;
    }

    /// <summary>
    /// 获取机构成员列表.
    /// </summary>
    /// <param name="organizeId">机构ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<OrganizeMemberListOutput>> GetOrganizeMemberList(string organizeId, CancellationToken cancellationToken = default)
    {
        // 获取分级管理组织
        var dataScope = _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>()
            .Where(it => it.UserId == _userManager.UserId && it.DeleteMark == null)
            .Where(it => it.ThisLayerSelect.Equals(1) || it.SubLayerSelect.Equals(1)
            || it.ThisLayerAdd.Equals(1) || it.SubLayerAdd.Equals(1)
            || it.ThisLayerDelete.Equals(1) || it.SubLayerDelete.Equals(1)
            || it.ThisLayerEdit.Equals(1) || it.SubLayerEdit.Equals(1)).ToList();

        var thisLayer = dataScope.Where(it => it.ThisLayerSelect.Equals(1) || it.ThisLayerAdd.Equals(1) || it.ThisLayerDelete.Equals(1) || it.ThisLayerEdit.Equals(1)).ToList();
        var subLayer = dataScope.Where(it => it.SubLayerSelect.Equals(1) || it.SubLayerAdd.Equals(1) || it.SubLayerDelete.Equals(1) || it.SubLayerEdit.Equals(1)).ToList();

        List<OrganizeMemberListOutput>? output = new List<OrganizeMemberListOutput>();

        if (organizeId.Equals("0"))
        {
            List<OrganizeEntity>? data = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(o => o.DeleteMark == null && o.EnabledMark == 1)
                .WhereIF(thisLayer.Any(), x => thisLayer.Select(x => x.OrganizeId).Contains(x.Id))
                .WhereIF(!thisLayer.Any(), x => thisLayer.Select(x => x.OrganizeId).Contains(x.Id)).OrderBy(o => o.SortCode).ToListAsync();

            if (subLayer.Any())
            {
                subLayer.ForEach(item =>
                {
                    var itemRes = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(o => o.DeleteMark == null && o.EnabledMark == 1)
                   .Where(x => x.OrganizeIdTree.Contains(item.OrganizeId) && !x.Id.Equals(item.OrganizeId)).OrderBy(o => o.SortCode).ToList();
                    data.AddRange(itemRes);
                });
            }

            data.ForEach(o =>
            {
                output.Add(new OrganizeMemberListOutput
                {
                    id = o.Id,
                    fullName = o.FullName,
                    enabledMark = o.EnabledMark,
                    type = o.Category,
                    icon = o.Category.Equals("company") ? "icon-ym icon-ym-tree-organization3" : "icon-ym icon-ym-tree-department1",
                    organizeIdTree = o.OrganizeIdTree,
                    hasChildren = true,
                    isLeaf = false
                });
            });
        }
        else
        {
            var userRelationList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.ObjectType.Equals("Organize") && x.ObjectId.Equals(organizeId)).Select(x => x.UserId).ToListAsync();
            List<UserEntity>? userList = await _repository.AsSugarClient().Queryable<UserEntity>()
                .Where(u => userRelationList.Contains(u.Id) && u.EnabledMark > 0 && u.DeleteMark == null).OrderBy(o => o.SortCode).ToListAsync();
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
                    isLeaf = true
                });
            });

            List<OrganizeEntity>? departmentList = await _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(o => o.DeleteMark == null && o.EnabledMark == 1)
                .Where(x => x.ParentId.Equals(organizeId))
                .WhereIF(thisLayer.Any(), x => thisLayer.Select(x => x.OrganizeId).Contains(x.Id))
                .WhereIF(!thisLayer.Any(), x => thisLayer.Select(x => x.OrganizeId).Contains(x.Id)).OrderBy(o => o.SortCode).ToListAsync();

            if (subLayer.Any())
            {
                subLayer.ForEach(item =>
                {
                    var itemRes = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(o => o.DeleteMark == null && o.EnabledMark == 1)
                   .Where(x => x.OrganizeIdTree.Contains(item.OrganizeId) && !x.Id.Equals(item.OrganizeId) && x.OrganizeIdTree.Contains(organizeId)).OrderBy(o => o.SortCode).ToList();
                    departmentList.AddRange(itemRes);
                });
            }

            departmentList.ForEach(o =>
            {
                output.Add(new OrganizeMemberListOutput()
                {
                    id = o.Id,
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

        if (!organizeId.Equals("0")) output.RemoveAll(x => x.id.Equals(organizeId));

        // 获取组织树
        var orgTree = _organizeService.GetOrgListTreeName();

        // 组织断层处理
        output.Where(x => x.parentId != "-1" && x.organizeIdTree.IsNotEmptyOrNull()).ToList().ForEach(item =>
        {
            item.fullName = orgTree.Find(x => x.Id.Equals(item.id)).Description;
            if (!output.Any(x => x.id.Equals(item.parentId)))
            {
                var pItem = output.Where(x => x.organizeIdTree.IsNotEmptyOrNull() && x.id != item.id && item.organizeIdTree.Contains(x.organizeIdTree)).FirstOrDefault();
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

        output.RemoveAll(x => x.type != "user" && x.parentId != "-1");

        if (!organizeId.Equals("0"))
        {
            var pOrgTreeName = orgTree.Find(x => x.Id.Equals(organizeId)).Description;
            output.ForEach(item => item.fullName = item.fullName.Replace(pOrgTreeName + "/", string.Empty));
        }

        return output;
    }

    #endregion
}
