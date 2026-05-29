using JNPF.Systems.Entitys.Dto.ModuleButton;
using JNPF.Systems.Entitys.System;

namespace JNPF.Systems.Interfaces.System;

/// <summary>
/// 功能按钮
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
public interface IModuleButtonService
{
    /// <summary>
    /// 列表.
    /// </summary>
    /// <param name="moduleId">功能id.</param>
    /// <returns></returns>
    Task<List<ModuleButtonEntity>> GetList(string? moduleId = default);

    /// <summary>
    /// 添加按钮.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<int> Create(ModuleButtonEntity entity);

    /// <summary>
    /// 获取用户功能按钮.
    /// <param name="moduleIdList">功能ids.</param>
    /// </summary>
    Task<List<ModuleButtonOutput>> GetUserModuleButtonList(List<string> moduleIdList);
}