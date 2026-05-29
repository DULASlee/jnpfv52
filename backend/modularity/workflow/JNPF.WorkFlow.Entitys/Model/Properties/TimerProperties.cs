using JNPF.DependencyInjection;

namespace JNPF.WorkFlow.Entitys.Model.Properties;

[SuppressSniffer]
public class TimerProperties
{
    /// <summary>
    /// 标题.
    /// </summary>
    public string? title { get; set; }

    /// <summary>
    /// 日.
    /// </summary>
    public int day { get; set; }

    /// <summary>
    /// 小时.
    /// </summary>
    public int hour { get; set; }

    /// <summary>
    /// 分钟.
    /// </summary>
    public int minute { get; set; }

    /// <summary>
    /// 秒.
    /// </summary>
    public int second { get; set; }

    /// <summary>
    /// 定时器节点的上一节点编码.
    /// </summary>
    public string? upNodeCode { get; set; }

    /// <summary>
    /// 转向节点.
    /// </summary>
    public string swerveNode { get; set; } = string.Empty;
}
