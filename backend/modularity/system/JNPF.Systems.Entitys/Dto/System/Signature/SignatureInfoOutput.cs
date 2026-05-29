using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Signature;

/// <summary>
/// 签章管理信息输出.
/// </summary>
[SuppressSniffer]
public class SignatureInfoOutput
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
    /// 编号.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 授权人.
    /// </summary>
    public List<string> userIds { get; set; }

    /// <summary>
    /// 签章.
    /// </summary>
    public string icon { get; set; }
}