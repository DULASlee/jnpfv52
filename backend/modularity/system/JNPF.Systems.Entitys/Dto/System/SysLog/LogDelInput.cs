using JNPF.DependencyInjection;

namespace JNPF.Systems.Entitys.Dto.SysLog;

/// <summary>
/// 日记批量删除输入.
/// </summary>
[SuppressSniffer]
public class LogDelInput
{
    /// <summary>
    /// 删除id.
    /// </summary>
    public string[] ids { get; set; }
}