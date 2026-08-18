// 文件：Gates/IGatePipeline.cs
// 命名空间：JNPF.InteAssistant.Gates

using JNPF.InteAssistant.Infrastructure.Background;

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 门控管道接口
///
/// 稳定接口——内部步骤变化不影响调用方
/// </summary>
public interface IGatePipeline
{
    /// <summary>
    /// 执行门控管道
    /// </summary>
    /// <param name="gateContext">可选上下文（扩展点，当前未使用）</param>
    Task<GateResult> ExecuteAsync(
        string userText,
        List<AttachmentFile> attachments,
        RequestContext ctx,
        object? gateContext = null,
        string visionApiUrl = "",
        string visionApiKey = "",
        string visionModel = "",
        CancellationToken ct = default);
}
