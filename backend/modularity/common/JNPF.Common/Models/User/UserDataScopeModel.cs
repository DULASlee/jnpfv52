using JNPF.DependencyInjection;

namespace JNPF.Common.Models.User;

/// <summary>
/// 用户数据范围集合.
/// </summary>
[SuppressSniffer]
public class UserDataScopeModel
{
    /// <summary>
    /// 机构ID.
    /// </summary>
    public string organizeId { get; set; }

    /// <summary>
    /// 机构类型 默认：组织， System：业务平台， Module：菜单.
    /// </summary>
    public string organizeType { get; set; }

    /// <summary>
    /// 新增.
    /// </summary>
    public bool Add { get; set; }

    /// <summary>
    /// 编辑.
    /// </summary>
    public bool Edit { get; set; }

    /// <summary>
    /// 删除.
    /// </summary>
    public bool Delete { get; set; }

    /// <summary>
    /// 查看.
    /// </summary>
    public bool Select { get; set; }

}