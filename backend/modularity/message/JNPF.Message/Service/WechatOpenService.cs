using JNPF.Common.Extension;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.Thirdparty.WeChat;
using JNPF.Extras.Thirdparty.WeChat.Internal;
using JNPF.Logging.Attributes;
using JNPF.Message.Entitys.Entity;
using JNPF.Systems.Entitys.Permission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.Tencent;
using SqlSugar;
using System.Text;
using System.Xml;

namespace JNPF.Message.Service;

/// <summary>
/// 公众号.
/// 版 本：V3.2
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2021-06-01.
/// </summary>
[ApiDescriptionSettings(Tag = "Message", Name = "WechatOpen", Order = 240)]
[Route("api/message/[controller]")]
public class WechatOpenService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarRepository<MessageWechatUserEntity> _repository;

    public WechatOpenService(ISqlSugarRepository<MessageWechatUserEntity> repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 公众号服务器配置修改验证.
    /// </summary>
    /// <param name="enCode"></param>
    /// <param name="postModel"></param>
    /// <param name="echostr"></param>
    /// <returns></returns>
    [HttpGet("token/{enCode}")]
    [AllowAnonymous]
    [IgnoreLog]
    [NonUnify]
    public dynamic CheckToken(string enCode, [FromQuery] PostModel postModel, [FromQuery] string echostr)
    {
        var messageAccountEntity = _repository.AsSugarClient().Queryable<MessageAccountEntity>().First(x => x.EnCode == enCode && x.Category == "7" && x.DeleteMark == null);
        if (messageAccountEntity.IsNullOrEmpty() && messageAccountEntity.AppId.IsNullOrEmpty() && messageAccountEntity.AppSecret.IsNullOrEmpty() && messageAccountEntity.AppKey.IsNullOrEmpty() && messageAccountEntity.AgentId.IsNullOrEmpty() && messageAccountEntity.Bearer.IsNullOrEmpty())
        {
            Console.WriteLine("公众号账户配置不存在或错误！");
            return string.Empty;
        }
        postModel.Token = messageAccountEntity.AgentId;
        if (CheckSignature.Check(postModel.Signature, postModel))
        {
            Console.WriteLine("验证成功");
            return echostr;
        }
        else
        {
            Console.WriteLine("验证失败");
            return string.Empty;
        }
    }

    /// <summary>
    /// 公众号关注/取关.
    /// </summary>
    /// <param name="enCode"></param>
    /// <param name="postModel"></param>
    /// <param name="openid"></param>
    /// <param name="encrypt_type"></param>
    /// <returns></returns>
    [HttpPost("token/{enCode}")]
    [AllowAnonymous]
    [IgnoreLog]
    [NonUnify]
    public async Task<dynamic> Create(string enCode, [FromQuery] PostModel postModel, [FromQuery] string openid, [FromQuery] string encrypt_type)
    {
        var messageAccountEntity = _repository.AsSugarClient().Queryable<MessageAccountEntity>().First(x => x.EnCode == enCode && x.Category == "7" && x.DeleteMark == null);
        if (messageAccountEntity.IsNullOrEmpty() && messageAccountEntity.AppId.IsNullOrEmpty() && messageAccountEntity.AppSecret.IsNullOrEmpty() && messageAccountEntity.AppKey.IsNullOrEmpty() && messageAccountEntity.AgentId.IsNullOrEmpty() && messageAccountEntity.Bearer.IsNullOrEmpty())
        {

            return string.Empty;
        }
        postModel.Token = messageAccountEntity.AgentId;
        postModel.EncodingAESKey = messageAccountEntity.Bearer;
        var input = GetWechatMPEvent(encrypt_type, postModel, messageAccountEntity);
        if (CheckSignature.Check(postModel.Signature, postModel))
        {
            var weChatUserEntity = await _repository.GetFirstAsync(x => x.GzhId == input.ToUserName && x.OpenId == openid);
            if (weChatUserEntity.IsNullOrEmpty())
            {
                var wechatUser = new WeChatMPUtil(messageAccountEntity.AppId, messageAccountEntity.AppSecret).GetWeChatUserInfo(openid);
                if (wechatUser.IsNullOrEmpty() || wechatUser.unionid.IsNullOrEmpty())
                {
                    Console.WriteLine("获取微信用户失败！");
                    return string.Empty;
                }
                var socialsUser = await _repository.AsSugarClient().Queryable<SocialsUsersEntity>().FirstAsync(x => x.SocialId == wechatUser.unionid && x.SocialType == "wechat_open" && x.DeleteMark == null);
                if (socialsUser.IsNotEmptyOrNull() && socialsUser.UserId.IsNotEmptyOrNull())
                {
                    weChatUserEntity = new MessageWechatUserEntity
                    {
                        GzhId = input.ToUserName,
                        UserId = socialsUser.UserId,
                        OpenId = openid,
                        CloseMark = 1
                    };
                    await _repository.AsInsertable(weChatUserEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }
                else
                {
                    Console.WriteLine("当前微信账号未绑定系统用户");
                    if (wechatUser.IsNotEmptyOrNull() && wechatUser.unionid.IsNotEmptyOrNull())
                    {
                        var socialsUsersEntity = new SocialsUsersEntity
                        {
                            SocialType = "wechat_open",
                            SocialId = wechatUser.unionid,
                        };
                        await _repository.AsSugarClient().Insertable(socialsUsersEntity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                    }
                }
            }
            else
            {
                switch (input.Event)
                {
                    case "subscribe":
                        weChatUserEntity.CloseMark = 1;
                        break;
                    case "unsubscribe":
                        weChatUserEntity.CloseMark = 0;
                        break;
                    default:
                        break;
                }
                await _repository.AsUpdateable(weChatUserEntity).IgnoreColumns(ignoreAllNullColumns: true).CallEntityMethod(m => m.LastModify()).ExecuteCommandHasChangeAsync();
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// 获取xml参数.
    /// </summary>
    /// <returns></returns>
    private WechatMPEvent GetWechatMPEvent(string encrypt_type, PostModel postModel, MessageAccountEntity messageAccountEntity)
    {
        try
        {
            var request = App.HttpContext.Request;
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            request.Body.ReadAsync(buffer, 0, buffer.Length);
            var body = Encoding.UTF8.GetString(buffer);
            XmlDocument doc = new XmlDocument();
            var input = new WechatMPEvent();
            doc.LoadXml(body);
            if (encrypt_type.IsNotEmptyOrNull() && encrypt_type.Equals("aes"))
            {
                WXBizMsgCrypt wxcpt = new WXBizMsgCrypt(messageAccountEntity.AgentId, messageAccountEntity.Bearer, messageAccountEntity.AppId);
                var result = string.Empty;
                var errcode = wxcpt.DecryptMsg(postModel.Msg_Signature, postModel.Timestamp, postModel.Nonce, body, ref result);
                if (errcode == 0)
                {
                    doc.LoadXml(result);
                    input.ToUserName = doc.DocumentElement.SelectSingleNode("ToUserName").InnerText.Trim();
                    input.FromUserName = doc.DocumentElement.SelectSingleNode("FromUserName").InnerText.Trim();
                    input.Event = doc.DocumentElement.SelectSingleNode("Event").InnerText.Trim();
                }
                else
                {
                    Console.WriteLine("公众号消息密文解密失败,错误码:" + errcode);
                }
            }
            else
            {
                input.ToUserName = doc.DocumentElement.SelectSingleNode("ToUserName").InnerText.Trim();
                input.FromUserName = doc.DocumentElement.SelectSingleNode("FromUserName").InnerText.Trim();
                input.Event = doc.DocumentElement.SelectSingleNode("Event").InnerText.Trim();
            }
            return input;
        }
        catch (Exception ex)
        {
            Console.WriteLine("公众号关注取关参数异常,异常原因:" + ex.Message);
            throw;
        }
    }
}
