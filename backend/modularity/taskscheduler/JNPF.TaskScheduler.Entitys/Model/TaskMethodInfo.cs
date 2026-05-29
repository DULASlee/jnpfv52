using JNPF.DependencyInjection;

namespace JNPF.TaskScheduler.Entitys.Model;

[SuppressSniffer]
public class TaskMethodInfo
{
    /// <summary>
    /// id.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 任务名称.
    /// </summary>
    public string fullName { get; set; }
}
