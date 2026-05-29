using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistantTask;

/// <summary>
/// 任务队列列表输出.
/// </summary>
[SuppressSniffer]
public class InteAssistantTaskListOutput
{
    /// <summary>
    /// 主键ID.
    /// </summary>
    public string id { get; set; }

    /// <summary>
    /// 实例ID.
    /// </summary>
    public string processId { get; set; }

    /// <summary>
    /// 原实例ID.
    /// </summary>
    public string parentId { get; set; }

    /// <summary>
    /// 原实例执行时间.
    /// </summary>
    public DateTime? parentTime { get; set; }

    /// <summary>
    /// 是否重试.
    /// </summary>
    public int isRetry { get; set; }

    /// <summary>
    /// 执行时间.
    /// </summary>
    public DateTime? executionTime { get; set; }

    /// <summary>
    /// 执行结果
    /// 1-成功.
    /// </summary>
    public int resultType { get; set; }
}