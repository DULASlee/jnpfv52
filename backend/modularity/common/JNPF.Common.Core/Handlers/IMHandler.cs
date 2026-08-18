using JNPF.Common.Configuration;
using JNPF.Common.Const;
using JNPF.Common.Core.Manager;
using JNPF.Common.Core.Manager.Tenant;
using JNPF.Common.Enums;
using JNPF.Common.Extension;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DataEncryption;
using JNPF.Extras.WebSockets.Models;
using JNPF.Message.Entitys;
using JNPF.Message.Entitys.Dto.IM;
using JNPF.Message.Entitys.Entity;
using JNPF.Message.Entitys.Enums;
using JNPF.Message.Entitys.Model.IM;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Permission;
using JNPF.Common.Core.Diagnostics;
using JNPF.WebSockets;
using Mapster;
using SqlSugar;
using System.Net.WebSockets;
using UAParser;

namespace JNPF.Common.Core.Handlers;

/// <summary>
/// IM 处理程序.
/// </summary>
public class IMHandler : WebSocketHandler
{
    /// <summary>
    /// SqlSugarClient客户端.
    /// 注意：去掉了 static — ISqlSugarClient 本身已注册为 Singleton，直接注入即可。
    /// </summary>
    private ISqlSugarClient? _sqlSugarClient;

    /// <summary>
    /// 缓存管理.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    private readonly ITenantManager _tenantManager;

    private readonly IDataBaseManager _dataBaseManager;

    /// <summary>
    /// 消息配置.
    /// </summary>
    private readonly MessageOptions _messageOptions = App.GetConfig<MessageOptions>("Message", true);

    /// <summary>
    /// 初始化一个<see cref="IMHandler"/>类型的新实例.
    /// </summary>
    public IMHandler(
        WebSocketConnectionManager webSocketConnectionManager,
        ISqlSugarClient sqlSugarClient,
        ITenantManager tenantManager,
        IDataBaseManager dataBaseManager,
        ICacheManager cacheManager)
        : base(webSocketConnectionManager)
    {
        _sqlSugarClient = sqlSugarClient;
        _tenantManager = tenantManager;
        _dataBaseManager = dataBaseManager;
        _cacheManager = cacheManager;
    }

    /// <summary>
    /// 消息接收.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="result"></param>
    /// <param name="receivedMessage"></param>
    /// <returns></returns>
    public override async Task ReceiveAsync(WebSocketClient client, WebSocketReceiveResult result, string receivedMessage)
    {
        MessageInput? message = null;
        try
        {
            message = receivedMessage.ToObject<MessageInput>();
            var claims = JWTEncryption.ReadJwtToken(message.token.Replace("Bearer ", string.Empty).Replace("bearer ", string.Empty))?.Claims;
            client.UserId = claims.FirstOrDefault(e => e.Type == ClaimConst.CLAINMUSERID)?.Value;
            client.Account = claims.FirstOrDefault(e => e.Type == ClaimConst.CLAINMACCOUNT)?.Value;
            client.UserName = claims.FirstOrDefault(e => e.Type == ClaimConst.CLAINMREALNAME)?.Value;
            client.TenantId = claims.FirstOrDefault(e => e.Type == ClaimConst.TENANTID)?.Value;
            client.LoginTime = string.Format("{0:yyyy-MM-dd HH:mm}", string.Format("{0}000", claims.FirstOrDefault(e => e.Type == "iat")?.Value).TimeStampToDateTime());
            client.LoginIpAddress = client.LoginIpAddress;
            client.IsMobileDevice = message.mobileDevice;
            client.onlineTicket = claims.FirstOrDefault(e => e.Type == ClaimConst.OnlineTicket)?.Value;
            if (client.WebSocket.State != WebSocketState.Open) return;
            await OnConnected(client.ConnectionId, client);
            WebSocketConnectionManager.AddToTenant(client.ConnectionId, client.TenantId);
            WebSocketConnectionManager.AddToUser(client.ConnectionId, string.Format("{0}-{1}", client.TenantId, client.UserId));
            message.sendClientId = client.ConnectionId;
            await MessageRoute(message);
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Error("IM", message?.method.ToString() ?? "unknown", ex,
                new { message?.toUserId, message?.sendClientId });
        }
    }

    /// <summary>
    /// 消息通道.
    /// </summary>
    /// <param name="message"></param>
    private async Task MessageRoute(MessageInput message)
    {
        WebSocketClient client = WebSocketConnectionManager.GetSocketById(message.sendClientId);
        if (client == null)
        {
            var err = $"[IMHandler] GetSocketById({message.sendClientId}) returned null";
            Console.WriteLine(err);
            try { File.AppendAllText("D:/JNPF-v52/backend/.claude/imhandler-error.log", err + "\n---\n"); } catch {}
            return;
        }
        if (string.IsNullOrEmpty(client.UserId))
        {
            await SendMessageAsync(client.ConnectionId, new { method = "logout" }.ToJsonString());
            return;
        }

        var tenant = await GetGlobalTenantCache(client.TenantId);
        // 多租户模式下才强制要求 tenant 存在；单租户/tenantId="0" 时默认 SameTime
        if (tenant == null && KeyVariable.MultiTenancy)
        {
            await SendMessageAsync(client.ConnectionId, new { method = "logout", msg = "此账号已在其他地方登录" }.ToJsonString());
            return;
        }

        client.SingleLogin = tenant != null ? (LoginMethod)tenant.SingleLogin : LoginMethod.SameTime;

        if (KeyVariable.MultiTenancy)
        {
            if (tenant != null)
            {
                _sqlSugarClient = _dataBaseManager.GetTenantSqlSugarClient(client.TenantId, tenant);
            }
            else
            {
                await _tenantManager.ChangTenant(_sqlSugarClient, client.TenantId);
            }
        }

        if (string.IsNullOrEmpty(client.HeadIcon))
        {
            UserEntity userEntity = await _sqlSugarClient.Queryable<UserEntity>().FirstAsync(it => it.Id == client.UserId);
            if (userEntity != null)
            {
                client.HeadIcon = "/api/file/Image/userAvatar/" + userEntity.HeadIcon;
                await OnConnected(client.ConnectionId, client);
            }
        }

        switch (message.method)
        {
            // 建立连接
            case MothodType.OnConnection:
                {
                    List<UserOnlineModel> list = await GetOnlineUserList(client.TenantId);
                    if (list == null)
                    {
                        list = new List<UserOnlineModel>();
                    }

                    switch (client.SingleLogin)
                    {
                        case LoginMethod.Single:
                            {
                                UserOnlineModel? user = list.Find(it => it.userId.Equals(client.UserId) && it.isMobileDevice.Equals(client.IsMobileDevice));
                                if (user == null)
                                {
                                    list.Add(new UserOnlineModel()
                                    {
                                        connectionId = client.ConnectionId,
                                        userId = client.UserId,
                                        account = client.Account,
                                        userName = client.UserName,
                                        lastTime = DateTime.Now,
                                        lastLoginIp = client.LoginIpAddress,
                                        tenantId = client.TenantId,
                                        lastLoginPlatForm = client.LoginPlatForm,
                                        isMobileDevice = client.IsMobileDevice,
                                        token = message.token,
                                        onlineTicket = client.onlineTicket,
                                        systemId = message.systemId,
                                        isSeparate = message.isSeparate
                                    });
                                    await SetOnlineUserList(client.TenantId, list);
                                }

                                // 不同浏览器
                                else if (user != null && !user.token.Equals(message.token))
                                {
                                    var onlineUser = WebSocketConnectionManager.GetSocketById(user.connectionId);
                                    if (onlineUser != null)
                                        await SendMessageAsync(onlineUser.ConnectionId, new { method = MessageSendType.logout.ToString(), msg = "此账号已在其他地方登录" }.ToJsonString());
                                    list.RemoveAll((x) => x.connectionId == user.connectionId);

                                    list.Add(new UserOnlineModel()
                                    {
                                        connectionId = client.ConnectionId,
                                        userId = client.UserId,
                                        account = client.Account,
                                        userName = client.UserName,
                                        lastTime = DateTime.Now,
                                        lastLoginIp = client.LoginIpAddress,
                                        tenantId = client.TenantId,
                                        lastLoginPlatForm = client.LoginPlatForm,
                                        isMobileDevice = client.IsMobileDevice,
                                        token = message.token,
                                        onlineTicket = client.onlineTicket,
                                        systemId = message.systemId,
                                        isSeparate = message.isSeparate
                                    });
                                    await SetOnlineUserList(client.TenantId, list);
                                }
                            }

                            break;
                        case LoginMethod.SameTime:
                            {
                                UserOnlineModel? user = list.Find(it => it.token.Equals(message.token));
                                if (user != null)
                                {
                                    WebSocketClient? onlineUser = WebSocketConnectionManager.GetSocketById(user.connectionId);
                                    if (onlineUser != null)
                                        await SendMessageAsync(onlineUser.ConnectionId, new { method = MessageSendType.closeSocket.ToString() }.ToJsonString());
                                    list.RemoveAll((x) => x.connectionId == user.connectionId);
                                }

                                list.Add(new UserOnlineModel()
                                {
                                    connectionId = client.ConnectionId,
                                    userId = client.UserId,
                                    account = client.Account,
                                    userName = client.UserName,
                                    lastTime = DateTime.Now,
                                    lastLoginIp = client.LoginIpAddress,
                                    tenantId = client.TenantId,
                                    lastLoginPlatForm = client.LoginPlatForm,
                                    isMobileDevice = client.IsMobileDevice,
                                    token = message.token,
                                    onlineTicket = client.onlineTicket,
                                    systemId = message.systemId,
                                    isSeparate = message.isSeparate
                                });
                                await SetOnlineUserList(client.TenantId, list);
                            }

                            break;
                    }

                    var onlineUserList = GetAllUserIdFromTenant(client.TenantId);

                    // 获取接收者为当前用户的聊天且未读的信息
                    var imContentList = _sqlSugarClient.Queryable<IMContentEntity>().Where(x => x.ReceiveUserId.Equals(client.UserId) && x.EnabledMark.Equals(0) && (SqlFunc.IsNullOrEmpty(x.DeleteUserId) || x.DeleteUserId != client.UserId)).GroupBy(x => new { x.SendUserId, x.ReceiveUserId }).Select(x => new IMContentEntity
                    {
                        EnabledMark = SqlFunc.AggregateSum(SqlFunc.IIF(x.EnabledMark == 0, 1, 0)),
                        SendUserId = x.SendUserId,
                        ReceiveUserId = x.ReceiveUserId
                    }).ToList();

                    var receiveList = _sqlSugarClient.Queryable<IMContentEntity>().Where(x => x.ReceiveUserId == client.UserId && (SqlFunc.IsNullOrEmpty(x.DeleteUserId) || x.DeleteUserId != client.UserId)).OrderBy(x => x.SendTime, OrderByType.Desc).ToList();
                    var unreadNums = imContentList.Adapt<List<IMUnreadNumModel>>();
                    foreach (var item in unreadNums)
                    {
                        var entity = receiveList.FirstOrDefault(x => x.SendUserId == item.sendUserId);
                        item.defaultMessage = entity?.Content;
                        item.defaultMessageType = entity?.ContentType;
                        item.defaultMessageTime = entity?.SendTime.ToString();
                    }
                    var unreadNoticeCount = await _sqlSugarClient.Queryable<MessageEntity>().CountAsync(x => x.Type == 1 && x.DeleteMark == null && x.UserId == client.UserId && x.IsRead == 0);
                    var unreadMessageCount = await _sqlSugarClient.Queryable<MessageEntity>().CountAsync(x => x.Type == 2 && x.DeleteMark == null && x.UserId == client.UserId && x.IsRead == 0);
                    var unreadSystemMessageCount = await _sqlSugarClient.Queryable<MessageEntity>().CountAsync(x => x.Type == 3 && x.DeleteMark == null && x.UserId == client.UserId && x.IsRead == 0);
                    var unreadScheduleCount = await _sqlSugarClient.Queryable<MessageEntity>().CountAsync(x => x.Type == 4 && x.DeleteMark == null && x.UserId == client.UserId && x.IsRead == 0);
                    var unreadTotalCount = await _sqlSugarClient.Queryable<MessageEntity>().CountAsync(x => x.DeleteMark == null && x.UserId == client.UserId && x.IsRead == 0);
                    var messageDefault = await _sqlSugarClient.Queryable<MessageEntity>().Where(x => x.DeleteMark == null && x.UserId == client.UserId).OrderBy(x => x.LastModifyTime, OrderByType.Desc).FirstAsync();
                    var messageDefaultText = messageDefault == null ? string.Empty : messageDefault.Title;
                    var messageDefaultTime = messageDefault == null ? DateTime.Now : messageDefault.CreatorTime;
                    await SendMessageAsync(client.ConnectionId, new { method = MessageSendType.initMessage.ToString(), onlineUserList, unreadNums, unreadNoticeCount, unreadMessageCount, messageDefaultText, messageDefaultTime, unreadSystemMessageCount, unreadScheduleCount, unreadTotalCount }.ToJsonString());

                    await SendMessageToTenantAsync(client.TenantId, new { method = MessageSendType.online.ToString(), userId = client.UserId }.ToJsonString(), client.ConnectionId);
                }

                break;

            // 发送消息
            case MothodType.SendMessage:
                {
                    string toUserId = message.toUserId;
                    MessageReceiveType messageType = message.messageType;
                    object messageContent = message.messageContent;
                    string fileName = string.Empty;

                    // Select only needed columns to avoid F_ENABLED_MARK issue on missing column
                    var toUserEntity = await _sqlSugarClient.Queryable<UserEntity>()
                        .Where(it => it.Id == toUserId)
                        .Select(it => new { it.Account, it.HeadIcon, it.RealName })
                        .FirstAsync();

                    // 将发送消息对象信息补全
                    var toAccount = toUserEntity.Account;
                    var toHeadIcon = toUserEntity.HeadIcon;
                    var toRealName = toUserEntity.RealName;

                    var entity = new IMContentEntity();
                    var toMessage = new object();
                    switch (messageType)
                    {
                        case MessageReceiveType.text:
                            entity = CreateIMContent(client.UserId, toUserId, messageContent.ToString(), messageType.ToString());
                            break;
                        case MessageReceiveType.image:
                            {
                                var directoryPath = FileVariable.IMContentFilePath;
                                if (!Directory.Exists(directoryPath))
                                    Directory.CreateDirectory(directoryPath);
                                var imageInput = messageContent.ToObject<MessagetImageInput>();
                                fileName = fileName = imageInput.name;

                                toMessage = new { path = "/api/file/Image/IM/" + fileName, width = imageInput.width, height = imageInput.height };

                                entity = CreateIMContent(client.UserId, toUserId, toMessage.ToJsonString(), messageType.ToString());
                            }

                            break;
                        case MessageReceiveType.voice:
                            var voiceInput = messageContent.ToObject<MessageVoiceInput>();
                            toMessage = new { path = "/api/file/Image/IM/" + voiceInput.name, length = voiceInput.length };
                            entity = CreateIMContent(client.UserId, toUserId, toMessage.ToJsonString(), messageType.ToString());
                            break;
                    }

                    // 写入到会话表中
                    if (await _sqlSugarClient.Queryable<ImReplyEntity>().AnyAsync(it => it.UserId == client.UserId && it.ReceiveUserId == toUserId))
                    {
                        var imReplyEntity = await _sqlSugarClient.Queryable<ImReplyEntity>().FirstAsync(it => it.UserId == client.UserId && it.ReceiveUserId == toUserId);
                        imReplyEntity.ReceiveTime = entity.SendTime;
                        await _sqlSugarClient.Updateable(imReplyEntity).ExecuteCommandAsync();
                    }
                    else
                    {
                        var imReplyEntity = new ImReplyEntity()
                        {
                            Id = SnowflakeIdHelper.NextId(),
                            UserId = client.UserId,
                            ReceiveUserId = toUserId,
                            ReceiveTime = entity.SendTime
                        };
                        await _sqlSugarClient.Insertable(imReplyEntity).ExecuteCommandAsync();
                    }

                    await _sqlSugarClient.Insertable(entity).ExecuteCommandAsync();

                    switch (messageType)
                    {
                        case MessageReceiveType.text:
                            await SendMessageAsync(client.ConnectionId, new { method = MessageSendType.sendMessage.ToString(), client.UserId, account = client.Account, headIcon = client.HeadIcon, realName = client.UserName, toAccount, toHeadIcon, messageType = messageType.ToString(), toUserId, toRealName, toMessage = messageContent, dateTime = DateTime.Now, latestDate = DateTime.Now }.ToJsonString());
                            break;
                        case MessageReceiveType.image:
                            var imageInput = messageContent.ToObject<MessagetImageInput>();
                            toMessage = new { path = "/api/file/Image/IM/" + fileName, width = imageInput.width, height = imageInput.height };
                            await SendMessageAsync(client.ConnectionId, new { method = MessageSendType.sendMessage.ToString(), client.UserId, account = client.Account, headIcon = client.HeadIcon, realName = client.UserName, toAccount, toHeadIcon, messageType = messageType.ToString(), toUserId, toMessage, dateTime = DateTime.Now, latestDate = DateTime.Now }.ToJsonString());
                            break;
                        case MessageReceiveType.voice:
                            var voiceInput = messageContent.ToObject<MessageVoiceInput>();
                            toMessage = new { path = "/api/file/Image/IM/" + voiceInput.name, length = voiceInput.length };
                            await SendMessageAsync(client.ConnectionId, new { method = MessageSendType.sendMessage.ToString(), client.UserId, account = client.Account, headIcon = client.HeadIcon, realName = client.UserName, toAccount, toHeadIcon, messageType = messageType.ToString(), toUserId, toMessage, dateTime = DateTime.Now }.ToJsonString());
                            break;
                    }

                    if (WebSocketConnectionManager.GetSocketClientToUserCount(string.Format("{0}-{1}", client.TenantId, toUserId)) > 0)
                    {
                        switch (messageType)
                        {
                            case MessageReceiveType.text:
                                await SendMessageToUserAsync(string.Format("{0}-{1}", client.TenantId, toUserId), new { method = MessageSendType.receiveMessage.ToString(), messageType = messageType.ToString(), formUserId = client.UserId, formMessage = messageContent, dateTime = DateTime.Now, latestDate = DateTime.Now, headIcon = client.HeadIcon, realName = client.UserName, account = client.Account }.ToJsonString());
                                break;
                            case MessageReceiveType.image:
                                var imageInput = messageContent.ToObject<MessagetImageInput>();
                                var formMessage = new { path = "/api/file/Image/IM/" + fileName, width = imageInput.width, height = imageInput.height };
                                await SendMessageToUserAsync(string.Format("{0}-{1}", client.TenantId, toUserId), new { method = MessageSendType.receiveMessage.ToString(), messageType = messageType.ToString(), formUserId = client.UserId, formMessage, dateTime = DateTime.Now, latestDate = DateTime.Now, headIcon = client.HeadIcon, realName = client.UserName, account = client.Account }.ToJsonString());
                                break;
                            case MessageReceiveType.voice:
                                var voiceInput = messageContent.ToObject<MessageVoiceInput>();
                                toMessage = new { path = "/api/file/Image/IM/" + voiceInput.name, length = voiceInput.length };
                                await SendMessageToUserAsync(string.Format("{0}-{1}", client.TenantId, toUserId), new { method = MessageSendType.receiveMessage.ToString(), messageType = messageType.ToString(), formUserId = client.UserId, formMessage = toMessage, dateTime = DateTime.Now, latestDate = DateTime.Now, headIcon = client.HeadIcon, realName = client.UserName, account = client.Account }.ToJsonString());
                                break;
                        }
                    }
                    await GeTuiMessage(toUserId, client);
                }

                break;
            case MothodType.UpdateReadMessage:
                var fromUserId = message.formUserId;
                await _sqlSugarClient.Updateable<IMContentEntity>()
                    .SetColumns(x => new IMContentEntity()
                    {
                        EnabledMark = 1,
                        ReceiveTime = DateTime.Now
                    }).Where(x => x.EnabledMark == 0 && x.SendUserId == fromUserId && x.ReceiveUserId == client.UserId).ExecuteCommandAsync();
                break;
            case MothodType.MessageList:
                var sendUserId = message.toUserId; // 发送者
                var receiveUserId = message.formUserId; // 接收者

                var data = await _sqlSugarClient.Queryable<IMContentEntity>().WhereIF(!string.IsNullOrEmpty(message.keyword), it => it.Content.Contains(message.keyword))
                        .Where(i => (i.SendUserId == message.toUserId && i.ReceiveUserId == message.formUserId) || (i.SendUserId == message.formUserId && i.ReceiveUserId == message.toUserId) && (SqlFunc.IsNullOrEmpty(i.DeleteUserId) || i.DeleteUserId != client.UserId)).OrderBy(it => it.SendTime, message.sord == "asc" ? OrderByType.Asc : OrderByType.Desc)
                        .Select(it => new IMContentListOutput
                        {
                            id = it.Id,
                            sendUserId = it.SendUserId,
                            sendTime = it.SendTime,
                            receiveUserId = it.ReceiveUserId,
                            receiveTime = it.ReceiveTime,
                            content = it.Content,
                            contentType = it.ContentType,
                            state = it.EnabledMark
                        }).ToPagedListAsync(message.currentPage, message.pageSize);

                await SendMessageAsync(client.ConnectionId, new { method = MessageSendType.messageList.ToString(), list = data.list.OrderBy(x => x.sendTime), pagination = data.pagination }.ToJsonString());
                break;
            case MothodType.HeartCheck: break;
        }
    }

    /// <summary>
    /// 获取在线用户列表.
    /// </summary>
    /// <param name="tenantId">租户ID.</param>
    /// <returns></returns>
    public async Task<List<UserOnlineModel>> GetOnlineUserList(string tenantId)
    {
        string cacheKey = string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, tenantId);
        return await _cacheManager.GetAsync<List<UserOnlineModel>>(cacheKey);
    }

    /// <summary>
    /// 保存在线用户列表.
    /// </summary>
    /// <param name="tenantId">租户ID.</param>
    /// <param name="onlineList">在线用户列表.</param>
    /// <returns></returns>
    public async Task<bool> SetOnlineUserList(string tenantId, List<UserOnlineModel> onlineList)
    {
        // 解析在线用户缓存信息(登录地点、浏览器、操作系统等).
        var s_uap = Parser.GetDefault();
        foreach (var item in onlineList)
        {
            item.loginSystem = s_uap.ParseOS(item.lastLoginPlatForm).ToString();
            item.loginBrowser = s_uap.ParseUserAgent(item.lastLoginPlatForm).ToString();
            item.loginAddress = await NetHelper.GetLocation(item.lastLoginIp);
        }
        return await _cacheManager.SetAsync(string.Format("{0}:{1}", CommonConst.CACHEKEYONLINEUSER, tenantId), onlineList);
    }

    /// <summary>
    /// 创建IM内容.
    /// </summary>
    /// <returns></returns>
    private IMContentEntity CreateIMContent(string sendUserId, string receiveUserId, string message, string messageType)
    {
        return new IMContentEntity()
        {
            Id = SnowflakeIdHelper.NextId(),
            SendUserId = sendUserId,
            SendTime = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
            ReceiveUserId = receiveUserId,
            EnabledMark = 0,
            Content = message,
            ContentType = messageType
        };
    }

    private async Task GeTuiMessage(string toUserIds, WebSocketClient client)
    {
        var getuiUrl = "{0}?clientId={1}&title={2}&content={3}1&text={4}&create=true";
        if (toUserIds.Any())
        {
            var clientIdList = await _sqlSugarClient.Queryable<UserDeviceEntity>().Where(x => toUserIds == x.UserId && x.DeleteMark == null).Select(x => x.ClientId).ToListAsync();
            if (clientIdList.Any())
            {
                var clientId = string.Join(",", clientIdList);
                var textDic = new Dictionary<string, string>();
                textDic.Add("type", "3");
                textDic.Add("name", client.UserName + "/" + client.Account);
                textDic.Add("formUserId", client.UserId);
                textDic.Add("headIcon", "/api/File/Image/userAvatar/" + client.HeadIcon);
                getuiUrl = string.Format(getuiUrl, _messageOptions.AppPushUrl, clientId, client.UserName + "/" + client.Account, "您有一条新消息", textDic.ToJsonString());
                await getuiUrl.GetAsStringAsync();
            }
        }
    }

    /// <summary>
    /// 获取全局租户缓存.
    /// </summary>
    /// <returns></returns>
    private async Task<GlobalTenantCacheModel> GetGlobalTenantCache(string tenantId)
    {
        string cacheKey = string.Format("{0}", CommonConst.GLOBALTENANT);
        return (await _cacheManager.GetAsync<List<GlobalTenantCacheModel>>(cacheKey))?.Find(it => it.TenantId.Equals(tenantId));
    }
}