using JNPF.Common.Core.MultiTenancy;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Dto.Ir;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Entitys.Ir;
using JNPF.InteAssistant.Ir;
using JNPF.InteAssistant.Skills;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

public interface ISkillDeliverableCoordinator
{
    Task AfterSkillCompletedAsync(
        string skillId,
        string tenantId,
        long pipelineId,
        string projectId,
        IReadOnlyList<AppendIrEventRequest> events,
        CancellationToken ct = default);
}

/// <summary>
/// Skill 完成后落盘业务交付物（01~09 deliverables/，SUP-01c + SUP-05 + P5-B03）。
/// </summary>
public sealed class SkillDeliverableCoordinator : ISkillDeliverableCoordinator, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IPipelineDeliverableService _deliverables;
    private readonly IRequirementSpecDocumentService _documents;
    private readonly IIrEventStoreService _eventStore;
    private readonly ISqlSugarClient _db;
    private readonly ILogger<SkillDeliverableCoordinator> _logger;

    public SkillDeliverableCoordinator(
        IPipelineDeliverableService deliverables,
        IRequirementSpecDocumentService documents,
        IIrEventStoreService eventStore,
        ISqlSugarClient db,
        ILogger<SkillDeliverableCoordinator> logger)
    {
        _deliverables = deliverables;
        _documents = documents;
        _eventStore = eventStore;
        _db = db;
        _logger = logger;
    }

    public async Task AfterSkillCompletedAsync(
        string skillId,
        string tenantId,
        long pipelineId,
        string projectId,
        IReadOnlyList<AppendIrEventRequest> events,
        CancellationToken ct = default)
    {
        try
        {
            switch (skillId)
            {
                case "pm-skill":
                    await SaveSkeletonAsync(tenantId, pipelineId, events, ct);
                    break;
                case "analyst-skill":
                    // P0：Round 3 Finalize 已用 RequirementDocumentRenderer 写入 02；
                    // AnalysisCompleted.finalized=true 时跳过旧 EventSpec 模板，避免覆盖新渲染器产物。
                    if (HasFinalizedAnalysis(events))
                    {
                        _logger.LogInformation(
                            "跳过旧 02 覆盖（Round3 Finalize 已落盘新渲染器） pipeline={PipelineId}", pipelineId);
                        break;
                    }
                    await SaveRequirementSpecAsync(tenantId, pipelineId, projectId, ct);
                    break;
                case DesignSkillIds.Architect:
                    await SaveArchitectureAsync(tenantId, pipelineId, events, ct);
                    break;
                case DesignSkillIds.DbDesign:
                    await SaveDdlAsync(tenantId, pipelineId, events, ct);
                    break;
                case DesignSkillIds.UiDesign:
                    await SaveFormPageIrAsync(tenantId, pipelineId, events, ct);
                    break;
                case DesignSkillIds.SystemDesign:
                    await SaveSystemDesignAsync(tenantId, pipelineId, events, ct);
                    break;
                case DevelopmentSkillIds.Developer:
                    await SaveCodegenManifestAsync(tenantId, pipelineId, events, ct);
                    break;
                case DevelopmentSkillIds.Tester:
                    await SaveTestSuiteAsync(tenantId, pipelineId, events, ct);
                    break;
                case DeploySkillIds.Deploy:
                    await SaveDeploymentReportAsync(tenantId, pipelineId, events, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Skill 交付物落盘失败 skill={SkillId} pipeline={PipelineId}", skillId, pipelineId);
        }
    }

    /// <summary>检测本轮事件是否含 AnalysisCompleted 且 finalized=true（Round 3 工程保障已写 02）。</summary>
    private static bool HasFinalizedAnalysis(IReadOnlyList<AppendIrEventRequest> events)
    {
        var completed = events.LastOrDefault(e => e.EventType == IrEventTypes.AnalysisCompleted);
        if (completed?.Payload is null) return false;
        try
        {
            var json = completed.Payload is string s
                ? s
                : JsonSerializer.Serialize(completed.Payload, JsonOptions);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("finalized", out var f)
                   && f.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task SaveSkeletonAsync(
        string tenantId, long pipelineId, IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct)
    {
        var skeleton = events.FirstOrDefault(e => e.EventType == IrEventTypes.SkeletonCreated);
        if (skeleton?.Payload == null)
            return;

        var json = skeleton.Payload is string s
            ? s
            : JsonSerializer.Serialize(skeleton.Payload, JsonOptions);

        var markdown = _documents.BuildSkeletonMarkdown(json);
        await _deliverables.SaveSkeletonMarkdownAsync(tenantId, pipelineId, markdown, ct);
        _logger.LogInformation("01-skeleton.md 已落盘 pipeline={PipelineId}", pipelineId);
    }

    private async Task SaveRequirementSpecAsync(
        string tenantId, long pipelineId, string projectId, CancellationToken ct)
    {
        var snapshots = await _eventStore.ListSnapshotsAsync(projectId, tenantId, pipelineId.ToString(), ct);
        var eventSpecs = snapshots
            .Where(s => string.Equals(s.FragmentType, IrFragmentTypes.EventSpec, StringComparison.Ordinal))
            .ToList();

        if (eventSpecs.Count == 0)
        {
            _logger.LogWarning("无 EventSpec 快照，跳过 02-requirement-spec.md pipeline={PipelineId}", pipelineId);
            return;
        }

        var requirement = await LoadUserRequirementAsync(pipelineId, ct);
        var pipelineTitle = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .Select(x => x.Name)
            .FirstAsync(ct);
        var markdown = _documents.BuildRequirementSpecMarkdown(pipelineId, requirement, eventSpecs, pipelineTitle);
        await _deliverables.SaveRequirementSpecAsync(tenantId, pipelineId, markdown, ct);
        _logger.LogInformation(
            "02-requirement-spec.md 已落盘 pipeline={PipelineId} events={Count}",
            pipelineId, eventSpecs.Count);
    }

    private async Task SaveArchitectureAsync(
        string tenantId, long pipelineId, IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct)
    {
        var payload = events.LastOrDefault(e => e.EventType == IrEventTypes.ArchitectureDecisionRecorded)?.Payload;
        if (payload == null) return;

        var md = DesignDeliverableFormatter.BuildArchitectureMarkdown(payload);
        await _deliverables.SaveArchitectureMarkdownAsync(tenantId, pipelineId, md, ct);
        _logger.LogInformation("03-architecture.md 已落盘 pipeline={PipelineId}", pipelineId);
    }

    private async Task SaveDdlAsync(
        string tenantId, long pipelineId, IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct)
    {
        var payload = events.LastOrDefault(e => e.EventType == IrEventTypes.DDLStabilized)?.Payload;
        if (payload == null) return;

        var sql = DesignDeliverableFormatter.ExtractDdlSql(payload);
        await _deliverables.SaveDdlSqlAsync(tenantId, pipelineId, sql, ct);
        _logger.LogInformation("05-ddl.sql 已落盘 pipeline={PipelineId}", pipelineId);
    }

    private async Task SaveFormPageIrAsync(
        string tenantId, long pipelineId, IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct)
    {
        var payload = events.LastOrDefault(e => e.EventType == IrEventTypes.UIDesignStabilized)?.Payload;
        if (payload == null) return;

        var json = DesignDeliverableFormatter.ExtractFormPageIrJson(payload);
        await _deliverables.SaveFormPageIrAsync(tenantId, pipelineId, json, ct);
        _logger.LogInformation("06-formpage-ir.json 已落盘 pipeline={PipelineId}", pipelineId);
    }

    private async Task SaveSystemDesignAsync(
        string tenantId, long pipelineId, IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct)
    {
        var payload = events.LastOrDefault(e => e.EventType == IrEventTypes.SystemDesignLocked)?.Payload;
        if (payload == null) return;

        var md = DesignDeliverableFormatter.BuildSystemDesignMarkdown(payload);
        await _deliverables.SaveSystemDesignMarkdownAsync(tenantId, pipelineId, md, ct);
        _logger.LogInformation("04-system-design.md 已落盘 pipeline={PipelineId}", pipelineId);
    }

    private async Task SaveCodegenManifestAsync(
        string tenantId, long pipelineId, IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct)
    {
        var payload = events.LastOrDefault(e =>
            e.EventType is IrEventTypes.CodeGenerated or IrEventTypes.CodeGeneratedStablePromoted)?.Payload;
        if (payload == null) return;

        var json = payload is string s ? s : JsonSerializer.Serialize(payload, JsonOptions);
        await _deliverables.SaveCodegenManifestAsync(tenantId, pipelineId, json, ct);
        _logger.LogInformation("07-codegen-manifest.json 已落盘 pipeline={PipelineId}", pipelineId);
    }

    private async Task SaveTestSuiteAsync(
        string tenantId, long pipelineId, IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct)
    {
        var payload = events.LastOrDefault(e => e.EventType == IrEventTypes.TestSuiteGenerated)?.Payload;
        if (payload == null) return;

        var json = payload is string s ? s : JsonSerializer.Serialize(payload, JsonOptions);
        await _deliverables.SaveTestSuiteJsonAsync(tenantId, pipelineId, json, ct);
        _logger.LogInformation("08-testsuite.json 已落盘 pipeline={PipelineId}", pipelineId);
    }

    private async Task SaveDeploymentReportAsync(
        string tenantId, long pipelineId, IReadOnlyList<AppendIrEventRequest> events, CancellationToken ct)
    {
        var verified = events.LastOrDefault(e => e.EventType == IrEventTypes.DeploymentVerified)?.Payload;
        if (verified == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# 部署交付报告（09-deployment-report）");
        sb.AppendLine();
        sb.AppendLine($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("## 试用访问");
        sb.AppendLine();
        sb.AppendLine("| 项 | 值 |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| 默认账号 | admin |");
        sb.AppendLine("| 默认密码 | admin123 |");
        sb.AppendLine();

        try
        {
            using var doc = JsonDocument.Parse(verified is string vs ? vs : JsonSerializer.Serialize(verified, JsonOptions));
            var root = doc.RootElement;
            if (root.TryGetProperty("previewUrl", out var preview))
                sb.AppendLine($"**预览链接**：{preview.GetString()}");
            if (root.TryGetProperty("downloadUrl", out var download))
                sb.AppendLine($"**源码包**：{download.GetString()}");
        }
        catch
        {
            sb.AppendLine("（部署 payload 解析失败，见 IR 事件）");
        }

        await _deliverables.SaveDeploymentReportAsync(tenantId, pipelineId, sb.ToString(), ct);
        _logger.LogInformation("09-deployment-report.md 已落盘 pipeline={PipelineId}", pipelineId);
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
