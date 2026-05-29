using JNPF.Common.Security;
using JNPF.DependencyInjection;
using System.Text.Json.Serialization;

namespace JNPF.Systems.Entitys.Dto.Organize;

/// <summary>
/// 机构成员列表输出.
/// </summary>
[SuppressSniffer]
public class OrganizeMemberListOutput : TreeModel
{
    /// <summary>
    /// 名称.
    /// </summary>
    public string fullName { get; set; }

    /// <summary>
    /// 是否超管.
    /// </summary>
    public int isAdministrator { get; set; }

    /// <summary>
    /// 有效标记.
    /// </summary>
    public int? enabledMark { get; set; }

    /// <summary>
    /// 类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 图标.
    /// </summary>
    public string icon { get; set; }

    /// <summary>
    /// 名称.
    /// </summary>
    [JsonIgnore]
    public string RealName { get; set; }

    /// <summary>
    /// 头像.
    /// </summary>
    public string headIcon { get; set; }

    /// <summary>
    /// 所有组织树.
    /// </summary>
    public string organize { get; set; }

    /// <summary>
    /// 组织的组织id 树.
    /// </summary>
    public string organizeIdTree { get; set; }
}