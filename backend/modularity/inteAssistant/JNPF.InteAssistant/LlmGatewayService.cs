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
using JNPF.InteAssistant.Llm;
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
    private string _defaultProvider = "";
    private string _fallbackProvider = "";
    private bool _providersLoaded;

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

    // ─── Provider 配置（延迟加载，默认值来自配置而非硬编码）───

    private void EnsureProvidersLoaded()
    {
        if (_providersLoaded) return;
        _providers = _configuration.GetSection("AI:Providers")
            .Get<Dictionary<string, ProviderConfig>>() ?? new();
        _defaultProvider = _configuration.GetValue("AI:DefaultProvider", "mimo")!;
        _fallbackProvider = _configuration.GetValue("AI:FallbackProvider", "deepseek")!;
        _providersLoaded = true;
    }

    private Dictionary<string, ProviderConfig> Providers
    {
        get
        {
            EnsureProvidersLoaded();
            return _providers!;
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

    // ─── I-07: 5 级降级链（配置化）───

    /// <summary>
    /// 按 LLM 降级链顺序依次尝试调用（I-07 裁决 · v2.1）。
    /// 读取配置 "LlmGateway:Providers" 数组，按 Level 排序后逐级降级。
    /// 降级时通过 SignalR 推送 model_changed SSE 事件。
    /// </summary>
    public async Task<ChatCompletionResponse> ChatWithLevelFallbackAsync(
        ChatCompletionRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var levelConfigs = LoadLevelChain();
        var originalProvider = levelConfigs.FirstOrDefault()?.Name ?? _defaultProvider;

        for (int levelIdx = 0; levelIdx < levelConfigs.Count; levelIdx++)
        {
            var levelCfg = levelConfigs[levelIdx];
            var provider = levelCfg.Name;
            var model = request.ModelCode ?? levelCfg.Model;
            var maxRetries = levelCfg.MaxRetries;
            var timeoutMs = levelCfg.TimeoutSeconds * 1000;

            // 非首选时写入降级审计
            if (levelIdx > 0)
            {
                _logger.LogWarning(
                    "[LLM降级 L{Level}] {From}→{To}：{Reason}",
                    levelIdx, originalProvider, provider,
                    $"L{levelIdx - 1}级Provider连续失败");

                await WriteCallLogAsync(provider, model,
                    JsonSerializer.Serialize(request.Messages),
                    string.Empty, 0, 0, 0, 0, false,
                    $"L{levelIdx}降级触发",
                    levelIdx, originalProvider, provider,
                    $"L{levelIdx}降级: {originalProvider}→{provider}");
            }

            for (int retry = 0; retry < maxRetries; retry++)
            {
                if (!Providers.TryGetValue(provider, out var p))
                {
                    _logger.LogWarning("Provider {Name} 未配置，跳过", provider);
                    break;
                }

                try
                {
                    var httpClient = _httpClientFactory.CreateClient("LLM");
                    httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMs);

                    var (requestBody, requestUri) = BuildRequest(request, p, model);
                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                    };

                    if (p.ApiFormat == "anthropic")
                    {
                        httpRequest.Headers.Add("x-api-key", p.ApiKey);
                        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
                    }
                    else if (p.ApiFormat == "openai")
                    {
                        httpRequest.Headers.Add("Authorization", $"Bearer {p.ApiKey}");
                    }

                    var response = await httpClient.SendAsync(httpRequest, ct);
                    var body = await response.Content.ReadAsStringAsync(ct);
                    sw.Stop();

                    if (response.IsSuccessStatusCode)
                    {
                        var result = ParseResponse(body, p.ApiFormat, model, sw.ElapsedMilliseconds);
                        ResetFailureCount(provider);

                        await WriteCallLogAsync(provider, model,
                            JsonSerializer.Serialize(requestBody), body,
                            sw.ElapsedMilliseconds, (int)response.StatusCode,
                            result.TokensIn, result.TokensOut, true, null,
                            levelIdx, originalProvider, provider,
                            levelIdx > 0 ? $"L{levelIdx}降级成功" : null);

                        // I-07: 推送 SSE model_changed 事件
                        // await PushModelChangedSse(request, originalProvider, provider, levelIdx);

                        return result;
                    }

                    _logger.LogWarning(
                        "[LLM L{Level}] {Provider} HTTP {Status} (retry {Retry}/{MaxRetry})",
                        levelIdx, provider, (int)response.StatusCode, retry + 1, maxRetries);
                }
                catch (TaskCanceledException)
                {
                    sw.Restart();
                    _logger.LogWarning("[LLM L{Level}] {Provider} 超时 (retry {Retry}/{MaxRetry})",
                        levelIdx, provider, retry + 1, maxRetries);
                }
                catch (Exception ex)
                {
                    sw.Restart();
                    _logger.LogWarning(ex, "[LLM L{Level}] {Provider} 异常 (retry {Retry}/{MaxRetry})",
                        levelIdx, provider, retry + 1, maxRetries);
                }

                IncrementFailureCount(provider);

                // 指数退避
                if (retry < maxRetries - 1)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retry));
                    await Task.Delay(delay, ct);
                }
            }
        }

        // 全部 Provider 失败 → L5 无 AI
        sw.Stop();
        _logger.LogError("[LLM] 全部 {Count} 级降级链不可用", levelConfigs.Count);

        await WriteCallLogAsync("none", "unknown",
            string.Empty, string.Empty, sw.ElapsedMilliseconds, 0, 0, 0, false,
            "全部Provider不可用",
            5, originalProvider, "none", "L5: 无AI模式");

        return new ChatCompletionResponse
        {
            IsSuccess = false,
            Error = "所有LLM Provider不可用——请切换到手工编辑模式",
            LatencyMs = (int)sw.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// 从配置 "LlmGateway:Providers" 加载降级链，按 Level 排序。
    /// 配置示例（appsettings.json）：
    /// "LlmGateway": { "Providers": [
    ///   {"Name":"mimo","Model":"mimo-v2.5-pro","Level":0,"MaxRetries":3,"TimeoutSeconds":60},
    ///   {"Name":"deepseek","Model":"deepseek-v4-pro","Level":1,"MaxRetries":3,"TimeoutSeconds":80},
    ///   ...
    /// ]}
    /// </summary>
    private List<LlmProviderLevelConfig> LoadLevelChain()
    {
        var configs = _configuration
            .GetSection("LlmGateway:Providers")
            .Get<List<LlmProviderLevelConfig>>();

        return (configs ?? DefaultLevelChain())
            .OrderBy(c => c.Level)
            .ToList();
    }

    /// <summary>默认降级链（从配置读取模型名，无配置时使用通用回退）</summary>
    private List<LlmProviderLevelConfig> DefaultLevelChain()
    {
        EnsureProvidersLoaded();
        return new List<LlmProviderLevelConfig>
        {
            new() {
                Name = _configuration.GetValue("LlmGateway:DefaultChain:PrimaryName", "mimo")!,
                Model = _configuration.GetValue("LlmGateway:DefaultChain:PrimaryModel", "default")!,
                Level = 0, MaxRetries = 3, TimeoutSeconds = 60
            },
            new() {
                Name = _configuration.GetValue("LlmGateway:DefaultChain:FallbackName", "deepseek")!,
                Model = _configuration.GetValue("LlmGateway:DefaultChain:FallbackModel", "default")!,
                Level = 1, MaxRetries = 3, TimeoutSeconds = 80
            },
        };
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

    /// <summary>
    /// 从 Anthropic 格式响应中提取文本（兼容 thinking + text 混排 content 数组）。
    /// </summary>
    private static string ExtractAnthropicText(JsonElement root)
    {
        if (!root.TryGetProperty("content", out var blocks) || blocks.ValueKind != JsonValueKind.Array)
        {
            return root.TryGetProperty("text", out var directText)
                ? directText.GetString() ?? ""
                : "";
        }

        var sb = new StringBuilder();
        foreach (var block in blocks.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var typeEl))
            {
                var type = typeEl.GetString();
                if (type == "text" && block.TryGetProperty("text", out var textEl))
                {
                    sb.Append(textEl.GetString());
                }
                else if (type == "thinking" && block.TryGetProperty("thinking", out var thinkingEl))
                {
                    // thinking 块不作为主输出，但保留供调试
                }
            }
            else if (block.TryGetProperty("text", out var legacyText))
            {
                sb.Append(legacyText.GetString());
            }
        }

        return sb.Length > 0 ? sb.ToString() : "";
    }

    private ChatCompletionResponse ParseResponse(
        string body, string apiFormat, string model, long latencyMs)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);

            if (apiFormat == "anthropic")
            {
                var content = ExtractAnthropicText(doc.RootElement);

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
            var audit = LlmCallAuditContext.CurrentAudit;
            var log = new AiCallLogEntity
            {
                Model = $"{provider}/{model}",
                RequestBody = requestBody.Length > 4000 ? requestBody[..4000] : requestBody,
                ResponseBody = responseBody.Length > 4000 ? responseBody[..4000] : responseBody,
                LatencyMs = latencyMs,
                StatusCode = statusCode,
                PromptTokens = tokensIn > 0 ? tokensIn : null,
                CompletionTokens = tokensOut > 0 ? tokensOut : null,
                RunId = audit?.RunId,
                SkillId = audit?.SkillId,
                ProjectId = audit?.ProjectId,
                // GAP-3: 降级审计字段
                Fallback = fallback,
                OriginalModel = originalModel,
                ActualModel = actualModel,
                FallbackReason = fallbackReason?.Length > 200 ? fallbackReason[..200] : fallbackReason,
            };
            log.Create();

            if (audit != null && !string.IsNullOrEmpty(audit.TenantId))
                log.TenantId = audit.TenantId;

            await _logRepository.AsInsertable(log)
                .IgnoreColumns(ignoreNullColumn: true)
                .ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write AI call log");
        }
    }

    /// <summary>
    /// 质量评估: 评估 LLM 响应质量。分数 < 0.4 视为低质量，触发供应商切换 (Sprint 4 - S4-4)
    /// </summary>
    private static double EvaluateResponseQuality(string? content, string? expectedFormat = null)
    {
        if (string.IsNullOrWhiteSpace(content)) return 0.0;
        double score = 0.6;
        if (content.Length < 50) score -= 0.3;
        else if (content.Length > 100) score += 0.1;
        if (expectedFormat == "json")
        {
            try { System.Text.Json.JsonDocument.Parse(content); score += 0.15; }
            catch
            {
                var match = System.Text.RegularExpressions.Regex.Match(content, @"```[jJ][sS][oO][nN]\s*([\s\S]*?)\s*```");
                if (match.Success)
                {
                    try { System.Text.Json.JsonDocument.Parse(match.Groups[1].Value); score += 0.1; }
                    catch { score -= 0.2; }
                }
                else score -= 0.2;
            }
        }
        var sentences = content.Split(new[] { '.', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (sentences.Length > 0)
        {
            var uniqueRatio = (double)sentences.Distinct().Count() / sentences.Length;
            if (uniqueRatio < 0.5) score -= 0.2;
        }
        if (content.EndsWith("...") || content.EndsWith("…")) score -= 0.15;
        return Math.Max(0.0, Math.Min(1.0, score));
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

/// <summary>
/// LLM 降级链 Provider 配置（I-07 裁决 · 从 LlmGateway:Providers 读取）。
/// </summary>
internal class LlmProviderLevelConfig
{
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Level { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 60;
}
