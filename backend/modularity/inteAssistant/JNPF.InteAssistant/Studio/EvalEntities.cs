using SqlSugar;
namespace JNPF.InteAssistant.Studio;

[SugarTable("BASE_AI_EVAL_GOLDEN_SET")]
public class EvalGoldenSetEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")] public long F_Id { get; set; }
    [SugarColumn(ColumnName = "F_Name")] public string F_Name { get; set; } = string.Empty;
    [SugarColumn(ColumnName = "F_Description")] public string? F_Description { get; set; }
    [SugarColumn(ColumnName = "F_Domain")] public string? F_Domain { get; set; }
    [SugarColumn(ColumnName = "F_TestCaseCount")] public int F_TestCaseCount { get; set; }
    [SugarColumn(ColumnName = "F_Enabled")] public bool F_Enabled { get; set; } = true;
    [SugarColumn(ColumnName = "F_CreatorTime")] public DateTime F_CreatorTime { get; set; }
    [SugarColumn(ColumnName = "F_CreatorUserId")] public long? F_CreatorUserId { get; set; }
    [SugarColumn(ColumnName = "F_ModifyTime")] public DateTime? F_ModifyTime { get; set; }
    [SugarColumn(ColumnName = "F_ModifyUserId")] public long? F_ModifyUserId { get; set; }
    [SugarColumn(ColumnName = "F_DeleteMark")] public int? F_DeleteMark { get; set; }
}

[SugarTable("BASE_AI_EVAL_CASE")]
public class EvalCaseEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")] public long F_Id { get; set; }
    [SugarColumn(ColumnName = "F_SetId")] public long F_SetId { get; set; }
    [SugarColumn(ColumnName = "F_Name")] public string F_Name { get; set; } = string.Empty;
    [SugarColumn(ColumnName = "F_Requirement")] public string F_Requirement { get; set; } = string.Empty;
    [SugarColumn(ColumnName = "F_ExpectedIR")] public string? F_ExpectedIR { get; set; }
    [SugarColumn(ColumnName = "F_Stage")] public int? F_Stage { get; set; }
    [SugarColumn(ColumnName = "F_ScoreThreshold")] public decimal F_ScoreThreshold { get; set; } = 0.8m;
    [SugarColumn(ColumnName = "F_Enabled")] public bool F_Enabled { get; set; } = true;
    [SugarColumn(ColumnName = "F_CreatorTime")] public DateTime F_CreatorTime { get; set; }
    [SugarColumn(ColumnName = "F_CreatorUserId")] public long? F_CreatorUserId { get; set; }
    [SugarColumn(ColumnName = "F_ModifyTime")] public DateTime? F_ModifyTime { get; set; }
    [SugarColumn(ColumnName = "F_ModifyUserId")] public long? F_ModifyUserId { get; set; }
    [SugarColumn(ColumnName = "F_DeleteMark")] public int? F_DeleteMark { get; set; }
}

[SugarTable("BASE_AI_EVAL_RUN")]
public class EvalRunEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")] public long F_Id { get; set; }
    [SugarColumn(ColumnName = "F_SetId")] public long F_SetId { get; set; }
    [SugarColumn(ColumnName = "F_RunAt")] public DateTime F_RunAt { get; set; }
    [SugarColumn(ColumnName = "F_TotalCases")] public int F_TotalCases { get; set; }
    [SugarColumn(ColumnName = "F_PassedCases")] public int F_PassedCases { get; set; }
    [SugarColumn(ColumnName = "F_AverageScore")] public decimal? F_AverageScore { get; set; }
    [SugarColumn(ColumnName = "F_PassRate")] public decimal? F_PassRate { get; set; }
    [SugarColumn(ColumnName = "F_DurationMs")] public long? F_DurationMs { get; set; }
    [SugarColumn(ColumnName = "F_Details")] public string? F_Details { get; set; }
    [SugarColumn(ColumnName = "F_CreatorTime")] public DateTime F_CreatorTime { get; set; }
    [SugarColumn(ColumnName = "F_CreatorUserId")] public long? F_CreatorUserId { get; set; }
}
