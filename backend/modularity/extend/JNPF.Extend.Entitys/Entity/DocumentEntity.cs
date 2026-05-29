using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 知识文档
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("EXT_DOCUMENT")]
public class DocumentEntity : CLDSEntityBase
{
    /// <summary>
    /// 文档父级.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string? ParentId { get; set; }

    /// <summary>
    /// 文档分类:【0-文件夹、1-文件】.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 文件名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 文件路径.
    /// </summary>
    [SugarColumn(ColumnName = "F_FILE_PATH")]
    public string? FilePath { get; set; }

    /// <summary>
    /// 文件大小.
    /// </summary>
    [SugarColumn(ColumnName = "F_FILE_SIZE")]
    public string? FileSize { get; set; }

    /// <summary>
    /// 文件后缀.
    /// </summary>
    [SugarColumn(ColumnName = "F_FILE_EXTENSION")]
    public string? FileExtension { get; set; }

    /// <summary>
    /// 阅读数量.
    /// </summary>
    [SugarColumn(ColumnName = "F_READ_COUNT")]
    public int? ReadCount { get; set; }

    /// <summary>
    /// 是否共享.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_SHARE")]
    public int? IsShare { get; set; }

    /// <summary>
    /// 共享时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_SHARE_TIME")]
    public DateTime? ShareTime { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }

    /// <summary>
    /// 下载地址.
    /// </summary>
    [SugarColumn(ColumnName = "F_UPLOAD_URL")]
    public string? UploadUrl { get; set; }
}
