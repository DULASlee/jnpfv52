using JNPF.DependencyInjection;

namespace JNPF.Common.Models.User;

/// <summary>
/// 岗位信息模型.
/// </summary>
[SuppressSniffer]
public class PositionInfoModel
{
    /// <summary>
    /// 岗位id.
    /// </summary>
    public string? id { get; set; }

    /// <summary>
    /// 岗位名称.
    /// </summary>
    public string? name { get; set; }
}