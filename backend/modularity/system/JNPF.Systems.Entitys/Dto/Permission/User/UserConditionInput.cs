using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.User;

/// <summary>
/// 获取用户列表输入.
/// </summary>
[SuppressSniffer]
public class UserConditionInput
{
    /// <summary>
    /// 部门id.
    /// </summary>
    public List<string> departIds { get; set; } = new List<string>();

    /// <summary>
    /// 岗位id.
    /// </summary>
    public List<string> positionIds { get; set; } = new List<string>();

    /// <summary>
    /// 用户id.
    /// </summary>
    public List<string> userIds { get; set; } = new List<string>();

    /// <summary>
    /// 角色Id.
    /// </summary>
    public List<string> roleIds { get; set; } = new List<string>();

    /// <summary>
    /// 分组Id.
    /// </summary>
    public List<string> groupIds { get; set; } = new List<string>();

    /// <summary>
    /// 分页和搜索.
    /// </summary>
    public PageInputBase pagination { get; set; }

    /// <summary>
    /// 分类 ( 组织标识:Organize , 岗位标识:Position , 角色标识:Role , 分组:Group ).
    /// </summary>
    public string type { get; set; }
}