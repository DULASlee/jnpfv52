using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

[SugarTable("ai_seed_templates", TableDescription = "领域种子模板")]
public class AiSeedTemplateEntity
{
    [SugarColumn(ColumnName = "F_Id", IsPrimaryKey = true)]
    public string Id { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_TemplateId")]
    public string TemplateId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Industry")]
    public string Industry { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_EventNamePattern")]
    public string EventNamePattern { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_ComplexityHint")]
    public string ComplexityHint { get; set; } = "simple";

    [SugarColumn(ColumnName = "F_CoverageScore")]
    public decimal CoverageScore { get; set; } = 0.80m;

    [SugarColumn(ColumnName = "F_TemplateJson")]
    public string TemplateJson { get; set; } = "{}";

    [SugarColumn(ColumnName = "F_CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "F_DeleteMark")]
    public bool DeleteMark { get; set; }
}
