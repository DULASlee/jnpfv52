using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Signature;

/// <summary>
/// 签章管理修改输入.
/// </summary>
[SuppressSniffer]
public class SignatureUpInput : SignatureCrInput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }
}