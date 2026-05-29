namespace JNPF.Engine.Entity.Model.Integrate;

public class InteAssisSuperQueryModel
{
    /// <summary>
    /// 匹配逻辑.
    /// </summary>
    public string matchLogic { get; set; }

    /// <summary>
    /// 分组条件JSON列.
    /// </summary>
    public List<InteAssisConditionGroup> conditionList { get; set; }
}

/// <summary>
/// 条件分组.
/// </summary>
public class InteAssisConditionGroup
{
    public string logic { get; set; }

    public List<InteAssisConditionjson> groups { get; set; }
}

/// <summary>
/// 条件JSON.
/// </summary>
public class InteAssisConditionjson : FieldsModel
{
    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 象征.
    /// </summary>
    public string symbol { get; set; }

    /// <summary>
    /// jnpfKey.
    /// </summary>
    public string jnpfKey { get; set; }

    /// <summary>
    /// 字段值.
    /// </summary>
    public object fieldValue { get; set; }

    /// <summary>
    /// .
    /// </summary>
    public string fieldValueJnpfKey { get; set; }

    /// <summary>
    /// 字段值类型
    /// 1-字段,2-自定义.
    /// </summary>
    public int fieldValueType { get; set; }

    public long cellKey { get; set; }

    public string id { get; set; }

    public string fullName { get; set; }
}