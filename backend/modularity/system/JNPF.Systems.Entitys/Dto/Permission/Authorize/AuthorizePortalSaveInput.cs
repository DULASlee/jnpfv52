using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.Authorize;

/// <summary>
/// 权限数据保存输入.
/// </summary>
[SuppressSniffer]
public class AuthorizePortalSaveInput
{
    /// <summary>
    /// 授权的门户Id.
    /// </summary>
    public List<string> ids { get; set; }
}