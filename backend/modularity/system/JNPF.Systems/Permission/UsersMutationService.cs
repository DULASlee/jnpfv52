using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Helper;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DatabaseAccessor;
using JNPF.DataEncryption;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.FriendlyException;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Dto.SysConfig;
using JNPF.Systems.Entitys.Dto.User;
using JNPF.Systems.Entitys.Enum;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Threading;

namespace JNPF.Systems;

/// <summary>
///  业务实现：用户信息写操作（CR-20260819-01 阶段 2 自 UsersService 剥离）.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Users", Order = 163)]
[Route("api/permission/[controller]")]
public class UsersMutationService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 配置文档.
    /// </summary>
    private readonly OauthOptions _oauthOptions = App.GetConfig<OauthOptions>("OAuth", true);

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
    /// 系统配置服务.
    /// </summary>
    private readonly ISysConfigService _sysConfigService;

    /// <summary>
    /// 第三方同步服务.
    /// </summary>
    private readonly ISynThirdInfoService _synThirdInfoService;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 租户管理.
    /// </summary>
    private readonly ITenantManager _tenantManager;

    /// <summary>
    /// IM中心处理程序.
    /// </summary>
    private IMHandler _imHandler;

    /// <summary>
    /// 初始化一个<see cref="UsersMutationService"/>类型的新实例.
    /// </summary>
    public UsersMutationService(
        ISqlSugarRepository<UserEntity> userRepository,
        IOrganizeService organizeService,
        IUserRelationService userRelationService,
        ISysConfigService sysConfigService,
        ISynThirdInfoService synThirdInfoService,
        ICacheManager cacheManager,
        IOptions<TenantOptions> tenantOptions,
        IUserManager userManager,
        ITenantManager tenantManager,
        IMHandler imHandler)
    {
        _repository = userRepository;
        _organizeService = organizeService;
        _userRelationService = userRelationService;
        _sysConfigService = sysConfigService;
        _userManager = userManager;
        _cacheManager = cacheManager;
        _synThirdInfoService = synThirdInfoService;
        _tenant = tenantOptions.Value;
        _tenantManager = tenantManager;
        _imHandler = imHandler;
    }

    #region POST

    /// <summary>
    /// 新建.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("")]
    [AllowAnonymous]
    public async Task Create([FromBody] UserCrInput input, CancellationToken cancellationToken = default)
    {
        if (_tenant.MultiTenancy)
        {
            var tenatInfo = await _tenantManager.GetTenant(_userManager.TenantId);

            // 是否超过租户账号额度
            if (tenatInfo.accountNum != 0 && tenatInfo.accountNum <= await _repository.AsQueryable().CountAsync(x => x.DeleteMark == null))
                throw Oops.Oh(ErrorCode.D1041);
        }

        var orgids = input.organizeId.Split(',');

        //目前支持外部自主注册，需要不检查该权限
        if (false)
        {
            if (!_userManager.DataScope.Any(it => orgids.Contains(it.organizeId) && it.Add) && !_userManager.IsAdministrator)
                throw Oops.Oh(ErrorCode.D1013);
        }

        if (await _repository.IsAnyAsync(u => u.Account == input.account && u.DeleteMark == null)) throw Oops.Oh(ErrorCode.D1003);
        UserEntity? entity = input.Adapt<UserEntity>();

        #region 用户表单

        entity.IsAdministrator = 0;
        entity.EntryDate = input.entryDate.IsNullOrEmpty() ? DateTime.Now : input.entryDate;
        entity.Birthday = input.birthday.IsNullOrEmpty() ? DateTime.Now : input.birthday;
        entity.QuickQuery = PinyinHelper.PinyinString(input.realName);
        entity.Secretkey = Guid.NewGuid().ToString();

        var defaultPassWord = await _repository.AsSugarClient().Queryable<SysConfigEntity>()
            .Where(it => it.Key.Equals("newUserDefaultPassword"))
            .Select(it => it.Value)
            .FirstAsync();
			
		if(!input.password.IsNullOrEmpty())
		{
			defaultPassWord = input.password;
		}
			
        entity.Password = MD5Encryption.Encrypt(MD5Encryption.Encrypt(defaultPassWord) + entity.Secretkey);
        string? headIcon = input.headIcon?.Split('/').ToList().Last();
        if (string.IsNullOrEmpty(headIcon))
            headIcon = "001.png";
        entity.HeadIcon = headIcon;

        // 多组织
        string[]? orgList = entity.OrganizeId.Split(",");
        entity.OrganizeId = orgList.FirstOrDefault();
        string[]? positionIds = entity.PositionId?.Split(",");
        List<string>? pIdList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.OrganizeId == entity.OrganizeId && positionIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();
        entity.PositionId = pIdList.FirstOrDefault(); // 多 岗位 默认取当前组织第一个

        #endregion

        try
        {
            // 新增用户记录
            await _repository.AsInsertable(entity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();

            // 将临时文件迁移至正式文件
            FileHelper.MoveFile(Path.Combine(FileVariable.TemporaryFilePath, headIcon), Path.Combine(FileVariable.UserAvatarFilePath, headIcon));

            List<UserRelationEntity>? userRelationList = new List<UserRelationEntity>();
            userRelationList.AddRange(_userRelationService.CreateUserRelation(entity.Id, input.roleId, "Role"));
            userRelationList.AddRange(_userRelationService.CreateUserRelation(entity.Id, input.positionId, "Position"));
            userRelationList.AddRange(_userRelationService.CreateUserRelation(entity.Id, input.organizeId, "Organize"));
            userRelationList.AddRange(_userRelationService.CreateUserRelation(entity.Id, input.groupId, "Group"));

            if (userRelationList.Count > 0) await _userRelationService.Create(userRelationList); // 批量新增用户关系

            #region 第三方同步

            try
            {
                SysConfigOutput? sysConfig = await _sysConfigService.GetInfo();
                List<UserEntity>? userList = new List<UserEntity>();
                userList.Add(entity);
                if (sysConfig.dingSynIsSynUser)
                    await _synThirdInfoService.SynUser(2, 3, sysConfig, userList);
                if (sysConfig.qyhIsSynUser)
                    await _synThirdInfoService.SynUser(1, 3, sysConfig, userList);
            }
            catch (Exception)
            {
            }

            #endregion

            // 单点登录同步
            await syncUserInfo(entity, "create", _userManager.TenantId);
        }
        catch (Exception)
        {
            throw Oops.Bah(ErrorCode.D5001);
        }

    }

    /// <summary>
    /// 删除.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task Delete(string id, CancellationToken cancellationToken = default)
    {
        UserEntity? entity = await _repository.GetFirstAsync(u => u.Id == id && u.DeleteMark == null);

        // 所属组织 分级权限验证
        List<string>? orgIdList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.UserId == id && x.ObjectType == "Organize").Select(x => x.ObjectId).ToListAsync();
        if (!_userManager.DataScope.Any(it => orgIdList.Contains(it.organizeId) && it.Delete) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        if (await _organizeService.GetIsManagerByUserId(id))
            throw Oops.Oh(ErrorCode.D2003);
        _ = entity ?? throw Oops.Oh(ErrorCode.D5002);
        if (entity.IsAdministrator == (int)AccountType.Administrator)
            throw Oops.Oh(ErrorCode.D1014);
        if (entity.Id == _userManager.UserId)
            throw Oops.Oh(ErrorCode.D1001);
        entity.DeleteTime = DateTime.Now;
        entity.DeleteMark = 1;
        entity.DeleteUserId = _userManager.UserId;

        // 用户软删除
        await _repository.AsUpdateable(entity).UpdateColumns(it => new { it.DeleteTime, it.DeleteMark, it.DeleteUserId }).ExecuteCommandAsync();

        // 直接删除用户关系表相关相关数据
        await _userRelationService.Delete(id);

        #region 第三方同步

        try
        {
            SysConfigOutput? sysConfig = await _sysConfigService.GetInfo();
            if (sysConfig.dingSynIsSynUser)
                await _synThirdInfoService.DelSynData(2, 3, sysConfig, id);
            if (sysConfig.qyhIsSynUser)
                await _synThirdInfoService.DelSynData(1, 3, sysConfig, id);
        }
        catch (Exception)
        {
        }

        #endregion

        // 单点登录同步
        await syncUserInfo(entity, "delete", _userManager.TenantId);
    }

    /// <summary>
    /// 更新.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task Update(string id, [FromBody] UserUpInput input, CancellationToken cancellationToken = default)
    {
        UserEntity? oldUserEntity = await _repository.GetFirstAsync(it => it.Id == id);
        input.roleId = input.roleId == null ? string.Empty : input.roleId;

        // 超级管理员 只有 admin 账号才有变更权限
        if (_userManager.UserId != oldUserEntity.Id && oldUserEntity.IsAdministrator == 1 && _userManager.Account != "admin")
            throw Oops.Oh(ErrorCode.D1033);

        // 超级管理员不能禁用
        if (oldUserEntity.IsAdministrator.Equals(1) && input.enabledMark.Equals(0))
            throw Oops.Oh(ErrorCode.D1015);

        // 旧数据
        List<string>? orgIdList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.UserId == id && x.ObjectType == "Organize").Select(x => x.ObjectId).ToListAsync();
        if (!_userManager.DataScope.Any(it => orgIdList.Contains(it.organizeId) && it.Edit) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        // 新数据
        var orgids = input.organizeId.Split(',');
        if (!_userManager.DataScope.Any(it => orgids.Contains(it.organizeId) && it.Edit) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        // 排除自己并且判断与其他是否相同
        if (await _repository.IsAnyAsync(u => u.Account == input.account && u.DeleteMark == null && u.Id != id)) throw Oops.Oh(ErrorCode.D1003);
        if (id == input.managerId) throw Oops.Oh(ErrorCode.D1021);

        // 直属主管的上级不能为自己的下属
        if (await GetIsMyStaff(id, input.managerId, 10)) throw Oops.Oh(ErrorCode.D1026);
        UserEntity? entity = input.Adapt<UserEntity>();
        entity.QuickQuery = PinyinHelper.PinyinString(input.realName);
        string? headIcon = input.headIcon.Split('/').ToList().Last();
        entity.HeadIcon = headIcon;
        entity.LastModifyTime = DateTime.Now;
        entity.LastModifyUserId = _userManager.UserId;
        entity.SystemId = oldUserEntity.SystemId;
        entity.AppSystemId = oldUserEntity.AppSystemId;
        if (entity.EnabledMark == 2) entity.UnLockTime = null;

        // 多 组织
        if (orgids.Contains(oldUserEntity.OrganizeId)) entity.OrganizeId = oldUserEntity.OrganizeId;
        else entity.OrganizeId = orgids.FirstOrDefault();

        // 获取默认组织下的岗位
        string[]? positionIds = entity.PositionId?.Split(",");
        List<string>? pIdList = await _repository.AsSugarClient().Queryable<PositionEntity>().Where(x => x.OrganizeId == entity.OrganizeId && positionIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();

        if (entity.PositionId.IsNotEmptyOrNull() && pIdList.Contains(oldUserEntity.PositionId)) entity.PositionId = oldUserEntity.PositionId;
        else entity.PositionId = pIdList.FirstOrDefault(); // 多 岗位 默认取第一个

        try
        {
            // 更新用户记录
            int newEntity = await _repository.AsUpdateable(entity).UpdateColumns(it => new {
                it.Account,
                it.RealName,
                it.QuickQuery,
                it.Gender,
                it.Email,
                it.OrganizeId,
                it.ManagerId,
                it.PositionId,
                it.RoleId,
                it.SortCode,
                it.EnabledMark,
                it.Description,
                it.HeadIcon,
                it.Nation,
                it.NativePlace,
                it.EntryDate,
                it.CertificatesType,
                it.CertificatesNumber,
                it.Education,
                it.UrgentContacts,
                it.UrgentTelePhone,
                it.PostalAddress,
                it.MobilePhone,
                it.Birthday,
                it.TelePhone,
                it.Landline,
                it.UnLockTime,
                it.GroupId,
                it.Ranks,
                it.LastModifyTime,
                it.SystemId,
                it.LastModifyUserId
            }).ExecuteCommandAsync();

            // 将临时文件迁移至正式文件
            FileHelper.MoveFile(Path.Combine(FileVariable.TemporaryFilePath, headIcon), Path.Combine(FileVariable.UserAvatarFilePath, headIcon));

            // 捞取用户分组
            var userGroupIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.UserId.Equals(id) && x.ObjectType.Equals("Group")).Select(x => x.ObjectId).ToListAsync();
            if (userGroupIds != null && userGroupIds.Any()) input.groupId = string.Join(",", userGroupIds);

            // 用户编辑界面：当变更【所属组织】【岗位】【角色】时，业务端用户强制退出，但超管和分管是不影响。
            var isLogout = false;
            if (oldUserEntity.IsAdministrator.Equals(0) && !_repository.AsSugarClient().Queryable<OrganizeAdministratorEntity>().Any(x => x.UserId.Equals(input.id)))
            {
                var userRelationIds = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.UserId.Equals(id)).ToListAsync();

                // 捞取用户组织
                var userOrgIds = userRelationIds.Where(x => x.ObjectType.Equals("Organize")).Select(x => x.ObjectId).ToList();
                var newRelationIds = input.organizeId.Split(",").ToList();
                newRelationIds.Remove("");
                if (newRelationIds.Count != userOrgIds.Count() || newRelationIds.Except(userOrgIds).Any()) isLogout = true;

                // 捞取用户角色
                var userRoleIds = userRelationIds.Where(x => x.ObjectType.Equals("Role")).Select(x => x.ObjectId).ToList();
                newRelationIds = input.roleId.Split(",").ToList();
                newRelationIds.Remove("");
                if (newRelationIds.Count() != userRoleIds.Count() || newRelationIds.Except(userRoleIds).Any()) isLogout = true;

                // 捞取用户岗位
                var userPosIds = userRelationIds.Where(x => x.ObjectType.Equals("Position")).Select(x => x.ObjectId).ToList();
                newRelationIds = input.positionId.Split(",").ToList();
                newRelationIds.Remove("");
                if (newRelationIds.Count() != userPosIds.Count() || newRelationIds.Except(userPosIds).Any()) isLogout = true;
            }

            // 直接删除用户关系表相关相关数据
            await _userRelationService.Delete(id);

            List<UserRelationEntity>? userRelationList = new List<UserRelationEntity>();
            userRelationList.AddRange(_userRelationService.CreateUserRelation(id, entity.RoleId, "Role"));
            userRelationList.AddRange(_userRelationService.CreateUserRelation(id, input.positionId, "Position"));
            userRelationList.AddRange(_userRelationService.CreateUserRelation(id, input.organizeId, "Organize"));
            userRelationList.AddRange(_userRelationService.CreateUserRelation(id, input.groupId, "Group"));
            if (userRelationList.Count > 0) await _userRelationService.Create(userRelationList); // 批量新增用户关系

            // 修改该用户信息，该用户会立即退出登录
            var onlineCacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, _userManager.TenantId);
            var list = await _cacheManager.GetAsync<List<UserOnlineModel>>(onlineCacheKey);
            if (list != null && list.Any())
            {
                var user = list.Find(it => it.tenantId == _userManager.TenantId && it.userId == id);
                if (user != null && isLogout)
                {
                    await _imHandler.SendMessageAsync(user.connectionId, new { method = "logout", msg = "用户信息已变更，请重新登录！" }.ToJsonString());

                    // 删除在线用户ID
                    list.RemoveAll((x) => x.connectionId == user.connectionId);
                    await _cacheManager.SetAsync(onlineCacheKey, list);

                    // 删除用户登录信息缓存
                    var cacheKey = string.Format("{0}:{1}:{2}", _userManager.TenantId, CommonConst.CACHEKEYUSER, user.userId);
                    await _cacheManager.DelAsync(cacheKey);
                    // P0-2: 同步清除 CurrentUser 缓存
                    await _cacheManager.DelAsync($"CurrentUser:{_userManager.TenantId}:{user.userId}:Web");
                    await _cacheManager.DelAsync($"CurrentUser:{_userManager.TenantId}:{user.userId}:App");
                }
            }
        }
        catch (Exception)
        {
            FileHelper.MoveFile(Path.Combine(FileVariable.UserAvatarFilePath, headIcon), Path.Combine(FileVariable.TemporaryFilePath, headIcon));
            throw Oops.Oh(ErrorCode.D5004);
        }

        #region 第三方同步

        try
        {
            SysConfigOutput? sysConfig = await _sysConfigService.GetInfo();
            List<UserEntity>? userList = new List<UserEntity>();
            userList.Add(entity);
            if (sysConfig.dingSynIsSynUser)
                await _synThirdInfoService.SynUser(2, 3, sysConfig, userList);
            if (sysConfig.qyhIsSynUser)
                await _synThirdInfoService.SynUser(1, 3, sysConfig, userList);
        }
        catch (Exception)
        {
        }

        #endregion

        // 单点登录同步
        await syncUserInfo(entity, "update", _userManager.TenantId);
    }

    /// <summary>
    /// 更新状态.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/State")]
    public async Task UpdateState(string id, CancellationToken cancellationToken = default)
    {
        UserEntity? entity = await _repository.GetFirstAsync(it => it.Id == id);
        if (!_userManager.DataScope.Any(it => it.organizeId == entity.OrganizeId && it.Edit == true) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);
        if (!await _repository.IsAnyAsync(u => u.Id == id && u.DeleteMark == null))
            throw Oops.Oh(ErrorCode.D1002);
        int isOk = await _repository.AsUpdateable().SetColumns(it => new UserEntity()
        {
            EnabledMark = SqlFunc.IIF(it.EnabledMark == 1, 0, 1),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandAsync();

        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D5005);
    }

    /// <summary>
    /// 重置密码.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpPost("{id}/Actions/ResetPassword")]
    public async Task ResetPassword(string id, [FromBody] UserResetPasswordInput input, CancellationToken cancellationToken = default)
    {
        UserEntity? entity = await _repository.GetFirstAsync(u => u.Id == id && u.DeleteMark == null);

        // 所属组织 分级权限验证
        List<string>? orgIdList = await _repository.AsSugarClient().Queryable<UserRelationEntity>().Where(x => x.UserId == id && x.ObjectType == "Organize").Select(x => x.ObjectId).ToListAsync();
        if (!_userManager.DataScope.Any(it => orgIdList.Contains(it.organizeId) && it.Edit) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);

        if (!input.userPassword.Equals(input.validatePassword))
            throw Oops.Oh(ErrorCode.D5006);
        _ = entity ?? throw Oops.Oh(ErrorCode.D1002);

        string? password = MD5Encryption.Encrypt(input.userPassword + entity.Secretkey);

        int isOk = await _repository.AsUpdateable().SetColumns(it => new UserEntity()
        {
            Password = password,
            ChangePasswordDate = SqlFunc.GetDate(),
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandAsync();

        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D5005);

        // 重置该用户密码，该用户会立即退出登录
        var onlineCacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, _userManager.TenantId);
        var list = await _cacheManager.GetAsync<List<UserOnlineModel>>(onlineCacheKey);
        var user = list?.Find(it => it.tenantId == _userManager.TenantId && it.userId == id);
        if (user != null)
        {
            await _imHandler.SendMessageAsync(user.connectionId, new { method = "logout", msg = "密码已变更，请重新登录！" }.ToJsonString());

            // 删除在线用户ID
            list.RemoveAll((x) => x.connectionId == user.connectionId);
            await _cacheManager.SetAsync(onlineCacheKey, list);

            // 删除用户登录信息缓存
            var cacheKey = string.Format("{0}:{1}:{2}", _userManager.TenantId, CommonConst.CACHEKEYUSER, user.userId);
            await _cacheManager.DelAsync(cacheKey);
            // P0-2: 同步清除 CurrentUser 缓存
            await _cacheManager.DelAsync($"CurrentUser:{_userManager.TenantId}:{user.userId}:Web");
            await _cacheManager.DelAsync($"CurrentUser:{_userManager.TenantId}:{user.userId}:App");
        }

        // 单点登录同步
        entity.Password = input.userPassword;
        await syncUserInfo(entity, "modifyPassword", _userManager.TenantId);
    }

    /// <summary>
    /// 解除锁定.
    /// </summary>
    /// <param name="id">主键.</param>
    /// <returns></returns>
    [HttpPut("{id}/Actions/Unlock")]
    public async Task Unlock(string id, CancellationToken cancellationToken = default)
    {
        UserEntity? entity = await _repository.GetFirstAsync(u => u.Id == id && u.DeleteMark == null);
        if (!_userManager.DataScope.Any(it => it.organizeId == entity.OrganizeId && it.Edit) && !_userManager.IsAdministrator)
            throw Oops.Oh(ErrorCode.D1013);
        int isOk = await _repository.AsUpdateable().SetColumns(it => new UserEntity()
        {
            LockMark = 0, // 解锁
            LogErrorCount = 0, // 解锁
            EnabledMark = 1, // 解锁
            UnLockTime = DateTime.Now, // 取消解锁时间
            LastModifyUserId = _userManager.UserId,
            LastModifyTime = SqlFunc.GetDate()
        }).Where(it => it.Id == id).ExecuteCommandAsync();

        if (!(isOk > 0)) throw Oops.Oh(ErrorCode.D5005);
    }

    #endregion

    #region PrivateMethod

    /// <summary>
    /// 是否我的下属.
    /// </summary>
    /// <param name="userId">当前用户.</param>
    /// <param name="managerId">主管ID.</param>
    /// <param name="tier">层级.</param>
    /// <returns></returns>
    private async Task<bool> GetIsMyStaff(string userId, string managerId, int tier)
    {
        bool isMyStaff = false;
        if (tier <= 0) return true;
        string? superiorUserId = (await _repository.GetFirstAsync(it => it.Id.Equals(managerId) && it.DeleteMark == null))?.ManagerId;
        if (superiorUserId == null)
        {
            isMyStaff = false;
        }
        else if (userId == superiorUserId)
        {
            isMyStaff = true;
        }
        else
        {
            tier--;
            isMyStaff = await GetIsMyStaff(userId, superiorUserId, tier);
        }

        return isMyStaff;
    }

    #endregion

    #region 单点登录 数据同步

    /// <summary>
    /// 同步数据导maxkey.
    /// </summary>
    /// <param name="userEntity"></param>
    /// <param name="method"></param>
    /// <param name="tenantId"></param>
    public async Task syncUserInfo(UserEntity userEntity, string method, string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_oauthOptions.Enabled)
            {
                var userName = string.Format("{0}:{1}", _oauthOptions.Pull.UserName, _oauthOptions.Pull.Password).ToBase64String();
                var map = parse(userEntity);
                tenantId = tenantId != null && tenantId.Length > 0 ? tenantId : "1";
                if (tenantId.Equals("default")) tenantId = "1";
                map.Add("instId", tenantId);
                var resString = string.Empty;
                var headers = new Dictionary<string, object>();
                headers.Add("Authorization", _oauthOptions.Pull.CredentialType + " " + userName);

                if (method.Equals("create"))
                {
                    resString = await (_oauthOptions.Pull.CreateRestAddress + "?appId=" + _oauthOptions.Pull.UserName).SetHeaders(headers).SetBody(map).PostAsStringAsync();
                }
                else if (method.Equals("update"))
                {
                    resString = await (_oauthOptions.Pull.ReplaceRestAddress + "?appId=" + _oauthOptions.Pull.UserName).SetHeaders(headers).SetBody(map).PutAsStringAsync();
                }
                else if (method.Equals("delete"))
                {
                    resString = await (_oauthOptions.Pull.DeleteRestAddress + "?appId=" + _oauthOptions.Pull.UserName).SetHeaders(headers).SetBody(map).DeleteAsStringAsync();
                }
                else if (method.Equals("modifyPassword"))
                {
                    resString = await (_oauthOptions.Pull.ChangePasswordRestAddress + "?appId=" + _oauthOptions.Pull.UserName).SetHeaders(headers).SetBody(map).PostAsStringAsync();
                }

                //            else if (method.Equals("modifyPassword")) {
                //                jsonObject = HttpUtil.httpRequest(_oauthOptions.Pull.getGetRestAddress() + username
                //                        , "GET"
                //                        , null, _oauthOptions.Pull.getCredentialType() + " " + _oauthOptions.Pull.getUserName() + "Og==" + _oauthOptions.Pull.getPassword()
                //                        , null);
                //            }
            }
        }
        catch (Exception)
        {

        }
    }

    private Dictionary<string, object> parse(UserEntity userEntity)
    {
        var map = new Dictionary<string, object>();

        // map.Add("id", userEntity.get("id"));
        map.Add("username", userEntity.Account);
        map.Add("password", userEntity.Password);
        map.Add("mobile", userEntity.MobilePhone);
        map.Add("email", userEntity.Email);
        map.Add("gender", userEntity.Gender);
        map.Add("createdBy", userEntity.CreatorUserId);
        map.Add("createdDate", userEntity.CreatorTime);
        map.Add("modifiedBy", userEntity.LastModifyUserId);
        map.Add("modifiedDate", userEntity.LastModifyTime);
        map.Add("displayName", userEntity.RealName);

        // map.Add("managerId", userEntity.get("managerId"));
        // map.Add("departmentId", userEntity.get("organizeId"));
        map.Add("loginCount", userEntity.LogSuccessCount);
        map.Add("badPasswordCount", userEntity.LogErrorCount);
        map.Add("lastLoginIp", userEntity.LastLogIP);
        map.Add("lastLoginTime", userEntity.LastLogTime);
        map.Add("status", userEntity.EnabledMark != null ? (userEntity.EnabledMark == 1 ? 1 : 4) : 4);
        return map;
    }

    #endregion
}
