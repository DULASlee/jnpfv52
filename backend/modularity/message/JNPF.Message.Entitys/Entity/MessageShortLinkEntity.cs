using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys.Entity;

/// <summary>
/// 消息连接
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_MSG_SHORT_LINK")]
public class MessageShortLinkEntity : CLDSEntityBase
{
    /// <summary>
    /// 短链接.
    /// </summary>
    [SugarColumn(ColumnName = "F_SHORT_LINK")]
    public string? ShortLink { get; set; }

    /// <summary>
    /// PC端链接.
    /// </summary>
    [SugarColumn(ColumnName = "F_REAL_PC_LINK")]
    public string? RealPcLink { get; set; }

    /// <summary>
    /// App端链接.
    /// </summary>
    [SugarColumn(ColumnName = "F_REAL_APP_LINK")]
    public string? RealAppLink { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_BODY_TEXT")]
    public string? BodyText { get; set; }

    /// <summary>
    /// 是否点击后失效.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_USED")]
    public int? IsUsed { get; set; }

    /// <summary>
    /// 点击次数.
    /// </summary>
    [SugarColumn(ColumnName = "F_CLICK_NUM")]
    public int? ClickNum { get; set; }

    /// <summary>
    /// 失效次数.
    /// </summary>
    [SugarColumn(ColumnName = "F_UNABLE_NUM")]
    public int? UnableNum { get; set; }

    /// <summary>
    /// 失效时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_UNABLE_TIME")]
    public DateTime? UnableTime { get; set; }

    /// <summary>
    /// 用户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string? UserId { get; set; }
}
