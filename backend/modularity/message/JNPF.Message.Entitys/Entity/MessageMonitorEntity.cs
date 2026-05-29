using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys.Entity;

/// <summary>
/// 消息监控
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_MSG_MONITOR")]
public class MessageMonitorEntity : CLDEntityBase
{
    /// <summary>
    /// 账号id.
    /// </summary>
    [SugarColumn(ColumnName = "F_ACCOUNT_ID")]
    public string? AccountId { get; set; }

    /// <summary>
    /// 账号名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_ACCOUNT_NAME")]
    public string? AccountName { get; set; }

    /// <summary>
    /// 账号编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_ACCOUNT_CODE")]
    public string? AccountCode { get; set; }

    /// <summary>
    /// 消息类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_MESSAGE_TYPE")]
    public string? MessageType { get; set; }

    /// <summary>
    /// 消息来源.
    /// </summary>
    [SugarColumn(ColumnName = "F_MESSAGE_SOURCE")]
    public string? MessageSource { get; set; }

    /// <summary>
    /// 发送时间.
    /// </summary>
    [SugarColumn(ColumnName = "F_SEND_TIME")]
    public DateTime? SendTime { get; set; }

    /// <summary>
    /// 消息模板id.
    /// </summary>
    [SugarColumn(ColumnName = "F_MESSAGE_TEMPLATE_ID")]
    public string? MessageTemplateId { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    [SugarColumn(ColumnName = "F_TITLE")]
    public string? Title { get; set; }

    /// <summary>
    /// 接收人.
    /// </summary>
    [SugarColumn(ColumnName = "F_RECEIVE_USER")]
    public string? ReceiveUser { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONTENT")]
    public string? Content { get; set; }
}
