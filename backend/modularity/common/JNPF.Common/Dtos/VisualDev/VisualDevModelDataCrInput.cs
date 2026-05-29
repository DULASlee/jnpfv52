using JNPF.Common.Models.WorkFlow;
using JNPF.DependencyInjection;

namespace JNPF.Common.Dtos.VisualDev;

/// <summary>
/// 在线功能开发数据创建输入.
/// </summary>
[SuppressSniffer]
public class VisualDevModelDataCrInput : FlowTaskOtherModel
{
    /// <summary>
    /// 数据.
    /// </summary>
    public string data { get; set; }

    /// <summary>
    /// 1-保存.
    /// </summary>
    public int status { get; set; }

    /// <summary>
    /// 是否触发集成助手参数
    /// 误删.
    /// </summary>
    public bool isInteAssis { get; set; } = true;

    /// <summary>
    /// 紧急程度.
    /// </summary>
    public int? flowUrgent { get; set; } = 1;
}
