namespace JNPF.Extras.Thirdparty.Sms;

/// <summary>
/// 短信参数信息.
/// </summary>
public class SmsParameterInfo
{
    /// <summary>
    /// 第三方id.
    /// </summary>
    public string keyId { get; set; }

    /// <summary>
    /// 第三方秘钥.
    /// </summary>
    public string keySecret { get; set; }

    /// <summary>
    /// 签名.
    /// </summary>
    public string signName { get; set; }

    /// <summary>
    /// 访问域名.
    /// </summary>
    public string domain { get; set; }

    /// <summary>
    /// 短信版本.
    /// </summary>
    public string version { get; set; }

    /// <summary>
    /// 手机号(阿里云).
    /// </summary>
    public string mobileAli { get; set; }

    /// <summary>
    /// 手机号(腾讯云,国内+86).
    /// </summary>
    public string[] mobileTx { get; set; }

    /// <summary>
    /// 模板id.
    /// </summary>
    public string templateId { get; set; }

    /// <summary>
    /// 模板参数(阿里云).
    /// </summary>
    public string templateParamAli { get; set; }

    /// <summary>
    /// 模板参数(腾讯云).
    /// </summary>
    public string[] templateParamTx { get; set; }

    /// <summary>
    /// 地域(腾讯云)
    /// 华北地区:ap-beijing,华南地区:ap-guangzhou,华东地区:ap-nanjing.
    /// </summary>
    public string region { get; set; }

    /// <summary>
    /// 应用id(腾讯云).
    /// </summary>
    public string appId { get; set; }
}