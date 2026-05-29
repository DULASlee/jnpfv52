using JNPF.DependencyInjection;

namespace JNPF.Engine.Entity.Model;

/// <summary>
/// 数据过滤条件.
/// </summary>
[SuppressSniffer]
public class DataFilteringModel
{
    /// <summary>
    /// 分组逻辑： 并且、或者.
    /// </summary>
    public string matchLogic { get; set; }

    /// <summary>
    /// 分组条件.
    /// </summary>
    public List<DataFilteringConditionListModel> conditionList { get; set; }
}

/// <summary>
/// 数据过滤条件集合.
/// </summary>
[SuppressSniffer]
public class DataFilteringConditionListModel
{
    /// <summary>
    /// 条件.
    /// </summary>
    public string logic { get; set; }

    /// <summary>
    /// 分组.
    /// </summary>
    public List<DataFilteringConditionGroupsListModel> groups { get; set; }
}

/// <summary>
/// 数据过滤条件分组集合.
/// </summary>
[SuppressSniffer]
public class DataFilteringConditionGroupsListModel : IndexEachConfigBase
{
    /// <summary>
    /// 字段过滤值.
    /// </summary>
    public object fieldValue { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 条件符号标识.
    /// </summary>
    public string symbol { get; set; }
}
