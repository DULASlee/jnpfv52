using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// KnowledgePatch 增量包
/// Foundry → Studio 的知识传递安全通道
/// </summary>
[SuppressSniffer]
public class KnowledgeIncrementPackage
{
    /// <summary>
    /// 知识内容 JSON
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 内容哈希（SHA256）
    /// </summary>
    public string PackageHash { get; set; }

    /// <summary>
    /// 签名（对 PackageHash 的 HMAC-SHA256）
    /// </summary>
    public string Signature { get; set; }
}
