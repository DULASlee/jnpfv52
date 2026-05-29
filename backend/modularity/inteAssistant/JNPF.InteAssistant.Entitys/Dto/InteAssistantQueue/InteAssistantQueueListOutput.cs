using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistantQueue;

/// <summary>
/// 任务队列列表输出.
/// </summary>
[SuppressSniffer]
public class InteAssistantQueueListOutput
{
    /// <summary>
    /// 队列名称.
    /// </summary>
    public string fullName;

    /// <summary>
    /// 状态.
    /// </summary>
    public int state;

    /// <summary>
    /// 执行时间.
    /// </summary>
    public DateTime? executionTime;
}