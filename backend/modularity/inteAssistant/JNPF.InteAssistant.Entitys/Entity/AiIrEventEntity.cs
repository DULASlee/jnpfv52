using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// IR 事件溯源主表（ai_ir_events）
/// </summary>
[SugarTable("ai_ir_events", TableDescription = "IR事件溯源")]
public class AiIrEventEntity
{
    [SugarColumn(ColumnName = "F_Id", IsPrimaryKey = true)]
    public string Id { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_ProjectId")]
    public string ProjectId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_TenantId")]
    public string TenantId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_EventType")]
    public string EventType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_FragmentType", IsNullable = true)]
    public string? FragmentType { get; set; }

    [SugarColumn(ColumnName = "F_FragmentId", IsNullable = true)]
    public string? FragmentId { get; set; }

    [SugarColumn(ColumnName = "F_FragmentVersion")]
    public int FragmentVersion { get; set; } = 1;

    [SugarColumn(ColumnName = "F_Payload")]
    public string Payload { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_SkillId", IsNullable = true)]
    public string? SkillId { get; set; }

    [SugarColumn(ColumnName = "F_SAStepName", IsNullable = true)]
    public string? SaStepName { get; set; }

    [SugarColumn(ColumnName = "F_Sequence", IsIdentity = true)]
    public long Sequence { get; set; }

    [SugarColumn(ColumnName = "F_CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "F_IsRollback")]
    public bool IsRollback { get; set; }
}
