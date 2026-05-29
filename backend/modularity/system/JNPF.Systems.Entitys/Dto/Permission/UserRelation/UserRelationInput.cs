using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.UserRelation;

/// <summary>
/// 用户关系输入.
/// </summary>
[SuppressSniffer]
public class UserRelationInput
{
    /// <summary>
    /// 用户列表.
    /// </summary>
    public List<string> ids { get; set; }
}