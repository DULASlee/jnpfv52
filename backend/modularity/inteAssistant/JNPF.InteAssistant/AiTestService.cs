using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant;

/// <summary>
/// AI 测试 API — 验证 LLM 网关联通性
/// 对齐文档 Sprint 0-B 地桩 9: LLM 测试端点
/// </summary>
[ApiDescriptionSettings(Tag = "AI", Name = "AiTest", Order = 185)]
public class AiTestService : IDynamicApiController, ITransient
{
    private readonly ILlmGatewayService _gateway;
    private readonly ILogger<AiTestService> _logger;

    public AiTestService(ILlmGatewayService gateway, ILogger<AiTestService> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    /// <summary>
    /// 测试 LLM 聊天
    /// </summary>
    [HttpPost("/api/founder/ai/test")]
    public async Task<ChatCompletionResponse> TestChatAsync(
        [FromBody] TestChatRequest request, CancellationToken ct)
    {
        _logger.LogInformation("AI test chat requested: {Prompt}",
            request.Prompt.Length > 100 ? request.Prompt[..100] : request.Prompt);

        var chatRequest = new ChatCompletionRequest
        {
            ProviderCode = request.ProviderCode ?? "",
            Messages = [new ChatMessage("user", request.Prompt)],
            MaxRetries = 1,
            TimeoutMs = 60000
        };

        return await _gateway.ChatAsync(chatRequest, ct);
    }

    /// <summary>
    /// 测试 LLM 健康检查
    /// </summary>
    [HttpPost("/api/founder/ai/health")]
    public async Task<object> TestHealthAsync(
        [FromBody] TestHealthRequest request, CancellationToken ct)
    {
        var providerCode = request.ProviderCode ?? "";
        if (providerCode.Length == 0)
        {
            // 检查所有 Provider
            var results = new Dictionary<string, bool>();
            foreach (var code in new[] { "mimo", "deepseek", "openai", "ollama" })
            {
                results[code] = await _gateway.HealthCheckAsync(code, ct);
            }
            return new { providers = results };
        }

        var healthy = await _gateway.HealthCheckAsync(providerCode, ct);
        var info = await _gateway.GetProviderInfoAsync(providerCode);

        return new
        {
            provider = providerCode,
            model = info.ModelName,
            healthy
        };
    }
}

/// <summary>
/// 测试聊天请求
/// </summary>
public record TestChatRequest
{
    public string Prompt { get; init; } = "";
    public string? ProviderCode { get; init; }
}

/// <summary>
/// 测试健康检查请求
/// </summary>
public record TestHealthRequest
{
    public string? ProviderCode { get; init; }
}
