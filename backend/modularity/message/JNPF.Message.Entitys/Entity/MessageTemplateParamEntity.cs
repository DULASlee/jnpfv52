using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys.Entity;

/// <summary>
/// 消息模板参数
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_MSG_TEMPLATE_PARAM")]
public class MessageTemplateParamEntity : CLDSEntityBase
{
    /// <summary>
    /// 参数名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FIELD_NAME")]
    public string? FieldName { get; set; }

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
}
