namespace JNPF.Message.Entitys.Dto.Notice;

public class NoticeOutput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 正文内容.
    /// </summary>
    public string bodyText { get; set; }

    /// <summary>
    /// 发送人员.
    /// </summary>
    public string releaseUser { get; set; }

    /// <summary>
    /// 创建人员.
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 发送时间.
    /// </summary>
    public DateTime? releaseTime { get; set; }

    /// <summary>
    /// 收件用户.
    /// </summary>
    public string toUserIds { get; set; }

    /// <summary>
    /// 附件.
    /// </summary>
    public string files { get; set; }

    /// <summary>
    /// 分类.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 图片.
    /// </summary>
    public string coverImage { get; set; }

    /// <summary>
    /// 摘要.
    /// </summary>
    public string excerpt { get; set; }

    /// <summary>
    /// 过期时间.
    /// </summary>
    public DateTime? expirationTime { get; set; }

    /// <summary>
    /// 通知方式.
    /// </summary>
    public int? remindCategory { get; set; }

    /// <summary>
    /// 发送配置id.
    /// </summary>
    public string sendConfigId { get; set; }

    /// <summary>
    /// 发送配置名称.
    /// </summary>
    public string sendConfigName { get; set; }

    /// <summary>
    /// 启用标识.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 提醒类型.
    /// </summary>
    public int? type { get; set; }
}
