using JNPF.Systems.Entitys.Dto.Module;
using JNPF.Systems.Entitys.System;

namespace JNPF.Systems.Interfaces.System;

/// <summary>
/// 菜单管理
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
public interface IModuleService
{
    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    Task<List<ModuleEntity>> GetList(string systemId);

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    Task<ModuleEntity> GetInfo(string id);

    /// <summary>
    /// 获取用户菜单树.
    /// </summary>
    /// <param name="type">登录类型.</param>
    /// <param name="systemId">SystemId.</param>
    /// <returns></returns>
    Task<List<ModuleNodeOutput>> GetUserTreeModuleList(string type, string systemId = "");

    /// <summary>
    /// 获取用户菜单树.
    /// </summary>
    /// <param name="type">登录类型.</param>
    /// <param name="systemId">SystemId.</param>
    /// <param name="mIds">指定过滤Ids.</param>
    /// <param name="mUrls">指定过滤Urls.</param>
    /// <returns></returns>
    Task<List<ModuleNodeOutput>> GetUserModuleListByIds(string type, string systemId = "", List<string> mIds = null, List<string> mUrls = null);

    /// <summary>
    /// 获取用户树形模块功能列表.
    /// </summary>
    /// <param name="type">登录类型.</param>
    /// <param name="systemId">SystemId.</param>
    Task<List<ModuleNodeOutput>> GetUserModuleList(string type, string systemId = "");

    /// <summary>
    /// 获取用户菜单树（使用预取的授权模块ID，避免循环内重复查询AuthorizeEntity）.
    /// </summary>
    /// <param name="type">登录类型.</param>
    /// <param name="authorizeModuleIds">预取的授权模块ID列表.</param>
    /// <param name="mIds">指定过滤Ids.</param>
    /// <param name="mUrls">指定过滤Urls.</param>
    /// <returns></returns>
    Task<List<ModuleNodeOutput>> GetUserModuleListWithAuthIds(string type, List<string> authorizeModuleIds, List<string> mIds = null, List<string> mUrls = null);
}