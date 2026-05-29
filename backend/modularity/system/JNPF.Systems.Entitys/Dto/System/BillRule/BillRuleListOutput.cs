using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.BillRule;

/// <summary>
/// 单据规则列表输出.
/// </summary>
[SuppressSniffer]
public class BillRuleListOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 业务名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 业务编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 当前流水号.
    /// </summary>
    public string outputNumber { get; set; }

    /// <summary>
    /// 流水位数.
    /// </summary>
    public int? digit { get; set; }

    /// <summary>
    /// 流水起始.
    /// </summary>
    public string startNumber { get; set; }

    /// <summary>
    /// 流水状态.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 排序码.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string? category { get; set; }
}