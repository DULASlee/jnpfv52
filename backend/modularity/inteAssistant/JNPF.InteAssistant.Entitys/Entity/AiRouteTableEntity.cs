using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// L2 项目路由表（ai_route_table）
/// </summary>
[SugarTable("ai_route_table", TableDescription = "AI项目路由")]
public class AiRouteTableEntity
{
    [SugarColumn(ColumnName = "F_Id", IsPrimaryKey = true)]
    public string Id { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_TenantId")]
    public string TenantId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_ProjectId")]
    public string ProjectId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_SandboxId")]
    public string SandboxId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_SandboxType")]
    public string SandboxType { get; set; } = "shared";

    [SugarColumn(ColumnName = "F_SandboxStatus")]
    public string SandboxStatus { get; set; } = "creating";

    [SugarColumn(ColumnName = "F_SandboxEndpoint", IsNullable = true)]
    public string? SandboxEndpoint { get; set; }

    [SugarColumn(ColumnName = "F_EtcdKey")]
    public string EtcdKey { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "F_LastHeartbeat", IsNullable = true)]
    public DateTime? LastHeartbeat { get; set; }
}
