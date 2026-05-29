using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Authorize;

/// <summary>
/// 权限数据查询输入.
/// </summary>
[SuppressSniffer]
public class AuthorizeDataQuery
{
    /// <summary>
    /// 类型.
    /// </summary>
    public string type { get; set; }

    /// <summary>
    /// 菜单ids.
    /// </summary>
    public string moduleIds { get; set; }
}