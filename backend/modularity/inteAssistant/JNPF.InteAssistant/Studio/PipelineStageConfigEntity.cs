using SqlSugar;
namespace JNPF.InteAssistant.Studio;
[SugarTable("BASE_AI_PIPELINE_STAGE_CONFIG")]
public class PipelineStageConfigEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")] public long F_Id { get; set; }
    [SugarColumn(ColumnName = "F_Stage")] public int F_Stage { get; set; }
    [SugarColumn(ColumnName = "F_StageName")] public string F_StageName { get; set; } = string.Empty;
    [SugarColumn(ColumnName = "F_Description")] public string? F_Description { get; set; }
    [SugarColumn(ColumnName = "F_AgentCode")] public string? F_AgentCode { get; set; }
    [SugarColumn(ColumnName = "F_PromptTemplateId")] public long? F_PromptTemplateId { get; set; }
    [SugarColumn(ColumnName = "F_TimeoutSeconds")] public int F_TimeoutSeconds { get; set; } = 300;
    [SugarColumn(ColumnName = "F_RequireConfirm")] public bool F_RequireConfirm { get; set; } = true;
    [SugarColumn(ColumnName = "F_AllowRollback")] public bool F_AllowRollback { get; set; } = true;
    [SugarColumn(ColumnName = "F_Enabled")] public bool F_Enabled { get; set; } = true;
    [SugarColumn(ColumnName = "F_CreatorTime")] public DateTime F_CreatorTime { get; set; }
    [SugarColumn(ColumnName = "F_CreatorUserId")] public long? F_CreatorUserId { get; set; }
    [SugarColumn(ColumnName = "F_ModifyTime")] public DateTime? F_ModifyTime { get; set; }
    [SugarColumn(ColumnName = "F_ModifyUserId")] public long? F_ModifyUserId { get; set; }
    [SugarColumn(ColumnName = "F_DeleteMark")] public bool F_DeleteMark { get; set; }
}
