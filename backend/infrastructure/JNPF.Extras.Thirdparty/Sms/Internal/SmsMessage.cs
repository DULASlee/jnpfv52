namespace JNPF.Extras.Thirdparty.Sms;

/// <summary>
/// 短信消息.
/// </summary>
public class SmsMessage
{
    /// <summary>
    /// 请求标识.
    /// </summary>
    public string RequestId { get; set; }

    /// <summary>
    /// 验证码.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// 消息.
    /// </summary>
    public string Message { get; set; }
}