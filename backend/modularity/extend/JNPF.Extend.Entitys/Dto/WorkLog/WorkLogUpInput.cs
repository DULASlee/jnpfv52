using JNPF.DependencyInjection;
using JNPF.Extend.Entitys.Dto.WorkLog;

namespace JNPF.Extend.Entitys.Dto.WoekLog;

/// <summary>
/// 
/// </summary>
[SuppressSniffer]
public class WorkLogUpInput : WorkLogCrInput
{
    /// <summary>
    /// id.
    /// </summary>
    public string? id { get; set; }
}
