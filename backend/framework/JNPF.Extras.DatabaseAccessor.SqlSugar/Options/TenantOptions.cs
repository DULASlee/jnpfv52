using JNPF.ConfigurableOptions;

namespace SqlSugar;

/// <summary>
/// 租户配置.
/// </summary>
public sealed class TenantOptions : IConfigurableOptions
{
    /// <summary>
    /// 是否多租户模式.
    /// </summary>
    public bool MultiTenancy { get; set; }

    /// <summary>
    /// 数据隔离类型 SCHEMA:库隔离 COLUMN:字段隔离.
    /// </summary>
    public string MultiTenancyType { get; set; }

    /// <summary>
    /// 多租户数据接口.
    /// </summary>
    public string MultiTenancyDBInterFace { get; set; }

    /// <summary>
    /// 多系统应用支持，系统应用独立
    /// </summary>
    public bool MultiSystem { get; set; }
}