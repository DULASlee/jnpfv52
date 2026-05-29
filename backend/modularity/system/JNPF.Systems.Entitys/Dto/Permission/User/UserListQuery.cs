using JNPF.Common.Filter;
using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.User;

/// <summary>
/// 用户列表查询输入.
/// </summary>
[SuppressSniffer]
public class UserListQuery : CommonInput
{
    /// <summary>
    /// 机构ID.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 岗位ID.
    /// </summary>
    public string positionId { get; set; }

    /// <summary>
    /// 性别.
    /// </summary>
    public int? gender { get; set; }
}