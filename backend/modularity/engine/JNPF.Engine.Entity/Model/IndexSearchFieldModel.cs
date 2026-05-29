using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 列表查询字段模型.
/// </summary>
[SuppressSniffer]
public class IndexSearchFieldModel : IndexEachConfigBase
{
    /// <summary>
    /// Id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 值.
    /// </summary>
    public object? value { get; set; }

    /// <summary>
    /// 查询类型.
    /// </summary>
    public int? searchType { get; set; }

    /// <summary>
    /// 是否关键词搜索.
    /// </summary>
    public bool isKeyword { get; set; }

    /// <summary>
    /// 是否查询当前以及子.
    /// </summary>
    public bool isIncludeSubordinate { get; set; }
}

/// <summary>
/// 数据过滤集合.
/// 数据过滤条件.
/// </summary>
public class RuleFieldModel : IndexEachConfigBase
{
    /// <summary>
    /// 字段过滤值.
    /// </summary>
    public object fieldValue { get; set; }

    /// <summary>
    /// 字段类型.
    /// </summary>
    public string fieldType { get; set; }

    /// <summary>
    /// 字段值类型.
    /// </summary>
    public string fieldValueType { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string fieldLabel { get; set; }

    /// <summary>
    /// 逻辑名.
    /// </summary>
    public string logicName { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 条件符号标识.
    /// </summary>
    public string symbol { get; set; }

    /// <summary>
    /// 逻辑： 并且、或者.
    /// </summary>
    public string logic { get; set; }

}