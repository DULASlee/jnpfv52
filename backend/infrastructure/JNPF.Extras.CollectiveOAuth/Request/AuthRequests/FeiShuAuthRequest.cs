using JNPF.Common.Security;
using JNPF.Extras.CollectiveOAuth.Cache;
using JNPF.Extras.CollectiveOAuth.Config;
using JNPF.Extras.CollectiveOAuth.Models;
using JNPF.Extras.CollectiveOAuth.Utils;
using System.Web;

namespace JNPF.Extras.CollectiveOAuth.Request;

public class FeiShuAuthRequest : DefaultAuthRequest
{
    public FeiShuAuthRequest(ClientConfig config) : base(config, new FeiShuAuthSource())
    {
    }

    public FeiShuAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
        : base(config, new FeiShuAuthSource(), authStateCache)
    {
    }

    private string getAppAccessToken()
    {
        var cacheKey = this.source.getName() + this.config.clientId;
        var cacheAppAccessToken = this.authStateCache.get(cacheKey);
        if (!cacheAppAccessToken.IsNullOrEmpty())
        {
            return cacheAppAccessToken;
        }
        else
        {
            var url = "https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal/";
            var requestObject = new Dictionary<string, object>();
            requestObject.Add("app_id", this.config.clientId);
            requestObject.Add("app_secret", this.config.clientSecret);
            var response = HttpUtils.RequestPost(url, requestObject.ToJsonString(), new Dictionary<string, object>() { { "Content-Type", "application/json" } });
            var jsonObject = response.parseObject();
            this.checkResponse(jsonObject);
            var appAccessToken = jsonObject.getString("app_access_token");
            this.authStateCache.cache(cacheKey, appAccessToken, 9 * 1000);
            return appAccessToken;
        }
    }

    protected override AuthToken getAccessToken(AuthCallback authCallback)
    {
        var requestObject = new Dictionary<string, object>();
        requestObject.Add("app_access_token", this.getAppAccessToken());
        requestObject.Add("grant_type", "authorization_code");
        requestObject.Add("code", authCallback.code);

        return this.getToken(requestObject, this.source.accessToken());
    }

    private AuthToken getToken(Dictionary<string, object> param, string url)
    {
        var response = HttpUtils.RequestPost(url, param.ToJsonString(), new Dictionary<string, object>() { { "Content-Type", "application/json" } });
        var jsonObject = response.parseObject();
        this.checkResponse(jsonObject);
        var data = jsonObject["data"].ToObject<Dictionary<string, object>>();
        var authToken = new AuthToken();
        authToken.accessToken = data.getString("access_token");
        authToken.refreshToken = data.getString("refresh_token");
        authToken.expireIn = data.getInt32("expires_in");
        authToken.tokenType = data.getString("token_type");
        authToken.openId = data.getString("open_id");
        return authToken;
    }

    protected override AuthUser getUserInfo(AuthToken authToken)
    {
        var accessToken = authToken.accessToken;
        string response = doGetUserInfo(authToken);
        var jsonObject = response.parseObject();
        this.checkResponse(jsonObject);
        var data = jsonObject["data"].ToObject<Dictionary<string, object>>();

        var authUser = new AuthUser();
        authUser.uuid = data.getString("union_id");
        authUser.username = data.getString("name");
        authUser.nickname = data.getString("name");
        authUser.avatar = data.getString("avatar_url");
        authUser.email = data.getString("email");
        //authUser.location = location;
        //authUser.remark = data.getString("bio");
        //authUser.gender = GlobalAuthUtil.getRealGender(userObj.getString("gender"));
        authUser.token = authToken;
        authUser.source = source.getName();

        authUser.originalUserStr = response;
        return authUser;
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
        config.redirectUri = HttpUtility.UrlEncode(config.redirectUri);
        config.redirectUri = config.redirectUri.Replace("+", "%20").Replace("*", "%2A").Replace("~", "%7E").Replace("/", "%2F");

        return UrlBuilder.fromBaseUrl(source.authorize())
            .queryParam("app_id", this.config.clientId)
            .queryParam("redirect_uri", config.redirectUri)
            .queryParam("state", getRealState(state))
            .build();
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
     * 返回获取accessToken的url
     *
     * @param code 授权码
     * @return 返回获取accessToken的url
     */
    protected override string accessTokenUrl(string code)
    {
        return UrlBuilder.fromBaseUrl(source.accessToken())
            .queryParam("app_id", config.clientId)
            .queryParam("app_secret", config.clientSecret)
            .build();
    }

}


