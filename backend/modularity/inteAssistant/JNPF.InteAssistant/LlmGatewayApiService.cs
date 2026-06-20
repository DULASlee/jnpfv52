using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant;

/// <summary>
/// LLM 网关 HTTP 入口 — 供 SA Service 等内部服务调用。
/// 对齐 start-dev.ps1 中 LLM_GATEWAY_URL=http://localhost:5000/api/LlmGateway/ChatAsync
/// </summary>
[ApiDescriptionSettings(Tag = "AI", Name = "LlmGateway", Order = 184)]
public class LlmGatewayApiService : IDynamicApiController, ITransient
{
    private readonly ILlmGatewayService _gateway;
    private readonly ILogger<LlmGatewayApiService> _logger;

    public LlmGatewayApiService(ILlmGatewayService gateway, ILogger<LlmGatewayApiService> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    /// <summary>
    /// 非流式聊天补全 — SA Service HttpLlmClient 调用此端点。
    /// </summary>
    [HttpPost("/api/LlmGateway/ChatAsync")]
    [AllowAnonymous]
    public async Task<ChatCompletionResponse> ChatAsync(
        [FromBody] ChatCompletionRequest request, CancellationToken ct)
    {
        _logger.LogInformation(
            "LlmGateway HTTP: Provider={Provider}, Messages={Count}",
            string.IsNullOrWhiteSpace(request.ProviderCode) ? "(default)" : request.ProviderCode,
            request.Messages?.Count ?? 0);

        return await _gateway.ChatAsync(request, ct);
    }
}
