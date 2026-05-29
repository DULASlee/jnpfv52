using JNPF.DependencyInjection;

namespace JNPF.Common.Models.User;

/// <summary>
/// 用户子系统.
/// </summary>
[SuppressSniffer]
public class UserSystemModel
{
    /// <summary>
    /// 系统id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 系统名称.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// 系统图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 是否当前系统.
    /// </summary>
    public bool currentSystem { get; set; } = false;

    /// <summary>
    /// 是否为开发的系统
    /// </summary>
    public bool devSystem { get; set; } = false;

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? sortCode { get; set; }
}
