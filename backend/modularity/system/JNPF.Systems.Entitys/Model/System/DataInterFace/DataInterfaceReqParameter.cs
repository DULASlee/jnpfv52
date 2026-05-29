using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Model.DataInterFace;

/// <summary>
/// 数据接口请求参数.
/// </summary>
[SuppressSniffer]
public class DataInterfaceReqParameter
{
    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 值.
    /// </summary>
    public string defaultValue { get; set; }

    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string dataType { get; set; }

    /// <summary>
    /// 必填.
    /// </summary>
    public string required { get; set; }

    /// <summary>
    /// 注释.
    /// </summary>
    public string fieldName { get; set; }

    /// <summary>
    /// 来源(1-接口参数 2-变量 3-自定义 4-分页参数).
    /// </summary>
    public string source { get; set; }
}