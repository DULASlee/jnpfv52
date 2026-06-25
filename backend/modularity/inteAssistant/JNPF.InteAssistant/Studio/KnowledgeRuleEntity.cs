using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 业务规则实体 (Sprint 3 - S3-3)
/// </summary>
[SugarTable("BASE_KNOWLEDGE_RULE")]
public class KnowledgeRuleEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")]
    public long F_Id { get; set; }

    [SugarColumn(ColumnName = "F_TenantId")]
    public string F_TenantId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Name")]
    public string F_Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Description")]
    public string? F_Description { get; set; }

    [SugarColumn(ColumnName = "F_Type")]
    public string F_Type { get; set; } = "decision-table";

    [SugarColumn(ColumnName = "F_Entity")]
    public string? F_Entity { get; set; }

    [SugarColumn(ColumnName = "F_Fields")]
    public string? F_Fields { get; set; }

    [SugarColumn(ColumnName = "F_Config")]
    public string? F_Config { get; set; }

    [SugarColumn(ColumnName = "F_Source")]
    public string F_Source { get; set; } = "human-created";

    [SugarColumn(ColumnName = "F_Version")]
    public int F_Version { get; set; } = 1;

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
