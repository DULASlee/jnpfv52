using DingTalk.Api;
using DingTalk.Api.Request;
using DingTalk.Api.Response;
using Mapster;
using static DingTalk.Api.Request.OapiRobotSendRequest;
using static DingTalk.Api.Response.OapiV2DepartmentListsubResponse;
using static DingTalk.Api.Response.OapiV2UserListResponse;

namespace JNPF.Extras.Thirdparty.DingDing;

/// <summary>
/// 钉钉.
/// </summary>
public class DingUtil
{
    /// <summary>
    /// 访问令牌.
    /// </summary>
    public string token { get; private set; }

    /// <summary>
    /// 构造函数.
    /// </summary>
    /// <param name="appKey">企业号ID.</param>
    /// <param name="appSecret">凭证密钥.</param>
    public DingUtil(string appKey, string appSecret)
    {
        token = GetDingToken(appKey, appSecret);
    }

    /// <summary>
    /// 构造函数.
    /// </summary>
    /// <param name="appKey">企业号ID.</param>
    /// <param name="appSecret">凭证密钥.</param>
    public DingUtil()
    {
    }

    /// <summary>
    /// 钉钉token.
    /// </summary>
    /// <param name="appKey">企业号ID.</param>
    /// <param name="appSecret">凭证密钥.</param>
    /// <returns></returns>
    public string GetDingToken(string appKey, string appSecret)
    {
        try
        {
            var tokenurl = "https://oapi.dingtalk.com/gettoken";
            DefaultDingTalkClient client = new DefaultDingTalkClient(tokenurl);
            OapiGettokenRequest req = new OapiGettokenRequest();
            req.SetHttpMethod("GET");
            req.Appkey = appKey;
            req.Appsecret = appSecret;
            OapiGettokenResponse response = client.Execute(req);
            if (response.Errcode == 0)
            {
                // 过期时间
                var timeout = DateTime.Now.Subtract(DateTime.Now.AddSeconds(response.ExpiresIn));
                return response.AccessToken;
            }
            else
            {
                throw new Exception("获取钉钉Token失败,失败原因:" + response.Errmsg);
            }

        }
        catch (Exception ex)
        {

            return string.Empty;
        }
    }

    /// <summary>
    /// 钉钉登录.
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public object DingLogin(string code)
    {
        var url = "https://oapi.dingtalk.com/user/getuserinfo";
        DefaultDingTalkClient client = new DefaultDingTalkClient(url);
        OapiUserGetuserinfoRequest req = new OapiUserGetuserinfoRequest();
        req.Code = code;
        req.SetHttpMethod("GET");
        OapiUserGetuserinfoResponse rsp = client.Execute(req, token);
        if (rsp.IsError)
            throw new Exception(rsp.ErrMsg);
        return rsp.Body;
    }

    #region 用户

    /// <summary>
    /// 添加钉钉用户.
    /// </summary>
    /// <param name="dingModel"></param>
    /// <returns></returns>
    public string CreateUser(DingUserParameter dingModel, ref string msg)
    {
        try
        {
            var userId = GetUserIdByMobile(dingModel.Mobile);
            if (!string.IsNullOrEmpty(userId))
                return userId;
            var url = "https://oapi.dingtalk.com/topapi/v2/user/create";
            DefaultDingTalkClient client = new DefaultDingTalkClient(url);
            OapiV2UserCreateRequest req = dingModel.Adapt<OapiV2UserCreateRequest>();
            OapiV2UserCreateResponse rsp = client.Execute(req, token);
            if (!rsp.IsError && rsp.Result != null)
            {
                return rsp.Result.Userid;
            }
            else
            {
                msg = rsp.Errmsg;
                return string.Empty;
            }
        }
        catch (Exception ex)
        {

            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 修改钉钉用户.
    /// </summary>
    /// <param name="dingModel">钉钉用户参数.</param>
    /// <param name="msg">返回信息.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public bool UpdateUser(DingUserParameter dingModel, ref string msg)
    {
        try
        {
            var url = "https://oapi.dingtalk.com/topapi/v2/user/update";
            DefaultDingTalkClient client = new DefaultDingTalkClient(url);
            OapiV2UserUpdateRequest req = dingModel.Adapt<OapiV2UserUpdateRequest>();
            OapiV2UserUpdateResponse rsp = client.Execute(req, token);
            if (rsp.IsError)
                msg = rsp.Errmsg;
            return rsp.Errcode == 0;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 修改钉钉用户.
    /// </summary>
    /// <param name="dingModel"></param>
    /// <param name="msg">返回信息.</param>
    public void DeleteUser(DingUserParameter dingModel, ref string msg)
    {
        try
        {
            var url = "https://oapi.dingtalk.com/topapi/v2/user/delete";
            DefaultDingTalkClient client = new DefaultDingTalkClient(url);
            OapiV2UserDeleteRequest req = dingModel.Adapt<OapiV2UserDeleteRequest>();
            OapiV2UserDeleteResponse rsp = client.Execute(req, token);
            if (rsp.IsError)
            {
                msg = rsp.Errmsg;
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 根据手机号查询用户
    /// 员工离职后，无法再通过手机号获取userId.
    /// </summary>
    /// <param name="mobile"></param>
    /// <returns></returns>
    public string GetUserIdByMobile(string mobile)
    {
        try
        {
            var url = "https://oapi.dingtalk.com/topapi/v2/user/getbymobile";
            DefaultDingTalkClient client = new DefaultDingTalkClient(url);
            OapiV2UserGetbymobileRequest req = new OapiV2UserGetbymobileRequest() { Mobile = mobile };
            OapiV2UserGetbymobileResponse rsp = client.Execute(req, token);
            if (!rsp.IsError && rsp.Result != null)
            {
                return rsp.Result.Userid;
            }
            else
            {
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取所有用户详情.
    /// </summary>
    /// <returns></returns>
    public List<ListUserResponseDomain> GetUserList()
    {
        try
        {
            var url = "https://oapi.dingtalk.com/topapi/v2/user/list";
            DefaultDingTalkClient client = new DefaultDingTalkClient(url);
            var userList = new List<ListUserResponseDomain>();
            var depList = new List<long>();
            depList.Add(1);
            GetDepIdList(1, ref depList);
            if (depList.Count > 0)
            {
                foreach (var item in depList)
                {
                    OapiV2UserListRequest req = new OapiV2UserListRequest() { DeptId = item, Cursor = 0, Size = 9999999 };
                    OapiV2UserListResponse rsp = client.Execute(req, token);
                    if (rsp.Errcode == 0 && rsp.Errmsg == "ok" && rsp.Result != null && rsp.Result.List != null)
                    {
                        userList = userList.Union(rsp.Result.List).ToList();
                    }
                }
            }

            return userList;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
    #endregion

    #region 部门

    /// <summary>
    /// 添加钉钉部门.
    /// </summary>
    /// <param name="dingModel"></param>
    /// <param name="msg">返回信息.</param>
    /// <returns></returns>
    public string CreateDep(DingDepartmentParameter dingModel, ref string msg)
    {
        try
        {
            var url = "https://oapi.dingtalk.com/topapi/v2/department/create";
            DefaultDingTalkClient client = new DefaultDingTalkClient(url);
            OapiV2DepartmentCreateRequest req = dingModel.Adapt<OapiV2DepartmentCreateRequest>();
            OapiV2DepartmentCreateResponse rsp = client.Execute(req, token);
            if (rsp.Errcode == 0)
            {
                if (!rsp.IsError && rsp.Result != null)
                {
                    return rsp.Result.DeptId.ToString();
                }
                else
                {
                    msg = string.Format("【组织{0}创建失败】{1}", dingModel.Name, rsp.Errmsg);
                    return string.Empty;
                }
            }
            else
            {
                msg = string.Format("【组织{0}创建失败】{1}", dingModel.Name, rsp.Errmsg);
                return string.Empty;
            }

        }
        catch (Exception ex)
        {
            msg = string.Format("【组织{0}创建失败】{1}", dingModel.Name, ex.Message);
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 修改钉钉部门.
    /// </summary>
    /// <param name="dingModel"></param>
    /// <param name="msg">返回信息.</param>
    public bool UpdateDep(DingDepartmentParameter dingModel, ref string msg)
    {
        try
        {
            var url = "https://oapi.dingtalk.com/topapi/v2/department/update";
            DefaultDingTalkClient client = new DefaultDingTalkClient(url);
            OapiV2DepartmentUpdateRequest req = dingModel.Adapt<OapiV2DepartmentUpdateRequest>();
            OapiV2DepartmentUpdateResponse rsp = client.Execute(req, token);
            if (!rsp.IsError)
            {
                msg = rsp.IsError ? string.Format("【组织{0}修改失败】{1}", dingModel.Name, rsp.Errmsg) : string.Empty;
                return !rsp.IsError;
            }
            else
            {
                msg = string.Format("【组织{0}修改失败】{1}", dingModel.Name, rsp.Errmsg);
                return false;
            }

        }
        catch (Exception ex)
        {
            msg = string.Format("【组织{0}修改失败】{1}", dingModel.Name, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除钉钉部门.
    /// </summary>
    /// <param name="dingModel"></param>
    /// <param name="msg">返回信息.</param>
    public bool DeleteDep(DingDepartmentParameter dingModel, ref string msg)
    {
        try
        {
            var url = "https://oapi.dingtalk.com/topapi/v2/department/delete";
            DefaultDingTalkClient client = new DefaultDingTalkClient(url);
            OapiV2DepartmentDeleteRequest req = dingModel.Adapt<OapiV2DepartmentDeleteRequest>();
            OapiV2DepartmentDeleteResponse rsp = client.Execute(req, token);
            if (!rsp.IsError)
            {
                msg = rsp.IsError ? rsp.Errmsg : string.Empty;
                return !rsp.IsError;
            }
            else
            {
                msg = rsp.Errmsg;
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 获取部门列表.
    /// </summary>
    /// <returns></returns>
    public List<DeptBaseResponseDomain> GetDepList()
    {
        try
        {
            var depId = new List<long>();
            depId.Add(1);
            GetDepIdList(1, ref depId);
            var list = new List<DeptBaseResponseDomain>();
            if (depId.Count > 0)
            {
                var url = "https://oapi.dingtalk.com/topapi/v2/department/listsub";
                DefaultDingTalkClient client = new DefaultDingTalkClient(url);
                OapiV2DepartmentListsubRequest req = new OapiV2DepartmentListsubRequest();
                foreach (var item in depId)
                {
                    req.DeptId = item;
                    OapiV2DepartmentListsubResponse rsp = client.Execute(req, token);
                    if (rsp.Errcode == 0 && rsp.Errmsg == "ok" && rsp.Result != null)
                    {
                        list = list.Union(rsp.Result).ToList();
                    }
                }
            }
            return list;
        }
        catch (Exception ex)
        {

            throw new Exception(ex.Message);
        }
    }

    /// <summary>
    /// 获取部门id列表.
    /// </summary>
    /// <returns></returns>
    public void GetDepIdList(long depId, ref List<long> ids)
    {
        try
        {
            var url = "https://oapi.dingtalk.com/topapi/v2/department/listsubid";
            DefaultDingTalkClient client = new DefaultDingTalkClient(url);
            OapiV2DepartmentListsubidRequest req = new OapiV2DepartmentListsubidRequest();
            req.DeptId = depId;
            OapiV2DepartmentListsubidResponse rsp = client.Execute(req, token);
            if (rsp.Errcode == 0 && rsp.Errmsg == "ok" && rsp.Result != null)
            {
                ids = ids.Union(rsp.Result.DeptIdList).ToList();
                foreach (var item in rsp.Result.DeptIdList)
                {
                    GetDepIdList(item, ref ids);
                }
            }
        }
        catch (Exception ex)
        {

            throw new Exception(ex.Message);
        }
    }
    #endregion

    /// <summary>
    /// 发送工作消息.
    /// </summary>
    /// <param name="dingModel"></param>
    public void SendWorkMsg(DingWorkMessageParameter dingModel)
    {
        var url = "https://oapi.dingtalk.com/topapi/message/corpconversation/asyncsend_v2";
        DefaultDingTalkClient client = new DefaultDingTalkClient(url);
        OapiMessageCorpconversationAsyncsendV2Request request = new OapiMessageCorpconversationAsyncsendV2Request()
        {
            AgentId = long.Parse(dingModel.agentId),
            UseridList = dingModel.toUsers,
            Msg = dingModel.msg
        };
        request.SetHttpMethod("POST");
        OapiMessageCorpconversationAsyncsendV2Response response = client.Execute(request, token);
        if (!response.Errmsg.Equals("ok"))
        {
            throw new Exception(response.Errmsg);
        }
    }

    /// <summary>
    /// 发送群聊消息.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="content"></param>
    public void SendGroupMsg(string url, string content)
    {
        DefaultDingTalkClient client = new DefaultDingTalkClient(url);
        OapiRobotSendRequest request = new OapiRobotSendRequest();
        request.Msgtype = "text";
        TextDomain text = new TextDomain();
        text.Content = content;
        request.Text_ = text;
        request.SetHttpMethod("POST");
        OapiRobotSendResponse response = client.Execute(request);
        if (!response.Errmsg.Equals("ok"))
        {
            throw new Exception(response.Errmsg);
        }
    }
}