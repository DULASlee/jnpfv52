namespace JNPF.InteAssistant.Entitys.Dto.InteAssistant;

/// <summary>
/// 聊天补全请求
/// 对齐前端 src/core/ai/llm/types.ts ChatRequest
/// </summary>
public record ChatCompletionRequest
{
    /// <summary>
    /// 供应商代码 ("deepseek", "mimo", "openai", "ollama")
    /// </summary>
    public string ProviderCode { get; init; } = "";

    /// <summary>
    /// 模型代码（可选，默认使用 Provider 的 DefaultModel）
    /// </summary>
    public string? ModelCode { get; init; }

    /// <summary>
    /// 系统提示词
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// 消息列表
    /// </summary>
    public List<ChatMessage> Messages { get; init; } = new();

    /// <summary>
    /// 温度参数 (0-2，默认 0.7)
    /// </summary>
    public double Temperature { get; init; } = 0.7;

    /// <summary>
    /// 最大输出 Token 数（默认 4096）
    /// </summary>
    public int MaxTokens { get; init; } = 4096;

    /// <summary>
    /// 响应格式 ("text", "json")
    /// </summary>
    public string? ResponseFormat { get; init; }

    /// <summary>
    /// 最大重试次数（默认 3）
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// 超时毫秒（默认 120000）
    /// </summary>
    public int TimeoutMs { get; init; } = 120000;
}

/// <summary>
/// 聊天消息
/// </summary>
public record ChatMessage
{
    public string Role { get; init; } = "user";
    public string Content { get; init; } = "";

    public ChatMessage() { }

    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}
