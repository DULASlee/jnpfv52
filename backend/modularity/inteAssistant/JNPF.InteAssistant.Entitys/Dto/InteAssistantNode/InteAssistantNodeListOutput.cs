using JNPF.DependencyInjection;

namespace JNPF.InteAssistant.Entitys.Dto.InteAssistantNode;

/// <summary>
/// 集成助手节点列表输出.
/// </summary>
[SuppressSniffer]
public class InteAssistantNodeListOutput
{
    /// <summary>
    /// 任务ID.
    /// </summary>
    public string taskId { get; set; }

    /// <summary>
    /// 节点编码.
    /// </summary>
    public string nodeCode { get; set; }

    /// <summary>
    /// 节点类型
    /// 新增 删除 修改 .
    /// </summary>
    public string nodeType { get; set; }

    /// <summary>
    /// 节点名称.
    /// </summary>
    public string nodeName { get; set; }

    /// <summary>
    /// 结果类型
    /// 0-失败,1-成功.
    /// </summary>
    public int resultType { get; set; }

    /// <summary>
    /// 错误结果.
    /// </summary>
    public object errorMsg { get; set; }

    /// <summary>
    /// 开始时间.
    /// </summary>
    public DateTime? startTime { get; set; }

    /// <summary>
    /// 结束时间.
    /// </summary>
    public DateTime? endTime { get; set; }

    /// <summary>
    /// 原实例ID.
    /// </summary>
    public string parentId { get; set; }

    /// <summary>
    /// 是否重试.
    /// </summary>
    public bool isRetry { get; set; }

    /// <summary>
    /// 图标类型
    /// 1-常规,0-重试.
    /// </summary>
    public int type { get; set; }

    /// <summary>
    /// 节点ID.
    /// </summary>
    public string id { get; set; }
}