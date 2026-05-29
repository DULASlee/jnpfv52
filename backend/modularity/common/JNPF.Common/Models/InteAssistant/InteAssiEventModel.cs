using JNPF.DependencyInjection;

namespace JNPF.Common.Models.InteAssistant;

/// <summary>
/// 集成助手事件模型.
/// </summary>
[SuppressSniffer]
public class InteAssiEventModel : InteAssiDataModel
{
    /// <summary>
    /// 模版ID.
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 触发事件(1.新增 2.修改 3.删除 4-导入 5-批量删除).
    /// </summary>]
    public int TriggerType { get; set; }
}