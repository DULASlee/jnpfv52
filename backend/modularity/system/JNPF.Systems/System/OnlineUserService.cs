using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Extension;
using JNPF.Common.Filter;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Message.Entitys.Dto.IM;
using JNPF.Message.Interfaces;
using JNPF.Systems.Entitys.Dto.OnlineUser;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Systems;

/// <summary>
///  业务实现：在线用户.
/// </summary>
[ApiDescriptionSettings(Tag = "System", Name = "OnlineUser", Order = 176)]
[Route("api/system/[controller]")]
public class OnlineUserService : IDynamicApiController, ITransient
{
    /// <summary>
    /// IM回应服务.
    /// </summary>
    private readonly IImReplyService _imReplyService;

    /// <summary>
    /// 缓存管理器.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 组织管理.
    /// </summary>
    private readonly IOrganizeService _organizeService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUsersService _usersService;

    /// <summary>
    /// 初始化一个<see cref="OnlineUserService"/>类型的新实例.
    /// </summary>
    public OnlineUserService(
        IImReplyService imReplyService,
        ICacheManager cacheManager,
        IOrganizeService organizeService,
        IUsersService usersService,
        IUserManager userManager)
    {
        _imReplyService = imReplyService;
        _organizeService = organizeService;
        _usersService = usersService;
        _cacheManager = cacheManager;
        _userManager = userManager;
    }

    /// <summary>
    /// 获取在线用户列表.
    /// </summary>
    /// <param name="input">参数.</param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList([FromQuery] PageInputBase input)
    {
        var tenantId = _userManager.TenantId;
        var userOnlineList = new List<UserOnlineModel>();
        var onlineKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, tenantId);
        if (_cacheManager.Exists(onlineKey))
        {
            userOnlineList = await GetOnlineUserList(tenantId);
            userOnlineList.ForEach(x => x.userId = x.connectionId);
            if (!input.keyword.IsNullOrEmpty())
                userOnlineList = userOnlineList.FindAll(x => x.userName.Contains(input.keyword));
        }

        var data = userOnlineList.Adapt<List<OnlineUserListOutput>>();

        if (data != null && data.Any())
        {
            // 处理组织树 名称
            List<OrganizeEntity>? orgList = _organizeService.GetOrgListTreeName();

            // 获取全部用户当前组织id
            var userOrgIds = await _usersService.GetUserListByExp(x => data.Select(xx => xx.userAccount).Contains(x.Account) && x.DeleteMark == null && x.EnabledMark == 1, u => new UserEntity() { Account = u.Account, OrganizeId = u.OrganizeId });
            data.ForEach(item =>
            {
                item.organize = orgList.Find(o => o.Id.Equals(userOrgIds.Find(x => x.Account.Equals(item.userAccount))?.OrganizeId))?.Description;
            });
        }

        return PageResult<OnlineUserListOutput>.SqlSugarPageResult(new SqlSugar.SqlSugarPagedList<OnlineUserListOutput>()
        {
            list = data,
            pagination = new SqlSugar.Pagination() { CurrentPage = input.currentPage, PageSize = input.pageSize, Total = data.Count }
        });
    }

    /// <summary>
    /// 强制下线.
    /// </summary>
    /// <param name="id"></param>
    [HttpDelete("{id}")]
    public async Task ForcedOffline(string id)
    {
        var tenantId = _userManager.TenantId;
        var list = await GetOnlineUserList(tenantId);
        var userList = list.FindAll(it => it.tenantId == tenantId && it.connectionId == id);
        userList.ForEach(async item =>
        {
            _imReplyService.ForcedOffline(item.connectionId);
            await DelOnlineUser(tenantId, item.userId);
            await DelUserInfo(tenantId, item.userId);
        });
    }

    /// <summary>
    /// 批量下线在线用户.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input">下线用户信息.</param>
    [HttpDelete("")]
    public async Task Clear(string id, [FromBody] BatchOnlineInput input)
    {
        var tenantId = _userManager.TenantId;
        var list = await GetOnlineUserList(tenantId);
        var userList = list.FindAll(it => it.tenantId == tenantId && input.ids.Contains(it.userId));
        userList.ForEach(async item =>
        {
            _imReplyService.ForcedOffline(item.connectionId);
            await DelOnlineUser(tenantId, item.userId);
            await DelUserInfo(tenantId, item.userId);
        });

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
    public async Task<bool> DelOnlineUser(string tenantId, string userId)
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
    public async Task<bool> DelUserInfo(string tenantId, string userId)
    {
        var cacheKey = string.Format("{0}:{1}:{2}", tenantId, CommonConst.CACHEKEYUSER, userId);
        return await _cacheManager.DelAsync(cacheKey);
    }
}