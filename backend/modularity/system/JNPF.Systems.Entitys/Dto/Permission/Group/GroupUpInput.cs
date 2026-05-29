using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Group;

/// <summary>
/// 修改分组输入.
/// </summary>
[SuppressSniffer]
public class GroupUpInput : GroupCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }
}