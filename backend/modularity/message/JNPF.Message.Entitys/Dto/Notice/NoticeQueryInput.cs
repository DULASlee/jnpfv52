using JNPF.Common.Filter;

namespace JNPF.Message.Entitys.Dto.Notice;

public class NoticeQueryInput : PageInputBase
{
    /// <summary>
    /// 状态(0-存草稿，1-已发布，2-已过期).
    /// </summary>
    public List<string> enabledMark { get; set; } = new List<string>();

    /// <summary>
    /// 类型.
    /// </summary>
    public List<string> type { get; set; } = new List<string>();

    /// <summary>
    /// 发布人.
    /// </summary>
    public List<string> releaseUser { get; set; } = new List<string>();

    /// <summary>
    /// 发布时间.
    /// </summary>
    public List<DateTime> releaseTime { get; set; } = new List<DateTime>();

    /// <summary>
    /// 失效时间.
    /// </summary>
    public List<DateTime> expirationTime { get; set; } = new List<DateTime>();

    /// <summary>
    /// 创建人.
    /// </summary>
    public List<string> creatorUser { get; set; } = new List<string>();

    /// <summary>
    /// 创建时间.
    /// </summary>
    public List<DateTime> creatorTime { get; set; } = new List<DateTime>();
}
