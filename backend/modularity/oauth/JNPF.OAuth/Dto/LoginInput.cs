using JNPF.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace JNPF.OAuth.Dto;

/// <summary>
/// 登录输入参数.
/// </summary>
[SuppressSniffer]
public class LoginInput
{
    /// <summary>
    /// 用户名.
    /// </summary>
    /// <example>13459475357</example>
    [Required(ErrorMessage = "用户名不能为空")]
    public string? account { get; set; }

    /// <summary>
    /// 密码.
    /// </summary>
    /// <example>e10adc3949ba59abbe56e057f20f883e</example>
    //[Required(ErrorMessage = "密码不能为空")]
    public string? password { get; set; }

    /// <summary>
    /// 验证码.
    /// </summary>
    public string? code { get; set; }

    /// <summary>
    /// 验证码时间戳.
    /// </summary>
    public string? timestamp { get; set; }

    /// <summary>
    /// 判断是否需要验证码.
    /// </summary>
    /// <example>password</example>
    public string? origin { get; set; }

    /// <summary>
    ///  租户库信息 第三方登录回调.
    /// </summary>
    public SqlSugar.ConnectionConfigOptions? socialsOptions { get; set; }

    /// <summary>
    /// 是否第三方登录回调.
    /// </summary>
    public bool isSocialsLoginCallBack { get; set; } = false;

    /// <summary>
    /// 未绑定 成功登录后 自动绑定 缓存 Key.
    /// </summary>
    public string jnpf_ticket { get; set; }

    /// <summary>
    /// 单点登录票据.
    /// </summary>
    public string online_ticket { get; set; }

    /// <summary>
    /// 授权类型 : official 官网专用.
    /// </summary>
    public string grant_type { get; set; }
}