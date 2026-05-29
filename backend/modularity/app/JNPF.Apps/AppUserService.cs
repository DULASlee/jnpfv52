using JNPF.Apps.Entitys.Dto;
using JNPF.Common.Core.Manager;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.Apps;

/// <summary>
/// App用户信息
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "App", Name = "User", Order = 800)]
[Route("api/App/[controller]")]
public class AppUserService : IDynamicApiController, ITransient
{
    /// <summary>
    /// 用户信息.
    /// </summary>
    private readonly IUsersService _usersService;

    /// <summary>
    /// 部门管理.
    /// </summary>
    private readonly IDepartmentService _departmentService;

    /// <summary>
    /// 岗位管理.
    /// </summary>
    private readonly IPositionService _positionService;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 构造.
    /// </summary>
    /// <param name="usersService"></param>
    /// <param name="departmentService"></param>
    /// <param name="positionService"></param>
    /// <param name="userManager"></param>
    public AppUserService(IUsersService usersService, IDepartmentService departmentService, IPositionService positionService, IUserManager userManager)
    {
        _usersService = usersService;
        _departmentService = departmentService;
        _positionService = positionService;
        _userManager = userManager;
    }

    #region Get

    /// <summary>
    /// 用户信息.
    /// </summary>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetInfo()
    {
        UserEntity? userEntity = _usersService.GetInfoByUserId(_userManager.UserId);
        AppUserOutput? appUserInfo = userEntity.Adapt<AppUserOutput>();
        appUserInfo.positionIds = userEntity.PositionId == null ? null : await _usersService.GetPosition(userEntity.OrganizeId);
        appUserInfo.departmentName = _departmentService.GetOrganizeNameTree(userEntity.OrganizeId);
        appUserInfo.organizeId = _departmentService.GetCompanyId(userEntity.OrganizeId);
        appUserInfo.organizeName = appUserInfo.departmentName;

        // 获取当前组织角色和全局角色
        List<string>? roleList = _userManager.GetUserRoleIds(userEntity.RoleId, userEntity.OrganizeId);
        appUserInfo.roleName = await _userManager.GetRoleNameByIds(string.Join(",", roleList));
        appUserInfo.manager = await _usersService.GetUserName(userEntity.ManagerId);
        return appUserInfo;
    }

    /// <summary>
    /// 用户信息.
    /// </summary>
    /// <returns></returns>
    [HttpGet("{id}")]
    public dynamic GetInfo(string id)
    {
        UserEntity? userEntity = _usersService.GetInfoByUserId(id);
        AppUserInfoOutput? appUserInfo = userEntity.Adapt<AppUserInfoOutput>();
        appUserInfo.organizeName = _departmentService.GetDepName(userEntity.OrganizeId);
        appUserInfo.positionName = _positionService.GetName(userEntity.PositionId);
        return appUserInfo;
    }

    #endregion
}