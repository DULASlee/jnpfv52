using JNPF.DependencyInjection;

namespace JNPF.Common.Models;

/// <summary>
/// 文件控件模型.
/// </summary>
[SuppressSniffer]
public class FileControlsModel
{
    /// <summary>
    /// 文件名称.
    /// </summary>
    public string? name { get; set; }

    /// <summary>
    /// 文件ID.
    /// </summary>
    public string? fileId { get; set; }

    /// <summary>
    /// 下载地址.
    /// </summary>
    public string? url { get; set; }

    /// <summary>
    /// 文件大小.
    /// </summary>
    public long? fileSize { get; set; }

    /// <summary>
    /// 文件后缀.
    /// </summary>
    public string? fileExtension { get; set; }

    /// <summary>
    /// 文件名称.
    /// </summary>
    public string? fileName { get; set; }

    /// <summary>
    /// 缩略图下载地址.
    /// </summary>
    public string? thumbUrl { get; set; }
}