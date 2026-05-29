using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 系统功能按钮.
/// </summary>
[SugarTable("BASE_MODULE_COLUMN")]
public class ModuleColumnEntity : SystemCLDSEntityBase
{
    /// <summary>
    /// 列表上级.
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_ID")]
    public string ParentId { get; set; }

    /// <summary>
    /// 列表名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 列表编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 绑定表格Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_BIND_TABLE")]
    public string BindTable { get; set; }

    /// <summary>
    /// 绑定表格描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_BIND_TABLE_NAME")]
    public string BindTableName { get; set; }

    /// <summary>
    /// 扩展属性.
    /// </summary>
    [SugarColumn(ColumnName = "F_PROPERTY_JSON")]
    public string PropertyJson { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }

    /// <summary>
    /// 功能主键.
    /// </summary>
    [SugarColumn(ColumnName = "F_MODULE_ID")]
    public string ModuleId { get; set; }

    /// <summary>
    /// 规则(0:主表，1：副表 2:子表).
    /// </summary>
    [SugarColumn(ColumnName = "F_FIELD_RULE")]
    public int? FieldRule { get; set; }

    /// <summary>
    /// 子表规则key.
    /// </summary>
    [SugarColumn(ColumnName = "F_CHILD_TABLE_KEY")]
    public string ChildTableKey { get; set; }
}