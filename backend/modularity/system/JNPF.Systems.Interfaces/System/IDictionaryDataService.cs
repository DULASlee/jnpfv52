using JNPF.Systems.Entitys.System;

namespace JNPF.Systems.Interfaces.System;

/// <summary>
/// 字典数据
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
public interface IDictionaryDataService
{
    /// <summary>
    /// 获取数据字典列表.
    /// </summary>
    /// <param name="dictionaryTypeId">分类id或编码.</param>
    /// <param name="enabledMark">是否过滤启用状态.</param>
    /// <returns></returns>
    Task<List<DictionaryDataEntity>> GetList(string dictionaryTypeId, bool enabledMark = true);

    /// <summary>
    /// 获取按钮信息.
    /// </summary>
    /// <param name="id">主键id.</param>
    /// <returns></returns>
    Task<DictionaryDataEntity> GetInfo(string id);
}