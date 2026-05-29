using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// 系统集成
/// 版 本：v3.5.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2023-9-3.
/// </summary>
[SugarTable("BASE_INTEGRATE", TableDescription = "系统集成")]
public class IntegrateEntity : CLDSEntityBase
{
    /// <summary>
    /// 集成名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 集成编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 集成模板.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEMPLATE_JSON")]
    public string TemplateJson { get; set; }

    /// <summary>
    /// 触发事件(1.新增 2.修改 3.删除).
    /// </summary>
    [SugarColumn(ColumnName = "F_TRIGGER_TYPE")]
    public int TriggerType { get; set; }

    /// <summary>
    /// 结果.
    /// </summary>
    [SugarColumn(ColumnName = "F_RESULTTYPE")]
    public string ResultType { get; set; }

    /// <summary>
    /// 触发类型(1-事件，2-定时,3-WebHook ).
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public int Type { get; set; }

    /// <summary>
    /// 表单id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_ID")]
    public string FormId { get; set; }

    /// <summary>
    /// 描述或说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}