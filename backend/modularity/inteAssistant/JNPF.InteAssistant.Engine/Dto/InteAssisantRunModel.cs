using JNPF.DependencyInjection;
using JNPF.InteAssistant.Entitys.Entity;

namespace JNPF.InteAssistant.Engine.Dto;

/// <summary>
/// 集成助手运行模型.
/// </summary>
[SuppressSniffer]
public class InteAssisantRunModel
{
    /// <summary>
    /// 任务数据.
    /// </summary>
    public string TaskData { get; set; }

    /// <summary>
    /// 节点实体.
    /// </summary>
    public List<IntegrateNodeEntity> NodeEntity { get; set; }
}