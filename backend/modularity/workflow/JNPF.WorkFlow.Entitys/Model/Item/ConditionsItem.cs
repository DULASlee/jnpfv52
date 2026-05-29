using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model.Item;

[SuppressSniffer]
public class ConditionsItem
{
    /// <summary>
    /// 字段名称.
    /// </summary>
    public string? fieldName { get; set; }

    /// <summary>
    /// 比较名称.
    /// </summary>
    public string? symbolName { get; set; }

    /// <summary>
    /// 字段值.
    /// </summary>
    public dynamic fieldValue { get; set; } = string.Empty;

    /// <summary>
    /// 逻辑名称.
    /// </summary>
    public string? logicName { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string? field { get; set; }

    /// <summary>
    /// 逻辑符号.
    /// </summary>
    public string? logic { get; set; }

    /// <summary>
    /// 比较符号.
    /// </summary>
    public string? symbol { get; set; }

    /// <summary>
    /// 控件类型.
    /// </summary>
    public string? jnpfKey { get; set; }

    /// <summary>
    /// 控件类型.
    /// </summary>
    public string? fieldValueJnpfKey { get; set; }

    /// <summary>
    /// 条件类型 1：字段 3:聚合函数匹配.
    /// </summary>
    public int? fieldType { get; set; } = 1;

    /// <summary>
    /// 1.字段 2.自定义.
    /// </summary>
    public int? fieldValueType { get; set; } = 2;
}
