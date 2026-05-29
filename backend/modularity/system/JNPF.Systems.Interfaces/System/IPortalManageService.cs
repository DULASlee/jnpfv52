using JNPF.Systems.Entitys.Dto.System.PortalManage;

namespace JNPF.Systems.Interfaces.System;

/// <summary>
/// 业务契约：门户管理
/// 版 本：V3.5.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2024-01-26.
/// </summary>
public interface IPortalManageService
{
    /// <summary>
    /// 获取用户授权的门户.
    /// </summary>
    /// <param name="systemId"></param>
    /// <returns></returns>
    Task<List<SysPortalManageOutput>> GetSysPortalManageList(string systemId);
}