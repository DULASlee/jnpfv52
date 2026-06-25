namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// 聊天补全响应
/// 对齐前端 src/core/ai/llm/types.ts ChatResponse
/// </summary>
public record ChatCompletionResponse
{
    /// <summary>
    /// 响应内容
    /// </summary>
    public string Content { get; init; } = "";

    /// <summary>
    /// 实际使用的模型
    /// </summary>
    public string ModelUsed { get; init; } = "";

    /// <summary>
    /// 输入 Token 数
    /// </summary>
    public int TokensIn { get; init; }

    /// <summary>
    /// 输出 Token 数
    /// </summary>
    public int TokensOut { get; init; }

    /// <summary>
    /// 延迟（毫秒）
    /// </summary>
    public int LatencyMs { get; init; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 错误信息（IsSuccess=false 时）
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Provider 信息
/// </summary>
public record ProviderInfo
{
    public string ProviderCode { get; init; } = "";
    public string ModelName { get; init; } = "";
}
