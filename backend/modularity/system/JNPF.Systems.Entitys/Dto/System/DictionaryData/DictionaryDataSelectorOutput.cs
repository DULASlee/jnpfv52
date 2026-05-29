using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DictionaryData;

/// <summary>
/// 数据字典下拉框输出.
/// </summary>
[SuppressSniffer]
public class DictionaryDataSelectorOutput : TreeModel
{
    /// <summary>
    /// 项目名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }
}