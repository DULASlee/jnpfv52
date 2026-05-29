namespace JNPF.Message.Entitys.Dto.MessageAccount;

public class MessageAccountInfoOutput
{
    /// <summary>
    /// 主键.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 配置类型.
    /// </summary>
    public string? type { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    public string? fullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    public string? enCode { get; set; }

    /// <summary>
    /// 发件人昵称.
    /// </summary>
    public string? addressorName { get; set; }

    /// <summary>
    /// SMTP服务器.
    /// </summary>
    public string? smtpServer { get; set; }

    /// <summary>
    /// SMTP端口.
    /// </summary>
    public int? smtpPort { get; set; }

    /// <summary>
    /// SSL安全链接.
    /// </summary>
    public int? sslLink { get; set; }

    /// <summary>
    /// SMTP用户.
    /// </summary>
    public string? smtpUser { get; set; }

    /// <summary>
    /// SMTP密码.
    /// </summary>
    public string? smtpPassword { get; set; }

    /// <summary>
    /// 渠道.
    /// </summary>
    public int? channel { get; set; }

    /// <summary>
    /// 短信签名.
    /// </summary>
    public string? smsSignature { get; set; }

    /// <summary>
    /// 应用ID.
    /// </summary>
    public string? appId { get; set; }

    /// <summary>
    /// 应用Secret.
    /// </summary>
    public string? appSecret { get; set; }

    /// <summary>
    /// EndPoint（阿里云）.
    /// </summary>
    public string? endPoint { get; set; }

    /// <summary>
    /// SDK AppID（腾讯云）.
    /// </summary>
    public string? sdkAppId { get; set; }

    /// <summary>
    /// AppKey（腾讯云）.
    /// </summary>
    public string? appKey { get; set; }

    /// <summary>
    /// 地域域名（腾讯云）.
    /// </summary>
    public string? zoneName { get; set; }

    /// <summary>
    /// 地域参数（腾讯云）.
    /// </summary>
    public string? zoneParam { get; set; }

    /// <summary>
    /// 企业id.
    /// </summary>
    public string? enterpriseId { get; set; }

    /// <summary>
    /// AgentID.
    /// </summary>
    public string? agentId { get; set; }

    /// <summary>
    /// WebHook类型.
    /// </summary>
    public string? webhookType { get; set; }

    /// <summary>
    /// WebHook地址.
    /// </summary>
    public string? webhookAddress { get; set; }

    /// <summary>
    /// 认证类型.
    /// </summary>
    public string? approveType { get; set; }

    /// <summary>
    /// Bearer令牌.
    /// </summary>
    public string? bearer { get; set; }

    /// <summary>
    /// 用户名（基本认证）.
    /// </summary>
    public string? userName { get; set; }

    /// <summary>
    /// 密码（基本认证）.
    /// </summary>
    public string? password { get; set; }

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
}
