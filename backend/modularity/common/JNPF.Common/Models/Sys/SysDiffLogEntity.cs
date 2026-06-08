using SqlSugar;

namespace JNPF.Common.Models.Sys;

/// <summary>
/// 数据变更审计日志实体
/// </summary>
[SugarTable("SYS_DIFF_LOG")]
public class SysDiffLogEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_ID")]
    public string Id { get; set; }

    [SugarColumn(ColumnName = "F_TABLE_NAME")]
    public string TableName { get; set; }

    [SugarColumn(ColumnName = "F_DIFF_TYPE")]
    public string DiffType { get; set; }

    [SugarColumn(ColumnName = "F_BEFORE_DATA", ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string BeforeData { get; set; }

    [SugarColumn(ColumnName = "F_AFTER_DATA", ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string AfterData { get; set; }

    [SugarColumn(ColumnName = "F_USER_ID", IsNullable = true)]
    public string UserId { get; set; }

    [SugarColumn(ColumnName = "F_USER_NAME", IsNullable = true)]
    public string UserName { get; set; }

    [SugarColumn(ColumnName = "F_TENANT_ID", IsNullable = true)]
    public string TenantId { get; set; }

    [SugarColumn(ColumnName = "F_TRACE_ID", IsNullable = true)]
    public string TraceId { get; set; }

    [SugarColumn(ColumnName = "F_CREATE_TIME")]
    public DateTime CreateTime { get; set; }
}
