using JNPF.DependencyInjection;

namespace JNPF.Extend.Entitys.Dto.Document;

/// <summary>
/// 获取知识管理列表（全部文档）.
/// </summary>
[SuppressSniffer]
public class DocumentListOutput
{
    /// <summary>
    /// 创建日期.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 是否分享.
    /// </summary>
    public int? isShare { get; set; }

    /// <summary>
    /// 类型(0-文件夹，1-文件).
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 大小.
    /// </summary>
    public string? fileSize { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 主键id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 后缀名.
    /// </summary>
    public string? fileExtension { get; set; }

    /// <summary>
    /// 父级Id.
    /// </summary>
    public string? parentId { get; set; }

    /// <summary>
    /// 文档下载地址.
    /// </summary>
    public string? uploaderUrl { get; set; }

    /// <summary>
    /// 文件路径.
    /// </summary>
    public string? filePath { get; set; }

    /// <summary>
    /// 是否支持预览.
    /// </summary>
    public string? isPreview { get; set; }
}
