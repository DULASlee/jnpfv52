using SqlSugar;
namespace JNPF.InteAssistant.Studio;
[SugarTable("BASE_AI_UI_TEMPLATE")]
public class UiTemplateEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")] public long F_Id { get; set; }
    [SugarColumn(ColumnName = "F_TenantId")] public string F_TenantId { get; set; } = "0";
    [SugarColumn(ColumnName = "F_Name")] public string F_Name { get; set; } = string.Empty;
    [SugarColumn(ColumnName = "F_Description")] public string? F_Description { get; set; }
    [SugarColumn(ColumnName = "F_Category")] public string? F_Category { get; set; }
    [SugarColumn(ColumnName = "F_ThumbnailUrl")] public string? F_ThumbnailUrl { get; set; }
    [SugarColumn(ColumnName = "F_TemplateData")] public string F_TemplateData { get; set; } = string.Empty;
    [SugarColumn(ColumnName = "F_Source")] public string F_Source { get; set; } = "official";
    [SugarColumn(ColumnName = "F_DesignerId")] public long? F_DesignerId { get; set; }
    [SugarColumn(ColumnName = "F_DesignerName")] public string? F_DesignerName { get; set; }
    [SugarColumn(ColumnName = "F_UseCount")] public int F_UseCount { get; set; }
    [SugarColumn(ColumnName = "F_Rating")] public decimal F_Rating { get; set; } = 5.0m;
    [SugarColumn(ColumnName = "F_Enabled")] public bool F_Enabled { get; set; } = true;
    [SugarColumn(ColumnName = "F_CreatorTime")] public DateTime F_CreatorTime { get; set; }
    [SugarColumn(ColumnName = "F_CreatorUserId")] public long? F_CreatorUserId { get; set; }
    [SugarColumn(ColumnName = "F_ModifyTime")] public DateTime? F_ModifyTime { get; set; }
    [SugarColumn(ColumnName = "F_ModifyUserId")] public long? F_ModifyUserId { get; set; }
    [SugarColumn(ColumnName = "F_DeleteMark")] public int? F_DeleteMark { get; set; }
}
