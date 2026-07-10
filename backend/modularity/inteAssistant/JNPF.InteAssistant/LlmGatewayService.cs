using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using JNPF.DatabaseAccessor;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.InteAssistant;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Interfaces;
using JNPF.InteAssistant.Llm;
using JNPF.InteAssistant.Skills.Cognitive;
using Microsoft.Extensions.Caching.Memory;
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

    /// <summary>Node-4: 真熔断器（时间窗口 + 半开 + 冷却）</summary>
    private readonly ILlmCircuitBreaker _circuitBreaker;

    /// <summary>
    /// 27 号 §7.1：全局 LLM 调用并发限流器。
    /// 本服务是 ITransient（每次注入新建实例），故 SemaphoreSlim 必须 static，
    /// 才能跨实例共享同一个并发上限（否则每个实例各持一把信号量 = 形同未限流）。
    /// 延迟初始化：首次使用时按 AI:MaxConcurrentLlmCalls（默认 3）创建。
    /// </summary>
    private static SemaphoreSlim? _concurrencyLimiter;
    private readonly int _maxConcurrentLlmCalls;

    /// <summary>27 号 §7.3：按任务路由 Provider（skillId → providerCode）。空则回退默认链。</summary>
    private readonly Dictionary<string, string> _providerRouting;

    /// <summary>
    /// 27 号 §6 响应缓存（IMemoryCache 实现，P1-2 修复 2026-07-10）。
    /// 对确定性请求（相同 provider+model+temperature+prompt）命中缓存直接返回，跳过 Provider 调用。
    /// TTL 由 AI:ResponseCacheTtlMinutes（默认 30）配置；=0 则禁用缓存。
    /// </summary>
    private readonly IMemoryCache? _responseCache;
    private readonly int _responseCacheTtlMinutes;

    /// <summary>缓存统计（跨实例共享，供 E2E 验收）。</summary>
    private static long _cacheHits;
    private static long _cacheMisses;

    /// <summary>获取当前缓存统计快照。</summary>
    public static (long Hits, long Misses) GetCacheStats() => (_cacheHits, _cacheMisses);

    public LlmGatewayService(
        IHttpClientFactory httpClientFactory,
        ISqlSugarRepository<AiCallLogEntity> logRepository,
        IConfiguration configuration,
        ILogger<LlmGatewayService> logger,
        ILlmCircuitBreaker circuitBreaker,
        IMemoryCache? responseCache = null)
    {
        _httpClientFactory = httpClientFactory;
        _logRepository = logRepository;
        _configuration = configuration;
        _logger = logger;
        _circuitBreaker = circuitBreaker;

        // 27 号 §7.1：并发限流上限（默认 3）
        _maxConcurrentLlmCalls = _configuration.GetValue("AI:MaxConcurrentLlmCalls", 3);

        // 27 号 §7.3：按任务路由表（AI:ProviderRouting）。无配置则空字典→回退默认链。
        _providerRouting = _configuration
            .GetSection("AI:ProviderRouting")
            .Get<Dictionary<string, string>>() ?? new();

        // 27 号 §6：响应缓存（默认 30 分钟，=0 禁用）
        _responseCache = responseCache;
        _responseCacheTtlMinutes = _configuration.GetValue("AI:ResponseCacheTtlMinutes", 30);
    }

    /// <summary>
    /// 27 号 §7.1：获取/初始化静态并发限流器（跨实例共享）。
    /// </summary>
    private SemaphoreSlim GetConcurrencyLimiter()
    {
        // 双检锁：避免并发首调时创建多个 SemaphoreSlim
        if (_concurrencyLimiter is null)
        {
            lock (typeof(LlmGatewayService))
            {
                _concurrencyLimiter ??= new SemaphoreSlim(
                    Math.Max(1, _maxConcurrentLlmCalls), Math.Max(1, _maxConcurrentLlmCalls));
            }
        }
        return _concurrencyLimiter;
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

    // ─── 27 号 §7.3：按任务路由 Provider ───

    /// <inheritdoc/>
    /// <remarks>
    /// 读取 AI:ProviderRouting 配置（skillId → providerCode）。无匹配或 Provider 不存在时回退默认 Provider。
    /// 调用方（三轮编排器）据此设 request.ProviderCode，实现按能力路由：
    ///   PM/PSpec 等重度任务 → 推理强的（如 deepseek）
    ///   门控/确认等轻量任务 → 速度快便宜的（如 mimo）
    /// 分摊负载 = 降低单一 Provider 被限流的概率。
    /// </remarks>
    public string ResolveProvider(string skillId)
    {
        EnsureProvidersLoaded();

        if (!string.IsNullOrWhiteSpace(skillId)
            && _providerRouting.TryGetValue(skillId, out var routed)
            && Providers.ContainsKey(routed))
        {
            return routed;
        }

        return _defaultProvider;
    }

    /// <summary>
    /// 27 号 §7.2：按任务类型解析超时（毫秒）。无配置时回退默认 60_000。
    /// 调用方据此设 request.TimeoutMs：
    ///   需求门控评估 30s / PM 骨架 90s / PSpec 60s / 确认 45s / 默认 60s
    /// </summary>
    public int ResolveTimeoutMs(string skillId)
    {
        var configured = _configuration.GetValue($"AI:TimeoutMs:{skillId}", 0);
        return configured > 0 ? configured : _configuration.GetValue("AI:TimeoutMs:Default", 60_000);
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

    /// <summary>
    /// 27 号 §6：计算响应缓存键（P1-2 修复 2026-07-10）。
    /// 键 = SHA256(provider|model|temperature|responseFormat|systemPrompt|messages)。
    /// 返回 null 表示该请求不应缓存（缓存禁用 / 高 Temperature 创意生成 / 消息为空）。
    /// </summary>
    private string? BuildResponseCacheKey(ChatCompletionRequest request)
    {
        // 缓存未启用（无 IMemoryCache 实例 或 TTL=0）
        if (_responseCache == null || _responseCacheTtlMinutes <= 0)
            return null;

        // 高 Temperature（>1.0）= 创意生成，不缓存以保证多样性
        if (request.Temperature > 1.0)
            return null;

        // 空消息不缓存
        if (request.Messages is not { Count: > 0 })
            return null;

        var sb = new StringBuilder();
        sb.Append(request.ProviderCode).Append('|');
        sb.Append(request.ModelCode ?? "").Append('|');
        sb.Append(request.Temperature.ToString("F2")).Append('|');
        sb.Append(request.ResponseFormat ?? "").Append('|');
        sb.Append(request.SystemPrompt ?? "").Append('|');
        foreach (var msg in request.Messages)
            sb.Append(msg.Role).Append(':').Append(msg.Content).Append('\n');

        using var sha = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return "llm:resp:" + Convert.ToHexString(hashBytes);
    }

    /// <summary>尝试从缓存获取响应。</summary>
    private bool TryGetCachedResponse(string cacheKey, out ChatCompletionResponse? response)
    {
        response = null;
        if (_responseCache == null) return false;
        if (_responseCache.TryGetValue(cacheKey, out var raw) && raw is ChatCompletionResponse r)
        {
            response = r;
            return true;
        }
        return false;
    }

    /// <summary>将响应写入缓存（带 TTL）。</summary>
    private void StoreCachedResponse(string cacheKey, ChatCompletionResponse response)
    {
        if (_responseCache == null) return;
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_responseCacheTtlMinutes),
            Size = null, // 不限制条目大小（IMemoryCache 默认不设 SizeLimit）
        };
        _responseCache.Set(cacheKey, response, options);
    }

    /// <inheritdoc/>
    public async Task<ChatCompletionResponse> ChatAsync(
        ChatCompletionRequest request, CancellationToken ct = default)
    {
        // 27 号 §6 响应缓存（P1-2 修复 2026-07-10）。
        // 对确定性请求命中缓存直接返回，跳过 Provider 调用。
        // 高 Temperature（>1.0）请求视为创意生成，不缓存以保证多样性。
        var cacheKey = BuildResponseCacheKey(request);
        if (cacheKey != null && TryGetCachedResponse(cacheKey, out var cached))
        {
            Interlocked.Increment(ref _cacheHits);
            _logger.LogDebug("LLM 响应缓存命中 key={Key}", cacheKey[0..Math.Min(12, cacheKey.Length)]);
            return cached! with { LatencyMs = 0 };
        }
        if (cacheKey != null)
            Interlocked.Increment(ref _cacheMisses);

        var sw = Stopwatch.StartNew();
        var model = request.ModelCode ?? string.Empty;

        // Node-4: 构建 N 级 Provider 降级链
        var chain = BuildProviderChain(request.ProviderCode.Length > 0 ? request.ProviderCode : null);
        var originalProvider = chain.FirstOrDefault().Name ?? _defaultProvider;

        for (int chainIdx = 0; chainIdx < chain.Count; chainIdx++)
        {
            var (providerName, providerModel) = chain[chainIdx];
            if (!Providers.TryGetValue(providerName, out var provider))
                continue;

            var resolvedModel = !string.IsNullOrWhiteSpace(model) ? model
                : !string.IsNullOrWhiteSpace(providerModel) ? providerModel
                : provider.DefaultModel;
            var maxRetries = request.MaxRetries > 0 ? request.MaxRetries : 3;
            var isFallback = chainIdx > 0;

            // Node-4 C2: 熔断器检查
            if (_circuitBreaker.CheckAndTransition(providerName))
            {
                _logger.LogWarning("熔断器开启，跳过 Provider={Provider}", providerName);
                continue;
            }

            // Node-4 C4: Token 预估算预检（仅首级首次）
            if (chainIdx == 0)
            {
                var estimated = LlmTokenEstimator.EstimateRequestTokens(request);
                _logger.LogDebug("Token预估: {Estimated}, MaxTokens={MaxTokens}", estimated, request.MaxTokens);
                if (estimated > 100_000)
                    _logger.LogWarning("请求Token预估值过大({Estimated})，可能超出模型上下文窗口", estimated);

                // 26 号 §12.5：超长 prompt → 截断 messages（而非直接拒绝）。
                // token 上限：可配置 AI:TokenLimit，默认 200_000（覆盖主流模型上下文窗口的最保守值）。
                // 输入侧预算 = tokenLimit - MaxTokens（为输出预留额度）。
                if (_configuration.GetValue("AI:EnforceTokenLimit", false) && estimated > 200_000)
                {
                    var tokenLimit = _configuration.GetValue("AI:TokenLimit", 200_000);
                    var inputBudget = Math.Max(1, tokenLimit - request.MaxTokens);
                    var originalMsgCount = request.Messages?.Count ?? 0;
                    request = LlmTokenEstimator.TruncateForTokenLimit(request, inputBudget);
                    var newEstimated = LlmTokenEstimator.EstimateRequestTokens(request);
                    _logger.LogWarning(
                        "Token 预估超限({Orig})，截断消息历史 {Before}→{After} 条，预估 {Orig}→{New}，MaxTokens={Max}",
                        estimated, originalMsgCount, request.Messages?.Count ?? 0,
                        estimated, newEstimated, request.MaxTokens);
                }
            }

            // 降级审计
            if (isFallback)
            {
                _logger.LogWarning("LLM降级: {From}→{To}（L{Level}）",
                    originalProvider, providerName, chainIdx);
                await WriteCallLogAsync(providerName, resolvedModel,
                    JsonSerializer.Serialize(request.Messages), string.Empty,
                    0, 0, 0, 0, false,
                    $"L{chainIdx}降级触发", chainIdx, originalProvider, providerName,
                    $"L{chainIdx}降级: {originalProvider}→{providerName}");
            }

            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    // 27 号 §7.1：并发限流——跨实例共享的 SemaphoreSlim，限制同时打 Provider 的 HTTP 请求数。
                    // 5 事件并行时最多 _maxConcurrentLlmCalls 个并发，其余排队等待，不打爆 Provider（避免 429 雪崩）。
                    await GetConcurrencyLimiter().WaitAsync(ct);
                    HttpResponseMessage? response; string? body; string? error;
                    try
                    {
                        (response, body, error) = await SendSingleProviderRequestAsync(
                            request, provider, resolvedModel, ct);
                    }
                    finally
                    {
                        _concurrencyLimiter!.Release();
                    }
                    sw.Stop();

                    if (response == null)
                    {
                        _logger.LogWarning(
                            "LLM error: Provider={Provider}, Attempt={Attempt}, Error={Error}",
                            providerName, retry + 1, error);
                    }
                    else if (response.IsSuccessStatusCode)
                    {
                        var (result, jsonWasFixed) = ParseResponse(body!, provider.ApiFormat, resolvedModel, sw.ElapsedMilliseconds);

                        // Node-4 C2: 熔断器记录成功
                        _circuitBreaker.RecordSuccess(providerName);
                        ResetFailureCount(providerName);

                        // Fix-7: JSON 自动修复标记写入审计日志
                        var fallbackReason = jsonWasFixed
                            ? "json_auto_fixed"
                            : (isFallback ? $"L{chainIdx}降级成功" : null);
                        if (jsonWasFixed)
                        {
                            _logger.LogWarning("LLM JSON 自动修复已生效 Provider={Provider} Model={Model}",
                                providerName, resolvedModel);
                        }

                        await WriteCallLogAsync(providerName, resolvedModel,
                            JsonSerializer.Serialize(request.Messages), body!,
                            sw.ElapsedMilliseconds, (int)response.StatusCode,
                            result.TokensIn, result.TokensOut, true, null,
                            chainIdx, originalProvider, providerName,
                            fallbackReason);

                        // 27 号 §6：成功响应写入缓存（仅当缓存启用且 key 非空）
                        if (cacheKey != null && result.IsSuccess)
                            StoreCachedResponse(cacheKey, result);

                        return result;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "LLM HTTP {Status}: Provider={Provider}, Attempt={Attempt}",
                            (int)response.StatusCode, providerName, retry + 1);

                        await WriteCallLogAsync(providerName, resolvedModel,
                            JsonSerializer.Serialize(request.Messages), body ?? string.Empty,
                            sw.ElapsedMilliseconds, (int)response.StatusCode,
                            0, 0, false, $"HTTP {(int)response.StatusCode}",
                            chainIdx, originalProvider, providerName, null);
                    }
                }
                catch (TaskCanceledException)
                {
                    sw.Stop();
                    _logger.LogWarning("LLM timeout: Provider={Provider}, Attempt={Attempt}",
                        providerName, retry + 1);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _logger.LogWarning(ex, "LLM error: Provider={Provider}, Attempt={Attempt}",
                        providerName, retry + 1);
                }

                // Node-4 C2: 熔断器记录失败
                _circuitBreaker.RecordFailure(providerName);
                IncrementFailureCount(providerName);

                // 指数退避（非本级最后一次重试，或非最后一级）
                if (retry < maxRetries - 1 || chainIdx < chain.Count - 1)
                {
                    sw.Restart();
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retry));
                    await Task.Delay(delay, ct);
                }
            }
        }

        // 全部 Provider 不可用
        sw.Stop();
        _logger.LogError("LLM全部Provider不可用: {Chain}",
            string.Join("→", chain.Select(c => c.Name)));

        await WriteCallLogAsync("none", "unknown",
            string.Empty, string.Empty, sw.ElapsedMilliseconds, 0, 0, 0, false,
            "全部Provider不可用", 5, originalProvider, "none", "L5: 无AI模式");

        return new ChatCompletionResponse
        {
            IsSuccess = false,
            Error = "所有LLM Provider不可用——请切换到手工编辑模式",
            LatencyMs = (int)sw.ElapsedMilliseconds
        };
    }

    // ─── CognitiveSkill R0: Tree-of-Thought 多路候选生成（施工包 21 §3.5）───

    /// <inheritdoc/>
    public async Task<TreeSearchResult> TreeSearchAsync(TreeSearchRequest request, CancellationToken ct = default)
    {
        var schedule = TreeSearchPlanner.BuildTemperatureSchedule(
            request.BranchCount, request.BaseTemperature, request.TemperatureStep);

        var branchTasks = schedule.Select((temperature, index) => RunBranchAsync(request, index, temperature, ct));
        var candidates = (await Task.WhenAll(branchTasks)).ToList();

        var anySuccess = candidates.Any(c => c.IsSuccess);
        return new TreeSearchResult
        {
            IsSuccess = anySuccess,
            Candidates = candidates,
            Error = anySuccess
                ? null
                : "TreeSearch 全部分支失败: " + string.Join(" | ",
                    candidates.Select(c => $"[{c.BranchIndex}@{c.Temperature}] {c.Error}")),
        };
    }

    private async Task<TreeSearchCandidate> RunBranchAsync(
        TreeSearchRequest request, int branchIndex, double temperature, CancellationToken ct)
    {
        // 每路走标准 ChatAsync：独立审计入 BASE_AI_CALL_LOG、独立重试/降级
        var response = await ChatAsync(new ChatCompletionRequest
        {
            ProviderCode = request.ProviderCode,
            ModelCode = request.ModelCode,
            SystemPrompt = request.SystemPrompt,
            Messages = request.Messages,
            Temperature = temperature,
            MaxTokens = request.MaxTokens,
            ResponseFormat = request.ResponseFormat,
            MaxRetries = 1,
            TimeoutMs = request.TimeoutMs,
        }, ct);

        return new TreeSearchCandidate
        {
            BranchIndex = branchIndex,
            Temperature = temperature,
            IsSuccess = response.IsSuccess,
            Content = response.Content,
            ModelUsed = response.ModelUsed,
            TokensIn = response.TokensIn,
            TokensOut = response.TokensOut,
            LatencyMs = response.LatencyMs,
            Error = response.Error,
        };
    }

    // ─── I-07: 5 级降级链（配置化）───

    /// <summary>
    /// 按 LLM 降级链顺序依次尝试调用（I-07 裁决 · v2.1）。
    /// 读取配置 "LlmGateway:Providers" 数组，按 Level 排序后逐级降级。
    /// Node-4 重构：委托给 ChatAsync 统一路径，自身退化为薄包装。
    /// </summary>
    public async Task<ChatCompletionResponse> ChatWithLevelFallbackAsync(
        ChatCompletionRequest request, CancellationToken ct = default)
    {
        // Node-4: 委托给 ChatAsync（N 级链 + 熔断器 + JSON 修复 + Token 预估）
        // 保留 per-level maxRetries 语义：取链中最保守的配置
        var levelConfigs = LoadLevelChain();
        var maxRetries = levelConfigs.Count > 0
            ? levelConfigs.Max(c => c.MaxRetries)
            : 3;
        var timeoutMs = levelConfigs.Count > 0
            ? levelConfigs.Min(c => c.TimeoutSeconds) * 1000
            : 120000;

        var adjusted = request with { MaxRetries = maxRetries, TimeoutMs = timeoutMs };
        return await ChatAsync(adjusted, ct);
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
        // Node-4: 构建 N 级 Provider 降级链
        var chain = BuildProviderChain(request.ProviderCode.Length > 0 ? request.ProviderCode : null);
        var originalProvider = chain.FirstOrDefault().Name ?? _defaultProvider;
        var maxRetries = request.MaxRetries > 0 ? request.MaxRetries : 3;

        for (int chainIdx = 0; chainIdx < chain.Count; chainIdx++)
        {
            var (providerName, providerModel) = chain[chainIdx];
            if (!Providers.TryGetValue(providerName, out var provider))
                continue;

            var model = request.ModelCode ?? providerModel ?? provider.DefaultModel;
            var isFallback = chainIdx > 0;

            // Node-4 C2: 熔断器检查
            if (_circuitBreaker.CheckAndTransition(providerName))
            {
                _logger.LogWarning("熔断器开启（Stream），跳过 Provider={Provider}", providerName);
                continue;
            }

            // P2-2（2026-07-10）：Stream 路径补 Token 预检（与 ChatAsync :249-268 对齐）。
            // 仅首级首次预估，超长 prompt 截断 messages，避免流式请求超上下文窗口直达 API。
            if (chainIdx == 0)
            {
                var estimated = LlmTokenEstimator.EstimateRequestTokens(request);
                _logger.LogDebug("Stream Token预估: {Estimated}, MaxTokens={MaxTokens}", estimated, request.MaxTokens);
                if (estimated > 100_000)
                    _logger.LogWarning("Stream 请求Token预估值过大({Estimated})，可能超出模型上下文窗口", estimated);

                if (_configuration.GetValue("AI:EnforceTokenLimit", false) && estimated > 200_000)
                {
                    var tokenLimit = _configuration.GetValue("AI:TokenLimit", 200_000);
                    var inputBudget = Math.Max(1, tokenLimit - request.MaxTokens);
                    var originalMsgCount = request.Messages?.Count ?? 0;
                    request = LlmTokenEstimator.TruncateForTokenLimit(request, inputBudget);
                    var newEstimated = LlmTokenEstimator.EstimateRequestTokens(request);
                    _logger.LogWarning(
                        "Stream Token 预估超限({Orig})，截断消息历史 {Before}→{After} 条，预估 {Orig}→{New}，MaxTokens={Max}",
                        estimated, originalMsgCount, request.Messages?.Count ?? 0,
                        estimated, newEstimated, request.MaxTokens);
                }
            }

            if (isFallback)
            {
                _logger.LogWarning("LLM Stream降级: {From}→{To}",
                    originalProvider, providerName);
            }

            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    // 27 号 §7.1：并发限流（Stream 连接建立阶段）
                    await GetConcurrencyLimiter().WaitAsync(ct);
                    HttpResponseMessage response;
                    object? requestBody = null;
                    try
                    {
                        var httpClient = _httpClientFactory.CreateClient("LLM");
                        httpClient.Timeout = TimeSpan.FromMilliseconds(request.TimeoutMs);

                        var (builtBody, requestUri) = BuildRequest(request, provider, model);
                        requestBody = builtBody;
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

                        response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
                    }
                    finally
                    {
                        _concurrencyLimiter!.Release();
                    }
                    response.EnsureSuccessStatusCode();

                    _circuitBreaker.RecordSuccess(providerName);
                    ResetFailureCount(providerName);

                    await WriteCallLogAsync(providerName, model,
                        JsonSerializer.Serialize(requestBody), "",
                        0, 200, 0, 0, true, null,
                        chainIdx, originalProvider, providerName,
                        isFallback ? $"Stream L{chainIdx}降级成功" : null);

                    return (await response.Content.ReadAsStreamAsync(ct), null);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    _circuitBreaker.RecordFailure(providerName);
                    IncrementFailureCount(providerName);

                    _logger.LogWarning(ex, "LLM stream err: Provider={Provider}, Attempt={Attempt}/{Max}",
                        providerName, retry + 1, maxRetries);

                    await WriteCallLogAsync(providerName, model ?? "unknown",
                        "", "", 0, 0, 0, 0, false, ex.Message,
                        chainIdx, originalProvider, model ?? providerName, null);
                }

                // 指数退避（非本级最后一次重试，或非最后一级）
                if (retry < maxRetries - 1 || chainIdx < chain.Count - 1)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retry));
                    await Task.Delay(delay, ct);
                }
            }
        }

        // 全部 Provider 不可用
        _logger.LogError("LLM Stream全部Provider不可用: {Chain}",
            string.Join("→", chain.Select(c => c.Name)));

        await WriteCallLogAsync("none", "unknown",
            "", "", 0, 0, 0, 0, false,
            "所有Provider均失败", 5, originalProvider, "none", "全部Provider不可用");

        return (null, "[ERROR] 所有LLM Provider不可用");
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

    /// <summary>
    /// 构建有序 Provider 降级链（最多 N 级）。
    /// request.ProviderCode 指定时作为 primary，其余从配置链补充。
    /// </summary>
    private List<(string Name, string? Model)> BuildProviderChain(string? requestedProvider)
    {
        var chain = new List<(string Name, string? Model)>();

        // 1. 显式请求的 provider 作为 primary（若有）
        if (!string.IsNullOrWhiteSpace(requestedProvider) && Providers.ContainsKey(requestedProvider))
        {
            chain.Add((requestedProvider, null)); // model 解析留给调用方
        }

        // 2. 从配置链补充（去重）
        var levelConfigs = LoadLevelChain();
        foreach (var cfg in levelConfigs)
        {
            if (!chain.Any(c => string.Equals(c.Name, cfg.Name, StringComparison.OrdinalIgnoreCase))
                && Providers.ContainsKey(cfg.Name))
            {
                chain.Add((cfg.Name, cfg.Model));
            }

            if (chain.Count >= 3) break; // 最多 3 级
        }

        // 3. 向后兼容：chain 为空时回退到旧 2 级逻辑
        if (chain.Count == 0)
        {
            EnsureProvidersLoaded();
            if (!string.IsNullOrEmpty(_defaultProvider) && Providers.ContainsKey(_defaultProvider))
                chain.Add((_defaultProvider, null));
            if (!string.IsNullOrEmpty(_fallbackProvider)
                && !string.Equals(_fallbackProvider, _defaultProvider, StringComparison.OrdinalIgnoreCase)
                && Providers.ContainsKey(_fallbackProvider))
                chain.Add((_fallbackProvider, null));
        }

        return chain;
    }

    /// <summary>
    /// 向单一 Provider 发送 HTTP 请求（不解析响应），返回原始结果。
    /// 提取自 ChatAsync 和 ChatWithLevelFallbackAsync 的重复代码。
    /// </summary>
    private async Task<(HttpResponseMessage? Response, string? Body, string? Error)>
        SendSingleProviderRequestAsync(
            ChatCompletionRequest request, ProviderConfig provider, string model, CancellationToken ct)
    {
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
            return (response, body, null);
        }
        catch (TaskCanceledException)
        {
            return (null, null, "timeout");
        }
        catch (Exception ex)
        {
            return (null, null, ex.Message);
        }
    }

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

    private (ChatCompletionResponse Response, bool JsonWasFixed) ParseResponse(
        string body, string apiFormat, string model, long latencyMs)
    {
        // 26 号 §12.4：ParseResponse 前置 JSON 修复。
        // FixJsonResponse 对合法 JSON 原样返回（不改动），对 markdown 包裹/前后 prose/尾逗号修复后返回，
        // 修复失败返回 null（保留原文，由下方 try/catch 兜底）。
        var proactivelyFixed = LlmJsonFixer.FixJsonResponse(body);
        var effectiveBody = proactivelyFixed ?? body;
        var wasProactivelyFixed = proactivelyFixed != null && proactivelyFixed != body;

        try
        {
            using var doc = JsonDocument.Parse(effectiveBody);

            if (apiFormat == "anthropic")
            {
                var content = ExtractAnthropicText(doc.RootElement);

                var usage = doc.RootElement.GetProperty("usage");
                var tokensIn = usage.GetProperty("input_tokens").GetInt32();
                var tokensOut = usage.GetProperty("output_tokens").GetInt32();

                return (new ChatCompletionResponse
                {
                    Content = content,
                    ModelUsed = model,
                    TokensIn = tokensIn,
                    TokensOut = tokensOut,
                    LatencyMs = (int)latencyMs,
                    IsSuccess = true
                }, false);
            }
            else
            {
                // OpenAI 兼容格式
                var choices = doc.RootElement.GetProperty("choices");
                var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";

                var usage = doc.RootElement.GetProperty("usage");
                var tokensIn = usage.GetProperty("prompt_tokens").GetInt32();
                var tokensOut = usage.GetProperty("completion_tokens").GetInt32();

                return (new ChatCompletionResponse
                {
                    Content = content,
                    ModelUsed = model,
                    TokensIn = tokensIn,
                    TokensOut = tokensOut,
                    LatencyMs = (int)latencyMs,
                    IsSuccess = true
                }, false);
            }
        }
        catch (JsonException ex)
        {
            // Node-4 C3 + 26 号 §12.4：JSON 自动修复。
            // 前置修复已尝试过（effectiveBody），此处只在"前置修复未生效"时再做一次修复尝试，
            // 避免重复调用。wasProactivelyFixed=true 说明前置修复返回了文本但深层解析仍失败，
            // 此时不再重复修复，直接兜底。
            var (fixedJson, wasFixed) = wasProactivelyFixed
                ? (effectiveBody, true)
                : LlmJsonFixer.TryFix(body);
            if (wasFixed && fixedJson != null && !wasProactivelyFixed)
            {
                _logger.LogWarning(ex,
                    "LLM JSON 响应已自动修复，Model={Model}, 原文长度={OrigLen}",
                    model, body.Length);
                try
                {
                    var (resp, _) = ParseResponse(fixedJson, apiFormat, model, latencyMs);
                    return (resp, true);  // Fix-7: 标记 jsonWasFixed 供调用方审计
                }
                catch
                {
                    // 修复后仍然无法解析，继续兜底（仍标记 jsonWasFixed）
                }
            }

            _logger.LogError(ex, "Failed to parse LLM response: {Body}", body.Length > 500 ? body[..500] : body);
            return (new ChatCompletionResponse
            {
                Content = body,
                ModelUsed = model,
                LatencyMs = (int)latencyMs,
                IsSuccess = true  // 即使解析失败也返回原始内容
            }, wasFixed);  // Fix-7: wasFixed 可能为 true（修复后递归解析仍失败）
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse LLM response: {Body}", body.Length > 500 ? body[..500] : body);
            return (new ChatCompletionResponse
            {
                Content = body,
                ModelUsed = model,
                LatencyMs = (int)latencyMs,
                IsSuccess = true  // 即使解析失败也返回原始内容
            }, false);
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
                // 三元组血缘:PipelineId 从 audit 透传
                PipelineId = audit?.PipelineId ?? "",
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
