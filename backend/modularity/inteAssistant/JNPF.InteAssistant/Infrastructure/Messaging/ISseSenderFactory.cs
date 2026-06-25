// 文件：Infrastructure/Messaging/ISseSenderFactory.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Messaging

using System.Threading.Channels;

namespace JNPF.InteAssistant.Infrastructure.Messaging;

/// <summary>
/// SSE 发送器工厂接口
///
/// 不同模块（SA门控、代码生成、报表导出）创建自己的 Sender 实例
/// </summary>
public interface ISseSenderFactory
{
    /// <summary>创建 SSE 发送器</summary>
    /// <param name="pipelineId">关联的 Pipeline ID</param>
    /// <param name="capacity">有界 Channel 容量（默认1000）</param>
    SseSender Create(string pipelineId, int capacity = 1000);

    /// <summary>创建 SSE 发送器并关联到 Channel</summary>
    SseSender Create(string pipelineId, Channel<SseEvent> channel);
}
