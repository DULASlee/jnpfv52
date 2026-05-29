namespace JNPF.Engine.Entity.Model.Integrate;

public class InteAssisInterfaceParameterModel
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 默认值.
    /// </summary>
    public string defaultValue { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 数据类型.
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 是否唯一值.
    /// </summary>
    public int required { get; set; }

    /// <summary>
    /// 字段名称.
    /// </summary>
    public string fieldName { get; set; }

    /// <summary>
    /// 参数.
    /// </summary>
    public string parameter { get; set; }

    /// <summary>
    /// 来源类型
    /// 1-字段,2-自定义.
    /// </summary>
    public int sourceType { get; set; }

    /// <summary>
    /// 关联字段.
    /// </summary>
    public string relationField { get; set; }

    /// <summary>
    /// 是否子表.
    /// </summary>
    public bool isSubTable { get; set; }
}