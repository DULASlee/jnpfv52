using JNPF.DependencyInjection;

namespace JNPF.Common.Models;

/// <summary>
/// 静态数据模型.
/// </summary>
[SuppressSniffer]
public class StaticDataModel : StaticData
{
    /// <summary>
    /// 子级.
    /// </summary>
    public List<StaticDataModel> children { get; set; }
}

/// <summary>
/// 静态数据.
/// </summary>
public class StaticData
{
    /// <summary>
    /// 选项名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 选项值.
    /// </summary>
    public string id { get; set; }
}