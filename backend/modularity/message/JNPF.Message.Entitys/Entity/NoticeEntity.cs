using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys.Entity;

/// <summary>
/// 公告
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_NOTICE")]
public class NoticeEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 标题.
    /// </summary>
    [SugarColumn(ColumnName = "F_TITLE")]
    public string Title { get; set; }

    /// <summary>
    /// 正文.
    /// </summary>
    [SugarColumn(ColumnName = "F_BODY_TEXT")]
    public string BodyText { get; set; }

    /// <summary>
    /// 收件用户.
    /// </summary>
    [SugarColumn(ColumnName = "F_TO_USER_IDS")]
    public string ToUserIds { get; set; }

    /// <summary>
    /// 封面图片.
    /// </summary>
    [SugarColumn(ColumnName = "F_COVER_IMAGE")]
    public string CoverImage { get; set; }

    /// <summary>
    /// 附件.
    /// </summary>
    [SugarColumn(ColumnName = "F_FILES")]
    public string Files { get; set; }

    /// <summary>
    /// 过期时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_EXPIRATION_TIME")]
    public DateTime? ExpirationTime { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string Category { get; set; }

    /// <summary>
    /// 提醒类型 1-站内信 2-自定义 3-不通知.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 发送配置.
    /// </summary>
    [SugarColumn(ColumnName = "F_SEND_CONFIG_ID")]
    public string SendConfigId { get; set; }

    /// <summary>
    /// 描述或说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}
