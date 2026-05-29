using JNPF.Systems.Entitys.Dto.ModuleForm;
using JNPF.Systems.Entitys.System;

namespace JNPF.Systems.Interfaces.System;

/// <summary>
/// 表单权限
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
public interface IModuleFormService
{
    /// <summary>
    /// 表单权限列表.
    /// </summary>
    /// <param name="moduleId">功能id.</param>
    /// <returns></returns>
    Task<List<ModuleFormEntity>> GetList(string? moduleId = default);

    /// <summary>
    /// 获取用户功能表单.
    /// <param name="moduleIdList">功能ids.</param>
    /// </summary>
    Task<List<ModuleFormOutput>> GetUserModuleFormList(List<string> moduleIdList);
}