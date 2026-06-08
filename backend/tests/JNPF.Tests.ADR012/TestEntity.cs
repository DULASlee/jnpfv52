using JNPF.Extras.DatabaseAccessor.SqlSugar.Models;
using SqlSugar;

namespace JNPF.Tests.ADR012;

/// <summary>
/// 测试用实体 — 实现 ITenantFilter，模拟真实租户实体.
/// </summary>
[SugarTable("TEST_TENANT_ENTITY")]
public class TestTenantEntity : ITenantFilter
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_ID")]
    public string Id { get; set; } = "";

    [SugarColumn(ColumnName = "F_TENANT_ID")]
    public string TenantId { get; set; } = "";

    [SugarColumn(ColumnName = "F_NAME")]
    public string Name { get; set; } = "";

    [SugarColumn(ColumnName = "F_VALUE")]
    public int Value { get; set; }
}

/// <summary>
/// 测试用实体 — 不实现 ITenantFilter，模拟非租户实体.
/// </summary>
[SugarTable("TEST_NORMAL_ENTITY")]
public class TestNormalEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_ID")]
    public string Id { get; set; } = "";

    [SugarColumn(ColumnName = "F_NAME")]
    public string Name { get; set; } = "";
}
