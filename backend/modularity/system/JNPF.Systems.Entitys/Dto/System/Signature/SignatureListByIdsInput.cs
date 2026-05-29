using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Signature;

/// <summary>
/// 签章管理下拉列表查询输入.
/// </summary>
[SuppressSniffer]
public class SignatureListByIdsInput
{
    /// <summary>
    /// ids.
    /// </summary>
    public List<string> ids { get; set; }
}