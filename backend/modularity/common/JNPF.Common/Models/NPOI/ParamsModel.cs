using JNPF.DependencyInjection;

namespace JNPF.Common.Models.NPOI;

/// <summary>
/// 参数模型.
/// </summary>
[SuppressSniffer]
public class ParamsModel
{
    /// <summary>
    /// 值.
    /// </summary>
    public string? value { get; set; }

    /// <summary>
    /// 字段.
    /// </summary>
    public string? field { get; set; }
}