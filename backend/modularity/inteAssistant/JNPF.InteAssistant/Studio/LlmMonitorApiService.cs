using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.InteAssistant.Llm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// LLM 监控 API——供 E2E 验收熔断器运行时行为与缓存命中率。
/// 28 号 §5：电路断路器 + 响应缓存验收端点。
/// </summary>
[ApiDescriptionSettings(Tag = "Studio", Name = "StudioLlmMonitor", Order = 196)]
[AllowAnonymous]
[Route("api/studio/llm")]
public class LlmMonitorApiService : IDynamicApiController, ITransient
{
    private readonly ILlmCircuitBreaker _circuitBreaker;

    public LlmMonitorApiService(ILlmCircuitBreaker circuitBreaker)
    {
        _circuitBreaker = circuitBreaker;
    }

    /// <summary>查询所有已知 Provider 的熔断器状态（供 E2E 断言 Closed→Open→HalfOpen 转换）。</summary>
    [HttpGet("circuit-breaker/status")]
    public object GetCircuitBreakerStatus()
    {
        var knownProviders = new[] { "deepseek", "mimo", "openai", "ollama" };
        var providers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var code in knownProviders)
        {
            var state = _circuitBreaker.GetState(code);
            providers[code] = new
            {
                state = state.ToString(),
                stateCode = (int)state,
            };
        }

        return new { providers };
    }

    /// <summary>查询 LLM 响应缓存统计（命中/未命中计数，供 E2E 验收缓存接线）。</summary>
    [HttpGet("{pipelineId:long}/cache-stats")]
    public object GetCacheStats(long pipelineId)
    {
        var (hits, misses) = LlmGatewayService.GetCacheStats();
        return new
        {
            enabled = true,
            hits,
            misses,
            hitRate = (hits + misses) > 0 ? (double)hits / (hits + misses) : 0.0,
            pipelineId,
        };
    }
}
