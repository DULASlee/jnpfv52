using JNPF.Common.Core.MultiTenancy;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

public interface IDeliverableRebuildService
{
    /// <summary>从 IR 快照/事件重建 deliverables/ 文件（Skill 已跑但落盘缺失时）。</summary>
    Task<DeliverableRebuildResult> RebuildAsync(
        long pipelineId,
        string tenantId,
        IReadOnlyList<string>? stages = null,
        CancellationToken ct = default);
}

public sealed class DeliverableRebuildResult
{
    public long PipelineId { get; init; }
    public IReadOnlyList<string> Written { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Skipped { get; init; } = Array.Empty<string>();
}

/// <summary>
/// SUP-01c 补建：从 IR 状态重建 01~06 交付物，无需重跑 Skill。
/// </summary>
public sealed class DeliverableRebuildService : IDeliverableRebuildService, ITransient
{
    private readonly IIrEventStoreService _eventStore;
    private readonly IPipelineDeliverableService _deliverables;
    private readonly IRequirementSpecDocumentService _documents;
    private readonly ISqlSugarClient _db;
    private readonly ILogger<DeliverableRebuildService> _logger;

    public DeliverableRebuildService(
        IIrEventStoreService eventStore,
        IPipelineDeliverableService deliverables,
        IRequirementSpecDocumentService documents,
        ISqlSugarClient db,
        ILogger<DeliverableRebuildService> logger)
    {
        _eventStore = eventStore;
        _deliverables = deliverables;
        _documents = documents;
        _db = db;
        _logger = logger;
    }

    public async Task<DeliverableRebuildResult> RebuildAsync(
        long pipelineId,
        string tenantId,
        IReadOnlyList<string>? stages = null,
        CancellationToken ct = default)
    {
        var projectId = pipelineId.ToString();
        var wantAll = stages == null || stages.Count == 0;
        bool Want(string code) => wantAll || stages!.Contains(code, StringComparer.OrdinalIgnoreCase);

        var written = new List<string>();
        var skipped = new List<string>();

        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, ct);

        if (Want("S1"))
        {
            var skeletonPayload = DesignDeliverableFormatter.FindSnapshotPayload(snapshots, IrFragmentTypes.Skeleton)
                ?? await FindEventPayloadAsync(projectId, tenantId, IrEventTypes.SkeletonCreated, ct);
            if (skeletonPayload != null)
            {
                var json = skeletonPayload is string s ? s : JsonSerializer.Serialize(skeletonPayload);
                var md = _documents.BuildSkeletonMarkdown(json);
                await _deliverables.SaveSkeletonMarkdownAsync(tenantId, pipelineId, md, ct);
                written.Add("01-skeleton.md");
            }
            else skipped.Add("01-skeleton.md(no Skeleton)");
        }

        if (Want("S2"))
        {
            var eventSpecs = snapshots
                .Where(s => string.Equals(s.FragmentType, IrFragmentTypes.EventSpec, StringComparison.Ordinal))
                .ToList();
            if (eventSpecs.Count > 0)
            {
                var requirement = await LoadUserRequirementAsync(pipelineId, ct);
                var pipelineTitle = await _db.Queryable<AiPipelineEntity>()
                    .Where(x => x.Id == pipelineId.ToString())
                    .Select(x => x.Name)
                    .FirstAsync(ct);
                var md = _documents.BuildRequirementSpecMarkdown(pipelineId, requirement, eventSpecs, pipelineTitle);
                await _deliverables.SaveRequirementSpecAsync(tenantId, pipelineId, md, ct);
                written.Add("02-requirement-spec.md");
            }
            else skipped.Add("02-requirement-spec.md(no EventSpec)");
        }

        if (Want("S3"))
        {
            var payload = DesignDeliverableFormatter.FindSnapshotPayload(snapshots, IrFragmentTypes.Architecture)
                ?? await FindEventPayloadAsync(projectId, tenantId, IrEventTypes.ArchitectureDecisionRecorded, ct);
            if (payload != null)
            {
                await _deliverables.SaveArchitectureMarkdownAsync(
                    tenantId, pipelineId, DesignDeliverableFormatter.BuildArchitectureMarkdown(payload), ct);
                written.Add("03-architecture.md");
            }
            else skipped.Add("03-architecture.md(no Architecture)");
        }

        if (Want("S4"))
        {
            var payload = DesignDeliverableFormatter.FindSnapshotPayload(snapshots, IrFragmentTypes.SystemDesign)
                ?? await FindEventPayloadAsync(projectId, tenantId, IrEventTypes.SystemDesignLocked, ct);
            if (payload != null)
            {
                await _deliverables.SaveSystemDesignMarkdownAsync(
                    tenantId, pipelineId, DesignDeliverableFormatter.BuildSystemDesignMarkdown(payload), ct);
                written.Add("04-system-design.md");
            }
            else skipped.Add("04-system-design.md(no SystemDesign)");
        }

        if (Want("S5"))
        {
            var payload = DesignDeliverableFormatter.FindSnapshotPayload(snapshots, IrFragmentTypes.DDL)
                ?? await FindEventPayloadAsync(projectId, tenantId, IrEventTypes.DDLStabilized, ct);
            if (payload != null)
            {
                await _deliverables.SaveDdlSqlAsync(
                    tenantId, pipelineId, DesignDeliverableFormatter.ExtractDdlSql(payload), ct);
                written.Add("05-ddl.sql");
            }
            else skipped.Add("05-ddl.sql(no DDL)");
        }

        if (Want("S6"))
        {
            var payload = DesignDeliverableFormatter.FindSnapshotPayload(snapshots, IrFragmentTypes.FormPageIR)
                ?? await FindEventPayloadAsync(projectId, tenantId, IrEventTypes.UIDesignStabilized, ct);
            if (payload != null)
            {
                await _deliverables.SaveFormPageIrAsync(
                    tenantId, pipelineId, DesignDeliverableFormatter.ExtractFormPageIrJson(payload), ct);
                written.Add("06-formpage-ir.json");
            }
            else skipped.Add("06-formpage-ir.json(no FormPageIR)");
        }

        _logger.LogInformation(
            "交付物重建完成 pipeline={PipelineId} written=[{Written}] skipped=[{Skipped}]",
            pipelineId, string.Join(", ", written), string.Join(", ", skipped));

        return new DeliverableRebuildResult
        {
            PipelineId = pipelineId,
            Written = written,
            Skipped = skipped,
        };
    }

    private async Task<object?> FindEventPayloadAsync(
        string projectId, string tenantId, string eventType, CancellationToken ct)
    {
        var row = await _db.Queryable<AiIrEventEntity>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId && x.EventType == eventType)
            .OrderByDescending(x => x.Sequence)
            .FirstAsync(ct);

        if (row == null || string.IsNullOrWhiteSpace(row.Payload))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(row.Payload);
            return doc.RootElement.Clone();
        }
        catch
        {
            return row.Payload;
        }
    }

    private async Task<string> LoadUserRequirementAsync(long pipelineId, CancellationToken ct)
    {
        var tenantId = TenantResolver.Resolve().ToString();
        var msg = await _db.Queryable<AiPipelineMessageEntity>()
            .Where(x => x.PipelineId == pipelineId.ToString() && x.TenantId == tenantId && x.Role == "user")
            .OrderByDescending(x => x.CreatorTime)
            .FirstAsync(ct);
        return msg?.Content ?? string.Empty;
    }
}
