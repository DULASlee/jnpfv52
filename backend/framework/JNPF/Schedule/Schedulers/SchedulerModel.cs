using System.Text.Json.Serialization;

namespace JNPF.Schedule;

/// <summary>
/// 作业计划模型
/// </summary>
/// <remarks>常用于接口返回或序列化操作</remarks>
[SuppressSniffer]
public sealed class SchedulerModel
{
    /// <summary>
    /// 作业信息
    /// </summary>
    [JsonInclude]
    public JobDetail JobDetail { get; internal set; }

    /// <summary>
    /// 作业触发器
    /// </summary>
    [JsonInclude]
    public Trigger[] Triggers { get; internal set; }
}