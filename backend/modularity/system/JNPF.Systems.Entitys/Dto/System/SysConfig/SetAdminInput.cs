using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SysConfig;

/// <summary>
/// 赋予超级管理员 输入.
/// </summary>
[SuppressSniffer]
public class SetAdminInput
{
    /// <summary>
    /// 赋予超级管理员 Id 集合.
    /// </summary>
    public List<string> adminIds { get; set; }
}