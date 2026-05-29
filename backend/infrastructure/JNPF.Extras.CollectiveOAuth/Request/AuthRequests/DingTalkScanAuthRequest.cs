using JNPF.Common.Security;
using JNPF.Extras.CollectiveOAuth.Cache;
using JNPF.Extras.CollectiveOAuth.Config;
using JNPF.Extras.CollectiveOAuth.Models;
using JNPF.Extras.CollectiveOAuth.Utils;

namespace JNPF.Extras.CollectiveOAuth.Request;

public class DingTalkScanAuthRequest : DefaultAuthRequest
{
    public DingTalkScanAuthRequest(ClientConfig config) : base(config, new DingTalkScanAuthSource())
    {
    }

    public DingTalkScanAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
        : base(config, new DingTalkScanAuthSource(), authStateCache)
    {
    }


    protected override AuthToken getAccessToken(AuthCallback authCallback)
    {
        var map = new Dictionary<string, object>();
        map.Add("clientId", this.config.clientId);
        map.Add("clientSecret", this.config.clientSecret);
        map.Add("code", authCallback.code);
        map.Add("refreshToken", authCallback.code);
        map.Add("grantType", "authorization_code");
        var head = new Dictionary<string, object>() { { "Content-Type", "application/json" } };
        var response = HttpUtils.RequestPost(source.accessToken(), map.ToJsonString(), head);
        var accessTokenObject = response.parseObject();
        this.checkResponse(accessTokenObject);
        var res = new AuthToken();
        res.accessToken = accessTokenObject["accessToken"].ToString();
        res.refreshToken = accessTokenObject["refreshToken"].ToString();
        res.expireIn = Convert.ToInt16(accessTokenObject["expireIn"]);
        res.openId = accessTokenObject.ContainsKey("openId") ? accessTokenObject["openId"].ToString() : string.Empty;
        return res;
    }

    protected override AuthUser getUserInfo(AuthToken authToken)
    {
        var httpHeader = new Dictionary<string, object>();
        httpHeader.Add("x-acs-dingtalk-access-token", authToken.accessToken);
        httpHeader.Add("Content-Type", "application/json");

        var response = HttpUtils.RequestGet(userInfoUrl(authToken), httpHeader);

        var data = response.parseObject();
        this.checkResponse(data);

        var authUser = new AuthUser();
        authUser.uuid = data.getString("unionId");
        authUser.username = data.getString("nick");
        authUser.nickname = data.getString("nick");
        authUser.email = data.getString("email");
        authUser.token = authToken;
        authUser.source = source.getName();

        authUser.originalUserStr = response;
        return authUser;

    }

    /**
     * 校验请求结果
     *
     * @param response 请求结果
     * @return 如果请求结果正常，则返回JSONObject
     */
    private void checkResponse(Dictionary<string, object> dic)
    {
        if (dic.ContainsKey("errcode") && dic.getInt32("errcode") != 0)
        {
            throw new Exception($"errcode: {dic.getString("errcode")}, errmsg: {dic.getString("errmsg")}");
        }
    }

    /**
     * 返回带{@code state}参数的授权url，授权回调时会带上这个{@code state}
     *
     * @param state state 验证授权流程的参数，可以防止csrf
     * @return 返回授权地址
     * @since 1.9.3
     */
    public override string authorize(string state)
    {
        return UrlBuilder.fromBaseUrl(source.authorize())
            .queryParam("response_type", "code")
            .queryParam("client_id", config.clientId)
            .queryParam("scope", "openid")
            .queryParam("redirect_uri", config.redirectUri)
            .queryParam("prompt", "consent")
            .build();
    }

}