using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DataInterFace;

/// <summary>
/// 数据接口列表查询输入.
/// </summary>
[SuppressSniffer]
public class DataInterfaceListQuery : PageInputBase
{
    /// <summary>
    /// 分类id.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 数据类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 启用标识.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 条件类型.
    /// </summary>
    public int? sourceType { get; set; }
}