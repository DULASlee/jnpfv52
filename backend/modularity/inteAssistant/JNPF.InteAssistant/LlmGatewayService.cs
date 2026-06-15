using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant;

/// <summary>
/// LLM 网关服务
/// 统一 LLM 调用入口，封装 provider 切换，每次调用写入 BASE_AI_CALL_LOG
/// 对齐前端 src/core/ai/llm/ — 支持 DeepSeek/MiMo/OpenAI/Ollama
/// </summary>
public class LlmGatewayService : ILlmGatewayService, ITransient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISqlSugarRepository<AiCallLogEntity> _logRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LlmGatewayService> _logger;

    private Dictionary<string, ProviderConfig>? _providers;
    private string _defaultProvider = "mimo";
    private string _fallbackProvider = "deepseek";

    /// <summary>GAP-2: 熔断计数器（provider → failureCount）</summary>
    private readonly ConcurrentDictionary<string, int> _failureCounts = new();

    public LlmGatewayService(
        IHttpClientFactory httpClientFactory,
        ISqlSugarRepository<AiCallLogEntity> logRepository,
        IConfiguration configuration,
        ILogger<LlmGatewayService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logRepository = logRepository;
        _configuration = configuration;
        _logger = logger;
    }

    // ─── GAP-2: 熔断计数器方法 ───

    private void IncrementFailureCount(string provider)
    {
        _failureCounts.AddOrUpdate(provider, 1, (_, c) => c + 1);
    }

    private void ResetFailureCount(string provider)
    {
        _failureCounts.TryRemove(provider, out _);
    }

    private int GetFailureCount(string provider)
    {
        return _failureCounts.TryGetValue(provider, out var c) ? c : 0;
    }

    // ─── Provider 配置（延迟加载）───

    private Dictionary<string, ProviderConfig> Providers
    {
        get
        {
            if (_providers == null)
            {
                _providers = _configuration.GetSection("AI:Providers")
                    .Get<Dictionary<string, ProviderConfig>>() ?? new();
                _defaultProvider = _configuration.GetValue("AI:DefaultProvider", "mimo")!;
                _fallbackProvider = _configuration.GetValue("AI:FallbackProvider", "deepseek")!;
            }
            return _providers;
        }
    }

    // ─── Obsolete 兼容方法 ───

    /// <inheritdoc/>
    [Obsolete]
    public async Task<string> ChatAsync(string prompt, string? model = null)
    {
        var request = new ChatCompletionRequest
        {
            Messages = [new ChatMessage("user", prompt)],
            ModelCode = model,
            MaxTokens = 4096,
            MaxRetries = 1,
            TimeoutMs = 120000
        };
        var response = await ChatAsync(request);
        return response.IsSuccess ? response.Content : $"[ERROR] {response.Error}";
    }

    /// <inheritdoc/>
    [Obsolete]
    public async Task<ProviderHealth> HealthCheckAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var healthy = await HealthCheckAsync(_defaultProvider);
            sw.Stop();
            return new ProviderHealth
            {
                IsHealthy = healthy,
                Provider = _defaultProvider,
                LatencyMs = sw.ElapsedMilliseconds,
                Error = healthy ? null : "Health check failed"
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ProviderHealth
            {
                IsHealthy = false,
                Provider = _defaultProvider,
                LatencyMs = sw.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }

    // ─── 新接口（对齐前端 LLMGateway）───

    /// <inheritdoc/>
    public async Task<ChatCompletionResponse> ChatAsync(
        ChatCompletionRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var originalProvider = request.ProviderCode.Length > 0
            ? request.ProviderCode : _defaultProvider;
        var currentProvider = originalProvider;
        var maxRetries = request.MaxRetries > 0 ? request.MaxRetries : 3;
        var retryCount = 0;

        if (!Providers.TryGetValue(currentProvider, out _))
        {
            return new ChatCompletionResponse
            {
                IsSuccess = false,
                Error = $"Provider '{currentProvider}' not configured",
                LatencyMs = (int)sw.ElapsedMilliseconds
            };
        }

        while (true)
        {
            if (!Providers.TryGetValue(currentProvider, out var provider))
            {
                return new ChatCompletionResponse
                {
                    IsSuccess = false,
                    Error = $"Provider '{currentProvider}' not configured",
                    LatencyMs = (int)sw.ElapsedMilliseconds
                };
            }

            var model = request.ModelCode ?? provider.DefaultModel;

            try
            {
                var httpClient = _httpClientFactory.CreateClient("LLM");
                httpClient.Timeout = TimeSpan.FromMilliseconds(request.TimeoutMs);

                var (requestBody, requestUri) = BuildRequest(request, provider, model);
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                };

                if (provider.ApiFormat == "anthropic")
                {
                    httpRequest.Headers.Add("x-api-key", provider.ApiKey);
                    httpRequest.Headers.Add("anthropic-version", "2023-06-01");
                }
                else if (provider.ApiFormat == "openai")
                {
                    httpRequest.Headers.Add("Authorization", $"Bearer {provider.ApiKey}");
                }

                var response = await httpClient.SendAsync(httpRequest, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                sw.Stop();

                if (response.IsSuccessStatusCode)
                {
                    var result = ParseResponse(body, provider.ApiFormat, model, sw.ElapsedMilliseconds);

                    // GAP-2: 成功归零熔断计数器
                    ResetFailureCount(currentProvider);

                    // GAP-3: 写入审计日志（含降级字段）
                    var isFallback = currentProvider != originalProvider;
                    await WriteCallLogAsync(currentProvider, model,
                        JsonSerializer.Serialize(requestBody), body,
                        sw.ElapsedMilliseconds, (int)response.StatusCode,
                        result.TokensIn, result.TokensOut, true, null,
                        isFallback ? 1 : 0, originalProvider, model,
                        isFallback ? $"主模型{originalProvider}连续失败，降级至{currentProvider}" : null);

                    return result;
                }

                _logger.LogWarning(
                    "LLM call attempt {Attempt} to {Provider}: HTTP {Status}",
                    retryCount + 1, currentProvider, (int)response.StatusCode);

                // 记录失败
                await WriteCallLogAsync(currentProvider, model,
                    JsonSerializer.Serialize(requestBody), body,
                    sw.ElapsedMilliseconds, (int)response.StatusCode,
                    0, 0, false, $"HTTP {response.StatusCode}",
                    0, originalProvider, model, null);
            }
            catch (TaskCanceledException)
            {
                sw.Stop();
                _logger.LogWarning("LLM timeout: Provider={Provider}, Attempt={Attempt}",
                    currentProvider, retryCount + 1);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogWarning(ex, "LLM error: Provider={Provider}, Attempt={Attempt}",
                    currentProvider, retryCount + 1);
            }

            retryCount++;
            IncrementFailureCount(currentProvider);

            // GAP-1: 主Provider连续失败 >= 3次 → 切换到备用Provider
            if (retryCount >= maxRetries && currentProvider == originalProvider
                && !string.IsNullOrEmpty(_fallbackProvider)
                && Providers.ContainsKey(_fallbackProvider))
            {
                _logger.LogWarning(
                    "LLM降级: {Original}连续失败{Count}次 → 切换至{Fallback}",
                    originalProvider, retryCount, _fallbackProvider);
                currentProvider = _fallbackProvider;
                retryCount = 0;
                continue;
            }

            // GAP-1: 备用Provider也用完重试次数 → 全部失败
            if (retryCount >= maxRetries)
            {
                _logger.LogError(
                    "LLM全部Provider不可用: {Original}→{Current}, 连续失败{Count}次",
                    originalProvider, currentProvider, retryCount);

                await WriteCallLogAsync(currentProvider, model ?? "unknown",
                    "", "", sw.ElapsedMilliseconds, 0, 0, 0, false,
                    $"所有Provider(主:{originalProvider},备:{currentProvider})均失败{retryCount}次",
                    2, originalProvider, "none", $"全部Provider不可用");

                return new ChatCompletionResponse
                {
                    IsSuccess = false,
                    Error = $"所有LLM Provider不可用: {originalProvider}和{currentProvider}均连续失败",
                    LatencyMs = (int)sw.ElapsedMilliseconds
                };
            }

            // 指数退避
            var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount - 1));
            await Task.Delay(delay, ct);
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> ChatStreamAsync(
        ChatCompletionRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var result = await TryCreateStreamAsync(request, ct);

        if (result.Error != null)
        {
            yield return result.Error;
            yield break;
        }

        // 从 stream 中逐行 yield（不能放在 try-catch 中）
        using var stream = result.Stream!;
        using var reader = new StreamReader(stream!);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var json = line[6..];
                if (json == "[DONE]") yield break;
                yield return json;
            }
        }
    }

    /// <summary>
    /// GAP-4: 带降级重试的流式连接建立
    /// </summary>
    private async Task<(Stream? Stream, string? Error)> TryCreateStreamAsync(
        ChatCompletionRequest request, CancellationToken ct)
    {
        var originalProvider = request.ProviderCode.Length > 0
            ? request.ProviderCode : _defaultProvider;
        var currentProvider = originalProvider;
        var maxRetries = request.MaxRetries > 0 ? request.MaxRetries : 3;
        var retryCount = 0;

        while (true)
        {
            if (!Providers.TryGetValue(currentProvider, out var provider))
                return (null, $"[ERROR] Provider '{currentProvider}' not configured");

            var model = request.ModelCode ?? provider.DefaultModel;

            try
            {
                var httpClient = _httpClientFactory.CreateClient("LLM");
                httpClient.Timeout = TimeSpan.FromMilliseconds(request.TimeoutMs);

                var (requestBody, requestUri) = BuildRequest(request, provider, model);
                if (requestBody is Dictionary<string, object> dict)
                    dict["stream"] = true;

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                };

                if (provider.ApiFormat == "anthropic")
                {
                    httpRequest.Headers.Add("x-api-key", provider.ApiKey);
                    httpRequest.Headers.Add("anthropic-version", "2023-06-01");
                }
                else if (provider.ApiFormat == "openai")
                {
                    httpRequest.Headers.Add("Authorization", $"Bearer {provider.ApiKey}");
                }

                var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                ResetFailureCount(currentProvider);

                var isFallback = currentProvider != originalProvider;
                await WriteCallLogAsync(currentProvider, model,
                    JsonSerializer.Serialize(requestBody), "",
                    0, 200, 0, 0, true, null,
                    isFallback ? 1 : 0, originalProvider, model,
                    isFallback ? $"主模型{originalProvider}连续失败，降级至{currentProvider}" : null);

                return (await response.Content.ReadAsStreamAsync(ct), null);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                retryCount++;
                IncrementFailureCount(currentProvider);

                _logger.LogWarning(ex, "LLM stream err: Provider={Provider}, Attempt={Attempt}",
                    currentProvider, retryCount);

                await WriteCallLogAsync(currentProvider, model ?? "unknown",
                    "", "", 0, 0, 0, 0, false, ex.Message,
                    0, originalProvider, model ?? currentProvider, null);

                if (retryCount >= maxRetries && currentProvider == originalProvider
                    && !string.IsNullOrEmpty(_fallbackProvider)
                    && Providers.ContainsKey(_fallbackProvider))
                {
                    _logger.LogWarning("LLM Stream降级: {Original}→{Fallback}", originalProvider, _fallbackProvider);
                    currentProvider = _fallbackProvider;
                    retryCount = 0;
                    continue;
                }

                if (retryCount >= maxRetries)
                {
                    _logger.LogError("LLM Stream全部Provider不可用");
                    await WriteCallLogAsync(currentProvider, model ?? "unknown",
                        "", "", 0, 0, 0, 0, false,
                        "所有Provider均失败", 2, originalProvider, "none", "全部Provider不可用");
                    return (null, "[ERROR] 所有LLM Provider不可用");
                }

                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount - 1));
                await Task.Delay(delay, ct);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HealthCheckAsync(string providerCode, CancellationToken ct = default)
    {
        if (!Providers.TryGetValue(providerCode, out var provider))
            return false;

        try
        {
            var httpClient = _httpClientFactory.CreateClient("LLM");
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // 发送轻量 ping（用空消息测试连通性）
            var requestBody = new Dictionary<string, object>
            {
                ["model"] = provider.DefaultModel,
                ["max_tokens"] = 1,
                ["messages"] = new[] { new { role = "user", content = "ping" } }
            };

            var requestUri = provider.ApiFormat == "anthropic"
                ? $"{provider.BaseUrl}/v1/messages"
                : $"{provider.BaseUrl}/chat/completions";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            if (provider.ApiFormat == "anthropic")
            {
                httpRequest.Headers.Add("x-api-key", provider.ApiKey);
                httpRequest.Headers.Add("anthropic-version", "2023-06-01");
            }
            else if (provider.ApiFormat == "openai")
            {
                httpRequest.Headers.Add("Authorization", $"Bearer {provider.ApiKey}");
            }

            var response = await httpClient.SendAsync(httpRequest, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for provider {Provider}", providerCode);
            return false;
        }
    }

    /// <inheritdoc/>
    public Task<ProviderInfo> GetProviderInfoAsync(string providerCode)
    {
        if (Providers.TryGetValue(providerCode, out var provider))
        {
            return Task.FromResult(new ProviderInfo
            {
                ProviderCode = providerCode,
                ModelName = provider.DefaultModel
            });
        }
        return Task.FromResult(new ProviderInfo
        {
            ProviderCode = providerCode,
            ModelName = "unknown"
        });
    }

    // ─── Private Helpers ───

    private (object RequestBody, string RequestUri) BuildRequest(
        ChatCompletionRequest request, ProviderConfig provider, string model)
    {
        if (provider.ApiFormat == "anthropic")
        {
            var messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                messages.Add(new { role = "system", content = request.SystemPrompt });
            }
            foreach (var msg in request.Messages)
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }

            var body = new Dictionary<string, object>
            {
                ["model"] = model,
                ["max_tokens"] = request.MaxTokens,
                ["temperature"] = request.Temperature,
                ["messages"] = messages
            };
            return (body, $"{provider.BaseUrl}/v1/messages");
        }
        else
        {
            // OpenAI 兼容格式
            var messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                messages.Add(new { role = "system", content = request.SystemPrompt });
            }
            foreach (var msg in request.Messages)
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }

            var body = new Dictionary<string, object>
            {
                ["model"] = model,
                ["max_tokens"] = request.MaxTokens,
                ["temperature"] = request.Temperature,
                ["messages"] = messages
            };

            if (request.ResponseFormat == "json")
            {
                body["response_format"] = new { type = "json_object" };
            }

            return (body, $"{provider.BaseUrl}/chat/completions");
        }
    }

    private ChatCompletionResponse ParseResponse(
        string body, string apiFormat, string model, long latencyMs)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);

            if (apiFormat == "anthropic")
            {
                var content = doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? "";

                var usage = doc.RootElement.GetProperty("usage");
                var tokensIn = usage.GetProperty("input_tokens").GetInt32();
                var tokensOut = usage.GetProperty("output_tokens").GetInt32();

                return new ChatCompletionResponse
                {
                    Content = content,
                    ModelUsed = model,
                    TokensIn = tokensIn,
                    TokensOut = tokensOut,
                    LatencyMs = (int)latencyMs,
                    IsSuccess = true
                };
            }
            else
            {
                // OpenAI 兼容格式
                var choices = doc.RootElement.GetProperty("choices");
                var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";

                var usage = doc.RootElement.GetProperty("usage");
                var tokensIn = usage.GetProperty("prompt_tokens").GetInt32();
                var tokensOut = usage.GetProperty("completion_tokens").GetInt32();

                return new ChatCompletionResponse
                {
                    Content = content,
                    ModelUsed = model,
                    TokensIn = tokensIn,
                    TokensOut = tokensOut,
                    LatencyMs = (int)latencyMs,
                    IsSuccess = true
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse LLM response: {Body}", body.Length > 500 ? body[..500] : body);
            return new ChatCompletionResponse
            {
                Content = body,
                ModelUsed = model,
                LatencyMs = (int)latencyMs,
                IsSuccess = true  // 即使解析失败也返回原始内容
            };
        }
    }

    private async Task WriteCallLogAsync(
        string provider, string model,
        string requestBody, string responseBody,
        long latencyMs, int statusCode,
        int tokensIn, int tokensOut,
        bool isSuccess, string? error,
        int? fallback = null,
        string? originalModel = null,
        string? actualModel = null,
        string? fallbackReason = null)
    {
        try
        {
            var log = new AiCallLogEntity
            {
                Model = $"{provider}/{model}",
                RequestBody = requestBody.Length > 4000 ? requestBody[..4000] : requestBody,
                ResponseBody = responseBody.Length > 4000 ? responseBody[..4000] : responseBody,
                LatencyMs = latencyMs,
                StatusCode = statusCode,
                // GAP-3: 降级审计字段
                Fallback = fallback,
                OriginalModel = originalModel,
                ActualModel = actualModel,
                FallbackReason = fallbackReason?.Length > 200 ? fallbackReason[..200] : fallbackReason,
            };
            log.Create();

            await _logRepository.AsInsertable(log)
                .IgnoreColumns(ignoreNullColumn: true)
                .ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write AI call log");
        }
    }
}

/// <summary>
/// Provider 配置（从 AI:Providers 读取）
/// </summary>
internal class ProviderConfig
{
    public string BaseUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string DefaultModel { get; set; } = "";
    public string ApiFormat { get; set; } = "anthropic";
}
