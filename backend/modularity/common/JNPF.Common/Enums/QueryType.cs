using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.Common.Enums;

/// <summary>
/// 查询类型的枚举.
/// </summary>
[SuppressSniffer]
public enum QueryType
{
    /// <summary>
    /// 等于.
    /// </summary>
    [Description("等于")]
    Equal,

    /// <summary>
    /// 模糊.
    /// </summary>
    [Description("模糊")]
    Contains,

    /// <summary>
    /// 大于.
    /// </summary>
    [Description("大于")]
    GreaterThan,

    /// <summary>
    /// 小于.
    /// </summary>
    [Description("小于")]
    LessThan,

    /// <summary>
    /// 不等于.
    /// </summary>
    [Description("不等于")]
    NotEqual,

    /// <summary>
    /// 大于等于.
    /// </summary>
    [Description("大于等于")]
    GreaterThanOrEqual,

    /// <summary>
    /// 小于等于.
    /// </summary>
    [Description("小于等于")]
    LessThanOrEqual,

    /// <summary>
    /// 包含.
    /// </summary>
    [Description("包含")]
    In,

    /// <summary>
    /// 不包含.
    /// </summary>
    [Description("不包含")]
    NotIn,

    /// <summary>
    /// 包含.
    /// </summary>
    [Description("包含")]
    Included,

    /// <summary>
    /// 不包含.
    /// </summary>
    [Description("不包含")]
    NotIncluded,

    /// <summary>
    /// 介于.
    /// </summary>
    [Description("介于")]
    Between,

    /// <summary>
    /// 为空.
    /// </summary>
    [Description("为空")]
    Null,

    /// <summary>
    /// 不为空.
    /// </summary>
    [Description("不为空")]
    NotNull,
}