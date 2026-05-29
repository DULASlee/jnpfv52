using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Signature;

/// <summary>
/// 签章管理列表查询输入.
/// </summary>
[SuppressSniffer]
public class SignatureListInput : PageInputBase
{
    /// <summary>
    /// 授权人.
    /// </summary>
    public string userId { get; set; }
}