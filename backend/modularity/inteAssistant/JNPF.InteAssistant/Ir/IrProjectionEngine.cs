using System.Diagnostics;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Codegen;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using SqlSugar;

namespace JNPF.InteAssistant.Ir;

public interface IIrProjectionEngine
{
    Task<AiIrFragmentSnapshotEntity?> ProjectEventAsync(AiIrEventEntity evt, CancellationToken ct = default);

    Task<IrRebuildResultDto> RebuildAsync(string tenantId, string projectId, CancellationToken ct = default);
}

/// <summary>
/// IR 投影引擎 MVP——按事件类型更新 ai_ir_fragment_snapshots
/// </summary>
public sealed class IrProjectionEngine : IIrProjectionEngine, ITransient
{
    private readonly ISqlSugarClient _db;

    public IrProjectionEngine(ISqlSugarClient db) => _db = db;

    public async Task<IrRebuildResultDto> RebuildAsync(string tenantId, string projectId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        await _db.Deleteable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId)
            .ExecuteCommandAsync(ct);

        var events = await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.TenantId == tenantId && x.ProjectId == projectId)
            .OrderBy(x => x.Sequence)
            .ToListAsync(ct);

        foreach (var evt in events)
            await ProjectEventAsync(evt, ct);

        var fragmentCount = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && !x.DeleteMark)
            .CountAsync(ct);

        sw.Stop();
        return new IrRebuildResultDto
        {
            EventCount = events.Count,
            FragmentCount = fragmentCount,
            ElapsedMs = sw.ElapsedMilliseconds,
            PassedPerformanceGate = events.Count <= 100 ? sw.ElapsedMilliseconds < 200 : null,
        };
    }

    public async Task<AiIrFragmentSnapshotEntity?> ProjectEventAsync(AiIrEventEntity evt, CancellationToken ct = default)
    {
        return evt.EventType switch
        {
            IrEventTypes.SkeletonCreated => await UpsertSkeletonAsync(evt, ct),
            IrEventTypes.SaStepCompleted => await ApplySaStepAsync(evt, ct),
            IrEventTypes.EventSpecRevised => await ReviseSpecAsync(evt, ct),
            IrEventTypes.FragmentInvalidated => await InvalidateFragmentAsync(evt, ct),
            IrEventTypes.EventSpecConfirmed => await UpsertEventSpecAsync(evt, ct),
            IrEventTypes.FragmentStabilized => await StabilizeAsync(evt, ct),
            IrEventTypes.ArchitectureDecisionRecorded => await UpsertIr2FragmentAsync(evt, IrStabilityStates.Stable, ct),
            IrEventTypes.DDLStabilized => await UpsertIr2FragmentAsync(evt, IrStabilityStates.Stable, ct),
            IrEventTypes.UIDesignStabilized => await UpsertIr2FragmentAsync(evt, IrStabilityStates.Stable, ct),
            IrEventTypes.SystemDesignLocked => await UpsertIr2FragmentAsync(evt, IrStabilityStates.Locked, ct),
            IrEventTypes.CodeGenerated => await UpsertIr3GeneratedCodeAsync(evt, IrStabilityStates.Draft, ct),
            IrEventTypes.CodegenFailed => await InvalidateIr3CodegenAsync(evt, ct),
            IrEventTypes.CodegenBuildValidated => await MergeIr3SandboxBuildAsync(evt, ct),
            IrEventTypes.CodeGeneratedStablePromoted => await PromoteIr3CodegenAsync(evt, ct),
            IrEventTypes.TestSuiteGenerated => await UpsertIr3TestSuiteAsync(evt, ct),
            IrEventTypes.ArchViolationDetected => null,
            _ => null,
        };
    }

    private async Task<AiIrFragmentSnapshotEntity?> UpsertIr2FragmentAsync(
        AiIrEventEntity evt, string stabilityState, CancellationToken ct)
    {
        var fragmentId = evt.FragmentId ?? throw new InvalidOperationException($"{evt.EventType} 缺少 fragmentId");
        var existing = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == evt.ProjectId && x.TenantId == evt.TenantId
                && x.FragmentId == fragmentId && !x.DeleteMark)
            .FirstAsync(ct);

        if (existing != null)
        {
            existing.IrContent = evt.Payload;
            existing.CurrentVersion = evt.FragmentVersion;
            existing.FragmentType = evt.FragmentType ?? existing.FragmentType;
            existing.StabilityState = stabilityState;
            existing.LastEventId = evt.Id;
            existing.UpdatedAt = evt.CreatedAt;
            await _db.Updateable(existing).ExecuteCommandAsync(ct);
            return existing;
        }

        var snap = new AiIrFragmentSnapshotEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            ProjectId = evt.ProjectId,
            TenantId = evt.TenantId,
            FragmentId = fragmentId,
            FragmentType = evt.FragmentType ?? IrFragmentTypes.Architecture,
            CurrentVersion = evt.FragmentVersion,
            StabilityState = stabilityState,
            IrContent = evt.Payload,
            SaStepsCompleted = "[]",
            LastEventId = evt.Id,
            UpdatedAt = evt.CreatedAt,
        };
        await _db.Insertable(snap).ExecuteCommandAsync(ct);
        return snap;
    }

    private async Task<AiIrFragmentSnapshotEntity?> UpsertIr3GeneratedCodeAsync(
        AiIrEventEntity evt, string stabilityState, CancellationToken ct)
    {
        var fragmentId = evt.FragmentId ?? throw new InvalidOperationException($"{evt.EventType} 缺少 fragmentId");
        var existing = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == evt.ProjectId && x.TenantId == evt.TenantId
                && x.FragmentId == fragmentId && !x.DeleteMark)
            .FirstAsync(ct);

        if (existing != null)
        {
            existing.IrContent = evt.Payload;
            existing.CurrentVersion = evt.FragmentVersion;
            existing.FragmentType = IrFragmentTypes.GeneratedCode;
            existing.StabilityState = stabilityState;
            existing.LastEventId = evt.Id;
            existing.UpdatedAt = evt.CreatedAt;
            await _db.Updateable(existing).ExecuteCommandAsync(ct);
            return existing;
        }

        var snap = new AiIrFragmentSnapshotEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            ProjectId = evt.ProjectId,
            TenantId = evt.TenantId,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.GeneratedCode,
            CurrentVersion = evt.FragmentVersion,
            StabilityState = stabilityState,
            IrContent = evt.Payload,
            SaStepsCompleted = "[]",
            LastEventId = evt.Id,
            UpdatedAt = evt.CreatedAt,
        };
        await _db.Insertable(snap).ExecuteCommandAsync(ct);
        return snap;
    }

    private async Task<AiIrFragmentSnapshotEntity?> InvalidateIr3CodegenAsync(AiIrEventEntity evt, CancellationToken ct)
    {
        var fragmentId = evt.FragmentId ?? throw new InvalidOperationException($"{evt.EventType} 缺少 fragmentId");
        var snap = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == evt.ProjectId && x.TenantId == evt.TenantId
                && x.FragmentId == fragmentId && !x.DeleteMark)
            .FirstAsync(ct);

        if (snap == null)
            return null;

        snap.IrContent = evt.Payload;
        snap.CurrentVersion = evt.FragmentVersion;
        snap.FragmentType = IrFragmentTypes.GeneratedCode;
        snap.StabilityState = IrStabilityStates.Invalidated;
        snap.LastEventId = evt.Id;
        snap.UpdatedAt = evt.CreatedAt;
        await _db.Updateable(snap).ExecuteCommandAsync(ct);
        return snap;
    }

    private async Task<AiIrFragmentSnapshotEntity?> MergeIr3SandboxBuildAsync(AiIrEventEntity evt, CancellationToken ct)
    {
        var fragmentId = evt.FragmentId ?? throw new InvalidOperationException($"{evt.EventType} 缺少 fragmentId");
        var snap = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == evt.ProjectId && x.TenantId == evt.TenantId
                && x.FragmentId == fragmentId && !x.DeleteMark)
            .FirstAsync(ct);

        if (snap == null)
            return null;

        try
        {
            snap.IrContent = CodegenManifestBuilder.MergeIr3Payload(snap.IrContent ?? "{}", evt.Payload);
        }
        catch
        {
            snap.IrContent = evt.Payload;
        }

        snap.CurrentVersion = evt.FragmentVersion;
        snap.StabilityState = IrStabilityStates.Draft;
        snap.LastEventId = evt.Id;
        snap.UpdatedAt = evt.CreatedAt;
        await _db.Updateable(snap).ExecuteCommandAsync(ct);
        return snap;
    }

    private async Task<AiIrFragmentSnapshotEntity?> PromoteIr3CodegenAsync(AiIrEventEntity evt, CancellationToken ct)
    {
        var fragmentId = evt.FragmentId ?? throw new InvalidOperationException($"{evt.EventType} 缺少 fragmentId");
        var snap = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == evt.ProjectId && x.TenantId == evt.TenantId
                && x.FragmentId == fragmentId && !x.DeleteMark)
            .FirstAsync(ct);

        if (snap == null || snap.StabilityState == IrStabilityStates.Invalidated)
            return null;

        try
        {
            snap.IrContent = CodegenManifestBuilder.MergeIr3Payload(snap.IrContent ?? "{}", evt.Payload);
        }
        catch
        {
            snap.IrContent = evt.Payload;
        }

        snap.CurrentVersion = evt.FragmentVersion;
        snap.StabilityState = IrStabilityStates.Stable;
        snap.LastEventId = evt.Id;
        snap.UpdatedAt = evt.CreatedAt;
        await _db.Updateable(snap).ExecuteCommandAsync(ct);
        return snap;
    }

    private async Task<AiIrFragmentSnapshotEntity?> UpsertIr3TestSuiteAsync(AiIrEventEntity evt, CancellationToken ct)
    {
        var fragmentId = evt.FragmentId ?? throw new InvalidOperationException($"{evt.EventType} 缺少 fragmentId");
        var existing = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == evt.ProjectId && x.TenantId == evt.TenantId
                && x.FragmentId == fragmentId && !x.DeleteMark)
            .FirstAsync(ct);

        if (existing != null)
        {
            existing.IrContent = evt.Payload;
            existing.CurrentVersion = evt.FragmentVersion;
            existing.FragmentType = IrFragmentTypes.TestSuite;
            existing.StabilityState = IrStabilityStates.Stable;
            existing.LastEventId = evt.Id;
            existing.UpdatedAt = evt.CreatedAt;
            await _db.Updateable(existing).ExecuteCommandAsync(ct);
            return existing;
        }

        var snap = new AiIrFragmentSnapshotEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            ProjectId = evt.ProjectId,
            TenantId = evt.TenantId,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.TestSuite,
            CurrentVersion = evt.FragmentVersion,
            StabilityState = IrStabilityStates.Stable,
            IrContent = evt.Payload,
            SaStepsCompleted = "[]",
            LastEventId = evt.Id,
            UpdatedAt = evt.CreatedAt,
        };
        await _db.Insertable(snap).ExecuteCommandAsync(ct);
        return snap;
    }

    private async Task<AiIrFragmentSnapshotEntity?> UpsertSkeletonAsync(AiIrEventEntity evt, CancellationToken ct)
    {
        var fragmentId = evt.FragmentId ?? "skeleton:SK-001";
        var existing = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == evt.ProjectId && x.TenantId == evt.TenantId
                && x.FragmentId == fragmentId && !x.DeleteMark)
            .FirstAsync(ct);

        if (existing != null)
        {
            existing.CurrentVersion = evt.FragmentVersion;
            existing.IrContent = evt.Payload;
            existing.LastEventId = evt.Id;
            existing.UpdatedAt = evt.CreatedAt;
            existing.StabilityState = IrStabilityStates.Draft;
            await _db.Updateable(existing).ExecuteCommandAsync(ct);
            return existing;
        }

        var snap = new AiIrFragmentSnapshotEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            ProjectId = evt.ProjectId,
            TenantId = evt.TenantId,
            FragmentId = fragmentId,
            FragmentType = evt.FragmentType ?? IrFragmentTypes.Skeleton,
            CurrentVersion = evt.FragmentVersion,
            StabilityState = IrStabilityStates.Draft,
            IrContent = evt.Payload,
            SaStepsCompleted = "[]",
            LastEventId = evt.Id,
            UpdatedAt = evt.CreatedAt,
        };
        await _db.Insertable(snap).ExecuteCommandAsync(ct);
        return snap;
    }

    private async Task<AiIrFragmentSnapshotEntity?> ApplySaStepAsync(AiIrEventEntity evt, CancellationToken ct)
    {
        var fragmentId = evt.FragmentId ?? "skeleton:SK-001";
        var snap = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == evt.ProjectId && x.TenantId == evt.TenantId
                && x.FragmentId == fragmentId && !x.DeleteMark)
            .FirstAsync(ct);

        if (snap == null)
        {
            snap = new AiIrFragmentSnapshotEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                ProjectId = evt.ProjectId,
                TenantId = evt.TenantId,
                FragmentId = fragmentId,
                FragmentType = evt.FragmentType ?? IrFragmentTypes.Skeleton,
                CurrentVersion = evt.FragmentVersion,
                StabilityState = IrStabilityStates.InProgress,
                IrContent = evt.Payload,
                SaStepsCompleted = "[]",
                LastEventId = evt.Id,
                UpdatedAt = evt.CreatedAt,
            };
            await _db.Insertable(snap).ExecuteCommandAsync(ct);
        }

        var steps = ParseSteps(snap.SaStepsCompleted);
        var stepName = evt.SaStepName ?? InferNextStep(steps);
        if (!string.IsNullOrEmpty(stepName) && !steps.Contains(stepName))
            steps.Add(stepName);

        snap.SaStepsCompleted = JsonSerializer.Serialize(steps);
        snap.LastEventId = evt.Id;
        snap.UpdatedAt = evt.CreatedAt;
        snap.StabilityState = IrStabilityStates.InProgress;

        await _db.Updateable(snap).ExecuteCommandAsync(ct);
        return snap;
    }

    private async Task<AiIrFragmentSnapshotEntity?> ReviseSpecAsync(AiIrEventEntity evt, CancellationToken ct)
    {
        var fragmentId = evt.FragmentId ?? throw new InvalidOperationException("EventSpecRevised 缺少 fragmentId");
        var snap = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == evt.ProjectId && x.TenantId == evt.TenantId
                && x.FragmentId == fragmentId && !x.DeleteMark)
            .FirstAsync(ct);
        if (snap == null) return null;

        var affected = ParseAffectedSteps(evt.Payload);
        var steps = ParseSteps(snap.SaStepsCompleted);
        if (affected.Count > 0)
            steps = EventSpecRevisionPlanner.TrimCompletedSteps(steps, affected);

        snap.CurrentVersion = evt.FragmentVersion;
        snap.IrContent = evt.Payload;
        snap.SaStepsCompleted = JsonSerializer.Serialize(steps);
        snap.StabilityState = IrStabilityStates.InProgress;
        snap.LastEventId = evt.Id;
        snap.UpdatedAt = evt.CreatedAt;
        await _db.Updateable(snap).ExecuteCommandAsync(ct);
        return snap;
    }

    private async Task<AiIrFragmentSnapshotEntity?> InvalidateFragmentAsync(AiIrEventEntity evt, CancellationToken ct)
    {
        var fragmentId = evt.FragmentId ?? throw new InvalidOperationException("FragmentInvalidated 缺少 fragmentId");
        var snap = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == evt.ProjectId && x.TenantId == evt.TenantId
                && x.FragmentId == fragmentId && !x.DeleteMark)
            .FirstAsync(ct);
        if (snap == null) return null;

        snap.StabilityState = IrStabilityStates.InProgress;
        snap.LastEventId = evt.Id;
        snap.UpdatedAt = evt.CreatedAt;
        await _db.Updateable(snap).ExecuteCommandAsync(ct);
        return snap;
    }

    private static List<string> ParseAffectedSteps(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("affectedSteps", out var stepsEl)
                && stepsEl.ValueKind == JsonValueKind.Array)
            {
                return stepsEl.EnumerateArray()
                    .Select(x => x.GetString())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .ToList();
            }
        }
        catch
        {
            /* ignore */
        }

        return new List<string>();
    }

    private async Task<AiIrFragmentSnapshotEntity?> UpsertEventSpecAsync(AiIrEventEntity evt, CancellationToken ct)
    {
        var fragmentId = evt.FragmentId ?? throw new InvalidOperationException("EventSpecConfirmed 缺少 fragmentId");
        var existing = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == evt.ProjectId && x.TenantId == evt.TenantId
                && x.FragmentId == fragmentId && !x.DeleteMark)
            .FirstAsync(ct);

        if (existing != null)
        {
            existing.IrContent = evt.Payload;
            existing.CurrentVersion = evt.FragmentVersion;
            existing.LastEventId = evt.Id;
            existing.UpdatedAt = evt.CreatedAt;
            existing.StabilityState = IrStabilityStates.Stable;
            await _db.Updateable(existing).ExecuteCommandAsync(ct);
            return existing;
        }

        var snap = new AiIrFragmentSnapshotEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            ProjectId = evt.ProjectId,
            TenantId = evt.TenantId,
            FragmentId = fragmentId,
            FragmentType = IrFragmentTypes.EventSpec,
            CurrentVersion = evt.FragmentVersion,
            StabilityState = IrStabilityStates.Stable,
            IrContent = evt.Payload,
            SaStepsCompleted = JsonSerializer.Serialize(IrSaSteps.All.ToList()),
            LastEventId = evt.Id,
            UpdatedAt = evt.CreatedAt,
        };
        await _db.Insertable(snap).ExecuteCommandAsync(ct);
        return snap;
    }

    private async Task<AiIrFragmentSnapshotEntity?> StabilizeAsync(AiIrEventEntity evt, CancellationToken ct)
    {
        var fragmentId = evt.FragmentId ?? "skeleton:SK-001";
        var snap = await _db.Queryable<AiIrFragmentSnapshotEntity>()
            .Where(x => x.ProjectId == evt.ProjectId && x.TenantId == evt.TenantId
                && x.FragmentId == fragmentId && !x.DeleteMark)
            .FirstAsync(ct);
        if (snap == null) return null;

        snap.StabilityState = IrStabilityStates.Stable;
        snap.LastEventId = evt.Id;
        snap.UpdatedAt = evt.CreatedAt;
        await _db.Updateable(snap).ExecuteCommandAsync(ct);
        return snap;
    }

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

    private static string? InferNextStep(List<string> completed)
    {
        foreach (var step in IrSaSteps.All)
        {
            if (!completed.Contains(step))
                return step;
        }

        return null;
    }
}
