using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Dto.UserRelation;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace JNPF.Systems;

/// <summary>
/// 业务实现：用户关系.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "UserRelation", Order = 169)]
[Route("api/permission/[controller]")]
public class UserRelationService : IUserRelationService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 服务基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<UserRelationEntity> _repository;

    /// <summary>
    /// 组织管理.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    /// <summary>
    /// 初始化一个<see cref="UserRelationService"/>类型的新实例.
    /// </summary>
    public UserRelationService(
        ISqlSugarRepository<UserRelationEntity> userRelationRepository,
        IOrganizeService organizeService,
        ICacheManager cacheManager,
        IMHandler imHandler,
        IUserManager userManager)
    {
        _repository = userRelationRepository;
        _organizeService = organizeService;
        _cacheManager = cacheManager;
        _imHandler = imHandler;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 获取岗位/角色成员列表.
    /// </summary>
    /// <param name="objectId">岗位id或角色id.</param>
    /// <returns></returns>
    [HttpGet("{objectId}")]
    public async Task<dynamic> GetListByObjectId(string objectId)
    {
        return new { ids = await _repository.AsQueryable().Where(u => u.ObjectId == objectId).Select(s => s.UserId).ToListAsync() };
    }

    #endregion

    #region Post

    /// <summary>
    /// 新建用户关系.
    /// </summary>
    /// <param name="objectId">功能主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("{objectId}")]
    [UnitOfWork]
    public async Task Create(string objectId, [FromBody] UserRelationCrInput input)
    {
        #region 分级权限验证

        if (input.objectType.Equals("Position") || input.objectType.Equals("Role"))
        {
            var orgIds = new List<string>();
            if (input.objectType.Equals("Position")) orgIds = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.Id.Equals(objectId)).Select(x => x.OrganizeId).ToListAsync();
            if (input.objectType.Equals("Role")) orgIds = await _repository.AsSugarClient().Queryable<OrganizeRelationEntity>().Where(x => x.ObjectId.Equals(objectId) && x.ObjectType == input.objectType).Select(x => x.OrganizeId).ToListAsync();

            if (!_userManager.DataScope.Any(it => orgIds.Contains(it.organizeId) && it.Edit) && !_userManager.IsAdministrator)
                throw Oops.Oh(ErrorCode.D1013); // 分级管控
        }

        #endregion

        List<string>? oldUserIds = await _repository.AsQueryable().Where(u => u.ObjectId.Equals(objectId) && u.ObjectType.Equals(input.objectType)).Select(s => s.UserId).ToListAsync();

        // 禁用用户不删除
        List<string>? disabledUserIds = await _repository.AsSugarClient().Queryable<UserEntity>().Where(u => u.EnabledMark == 0 && oldUserIds.Contains(u.Id)).Select(s => s.Id).ToListAsync();

        // 清空原有数据
        await _repository.DeleteAsync(u => u.ObjectId.Equals(objectId) && u.ObjectType.Equals(input.objectType) && !disabledUserIds.Contains(u.UserId));

        // 创建新数据
        List<UserRelationEntity>? dataList = new List<UserRelationEntity>();
        input.userIds.ForEach(item =>
        {
            dataList.Add(new UserRelationEntity()
            {
                Id = SnowflakeIdHelper.NextId(),
                CreatorTime = DateTime.Now,
                CreatorUserId = _userManager.UserId,
                UserId = item,
                ObjectType = input.objectType,
                ObjectId = objectId,
                SortCode = input.userIds.IndexOf(item)
            });
        });
        if (dataList.Count > 0)
            await _repository.AsInsertable(dataList).ExecuteCommandAsync();

        // 修改用户
        // 计算旧用户数组与新用户数组差
        List<string>? addList = input.userIds.Except(oldUserIds).ToList();
        List<string>? delList = oldUserIds.Except(input.userIds).ToList();
        delList = delList.Except(disabledUserIds).ToList();

        // 处理新增用户岗位
        if (addList.Count > 0)
        {
            List<UserEntity>? addUserList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(u => addList.Contains(u.Id)).ToListAsync();
            addUserList.ForEach(async item =>
            {
                if (input.objectType.Equals("Position"))
                {
                    List<string>? idList = string.IsNullOrEmpty(item.PositionId) ? new List<string>() : item.PositionId.Split(',').ToList();
                    idList.Add(objectId);

                    #region 获取默认组织下的岗位

                    if (item.PositionId.IsNullOrEmpty())
                    {
                        List<string>? pIdList = await _repository.AsSugarClient().Queryable<PositionEntity>()
                        .Where(x => x.OrganizeId == item.OrganizeId && idList.Contains(x.Id)).Select(x => x.Id).ToListAsync();
                        item.PositionId = pIdList.FirstOrDefault(); // 多岗位 默认取第一个
                        item.LastModifyTime = DateTime.Now;
                        item.LastModifyUserId = _userManager.UserId;
                    }
                    #endregion
                }
                else if (input.objectType.Equals("Role"))
                {
                    List<string>? idList = string.IsNullOrEmpty(item.RoleId) ? new List<string>() : item.RoleId.Split(',').ToList();
                    idList.Add(objectId);
                    item.RoleId = string.Join(",", idList.ToArray()).TrimStart(',').TrimEnd(',');
                    item.LastModifyTime = DateTime.Now;
                    item.LastModifyUserId = _userManager.UserId;
                }
                else if (input.objectType.Equals("Group"))
                {
                    item.GroupId = string.IsNullOrEmpty(item.GroupId) ? objectId : item.GroupId;
                }
            });
            await _repository.AsSugarClient().Updateable(addUserList).UpdateColumns(it => new { it.RoleId, it.PositionId, it.GroupId }).CallEntityMethod(m => m.LastModify()).ExecuteCommandAsync();

            await ForcedOffline(addList);
        }

        // 移除用户
        if (delList.Count > 0)
        {
            List<UserEntity>? delUserList = await _repository.AsSugarClient().Queryable<UserEntity>().Where(u => delList.Contains(u.Id)).ToListAsync();
            foreach (UserEntity? item in delUserList)
            {
                if (input.objectType.Equals("Position"))
                {
                    if (item.PositionId.IsNotEmptyOrNull())
                    {
                        List<string>? idList = item.PositionId.Split(',').ToList();
                        idList.RemoveAll(x => x == objectId);
                    }

                    #region 获取默认组织下的岗位

                    List<string>? pList = _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.OrganizeId == item.OrganizeId).Select(x => x.Id).ToList();
                    List<string>? pIdList = _repository.AsQueryable().Where(x => x.UserId == item.Id && x.ObjectType == "Position" && pList.Contains(x.ObjectId)).Select(x => x.ObjectId).ToList();

                    item.PositionId = pIdList.FirstOrDefault(); // 多岗位 默认取第一个

                    #endregion

                    _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(it => new UserEntity()
                    {
                        PositionId = item.PositionId,
                        LastModifyTime = SqlFunc.GetDate(),
                        LastModifyUserId = _userManager.UserId
                    }).Where(it => it.Id == item.Id).ExecuteCommand();
                }
                else if (input.objectType.Equals("Role"))
                {
                    if (item.RoleId.IsNotEmptyOrNull())
                    {
                        List<string>? idList = item.RoleId.Split(',').ToList();
                        idList.RemoveAll(x => x == objectId);

                        _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(it => new UserEntity()
                        {
                            RoleId = string.Join(",", idList.ToArray()).TrimStart(',').TrimEnd(','),
                            LastModifyTime = SqlFunc.GetDate(),
                            OrganizeId = item.OrganizeId,
                            PositionId = item.PositionId,
                            LastModifyUserId = _userManager.UserId
                        }).Where(it => it.Id == item.Id).ExecuteCommand();
                    }
                }
                else if (input.objectType.Equals("Group"))
                {
                    List<string>? pIdList = _repository.AsQueryable().Where(x => x.UserId == item.Id && x.ObjectType == "Group").Select(x => x.ObjectId).ToList();
                    item.GroupId = null;
                    if (pIdList.Any())
                    {
                        _repository.AsSugarClient().Updateable<UserEntity>().SetColumns(it => new UserEntity()
                        {
                            GroupId = pIdList.First(),
                            LastModifyTime = SqlFunc.GetDate(),
                            LastModifyUserId = _userManager.UserId
                        }).Where(it => it.Id == item.Id).ExecuteCommand();
                    }
                }
            }

            await ForcedOffline(delList);
        }
    }

    #endregion

    #region PublicMethod

    /// <summary>
    /// 创建用户关系.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <param name="ids">对象ID组.</param>
    /// <param name="relationType">关系类型(岗位-Position;角色-Role;组织-Organize;分组-Group;).</param>
    /// <returns></returns>
    [NonAction]
    public List<UserRelationEntity> CreateUserRelation(string userId, string ids, string relationType)
    {
        List<UserRelationEntity> userRelationList = new List<UserRelationEntity>();
        if (!ids.IsNullOrEmpty())
        {
            List<string>? position = new List<string>(ids.Split(','));
            position.ForEach(item =>
            {
                UserRelationEntity? entity = new UserRelationEntity();
                entity.Id = SnowflakeIdHelper.NextId();
                entity.ObjectType = relationType;
                entity.ObjectId = item;
                entity.SortCode = position.IndexOf(item);
                entity.UserId = userId;
                entity.CreatorTime = DateTime.Now;
                entity.CreatorUserId = _userManager.UserId;
                userRelationList.Add(entity);
            });
        }

        return userRelationList;
    }

    /// <summary>
    /// 创建用户关系.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [NonAction]
    public async Task Create(List<UserRelationEntity> input)
    {
        await _repository.AsInsertable(input).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">用户ID.</param>
    /// <returns></returns>
    [NonAction]
    public async Task Delete(string id)
    {

        await _repository.AsDeleteable().Where(u => u.UserId == id).ExecuteCommandAsync();
    }

    /// <summary>
    /// 根据用户主键获取列表.
    /// </summary>
    /// <param name="userId">用户主键.</param>
    /// <returns></returns>
    [NonAction]
    public async Task<List<UserRelationEntity>> GetListByUserId(string userId)
    {
        return await _repository.AsQueryable().Where(m => m.UserId == userId).OrderBy(o => o.CreatorTime).ToListAsync();
    }

    /// <summary>
    /// 获取用户.
    /// </summary>
    /// <param name="type">关系类型.</param>
    /// <param name="objId">对象ID.</param>
    /// <returns></returns>
    [NonAction]
    public List<string> GetUserId(string type, string objId)
    {
        return _repository.AsSugarClient().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.UserId == b.Id))
                .Where((a, b) => a.ObjectType == type && a.ObjectId == objId && b.DeleteMark == null && b.EnabledMark > 0).Select(a => a.UserId).Distinct().ToList();
    }

    /// <summary>
    /// 获取用户.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="objId"></param>
    /// <returns></returns>
    [NonAction]
    public List<string> GetUserId(List<string> objId, string type = null)
    {
        if (objId.Any())
        {
            if (type.IsNotEmptyOrNull())
            {
                return _repository.AsSugarClient().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.UserId == b.Id))
                .Where((a, b) => a.ObjectType == type && b.DeleteMark == null && (objId.Contains(a.ObjectId) || objId.Contains(a.UserId)) && b.EnabledMark > 0).Select(a => a.UserId).Distinct().ToList();
            }
            else
            {
                return _repository.AsSugarClient().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.UserId == b.Id))
               .Where((a, b) => b.DeleteMark == null && (objId.Contains(a.ObjectId) || objId.Contains(a.UserId)) && b.EnabledMark > 0).Select(a => a.UserId).Distinct().ToList();
            }
        }
        else
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// 获取用户(分页).
    /// </summary>
    /// <param name="userIds">用户ID组.</param>
    /// <param name="objIds">对象ID组.</param>
    /// <param name="pageInputBase">分页参数.</param>
    /// <returns></returns>
    [NonAction]
    public dynamic GetUserPage(UserConditionInput input, ref bool hasCandidates)
    {
        SqlSugarPagedList<UserListOutput>? data = new SqlSugarPagedList<UserListOutput>();

        input.userIds = GetUserId(input.userIds);

        data = _repository.AsSugarClient().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, b.Id == a.UserId))
            .Where((a, b) => b.DeleteMark == null && input.userIds.Contains(b.Id))
            .WhereIF(input.pagination.keyword.IsNotEmptyOrNull(), (a, b) => b.Account.Contains(input.pagination.keyword) || b.RealName.Contains(input.pagination.keyword))
            .GroupBy((a, b) => new { Id = b.Id, Account = b.Account, RealName = b.RealName, Gender = b.Gender, MobilePhone = b.MobilePhone })
            .Select((a, b) => new UserListOutput()
            {
                id = b.Id,
                organizeId = SqlFunc.Subqueryable<UserEntity>().Where(e => e.Id == b.Id).Select(u => u.OrganizeId),
                account = b.Account,
                fullName = SqlFunc.MergeString(b.RealName, "/", b.Account),
                headIcon = SqlFunc.Subqueryable<UserEntity>().Where(e => e.Id == b.Id).Select(u => SqlFunc.MergeString("/api/File/Image/userAvatar/", u.HeadIcon)),
                gender = b.Gender,
                mobilePhone = b.MobilePhone
            }).ToPagedList(input.pagination.currentPage, input.pagination.pageSize);

        if (data.list.Any())
        {
            hasCandidates = true;
            var orgList = _organizeService.GetOrgListTreeName();

            // 获取所属组织的所有成员
            List<UserRelationEntity>? userList = _repository.AsSugarClient().Queryable<UserRelationEntity>()
                .Where(x => x.ObjectType == "Organize" && data.list.Select(x => x.id).Contains(x.UserId)).ToList();

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
    /// 新用户组件获取人员.
    /// </summary>
    /// <param name="Ids"></param>
    /// <returns></returns>
    public List<string> GetUserId(List<string> Ids)
    {
        var userIdList = new List<string>();
        var objIdList = new List<string>();
        foreach (var item in Ids)
        {
            var strList = item.Split("--").ToList();
            if (strList[1].Equals("user"))
            {
                userIdList.Add(strList[0]);
            }
            else if (strList[1].Equals("department") || strList[1].Equals("company"))
            {
                var orgIds = _repository.AsSugarClient().Queryable<OrganizeEntity>().Where(x => x.OrganizeIdTree.Contains(strList[0]) && x.EnabledMark == 1 && x.DeleteMark == null).Select(x => x.Id).ToList();
                objIdList = objIdList.Union(orgIds).ToList();
            }
            else
            {
                objIdList.Add(strList[0]);
            }
        }
        var otherUserIds = _repository.AsSugarClient().Queryable<UserRelationEntity, UserEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.UserId == b.Id))
                .Where((a, b) => b.DeleteMark == null && objIdList.Contains(a.ObjectId) && b.EnabledMark == 1).Select(a => a.UserId).Distinct().ToList();
        return otherUserIds.Union(userIdList).ToList();
    }

    /// <summary>
    /// 强制在线用户下线.
    /// </summary>
    /// <param name="userIds">用户Id.</param>
    /// <returns></returns>
    public async Task ForcedOffline(List<string> userIds)
    {
        var uIdList = _repository.AsSugarClient().Queryable<UserEntity>().Where(x => userIds.Contains(x.Id) && x.IsAdministrator.Equals(0)).Select(x => x.Id).ToList();
        var orgAdminIds = _repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Select(x => x.UserId).ToList();
        var idList = userIds.Where(x => uIdList.Except(orgAdminIds).Contains(x)).ToList();

        var onlineCacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, _userManager.TenantId);
        var list = await _cacheManager.GetAsync<List<UserOnlineModel>>(onlineCacheKey);
        if (list != null && list.Any())
        {
            var fList = list.Where(it => it.tenantId == _userManager.TenantId && idList.Contains(it.userId)).ToList();
            foreach (var user in fList)
            {
                await _imHandler.SendMessageAsync(user.connectionId, new { method = "logout", msg = "用户信息已变更，请重新登录！" }.ToJsonString());

                // 删除在线用户ID
                list.RemoveAll((x) => x.connectionId == user.connectionId);
                await _cacheManager.SetAsync(onlineCacheKey, list);

                // 删除用户登录信息缓存
                var cacheKey = string.Format("{0}:{1}:{2}", _userManager.TenantId, CommonConst.CACHEKEYUSER, user.userId);
                await _cacheManager.DelAsync(cacheKey);
            }
        }
    }
    #endregion
}