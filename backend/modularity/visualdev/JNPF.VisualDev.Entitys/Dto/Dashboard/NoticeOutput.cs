namespace JNPF.VisualDev.Entitys.Dto.Dashboard;

/// <summary>
/// 通知公告输出.
/// </summary>
public class NoticeOutput
{
    /// <summary>
    /// ID.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 图片.
    /// </summary>
    public string coverImage { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 摘要.
    /// </summary>
    public string excerpt { get; set; }

    /// <summary>
    /// 发布时间.
    /// </summary>
    public DateTime? releaseTime { get; set; }

    /// <summary>
    /// 发布人.
    /// </summary>
    public string releaseUser { get; set; }
}
