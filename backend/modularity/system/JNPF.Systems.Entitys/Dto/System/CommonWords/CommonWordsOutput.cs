using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.System.CommonWords;

/// <summary>
/// 审批常用语输出.
/// </summary>
[SuppressSniffer]
public class CommonWordsOutput
{
    /// <summary>
    /// 自然主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 常用语.
    /// </summary>
    public string commonWordsText { get; set; }

    /// <summary>
    /// 常用语类型(0:系统,1:个人).
    /// </summary>
    public int commonWordsType { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 有效标志.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }
}
