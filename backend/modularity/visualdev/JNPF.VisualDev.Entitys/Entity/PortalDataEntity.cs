using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.VisualDev.Entitys;

/// <summary>
/// 门户子表.
/// </summary>
[SugarTable("BASE_PORTAL_DATA")]
public class PortalDataEntity : CLDEntityBase
{
    /// <summary>
    /// 门户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_PORTAL_ID")]
    public string PortalId { get; set; }

    /// <summary>
    /// 系统id.
    /// </summary>
    [SugarColumn(ColumnName = "F_SYSTEM_ID")]
    public string SystemId { get; set; }

    /// <summary>
    /// 表单配置JSON.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_DATA")]
    public string? FormData { get; set; }

    /// <summary>
    /// web:网页端 app:手机端.
    /// </summary>
    [SugarColumn(ColumnName = "F_PLATFORM")]
    public string Platform { get; set; }

    /// <summary>
    /// 类型（model：模型、release：发布、custom：自定义）.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public string Type { get; set; }
}
