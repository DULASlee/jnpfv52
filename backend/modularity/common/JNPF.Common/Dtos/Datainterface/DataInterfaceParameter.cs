namespace JNPF.Common.Dtos.Datainterface;

/// <summary>
/// 数据接口对外参数.
/// </summary>
public class DataInterfaceParameter
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 参数key值.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 参数value值.
    /// </summary>
    public object defaultValue { get; set; }

    /// <summary>
    /// 参数类型.
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 必填.
    /// </summary>
    public int required { get; set; }

    /// <summary>
    /// 映射字段.
    /// </summary>
    public object relationField { get; set; }

    /// <summary>
    /// 参数说明.
    /// </summary>
    public string fieldName { get; set; }

    /// <summary>
    /// 参数value值来源类型(1-字段 2-自定义 3-为空 4-系统变量).
    /// </summary>
    public int sourceType { get; set; }

    /// <summary>
    /// 是否子表.
    /// </summary>
    public bool isSubTable { get; set; }
}
