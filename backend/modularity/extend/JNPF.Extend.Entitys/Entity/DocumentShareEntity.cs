using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Extend.Entitys;

/// <summary>
/// 知识文档共享
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01 .
/// </summary>
[SugarTable("EXT_DOCUMENT_SHARE")]
public class DocumentShareEntity : CLDEntityBase
{
    /// <summary>
    /// 文档主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_DOCUMENT_ID")]
    public string? DocumentId { get; set; }

    /// <summary>
    /// 共享人员.
    /// </summary>
    [SugarColumn(ColumnName = "F_SHARE_USER_ID")]
    public string? ShareUserId { get; set; }

    /// <summary>
    /// 共享时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_SHARE_TIME")]
    public DateTime? ShareTime { get; set; }
}
