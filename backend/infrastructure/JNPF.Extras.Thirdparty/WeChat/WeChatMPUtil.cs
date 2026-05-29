using Senparc.Weixin.MP;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using Senparc.Weixin.MP.AdvancedAPIs.User;
using Senparc.Weixin.MP.Containers;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.Tencent;

namespace JNPF.Extras.Thirdparty.WeChat;

/// <summary>
/// 微信公众号.
/// </summary>
public class WeChatMPUtil
{
    /// <summary>
    /// 访问令牌.
    /// </summary>
    public string accessToken { get; private set; }

    /// <summary>
    /// 构造函数.
    /// </summary>
    public WeChatMPUtil() { }

    /// <summary>
    /// 构造函数.
    /// </summary>
    public WeChatMPUtil(string appId, string appSecret)
    {
        try
        {
            accessToken = AccessTokenContainer.TryGetAccessToken(appId, appSecret);
        }
        catch (Exception ex)
        {
        }
    }

    /// <summary>
    ///  根据OpenId进行群发.
    /// </summary>
    /// <param name="type">消息类型.</param>
    /// <param name="value">群发媒体文件时传入mediaId,群发文本消息时传入content,群发卡券时传入cardId.</param>
    /// <param name="clientmsgid">开发者侧群发msgid，长度限制64字节，如不填，则后台默认以群发范围和群发内容的摘要值做为clientmsgid.</param>
    /// <param name="openIds"> openId字符串数组.</param>
    /// <param name="timeOut">代理请求超时时间（毫秒）.</param>
    /// <returns></returns>
    public bool SendGroupMessageByOpenId(int type, string value, string[] openIds, string clientmsgid = null, int timeOut = 10000)
    {
        try
        {
            GroupMessageType messageType = new GroupMessageType();
            switch (type)
            {
                case 1://文本
                    messageType = GroupMessageType.text;
                    break;
                case 2://图片
                    messageType = GroupMessageType.image;
                    break;
                case 3://语音
                    messageType = GroupMessageType.voice;
                    break;
                case 4://视频
                    messageType = GroupMessageType.video;
                    break;
                case 5://图文
                    messageType = GroupMessageType.mpnews;
                    break;
                case 6:// 卡券
                    messageType = GroupMessageType.wxcard;
                    break;
                default:
                    break;
            }
            var result = GroupMessageApi.SendGroupMessageByOpenId(accessToken, messageType, value, clientmsgid, timeOut, openIds);
            return result.errcode == 0;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// 根据OpenId发送模板消息.
    /// </summary>
    /// <param name="openId"></param>
    /// <param name="templateId"></param>
    /// <param name="url"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public void SendTemplateMessage(string openId, string templateId, string url, object data, TemplateModel_MiniProgram miniProgram = null)
    {
        var result = TemplateApi.SendTemplateMessage(accessToken, openId, templateId, url, data, miniProgram);
        if (result.errcode != 0)
        {
            throw new Exception(result.errmsg);
        }
    }

    /// <summary>
    /// 根据OpenId发送模板消息.
    /// </summary>
    /// <param name="openId"></param>
    /// <param name="templateId"></param>
    /// <param name="url"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public string GetTemplateMp(string templateId)
    {
        try
        {
            var result = TemplateApi.GetPrivateTemplate(accessToken);
            if (result.errcode == 0)
            {
                var data = result.template_list.Find(x => x.template_id == templateId);
                return data.content;
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 通过openId获取用户信息.
    /// </summary>
    /// <param name="openId"></param>
    /// <returns></returns>
    public UserInfoJson GetWeChatUserInfo(string openId)
    {
        return UserApi.Info(accessToken, openId);
    }
}
