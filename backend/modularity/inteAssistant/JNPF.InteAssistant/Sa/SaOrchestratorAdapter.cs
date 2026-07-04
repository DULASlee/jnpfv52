using System.Net.Http.Json;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Sa;

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
}

public sealed class SaStepResult
{
    public string IrStepName { get; init; } = string.Empty;
    public string AgentName { get; init; } = string.Empty;
    public object Output { get; init; } = new { };
    public bool UsedFallback { get; init; }
    public int DurationMs { get; init; }
}

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
        var usedFallback = false;

        try
        {
            output = await CallSaServiceAsync(
                tenantId, projectId, eventId, agentName, irStepName,
                requirementText, skeletonJson, previousSteps, runId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "sa-service 调用失败，使用本地回退: {Step} event={EventId}", irStepName, eventId);
            output = BuildFallbackOutput(irStepName, eventId);
            usedFallback = true;
        }

        sw.Stop();

        var payload = JsonSerializer.Serialize(new
        {
            eventId,
            step = irStepName,
            agent = agentName,
            output,
            usedFallback,
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
            UsedFallback = usedFallback,
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
            ?? throw new InvalidOperationException("sa-service 返回空响应");
        return body.Output ?? new { };
    }

    private static object? TryParseJson(string json)
    {
        try { return JsonSerializer.Deserialize<object>(json, JsonOptions); }
        catch { return json; }
    }

    private static object BuildFallbackOutput(string irStepName, string eventId) => irStepName switch
    {
        "DomainModel" => new { eventId, domain = "general", entities = new[] { eventId } },
        "AggregateDesign" => new { eventId, aggregates = new[] { new { name = eventId, root = true } } },
        "EventCatalog" => new { eventId, events = new[] { new { name = eventId, trigger = "user" } } },
        "CommandQuery" => new { eventId, commands = Array.Empty<object>(), queries = Array.Empty<object>() },
        "IntegrationPoints" => new { eventId, integrations = Array.Empty<object>() },
        "WorkflowSpec" => new { eventId, states = new[] { "Draft", "Approved" }, transitions = Array.Empty<object>() },
        "UISpec" => new { eventId, screens = new[] { new { name = $"{eventId}Form", fields = Array.Empty<object>() } } },
        "DataModel" => new { eventId, tables = new[] { new { name = eventId, columns = Array.Empty<object>() } } },
        "DeliveryChecklist" => new { eventId, checklist = new[] { "spec-complete", "ioi-passed" } },
        _ => new { eventId, step = irStepName, stub = true },
    };

    private sealed class SaRunStepResponse
    {
        public object? Output { get; set; }
        public int DurationMs { get; set; }
    }
}
