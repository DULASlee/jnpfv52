// 文件：Gates/GatePipelineOptions.cs
// 命名空间：JNPF.InteAssistant.Gates
// 职责：门控配置（支持 IOptionsMonitor 热重载）

namespace JNPF.InteAssistant.Gates;

/// <summary>
/// 门控管道配置
///
/// 配置文件位置：Configurations/GatePipeline.json
/// 支持热重载：改配置文件后不需要重启后端
/// </summary>
public sealed class GatePipelineOptions
{
    /// <summary>配置节名称</summary>
    public const string SectionName = "GatePipeline";

    /// <summary>允许处理的文件扩展名</summary>
    public HashSet<string> AllowedExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".docx", ".doc", ".xlsx", ".xls", ".pdf", ".txt", ".csv", ".md",
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"
    };

    /// <summary>明确禁止的文件扩展名</summary>
    public HashSet<string> BlockedExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".sh", ".ps1", ".cmd",
        ".zip", ".rar", ".7z", ".tar", ".gz",
        ".mp3", ".mp4", ".avi", ".mov", ".wav", ".flv"
    };

    /// <summary>单文件最大大小（字节），默认 20MB</summary>
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>附件总大小限制（字节），默认 50MB</summary>
    public long MaxTotalSizeBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>最大附件数量</summary>
    public int MaxAttachmentCount { get; set; } = 10;

    /// <summary>单文件处理超时</summary>
    public TimeSpan PerFileTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>附件并行处理数</summary>
    public int MaxConcurrentFiles { get; set; } = 3;
}
