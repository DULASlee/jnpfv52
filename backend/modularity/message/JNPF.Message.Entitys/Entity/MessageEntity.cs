using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys;

/// <summary>
/// 消息实例
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_MESSAGE")]
public class MessageEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 类别：1-通知公告，2-系统消息、3-私信消息.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int? Type { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    [SugarColumn(ColumnName = "F_TITLE")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 流程跳转类型 1:审批 2:委托.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_TYPE")]
    public int? FlowType { get; set; }

    /// <summary>
    /// 用户主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_ID")]
    public string UserId { get; set; }

    /// <summary>
    /// 是否阅读.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_READ")]
    public int? IsRead { get; set; }

    /// <summary>
    /// 阅读时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_READ_TIME")]
    public DateTime? ReadTime { get; set; }

    /// <summary>
    /// 阅读次数.
    /// </summary>
    [SugarColumn(ColumnName = "F_READ_COUNT")]
    public int? ReadCount { get; set; }

    /// <summary>
    /// 正文.
    /// </summary>
    [SugarColumn(ColumnName = "F_BODY_TEXT")]
    public string BodyText { get; set; }
}