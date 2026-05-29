using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.MessageTemplate;

/// <summary>
/// base_message_template修改输入参数.
/// </summary>
[SuppressSniffer]
public class MessageTemplateCrInput
{
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
    /// 有效标志.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 是否站内信.
    /// </summary>
    public int? isStationLetter { get; set; }

    /// <summary>
    /// 是否邮箱.
    /// </summary>
    public int? isEmail { get; set; }

    /// <summary>
    /// 是否企业微信.
    /// </summary>
    public int? isWecom { get; set; }

    /// <summary>
    /// 是否钉钉.
    /// </summary>
    public int? isDingTalk { get; set; }

    /// <summary>
    /// 是否短信.
    /// </summary>
    public int? isSms { get; set; }

    /// <summary>
    /// 短信模板ID.
    /// </summary>
    public string smsId { get; set; }

    /// <summary>
    /// 模板参数JSON.
    /// </summary>
    public string templateJson { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    public string content { get; set; }
}