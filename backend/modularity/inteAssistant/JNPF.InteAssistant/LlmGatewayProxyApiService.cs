// 文件：LlmGatewayProxyApiService.cs
// 命名空间：JNPF.InteAssistant
// 职责：内部 LLM 代理端点 — 供 sa-service 等子进程统一调用后端 ILlmGatewayService
//       （含审计 BASE_AI_CALL_LOG、provider 熔断/降级、统一鉴权），避免子进程直连外部 LLM API

using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant;

/// <summary>
/// 内部 LLM 网关代理 — sa-service / codegen-host 等子进程通过此端点统一调用 LLM
///
/// 设计意图（对齐 sa-service HttpLlmClient 的"桥接后端 ILlmGatewayService"注释）：
///   - 子进程不直连 deepseek/openai，统一经后端 → 审计/熔断/降级/配额
///   - sa-service 配置 LLM_GATEWAY_URL=http://localhost:5000/api/studio/llm/internal-chat
///
/// 鉴权说明：[AllowAnonymous] — 内部子进程调用，部署时通过网络隔离保护（仅 localhost / 内网）。
///   生产强化方向：共享密钥（X-Internal-Key）+ IP 白名单。
/// </summary>
[Route("api/studio/llm")]
[AllowAnonymous]
public class LlmGatewayProxyApiService : IDynamicApiController, ITransient
{
    private readonly ILlmGatewayService _llmGateway;
    private readonly ILogger<LlmGatewayProxyApiService> _logger;

    public LlmGatewayProxyApiService(
        ILlmGatewayService llmGateway,
        ILogger<LlmGatewayProxyApiService> logger)
    {
        _llmGateway = llmGateway;
        _logger = logger;
    }

    /// <summary>
    /// 内部 LLM 聊天代理 — 接收 ChatCompletionRequest，转调 ILlmGatewayService.ChatAsync
    /// </summary>
    [HttpPost("internal-chat")]
    public async Task<object> InternalChatAsync([FromBody] ChatCompletionRequest request, CancellationToken ct)
    {
        // 兜底默认值：sa-service 可能只传 systemPrompt + messages（init-only 属性用 with 补齐）
        var normalized = request with
        {
            MaxTokens = request.MaxTokens > 0 ? request.MaxTokens : 4096,
            TimeoutMs = request.TimeoutMs > 0 ? request.TimeoutMs : 90_000,
            MaxRetries = request.MaxRetries > 0 ? request.MaxRetries : 2,
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await _llmGateway.ChatAsync(normalized, ct);
            sw.Stop();

            _logger.LogInformation(
                "LLM 代理调用: provider={Provider} success={Success} latency={Ms}ms tokens={In}/{Out}",
                request.ProviderCode, response.IsSuccess, sw.ElapsedMilliseconds,
                response.TokensIn, response.TokensOut);

            // 返回 RESTfulResult 包装（对齐 sa-service 期望的 { code, data: { content, isSuccess } }）
            return new
            {
                code = 200,
                msg = "操作成功",
                data = new
                {
                    content = response.Content,
                    isSuccess = response.IsSuccess,
                    error = response.Error,
                    tokensIn = response.TokensIn,
                    tokensOut = response.TokensOut,
                    latencyMs = response.LatencyMs,
                    providerUsed = request.ProviderCode,
                },
            };
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("LLM 代理调用超时: provider={Provider} timeout={Ms}ms", request.ProviderCode, sw.ElapsedMilliseconds);
            return new
            {
                code = 200,
                msg = "操作成功",
                data = new { content = "", isSuccess = false, error = "LLM 调用超时" },
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "LLM 代理调用异常: provider={Provider}", request.ProviderCode);
            return new
            {
                code = 200,
                msg = "操作成功",
                data = new { content = "", isSuccess = false, error = ex.Message },
            };
        }
    }
}
