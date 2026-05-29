using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Authorize;

/// <summary>
/// 权限数据应用权限输出模型.
/// </summary>
[SuppressSniffer]
public class AuthorizeSystemDataModelOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
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

    /// <summary>
    /// 系统Id.
    /// </summary>
    public string systemId { get; set; }

    /// <summary>
    /// 是否置灰 true是置灰.
    /// </summary>
    public bool disabled { get; set; }
}