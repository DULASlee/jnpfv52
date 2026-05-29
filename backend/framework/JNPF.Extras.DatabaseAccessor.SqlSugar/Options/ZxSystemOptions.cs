using JNPF.ConfigurableOptions;

namespace SqlSugar;

/// <summary>
/// 租户配置.
/// </summary>
public sealed class ZxSystemOptions : IConfigurableOptions
{
    /// <summary>
    /// 是否多系统模式.
    /// </summary>
    public bool MultiSystem { get; set; }

}