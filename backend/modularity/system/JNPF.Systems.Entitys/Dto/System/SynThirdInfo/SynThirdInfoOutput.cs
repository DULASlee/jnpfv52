using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SynThirdInfo;

/// <summary>
/// 第三方同步信息输出.
/// </summary>
[SuppressSniffer]
public class SynThirdInfoOutput
{
    /// <summary>
    /// 总数.
    /// </summary>
    public int recordTotal { get; set; }

    /// <summary>
    /// 同步时间.
    /// </summary>
    public DateTime? synDate { get; set; }

    /// <summary>
    /// 失败条数.
    /// </summary>
    public int synFailCount { get; set; }

    /// <summary>
    /// 成功条数.
    /// </summary>
    public int synSuccessCount { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string synType { get; set; }

    /// <summary>
    /// 未同步条数.
    /// </summary>
    public int unSynCount { get; set; }
}