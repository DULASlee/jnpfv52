namespace JNPF.Schedule;

/// <summary>
/// 作业集群服务上下文
/// </summary>
[SuppressSniffer]
public sealed class JobClusterContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clusterId">作业集群 Id</param>
    internal JobClusterContext(string clusterId)
    {
        ClusterId = clusterId;
    }

    /// <summary>
    /// 作业集群 Id
    /// </summary>
    public string ClusterId { get; }
}