using AlibabaCloud.OpenApiClient.Models;
using JNPF.Common.Security;
using TencentCloud.Common;
using TencentCloud.Common.Profile;
using TencentCloud.Sms.V20210111;
using TencentCloud.Sms.V20210111.Models;

namespace JNPF.Extras.Thirdparty.Sms;

public class SmsUtil
{
    #region 阿里云

    /// <summary>
    /// 发送（阿里云短信）.
    /// </summary>
    /// <param name="smsModel"></param>
    /// <returns></returns>
    public static string SendSmsByAli(SmsParameterInfo smsModel)
    {
        var config = new Config
        {
            // 您的AccessKey ID
            AccessKeyId = smsModel.keyId,

            // 您的AccessKey Secret
            AccessKeySecret = smsModel.keySecret,
        };
        config.Endpoint = smsModel.domain;
        AlibabaCloud.SDK.Dysmsapi20170525.Client client = new AlibabaCloud.SDK.Dysmsapi20170525.Client(config);
        AlibabaCloud.SDK.Dysmsapi20170525.Models.SendSmsRequest sendSmsRequest = new AlibabaCloud.SDK.Dysmsapi20170525.Models.SendSmsRequest();
        sendSmsRequest.PhoneNumbers = smsModel.mobileAli;
        sendSmsRequest.SignName = smsModel.signName;
        sendSmsRequest.TemplateCode = smsModel.templateId;
        sendSmsRequest.TemplateParam = smsModel.templateParamAli;

        AlibabaCloud.SDK.Dysmsapi20170525.Models.SendSmsResponse sendSmsResponse = client.SendSms(sendSmsRequest);
        if (sendSmsResponse.Body.Code.Equals("OK") && sendSmsResponse.Body.Message.Equals("OK"))
        {
            return sendSmsResponse.Body.RequestId;
        }
        else
        {
            throw new Exception(sendSmsResponse.Body.Message);
        }
    }

    /// <summary>
    /// 获取模板配置字段.
    /// </summary>
    /// <param name="smsModel"></param>
    /// <returns></returns>
    public static string GetTemplateByAli(SmsParameterInfo smsModel)
    {
        try
        {
            var config = new Config
            {
                // 您的AccessKey ID
                AccessKeyId = smsModel.keyId,

                // 您的AccessKey Secret
                AccessKeySecret = smsModel.keySecret,
            };
            config.Endpoint = smsModel.domain;
            AlibabaCloud.SDK.Dysmsapi20170525.Client client = new AlibabaCloud.SDK.Dysmsapi20170525.Client(config);
            AlibabaCloud.SDK.Dysmsapi20170525.Models.QuerySmsTemplateRequest querySmsTemplateRequest = new AlibabaCloud.SDK.Dysmsapi20170525.Models.QuerySmsTemplateRequest();
            querySmsTemplateRequest.TemplateCode = smsModel.templateId;
            AlibabaCloud.SDK.Dysmsapi20170525.Models.QuerySmsTemplateResponse querySmsTemplateResponse = client.QuerySmsTemplate(querySmsTemplateRequest);
            if (querySmsTemplateResponse.Body.Code.Equals("OK"))
            {
                return querySmsTemplateResponse.Body.TemplateContent;
            }
            else
            {
                throw new Exception("获取模板失败");
            }
        }
        catch (Exception)
        {

            throw new Exception("获取模板失败");
        }
    }

    #endregion

    #region 腾讯云

    /// <summary>
    /// 腾讯云短信.
    /// </summary>
    /// <param name="smsModel"></param>
    /// <returns></returns>
    public static string SendSmsByTencent(SmsParameterInfo smsModel)
    {
        Credential cred = new Credential
        {
            SecretId = smsModel.keyId,
            SecretKey = smsModel.keySecret
        };

        ClientProfile clientProfile = new ClientProfile();
        HttpProfile httpProfile = new HttpProfile();
        httpProfile.Endpoint = smsModel.domain;
        clientProfile.HttpProfile = httpProfile;

        SmsClient client = new SmsClient(cred, smsModel.region, clientProfile);
        SendSmsRequest req = new SendSmsRequest();
        req.PhoneNumberSet = smsModel.mobileTx;
        req.SmsSdkAppId = smsModel.appId;
        req.SignName = smsModel.signName;
        req.TemplateId = smsModel.templateId;
        req.TemplateParamSet = smsModel.templateParamTx;
        SendSmsResponse resp = client.SendSmsSync(req);
        if (!resp.SendStatusSet.FirstOrDefault().Code.Equals("Ok"))
        {
            throw new Exception(resp.SendStatusSet.FirstOrDefault().Message);
        }
        return resp.RequestId;
    }

    /// <summary>
    /// 获取模板配置字段.
    /// </summary>
    /// <param name="smsModel"></param>
    /// <returns></returns>
    public static string GetTemplateByTencent(SmsParameterInfo smsModel)
    {
        try
        {
            Credential cred = new Credential
            {
                SecretId = smsModel.keyId,
                SecretKey = smsModel.keySecret
            };

            ClientProfile clientProfile = new ClientProfile();
            HttpProfile httpProfile = new HttpProfile();
            httpProfile.Endpoint = smsModel.domain;
            clientProfile.HttpProfile = httpProfile;

            SmsClient client = new SmsClient(cred, smsModel.region, clientProfile);
            DescribeSmsTemplateListRequest req = new DescribeSmsTemplateListRequest();
            req.International = 0;
            req.TemplateIdSet = new ulong?[] { ulong.Parse(smsModel.templateId) };
            DescribeSmsTemplateListResponse resp = client.DescribeSmsTemplateListSync(req);
            if (resp.DescribeTemplateStatusSet.Count() > 0 && !string.IsNullOrEmpty(resp.RequestId))
            {
                var data = resp.DescribeTemplateStatusSet.FirstOrDefault().ToObject<Dictionary<string, object>>();
                var templateContent = data["TemplateContent"].ToString();
                return templateContent;
            }
            else
            {
                throw new Exception("获取模板失败");
            }

        }
        catch (Exception ex)
        {

            throw new Exception("获取模板失败");
        }
    }

    #endregion
}