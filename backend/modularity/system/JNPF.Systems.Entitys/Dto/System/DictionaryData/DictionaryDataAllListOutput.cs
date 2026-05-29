using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DictionaryData;

/// <summary>
/// 数据字典全部列表输出.
/// </summary>
[SuppressSniffer]
public class DictionaryDataAllListOutput
{
    /// <summary>
    /// 字典分类id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 是否树形.
    /// </summary>
    public int isTree { get; set; }

    /// <summary>
    /// 字典分类编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 字典分类名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 字典列表.
    /// </summary>
    public object dictionaryList { get; set; }
}