using JNPF.Extras.CollectiveOAuth.Enums;

namespace JNPF.Extras.CollectiveOAuth.Models;

/// <summary>
/// 授权响应.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// 授权响应状态码.
    /// </summary>
    public int code { get; set; }

    /// <summary>
    /// 授权响应信息.
    /// </summary>
    public string msg { get; set; }

    /// <summary>
    /// 授权响应数据，当且仅当 code = 2000 时返回.
    /// </summary>
    public object data { get; set; }

    /// <summary>
    /// 是否请求成功.
    /// </summary>
    /// <returns>true或者false.</returns>
    public bool ok()
    {
        return this.code == Convert.ToInt32(AuthResponseStatus.SUCCESS);
    }

    public AuthResponse(int code, string msg, object data = null)
    {
        this.code = code;
        this.msg = msg;
        this.data = data;
    }
}