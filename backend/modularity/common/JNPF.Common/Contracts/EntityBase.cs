using JNPF.DependencyInjection;
using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Common.Contracts;

/// <summary>
/// 实体类基类.
/// </summary>
[SuppressSniffer]
public abstract class EntityBase<TKey> : ITenantFilter,IZxSystemFilter, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 获取或设置 编号.
    /// </summary>
    [SugarColumn(ColumnName = "F_ID", ColumnDescription = "主键", IsPrimaryKey = true)]
    public TKey Id { get; set; }

    /// <summary>
    /// 获取或设置 租户id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TENANT_ID", ColumnDescription = "租户id")]
    public string TenantId { get; set; }

    /// <summary>
    /// 系统应用Id
    /// </summary>
    [SugarColumn(ColumnName = "F_ZX_SYSTEM_ID", ColumnDescription = "系统应用id")]
    public string ZxSystemId { get; set; }

}