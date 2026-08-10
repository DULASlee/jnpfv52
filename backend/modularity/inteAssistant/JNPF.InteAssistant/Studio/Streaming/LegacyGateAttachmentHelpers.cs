using System.Security.Cryptography;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.InteAssistant.Gates;

namespace JNPF.InteAssistant.Studio.Streaming;

/// <summary>Column values for ProcessStatus=Running update.</summary>
public readonly record struct AttachmentRunningUpdate(int ProcessStatus, DateTime LastModifyTime);

/// <summary>Column values for ProcessStatus=Done update.</summary>
public readonly record struct AttachmentDoneUpdate(
    int ProcessStatus,
    string? ExtractedText,
    string FileHash,
    DateTime LastModifyTime);

/// <summary>Column values for ProcessStatus=Failed update.</summary>
public readonly record struct AttachmentFailedUpdate(
    int ProcessStatus,
    string ProcessError,
    DateTime LastModifyTime);

/// <summary>
/// Pure helpers for LEGACY gate attachment persist / cache / error shaping.
/// Download HTTP, DB, and AttachmentProcessor stay at the call site.
/// </summary>
public static class LegacyGateAttachmentHelpers
{
    public const int ProcessStatusPending = 0;
    public const int ProcessStatusRunning = 1;
    public const int ProcessStatusDone = 2;
    public const int ProcessStatusFailed = 3;
    public const int ProcessErrorMaxLength = 2000;

    /// <summary>Legacy HttpClient timeout for attachment self-download.</summary>
    public static readonly TimeSpan AttachmentDownloadTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Legacy equality: empty/null URLs also dedupe (AttachmentPayload defaults to "").
    /// </summary>
    public static bool UrlAlreadyExists(IEnumerable<string?> existingUrls, string? url)
        => existingUrls.Any(e => e == url);

    public static string FileTypeFromFileName(string? fileName)
        => Path.GetExtension(fileName)?.TrimStart('.') ?? string.Empty;

    public static InteAssistantAttachment CreatePendingEntity(
        string pipelineId,
        string projectId,
        string fileName,
        string fileUrl,
        string? creatorUserId,
        string? creatorUserName,
        string tenantId,
        DateTime createTime,
        string? id = null)
    {
        return new InteAssistantAttachment
        {
            F_Id = id ?? Guid.NewGuid().ToString("N"),
            PipelineId = pipelineId,
            ProjectId = projectId,
            FileName = fileName,
            FileUrl = fileUrl,
            FileSize = 0,
            FileType = FileTypeFromFileName(fileName),
            FileHash = null,
            ProcessStatus = ProcessStatusPending,
            CreatorUserId = creatorUserId,
            CreatorUserName = creatorUserName,
            TenantId = tenantId,
            CreateTime = createTime,
            DeleteMark = false,
        };
    }

    public static bool IsExtractedCacheHit(int processStatus, string? extractedText)
        => processStatus == ProcessStatusDone && !string.IsNullOrWhiteSpace(extractedText);

    public static string TruncateProcessError(string? message, int maxLength = ProcessErrorMaxLength)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;
        return message.Length > maxLength ? message[..maxLength] : message;
    }

    public static string JoinExtractedTexts(IEnumerable<string> texts)
        => string.Join("\n\n", texts);

    /// <summary>
    /// Prefer in-memory download cache; returns false when caller must re-download.
    /// </summary>
    public static bool TryTakeCachedBytes(
        IReadOnlyDictionary<string, byte[]> downloadedBytes,
        string fileUrl,
        out byte[] bytes)
        => downloadedBytes.TryGetValue(fileUrl, out bytes!);

    /// <summary>Store bytes under FileUrl so vision step can avoid a second HTTP GET.</summary>
    public static void RememberDownloadedBytes(
        IDictionary<string, byte[]> downloadedBytes,
        string fileUrl,
        byte[] bytes)
        => downloadedBytes[fileUrl] = bytes;

    /// <summary>Lower-invariant hex SHA-256 (legacy ComputeSha256).</summary>
    public static string ComputeSha256Hex(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool ShouldAppendExtractedText(string? extracted)
        => !string.IsNullOrWhiteSpace(extracted);

    public static AttachmentFile ToProcessorFile(string fileName, byte[] content)
        => new() { FileName = fileName, Content = content };

    /// <summary>
    /// Strip leading "Bearer " (ordinal ignore-case). Null/whitespace → null (skip Authorization header).
    /// </summary>
    public static string? StripBearerPrefix(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization))
            return null;
        return authorization.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    public static AttachmentRunningUpdate BuildRunningUpdate(DateTime now)
        => new(ProcessStatusRunning, now);

    public static AttachmentDoneUpdate BuildDoneUpdate(string? extractedText, string fileHash, DateTime now)
        => new(ProcessStatusDone, extractedText, fileHash, now);

    public static AttachmentFailedUpdate BuildFailedUpdate(string? exceptionMessage, DateTime now)
        => new(ProcessStatusFailed, TruncateProcessError(exceptionMessage), now);

    /// <summary>Cache-hit path: append extracted text when status/text qualify.</summary>
    public static bool TryCollectCacheHitText(
        int processStatus,
        string? extractedText,
        ICollection<string> attachmentTexts)
    {
        if (!IsExtractedCacheHit(processStatus, extractedText))
            return false;
        attachmentTexts.Add(extractedText!);
        return true;
    }

    /// <summary>After successful parse: append when non-whitespace.</summary>
    public static bool CollectExtractedIfPresent(string? extracted, ICollection<string> attachmentTexts)
    {
        if (!ShouldAppendExtractedText(extracted))
            return false;
        attachmentTexts.Add(extracted!);
        return true;
    }
}
