namespace JNPF.Common.Dtos.Message;

public class MessageSendParam
{
    /// <summary>
    /// 字段.
    /// </summary>
    public string field { get; set; }

    /// <summary>
    /// 字段说明.
    /// </summary>
    public string fieldName { get; set; }

    /// <summary>
    /// 主键.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 模板编码.
    /// </summary>
    public string templateCode { get; set; }

    /// <summary>
    /// 模板id.
    /// </summary>
    public string templateId { get; set; }

    /// <summary>
    /// 模板名称.
    /// </summary>
    public string templateName { get; set; }

    /// <summary>
    /// 模板类型.
    /// </summary>
    public string templateType { get; set; }

    /// <summary>
    /// 值.
    /// </summary>
    public string value { get; set; }

    /// <summary>
    /// 关联字段.
    /// </summary>
    public string? relationField { get; set; } = string.Empty;

    /// <summary>
    /// 是否字表.
    /// </summary>
    public bool isSubTable { get; set; }
}
