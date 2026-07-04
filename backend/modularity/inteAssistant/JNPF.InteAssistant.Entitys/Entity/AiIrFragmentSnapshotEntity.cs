using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// IR 片段投影视图（ai_ir_fragment_snapshots）
/// </summary>
[SugarTable("ai_ir_fragment_snapshots", TableDescription = "IR片段快照")]
public class AiIrFragmentSnapshotEntity
{
    [SugarColumn(ColumnName = "F_Id", IsPrimaryKey = true)]
    public string Id { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_ProjectId")]
    public string ProjectId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_TenantId")]
    public string TenantId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_FragmentId")]
    public string FragmentId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_FragmentType")]
    public string FragmentType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_CurrentVersion")]
    public int CurrentVersion { get; set; }

    [SugarColumn(ColumnName = "F_StabilityState")]
    public string StabilityState { get; set; } = "draft";

    [SugarColumn(ColumnName = "F_IrContent")]
    public string IrContent { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_SAStepsCompleted", IsNullable = true)]
    public string? SaStepsCompleted { get; set; }

    [SugarColumn(ColumnName = "F_LastEventId")]
    public string LastEventId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_UpdatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "F_DeleteMark")]
    public bool DeleteMark { get; set; }
}
