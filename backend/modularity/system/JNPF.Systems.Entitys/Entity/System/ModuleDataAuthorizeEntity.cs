using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 数据权限
/// 版 本：V3.0.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2017.09.20.
/// </summary>
[SugarTable("BASE_MODULE_AUTHORIZE")]
public class ModuleDataAuthorizeEntity : SystemCLDSEntityBase
{
    /// <summary>
    /// 字段名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 字段编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 字段类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_TYPE")]
    public string Type { get; set; }

    /// <summary>
    /// 条件符号.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONDITION_SYMBOL")]
    public string ConditionSymbol { get; set; }

    /// <summary>
    /// 条件内容.
    /// </summary>
    [SugarColumn(ColumnName = "F_CONDITION_TEXT")]
    public string ConditionText { get; set; }

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

    /// <summary>
    /// 绑定表格Id.
    /// </summary>
    [SugarColumn(ColumnName = "F_BIND_TABLE")]
    public string BindTable { get; set; }

    /// <summary>
    /// 时间格式.
    /// </summary>
    [SugarColumn(ColumnName = "F_FORMAT")]
    public string Format { get; set; }
}