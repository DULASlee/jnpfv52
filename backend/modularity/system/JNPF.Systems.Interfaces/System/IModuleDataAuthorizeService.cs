using JNPF.Systems.Entitys.System;

namespace JNPF.Systems.Interfaces.System;

/// <summary>
/// 数据权限
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
public interface IModuleDataAuthorizeService
{
    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="moduleId">功能id.</param>
    /// <returns></returns>
    Task<List<ModuleDataAuthorizeEntity>> GetList(string? moduleId = default);
}