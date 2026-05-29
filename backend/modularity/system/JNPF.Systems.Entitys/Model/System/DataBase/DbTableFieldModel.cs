using JNPF.Common.Extension;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.DataBase;

/// <summary>
/// 数据表字段模型.
/// </summary>
[SuppressSniffer]
public class DbTableFieldModel
{
    /// <summary>
    /// 字段名.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 小写字段名称.
    /// </summary>
    public string LowerTableName => string.IsNullOrWhiteSpace(field) ? null : field.ReplaceRegex("^f_", string.Empty).ParseToPascalCase().ToLowerCase();

    /// <summary>
    /// 字段说明.
    /// </summary>
    public string fieldName { get; set; }

    /// <summary>
    /// 数据类型.
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 数据长度.
    /// </summary>
    public string dataLength { get; set; } = "0";

    /// <summary>
    /// 数据精度.
    /// </summary>
    public int? decimalDigits { get; set; }

    /// <summary>
    /// 自增.
    /// </summary>
    public bool identity { get; set; }

    /// <summary>
    /// 主键.
    /// </summary>
    public bool primaryKey { get; set; }

    /// <summary>
    /// 允许null值.
    /// </summary>
    public int? allowNull { get; set; }

    /// <summary>
    /// 默认值.
    /// </summary>
    public string defaults { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string description
    {
        get
        {
            return this.field + "（" + this.fieldName + "）";
        }
    }
}