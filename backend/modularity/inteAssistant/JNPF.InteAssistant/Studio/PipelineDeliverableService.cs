using System.Text;
using System.Text.Json;
using JNPF.Common.Core.MultiTenancy;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Infrastructure.Attachments;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Studio;

public interface IPipelineDeliverableService
{
    Task<string> SaveAttachmentExtractAsync(
        string tenantId,
        long pipelineId,
        string attachmentId,
        string fileName,
        string extractedText,
        CancellationToken ct = default);

    Task SaveGateDeliverablesAsync(
        string tenantId,
        long pipelineId,
        GateResult gateResult,
        IReadOnlyList<AttachmentItemSummary> attachments,
        CancellationToken ct = default);

    Task SaveSkeletonMarkdownAsync(
        string tenantId,
        long pipelineId,
        string markdown,
        CancellationToken ct = default);

    Task SaveRequirementSpecAsync(
        string tenantId,
        long pipelineId,
        string markdown,
        CancellationToken ct = default);

    Task SaveArchitectureMarkdownAsync(
        string tenantId,
        long pipelineId,
        string markdown,
        CancellationToken ct = default);

    Task SaveSystemDesignMarkdownAsync(
        string tenantId,
        long pipelineId,
        string markdown,
        CancellationToken ct = default);

    Task SaveDdlSqlAsync(
        string tenantId,
        long pipelineId,
        string sql,
        CancellationToken ct = default);

    Task SaveFormPageIrAsync(
        string tenantId,
        long pipelineId,
        string json,
        CancellationToken ct = default);

    Task SaveCodegenManifestAsync(
        string tenantId,
        long pipelineId,
        string json,
        CancellationToken ct = default);

    Task SaveTestSuiteJsonAsync(
        string tenantId,
        long pipelineId,
        string json,
        CancellationToken ct = default);

    Task SaveDeploymentReportAsync(
        string tenantId,
        long pipelineId,
        string markdown,
        CancellationToken ct = default);

    Task<IReadOnlyList<DeliverableListItem>> ListByPipelineAsync(
        long pipelineId,
        string? stageCode,
        CancellationToken ct = default);

    Task<(string absolutePath, string contentType, string fileName)> ResolveDeliverableAsync(
        long pipelineId,
        string relativePath,
        CancellationToken ct = default);
}

public sealed record DeliverableListItem
{
    public string Id { get; init; } = "";
    public string StageCode { get; init; } = "";
    public string FileName { get; init; } = "";
    public string RelativePath { get; init; } = "";
    public string ContentType { get; init; } = "";
    public long FileSize { get; init; }
    public DateTime CreateTime { get; init; }
    public string DownloadUrl { get; init; } = "";
}

/// <summary>
/// 流水线 deliverables/ 落盘与 DB 索引（S0 门控报告、附件解析文本等）
/// </summary>
public sealed class PipelineDeliverableService : IPipelineDeliverableService, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly ISqlSugarClient _db;
    private readonly ILogger<PipelineDeliverableService> _logger;

    public PipelineDeliverableService(ISqlSugarClient db, ILogger<PipelineDeliverableService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string> SaveAttachmentExtractAsync(
        string tenantId,
        long pipelineId,
        string attachmentId,
        string fileName,
        string extractedText,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
            return "";

        var safeName = SanitizeFileName(Path.GetFileNameWithoutExtension(fileName));
        var relativePath = $"attachments/{attachmentId}-{safeName}.txt";
        var absolutePath = await WriteTextAsync(tenantId, pipelineId, relativePath, extractedText, ct);

        await UpsertDeliverableIndexAsync(
            tenantId,
            pipelineId,
            "S0",
            $"{safeName}-extracted.txt",
            relativePath,
            "text/plain; charset=utf-8",
            absolutePath,
            ct);

        return relativePath;
    }

    public async Task SaveGateDeliverablesAsync(
        string tenantId,
        long pipelineId,
        GateResult gateResult,
        IReadOnlyList<AttachmentItemSummary> attachments,
        CancellationToken ct = default)
    {
        var report = new
        {
            pipelineId,
            passed = gateResult.Passed,
            generatedAt = DateTime.Now,
            mergedTextLength = gateResult.MergedText?.Length ?? 0,
            attachmentTextLength = gateResult.AttachmentText?.Length ?? 0,
            attachmentCount = gateResult.AttachmentCount,
            blockedCount = gateResult.BlockedCount,
            warnings = gateResult.Warnings,
            semanticFitness = gateResult.SemanticFitness,
            attachments = attachments.Select(a => new
            {
                a.Id,
                a.FileName,
                a.ProcessStatus,
                a.ExtractedLength,
                a.Error,
            }),
        };

        var reportJson = JsonSerializer.Serialize(report, JsonOptions);
        var reportPath = await WriteTextAsync(tenantId, pipelineId, "00-gate-report.json", reportJson, ct);
        await UpsertDeliverableIndexAsync(
            tenantId, pipelineId, "S0", "00-gate-report.json", "00-gate-report.json",
            "application/json", reportPath, ct);

        if (!string.IsNullOrWhiteSpace(gateResult.MergedText))
        {
            var mergedPath = await WriteTextAsync(
                tenantId, pipelineId, "00-merged-requirement.md", gateResult.MergedText, ct);
            await UpsertDeliverableIndexAsync(
                tenantId, pipelineId, "S0", "00-merged-requirement.md", "00-merged-requirement.md",
                "text/markdown; charset=utf-8", mergedPath, ct);
        }

        _logger.LogInformation(
            "S0 门控交付物已落盘: PipelineId={Id}, Report={Report}",
            pipelineId, reportPath);
    }

    public async Task SaveSkeletonMarkdownAsync(
        string tenantId,
        long pipelineId,
        string markdown,
        CancellationToken ct = default)
    {
        var path = await WriteTextAsync(tenantId, pipelineId, "01-skeleton.md", markdown, ct);
        await UpsertDeliverableIndexAsync(
            tenantId, pipelineId, "S1", "01-skeleton.md", "01-skeleton.md",
            "text/markdown; charset=utf-8", path, ct);
    }

    public async Task SaveRequirementSpecAsync(
        string tenantId,
        long pipelineId,
        string markdown,
        CancellationToken ct = default)
    {
        var path = await WriteTextAsync(tenantId, pipelineId, "02-requirement-spec.md", markdown, ct);
        await UpsertDeliverableIndexAsync(
            tenantId, pipelineId, "S2", "02-requirement-spec.md", "02-requirement-spec.md",
            "text/markdown; charset=utf-8", path, ct);
    }

    public async Task SaveArchitectureMarkdownAsync(
        string tenantId, long pipelineId, string markdown, CancellationToken ct = default)
    {
        var path = await WriteTextAsync(tenantId, pipelineId, "03-architecture.md", markdown, ct);
        await UpsertDeliverableIndexAsync(
            tenantId, pipelineId, "S3", "03-architecture.md", "03-architecture.md",
            "text/markdown; charset=utf-8", path, ct);
    }

    public async Task SaveSystemDesignMarkdownAsync(
        string tenantId, long pipelineId, string markdown, CancellationToken ct = default)
    {
        var path = await WriteTextAsync(tenantId, pipelineId, "04-system-design.md", markdown, ct);
        await UpsertDeliverableIndexAsync(
            tenantId, pipelineId, "S4", "04-system-design.md", "04-system-design.md",
            "text/markdown; charset=utf-8", path, ct);
    }

    public async Task SaveDdlSqlAsync(
        string tenantId, long pipelineId, string sql, CancellationToken ct = default)
    {
        var path = await WriteTextAsync(tenantId, pipelineId, "05-ddl.sql", sql, ct);
        await UpsertDeliverableIndexAsync(
            tenantId, pipelineId, "S5", "05-ddl.sql", "05-ddl.sql",
            "text/plain; charset=utf-8", path, ct);
    }

    public async Task SaveFormPageIrAsync(
        string tenantId, long pipelineId, string json, CancellationToken ct = default)
    {
        var path = await WriteTextAsync(tenantId, pipelineId, "06-formpage-ir.json", json, ct);
        await UpsertDeliverableIndexAsync(
            tenantId, pipelineId, "S6", "06-formpage-ir.json", "06-formpage-ir.json",
            "application/json; charset=utf-8", path, ct);
    }

    public async Task SaveCodegenManifestAsync(
        string tenantId, long pipelineId, string json, CancellationToken ct = default)
    {
        var path = await WriteTextAsync(tenantId, pipelineId, "07-codegen-manifest.json", json, ct);
        await UpsertDeliverableIndexAsync(
            tenantId, pipelineId, "S7", "07-codegen-manifest.json", "07-codegen-manifest.json",
            "application/json; charset=utf-8", path, ct);
    }

    public async Task SaveTestSuiteJsonAsync(
        string tenantId, long pipelineId, string json, CancellationToken ct = default)
    {
        var path = await WriteTextAsync(tenantId, pipelineId, "08-testsuite.json", json, ct);
        await UpsertDeliverableIndexAsync(
            tenantId, pipelineId, "S7", "08-testsuite.json", "08-testsuite.json",
            "application/json; charset=utf-8", path, ct);
    }

    public async Task SaveDeploymentReportAsync(
        string tenantId, long pipelineId, string markdown, CancellationToken ct = default)
    {
        var path = await WriteTextAsync(tenantId, pipelineId, "09-deployment-report.md", markdown, ct);
        await UpsertDeliverableIndexAsync(
            tenantId, pipelineId, "S8", "09-deployment-report.md", "09-deployment-report.md",
            "text/markdown; charset=utf-8", path, ct);
    }

    public async Task<IReadOnlyList<DeliverableListItem>> ListByPipelineAsync(
        long pipelineId,
        string? stageCode,
        CancellationToken ct = default)
    {
        var pipelineKey = pipelineId.ToString();
        var tenantId = TenantResolver.Resolve().ToString();
        var query = _db.Queryable<InteAssistantDeliverable>()
            .Where(d => d.PipelineId == pipelineKey
                        && d.TenantId == tenantId
                        && d.DeleteMark == false);

        if (!string.IsNullOrWhiteSpace(stageCode))
            query = query.Where(d => d.StageCode == stageCode);

        var rows = await query.OrderBy(d => d.CreateTime).ToListAsync(ct);
        return rows.Select(MapListItem).ToList();
    }

    public async Task<(string absolutePath, string contentType, string fileName)> ResolveDeliverableAsync(
        long pipelineId,
        string relativePath,
        CancellationToken ct = default)
    {
        var tenantId = TenantResolver.Resolve().ToString();
        var row = await _db.Queryable<InteAssistantDeliverable>()
            .Where(d => d.PipelineId == pipelineId.ToString()
                        && d.TenantId == tenantId
                        && d.RelativePath == relativePath
                        && d.DeleteMark == false)
            .FirstAsync(ct);

        if (row == null)
            throw new FileNotFoundException($"交付物不存在: {relativePath}");

        var projectId = await ResolveProjectIdAsync(pipelineId, ct);
        var absolute = Path.Combine(
            StudioWorkspaceHelper.GetDeliverablesPath(row.TenantId, projectId, pipelineId.ToString()),
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(absolute))
            throw new FileNotFoundException($"交付物文件缺失: {relativePath}");

        StudioWorkspaceHelper.AssertWithinDeliverables(absolute, row.TenantId, projectId, pipelineId.ToString());
        return (absolute, row.ContentType, row.FileName);
    }

    private async Task<string> ResolveProjectIdAsync(long pipelineId, CancellationToken ct)
    {
        var pipeline = await _db.Queryable<AiPipelineEntity>()
            .Where(x => x.Id == pipelineId.ToString())
            .Select(x => new { x.ProjectId })
            .FirstAsync(ct);

        if (pipeline == null)
            return pipelineId.ToString();

        return string.IsNullOrWhiteSpace(pipeline.ProjectId)
            ? pipelineId.ToString()
            : pipeline.ProjectId;
    }

    private async Task<string> WriteTextAsync(
        string tenantId,
        long pipelineId,
        string relativePath,
        string content,
        CancellationToken ct)
    {
        var pipelineKey = pipelineId.ToString();
        var projectId = await ResolveProjectIdAsync(pipelineId, ct);
        StudioWorkspaceHelper.EnsureDeliverablesDirectory(tenantId, projectId, pipelineKey);
        var absolute = Path.Combine(
            StudioWorkspaceHelper.GetDeliverablesPath(tenantId, projectId, pipelineKey),
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        StudioWorkspaceHelper.AssertWithinDeliverables(absolute, tenantId, projectId, pipelineKey);

        var dir = Path.GetDirectoryName(absolute);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(absolute, content, Encoding.UTF8, ct);
        return absolute;
    }

    private async Task UpsertDeliverableIndexAsync(
        string tenantId,
        long pipelineId,
        string stageCode,
        string fileName,
        string relativePath,
        string contentType,
        string absolutePath,
        CancellationToken ct)
    {
        var pipelineKey = pipelineId.ToString();
        var fileInfo = new FileInfo(absolutePath);
        var existing = await _db.Queryable<InteAssistantDeliverable>()
            .Where(d => d.PipelineId == pipelineKey
                        && d.TenantId == tenantId
                        && d.RelativePath == relativePath
                        && d.DeleteMark == false)
            .FirstAsync(ct);

        if (existing != null)
        {
            await _db.Updateable<InteAssistantDeliverable>()
                .SetColumns(d => d.FileSize == fileInfo.Length)
                .SetColumns(d => d.ContentType == contentType)
                .SetColumns(d => d.CreateTime == DateTime.Now)
                .Where(d => d.F_Id == existing.F_Id)
                .ExecuteCommandAsync(ct);
            return;
        }

        var entity = new InteAssistantDeliverable
        {
            F_Id = Guid.NewGuid().ToString("N"),
            PipelineId = pipelineKey,
            // ProjectId 兜底为 pipelineId（历史兼容；多 pipeline 共享 project 场景待调用方传入精确值）
            ProjectId = pipelineKey,
            StageCode = stageCode,
            FileName = fileName,
            RelativePath = relativePath,
            ContentType = contentType,
            FileSize = fileInfo.Length,
            CreateTime = DateTime.Now,
            TenantId = tenantId,
            DeleteMark = false,
        };
        await _db.Insertable(entity).ExecuteCommandAsync(ct);
    }

    private static DeliverableListItem MapListItem(InteAssistantDeliverable row) =>
        new()
        {
            Id = row.F_Id,
            StageCode = row.StageCode,
            FileName = row.FileName,
            RelativePath = row.RelativePath,
            ContentType = row.ContentType,
            FileSize = row.FileSize,
            CreateTime = row.CreateTime,
            DownloadUrl = $"/api/studio/pipeline/execute/{row.PipelineId}/deliverables/content?relativePath={Uri.EscapeDataString(row.RelativePath)}",
        };

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "attachment";
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Length > 80 ? name[..80] : name;
    }
}
