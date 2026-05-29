using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.MessageTemplate;

/// <summary>
/// base_message_template输入参数.
/// </summary>
[SuppressSniffer]
public class MessageTemplateListOutput
{
    /// <summary>
    /// 自然主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 分类（数据字典）.
    /// </summary>
    public string category { get; set; }

    /// <summary>
    /// 模板名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 模板编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string title { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    public string content { get; set; }

    /// <summary>
    /// 通知方式.
    /// </summary>
    public string _noticeMethod { get; set; }

    /// <summary>
    /// 通知方式.
    /// </summary>
    public string noticeMethod { get { return _noticeMethod.TrimEnd(','); } }

    /// <summary>
    /// 有效标志.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 消息内容.
    /// </summary>
    public string templateJson { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string creatorUser { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }
}