using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys.Entity;

/// <summary>
/// 消息发送模板配置
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_MSG_SEND_TEMPLATE")]
public class MessageSendTemplateEntity : CLDSEntityBase
{
    /// <summary>
    /// 消息发送配置id.
    /// </summary>
    [SugarColumn(ColumnName = "F_SEND_CONFIG_ID")]
    public string? SendConfigId { get; set; }

    /// <summary>
    /// 消息模板id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE_ID")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// 消息类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_MESSAGE_TYPE")]
    public string? MessageType { get; set; }

    /// <summary>
    /// 账号配置id.
    /// </summary>
    [SugarColumn(ColumnName = "F_ACCOUNT_CONFIG_ID")]
    public string? AccountConfigId { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }
}
