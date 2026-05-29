using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DictionaryData;

/// <summary>
/// 数据字典列表查询输入.
/// </summary>
[SuppressSniffer]
public class DictionaryDataListQuery
{
    /// <summary>
    /// 是否树形.
    /// </summary>
    public string isTree { get; set; }

    /// <summary>
    /// 搜索关键字.
    /// </summary>
    public string keyword { get; set; }
}