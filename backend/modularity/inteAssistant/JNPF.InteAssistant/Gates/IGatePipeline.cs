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
    Task<GateResult> ExecuteAsync(
        string userText,
        List<AttachmentFile> attachments,
        RequestContext ctx,
        string visionApiUrl = "",
        string visionApiKey = "",
        string visionModel = "",
        CancellationToken ct = default);
}
