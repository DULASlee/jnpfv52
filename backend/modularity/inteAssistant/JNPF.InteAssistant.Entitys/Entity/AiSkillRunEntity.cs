using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

[SugarTable("ai_skill_runs", TableDescription = "Skill执行审计")]
public class AiSkillRunEntity
{
    [SugarColumn(ColumnName = "F_Id", IsPrimaryKey = true)]
    public string Id { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_TenantId")]
    public string TenantId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_ProjectId")]
    public string ProjectId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_SkillId")]
    public string SkillId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Status")]
    public string Status { get; set; } = "running";

    [SugarColumn(ColumnName = "F_StartedAt")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "F_CompletedAt", IsNullable = true)]
    public DateTime? CompletedAt { get; set; }

    [SugarColumn(ColumnName = "F_TokenConsumed")]
    public long TokenConsumed { get; set; }

    [SugarColumn(ColumnName = "F_ErrorMessage", IsNullable = true)]
    public string? ErrorMessage { get; set; }

    [SugarColumn(ColumnName = "F_Metadata", IsNullable = true)]
    public string? Metadata { get; set; }
}
