// 文件：Infrastructure/Messaging/SseSender.cs
// 命名空间：JNPF.InteAssistant.Infrastructure.Messaging
// 职责：SSE 消息发送器

using System.Text.Json;
using System.Threading.Channels;

namespace JNPF.InteAssistant.Infrastructure.Messaging;

/// <summary>
/// SSE 消息发送器
///
/// 线程安全：Interlocked.Exchange 保证 Complete/Error 只执行一次
/// 背压：有界 Channel + WaitToWriteAsync
/// 性能：静态 JsonSerializerOptions 缓存反射元数据
///
/// 用法：
///   using var sse = senderFactory.Create(pipelineId);
///   sse.Token("AI回复");
///   sse.Document(new DocumentInfo(...));
///   sse.StageComplete(new StageCompleteInfo(...));
///   sse.Complete();
/// </summary>
public sealed class SseSender : IDisposable
{
    private readonly Channel<SseEvent> _channel;
    private readonly string _pipelineId;

    // 0=未完成, 1=已完成——Interlocked 保证原子性
    private int _completedFlag;
    private int _messageCount;

    // 静态 JSON 选项——反射元数据缓存
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    internal SseSender(Channel<SseEvent> channel, string pipelineId)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _pipelineId = pipelineId ?? throw new ArgumentNullException(nameof(pipelineId));
    }

    public Channel<SseEvent> Channel => _channel;
    public int MessageCount => _messageCount;
    public bool IsCompleted => _completedFlag == 1;
    public string PipelineId => _pipelineId;

    /// <summary>发送 token（同步，Channel 满时丢弃最旧）</summary>
    public bool Token(string content)
    {
        if (_completedFlag == 1 || string.IsNullOrEmpty(content)) return false;
        var success = _channel.Writer.TryWrite(new SseEvent(SseEventType.Token, content));
        if (success) Interlocked.Increment(ref _messageCount);
        return success;
    }

    /// <summary>发送 token（异步，Channel 满时等待空间，不丢数据）</summary>
    public async ValueTask<bool> TokenAsync(string content, CancellationToken ct = default)
    {
        if (_completedFlag == 1 || string.IsNullOrEmpty(content)) return false;
        await _channel.Writer.WaitToWriteAsync(ct);
        return Token(content);
    }

    /// <summary>发送思考过程</summary>
    public bool Thinking(string content)
    {
        if (_completedFlag == 1 || string.IsNullOrEmpty(content)) return false;
        return _channel.Writer.TryWrite(new SseEvent(SseEventType.Thinking, content));
    }

    /// <summary>发送文档下载事件</summary>
    public bool Document(DocumentInfo info)
    {
        if (_completedFlag == 1 || info == null) return false;
        var data = JsonSerializer.Serialize(info, s_jsonOptions);
        return _channel.Writer.TryWrite(new SseEvent(SseEventType.Document, data));
    }

    /// <summary>发送阶段完成事件</summary>
    public bool StageComplete(StageCompleteInfo info)
    {
        if (_completedFlag == 1 || info == null) return false;
        var data = JsonSerializer.Serialize(info, s_jsonOptions);
        return _channel.Writer.TryWrite(new SseEvent(SseEventType.StageComplete, data));
    }

    /// <summary>
    /// 发送错误——只发 error，不发 done
    /// </summary>
    public void Error(string message)
    {
        if (Interlocked.Exchange(ref _completedFlag, 1) == 1) return;
        _channel.Writer.TryWrite(new SseEvent(SseEventType.Error, message));
        _channel.Writer.TryComplete();
    }

    /// <summary>
    /// 正常完成——发 done 后关闭 Channel
    /// </summary>
    public void Complete()
    {
        if (Interlocked.Exchange(ref _completedFlag, 1) == 1) return;

        if (!_channel.Writer.TryWrite(new SseEvent(SseEventType.Done)))
        {
            // Channel 满时异步等待写入 Done
            _ = _channel.Writer.WriteAsync(new SseEvent(SseEventType.Done))
                .AsTask()
                .ContinueWith(_ => _channel.Writer.TryComplete());
            return;
        }

        _channel.Writer.TryComplete();
    }

    /// <summary>安全写入——Channel 已关闭时不抛异常</summary>
    public bool TrySend(string eventType, string data)
    {
        if (_completedFlag == 1) return false;
        return _channel.Writer.TryWrite(new SseEvent(eventType, data));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _completedFlag, 1) == 1) return;
        _channel.Writer.TryComplete();
    }
}
