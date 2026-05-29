using SqlSugar;

namespace JNPF.Common.Models;

/// <summary>
/// 高级查询模型.
/// </summary>
public class SuperQueryModel
{
    /// <summary>
    /// 匹配逻辑.
    /// </summary>
    public string matchLogic { get; set; }

    /// <summary>
    /// 分组条件JSON列.
    /// </summary>
    public List<ConditionGroup> conditionList { get; set; }
}

/// <summary>
/// 条件分组.
/// </summary>
public class ConditionGroup
{
    public string logic { get; set; }

    public List<Conditionjson> groups { get; set; }
}

/// <summary>
/// 条件JSON.
/// </summary>
public class Conditionjson
{
    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 字段值.
    /// </summary>
    public object fieldValue { get; set; }

    /// <summary>
    /// 象征.
    /// </summary>
    public string symbol { get; set; }

    /// <summary>
    /// jnpfKey.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 多选.
    /// </summary>
    public bool multiple { get; set; }

    /// <summary>
    /// format.
    /// </summary>
    public string format { get; set; }

    /// <summary>
    /// 配置.
    /// </summary>
    public ConditionJsonConfig __config__ { get; set; }
}

public class ConditionJsonConfig
{
    /// <summary>
    /// jnpf识别符.
    /// </summary>
    public string jnpfKey { get; set; }

}

/// <summary>
/// 转换高级查询.
/// </summary>
public class ConvertSuper
{
    /// <summary>
    /// where类型.
    /// </summary>
    public WhereType whereType { get; set; }

    /// <summary>
    /// 转换高级查询.
    /// </summary>
    public List<ConvertSuperGroup> convertGroups { get; set; }
}

/// <summary>
/// 转换高级查询 分组.
/// </summary>
public class ConvertSuperGroup
{
    /// <summary>
    /// where类型.
    /// </summary>
    public WhereType whereType { get; set; }

    /// <summary>
    /// 转换高级查询.
    /// </summary>
    public List<ConvertSuperQuery> convertSuperQuery { get; set; }
}

/// <summary>
/// 转换高级查询.
/// </summary>
public class ConvertSuperQuery
{
    /// <summary>
    /// where类型.
    /// </summary>
    public WhereType whereType { get; set; }

    /// <summary>
    /// jnpfKey.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 字段值.
    /// </summary>
    public string fieldValue { get; set; }

    /// <summary>
    /// 条件类型.
    /// </summary>
    public ConditionalType conditionalType { get; set; }

    /// <summary>
    /// 是否主条件.
    /// </summary>
    public bool mainWhere { get; set; }

    /// <summary>
    /// 象征.
    /// </summary>
    public string symbol { get; set; }

    /// <summary>
    /// 子集查询.
    /// </summary>
    public List<ConvertSuperQuery> childConvertSuperQuery { get; set; }
}