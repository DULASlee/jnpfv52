using JNPF.Common.Security;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DataInterFace;

/// <summary>
/// 数据接口下拉框输出.
/// </summary>
[SuppressSniffer]
public class DataInterfaceSelectorOutput : TreeModel
{
    /// <summary>
    /// 分类id.
    /// </summary>
    public string categoryId { get; set; }

    /// <summary>
    /// 接口名.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? sortCode { get; set; }
}