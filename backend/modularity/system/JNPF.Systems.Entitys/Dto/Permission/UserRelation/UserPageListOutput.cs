using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.UserRelation;

/// <summary>
/// 用户分页列表输出.
/// </summary>
[SuppressSniffer]
public class UserPageListOutput
{
    /// <summary>
    /// 用户id.
    /// </summary>
    public string userId { get; set; }

    /// <summary>
    /// 用户名称.
    /// </summary>
    public string userName { get; set; }
}