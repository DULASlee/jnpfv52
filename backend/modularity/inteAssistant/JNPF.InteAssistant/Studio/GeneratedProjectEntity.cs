using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 已生成系统实体 — 映射 BASE_AI_GENERATED_PROJECT 表 (Sprint 1)
/// 流水线完成后写入，供"已生成系统"菜单查询
/// </summary>
[SugarTable("BASE_AI_GENERATED_PROJECT")]
public class GeneratedProjectEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")]
    public long F_Id { get; set; }

    [SugarColumn(ColumnName = "F_TenantId")]
    public string F_TenantId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_UserId")]
    public long F_UserId { get; set; }

    [SugarColumn(ColumnName = "F_ProjectName")]
    public string F_ProjectName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Description")]
    public string? F_Description { get; set; }

    [SugarColumn(ColumnName = "F_PipelineStatus")]
    public string F_PipelineStatus { get; set; } = "stage1";

    [SugarColumn(ColumnName = "F_CurrentStage")]
    public int F_CurrentStage { get; set; } = 1;

    [SugarColumn(ColumnName = "F_SandboxUrl")]
    public string? F_SandboxUrl { get; set; }

    [SugarColumn(ColumnName = "F_SandboxAccount")]
    public string? F_SandboxAccount { get; set; }

    [SugarColumn(ColumnName = "F_SandboxPassword")]
    public string? F_SandboxPassword { get; set; }

    [SugarColumn(ColumnName = "F_SourceZipUrl")]
    public string? F_SourceZipUrl { get; set; }

    [SugarColumn(ColumnName = "F_DeployDocUrl")]
    public string? F_DeployDocUrl { get; set; }

    [SugarColumn(ColumnName = "F_RequirementIR")]
    public string? F_RequirementIR { get; set; }

    [SugarColumn(ColumnName = "F_ArchitectureIR")]
    public string? F_ArchitectureIR { get; set; }

    [SugarColumn(ColumnName = "F_DesignIR")]
    public string? F_DesignIR { get; set; }

    [SugarColumn(ColumnName = "F_FinalIR")]
    public string? F_FinalIR { get; set; }

    [SugarColumn(ColumnName = "F_IsRead")]
    public bool F_IsRead { get; set; }

    [SugarColumn(ColumnName = "F_UpdateCount")]
    public int F_UpdateCount { get; set; }

    [SugarColumn(ColumnName = "F_CreatorTime")]
    public DateTime F_CreatorTime { get; set; }

    [SugarColumn(ColumnName = "F_CreatorUserId")]
    public long? F_CreatorUserId { get; set; }

    [SugarColumn(ColumnName = "F_CreatorUserName")]
    public string? F_CreatorUserName { get; set; }

    [SugarColumn(ColumnName = "F_ModifyTime")]
    public DateTime? F_ModifyTime { get; set; }

    [SugarColumn(ColumnName = "F_ModifyUserId")]
    public long? F_ModifyUserId { get; set; }

    [SugarColumn(ColumnName = "F_ModifyUserName")]
    public string? F_ModifyUserName { get; set; }

    [SugarColumn(ColumnName = "F_DeleteMark")]
    public bool F_DeleteMark { get; set; }
}
