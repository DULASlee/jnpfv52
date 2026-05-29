using JNPF.ClayObject.Extensions;
using JNPF.Common.Configuration;
using JNPF.Common.Core.Handlers;
using JNPF.Common.Core.Manager;
using JNPF.Common.Dtos.Message;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.Extras.Thirdparty.DingDing;
using JNPF.Extras.Thirdparty.Email;
using JNPF.Extras.Thirdparty.Sms;
using JNPF.Extras.Thirdparty.WeChat;
using JNPF.FriendlyException;
using JNPF.InteAssistant.Entitys.Entity;
using JNPF.Message.Entitys;
using JNPF.Message.Entitys.Entity;
using JNPF.Message.Interfaces;
using JNPF.Message.Interfaces.Message;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Entitys.System;
using JNPF.Systems.Interfaces.Permission;
using JNPF.Systems.Interfaces.System;
using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;
using SqlSugar;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace JNPF.Message.Service;

/// <summary>
/// 消息中心处理类.
/// </summary>
public class MessageManager : IMessageManager, ITransient
{
    private readonly ISqlSugarRepository<MessageEntity> _repository;
    private readonly IUsersService _usersService;
    private readonly IMHandler _imHandler;
    private readonly IUserManager _userManager;
    private readonly IShortLinkService _shortLinkService;
    private readonly ISysConfigService _sysConfigService;
    private readonly MessageOptions _messageOptions = App.GetConfig<MessageOptions>("Message", true);
    private readonly WechatMiniProgramService _wechatMiniProgramService;
    public MessageManager(
        ISqlSugarRepository<MessageEntity> repository,
        IUsersService usersService,
        IMHandler imHandler,
        IShortLinkService shortLinkService,
        ISysConfigService sysConfigService,
        IUserManager userManager,
        WechatMiniProgramService wechatMiniProgramService)
    {
        _repository = repository;
        _usersService = usersService;
        _shortLinkService = shortLinkService;
        _sysConfigService = sysConfigService;
        _imHandler = imHandler;
        _userManager = userManager;
        _wechatMiniProgramService = wechatMiniProgramService;
    }

    #region Public

    /// <summary>
    /// 默认消息发送.
    /// </summary>
    /// <param name="toUserIds"></param>
    /// <param name="messageList"></param>
    /// <returns></returns>
    public async Task SendDefaultMsg(List<string> toUserIds, List<MessageEntity> messageList)
    {
        await WebSocketSend(toUserIds, messageList);

        #region 消息监控
        var messageMonitorEntity = new MessageMonitorEntity();
        messageMonitorEntity.MessageType = "1";
        messageMonitorEntity.MessageSource = messageList.FirstOrDefault()?.Type.ToString();
        messageMonitorEntity.SendTime = DateTime.Now;
        messageMonitorEntity.MessageTemplateId = string.Empty;
        messageMonitorEntity.Title = messageList.FirstOrDefault()?.Title;
        messageMonitorEntity.Content = string.Empty;
        messageMonitorEntity.ReceiveUser = toUserIds.ToJsonString();
        await _repository.AsSugarClient().Insertable(messageMonitorEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        #endregion
    }

    /// <summary>
    /// 自定义消息发送.
    /// </summary>
    /// <param name="messageSendModel"></param>
    /// <param name="bodyDic">跳转页面参数,参数格式 key:用户id，value:跳转json.</param>
    /// <param name="taskEntity"></param>
    /// <returns></returns>
    public async Task<string> SendDefinedMsg(MessageSendModel messageSendModel, Dictionary<string, object> bodyDic, IntegrateTaskEntity? taskEntity = null)
    {
        var errorList = new List<string>();
        var messageTemplateEntity = await _repository.AsSugarClient().Queryable<MessageTemplateEntity>().FirstAsync(x => x.Id == messageSendModel.templateId && x.DeleteMark == null);
        var messageAccountEntity = await _repository.AsSugarClient().Queryable<MessageAccountEntity>().FirstAsync(x => x.Id == messageSendModel.accountConfigId && x.DeleteMark == null);
        var paramsDic = messageSendModel.paramJson.ToDictionary(x => x.field, y => y.value);//参数
        var title = messageTemplateEntity.Title;
        var content = messageTemplateEntity.Content;
        if (messageTemplateEntity.MessageType == "6") messageSendModel.toUser = new List<string> { _userManager.UserId };
        var sysconfig = await _sysConfigService.GetInfo();
        foreach (var item in messageSendModel.toUser)
        {
            var userId = item.Replace("-delegate", string.Empty);
            var user = await _usersService.GetInfoByUserIdAsync(userId);
            var userName = user.RealName;
            try
            {
                if (bodyDic.IsNotEmptyOrNull() && bodyDic.ContainsKey(item))
                {
                    var shortLinkEntity = await _shortLinkService.Create(userId, bodyDic[item].ToJsonString());
                    paramsDic["@FlowLink"] = string.Format("{0}/dev/api/message/ShortLink/{1}", _messageOptions.DoMainPc, shortLinkEntity.ShortLink);
                    if (KeyVariable.MultiTenancy)
                    {
                        paramsDic["@FlowLink"] = string.Format("{0}/dev/api/message/ShortLink/{1}/{2}", _messageOptions.DoMainPc, shortLinkEntity.ShortLink, _userManager.TenantId);
                    }
                }
                messageTemplateEntity.Title = MessageTemplateManage(title, paramsDic);
                messageTemplateEntity.Content = MessageTemplateManage(content, paramsDic);
                switch (messageTemplateEntity.MessageType)
                {
                    case "1"://站内信
                        var messageList = GetMessageList(messageTemplateEntity.EnCode, new List<string>() { userId }, paramsDic, messageTemplateEntity.MessageSource.ParseToInt(), bodyDic);
                        await WebSocketSend(new List<string>() { userId }, messageList);
                        break;
                    case "2"://邮件
                        EmailSend(new List<string>() { userId }, messageTemplateEntity, messageAccountEntity);
                        break;
                    case "3"://短信
                        SmsSend(new List<string>() { userId }, messageTemplateEntity, messageAccountEntity, paramsDic);
                        break;
                    case "4"://钉钉
                        var dingIds = _repository.AsSugarClient().Queryable<SynThirdInfoEntity>()
                            .Where(x => x.ThirdType == 2 && x.DataType == 3 && x.SysObjId == userId && !SqlFunc.IsNullOrEmpty(x.ThirdObjId))
                        .Select(x => x.ThirdObjId).ToList();
                        if (dingIds.Count > 0)
                        {
                            var dingMsg = new { msgtype = "text", text = new { content = messageTemplateEntity.Content } }.ToJsonString();
                            DingWorkMessageParameter dingWorkMsgModel = new DingWorkMessageParameter()
                            {
                                toUsers = string.Join(",", dingIds),
                                agentId = sysconfig.dingAgentId,
                                msg = dingMsg
                            };
                            new DingUtil(sysconfig.dingSynAppKey, sysconfig.dingSynAppSecret).SendWorkMsg(dingWorkMsgModel);
                        }
                        else
                        {
                            throw Oops.Oh(ErrorCode.D7015);
                        }
                        break;
                    case "5"://企业微信
                        var qyIds = _repository.AsSugarClient().Queryable<SynThirdInfoEntity>()
                            .Where(x => x.ThirdType == 1 && x.DataType == 3 && x.SysObjId == userId && !SqlFunc.IsNullOrEmpty(x.ThirdObjId))
                        .Select(x => x.ThirdObjId).ToList();
                        var weChat = new WeChatUtil(sysconfig.qyhCorpId, sysconfig.qyhCorpSecret);
                        if (qyIds.Count > 0)
                        {
                            await weChat.SendText(sysconfig.qyhAgentId, messageTemplateEntity.Content, string.Join(",", qyIds));
                        }
                        else
                        {
                            throw Oops.Oh(ErrorCode.D7015);
                        }
                        break;
                    case "6"://WebHook
                        await WebHookSend(messageTemplateEntity, messageAccountEntity);
                        break;
                    case "7"://微信公众号
                        var body = bodyDic.ContainsKey(item) ? bodyDic[item].ToJsonString() : string.Empty;
                        WeChatMpSend(userId, messageTemplateEntity, messageAccountEntity, paramsDic, body);
                        break;
                    case "8"://消息弹窗
                        var messageList2 = GetMessageList(messageTemplateEntity.EnCode, new List<string>() { userId }, paramsDic, messageTemplateEntity.MessageSource.ParseToInt(), bodyDic);

                        // 因可以匿名调接口
                        string tenantId = _userManager.TenantId.IsNullOrEmpty() ? "default" : _userManager.TenantId;

                        await WebSocketSend(tenantId, new List<string>() { userId }, messageList2, messageSendModel.gotoLink, messageSendModel.linkParams);
                        break;
                    case "22":
                        // 微信小程序推送.
                        await _wechatMiniProgramService.SendMessage(messageTemplateEntity, taskEntity, messageAccountEntity, user);
                        break;
                }
            }
            catch (Exception ex)
            {
                errorList.Add(string.Format("用户{0}【{1}】", userName, ex.Message));
            }
        }

        #region 消息监控
        var messageMonitorEntity = new MessageMonitorEntity();
        messageMonitorEntity.MessageType = messageTemplateEntity.MessageType;
        messageMonitorEntity.MessageSource = messageTemplateEntity.MessageSource;
        messageMonitorEntity.SendTime = DateTime.Now;
        messageMonitorEntity.MessageTemplateId = messageTemplateEntity.Id;
        messageMonitorEntity.Title = messageTemplateEntity.Title;
        messageMonitorEntity.Content = messageTemplateEntity.Content;
        messageMonitorEntity.ReceiveUser = messageSendModel.toUser.ToJsonString();
        await _repository.AsSugarClient().Insertable(messageMonitorEntity).IgnoreColumns(ignoreNullColumn: true).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
        #endregion
        return errorList.Any() ? string.Join(",", errorList) : string.Empty;
    }

    /// <summary>
    /// 获取自定义消息发送配置.
    /// </summary>
    /// <param name="sendConfigId"></param>
    /// <returns></returns>
    public async Task<List<MessageSendModel>> GetMessageSendModels(string sendConfigId)
    {
        var msgSendType = await _repository.AsSugarClient().Queryable<DictionaryTypeEntity>().FirstAsync(x => x.EnCode == "msgSendType" && x.DeleteMark == null);
        var list = await _repository.AsSugarClient().Queryable<MessageSendTemplateEntity, MessageTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id))
            .Where((a, b) => a.SendConfigId == sendConfigId && a.DeleteMark == null && b.DeleteMark == null)
            .Select((a, b) => new MessageSendModel
            {
                accountConfigId = a.AccountConfigId,
                id = a.Id,
                messageType = SqlFunc.Subqueryable<DictionaryDataEntity>().Where(u => u.DictionaryTypeId == msgSendType.Id && u.EnCode == a.MessageType).Select(u => u.FullName),
                msgTemplateName = b.FullName,
                sendConfigId = a.SendConfigId,
                templateId = a.TemplateId,
            }).ToListAsync();
        foreach (var item in list)
        {
            // 是否存在参数.
            var flag = await _repository.AsSugarClient().Queryable<MessageSmsFieldEntity>().AnyAsync(x => x.TemplateId == item.templateId && x.DeleteMark == null);
            if (flag)
            {
                item.paramJson = await _repository.AsSugarClient().Queryable<MessageTemplateParamEntity, MessageTemplateEntity, MessageSmsFieldEntity>((a, b, c) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id, JoinType.Left, a.TemplateId == c.TemplateId))
                .Where((a, b, c) => a.TemplateId == item.templateId && a.DeleteMark == null && b.DeleteMark == null && a.Field == c.Field && a.Field != "@FlowLink")
                .Select((a, b) => new MessageSendParam
                {
                    field = a.Field,
                    fieldName = a.FieldName,
                    id = a.Id,
                    templateCode = b.TemplateCode,
                    templateId = a.TemplateId,
                    templateName = b.FullName,
                    templateType = b.TemplateType
                }).ToListAsync();
            }
            else
            {
                item.paramJson = await _repository.AsSugarClient().Queryable<MessageTemplateParamEntity, MessageTemplateEntity>((a, b) => new JoinQueryInfos(JoinType.Left, a.TemplateId == b.Id))
                .Where((a, b) => a.TemplateId == item.templateId && a.DeleteMark == null && b.DeleteMark == null && a.Field != "@FlowLink")
                .Where((a, b) => b.Title.Contains(a.Field) || b.Content.Contains(a.Field))
                .Select((a, b) => new MessageSendParam
                {
                    field = a.Field,
                    fieldName = a.FieldName,
                    id = a.Id,
                    templateCode = b.TemplateCode,
                    templateId = a.TemplateId,
                    templateName = b.FullName,
                    templateType = b.TemplateType
                }).ToListAsync();
            }
        }
        return list;
    }

    /// <summary>
    /// 获取消息实例.
    /// </summary>
    /// <param name="enCode">消息编码.</param>
    /// <param name="toUserIds">发送人员.</param>
    /// <param name="paramDic">标题或内容替换参数.</param>
    /// <param name="type">消息类型 1-公告 2-流程 3-系统 4-日程.</param>
    /// <param name="flowType">流程类型 1-审批 2-委托.</param>
    /// <returns></returns>
    public List<MessageEntity> GetMessageList(string enCode, List<string> toUserIds, Dictionary<string, string> paramDic, int type, Dictionary<string, object> bodyDic = null, int flowType = 1)
    {
        var messageList = new List<MessageEntity>();
        var msgTemplateEntity = _repository.AsSugarClient().Queryable<MessageTemplateEntity>().First(x => x.EnCode == enCode && x.DeleteMark == null);

        foreach (var item in toUserIds)
        {
            var messageEntity = new MessageEntity();
            messageEntity.Id = SnowflakeIdHelper.NextId();
            messageEntity.Type = type;
            messageEntity.FlowType = flowType;
            messageEntity.IsRead = 0;
            if (msgTemplateEntity.IsNotEmptyOrNull())
            {
                messageEntity.Title = MessageTemplateManage(msgTemplateEntity.Title, paramDic);
            }
            switch (type)
            {
                case 1:
                    messageEntity.UserId = item;
                    messageEntity.BodyText = bodyDic.ToJsonString();
                    break;
                case 2:
                    messageEntity.UserId = item.Replace("-delegate", string.Empty);
                    messageEntity.BodyText = bodyDic.IsNotEmptyOrNull() && bodyDic.ContainsKey(item) ? bodyDic[item].ToJsonString() : null;
                    break;
                case 3:
                    messageEntity.UserId = item;
                    messageEntity.BodyText = messageEntity.Title;
                    break;
                case 4:
                    messageEntity.UserId = item;
                    messageEntity.BodyText = bodyDic.IsNotEmptyOrNull() && bodyDic.ContainsKey(item) ? bodyDic[item].ToJsonString() : null;
                    break;
                default:
                    break;
            }
            messageList.Add(messageEntity);
        }
        return messageList;
    }

    /// <summary>
    /// 强制下线.
    /// </summary>
    /// <param name="connectionId"></param>
    public async Task ForcedOffline(string connectionId)
    {
        await _imHandler.SendMessageAsync(connectionId, new { method = "logout", msg = "此账号已在其他地方登录" }.ToJsonString());
    }
    #endregion

    #region Private

 
    /// <summary>
    /// 保存数据.
    /// </summary>
    /// <param name="entity">实体对象.</param>
    /// <param name="receiveEntityList">收件用户.</param>
    private int SaveMessage(List<MessageEntity> messageList)
    {
        try
        {
            // 数组不支持IgnoreColumns(ignoreNullColumn: true)
            return _repository.AsInsertable(messageList).CallEntityMethod(m => m.Create()).ExecuteCommand();
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// 消息模板参数替换.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="paramDic"></param>
    /// <param name="isGzh"></param>
    /// <returns></returns>
    private string MessageTemplateManage(string text, Dictionary<string, string> paramDic, bool isGzh = false)
    {
        if (text.IsNotEmptyOrNull())
        {
            // 系统参数
            //var sysParams = new List<string> { "@Title", "@CreatorUserName", "@Content", "@Remark", "@FlowLink", "@StartDate", "@StartTime", "@EndDate", "@EndTime" };
            foreach (var item in paramDic.Keys)
            {
                if (isGzh)
                {
                    text = text.Replace(item, paramDic[item]);
                }
                else
                {
                    text = text.Replace("{" + item + "}", paramDic[item]);
                }
            }
        }
        return text;
    }

    /// <summary>
    /// 邮箱.
    /// </summary>
    /// <param name="userList"></param>
    /// <param name="messageTemplateEntity"></param>
    /// <param name="messageAccountEntity"></param>
    private void EmailSend(List<string> userList, MessageTemplateEntity messageTemplateEntity, MessageAccountEntity messageAccountEntity)
    {
        foreach (var item in userList)
        {
            var user = _usersService.GetInfoByUserId(item);
            if (user.IsNotEmptyOrNull() && user.Email.IsNotEmptyOrNull())
            {
                var mailModel = new MailInfo();
                mailModel.To = user.Email;
                mailModel.ToName = user.RealName;
                mailModel.Subject = messageTemplateEntity.Title;
                mailModel.BodyText = HttpUtility.HtmlDecode(messageTemplateEntity.Content);
                var mailParameterInfo = new MailParameterInfo
                {
                    AccountName = messageAccountEntity.AddressorName,
                    Account = messageAccountEntity.SmtpUser,
                    Password = messageAccountEntity.SmtpPassword,
                    SMTPHost = messageAccountEntity.SmtpServer,
                    SMTPPort = messageAccountEntity.SmtpPort.ParseToInt(),
                    Ssl = messageAccountEntity.SslLink == 1
                };
                MailUtil.Send(mailParameterInfo, mailModel);
            }
        }
    }

    /// <summary>
    /// 短信.
    /// </summary>
    /// <param name="userList"></param>
    /// <param name="messageTemplateEntity"></param>
    /// <param name="messageAccountEntity"></param>
    /// <param name="smsParams"></param>
    private void SmsSend(List<string> userList, MessageTemplateEntity messageTemplateEntity, MessageAccountEntity messageAccountEntity, Dictionary<string, string> smsParams)
    {
        var phoneList = new List<string>();//电话号码
        foreach (var item in userList)
        {
            var user = _usersService.GetInfoByUserId(item);
            if (user.IsNotEmptyOrNull() && user.MobilePhone.IsNotEmptyOrNull())
            {
                phoneList.Add("+86" + user.MobilePhone);
            }
        }
        var smsModel = new SmsParameterInfo()
        {
            keyId = messageAccountEntity.AppId,
            keySecret = messageAccountEntity.AppSecret,
            region = messageAccountEntity.ZoneParam,
            domain = messageAccountEntity.Channel.Equals("1") ? messageAccountEntity.EndPoint : messageAccountEntity.ZoneName,
            templateId = messageTemplateEntity.TemplateCode,
            signName = messageAccountEntity.SmsSignature,
            appId = messageAccountEntity.SdkAppId
        };
        var smsFieldList = _repository.AsSugarClient().Queryable<MessageSmsFieldEntity>().Where(x => x.TemplateId == messageTemplateEntity.Id).ToDictionary(x => x.SmsField, y => y.Field);
        foreach (var item in smsFieldList.Keys)
        {
            if (smsParams.Keys.Contains(smsFieldList[item].ToString()))
            {
                smsFieldList[item] = smsParams[smsFieldList[item].ToString()];
            }
        }
        if (messageAccountEntity.Channel.Equals("1"))
        {
            messageTemplateEntity.Content = SmsUtil.GetTemplateByAli(smsModel);
            smsModel.mobileAli = string.Join(",", phoneList);
            smsModel.templateParamAli = smsFieldList.ToJsonString();
            foreach (var item in smsFieldList.Keys)
            {
                messageTemplateEntity.Content = messageTemplateEntity.Content.Replace("${" + item + "}", smsFieldList[item].ToString());
            }
            SmsUtil.SendSmsByAli(smsModel);
        }
        else
        {
            messageTemplateEntity.Content = SmsUtil.GetTemplateByTencent(smsModel);
            smsModel.mobileTx = phoneList.ToArray();
            List<string> mList = new List<string>();
            var fields = messageTemplateEntity.Content.Substring3();
            foreach (string item in fields)
            {
                if (smsFieldList.ContainsKey(item))
                    mList.Add(smsFieldList[item].ToString());
            }
            smsModel.templateParamTx = mList.ToArray();
            foreach (var item in smsFieldList.Keys)
            {
                messageTemplateEntity.Content = messageTemplateEntity.Content.Replace("{" + item + "}", smsFieldList[item].ToString());
            }
            SmsUtil.SendSmsByTencent(smsModel);
        }
    }

    /// <summary>
    /// webhook.
    /// </summary>
    /// <param name="messageTemplateEntity"></param>
    /// <param name="messageAccountEntity"></param>
    /// <returns></returns>
    private async Task WebHookSend(MessageTemplateEntity messageTemplateEntity, MessageAccountEntity messageAccountEntity)
    {
        // 钉钉
        if (messageAccountEntity.WebhookType == 1)
        {
            // 认证
            if (messageAccountEntity.ApproveType == 2) SignWebhook(messageAccountEntity);
            new DingUtil().SendGroupMsg(messageAccountEntity.WebhookAddress, messageTemplateEntity.Content);
        }
        // 企业微信
        if (messageAccountEntity.WebhookType == 2)
        {
            var bodyDic = new Dictionary<string, object>();
            bodyDic.Add("msgtype", "text");
            bodyDic.Add("text", new { content = messageTemplateEntity.Content });
            await messageAccountEntity.WebhookAddress.SetBody(bodyDic).PostAsStringAsync();
        }
    }

    /// <summary>
    /// webhook签名.
    /// </summary>
    /// <param name="messageAccountEntity"></param>
    private void SignWebhook(MessageAccountEntity messageAccountEntity)
    {
        //  webhook加签密钥
        var secret = messageAccountEntity.Bearer;

        //  获取时间戳
        var timestamp = DateTime.Now.ParseToUnixTime();

        var signature = string.Empty;
        using (var hmac = new HMACSHA256(secret.ToBase64String().ToBytes()))
        {
            byte[] hashmessage = hmac.ComputeHash(timestamp.ToString().ToBytes(Encoding.UTF8));
            signature = hashmessage.ToHexString();
        }

        messageAccountEntity.WebhookAddress = string.Format("{0}&timestamp={1}&signature={2}", messageAccountEntity.WebhookAddress, timestamp, signature);
    }

    /// <summary>
    /// 公众号.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="messageTemplateEntity"></param>
    /// <param name="messageAccountEntity"></param>
    /// <param name="paramDic"></param>
    /// <param name="bodyDic"></param>
    private async void WeChatMpSend(string userId, MessageTemplateEntity messageTemplateEntity, MessageAccountEntity messageAccountEntity, Dictionary<string, string> paramDic, string bodyDic)
    {
        var weChatMP = new WeChatMPUtil(messageAccountEntity.AppId, messageAccountEntity.AppSecret);
        var wechatUser = _repository.AsSugarClient().Queryable<MessageWechatUserEntity>().First(x => userId == x.UserId && x.CloseMark == 1 && x.DeleteMark == null);
        if (wechatUser == null) throw Oops.Oh(ErrorCode.D7016);
        var openId = wechatUser.IsNotEmptyOrNull() ? wechatUser.OpenId : string.Empty;
        var mpFieldList = _repository.AsSugarClient().Queryable<MessageSmsFieldEntity>().Where(x => x.TemplateId == messageTemplateEntity.Id).ToList();
        var mpTempDic = new Dictionary<string, object>();
        foreach (var item in mpFieldList)
        {
            if (paramDic.Keys.Contains(item.Field))
            {
                GetGZHTemplate(mpTempDic, paramDic[item.Field], item.SmsField);
            }
        }
        var url = paramDic.ContainsKey("@FlowLink") ? paramDic["@FlowLink"] : string.Empty;
        // 跳转小程序
        if (messageTemplateEntity.WxSkip == "1")
        {
            var config = bodyDic.ToBase64String();
            var token = await _shortLinkService.CreateToken(userId, _userManager.TenantId);
            var miniProgram = new TemplateModel_MiniProgram
            {
                appid = messageTemplateEntity.XcxAppId,
                pagepath = "/pages/workFlow/flowBefore/index?config=" + config + "&token=" + token
            };
            weChatMP.SendTemplateMessage(openId, messageTemplateEntity.TemplateCode, url, mpTempDic, miniProgram);
        }
        else
        {
            weChatMP.SendTemplateMessage(openId, messageTemplateEntity.TemplateCode, url, mpTempDic);
        }
    }

    /// <summary>
    /// 公众号参数验证.
    /// </summary>
    /// <param name="mpTempDic"></param>
    /// <param name="paramValue"></param>
    /// <param name="smsField"></param>
    /// <exception cref="Exception"></exception>
    private void GetGZHTemplate(Dictionary<string, object> mpTempDic, string paramValue, string smsField)
    {
        if (smsField.Contains("-"))
        {
            var tempFields = smsField.Split("-").ToList();
            if (tempFields.Count == 2 && tempFields[1].IsNotEmptyOrNull())
            {
                switch (tempFields[1])
                {
                    case "time":
                        if (paramValue.IsNumeric())
                        {
                            var date = paramValue.TimeStampToDateTime().ToString("yyyy年MM月dd日 HH:mm");
                            mpTempDic[tempFields[0]] = new { value = date };
                        }
                        else
                        {
                            DateTime result;
                            var flag = DateTime.TryParse(paramValue, out result);
                            if (flag)
                            {
                                var date = result.ToString("yyyy年MM月dd日 HH:mm");
                                mpTempDic[tempFields[0]] = new { value = date };
                            }
                            else
                            {
                                throw new Exception(string.Format("公众号模板参数{0}格式错误", smsField));
                            }
                        }
                        break;
                    case "amount":
                        if (paramValue.IsAmount())
                        {
                            mpTempDic[tempFields[0]] = new { value = paramValue };
                        }
                        else
                        {
                            throw new Exception(string.Format("公众号模板参数{0}格式错误", smsField));
                        }
                        break;
                    case "phone_number":
                        if (paramValue.IsMobileNumber())
                        {
                            mpTempDic[tempFields[0]] = new { value = paramValue };
                        }
                        else
                        {
                            throw new Exception(string.Format("公众号模板参数{0}格式错误", smsField));
                        }
                        break;
                    case "car_number":
                        if (paramValue.IsCarNumber())
                        {
                            mpTempDic[tempFields[0]] = new { value = paramValue };
                        }
                        else
                        {
                            throw new Exception(string.Format("公众号模板参数{0}格式错误", smsField));
                        }
                        break;
                    default:
                        mpTempDic[smsField] = new { value = paramValue };
                        break;
                }
            }
            else
            {
                mpTempDic[smsField] = new { value = paramValue };
            }
        }
        else
        {
            mpTempDic[smsField] = new { value = paramValue };
        }

    }

    /// <summary>
    /// 站内信.
    /// </summary>
    /// <param name="toUserIds"></param>
    /// <param name="messageEntity"></param>
    /// <param name="receiveEntityList"></param>
    /// <param name="gotolink"></param>
    /// <returns></returns>
    private async Task WebSocketSend(List<string> toUserIds, List<MessageEntity> messageList)
    {
        SaveMessage(messageList);
        if (toUserIds.Any())
        {
            foreach (var item in toUserIds)
            {
                var userId = item.Replace("-delegate", string.Empty);
                var messageEntity = messageList.Find(x => x.UserId == userId);
                // 消息推送 - 指定用户
                await _imHandler.SendMessageToUserAsync(string.Format("{0}-{1}", _userManager.TenantId, userId), new { method = "messagePush", messageType = 2, userId = _userManager.UserId, toUserId = toUserIds, title = messageEntity.Title, unreadNoticeCount = 1, id = messageEntity.Id }.ToJsonString());
            }
        }
        else
        {
            await _imHandler.SendMessageToTenantAsync(_userManager.TenantId, new { method = "messagePush", messageType = 1, userId = _userManager.UserId, toUserId = toUserIds, title = messageList.FirstOrDefault()?.Title, unreadNoticeCount = 1, id = messageList.FirstOrDefault()?.Id }.ToJsonString());
        }
    }

    /// <summary>
    /// 信息弹窗
    /// </summary>
    /// <param name="toUserIds"></param>
    /// <param name="messageEntity"></param>
    /// <param name="receiveEntityList"></param>
    /// <param name="gotolink"></param>
    /// <returns></returns>
    private async Task WebSocketSend(string tenantId, List<string> toUserIds, List<MessageEntity> messageList, string gotolink, Dictionary<string, string> linkParams)
    {
        SaveMessage(messageList);
        if (toUserIds.Any())
        {
            foreach (var item in toUserIds)
            {
                var userId = item.Replace("-delegate", string.Empty);
                var messageEntity = messageList.Find(x => x.UserId == userId);
                // 消息推送 - 指定用户
                string msgJson = new { method = "messagePush", messageType = 2, userId = _userManager.UserId, toUserId = toUserIds, title = messageEntity.Title, unreadNoticeCount = 1, id = messageEntity.Id, gotolink = gotolink, linkParams = linkParams }.ToJsonString();
                await _imHandler.SendMessageToUserAsync(string.Format("{0}-{1}", tenantId, userId), msgJson);
            }
        }
        else
        {
            await _imHandler.SendMessageToTenantAsync(tenantId, new { method = "messagePush", messageType = 1, userId = _userManager.UserId, toUserId = toUserIds, title = messageList.FirstOrDefault()?.Title, unreadNoticeCount = 1, id = messageList.FirstOrDefault()?.Id, gotolink = gotolink, linkParams = linkParams }.ToJsonString());
        }
    }
    #endregion
}
