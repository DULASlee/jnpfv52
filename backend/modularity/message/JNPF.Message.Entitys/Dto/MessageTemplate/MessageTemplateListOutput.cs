using JNPF.Message.Entitys.Model.MessageTemplate;

namespace JNPF.Message.Entitys.Dto.MessageTemplate;

public class MessageTemplateListOutput
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
    public string? messageType { get; set; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string? title { get; set; }

    /// <summary>
    /// 内容.
    /// </summary>
    public string? content { get; set; }

    /// <summary>
    /// 模板编号.
    /// </summary>
    public string? templateCode { get; set; }

    /// <summary>
    /// 跳转方式.
    /// </summary>
    public string? wxSkip { get; set; }

    /// <summary>
    /// 小程序id.
    /// </summary>
    public string? xcxAppId { get; set; }

    /// <summary>
    /// 排序.
    /// </summary>
    public long? sortCode { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string? description { get; set; }

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
    /// 短信变量.
    /// </summary>
    public List<SmsFieldModel> smsFieldList { get; set; } = new List<SmsFieldModel>();

    /// <summary>
    /// 模板参数.
    /// </summary>
    public List<TemplateParamModel> templateParamList { get; set; } = new List<TemplateParamModel>();
}
