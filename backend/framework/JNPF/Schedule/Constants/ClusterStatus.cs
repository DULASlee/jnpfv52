namespace JNPF.Schedule;

/// <summary>
/// 作业集群状态
/// </summary>
[SuppressSniffer]
public enum ClusterStatus : uint
{
    /// <summary>
    /// 宕机
    /// </summary>
    Crashed = 0,

    /// <summary>
    /// 工作中
    /// </summary>
    Working = 1,

    /// <summary>
    /// 等待被唤醒
    /// </summary>
    Waiting = 2
}