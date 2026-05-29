using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DictionaryData;

/// <summary>
/// 数据字典下拉框数据不为树输出.
/// </summary>
[SuppressSniffer]
public class DictionaryDataSelectorDataNotTreeOutput
{
    /// <summary>
    /// 主键id.
    /// </summary>
    /// <returns></returns>
    public string id { get; set; }

    /// <summary>
    /// 项目名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }
}