using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.DataInterFace;

/// <summary>
/// 数据接口列表.
/// </summary>
[SuppressSniffer]
public class DataInterfaceListOutput
{
    /// <summary>
    /// 主键id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 接口名.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 排序号.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 数据类型(1-SQL数据，2-静态数据，3-Api数据).
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 租户id.
    /// </summary>
    public string tenantId { get; set; }

    /// <summary>
    /// 参数json.
    /// </summary>
    public string parameterJson { get; set; }

    /// <summary>
    /// 字段json.
    /// </summary>
    public string fieldJson { get; set; }

    /// <summary>
    /// 是否鉴权.
    /// </summary>
    public int? isPostPosition { get; set; }

    /// <summary>
    /// 是否分页.
    /// </summary>
    public int? hasPage { get; set; }

    public string configJson { get; set; }

    /// <summary>
    /// 配置信息.
    /// </summary>
    public string dataConfigJson { get; set; }
}