using JNPF.Common.Core.Manager;
using JNPF.Common.Enums;
using JNPF.Common.Manager;
using JNPF.Common.Models.User;
using JNPF.Common.Options;
using JNPF.Common.Security;
using JNPF.DependencyInjection;
using JNPF.DynamicApiController;
using JNPF.Extras.CollectiveOAuth.Cache;
using JNPF.Extras.CollectiveOAuth.Config;
using JNPF.Extras.CollectiveOAuth.Enums;
using JNPF.Extras.CollectiveOAuth.Models;
using JNPF.Extras.CollectiveOAuth.Request;
using JNPF.Extras.CollectiveOAuth.Utils;
using JNPF.FriendlyException;
using JNPF.Logging.Attributes;
using JNPF.RemoteRequest.Extensions;
using JNPF.Systems.Entitys.Dto.Socials;
using JNPF.Systems.Entitys.Model.Permission.SocialsUser;
using JNPF.Systems.Entitys.Permission;
using JNPF.Systems.Interfaces.Permission;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Web;

namespace JNPF.Systems;

/// <summary>
/// 业务实现:第三方登录.
/// </summary>
[ApiDescriptionSettings(Tag = "Permission", Name = "Socials", Order = 168)]
[Route("api/permission/[controller]")]
public class SocialsUserService : ISocialsUserService, IDynamicApiController, ITransient
{
    /// <summary>
    /// 配置文档.
    /// </summary>
    private readonly SocialsOptions _socialsOptions = App.GetConfig<SocialsOptions>("Socials", true);

    /// <summary>
    /// 基础仓储.
    /// </summary>
    private readonly ISqlSugarRepository<SocialsUsersEntity> _repository;

    /// <summary>
    /// 操作权限服务.
    /// </summary>
    private readonly IAuthorizeService _authorizeService;

    /// <summary>
    /// 缓存管理器.
    /// </summary>
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// 用户管理.
    /// </summary>
    private readonly IUserManager _userManager;

    /// <summary>
    /// 多租户配置选项.
    /// </summary>
    private readonly TenantOptions _tenant;

    /// <summary>
    /// 初始化.
    /// </summary>
    public SocialsUserService(
        ISqlSugarRepository<SocialsUsersEntity> userRepository,
        IAuthorizeService authorizeService,
        ICacheManager cacheManager,
        IOptions<TenantOptions> tenantOptions,
        IUserManager userManager)
    {
        _tenant = tenantOptions.Value;
        _repository = userRepository;
        _authorizeService = authorizeService;
        _cacheManager = cacheManager;
        _userManager = userManager;
    }

    #region GET

    /// <summary>
    /// 获取用户授权列表.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpGet("")]
    public async Task<dynamic> GetList(string userId)
    {
        if (userId.IsNullOrWhiteSpace()) userId = _userManager.UserId;

        if (_socialsOptions.Config == null || !_socialsOptions.Config.Any())
            throw Oops.Oh(ErrorCode.D5025);

        var res = new List<SocialsUserListOutput>();
        var platformInfos = GetPlatFormInfos();
        _socialsOptions.Config.ForEach(item =>
        {
            platformInfos.ForEach(it =>
            {
                if (it["enname"].ToString().ToLower().Equals(item.Provider))
                    res.Add(it.Adapt<SocialsUserListOutput>());
            });
        });

        // 查询绑定信息
        var data = await _repository.AsQueryable().Where(x => x.UserId == userId && x.DeleteMark == null).ToListAsync();

        res.ForEach(item =>
        {
            data.ForEach(it =>
            {
                if (item.enname.Equals(it.SocialType.ToLower())) item.entity = it.Adapt<SocialsUserModel>();
            });
        });

        return res;
    }

    /// <summary>
    /// 重定向第三方登录页面.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    [HttpGet("Render/{source}")]
    [IgnoreLog]
    [NonUnify]
    public async Task<dynamic> Render(string source)
    {
        var authRequest = GetAuthRequest(source, _userManager.UserId, false, null, _userManager.TenantId);
        var res = authRequest.authorize(AuthStateUtils.createState());
        return new { code = 200, msg = res };
    }

    /// <summary>
    /// 获取当前用户信息.
    /// </summary>
    /// <param name="ticket"></param>
    /// <returns></returns>
    [HttpGet("List")]
    public List<SocialsUserListOutput> GetLoginList(string ticket)
    {
        if (!_socialsOptions.SocialsEnabled) return null;

        var platformInfos = GetPlatFormInfos();
        var res = new List<SocialsUserListOutput>();
        _socialsOptions.Config.ForEach(item =>
        {
            platformInfos.ForEach(it =>
            {
                if (it["enname"].ToString().ToLower().Equals(item.Provider))
                {
                    var itModel = it.Adapt<SocialsUserListOutput>();
                    var authRequest = GetAuthRequest(itModel.enname, null, true, ticket, null);
                    itModel.renderUrl = authRequest.authorize(AuthStateUtils.createState());
                    res.Add(itModel);
                }
            });
        });

        return res;
    }

    /// <summary>
    /// 绑定.
    /// </summary>
    /// <returns></returns>
    public async Task<string> Binding([FromQuery] SocialsUserInputModel model)
    {
        var res = new AuthResponse(5001, string.Empty);

        // 微信小程序唤醒登录.
        if (!model.uuid.IsNullOrWhiteSpace())
        {
            AuthUser user = new AuthUser();
            user.uuid = model.uuid;
            user.source = model.source;
            user.username = model.socialName;
            res = new AuthResponse(2000, null, user);
        }
        else
        {
            // 获取第三方请求
            AuthCallbackNew callback = SetAuthCallback(model.code, model.state);

            // 获取第三方请求
            var authRequest = GetAuthRequest(model.source, model.userId, false, null, null);
            res = authRequest.login(callback);
        }

        if (res.ok())
        {
            var resData = res.data.ToObject<AuthUser>();
            var uuid = GetSocialUuid(res);
            var socialsUserEntity = new SocialsUsersEntity();
            socialsUserEntity.UserId = model.userId;
            socialsUserEntity.SocialType = model.source;
            socialsUserEntity.SocialName = resData.username;
            socialsUserEntity.SocialId = uuid;
            if (model.jnpf_ticket.IsNullOrEmpty())
            {
                var sInfo = await _repository.AsQueryable().Where(x => x.UserId.Equals(model.userId) && x.SocialType.Equals(model.source) && x.DeleteMark == null).FirstAsync();
                if (sInfo != null)
                {
                    UserEntity info = await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.Id.Equals(sInfo.UserId)).FirstAsync();
                    return new { code = 201, message = "当前账户已被" + info.RealName + "/" + info.Account + "绑定，不能重复绑定", msg = "当前账户已被" + info.RealName + "/" + info.Account + "绑定，不能重复绑定" }.ToJsonString();
                }
                var resCount = 0;
                if (await _repository.AsQueryable().AnyAsync(x => x.SocialId == model.uuid && x.SocialType.Equals(model.source) && x.DeleteMark == null))
                {
                    resCount = await _repository.AsUpdateable().SetColumns(it => new SocialsUsersEntity
                    {
                        UserId = model.userId,
                        SocialType = model.source,
                        SocialName = resData.username,
                        SocialId = uuid,
                    }).Where(x => x.SocialId == uuid && x.SocialType.Equals(model.source) && x.DeleteMark == null).ExecuteCommandAsync();
                }
                else
                {
                    resCount = await _repository.AsSugarClient().Insertable(socialsUserEntity).CallEntityMethod(m => m.Creator()).ExecuteCommandAsync();
                }

                // 租户开启时-添加租户库绑定数据
                if (_tenant.MultiTenancy && resCount > 0)
                {
                    var info = await _repository.AsSugarClient().Queryable<UserEntity>().FirstAsync(x => x.Id.Equals(model.userId));

                    var param = socialsUserEntity.ToObject<Dictionary<string, string>>();
                    param.Add("tenantId", model.tenantId);
                    param.Add("account", info.Account);
                    param.Add("accountName", info.RealName + "/" + info.Account);

                    var postUrl = _tenant.MultiTenancyDBInterFace + "socials";
                    var result = (await postUrl.SetHeaders(new Dictionary<string, object> {
                        { "X-Forwarded-For", NetHelper.Ip}
                    }).SetBody(param).PostAsStringAsync()).ToObject<Dictionary<string, string>>();

                    if (result == null || "500".Equals(result["code"]) || "400".Equals(result["code"]))
                    {
                        return new { code = 201, message = "用户租户绑定错误!", msg = "用户租户绑定错误!" }.ToJsonString();
                    }
                }
            }

            return new { code = 200, message = "绑定成功!", msg = "绑定成功!", data = socialsUserEntity }.ToJsonString();
        }

        return new { code = 201, message = "第三方回调失败!", msg = "第三方回调失败!" }.ToJsonString();
    }

    #endregion

    #region Post

    /// <summary>
    /// 解绑.
    /// </summary>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [IgnoreLog]
    [NonUnify]
    public async Task<dynamic> DeleteSocials(string id)
    {
        var oidList = _userManager.DataScope.Where(x => x.Edit).Select(x => x.organizeId).ToList();
        var entity = await _repository.AsQueryable().FirstAsync(x => x.Id.Equals(id));
        if (!_userManager.IsAdministrator && !_userManager.UserId.Equals(entity.UserId)
            && !_repository.AsSugarClient().Queryable<UserRelationEntity>().Any(x => oidList.Contains(x.ObjectId) && x.UserId.Equals(entity.UserId) && x.ObjectType.Equals("Organize")))
            return new { code = 500, msg = "没有编辑权限，不能解绑" };

        var res = await _repository.AsUpdateable(entity).CallEntityMethod(m => m.Delete()).UpdateColumns(it => new { it.DeleteMark, it.DeleteTime, it.DeleteUserId })
        .Where(x => x.Id.Equals(id)).ExecuteCommandHasChangeAsync();

        if (res)
        {
            // 多租户开启-解除绑定
            if (_tenant.MultiTenancy)
            {
                var postUrl = string.Format(_tenant.MultiTenancyDBInterFace + "socials?userId={0}&tenantId={1}&socialsType={2}", entity.UserId, _userManager.TenantId, entity.SocialType);
                var result = (await postUrl.SetHeaders(new Dictionary<string, object> {
                    { "X-Forwarded-For", NetHelper.Ip}
                }).DeleteAsStringAsync()).ToObject<Dictionary<string, string>>();
                if (result == null || "500".Equals(result["code"]) || "400".Equals(result["code"]))
                    return new { code = 500, msg = "多租户解绑失败" };
            }

            return new { code = 200, msg = "解绑成功" };
        }

        return new { code = 500, msg = "解绑失败" };
    }

    #endregion

    /// <summary>
    /// 获取第三方登录相关基础信息.
    /// </summary>
    /// <returns></returns>
    public List<Dictionary<string, object>> GetPlatFormInfos()
    {
        var list = new List<List<object>>();
        list.Add(new List<object>() { "WECHAT_OPEN", "微信", "/cdn/socials/wechat_open.png", "绑定微信后，用户可通过微信扫码登录JNPF系统。", string.Empty, "v1.1.0", true, "icon-ym icon-ym-logo-wechat" });
        list.Add(new List<object>() { "QQ", "QQ", "/cdn/socials/qq.png", "绑定QQ后，用户可通过QQ扫码登录JNPF系统。", string.Empty, "v1.1.0", true, "icon-ym icon-ym-logo-qq" });
        list.Add(new List<object>() { "WECHAT_ENTERPRISE", "企业微信", "/cdn/socials/wxWork.png", "绑定企业微信后，您可在网页端扫码登录， 在企业微信应用内和小程序免登录， 并能实时接收小程序通知，沟通和协作将更加便捷。", string.Empty, "v1.10.0", true, "icon-ym icon-ym-logo-wxWork" });
        list.Add(new List<object>() { "DINGTALK", "钉钉", "/cdn/socials/dingtalk.png", "绑定阿里钉钉后，您可在网页端扫码登录并能接收相关通知。", string.Empty, "v1.0.1", true, "icon-ym icon-ym-logo-dingding" });
        list.Add(new List<object>() { "FEISHU", "飞书", "/cdn/socials/feishu.png", "绑定飞书后，用户可扫码登录 JNPF。", string.Empty, "1.15.9", true, "icon-ym icon-ym-logo-feishu" });
        //list.Add(new List<object>() { "GITHUB", "Github", "/cdn/socials/gitHub.png", "绑定GitHub后，用户可扫码登录 JNPF。", string.Empty, "v1.0.1", true, "icon-ym icon-ym-logo-github" });
        //list.Add(new List<object>() { "GITEE", "Gitee", string.Empty, "绑定Gitee后，用户可登录 JNPF。", string.Empty, "v1.0.1", false, "icon-ym icon-ym-logo-github" });
        list.Add(new List<object>() { "wechat_applets", "小程序", "/cdn/socials/wechat_applets.png", "绑定小程序后，用户可扫码登录系统。", string.Empty, "1.15.9", true, "icon-ym icon-ym icon-ym-logo-wechat" });


        var res = new List<Dictionary<string, object>>();
        list.ForEach(item =>
        {
            res.Add(new Dictionary<string, object>()
            {
                { "enname", item[0].ToString().ToLower() },
                { "name", item[1] },
                { "logo", item[2] },
                { "describetion", item[3] },
                { "apiDoc", item[4] },
                { "since", item[5] },
                { "latest", item[6] },
                { "icon", item[7] }
            });
        });

        return res;
    }

    /// <summary>
    /// 获取默认的 Request.
    /// </summary>
    /// <param name="authSource"></param>
    /// <param name="userId"></param>
    /// <param name="isLogin"></param>
    /// <param name="ticket"></param>
    /// <param name="tenantId"></param>
    /// <returns>{@link AuthRequest}.</returns>
    public IAuthRequest GetAuthRequest(string authSource, string userId, bool isLogin, string ticket, string tenantId)
    {
        string addUrlStr = string.Empty;
        string urlStr = string.Format("{0}/api/oauth/Login/socials?source={1}", _socialsOptions.DoMain, authSource);

        if (!userId.IsNullOrWhiteSpace())
            addUrlStr = "&userId=" + userId;
        if (!ticket.IsNullOrWhiteSpace())
            addUrlStr = "&jnpf_ticket=" + ticket;
        if (!_userManager.TenantId.IsNullOrWhiteSpace())
            addUrlStr += "&tenantId=" + _userManager.TenantId;

        var config = _socialsOptions.Config.Find(x => x.Provider.Equals(authSource.ToLower()));
        ClientConfig clientConfig = new ClientConfig();
        clientConfig.clientId = config.ClientId;
        clientConfig.clientSecret = config.ClientSecret;
        clientConfig.agentId = config.AgentId;
        clientConfig.redirectUri = urlStr + addUrlStr;

        IAuthStateCache authStateCache = new DefaultAuthStateCache();
        DefaultAuthSourceEnum authSourceEnum = GlobalAuthUtil.enumFromString<DefaultAuthSourceEnum>(authSource.ToUpper());

        switch (authSourceEnum)
        {
            case DefaultAuthSourceEnum.WECHAT_MP:
                return new WeChatMpAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.WECHAT_OPEN:
                return new WeChatOpenAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.WECHAT_APPLETS:
                return new WeChatAppletsAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.WECHAT_ENTERPRISE:
            //return new WeChatEnterpriseAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.WECHAT_ENTERPRISE_SCAN:
                clientConfig.redirectUri = HttpUtility.UrlEncode(clientConfig.redirectUri);
                return new WeChatEnterpriseScanAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.ALIPAY_MP:
                return new AlipayMpAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.GITEE:
                return new GiteeAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.GITHUB:
                return new GithubAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.BAIDU:
                return new BaiduAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.XIAOMI:
                return new XiaoMiAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.DINGTALK:
                clientConfig.redirectUri = HttpUtility.UrlEncode(clientConfig.redirectUri);
                return new DingTalkScanAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.OSCHINA:
                return new OschinaAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.CODING:
                return new CodingAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.LINKEDIN:
                return new LinkedInAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.WEIBO:
                return new WeiboAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.QQ:
                return new QQAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.DOUYIN:
                return new DouyinAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.GOOGLE:
                return new GoogleAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.FACEBOOK:
                return new FackbookAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.MICROSOFT:
                return new MicrosoftAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.TOUTIAO:
                return new ToutiaoAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.TEAMBITION:
                return new TeambitionAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.RENREN:
                return new RenrenAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.PINTEREST:
                return new PinterestAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.STACK_OVERFLOW:
                return new StackOverflowAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.HUAWEI:
                return new HuaweiAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.KUJIALE:
                return new KujialeAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.GITLAB:
                return new GitlabAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.MEITUAN:
                return new MeituanAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.ELEME:
                return new ElemeAuthRequest(clientConfig, authStateCache);

            case DefaultAuthSourceEnum.FEISHU:
                //clientConfig.redirectUri = HttpUtility.UrlEncode(clientConfig.redirectUri);
                return new FeiShuAuthRequest(clientConfig, authStateCache);

            default:
                return null;
        }
    }

    /// <summary>
    /// 设置第三方code state参数.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public AuthCallbackNew SetAuthCallback(string code, string state)
    {
        AuthCallbackNew callback = new AuthCallbackNew();
        callback.authCode = code;
        callback.auth_code = code;
        callback.authorization_code = code;
        callback.code = code;
        callback.state = state;
        return callback;
    }

    public string GetSocialUuid(AuthResponse res)
    {
        var resData = res.data.ToObject<AuthUser>();
        var uuid = resData.uuid;
        if (resData.token != null && !resData.token.unionId.IsNullOrWhiteSpace())
            uuid = resData.token.unionId;
        return uuid;
    }

    public async Task<SocialsUserInfo> GetUserInfo(string source, string uuid, string socialName)
    {
        var socialsUserInfo = new SocialsUserInfo();
        var userInfo = new UserInfoModel();

        // 查询租户绑定
        if ("wechat_applets".Equals(source)) source = "wechat_open";

        // 多租户
        if (_tenant.MultiTenancy)
        {
            //            JSONObject object = HttpUtil.httpRequest(configValueUtil.getMultiTenancyUrl() + "socials/list?socialsId=" + uuid, "GET", null);
            //            if (object == null || "500".equals(object.get("code").toString()) || "400".equals(object.getString("code")))
            //            {
            //                throw new LoginException("租户绑定信息查询错误！");
            //            }
            //            if ("200".equals(object.get("code").toString()))
            //            {
            //                JSONArray data = JSONArray.parseArray(object.get("data").toString());
            //                int size = data.size();
            //                System.out.println(size);
            //                if (data == null || data.size() == 0)
            //                {
            //                    socialsUserInfo.setSocialUnionid(uuid);
            //                    socialsUserInfo.setSocialName(socialName);
            //                    return socialsUserInfo;
            //                }
            //                else if (data.size() == 1)
            //                {
            //                    //租户开启时-切换租户库
            //                    JSONObject oneUser = (JSONObject)data.get(0);
            //                    setTenantData(oneUser.get("tenantId").toString(), userInfo);
            //                    List<SocialsUserEntity> list = socialsUserService.getUserIfnoBySocialIdAndType(uuid, source);
            //                    if (CollectionUtil.isEmpty(list))
            //                    {
            //                        throw new LoginException("第三方未绑定账号！");
            //                    }
            //                    UserEntity infoById = userService.getInfo(list.get(0).getUserId());
            //                    userInfo = JsonUtil.getJsonToBean(infoById, UserInfo.class);
            //                    userInfo.setUserId(infoById.getId());
            //                    userInfo.setUserAccount(oneUser.get("tenantId").toString() + "@" + infoById.getAccount());
            //                    socialsUserInfo.setTenantUserInfo(data);
            //                    socialsUserInfo.setUserInfo(userInfo);
            //                } else {
            //                    socialsUserInfo.setTenantUserInfo(data);
            //                }
            //            }
        }
        else
        {
            // 查询绑定
            var sInfo = await _repository.AsQueryable().FirstAsync(x => x.SocialId.Equals(uuid) && x.SocialType.Equals(source));
            if (sInfo != null)
            {
                socialsUserInfo.userInfo = (await _repository.AsSugarClient().Queryable<UserEntity>().Where(x => x.Id.Equals(sInfo.UserId)).FirstAsync()).Adapt<UserInfoModel>();
            }
            else
            {
                socialsUserInfo.socialUnionid = uuid;
                socialsUserInfo.socialName = socialName;
            }
        }

        return socialsUserInfo;
    }

    public async Task<dynamic> GetSocialsUserInfo([FromQuery] SocialsUserInputModel model)
    {
        // 获取第三方请求
        AuthCallbackNew callback = SetAuthCallback(model.code, model.state);
        var authRequest = GetAuthRequest(model.source, null, false, null, null);
        var res = authRequest.login(callback);
        if (AuthResponseStatus.FAILURE.GetCode() == res.code)
            throw Oops.Oh("连接失败！");
        else if (AuthResponseStatus.SUCCESS.GetCode() != res.code)
            throw Oops.Oh("授权失败:" + res.msg);

        // 登录用户第三方id
        string uuid = GetSocialUuid(res);
        var resData = res.data.ToObject<AuthUser>();
        var socialName = resData.username.IsNullOrWhiteSpace() ? resData.nickname : resData.username;
        return await GetUserInfo(model.source, uuid, socialName);
    }

}