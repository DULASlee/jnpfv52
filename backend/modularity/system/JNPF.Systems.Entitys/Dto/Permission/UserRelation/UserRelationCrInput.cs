using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.UserRelation;

/// <summary>
/// 用户关系创建输入.
/// </summary>
[SuppressSniffer]
public class UserRelationCrInput
{
    /// <summary>
    /// 关系类型.
    /// </summary>
    public string objectType { get; set; }

    /// <summary>
    /// 用户数组.
    /// </summary>
    public List<string> userIds { get; set; }
}
