using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Infrastructure.Telemetry;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Infrastructure.Messaging;
using JNPF.InteAssistant.Runtime;
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

    Task<List<IrEventDto>> ListEventsAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default);

    Task<List<IrFragmentSnapshotDto>> ListSnapshotsAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default);

    Task<IrStabilityDto?> GetStabilityAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default);

    Task<IrFragmentSnapshotDto?> GetSnapshotAtVersionAsync(
        string projectId, string tenantId, string pipelineId, string fragmentId, int? version, CancellationToken ct = default);

    /// <summary>读取最新一条事件的完整 Payload（续跑判据/答案回放，避免 PayloadPreview 截断）。</summary>
    Task<string?> GetLatestEventPayloadAsync(
        string projectId, string tenantId, string pipelineId, string eventType, CancellationToken ct = default);

    /// <summary>按 Sequence 升序读取某事件类型的全部完整 Payload（多轮澄清合并）。</summary>
    Task<List<string>> ListFullEventPayloadsAsync(
        string projectId, string tenantId, string pipelineId, string eventType, CancellationToken ct = default);

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
        // P6-O01 ir.append OTel Span
        using var activity = StudioActivitySource.StartIrAppend(request.EventType, request.FragmentId);
        var evt = new AiIrEventEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            ProjectId = projectId,
            TenantId = tenantId,
            // 三元组血缘:PipelineId 从 SkillExecutionScope 透传（历史兜底 projectId）
            PipelineId = SkillExecutionScope.CurrentScope?.PipelineId.ToString() ?? projectId,
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

    public async Task<List<IrEventDto>> ListEventsAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
    {
        var rows = await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && x.PipelineId == pipelineId)
            .OrderByDescending(x => x.Sequence)
            .ToListAsync(ct);

        return rows.Select(ToEventDto).ToList();
    }

    public async Task<List<IrFragmentSnapshotDto>> ListSnapshotsAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
    {
        var rows = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && x.PipelineId == pipelineId && !x.DeleteMark)
            .OrderBy(x => x.FragmentId)
            .ToListAsync(ct);

        return rows.Select(ToSnapshotDto).ToList();
    }

    public async Task<IrStabilityDto?> GetStabilityAsync(string projectId, string tenantId, string pipelineId, CancellationToken ct = default)
    {
        var snap = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && x.PipelineId == pipelineId && !x.DeleteMark)
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
        string pipelineId,
        string fragmentId,
        int? version,
        CancellationToken ct = default)
    {
        if (version == null)
        {
            var current = await _db.Queryable<AiIrFragmentSnapshotEntity>()
                .FirstAsync(x => x.ProjectId == projectId && x.TenantId == tenantId && x.PipelineId == pipelineId
                    && x.FragmentId == fragmentId && !x.DeleteMark, ct);
            return current == null ? null : ToSnapshotDto(current);
        }

        var evt = await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && x.PipelineId == pipelineId
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

    public async Task<string?> GetLatestEventPayloadAsync(
        string projectId, string tenantId, string pipelineId, string eventType, CancellationToken ct = default)
    {
        return await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && x.PipelineId == pipelineId
                && x.EventType == eventType)
            .OrderByDescending(x => x.Sequence)
            .Select(x => x.Payload)
            .FirstAsync(ct);
    }

    public async Task<List<string>> ListFullEventPayloadsAsync(
        string projectId, string tenantId, string pipelineId, string eventType, CancellationToken ct = default)
    {
        return await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && x.PipelineId == pipelineId
                && x.EventType == eventType)
            .OrderBy(x => x.Sequence)
            .Select(x => x.Payload)
            .ToListAsync(ct);
    }

    private async Task EnsureRouteAsync(string projectId, string tenantId, CancellationToken ct)
    {
        var existing = await _db.Queryable<AiRouteTableEntity>()
            .FirstAsync(x => x.ProjectId == projectId && x.TenantId == tenantId, ct);

        if (existing != null)
        {
            // P6-R01 心跳更新（替代 etcd，务实版：每次 IR 事件追加时刷新 LastHeartbeat）
            await _db.Updateable<AiRouteTableEntity>()
                .SetColumns(x => x.LastHeartbeat == DateTime.UtcNow)
                .Where(x => x.Id == existing.Id)
                .ExecuteCommandAsync(ct);
            return;
        }

        await _db.Insertable(new AiRouteTableEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ProjectId = projectId,
            SandboxId = $"sb-{projectId}",
            SandboxType = "shared",
            SandboxStatus = "creating",
            EtcdKey = $"/ai/{tenantId}/{projectId}/sandbox",
            // greenfield 自锚定：project 创建时 pipelineId ≡ projectId
            SandboxEndpoint = StudioWorkspaceHelper.GetPipelinePath(tenantId, projectId, projectId),
            CreatedAt = DateTime.UtcNow,
            LastHeartbeat = DateTime.UtcNow,
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

        // Bug 2 fix: AnalystSkillService.FinalizeAsync 在 enableFinalization=false（Round 1/2）
        // 时仍写 AnalysisCompleted IR 事件（finalized=false）。此处若无条件推 SSE，
        // 前端收到即认为需求分析完成 → 弹出需求说明书推进架构设计 → 跳过三轮澄清。
        // 修复：仅当 finalized=true（Round 3 工程保障完成）时才推送 AnalysisCompleted SSE。
        if (evt.EventType == IrEventTypes.AnalysisCompleted)
        {
            if (TryParseFinalized(evt.Payload))
            {
                var analysisPayload = JsonSerializer.Serialize(new
                {
                    projectId = evt.ProjectId,
                    eventSpecCount = TryParseEventSpecCount(evt.Payload),
                    allStable = true,
                }, JsonOptions);
                _sseHub.TryPush(pipelineId, SseEventType.AnalysisCompleted, analysisPayload);
            }
            else
            {
                _logger.LogInformation(
                    "AnalysisCompleted 事件写入但 finalized=false，抑制 SSE 推送（Round 1/2 不应触发前端完成进度）pipeline={PipelineId}",
                    pipelineId);
            }
        }
    }

    /// <summary>
    /// 解析 AnalysisCompleted Payload 中的 finalized 标志。
    /// 若 Payload 无法解析则默认返回 true（向后兼容历史事件）。
    /// </summary>
    private static bool TryParseFinalized(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("finalized", out var finalizedEl))
                return finalizedEl.GetBoolean();
        }
        catch
        {
            /* ignore — 向后兼容：无法解析的历史事件默认为已 finalize */
        }

        return true;
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
