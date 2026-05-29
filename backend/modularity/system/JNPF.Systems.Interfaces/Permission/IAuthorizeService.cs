using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;

namespace JNPF.Systems.Interfaces.Permission;

/// <summary>
/// 业务契约：操作权限.
/// </summary>
public interface IAuthorizeService
{
    /// <summary>
    /// 当前用户模块权限.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <param name="systemIds">当前系统Ids .</param>
    /// <returns></returns>
    Task<List<ModuleEntity>> GetCurrentUserModuleAuthorize(string userId, string[] systemIds);

    /// <summary>
    /// 当前用户模块按钮权限.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    Task<List<ModuleButtonEntity>> GetCurrentUserButtonAuthorize(string userId);

    /// <summary>
    /// 当前用户模块列权限.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    Task<List<ModuleColumnEntity>> GetCurrentUserColumnAuthorize(string userId);

    /// <summary>
    /// 当前用户模块权限资源.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    Task<List<ModuleDataAuthorizeSchemeEntity>> GetCurrentUserResourceAuthorize(string userId);

    /// <summary>
    /// 当前用户模块表单权限.
    /// </summary>
    /// <param name="userId">用户ID.</param>
    /// <returns></returns>
    Task<List<ModuleFormEntity>> GetCurrentUserFormAuthorize(string userId);

    /// <summary>
    /// 获取权限项ids.
    /// </summary>
    /// <param name="roleId">角色id.</param>
    /// <param name="itemType">项类型.</param>
    /// <returns></returns>
    Task<List<string>> GetAuthorizeItemIds(string roleId, string itemType);

    /// <summary>
    /// 是否存在权限资源.
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<bool> GetIsExistModuleDataAuthorizeScheme(string[] ids);

    /// <summary>
    /// 获取权限列表.
    /// </summary>
    /// <param name="objectId">对象主键.</param>
    /// <returns></returns>
    Task<List<AuthorizeEntity>> GetAuthorizeListByObjectId(string objectId);
}