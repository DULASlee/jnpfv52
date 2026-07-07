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

    // ─── P7-E01 Phase7 扩展（2026-07-08）───
    /// <summary>三元组 R12：租户隔离</summary>
    [SugarColumn(ColumnName = "F_TenantId")] public string F_TenantId { get; set; } = string.Empty;
    /// <summary>三元组 R12：项目隔离</summary>
    [SugarColumn(ColumnName = "F_ProjectId")] public string F_ProjectId { get; set; } = string.Empty;
    /// <summary>三元组 R12：流水线隔离</summary>
    [SugarColumn(ColumnName = "F_PipelineId")] public string F_PipelineId { get; set; } = string.Empty;
    /// <summary>关联具体测试用例（pass^k 一致性按 case 聚合）</summary>
    [SugarColumn(ColumnName = "F_CaseId", IsNullable = true)] public long? F_CaseId { get; set; }
    /// <summary>四层评估结果 JSON {l1,l2,l3,l4}</summary>
    [SugarColumn(ColumnName = "F_LayerResults", IsNullable = true)] public string? F_LayerResults { get; set; }
    /// <summary>L1-L3 综合通过（fail-fast 后的整体结论）</summary>
    [SugarColumn(ColumnName = "F_OverallPassed", IsNullable = true)] public bool? F_OverallPassed { get; set; }
    /// <summary>L4 Judge 与人工的 Cohen's kappa（P7-E02 校准写入，P7-E01 预留）</summary>
    [SugarColumn(ColumnName = "F_JudgeKappa", IsNullable = true)] public decimal? F_JudgeKappa { get; set; }
    /// <summary>pass^k 一致性（首版 k=1，预留扩展点）</summary>
    [SugarColumn(ColumnName = "F_Consistency", IsNullable = true)] public decimal? F_Consistency { get; set; }
    /// <summary>eval run 状态：pending/running/completed/failed</summary>
    [SugarColumn(ColumnName = "F_Status")] public string F_Status { get; set; } = "pending";
}
