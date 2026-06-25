using SqlSugar;

namespace JNPF.InteAssistant.Studio;

[SugarTable("BASE_AI_AGENT_CONFIG")]
public class AgentConfigEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")]
    public long F_Id { get; set; }

    [SugarColumn(ColumnName = "F_AgentCode")]
    public string F_AgentCode { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Name")]
    public string F_Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Description")]
    public string? F_Description { get; set; }

    [SugarColumn(ColumnName = "F_AgentType")]
    public string F_AgentType { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_PromptTemplateId")]
    public long? F_PromptTemplateId { get; set; }

    [SugarColumn(ColumnName = "F_SystemPrompt")]
    public string? F_SystemPrompt { get; set; }

    [SugarColumn(ColumnName = "F_ModelProvider")]
    public string? F_ModelProvider { get; set; }

    [SugarColumn(ColumnName = "F_ModelName")]
    public string? F_ModelName { get; set; }

    [SugarColumn(ColumnName = "F_Temperature")]
    public decimal F_Temperature { get; set; } = 0.7m;

    [SugarColumn(ColumnName = "F_MaxTokens")]
    public int F_MaxTokens { get; set; } = 4096;

    [SugarColumn(ColumnName = "F_Config")]
    public string? F_Config { get; set; }

    [SugarColumn(ColumnName = "F_Enabled")]
    public bool F_Enabled { get; set; } = true;

    [SugarColumn(ColumnName = "F_Sort")]
    public int F_Sort { get; set; }

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
