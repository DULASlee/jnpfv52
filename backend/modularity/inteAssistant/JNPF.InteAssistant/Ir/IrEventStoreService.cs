using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Ir;

public interface IIrEventStoreService
{
    Task<AiIrEventEntity> AppendAsync(
        string projectId,
        string tenantId,
        AppendIrEventRequest request,
        CancellationToken ct = default);

    Task<List<IrEventDto>> ListEventsAsync(string projectId, string tenantId, CancellationToken ct = default);

    Task<List<IrFragmentSnapshotDto>> ListSnapshotsAsync(string projectId, string tenantId, CancellationToken ct = default);

    Task<IrStabilityDto?> GetStabilityAsync(string projectId, string tenantId, CancellationToken ct = default);

    Task<IrFragmentSnapshotDto?> GetSnapshotAtVersionAsync(
        string projectId, string tenantId, string fragmentId, int? version, CancellationToken ct = default);

    Task EnsureProjectAsync(string projectId, string tenantId, string projectName, string creatorUserId, CancellationToken ct = default);
}

public sealed class IrEventStoreService : IIrEventStoreService, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ISqlSugarClient _db;
    private readonly IIrProjectionEngine _projection;
    private readonly IPipelineSseChannelHub _sseHub;
    private readonly IIrSchemaValidator _schemaValidator;
    private readonly IStabilityGateService _stabilityGate;
    private readonly IInferredRuleStabilityPolicy _inferredPolicy;
    private readonly IIoiValidatorService _ioiValidator;
    private readonly ILogger<IrEventStoreService> _logger;

    public IrEventStoreService(
        ISqlSugarClient db,
        IIrProjectionEngine projection,
        IPipelineSseChannelHub sseHub,
        IIrSchemaValidator schemaValidator,
        IStabilityGateService stabilityGate,
        IInferredRuleStabilityPolicy inferredPolicy,
        IIoiValidatorService ioiValidator,
        ILogger<IrEventStoreService> logger)
    {
        _db = db;
        _projection = projection;
        _sseHub = sseHub;
        _schemaValidator = schemaValidator;
        _stabilityGate = stabilityGate;
        _inferredPolicy = inferredPolicy;
        _ioiValidator = ioiValidator;
        _logger = logger;
    }

    public async Task EnsureProjectAsync(
        string projectId,
        string tenantId,
        string projectName,
        string creatorUserId,
        CancellationToken ct = default)
    {
        var exists = await _db.Queryable<AiProjectEntity>()
            .AnyAsync(x => x.Id == projectId && !x.DeleteMark, ct);
        if (exists) return;

        await _db.Insertable(new AiProjectEntity
        {
            Id = projectId,
            TenantId = tenantId,
            ProjectName = projectName,
            CreatorUserId = creatorUserId,
            CreatedAt = DateTime.UtcNow,
        }).ExecuteCommandAsync(ct);

        await EnsureRouteAsync(projectId, tenantId, ct);

        await AppendAsync(projectId, tenantId, new AppendIrEventRequest
        {
            EventType = IrEventTypes.ProjectCreated,
            Payload = JsonSerializer.Serialize(new { projectId, projectName }, JsonOptions),
        }, ct);
    }

    public async Task<AiIrEventEntity> AppendAsync(
        string projectId,
        string tenantId,
        AppendIrEventRequest request,
        CancellationToken ct = default)
    {
        if (request.EventType == IrEventTypes.EventSpecConfirmed)
            _ioiValidator.Validate(request.Payload);

        _schemaValidator.Validate(request.EventType, request.Payload);
        return await AppendCoreAsync(projectId, tenantId, request, evaluateStabilityGate: true, ct);
    }

    private async Task<AiIrEventEntity> AppendCoreAsync(
        string projectId,
        string tenantId,
        AppendIrEventRequest request,
        bool evaluateStabilityGate,
        CancellationToken ct)
    {
        var evt = new AiIrEventEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            ProjectId = projectId,
            TenantId = tenantId,
            EventType = request.EventType,
            FragmentId = request.FragmentId,
            FragmentType = request.FragmentType,
            FragmentVersion = request.FragmentVersion,
            Payload = request.Payload,
            SkillId = request.SkillId,
            SaStepName = request.SaStepName,
            CreatedAt = DateTime.UtcNow,
        };

        await _db.Insertable(evt).ExecuteCommandAsync(ct);
        _logger.LogInformation("IR event appended: {EventType} project={ProjectId}", evt.EventType, projectId);

        var snapshot = await _projection.ProjectEventAsync(evt, ct);
        PushSse(projectId, evt, snapshot);

        if (evaluateStabilityGate
            && snapshot != null
            && _stabilityGate.ShouldStabilize(snapshot, evt.EventType)
            && await _inferredPolicy.CanStabilizeAsync(snapshot, projectId, tenantId, ct))
        {
            await AppendCoreAsync(projectId, tenantId, new AppendIrEventRequest
            {
                EventType = IrEventTypes.FragmentStabilized,
                FragmentId = snapshot.FragmentId,
                FragmentType = snapshot.FragmentType,
                FragmentVersion = snapshot.CurrentVersion,
                Payload = JsonSerializer.Serialize(new
                {
                    fragmentId = snapshot.FragmentId,
                    stabilityState = IrStabilityStates.Stable,
                    saStepsCompleted = ParseSteps(snapshot.SaStepsCompleted),
                }, JsonOptions),
                SkillId = "stability-gate",
            }, evaluateStabilityGate: false, ct);
        }

        if (evt.EventType == IrEventTypes.SkeletonCreated && !string.IsNullOrEmpty(evt.FragmentId))
        {
            await _db.Updateable<AiProjectEntity>()
                .SetColumns(x => x.SkeletonId == evt.FragmentId)
                .SetColumns(x => x.LastModifyTime == DateTime.UtcNow)
                .Where(x => x.Id == projectId)
                .ExecuteCommandAsync(ct);
        }

        if (evt.EventType == IrEventTypes.AnalysisCompleted)
        {
            await _db.Updateable<AiProjectEntity>()
                .SetColumns(x => x.AnalysisCompletedAt == DateTime.UtcNow)
                .SetColumns(x => x.CurrentPhase == "analyst-skill")
                .SetColumns(x => x.LastModifyTime == DateTime.UtcNow)
                .Where(x => x.Id == projectId && x.TenantId == tenantId)
                .ExecuteCommandAsync(ct);
        }

        return evt;
    }

    public async Task<List<IrEventDto>> ListEventsAsync(string projectId, string tenantId, CancellationToken ct = default)
    {
        var rows = await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId)
            .OrderByDescending(x => x.Sequence)
            .ToListAsync(ct);

        return rows.Select(ToEventDto).ToList();
    }

    public async Task<List<IrFragmentSnapshotDto>> ListSnapshotsAsync(string projectId, string tenantId, CancellationToken ct = default)
    {
        var rows = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && !x.DeleteMark)
            .OrderBy(x => x.FragmentId)
            .ToListAsync(ct);

        return rows.Select(ToSnapshotDto).ToList();
    }

    public async Task<IrStabilityDto?> GetStabilityAsync(string projectId, string tenantId, CancellationToken ct = default)
    {
        var snap = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && !x.DeleteMark)
            .OrderBy(x => x.FragmentId)
            .FirstAsync(ct);

        if (snap == null) return null;

        var steps = ParseSteps(snap.SaStepsCompleted);
        return new IrStabilityDto
        {
            FragmentId = snap.FragmentId,
            FragmentType = snap.FragmentType,
            StabilityState = snap.StabilityState,
            SaStepsCompleted = steps.ToArray(),
            RequiredSteps = IrSaSteps.All.Length,
            CompletedCount = steps.Count,
            IsStable = snap.StabilityState == IrStabilityStates.Stable,
        };
    }

    public async Task<IrFragmentSnapshotDto?> GetSnapshotAtVersionAsync(
        string projectId,
        string tenantId,
        string fragmentId,
        int? version,
        CancellationToken ct = default)
    {
        if (version == null)
        {
            var current = await _db.Queryable<AiIrFragmentSnapshotEntity>()
                .FirstAsync(x => x.ProjectId == projectId && x.TenantId == tenantId
                    && x.FragmentId == fragmentId && !x.DeleteMark, ct);
            return current == null ? null : ToSnapshotDto(current);
        }

        var evt = await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId
                && x.FragmentId == fragmentId && x.FragmentVersion == version.Value)
            .OrderByDescending(x => x.Sequence)
            .FirstAsync(ct);

        if (evt == null) return null;

        return new IrFragmentSnapshotDto
        {
            FragmentId = fragmentId,
            FragmentType = evt.FragmentType ?? IrFragmentTypes.Skeleton,
            StabilityState = IrStabilityStates.Draft,
            CurrentVersion = evt.FragmentVersion,
            Payload = ParseIrContentPayload(evt.Payload),
            UpdatedAt = evt.CreatedAt,
        };
    }

    private async Task EnsureRouteAsync(string projectId, string tenantId, CancellationToken ct)
    {
        var exists = await _db.Queryable<AiRouteTableEntity>()
            .AnyAsync(x => x.ProjectId == projectId && x.TenantId == tenantId, ct);
        if (exists) return;

        await _db.Insertable(new AiRouteTableEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ProjectId = projectId,
            SandboxId = $"sb-{projectId}",
            SandboxType = "shared",
            SandboxStatus = "creating",
            EtcdKey = $"/ai/{tenantId}/{projectId}/sandbox",
            SandboxEndpoint = StudioWorkspaceHelper.GetPipelinePath(tenantId, projectId),
            CreatedAt = DateTime.UtcNow,
        }).ExecuteCommandAsync(ct);
    }

    private void PushSse(string projectId, AiIrEventEntity evt, AiIrFragmentSnapshotEntity? snapshot)
    {
        if (!long.TryParse(projectId, out var pipelineId))
            return;

        var preview = evt.Payload.Length > 500 ? evt.Payload[..500] + "…" : evt.Payload;
        var irPayload = JsonSerializer.Serialize(new IrEventDto
        {
            EventId = evt.Id,
            EventType = evt.EventType,
            FragmentId = evt.FragmentId,
            FragmentType = evt.FragmentType,
            FragmentVersion = evt.FragmentVersion,
            SkillId = evt.SkillId,
            SaStepName = evt.SaStepName,
            CreatedAt = evt.CreatedAt,
            PayloadPreview = preview,
        }, JsonOptions);

        _sseHub.TryPush(pipelineId, SseEventType.IrEvent, irPayload);

        if (snapshot == null) return;

        var steps = ParseSteps(snapshot.SaStepsCompleted);
        var fragmentPayload = JsonSerializer.Serialize(new
        {
            fragmentId = snapshot.FragmentId,
            fragmentType = snapshot.FragmentType,
            stabilityState = snapshot.StabilityState,
            currentVersion = snapshot.CurrentVersion,
            saStepsCompleted = steps,
        }, JsonOptions);

        _sseHub.TryPush(pipelineId, SseEventType.FragmentUpdated, fragmentPayload);

        if (evt.EventType == IrEventTypes.AnalysisCompleted)
        {
            var analysisPayload = JsonSerializer.Serialize(new
            {
                projectId = evt.ProjectId,
                eventSpecCount = TryParseEventSpecCount(evt.Payload),
                allStable = true,
            }, JsonOptions);
            _sseHub.TryPush(pipelineId, SseEventType.AnalysisCompleted, analysisPayload);
        }
    }

    private static int TryParseEventSpecCount(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("eventSpecCount", out var countEl))
                return countEl.GetInt32();
        }
        catch
        {
            /* ignore */
        }

        return 0;
    }

    private static IrEventDto ToEventDto(AiIrEventEntity evt)
    {
        var preview = evt.Payload.Length > 500 ? evt.Payload[..500] + "…" : evt.Payload;
        return new IrEventDto
        {
            EventId = evt.Id,
            EventType = evt.EventType,
            FragmentId = evt.FragmentId,
            FragmentType = evt.FragmentType,
            FragmentVersion = evt.FragmentVersion,
            SkillId = evt.SkillId,
            SaStepName = evt.SaStepName,
            CreatedAt = evt.CreatedAt,
            PayloadPreview = preview,
        };
    }

    private static IrFragmentSnapshotDto ToSnapshotDto(AiIrFragmentSnapshotEntity snap)
    {
        return new IrFragmentSnapshotDto
        {
            FragmentId = snap.FragmentId,
            FragmentType = snap.FragmentType,
            StabilityState = snap.StabilityState,
            CurrentVersion = snap.CurrentVersion,
            SaStepsCompleted = ParseSteps(snap.SaStepsCompleted).ToArray(),
            Payload = ParseIrContentPayload(snap.IrContent),
            UpdatedAt = snap.UpdatedAt,
        };
    }

    /// <summary>
    /// Newtonsoft 序列化 JsonElement 会退化为 { ValueKind }；转为 Dictionary/List 基元树。
    /// </summary>
    private static object? ParseIrContentPayload(string? irContent)
    {
        if (string.IsNullOrWhiteSpace(irContent)) return null;
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(irContent);
            return ConvertJsonElement(element);
        }
        catch
        {
            return irContent;
        }
    }

    private static object? ConvertJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject()
            .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var n) ? n : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => null,
    };

    private static List<string> ParseSteps(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
