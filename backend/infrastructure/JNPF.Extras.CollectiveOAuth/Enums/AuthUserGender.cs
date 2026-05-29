using System.ComponentModel;

namespace JNPF.Extras.CollectiveOAuth.Enums;

/// <summary>
/// 授权用户性别.
/// </summary>
public enum AuthUserGender
{
    [Description("男")]
    MALE = 1,
    [Description("女")]
    FEMALE = 0,
    [Description("未知")]
    UNKNOWN = -1
}