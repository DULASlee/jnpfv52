using System.Net.Http.Json;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Runtime;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Sa;

/// <summary>
/// PM 骨架中的业务事件（传给 sa-service skeletonBusinessEvents）。
/// </summary>
public sealed record SaSkeletonEventInput(string EventId, string EventName, string ComplexityHint);

public interface ISaOrchestratorAdapter
{
    Task<SaStepResult> RunStepAsync(
        string tenantId,
        string projectId,
        string eventId,
        string irStepName,
        string requirementText,
        string skeletonJson,
        IReadOnlyDictionary<string, object> previousSteps,
        string? runId,
        CancellationToken ct = default);

    /// <summary>
    /// 调 /api/sa/run-async（玛维斯算法 + 事件并行），轮询至完成并返回全量 SA 产出。
    /// projectId = 逻辑项目；pipelineId = 流水线实例（三元组落库）。
    /// </summary>
    Task<SaProjectResult> RunProjectAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        string requirementText,
        IReadOnlyList<SaSkeletonEventInput>? skeletonEvents,
        string? runId,
        CancellationToken ct = default);
}

public sealed class SaStepResult
{
    public string IrStepName { get; init; } = string.Empty;
    public string AgentName { get; init; } = string.Empty;
    public object Output { get; init; } = new { };
    public int DurationMs { get; init; }
}

/// <summary>
/// SA 全量产出（对应 /api/sa/tasks/:taskId 的 result 字段）。
/// </summary>
public sealed class SaProjectResult
{
    /// <summary>
    /// 每个业务事件的分析结果，按 Scope.businessEvents 顺序排列。
    /// </summary>
    public required IReadOnlyList<SaEventResult> EventResults { get; init; }

    public int TotalDurationMs { get; init; }
}

/// <summary>
/// 单个业务事件的分析结果，steps 以 IR 步骤名为 key（匹配 SaStepMapping）。
/// </summary>
public sealed class SaEventResult
{
    public string EventId { get; init; } = string.Empty;
    public string EventName { get; init; } = string.Empty;
    public string Complexity { get; init; } = "simple";
    /// <summary>IR 步骤名 → 产出 JSON 字符串。</summary>
    public IReadOnlyDictionary<string, object> Steps { get; init; } = new Dictionary<string, object>();
    public string? Error { get; init; }
}

/// <summary>
/// sa-service 逐步调用适配器（R2：失败直接抛，禁止 BuildFallbackOutput 假产出）。
/// </summary>
public sealed class SaOrchestratorAdapter : ISaOrchestratorAdapter, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIrEventStoreService _irEventStore;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SaOrchestratorAdapter> _logger;

    public SaOrchestratorAdapter(
        IHttpClientFactory httpClientFactory,
        IIrEventStoreService irEventStore,
        IConfiguration configuration,
        ILogger<SaOrchestratorAdapter> logger)
    {
        _httpClientFactory = httpClientFactory;
        _irEventStore = irEventStore;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SaStepResult> RunStepAsync(
        string tenantId,
        string projectId,
        string eventId,
        string irStepName,
        string requirementText,
        string skeletonJson,
        IReadOnlyDictionary<string, object> previousSteps,
        string? runId,
        CancellationToken ct = default)
    {
        runId ??= SkillExecutionScope.CurrentScope?.RunId;
        var agentName = SaStepMapping.ToAgentName(irStepName);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        object output;
        try
        {
            output = await CallSaServiceAsync(
                tenantId, projectId, eventId, agentName, irStepName,
                requirementText, skeletonJson, previousSteps, runId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "sa-service 调用失败: {Step} event={EventId}", irStepName, eventId);
            throw Oops.Bah($"sa-service 步骤 {irStepName} 失败 (event={eventId}): {ex.Message}");
        }

        sw.Stop();

        var payload = JsonSerializer.Serialize(new
        {
            eventId,
            step = irStepName,
            agent = agentName,
            output,
        }, JsonOptions);

        await _irEventStore.AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.SaStepCompleted,
            FragmentId = $"eventspec:{eventId}",
            FragmentType = IrFragmentTypes.EventSpec,
            Payload = payload,
            SkillId = "analyst-skill",
            SaStepName = irStepName,
        }, ct);

        return new SaStepResult
        {
            IrStepName = irStepName,
            AgentName = agentName,
            Output = output,
            DurationMs = (int)sw.ElapsedMilliseconds,
        };
    }

    private async Task<object> CallSaServiceAsync(
        string tenantId, string projectId, string eventId, string agentName, string irStepName,
        string requirementText, string skeletonJson,
        IReadOnlyDictionary<string, object> previousSteps, string? runId, CancellationToken ct)
    {
        var baseUrl = _configuration["SaService:BaseUrl"] ?? "http://localhost:3001";
        var client = _httpClientFactory.CreateClient("SaService");
        client.Timeout = TimeSpan.FromMinutes(5);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/sa/run-step");
        request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        request.Headers.TryAddWithoutValidation("X-Project-Id", projectId);
        if (!string.IsNullOrEmpty(runId))
            request.Headers.TryAddWithoutValidation("X-Skill-Run-Id", runId);

        request.Content = JsonContent.Create(new
        {
            tenantId,
            projectId,
            eventId,
            agentName,
            irStepName,
            requirementText,
            skeleton = TryParseJson(skeletonJson),
            previousSteps,
        }, options: JsonOptions);

        var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SaRunStepResponse>(JsonOptions, ct)
            ?? throw Oops.Oh("sa-service 返回空响应");
        return body.Output ?? new { };
    }

    // ============================================================
    // RunProjectAsync — 玛维斯算法正确入口
    // 调 /api/sa/run-async（立即返回 taskId），轮询 /api/sa/tasks/:taskId
    // 直至 completed 或 failed（最多 30 分钟）。
    // ============================================================
    public async Task<SaProjectResult> RunProjectAsync(
        string tenantId,
        string projectId,
        long pipelineId,
        string requirementText,
        IReadOnlyList<SaSkeletonEventInput>? skeletonEvents,
        string? runId,
        CancellationToken ct)
    {
        var projectIdNum = long.TryParse(projectId, out var p) ? p : pipelineId;
        var baseUrl = _configuration["SaService:BaseUrl"] ?? "http://localhost:3001";
        var client = _httpClientFactory.CreateClient("SaService");
        client.Timeout = TimeSpan.FromSeconds(30); // 单次 HTTP 请求超时（非整体超时）

        // ── 1. 发起异步 SA 任务 ──
        using var startReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/sa/run-async");
        startReq.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        if (!string.IsNullOrEmpty(runId))
            startReq.Headers.TryAddWithoutValidation("X-Skill-Run-Id", runId);

        startReq.Content = JsonContent.Create(new
        {
            tenantId,
            projectId = projectIdNum,
            pipelineId,
            requirementText,
            skeletonBusinessEvents = skeletonEvents?.Select(e => new
            {
                eventId = e.EventId,
                eventName = e.EventName,
                complexityHint = e.ComplexityHint,
            }).ToList(),
            userId = "analyst-skill",
            runId,
        }, options: JsonOptions);

        var startResp = await client.SendAsync(startReq, ct);
        startResp.EnsureSuccessStatusCode();
        var startBody = await startResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var taskId = startBody.GetProperty("taskId").GetString()
            ?? throw Oops.Oh("sa-service run-async 未返回 taskId");

        _logger.LogInformation("SA 异步任务已启动 taskId={TaskId} project={ProjectId}", taskId, projectId);

        // ── 2. 轮询直到完成（最多 30 分钟，每 15s 一次）──
        var deadline = DateTimeOffset.UtcNow.AddMinutes(30);
        var pollUrl = $"{baseUrl.TrimEnd('/')}/api/sa/tasks/{taskId}";

        while (DateTimeOffset.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(15), ct);

            JsonElement pollBody;
            try
            {
                var pollResp = await client.GetAsync(pollUrl, ct);
                if (!pollResp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("SA 轮询返回 {Status}，继续等待", pollResp.StatusCode);
                    continue;
                }
                pollBody = await pollResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "SA 轮询请求失败，继续等待");
                continue;
            }

            var status = pollBody.TryGetProperty("status", out var s) ? s.GetString() : null;

            if (status == "completed")
            {
                _logger.LogInformation("SA 任务完成 taskId={TaskId}", taskId);
                return ParseSaProjectResult(pollBody);
            }
            if (status == "failed")
            {
                var errMsg = pollBody.TryGetProperty("error", out var e) ? e.GetString() : "SA 流水线失败";
                throw Oops.Bah($"SA 分析失败 (taskId={taskId}): {errMsg}");
            }

            _logger.LogDebug("SA 任务运行中 taskId={TaskId} status={Status}", taskId, status);
        }

        throw Oops.Bah($"SA 分析超时（30分钟）taskId={taskId} project={projectId}");
    }

    private SaProjectResult ParseSaProjectResult(JsonElement pollBody)
    {
        var result = pollBody.TryGetProperty("result", out var r) ? r : pollBody;
        var durationMs = result.TryGetProperty("metadata", out var meta)
            && meta.TryGetProperty("totalDuration", out var dur)
            ? dur.GetInt32() : 0;

        var eventResults = new List<SaEventResult>();

        if (result.TryGetProperty("eventResults", out var eventsEl)
            && eventsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var ev in eventsEl.EnumerateArray())
            {
                var eventId = ev.TryGetProperty("eventId", out var eid) ? eid.ToString() : "";
                var eventName = ev.TryGetProperty("eventName", out var en) ? en.GetString() ?? "" : "";
                var complexity = ev.TryGetProperty("complexity", out var cx) ? cx.GetString() ?? "simple" : "simple";
                var error = ev.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null
                    ? err.GetString() : null;

                var steps = new Dictionary<string, object>(StringComparer.Ordinal);
                if (ev.TryGetProperty("steps", out var stepsEl) && stepsEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in stepsEl.EnumerateObject())
                    {
                        steps[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText(), JsonOptions) ?? new { };
                    }
                }

                eventResults.Add(new SaEventResult
                {
                    EventId = eventId,
                    EventName = eventName,
                    Complexity = complexity,
                    Steps = steps,
                    Error = error,
                });
            }
        }

        return new SaProjectResult
        {
            EventResults = eventResults,
            TotalDurationMs = durationMs,
        };
    }

    private static object? TryParseJson(string json)
    {
        try { return JsonSerializer.Deserialize<object>(json, JsonOptions); }
        catch { return json; }
    }

    private sealed class SaRunStepResponse
    {
        public object? Output { get; set; }
        public int DurationMs { get; set; }
    }
}
