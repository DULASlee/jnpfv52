using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Authorize;

/// <summary>
/// 权限数据输出.
/// </summary>
[SuppressSniffer]
public class AuthorizeDataOutput
{
    /// <summary>
    /// 树形结构.
    /// </summary>
    public List<AuthorizeDataModelOutput> list { get; set; }

    /// <summary>
    /// 已选中ID.
    /// </summary>
    public List<string> ids { get; set; }

    /// <summary>
    /// 所有id.
    /// </summary>
    public List<string> all { get; set; }
}
