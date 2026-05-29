using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Systems.Entitys.System;

/// <summary>
/// 常用字段
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_COMMON_FIELDS")]
public class ComFieldsEntity : CLDSEntityBase
{
    /// <summary>
    /// 字段注释.
    /// </summary>
    [SugarColumn(ColumnName = "F_FIELD_NAME")]
    public string FieldName { get; set; }

    /// <summary>
    /// 列名.
    /// </summary>
    [SugarColumn(ColumnName = "F_FIELD")]
    public string Field { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_TYPE")]
    public string DataType { get; set; }

    /// <summary>
    /// 长度.
    /// </summary>
    [SugarColumn(ColumnName = "F_DATA_LENGTH")]
    public string DataLength { get; set; }

    /// <summary>
    /// 允许空.
    /// </summary>
    [SugarColumn(ColumnName = "F_ALLOW_NULL")]
    public int? AllowNull { get; set; }

    /// <summary>
    /// 描述说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string Description { get; set; }
}