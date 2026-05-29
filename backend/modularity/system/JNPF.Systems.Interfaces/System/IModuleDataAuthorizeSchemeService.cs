using JNPF.Systems.Entitys.Dto.ModuleDataAuthorizeScheme;
using JNPF.Systems.Entitys.System;

namespace JNPF.Systems.Interfaces.System;

/// <summary>
/// 数据权限方案
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
public interface IModuleDataAuthorizeSchemeService
{
    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="moduleId">功能id.</param>
    /// <returns></returns>
    Task<List<ModuleDataAuthorizeSchemeEntity>> GetList(string? moduleId = default);

    /// <summary>
    /// 获取用户数据权限方案.
    /// <param name="moduleIdList">功能ids.</param>
    /// </summary>
    Task<List<ModuleDataAuthorizeSchemeOutput>> GetResourceList(List<string> moduleIdList);
}