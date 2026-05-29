using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.UserRelation;

/// <summary>
/// 用户关系列表.
/// </summary>
[SuppressSniffer]
public class UserRelationListOutput
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 用户id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 头像.
    /// </summary>
    public string headIcon { get; set; }

    /// <summary>
    /// 组织.
    /// </summary>
    public string organize { get; set; }
}
