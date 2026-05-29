using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Message.Entitys.Entity;

/// <summary>
/// 消息账号配置
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[SugarTable("BASE_MSG_ACCOUNT")]
public class MessageAccountEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 分类.
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string? Category { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string? FullName { get; set; }

    /// <summary>
    /// 编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string? EnCode { get; set; }

    /// <summary>
    /// 发件人昵称.
    /// </summary>
    [SugarColumn(ColumnName = "F_ADDRESSOR_NAME")]
    public string? AddressorName { get; set; }

    /// <summary>
    /// SMTP服务器.
    /// </summary>
    [SugarColumn(ColumnName = "F_SMTP_SERVER")]
    public string? SmtpServer { get; set; }

    /// <summary>
    /// SMTP端口.
    /// </summary>
    [SugarColumn(ColumnName = "F_SMTP_PORT")]
    public int? SmtpPort { get; set; }

    /// <summary>
    /// SSL安全链接.
    /// </summary>
    [SugarColumn(ColumnName = "F_SSL_LINK")]
    public int? SslLink { get; set; }

    /// <summary>
    /// SMTP用户.
    /// </summary>
    [SugarColumn(ColumnName = "F_SMTP_USER")]
    public string? SmtpUser { get; set; }

    /// <summary>
    /// SMTP密码.
    /// </summary>
    [SugarColumn(ColumnName = "F_SMTP_PASSWORD")]
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// 渠道.
    /// </summary>
    [SugarColumn(ColumnName = "F_CHANNEL")]
    public int? Channel { get; set; }

    /// <summary>
    /// 短信签名.
    /// </summary>
    [SugarColumn(ColumnName = "F_SMS_SIGNATURE")]
    public string? SmsSignature { get; set; }

    /// <summary>
    /// 应用ID.
    /// </summary>
    [SugarColumn(ColumnName = "F_APP_ID")]
    public string? AppId { get; set; }

    /// <summary>
    /// 应用Secret.
    /// </summary>
    [SugarColumn(ColumnName = "F_APP_SECRET")]
    public string? AppSecret { get; set; }

    /// <summary>
    /// EndPoint（阿里云）.
    /// </summary>
    [SugarColumn(ColumnName = "F_END_POINT")]
    public string? EndPoint { get; set; }

    /// <summary>
    /// SDK AppID（腾讯云）.
    /// </summary>
    [SugarColumn(ColumnName = "F_SDK_APP_ID")]
    public string? SdkAppId { get; set; }

    /// <summary>
    /// AppKey（腾讯云）.
    /// </summary>
    [SugarColumn(ColumnName = "F_APP_KEY")]
    public string? AppKey { get; set; }

    /// <summary>
    /// 地域域名（腾讯云）.
    /// </summary>
    [SugarColumn(ColumnName = "F_ZONE_NAME")]
    public string? ZoneName { get; set; }

    /// <summary>
    /// 地域参数（腾讯云）.
    /// </summary>
    [SugarColumn(ColumnName = "F_ZONE_PARAM")]
    public string? ZoneParam { get; set; }

    /// <summary>
    /// 企业id.
    /// </summary>
    [SugarColumn(ColumnName = "F_ENTERPRISE_ID")]
    public string? EnterpriseId { get; set; }

    /// <summary>
    /// AgentID.
    /// </summary>
    [SugarColumn(ColumnName = "F_AGENT_ID")]
    public string? AgentId { get; set; }

    /// <summary>
    /// WebHook类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_WEBHOOK_TYPE")]
    public int? WebhookType { get; set; }

    /// <summary>
    /// WebHook地址.
    /// </summary>
    [SugarColumn(ColumnName = "F_WEBHOOK_ADDRESS")]
    public string? WebhookAddress { get; set; }

    /// <summary>
    /// 认证类型.
    /// </summary>
    [SugarColumn(ColumnName = "F_APPROVE_TYPE")]
    public int? ApproveType { get; set; }

    /// <summary>
    /// Bearer令牌.
    /// </summary>
    [SugarColumn(ColumnName = "F_BEARER")]
    public string? Bearer { get; set; }

    /// <summary>
    /// 用户名（基本认证）.
    /// </summary>
    [SugarColumn(ColumnName = "F_USER_NAME")]
    public string? UserName { get; set; }

    /// <summary>
    /// 密码（基本认证）.
    /// </summary>
    [SugarColumn(ColumnName = "F_PASSWORD")]
    public string? Password { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }
}
