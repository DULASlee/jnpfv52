using SqlSugar;

namespace JNPF.InteAssistant.Studio;

[SugarTable("BASE_AI_AGENT_SKILL")]
public class AgentSkillEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")]
    public long F_Id { get; set; }

    [SugarColumn(ColumnName = "F_AgentId")]
    public long F_AgentId { get; set; }

    [SugarColumn(ColumnName = "F_SkillCode")]
    public string F_SkillCode { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Name")]
    public string F_Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Description")]
    public string? F_Description { get; set; }

    [SugarColumn(ColumnName = "F_SkillType")]
    public string? F_SkillType { get; set; }

    [SugarColumn(ColumnName = "F_Config")]
    public string? F_Config { get; set; }

    [SugarColumn(ColumnName = "F_Enabled")]
    public bool F_Enabled { get; set; } = true;

    [SugarColumn(ColumnName = "F_CreatorTime")]
    public DateTime F_CreatorTime { get; set; }

    [SugarColumn(ColumnName = "F_CreatorUserId")]
    public long? F_CreatorUserId { get; set; }

    [SugarColumn(ColumnName = "F_ModifyTime")]
    public DateTime? F_ModifyTime { get; set; }

    [SugarColumn(ColumnName = "F_ModifyUserId")]
    public long? F_ModifyUserId { get; set; }

    [SugarColumn(ColumnName = "F_DeleteMark")]
    public bool F_DeleteMark { get; set; }
}
