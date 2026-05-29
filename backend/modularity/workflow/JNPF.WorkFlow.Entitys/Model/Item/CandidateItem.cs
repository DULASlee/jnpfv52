using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model.Item;

[SuppressSniffer]
public class CandidateItem
{
    /// <summary>
    /// 用户id.
    /// </summary>
    public string? userId { get; set; }

    /// <summary>
    /// 用户名.
    /// </summary>
    public string? userName { get; set; }
}
