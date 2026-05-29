using JNPF.Common.Core.Job;
using JNPF.DependencyInjection;

namespace JNPF.Common.Core.Manager.Job;

/// <summary>
/// 作业信息模型.
/// </summary>
[SuppressSniffer]
public class JobDetailModel : JNPFHttpJobMessage
{
    /// <summary>
    /// 作业ID.
    /// </summary>
    public string JobId { get; set; }

    /// <summary>
    /// 作业组名称.
    /// </summary>
    public string GroupName { get; set; }

    /// <summary>
    /// 描述信息.
    /// </summary>
    public string Description { get; set; }
}