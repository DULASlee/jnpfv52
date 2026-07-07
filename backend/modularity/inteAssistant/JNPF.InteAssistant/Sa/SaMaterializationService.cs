using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Runtime;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Sa;

public interface ISaMaterializationService
{
    Task MaterializeAfterConfirmAsync(PipelineTriple triple, CancellationToken ct = default);
}

/// <summary>
/// 用户 confirm-requirement-spec 后：读 SaNineViewCompiled bundle → C# SaMaterializer 物化 → IR 事件。
/// </summary>
public sealed class SaMaterializationService : ISaMaterializationService, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IIrEventStoreService _eventStore;
    private readonly ISaMaterializer _materializer;
    private readonly ISqlSugarClient _db;
    private readonly ILogger<SaMaterializationService> _logger;

    public SaMaterializationService(
        IIrEventStoreService eventStore,
        ISaMaterializer materializer,
        ISqlSugarClient db,
        ILogger<SaMaterializationService> logger)
    {
        _eventStore = eventStore;
        _materializer = materializer;
        _db = db;
        _logger = logger;
    }

    public async Task MaterializeAfterConfirmAsync(PipelineTriple triple, CancellationToken ct = default)
    {
        var bundle = await LoadCompiledBundleAsync(triple, ct);
        if (bundle == null)
        {
            await AppendFailedAsync(triple, "未找到 SaNineViewCompiled 事件，无法物化", ct);
            return;
        }

        try
        {
            var result = await _materializer.MaterializeAsync(triple, bundle, ct);
            await _eventStore.AppendAsync(triple.ProjectId, triple.TenantId, new AppendIrEventRequest
            {
                EventType = IrEventTypes.SaMaterializationCompleted,
                Payload = JsonSerializer.Serialize(new
                {
                    tenantId = triple.TenantId,
                    projectId = triple.ProjectId,
                    pipelineId = triple.PipelineId,
                    bundleHash = bundle.BundleHash,
                    scopeId = result.ScopeId,
                    dictId = result.DictId,
                    eventCount = result.EventCount,
                    durationMs = result.DurationMs,
                }, JsonOptions),
                SkillId = "analyst-skill",
            }, ct);

            _logger.LogInformation(
                "SA 物化完成 pipeline={PipelineId} scope={ScopeId} events={Events}",
                triple.PipelineId, result.ScopeId, result.EventCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SA 物化失败 pipeline={PipelineId}", triple.PipelineId);
            await AppendFailedAsync(triple, ex.Message, ct);
            throw;
        }
    }

    private async Task<SaNineViewCompileResult?> LoadCompiledBundleAsync(PipelineTriple triple, CancellationToken ct)
    {
        var compiled = await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.ProjectId == triple.ProjectId
                && x.TenantId == triple.TenantId
                && x.PipelineId == triple.PipelineId.ToString()
                && x.EventType == IrEventTypes.SaNineViewCompiled)
            .OrderByDescending(x => x.Sequence)
            .FirstAsync(ct);

        if (compiled?.Payload == null)
        {
            // ADR-005 三元组隔离：移除原"去 PipelineId 重查"fallback（会串会话 bundle）。
            // PipelineId 缺失说明事件写入不完整或迁移未跑，应排查而非回退到跨会话查询。
            _logger?.LogWarning(
                "未找到 pipeline 维度的 SaNineViewCompiled 事件，不再回退到 project 级查询（避免串会话）。projectId={ProjectId} pipelineId={PipelineId}",
                triple.ProjectId, triple.PipelineId);
            return null;
        }

        if (compiled?.Payload == null)
            return null;

        try
        {
            var json = compiled.Payload;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var bundleEl = root.TryGetProperty("bundle", out var b) ? b : root;

            if (!bundleEl.TryGetProperty("projectSteps", out var stepsEl)
                || !bundleEl.TryGetProperty("eventResults", out var eventsEl))
                return null;

            var projectSteps = JsonSerializer.Deserialize<Dictionary<string, object>>(
                stepsEl.GetRawText(), JsonOptions) ?? new Dictionary<string, object>();

            var eventResults = JsonSerializer.Deserialize<List<SaEventResult>>(
                eventsEl.GetRawText(), JsonOptions) ?? new List<SaEventResult>();

            var hash = bundleEl.TryGetProperty("bundleHash", out var h) ? h.GetString() ?? ""
                : root.TryGetProperty("bundleHash", out var h2) ? h2.GetString() ?? "" : "";

            var ms = bundleEl.TryGetProperty("compileDurationMs", out var m) ? m.GetInt32() : 0;

            return new SaNineViewCompileResult
            {
                Source = new PreAnalysisModel(),
                ProjectSteps = projectSteps,
                EventResults = eventResults,
                CompileDurationMs = ms,
                BundleHash = hash,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析 SaNineViewCompiled payload 失败");
        }

        return null;
    }

    private Task AppendFailedAsync(PipelineTriple triple, string message, CancellationToken ct)
        => _eventStore.AppendAsync(triple.ProjectId, triple.TenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.SaMaterializationFailed,
            Payload = JsonSerializer.Serialize(new
            {
                tenantId = triple.TenantId,
                projectId = triple.ProjectId,
                pipelineId = triple.PipelineId,
                error = message,
            }, JsonOptions),
            SkillId = "analyst-skill",
        }, ct);
}
