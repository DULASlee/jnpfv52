using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Signature;

/// <summary>
/// 签章管理列表查询输入.
/// </summary>
[SuppressSniffer]
public class SignatureCrInput
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 签章.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 授权人.
    /// </summary>
    public List<string> userIds { get; set; }
}