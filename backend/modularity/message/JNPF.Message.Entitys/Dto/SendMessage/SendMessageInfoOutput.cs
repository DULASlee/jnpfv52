using JNPF.Message.Entitys.Model.MessageTemplate;

namespace JNPF.Message.Entitys.Dto.SendMessage;

public class SendMessageInfoOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string enCode { get; set; }

    /// <summary>
    /// 模板类型.
    /// </summary>
    public string templateType { get; set; }

    /// <summary>
    /// 消息来源.
    /// </summary>
    public string messageSource { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public int sortCode { get; set; }

    /// <summary>
    /// 状态.
    /// </summary>
    public string enabledMark { get; set; }

    /// <summary>
    /// 备注.
    /// </summary>
    public string description { get; set; }

    /// <summary>
    /// 模板.
    /// </summary>
    public List<SendTemplateModel> sendConfigTemplateList { get; set; }
}
