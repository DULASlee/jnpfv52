using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using JNPF.Common.Core.Manager.Files;
using JNPF.Common.Core.MultiTenancy;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Gates;
using JNPF.InteAssistant.Infrastructure.Background;
using JNPF.InteAssistant.Studio;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace JNPF.InteAssistant.Infrastructure.Attachments;

public interface IPipelineAttachmentService
{
    /// <summary>将前端 annex URL 登记到 inte_assistant_attachment（去重）</summary>
    Task<int> RegisterAsync(
        long pipelineId,
        IEnumerable<AttachmentRegisterItem> payloads,
        RequestContext ctx,
        CancellationToken ct = default);

    /// <summary>下载、解析并更新 ProcessStatus；返回门控可用的 AttachmentFile（含字节与缓存文本）</summary>
    Task<AttachmentPrepareResult> PrepareForGateAsync(
        long pipelineId,
        RequestContext ctx,
        CancellationToken ct = default);

    /// <summary>列出 Pipeline 已登记附件（含解析状态）</summary>
    Task<IReadOnlyList<PipelineAttachmentListItem>> ListByPipelineAsync(
        long pipelineId,
        CancellationToken ct = default);

    /// <summary>下载附件原文件字节</summary>
    Task<(byte[] Content, string FileName, string ContentType)> DownloadOriginalAsync(
        long pipelineId,
        string attachmentId,
        RequestContext ctx,
        CancellationToken ct = default);

    /// <summary>获取附件解析文本（DB 缓存）</summary>
    Task<string?> GetExtractedTextAsync(
        long pipelineId,
        string attachmentId,
        CancellationToken ct = default);
}

public sealed record PipelineAttachmentListItem
{
    public string Id { get; init; } = "";
    public string FileName { get; init; } = "";
    public string FileUrl { get; init; } = "";
    public string FileType { get; init; } = "";
    public long FileSize { get; init; }
    public int ProcessStatus { get; init; }
    public int ExtractedLength { get; init; }
    public string? ProcessError { get; init; }
    public DateTime CreateTime { get; init; }
    public string DownloadOriginalUrl { get; init; } = "";
    public string DownloadExtractedUrl { get; init; } = "";
}

public sealed record AttachmentPrepareResult
{
    public List<AttachmentFile> Files { get; init; } = new();
    public List<AttachmentItemSummary> Items { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public int FailedCount { get; init; }
}

public sealed record AttachmentItemSummary
{
    public string Id { get; init; } = "";
    public string FileName { get; init; } = "";
    public int ProcessStatus { get; init; }
    public int ExtractedLength { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// 流水线附件入库与解析 — execute / sa-gate 共用
/// 表：<see cref="InteAssistantAttachment"/>（F_ProcessStatus: 0待处理 2已完成 3失败）
/// </summary>
public sealed class PipelineAttachmentService : IPipelineAttachmentService, ITransient
{
    private static readonly Regex AnnexFileUrlPattern = new(
        @"(?i)/api/file/image/(?<type>[^/]+)/(?<fileName>[^/?#]+)",
        RegexOptions.Compiled);

    private readonly ISqlSugarClient _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileManager _fileManager;
    private readonly AttachmentProcessor _attachmentProcessor;
    private readonly IAttachmentChunkArchive _chunkArchive;
    private readonly IPipelineDeliverableService _deliverableService;
    private readonly ILogger<PipelineAttachmentService> _logger;

    public PipelineAttachmentService(
        ISqlSugarClient db,
        IHttpClientFactory httpClientFactory,
        IFileManager fileManager,
        AttachmentProcessor attachmentProcessor,
        IAttachmentChunkArchive chunkArchive,
        IPipelineDeliverableService deliverableService,
        ILogger<PipelineAttachmentService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _fileManager = fileManager;
        _attachmentProcessor = attachmentProcessor;
        _chunkArchive = chunkArchive;
        _deliverableService = deliverableService;
        _logger = logger;
    }

    public async Task<int> RegisterAsync(
        long pipelineId,
        IEnumerable<AttachmentRegisterItem> payloads,
        RequestContext ctx,
        CancellationToken ct = default)
    {
        var list = payloads?.Where(p => !string.IsNullOrWhiteSpace(p.Url)).ToList() ?? new List<AttachmentRegisterItem>();
        if (list.Count == 0) return 0;

        var pipelineKey = pipelineId.ToString();
        var existing = await _db.Queryable<InteAssistantAttachment>()
            .Where(a => a.PipelineId == pipelineKey && a.DeleteMark == false)
            .ToListAsync(ct);

        var inserted = 0;
        foreach (var att in list)
        {
            var dup = existing.FirstOrDefault(e => e.FileUrl == att.Url);
            if (dup != null)
            {
                if (dup.ProcessStatus == 3)
                {
                    await _db.Updateable<InteAssistantAttachment>()
                        .SetColumns(a => a.ProcessStatus == 0)
                        .SetColumns(a => a.ProcessError == null)
                        .SetColumns(a => a.LastModifyTime == DateTime.Now)
                        .Where(a => a.F_Id == dup.F_Id)
                        .ExecuteCommandAsync(ct);
                    dup.ProcessStatus = 0;
                    dup.ProcessError = null;
                }
                continue;
            }

            var entity = new InteAssistantAttachment
            {
                F_Id = Guid.NewGuid().ToString("N"),
                PipelineId = pipelineKey,
                // 三元组血缘:ProjectId 兜底为 pipelineId(历史数据 projectId≡pipelineId)
                ProjectId = pipelineKey,
                FileName = string.IsNullOrWhiteSpace(att.Name) ? Path.GetFileName(att.Url) : att.Name,
                FileUrl = att.Url,
                FileSize = 0,
                FileType = Path.GetExtension(att.Name)?.TrimStart('.') ?? "",
                ProcessStatus = 0,
                CreatorUserId = ctx.UserId,
                CreatorUserName = ctx.UserName,
                TenantId = ctx.TenantId,
                CreateTime = DateTime.Now,
                DeleteMark = false,
            };

            await _db.Insertable(entity).ExecuteCommandAsync(ct);
            existing.Add(entity);
            inserted++;
        }

        _logger.LogInformation("附件登记: PipelineId={Id}, 新增={Count}", pipelineId, inserted);
        return inserted;
    }

    public async Task<AttachmentPrepareResult> PrepareForGateAsync(
        long pipelineId,
        RequestContext ctx,
        CancellationToken ct = default)
    {
        var pipelineKey = pipelineId.ToString();
        var rows = await _db.Queryable<InteAssistantAttachment>()
            .Where(a => a.PipelineId == pipelineKey && a.DeleteMark == false)
            .OrderBy(a => a.CreateTime)
            .ToListAsync(ct);

        var files = new List<AttachmentFile>();
        var items = new List<AttachmentItemSummary>();
        var warnings = new List<string>();
        var failed = 0;
        var http = _httpClientFactory.CreateClient();

        foreach (var att in rows)
        {
            byte[] bytes;
            try
            {
                bytes = await DownloadAsync(http, ctx, att.FileUrl, ct);
            }
            catch (Exception ex)
            {
                failed++;
                await MarkFailedAsync(att.F_Id, ex.Message, ct);
                warnings.Add($"附件 {att.FileName} 下载失败");
                items.Add(new AttachmentItemSummary
                {
                    Id = att.F_Id,
                    FileName = att.FileName,
                    ProcessStatus = 3,
                    Error = ex.Message,
                });
                continue;
            }

            if (att.ProcessStatus == 3)
            {
                att.ProcessStatus = 0;
                att.ProcessError = null;
            }

            if (att.ProcessStatus != 2 || string.IsNullOrWhiteSpace(att.ExtractedText))
            {
                try
                {
                    await _db.Updateable<InteAssistantAttachment>()
                        .SetColumns(a => a.ProcessStatus == 1)
                        .SetColumns(a => a.LastModifyTime == DateTime.Now)
                        .Where(a => a.F_Id == att.F_Id)
                        .ExecuteCommandAsync(ct);

                    var tenantId = string.IsNullOrWhiteSpace(att.TenantId) ? ctx.TenantId : att.TenantId;
                    var projectId = string.IsNullOrWhiteSpace(att.ProjectId) ? pipelineKey : att.ProjectId;
                    var fileHash = ComputeSha256(bytes);

                    // 分批解析 → 每批增量写入 StudioWorkspace 分块存档 → 再合并取出
                    string extracted;
                    int chunkCount;
                    try
                    {
                        (extracted, chunkCount) = await ExtractToChunkArchiveAsync(
                            tenantId, projectId, pipelineKey, att, bytes, ct);
                    }
                    catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
                    {
                        _logger.LogWarning(ex, "附件分批解析首次失败，重试一次: {FileName}", att.FileName);
                        await Task.Delay(200, ct);
                        (extracted, chunkCount) = await ExtractToChunkArchiveAsync(
                            tenantId, projectId, pipelineKey, att, bytes, ct);
                    }

                    // #region agent log
                    AgentDebugLog("A", "PipelineAttachmentService.PrepareForGateAsync", "chunk extract ready",
                        $"{{\"fileName\":{JsonStr(att.FileName)},\"bytes\":{bytes.Length},\"extractedLen\":{extracted.Length},\"chunkCount\":{chunkCount},\"hashLen\":{fileHash.Length}}}");
                    // #endregion

                    // DB 只缓存短预览；全文权威源 = StudioWorkspace 分块存档（禁止再因 NVARCHAR 截断判失败）
                    var dbPreview = BuildDbPreview(extracted);
                    try
                    {
                        await PersistExtractSuccessAsync(att.F_Id, dbPreview, fileHash, bytes.Length, ct);
                    }
                    catch (Exception persistEx) when (persistEx is not OutOfMemoryException and not OperationCanceledException)
                    {
                        _logger.LogWarning(persistEx,
                            "附件预览写入 DB 失败，分块存档已就绪，继续门控: {FileName}", att.FileName);
                        await MarkProcessStatusOnlyAsync(att.F_Id, processStatus: 2, ct);
                        // #region agent log
                        AgentDebugLog("D", "PipelineAttachmentService.PrepareForGateAsync", "db preview persist failed; chunks ok",
                            $"{{\"fileName\":{JsonStr(att.FileName)},\"err\":{JsonStr(persistEx.Message)},\"chunkCount\":{chunkCount},\"extractedLen\":{extracted.Length}}}");
                        // #endregion
                    }

                    att.ProcessStatus = 2;
                    att.ExtractedText = extracted;
                    att.FileSize = bytes.Length;

                    if (!string.IsNullOrWhiteSpace(extracted) && !string.IsNullOrWhiteSpace(tenantId))
                    {
                        // 交付物也按分块权威：写入合并全文到文件，不依赖 DB 列宽
                        await _deliverableService.SaveAttachmentExtractAsync(
                            tenantId, pipelineId, att.F_Id, att.FileName, extracted, ct);
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    // #region agent log
                    AgentDebugLog("A", "PipelineAttachmentService.PrepareForGateAsync", "extract/persist failed",
                        $"{{\"fileName\":{JsonStr(att.FileName)},\"err\":{JsonStr(ex.Message)},\"exType\":{JsonStr(ex.GetType().Name)}}}");
                    // #endregion
                    await MarkFailedAsync(att.F_Id, ex.Message, ct);
                    _logger.LogWarning(ex, "附件解析失败: PipelineId={Id}, FileName={FileName}", pipelineId, att.FileName);
                    warnings.Add($"附件 {att.FileName} 解析失败：{ex.Message}");
                    items.Add(new AttachmentItemSummary
                    {
                        Id = att.F_Id,
                        FileName = att.FileName,
                        ProcessStatus = 3,
                        Error = ex.Message,
                    });
                    continue;
                }
            }
            else if (att.FileSize <= 0)
            {
                await _db.Updateable<InteAssistantAttachment>()
                    .SetColumns(a => a.FileSize == bytes.Length)
                    .SetColumns(a => a.LastModifyTime == DateTime.Now)
                    .Where(a => a.F_Id == att.F_Id)
                    .ExecuteCommandAsync(ct);
            }

            // 优先从分块存档合并全文（权威源）；无存档再回退 DB 缓存
            var preText = await ResolveExtractedTextAsync(att, pipelineKey, ct) ?? att.ExtractedText;

            files.Add(new AttachmentFile
            {
                FileName = att.FileName,
                Content = bytes,
                PreExtractedText = preText,
                AttachmentId = att.F_Id,
            });
            items.Add(new AttachmentItemSummary
            {
                Id = att.F_Id,
                FileName = att.FileName,
                ProcessStatus = 2,
                ExtractedLength = preText?.Length ?? 0,
            });
        }

        return new AttachmentPrepareResult
        {
            Files = files,
            Items = items,
            Warnings = warnings,
            FailedCount = failed,
        };
    }

    /// <summary>
    /// 分批解析附件，每批立即写入 StudioWorkspace 分块存档，最后按序合并。
    /// </summary>
    private async Task<(string Merged, int ChunkCount)> ExtractToChunkArchiveAsync(
        string tenantId, string projectId, string pipelineId,
        InteAssistantAttachment att, byte[] bytes, CancellationToken ct)
    {
        await _chunkArchive.ResetAsync(tenantId, projectId, pipelineId, att.F_Id, att.FileName, ct);

        var file = new AttachmentFile { FileName = att.FileName, Content = bytes, AttachmentId = att.F_Id };
        var header = $"===== 附件：{att.FileName} =====\n";
        var wrote = 0;

        foreach (var chunk in _attachmentProcessor.ExtractChunks(file, AttachmentProcessor.DefaultTargetChunkChars))
        {
            ct.ThrowIfCancellationRequested();
            var body = wrote == 0 ? header + chunk.Text : chunk.Text;
            await _chunkArchive.AppendChunkAsync(
                tenantId, projectId, pipelineId, att.F_Id,
                chunk.Index, body, chunk.SourceHint, ct);
            wrote++;
        }

        if (wrote == 0)
        {
            await _chunkArchive.AppendChunkAsync(
                tenantId, projectId, pipelineId, att.F_Id,
                0, header + "[附件无可提取文本]", "empty", ct);
            wrote = 1;
        }

        var merged = await _chunkArchive.TryMergeAsync(tenantId, projectId, pipelineId, att.F_Id, ct);
        if (merged == null)
            throw new InvalidOperationException($"分块存档合并失败: {att.FileName}");

        _logger.LogInformation(
            "附件分批解析完成: File={File} Chunks={Chunks} Chars={Chars} Pipeline={Pipeline}",
            att.FileName, merged.Value.Manifest.ChunkCount, merged.Value.Manifest.TotalChars, pipelineId);

        return (merged.Value.Text, merged.Value.Manifest.ChunkCount);
    }

    private async Task<string?> ResolveExtractedTextAsync(
        InteAssistantAttachment att, string pipelineKey, CancellationToken ct)
    {
        var tenantId = att.TenantId;
        var projectId = string.IsNullOrWhiteSpace(att.ProjectId) ? pipelineKey : att.ProjectId;
        if (string.IsNullOrWhiteSpace(tenantId)) return att.ExtractedText;

        var merged = await _chunkArchive.TryMergeAsync(tenantId, projectId, pipelineKey, att.F_Id, ct);
        return merged?.Text ?? att.ExtractedText;
    }

    public async Task<IReadOnlyList<PipelineAttachmentListItem>> ListByPipelineAsync(
        long pipelineId,
        CancellationToken ct = default)
    {
        var pipelineKey = pipelineId.ToString();
        var tenantId = TenantResolver.Resolve().ToString();
        var rows = await _db.Queryable<InteAssistantAttachment>()
            .Where(a => a.PipelineId == pipelineKey && a.TenantId == tenantId && a.DeleteMark == false)
            .OrderBy(a => a.CreateTime)
            .ToListAsync(ct);

        return rows.Select(a =>
        {
            var projectId = string.IsNullOrWhiteSpace(a.ProjectId) ? pipelineKey : a.ProjectId;
            var manifest = _chunkArchive.TryReadManifest(a.TenantId, projectId, pipelineKey, a.F_Id);
            var extractedLen = manifest?.TotalChars ?? a.ExtractedText?.Length ?? 0;
            return new PipelineAttachmentListItem
            {
                Id = a.F_Id,
                FileName = a.FileName,
                FileUrl = a.FileUrl,
                FileType = a.FileType,
                FileSize = a.FileSize,
                ProcessStatus = a.ProcessStatus,
                ExtractedLength = extractedLen,
                ProcessError = a.ProcessError,
                CreateTime = a.CreateTime,
                DownloadOriginalUrl = $"/api/studio/pipeline/execute/{pipelineId}/attachments/{a.F_Id}/download",
                DownloadExtractedUrl = a.ProcessStatus == 2 && extractedLen > 0
                    ? $"/api/studio/pipeline/execute/{pipelineId}/attachments/{a.F_Id}/extracted"
                    : "",
            };
        }).ToList();
    }

    public async Task<(byte[] Content, string FileName, string ContentType)> DownloadOriginalAsync(
        long pipelineId,
        string attachmentId,
        RequestContext ctx,
        CancellationToken ct = default)
    {
        var att = await GetAttachmentOrThrowAsync(pipelineId, attachmentId, ct);
        var http = _httpClientFactory.CreateClient();
        var bytes = await DownloadAsync(http, ctx, att.FileUrl, ct);
        var contentType = GuessContentType(att.FileName);
        return (bytes, att.FileName, contentType);
    }

    public async Task<string?> GetExtractedTextAsync(
        long pipelineId,
        string attachmentId,
        CancellationToken ct = default)
    {
        var att = await GetAttachmentOrThrowAsync(pipelineId, attachmentId, ct);
        if (att.ProcessStatus != 2)
            return null;

        var merged = await ResolveExtractedTextAsync(att, pipelineId.ToString(), ct);
        return string.IsNullOrWhiteSpace(merged) ? null : merged;
    }

    private async Task<InteAssistantAttachment> GetAttachmentOrThrowAsync(
        long pipelineId, string attachmentId, CancellationToken ct)
    {
        var tenantId = TenantResolver.Resolve().ToString();
        var att = await _db.Queryable<InteAssistantAttachment>()
            .Where(a => a.F_Id == attachmentId
                        && a.PipelineId == pipelineId.ToString()
                        && a.TenantId == tenantId
                        && a.DeleteMark == false)
            .FirstAsync(ct);

        if (att == null)
            throw new InvalidOperationException($"附件不存在: {attachmentId}");
        return att;
    }

    private static string GuessContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".txt" => "text/plain; charset=utf-8",
            ".md" => "text/markdown; charset=utf-8",
            _ => "application/octet-stream",
        };
    }

    private async Task<byte[]> DownloadAsync(HttpClient http, RequestContext ctx, string fileUrl, CancellationToken ct)
    {
        if (ctx.DownloadCache.TryGetValue(fileUrl, out var cached))
            return cached;

        var bytes = await TryReadLocalAnnexAsync(fileUrl, ct)
            ?? await DownloadViaHttpAsync(http, ctx, fileUrl, ct);
        ctx.DownloadCache[fileUrl] = bytes;
        return bytes;
    }

    /// <summary>annex URL 优先走磁盘读取，避免 TenantMiddleware 对 /api/File/* 的 HTTP 自调用 403</summary>
    private async Task<byte[]?> TryReadLocalAnnexAsync(string fileUrl, CancellationToken ct)
    {
        var match = AnnexFileUrlPattern.Match(fileUrl);
        if (!match.Success)
            return null;

        var type = match.Groups["type"].Value;
        var fileName = match.Groups["fileName"].Value.Replace("@", ".");
        var filePath = Path.Combine(_fileManager.GetPathByType(type), fileName);

        await using var stream = await _fileManager.GetFileStream(filePath);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }

    private async Task<byte[]> DownloadViaHttpAsync(HttpClient http, RequestContext ctx, string fileUrl, CancellationToken ct)
    {
        var resolved = fileUrl;
        if (!resolved.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = ctx.GetBaseUrl();
            if (string.IsNullOrEmpty(baseUrl))
                throw new InvalidOperationException($"无法解析附件 URL: {fileUrl}");
            resolved = baseUrl + (fileUrl.StartsWith('/') ? fileUrl : "/" + fileUrl);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, resolved);
        if (!string.IsNullOrWhiteSpace(ctx.Authorization))
            request.Headers.TryAddWithoutValidation("Authorization", ctx.Authorization);
        if (!string.IsNullOrWhiteSpace(ctx.TenantId))
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", ctx.TenantId);
        request.Headers.TryAddWithoutValidation("jnpf-origin", "pc");

        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task MarkFailedAsync(string id, string message, CancellationToken ct)
    {
        var errMsg = message.Length > 2000 ? message[..2000] : message;
        await _db.Updateable<InteAssistantAttachment>()
            .SetColumns(a => a.ProcessStatus == 3)
            .SetColumns(a => a.ProcessError == errMsg)
            .SetColumns(a => a.LastModifyTime == DateTime.Now)
            .Where(a => a.F_Id == id)
            .ExecuteCommandAsync(ct);
    }

    /// <summary>DB 预览上限（分块存档才是全文；列宽兼容 NVARCHAR(4000)）。</summary>
    private const int DbExtractPreviewMaxChars = 3500;

    private static string BuildDbPreview(string extracted)
    {
        if (string.IsNullOrEmpty(extracted)) return "";
        if (extracted.Length <= DbExtractPreviewMaxChars) return extracted;
        return extracted[..DbExtractPreviewMaxChars] + "\n…(全文见附件分块存档，共 " + extracted.Length + " 字)";
    }

    /// <summary>
    /// 写入附件表成功态 + 短预览。全文 MUST 读分块存档，禁止依赖本列存完整大文件。
    /// </summary>
    private async Task PersistExtractSuccessAsync(
        string id, string? extractedPreview, string fileHash, long fileSize, CancellationToken ct)
    {
        var extractedParam = new SugarParameter("@extracted", (object?)extractedPreview ?? DBNull.Value)
        {
            DbType = System.Data.DbType.String,
            Size = -1,
        };
        await _db.Ado.ExecuteCommandAsync(
            @"UPDATE [inte_assistant_attachment]
              SET [F_ProcessStatus] = 2,
                  [F_ExtractedText] = @extracted,
                  [F_FileHash] = @hash,
                  [F_FileSize] = @size,
                  [F_ProcessError] = NULL,
                  [F_LastModifyTime] = GETDATE()
              WHERE [F_Id] = @id",
            extractedParam,
            new SugarParameter("@hash", fileHash),
            new SugarParameter("@size", fileSize),
            new SugarParameter("@id", id));
        ct.ThrowIfCancellationRequested();
    }

    private async Task MarkProcessStatusOnlyAsync(string id, int processStatus, CancellationToken ct)
    {
        await _db.Updateable<InteAssistantAttachment>()
            .SetColumns(a => a.ProcessStatus == processStatus)
            .SetColumns(a => a.ProcessError == null)
            .SetColumns(a => a.LastModifyTime == DateTime.Now)
            .Where(a => a.F_Id == id)
            .ExecuteCommandAsync(ct);
    }

    private static string ComputeSha256(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // #region agent log
    private static void AgentDebugLog(string hypothesisId, string location, string message, string dataJson)
    {
        try
        {
            var line =
                $"{{\"sessionId\":\"ead5d0\",\"runId\":\"post-fix\",\"hypothesisId\":{JsonStr(hypothesisId)},\"location\":{JsonStr(location)},\"message\":{JsonStr(message)},\"data\":{dataJson},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            const string path = @"D:\JNPF-v52\debug-ead5d0.log";
            File.AppendAllText(path, line, Encoding.UTF8);
        }
        catch
        {
            /* debug ingest must never break gate */
        }
    }

    private static string JsonStr(string? s) =>
        System.Text.Json.JsonSerializer.Serialize(s ?? "");
    // #endregion
}

public sealed record AttachmentRegisterItem
{
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
}
