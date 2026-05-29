using JNPF.Common.Enums;
using JNPF.DependencyInjection;
using JNPF.JsonSerialization;
using JNPF.Systems.Entitys.Enum;
using Newtonsoft.Json;

namespace JNPF.OAuth.Model;

/// <summary>
/// 登录时需要的系统配置.
/// </summary>
[SuppressSniffer]
public class SysConfigByOAuthModel
{
    /// <summary>
    /// 是否开启验证码（0：不启用，1：启用）.
    /// </summary>
    [JsonConverter(typeof(BoolJsonConverter))]
    public bool enableVerificationCode { get; set; }

    /// <summary>
    /// 错误策略（1：账号锁定，2：延时登录）.
    /// </summary>
    public ErrorStrategy lockType { get; set; }

    /// <summary>
    /// 错误密码次数.
    /// </summary>
    public int passwordErrorsNumber { get; set; } = 6;

    /// <summary>
    /// 延时登录时间(分钟).
    /// </summary>
    public int lockTime { get; set; } = 10;

    /// <summary>
    /// 是否开启白名单验证.
    /// </summary>
    [JsonConverter(typeof(BoolJsonConverter))]
    public bool whitelistSwitch { get; set; }

    /// <summary>
    /// 白名单.
    /// </summary>
    public string whiteListIp { get; set; }

    /// <summary>
    /// 超时登出.
    /// </summary>
    public long tokenTimeout { get; set; }

    /// <summary>
    /// 单一登录方式（1：后登录踢出先登录 2：同时登录）.
    /// </summary>
    public LoginMethod singleLogin { get; set; }
}