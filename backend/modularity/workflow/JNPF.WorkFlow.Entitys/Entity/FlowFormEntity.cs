using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程表单.
/// </summary>
[SugarTable("FLOW_FORM")]
public class FlowFormEntity : SystemCLDSEntityBase
{
    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string? EnCode { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string? Category { get; set; }

    /// <summary>
    /// Web地址.
    /// </summary>
    [SugarColumn(ColumnName = "F_URL_ADDRESS")]
    public string? UrlAddress { get; set; }

    /// <summary>
    /// APP地址.
    /// </summary>
    [SugarColumn(ColumnName = "F_APP_URL_ADDRESS")]
    public string? AppUrlAddress { get; set; }

    /// <summary>
    /// 表单json.
    /// </summary>
    [SugarColumn(ColumnName = "F_PROPERTY_JSON")]
    public string? PropertyJson { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }

    /// <summary>
    /// 流程类型（0：发起流程，1：功能流程）.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_TYPE")]
    public int? FlowType { get; set; }

    /// <summary>
    /// 表单类型（1：系统表单 2：自定义表单）.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORM_TYPE")]
    public int? FormType { get; set; }

    /// <summary>
    /// 关联表单.
    /// </summary>
    [SugarColumn(ColumnName = "F_TABLE_JSON")]
    public string? TableJson { get; set; }

    /// <summary>
    /// 数据源id.
    /// </summary>
    [SugarColumn(ColumnName = "F_DB_LINK_ID")]
    public string? DbLinkId { get; set; }

    /// <summary>
    /// 接口路径.
    /// </summary>
    [SugarColumn(ColumnName = "F_INTERFACE_URL")]
    public string? InterfaceUrl { get; set; }

    /// <summary>
    /// 表单json草稿.
    /// </summary>
    [SugarColumn(ColumnName = "F_DRAFT_JSON")]
    public string? DraftJson { get; set; }

    /// <summary>
    /// 流程id.
    /// </summary>
    [SugarColumn(ColumnName = "F_FLOW_ID")]
    public string? FlowId { get; set; }

    /// <summary>
    /// 状态(0-发布，1-已发布 ，2-已修改).
    /// </summary>
    [SugarColumn(ColumnName = "F_STATE")]
    public int State { get; set; } = 0;
}
