using JNPF.Message.Entitys.Model.MessageTemplate;

namespace JNPF.Message.Entitys.Dto.SendMessage;

public class SendMessageListOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string? enCode { get; set; }

    /// <summary>
    /// 模板类型.
    /// </summary>
    public string? templateType { get; set; }

    /// <summary>
    /// 消息来源.
    /// </summary>
    public string? messageSource { get; set; }

    /// <summary>
    /// 消息类型.
    /// </summary>
    public List<MessageTypeModel> messageType { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 租户id.
    /// </summary>
    public string? tenantId { get; set; }

    /// <summary>
    /// 创建人.
    /// </summary>
    public string? creatorUser { get; set; }

    /// <summary>
    /// 创建时间.
    /// </summary>
    public DateTime? creatorTime { get; set; }

    /// <summary>
    /// 修改时间.
    /// </summary>
    public DateTime? lastModifyTime { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 消息类型.
    /// </summary>
    public object templateJson { get; set; }
}
