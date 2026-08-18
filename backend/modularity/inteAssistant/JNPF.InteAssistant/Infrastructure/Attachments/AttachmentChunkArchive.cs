using System.Text;
using System.Text.Json;
using JNPF.DependencyInjection;
using JNPF.InteAssistant.Gates;
using Microsoft.Extensions.Logging;

namespace JNPF.InteAssistant.Infrastructure.Attachments;

/// <summary>
/// 附件解析文本分块存档（StudioWorkspace 文件增量写入）。
/// 路径：{pipelineRoot}/attachments/{attachmentId}/chunks/NNNN.txt + manifest.json
/// 消费方 MUST 通过 <see cref="MergeAsync"/> 按序取出合并，禁止依赖单字段截断全文。
/// </summary>
public interface IAttachmentChunkArchive
{
    /// <summary>清空旧分块并初始化 manifest（解析开始前调用）。</summary>
    Task ResetAsync(
        string tenantId, string projectId, string pipelineId,
        string attachmentId, string fileName, CancellationToken ct = default);

    /// <summary>增量写入一块文本（立即落盘 + 更新 manifest）。</summary>
    Task AppendChunkAsync(
        string tenantId, string projectId, string pipelineId,
        string attachmentId, int chunkIndex, string text, string sourceHint,
        CancellationToken ct = default);

    /// <summary>按 chunkIndex 升序合并全部块；无存档返回 null。</summary>
    Task<(string Text, AttachmentChunkManifest Manifest)?> TryMergeAsync(
        string tenantId, string projectId, string pipelineId,
        string attachmentId, CancellationToken ct = default);

    /// <summary>读取 manifest；不存在返回 null。</summary>
    AttachmentChunkManifest? TryReadManifest(
        string tenantId, string projectId, string pipelineId, string attachmentId);

    bool Exists(string tenantId, string projectId, string pipelineId, string attachmentId);
}

public sealed class AttachmentChunkManifest
{
    public string TenantId { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public string PipelineId { get; set; } = "";
    public string AttachmentId { get; set; } = "";
    public string FileName { get; set; } = "";
    public int ChunkCount { get; set; }
    public int TotalChars { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<AttachmentChunkManifestEntry> Chunks { get; set; } = new();
}

public sealed class AttachmentChunkManifestEntry
{
    public int Index { get; set; }
    public int Chars { get; set; }
    public string SourceHint { get; set; } = "";
    public string RelativePath { get; set; } = "";
}

public sealed class AttachmentChunkArchive : IAttachmentChunkArchive, ITransient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ILogger<AttachmentChunkArchive> _logger;

    public AttachmentChunkArchive(ILogger<AttachmentChunkArchive> logger)
    {
        _logger = logger;
    }

    public Task ResetAsync(
        string tenantId, string projectId, string pipelineId,
        string attachmentId, string fileName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var root = GetAttachmentRoot(tenantId, projectId, pipelineId, attachmentId);
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
        Directory.CreateDirectory(Path.Combine(root, "chunks"));

        var manifest = new AttachmentChunkManifest
        {
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = pipelineId,
            AttachmentId = attachmentId,
            FileName = fileName,
            ChunkCount = 0,
            TotalChars = 0,
            UpdatedAt = DateTime.Now,
            Chunks = new List<AttachmentChunkManifestEntry>(),
        };
        WriteManifest(root, manifest);
        // #region agent log
        AgentDebugLog("B", "AttachmentChunkArchive.ResetAsync", "chunk archive reset",
            $"{{\"root\":{JsonStr(root)},\"attachmentId\":{JsonStr(attachmentId)},\"fileName\":{JsonStr(fileName)}}}");
        // #endregion
        return Task.CompletedTask;
    }

    public Task AppendChunkAsync(
        string tenantId, string projectId, string pipelineId,
        string attachmentId, int chunkIndex, string text, string sourceHint,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var root = GetAttachmentRoot(tenantId, projectId, pipelineId, attachmentId);
        Directory.CreateDirectory(Path.Combine(root, "chunks"));

        var relative = $"chunks/{chunkIndex:D4}.txt";
        var absolute = Path.Combine(root, relative);
        var sanitized = AttachmentProcessor.SanitizeExtractedText(text ?? "");
        // 增量落盘：每块写完即 fsync 语义（File.WriteAllText）
        File.WriteAllText(absolute, sanitized, Encoding.UTF8);

        var manifest = TryReadManifest(root) ?? new AttachmentChunkManifest
        {
            TenantId = tenantId,
            ProjectId = projectId,
            PipelineId = pipelineId,
            AttachmentId = attachmentId,
        };
        manifest.FileName = string.IsNullOrWhiteSpace(manifest.FileName) ? "" : manifest.FileName;
        manifest.Chunks.RemoveAll(c => c.Index == chunkIndex);
        manifest.Chunks.Add(new AttachmentChunkManifestEntry
        {
            Index = chunkIndex,
            Chars = sanitized.Length,
            SourceHint = sourceHint ?? "",
            RelativePath = relative,
        });
        manifest.Chunks = manifest.Chunks.OrderBy(c => c.Index).ToList();
        manifest.ChunkCount = manifest.Chunks.Count;
        manifest.TotalChars = manifest.Chunks.Sum(c => c.Chars);
        manifest.UpdatedAt = DateTime.Now;
        WriteManifest(root, manifest);

        _logger.LogDebug(
            "附件分块已写入 attachment={Att} index={Idx} chars={Chars} hint={Hint}",
            attachmentId, chunkIndex, sanitized.Length, sourceHint);
        // #region agent log
        if (chunkIndex == 0 || chunkIndex % 5 == 0)
        {
            AgentDebugLog("B", "AttachmentChunkArchive.AppendChunkAsync", "chunk appended",
                $"{{\"attachmentId\":{JsonStr(attachmentId)},\"chunkIndex\":{chunkIndex},\"chars\":{sanitized.Length},\"totalChars\":{manifest.TotalChars},\"chunkCount\":{manifest.ChunkCount},\"hint\":{JsonStr(sourceHint)},\"path\":{JsonStr(absolute)}}}");
        }
        // #endregion
        return Task.CompletedTask;
    }

    public async Task<(string Text, AttachmentChunkManifest Manifest)?> TryMergeAsync(
        string tenantId, string projectId, string pipelineId,
        string attachmentId, CancellationToken ct = default)
    {
        var root = GetAttachmentRoot(tenantId, projectId, pipelineId, attachmentId);
        var manifest = TryReadManifest(root);
        if (manifest == null || manifest.ChunkCount == 0)
            return null;

        var sb = new StringBuilder(Math.Max(256, manifest.TotalChars));
        var missing = 0;
        foreach (var entry in manifest.Chunks.OrderBy(c => c.Index))
        {
            ct.ThrowIfCancellationRequested();
            var path = Path.Combine(root, entry.RelativePath);
            if (!File.Exists(path))
            {
                missing++;
                _logger.LogWarning("分块文件缺失: {Path}", path);
                continue;
            }
            var part = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append(part);
        }

        var merged = sb.ToString();
        // #region agent log
        AgentDebugLog("C", "AttachmentChunkArchive.TryMergeAsync", "chunks merged",
            $"{{\"attachmentId\":{JsonStr(attachmentId)},\"chunkCount\":{manifest.ChunkCount},\"manifestTotalChars\":{manifest.TotalChars},\"mergedLen\":{merged.Length},\"missingChunks\":{missing},\"root\":{JsonStr(root)}}}");
        // #endregion
        return (merged, manifest);
    }

    public AttachmentChunkManifest? TryReadManifest(
        string tenantId, string projectId, string pipelineId, string attachmentId)
        => TryReadManifest(GetAttachmentRoot(tenantId, projectId, pipelineId, attachmentId));

    public bool Exists(string tenantId, string projectId, string pipelineId, string attachmentId)
    {
        var root = GetAttachmentRoot(tenantId, projectId, pipelineId, attachmentId);
        return File.Exists(Path.Combine(root, "manifest.json"));
    }

    private static string GetAttachmentRoot(
        string tenantId, string projectId, string pipelineId, string attachmentId)
    {
        var pipelineRoot = StudioWorkspaceHelper.GetPipelinePath(tenantId, projectId, pipelineId);
        return Path.Combine(pipelineRoot, "attachments", attachmentId);
    }

    private static AttachmentChunkManifest? TryReadManifest(string root)
    {
        var path = Path.Combine(root, "manifest.json");
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<AttachmentChunkManifest>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static void WriteManifest(string root, AttachmentChunkManifest manifest)
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "manifest.json");
        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    // #region agent log
    private static void AgentDebugLog(string hypothesisId, string location, string message, string dataJson)
    {
        try
        {
            var line =
                $"{{\"sessionId\":\"ead5d0\",\"runId\":\"chunk-verify\",\"hypothesisId\":{JsonStr(hypothesisId)},\"location\":{JsonStr(location)},\"message\":{JsonStr(message)},\"data\":{dataJson},\"timestamp\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n";
            File.AppendAllText(@"D:\JNPF-v52\debug-ead5d0.log", line, Encoding.UTF8);
        }
        catch { /* never break extract */ }
    }

    private static string JsonStr(string? s) => JsonSerializer.Serialize(s ?? "");
    // #endregion
}
