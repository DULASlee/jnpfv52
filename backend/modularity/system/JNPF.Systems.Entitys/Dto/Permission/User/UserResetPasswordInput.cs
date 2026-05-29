using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.User;

/// <summary>
/// 用户重置密码输入.
/// </summary>
[SuppressSniffer]
public class UserResetPasswordInput
{
    /// <summary>
    /// 用户密码.
    /// </summary>
    public string userPassword { get; set; }

    /// <summary>
    /// 验证密码.
    /// </summary>
    public string validatePassword { get; set; }
}