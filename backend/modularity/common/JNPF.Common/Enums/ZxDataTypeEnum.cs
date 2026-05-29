using JNPF.DependencyInjection;

namespace JNPF.Common.Enums;

/// <summary>
/// 数据隔离类型
/// </summary>
[SuppressSniffer]
public enum ZxDataTypeEnum
{
    /// <summary>
    /// 业务数据
    /// </summary>
    TenantSystem = 0,

    /// <summary>
    /// 仅租户数据
    /// </summary>
    Tenant = 1,

    /// <summary>
    /// 仅系统数据
    /// </summary>
    System = 2,


    /// <summary>
    /// 无租房与系统数据，低层逻辑使用
    /// </summary>
    Framework = 3,
 
}