using JNPF.Extras.CollectiveOAuth.Cache;
using JNPF.Extras.CollectiveOAuth.Config;
using JNPF.Extras.CollectiveOAuth.Enums;
using JNPF.Extras.CollectiveOAuth.Models;
using JNPF.Extras.CollectiveOAuth.Utils;

namespace JNPF.Extras.CollectiveOAuth.Request;

public partial class WeChatAppletsAuthRequest : DefaultAuthRequest
{
    public WeChatAppletsAuthRequest(ClientConfig config) : base(config, new WechatAppletsAuthSource())
    {
    }

    public WeChatAppletsAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
        : base(config, new WechatAppletsAuthSource(), authStateCache)
    {
    }

    protected override AuthToken getAccessToken(AuthCallback authCallback)
    {
        return null;
    }

    protected override AuthUser getUserInfo(AuthToken authToken)
    {
        return null;
    }

    protected string userInfoUrl(AuthCallback authCallback)
    {
        return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("appid", config.clientId)
                .queryParam("secret", config.clientSecret)
                .queryParam("js_code", authCallback.code)
                .queryParam("grant_type", "authorization_code").build();
    }

    public override AuthResponse login(AuthCallback authCallback)
    {
        try
        {
            AuthUser user = getUserUnionid(authCallback);
            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, user);
        }
        catch (Exception var4)
        {
            return responseError(var4);
        }
    }

    /**
     * 处理{@link AuthDefaultRequest#login(AuthCallback)} 发生异常的情况，统一响应参数
     *
     * @param e 具体的异常
     * @return AuthResponse
     */
    private AuthResponse responseError(Exception e)
    {
        int errorCode = Convert.ToInt32(AuthResponseStatus.FAILURE);
        string errorMsg = e.Message;
        return new AuthResponse(errorCode, errorMsg);
    }

    protected AuthUser getUserUnionid(AuthCallback authCallback)
    {
        var response = HttpUtils.RequestGet(userInfoUrl(authCallback));
        var obj = response.parseObject();
        checkResponse(obj);
        AuthToken authToken = new AuthToken();
        if (obj.ContainsKey("unionid"))
        {
            authToken.unionId = obj["unionid"].ToString();
        }
        var authUser = new AuthUser();
        authUser.uuid = obj.getString("unionId");
        authUser.username = obj.getString("nick");
        authUser.nickname = obj.getString("nick");
        authUser.email = obj.getString("email");
        authUser.token = authToken;
        authUser.source = source.getName();

        authUser.originalUserStr = response;
        return authUser;
    }

    private void checkResponse(Dictionary<string, object> dic)
    {
        if (dic.ContainsKey("errcode"))
        {
            throw new Exception($"errcode: {dic.getString("errcode")}, errmsg: {dic.getString("errmsg")}");
        }
    }
}