using COSXML.Model.Tag;
using JNPF.Common.Extension;
using JNPF.Common.Models.InteAssistant;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Engine.Entity.Model.Integrate;
using JNPF.Extras.CollectiveOAuth.Models;
using JNPF.Extras.CollectiveOAuth.Utils;
using JNPF.Extras.Thirdparty.WeChat;
using JNPF.Extras.Thirdparty.WeChat.Internal;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.Logging.Attributes;
using JNPF.Message.Entitys.Entity;
using JNPF.Systems.Entitys.Permission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Qiniu.Util;
using Senparc.CO2NET.Helpers.Serializers;
using Senparc.NeuChar.NeuralSystems;
using Senparc.Weixin;
using Senparc.Weixin.Annotations;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.Tencent;
using SqlSugar;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml;

namespace JNPF.Message.Service;

/// <summary>
/// 公众号.
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "Message", Name = "WechatMiniProgram", Order = 240)]
[Route("api/message/[controller]")]
public class WechatMiniProgramService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<MessageWechatUserEntity> _repository;

    public WechatMiniProgramService(ISqlSugarRepository<MessageWechatUserEntity> repository)
    {
        _repository = repository;
    }




    /// <summary>
    /// 获取用户openId
    /// </summary>
    /// <param name="jCode"></param>
    /// <returns></returns>
    [HttpPost("token/GetOpenId")]
    [AllowAnonymous]
    [LogPolicy(LogPolicy.IgnoreRequest)]
    public async Task<dynamic> GetOpenId(string code)
    {
        var messageAccountEntity = _repository.AsSugarClient().Queryable<MessageAccountEntity>().First(x => x.EnCode == "xiaoyou" && x.Category == "7" && x.DeleteMark == null);
        string appId = messageAccountEntity.AppId;
        string appSecret = messageAccountEntity.AppSecret;

        string requestUrl = "https://api.weixin.qq.com/sns/jscode2session?appid=" + appId + "&secret=" + appSecret + "&js_code=" + code + "&grant_type=authorization_code";

        string response = HttpUtils.RequestGet(requestUrl);
        var accessTokenObject = response.parseObject();
        var authToken = new AuthToken();
        authToken.openId = accessTokenObject.getString("openid");
        return accessTokenObject;
    }

    [NonAction]
    public async Task SendMessage(MessageTemplateEntity templateEntity, IntegrateTaskEntity? taskEntity, MessageAccountEntity accountEntity, UserEntity user)
    {
        if (string.IsNullOrEmpty( user.OpenId))
        {
            return;
            //return $"用户OpenId信息为空，无法发送";
        }

        var weChatApp = new WeChatApp(accountEntity.AppId, accountEntity.AppSecret);
        Dictionary<string, string> dataItems = new Dictionary<string, string>();
        JArray taskDataObject = JArray.Parse(taskEntity.Data);
        var templateJson = taskEntity.TemplateJson.ToObject<DesignModel>();
        Dictionary<string, string> templateItems = new Dictionary<string, string>();
        List<TemplateModel> templateModels = new List<TemplateModel>();
        foreach (JObject jsonObject in templateJson.properties.formFieldList)
        {
            var property = jsonObject.Properties().FirstOrDefault();
            string value = property?.Value.ToString();

            List<SelectOptions> options = new List<SelectOptions>();
            try
            {
                var optionStr = property.Parent["options"]?.ToString();
                options = JsonSerialization.JSON.Deserialize<List<SelectOptions>>(optionStr);
            }
            catch
            {
            }
            templateModels.Add(new TemplateModel
            {
                Name = value,
                options = options
            });
        }
        foreach (JObject dataObject in taskDataObject)
        {
            //该方法暂时为湘阴校友定制的方法，未实现通用化

            JObject taskObject = JObject.Parse(dataObject["Data"].ToString());
            JObject jsonObject = JObject.Parse(templateEntity.Content);
            foreach (JProperty property in jsonObject.Properties())
            {
                var pValueName = property.Value.ToString().Replace("@", "");
                var pName = property.Name;
                var propertyValue = string.Empty;
                if (pValueName.Equals("userName"))
                {
                    propertyValue = "新注册校友";
                }
                else
                {
                    if (pName.ToLower().IndexOf("date") != -1)
                    {
                        try
                        {
                            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(taskObject[pValueName].ToString()) / 1000);
                            propertyValue = dateTimeOffset.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        catch
                        {
                            propertyValue = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                    else if (pName.ToLower().IndexOf("phrase") != -1)
                    {
                        propertyValue = "待审核";
                    }
                    else
                    {

                        propertyValue = pValueName;// taskObject[pValueName]?.ToString();
                    }
                }

                dataItems.Add(property.Name,
                              value: propertyValue);

                Console.WriteLine($"属性名: {property.Name}");
                Console.WriteLine($"值: {property.Value}");
            }
        }
        await weChatApp.SendSubscribeMessage(user.OpenId, templateEntity.TemplateCode, dataItems, templateEntity.XcxAppId);
    }

}
