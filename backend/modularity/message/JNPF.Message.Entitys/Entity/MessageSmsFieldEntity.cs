using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys.Entity;

/// <summary>
/// 短信变量
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_MSG_SMS_FIELD")]
public class MessageSmsFieldEntity : CLDSEntityBase
{
    /// <summary>
    /// 参数id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FIELD_ID")]
    public string? FieldId { get; set; }

    /// <summary>
    /// 短信变量.
    /// </summary>
    [SugarColumn(ColumnName = "F_SMS_FIELD")]
    public string? SmsField { get; set; }

    /// <summary>
    /// 模板id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE_ID")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// 参数.
    /// </summary>
    [SugarColumn(ColumnName = "F_FIELD")]
    public string? Field { get; set; }

    /// <summary>
    /// 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_TITLE")]
    public string? IsTitle { get; set; }
}
