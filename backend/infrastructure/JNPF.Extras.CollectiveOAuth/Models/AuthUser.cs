using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Models;

/// <summary>
/// 授权用户信息.
/// </summary>
public class AuthUser
{
    /// <summary>
    /// 用户第三方系统的唯一id。在调用方集成改组件时，可以用uuid + source唯一确定一个用户.
    /// <para>@since 1.3.3.</para>
    /// </summary>
    public string uuid { get; set; }

    /// <summary>
    /// 用户名.
    /// </summary>
    public string username { get; set; }

    /// <summary>
    /// 用户昵称.
    /// </summary>
    public string nickname { get; set; }

    /// <summary>
    /// 用户头像.
    /// </summary>
    public string avatar { get; set; }

    /// <summary>
    /// 用户网址.
    /// </summary>
    public string blog { get; set; }

    /// <summary>
    /// 所在公司.
    /// </summary>
    public string company { get; set; }

    /// <summary>
    /// 位置.
    /// </summary>
    public string location { get; set; }

    /// <summary>
    /// 用户邮箱.
    /// </summary>
    public string email { get; set; }

    /// <summary>
    /// 用户备注（各平台中的用户个人介绍）.
    /// </summary>
    public string remark { get; set; }

    /// <summary>
    /// 性别.
    /// </summary>
    public AuthUserGender gender { get; set; }

    /// <summary>
    /// 用户来源.
    /// </summary>
    public string source { get; set; }

    /// <summary>
    /// 用户授权的token信息.
    /// </summary>
    public AuthToken token { get; set; }

    /// <summary>
    /// 原有的用户信息(第三方返回的).
    /// </summary>
    public object originalUser { get; set; }

    public string originalUserStr { get; set; }
}