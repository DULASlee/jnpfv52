using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.OnlineUser;

/// <summary>
/// 在线用户批量下线输入.
/// </summary>
[SuppressSniffer]
public class BatchOnlineInput
{
    /// <summary>
    /// 删除id 列表.
    /// </summary>
    public List<string> ids { get; set; }
}