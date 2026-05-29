using JNPF.Systems.Entitys.Dto.DbLink;
using JNPF.Systems.Entitys.System;

namespace JNPF.Systems.Interfaces.System;

/// <summary>
/// 数据连接
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
public interface IDbLinkService
{
    /// <summary>
    /// 列表.
    /// </summary>
    /// <returns></returns>
    Task<List<DbLinkListOutput>> GetList();

    /// <summary>
    /// 信息.
    /// </summary>
    /// <param name="id">主键值.</param>
    /// <returns></returns>
    Task<DbLinkEntity> GetInfo(string id);
}