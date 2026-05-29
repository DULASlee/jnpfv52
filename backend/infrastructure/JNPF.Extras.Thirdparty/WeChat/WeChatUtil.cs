using Senparc.Weixin.Work.AdvancedAPIs;
using Senparc.Weixin.Work.AdvancedAPIs.MailList;
using Senparc.Weixin.Work.AdvancedAPIs.MailList.Member;
using Senparc.Weixin.Work.Containers;
using System.Text.Json;

namespace JNPF.Extras.Thirdparty.WeChat;

/// <summary>
/// 企业号微信.
/// </summary>
public class WeChatUtil
{
    /// <summary>
    /// 访问令牌.
    /// </summary>
    public string accessToken { get; private set; }

    /// <summary>
    /// 构造函数.
    /// </summary>
    public WeChatUtil(string corpId, string corpSecret, string appSecret = null)
    {
        try
        {
            if (appSecret != null)
                corpSecret = appSecret;
            accessToken = AccessTokenContainer.TryGetToken(corpId, corpSecret);
        }
        catch (Exception)
        {
        }
    }

    #region 部门

    /// <summary>
    /// 查询部门.
    /// </summary>
    public List<DepartmentList> GetDepartmentList()
    {
        var result = MailListApi.GetDepartmentList(accessToken);
        if (result.errcode == 0)
        {
            var departmentList = result.department;
            //根部门
            var department = result.department.Find(x => x.id == 1);
            departmentList.Remove(department);
            return departmentList.OrderBy(x => x.id).ToList();
        }
        else
        {
            throw new Exception(result.errmsg);
        }
    }

    /// <summary>
    /// 创建部门.
    /// </summary>
    /// <param name="name">部门名称.</param>
    /// <param name="parentId">父亲部门id.</param>
    /// <param name="order">在父部门中的次序.</param>
    /// <param name="msg">返回信息.</param>
    /// <param name="id">部门id.</param>
    /// <param name="timeOut">代理请求超时时间（毫秒）.</param>
    /// <returns></returns>
    public string CreateDepartment(string name, long parentId, int order, ref string msg, int? id = null, int timeOut = 10000)
    {
        try
        {
            var result = MailListApi.CreateDepartment(accessToken, name, parentId);
            if (result.errcode == 0)
            {
                return result.id.ToString();
            }
            else
            {
                msg = result.errmsg;
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            msg = string.Format("【组织{0}创建失败】{1}", name, ex.Message);
            return string.Empty;
        }
    }

    /// <summary>
    /// 编辑部门.
    /// </summary>
    /// <param name="id">部门Id.</param>
    /// <param name="name">部门名称.</param>
    /// <param name="parentId">父亲部门id.</param>
    /// <param name="order">在父部门中的次序.</param>
    /// <param name="msg">返回信息.</param>
    /// <param name="timeOut">代理请求超时时间（毫秒）.</param>
    /// <returns></returns>
    public bool UpdateDepartment(int? id, string name, int? parentId, int? order, ref string msg, int timeOut = 10000)
    {
        try
        {
            var result = MailListApi.UpdateDepartment(accessToken, Convert.ToInt64(id), name);
            if (result.errcode == 0)
            {
                return true;
            }
            else
            {
                msg = result.errmsg;
                return false;
            }
        }
        catch (Exception ex)
        {
            msg = string.Format("【组织{0}修改失败】{1}", name, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 删除部门.
    /// </summary>
    /// <param name="id">部门Id.</param>
    /// <param name="msg">返回信息.</param>
    /// <returns></returns>
    public bool DeleteDepartment(int? id, ref string msg)
    {
        var result = MailListApi.DeleteDepartment(accessToken, Convert.ToInt64(id));
        if (result.errcode == 0)
        {
            return true;
        }
        else
        {
            msg = result.errmsg;
            return false;
        }
    }

    /// <summary>
    /// 获取部门成员.
    /// </summary>
    /// <param name="departmentId">获取的部门id.</param>
    /// <param name="fetchChild">1/0：是否递归获取子部门下面的成员.</param>
    /// <returns></returns>
    public List<GetMemberResult> GetDepartmentMember()
    {
        var userList = new List<GetMemberResult>();
        var result = MailListApi.GetDepartmentMemberInfo(accessToken, 1, 1);
        if (result.errcode == 0 && result.userlist.Count > 0)
        {
            userList = result.userlist;
        }
        return userList;
    }
    #endregion

    #region 用户

    /// <summary>
    /// 获取成员.
    /// </summary>
    /// <param name="userId">用户Id.</param>
    /// <returns></returns>
    public string GetMember(string userId)
    {
        var result = MailListApi.GetMember(accessToken, userId);
        if (result.errcode == 0)
        {
            return JsonSerializer.Serialize(result);
        }
        else
        {
            throw new Exception(result.errmsg);
        }
    }

    /// <summary>
    /// 创建成员.
    /// </summary>
    /// <param name="member">企业用户.</param>
    /// <param name="msg">返回信息.</param>
    /// <param name="timeOut">代理请求超时时间（毫秒）.</param>
    public bool CreateMember(QYMember member, ref string msg, int timeOut = 10000)
    {
        try
        {
            MemberCreateRequest memberCreate = new MemberCreateRequest();
            memberCreate.name = member.name;
            memberCreate.avatar_mediaid = member.avatar_mediaid;
            memberCreate.department = member.department;
            memberCreate.email = member.email;
            memberCreate.enable = member.enable;
            memberCreate.english_name = member.english_name;
            memberCreate.extattr = (Extattr)member.extattr;
            memberCreate.external_profile = (External_Profile)member.external_profile;
            memberCreate.gender = member.gender;
            memberCreate.mobile = member.mobile;
            memberCreate.order = member.order;
            memberCreate.position = member.position;
            memberCreate.telephone = member.telephone;
            memberCreate.userid = member.userid;
            var result = MailListApi.CreateMember(accessToken, memberCreate, timeOut);
            if (result.errcode == 0)
                return true;
            return false;
        }
        catch (Exception ex)
        {
            msg = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 编辑成员.
    /// </summary>
    /// <param name="member">企业用户.</param>
    /// <param name="msg">返回信息.</param>
    /// <param name="timeOut">代理请求超时时间（毫秒）.</param>
    public bool UpdateMember(QYMember member, ref string msg, int timeOut = 10000)
    {
        try
        {
            MemberUpdateRequest memberUpdate = new MemberUpdateRequest();
            memberUpdate.name = member.name;
            memberUpdate.avatar_mediaid = member.avatar_mediaid;
            memberUpdate.department = member.department;
            memberUpdate.email = member.email;
            memberUpdate.enable = member.enable;
            memberUpdate.english_name = member.english_name;
            memberUpdate.extattr = (Extattr)member.extattr;
            memberUpdate.external_profile = (External_Profile)member.external_profile;
            memberUpdate.gender = member.gender;
            memberUpdate.mobile = member.mobile;
            memberUpdate.order = member.order;
            memberUpdate.position = member.position;
            memberUpdate.telephone = member.telephone;
            memberUpdate.userid = member.userid;
            var result = MailListApi.UpdateMember(accessToken, memberUpdate, timeOut);
            if (result.errcode == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            msg = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 删除成员.
    /// </summary>
    /// <param name="userId">成员Id.</param>
    /// <param name="msg">返回信息.</param>
    /// <returns></returns>
    public bool DeleteMember(string userId, string msg = null)
    {
        try
        {
            var result = MailListApi.DeleteMember(accessToken, userId);
            if (result.errcode == 0)
            {
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            msg = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 批量删除成员.
    /// </summary>
    /// <param name="useridlist">成员UserID列表.</param>
    /// <param name="timeOut">代理请求超时时间（毫秒）.</param>
    /// <returns></returns>
    public bool BatchDeleteMember(string[] useridlist, int timeOut = 10000)
    {
        var result = MailListApi.BatchDeleteMember(accessToken, useridlist, timeOut = 10000);
        if (result.errcode == 0)
        {
            return true;
        }
        else
        {
            throw new Exception(result.errmsg);
        }
    }

    /// <summary>
    /// 手机号获取成员.
    /// </summary>
    /// <param name="mobile">手机号码.</param>
    public string GetUserid(string mobile)
    {
        try
        {
            var result = MailListApi.GetUserid(accessToken, mobile, 10000);
            if (result.errcode == 0)
            {
                return result.userid;
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
    #endregion

    #region 消息

    /// <summary>
    /// 发送消息.
    /// </summary>
    /// <param name="agentId">企业应用的id.</param>
    /// <param name="content">消息内容.</param>
    /// <param name="toUser">UserID列表.</param>
    /// <param name="toParty">PartyID列表.</param>
    /// <param name="toTag">TagID列表.</param>
    /// <param name="safe">否是保密消息.</param>
    /// <param name="timeOut">代理请求超时时间（毫秒）.</param>
    /// <returns></returns>
    public async Task SendText(string agentId, string content, string toUser, string toParty = null, string toTag = null, int safe = 0, int timeOut = 10000)
    {
        var result = await MassApi.SendTextAsync(accessToken, agentId, content, toUser, toParty, toTag, safe);
        if (result.errcode != 0)
        {
            throw new Exception(result.errmsg);
        }
    }

    /// <summary>
    ///  发送文件消息.
    /// </summary>
    /// <param name="agentId">企业应用的id.</param>
    /// <param name="mediaId">媒体资源文件ID.</param>
    /// <param name="toUser">UserID列表.</param>
    /// <param name="toParty">PartyID列表.</param>
    /// <param name="toTag">TagID列表.</param>
    /// <param name="safe">否是保密消息.</param>
    /// <param name="timeOut">代理请求超时时间（毫秒）.</param>
    /// <returns></returns>
    public bool SendFile(string agentId, string mediaId, string toUser, string toParty = null, string toTag = null, int safe = 0, int timeOut = 10000)
    {
        var result = MassApi.SendFile(accessToken, agentId, mediaId, toUser, toParty, toTag, safe);
        if (result.errcode == 0)
        {
            return true;
        }
        else
        {
            throw new Exception(result.errmsg);
        }
    }

    /// <summary>
    ///  发送图片消息.
    /// </summary>
    /// <param name="agentId">企业应用的id.</param>
    /// <param name="mediaId">媒体资源文件ID.</param>
    /// <param name="toUser">UserID列表.</param>
    /// <param name="toParty">PartyID列表.</param>
    /// <param name="toTag">TagID列表.</param>
    /// <param name="safe">否是保密消息.</param>
    /// <param name="timeOut">代理请求超时时间（毫秒）.</param>
    public bool SendImage(string agentId, string mediaId, string toUser, string toParty = null, string toTag = null, int safe = 0, int timeOut = 10000)
    {
        var result = MassApi.SendImage(accessToken, agentId, mediaId, toUser, toParty, toTag, safe);
        if (result.errcode == 0)
        {
            return true;
        }
        else
        {
            throw new Exception(result.errmsg);
        }
    }

    /// <summary>
    ///  发送视频消息.
    /// </summary>
    /// <param name="agentId">企业应用的id.</param>
    /// <param name="mediaId">媒体资源文件ID.</param>
    /// <param name="toUser">UserID列表.</param>
    /// <param name="toParty">PartyID列表.</param>
    /// <param name="toTag">TagID列表.</param>
    /// <param name="title"> 视频消息的标题.</param>
    /// <param name="description">视频消息的描述.</param>
    /// <param name="safe">否是保密消息.</param>
    /// <param name="timeOut">代理请求超时时间（毫秒）.</param>
    /// <returns></returns>
    public bool SendVideo(string agentId, string mediaId, string toUser, string toParty = null, string toTag = null, string title = null, string description = null, int safe = 0, int timeOut = 10000)
    {
        var result = MassApi.SendVideo(accessToken, agentId, mediaId, toUser, toParty, toTag, title, description, safe);
        if (result.errcode == 0)
        {
            return true;
        }
        else
        {
            throw new Exception(result.errmsg);
        }
    }

    /// <summary>
    ///  发送语音消息.
    /// </summary>
    /// <param name="agentId">企业应用的id.</param>
    /// <param name="mediaId">媒体资源文件ID.</param>
    /// <param name="toUser">UserID列表.</param>
    /// <param name="toParty">PartyID列表.</param>
    /// <param name="toTag">TagID列表.</param>
    /// <param name="safe">否是保密消息.</param>
    /// <param name="timeOut">代理请求超时时间（毫秒）.</param>
    /// <returns></returns>
    public bool SendVoice(string agentId, string mediaId, string toUser, string toParty = null, string toTag = null, int safe = 0, int timeOut = 10000)
    {
        var result = MassApi.SendVoice(accessToken, agentId, mediaId, toUser, toParty, toTag, safe);
        if (result.errcode == 0)
        {
            return true;
        }
        else
        {
            throw new Exception(result.errmsg);
        }
    }

    #endregion
}