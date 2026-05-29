using JNPF.DependencyInjection;
using System.ComponentModel;

namespace JNPF.Systems.Entitys.Enum;

/// <summary>
/// 系统配置.
/// </summary>
[SuppressSniffer]
public enum SysConfig
{
    /// <summary>
    /// 账号过期时间.
    /// </summary>
    [Description("账号过期时间")]
    TokenTimeout,

    /// <summary>
    /// 验证码数.
    /// </summary>
    [Description("验证码数")]
    verificationCodeNumber,
}