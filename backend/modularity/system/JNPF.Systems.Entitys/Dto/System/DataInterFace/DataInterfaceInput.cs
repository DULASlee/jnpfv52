using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DataInterFace;

/// <summary>
/// 数据接口创建输入.
/// </summary>
[SuppressSniffer]
public class DataInterfaceInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 接口名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 分类id.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 类型(1-静态数据，2-sql，3-api).
    /// </summary>
    public int? type { get; set; }

    /// <summary>
    /// 动作(3-查询).
    /// </summary>
    public int? action { get; set; }

    /// <summary>
    /// 分页(0-禁用，1-启用).
    /// </summary>
    public int? hasPage { get; set; }

    /// <summary>
    /// 鉴权接口(0-禁用，1-启用).
    /// </summary>
    public int? isPostPosition { get; set; }

    /// <summary>
    /// 异常验证json.
    /// </summary>
    public string dataExceptionJson { get; set; }

    /// <summary>
    /// 数据配置json.
    /// </summary>
    public string dataConfigJson { get; set; }

    /// <summary>
    /// 数据统计json.
    /// </summary>
    public string dataCountJson { get; set; }

    /// <summary>
    /// 数据回显json.
    /// </summary>
    public string dataEchoJson { get; set; }

    /// <summary>
    /// 数据处理json.
    /// </summary>
    public string dataJsJson { get; set; }

    /// <summary>
    /// 参数json.
    /// </summary>
    public string parameterJson { get; set; }

    /// <summary>
    /// 字段json.
    /// </summary>
    public string fieldJson { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 描述.
    /// </summary>
    public string description { get; set; }
}