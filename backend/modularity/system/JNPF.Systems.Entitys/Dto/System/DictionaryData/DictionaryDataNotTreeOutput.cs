using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DictionaryData;

/// <summary>
/// 数据字典不为树形输出.
/// </summary>
[SuppressSniffer]
public class DictionaryDataNotTreeOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 项目名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 项目编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 状态(1-可用,0-禁用).
    /// </summary>
    public int enabledMark { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }
}
