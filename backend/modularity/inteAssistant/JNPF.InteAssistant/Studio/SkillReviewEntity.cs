using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 人工抽检评审表（P7-E02/E03）。
/// 2026 实践：用于 Judge Cohen's kappa 校准（join Judge 结果与人工 verdict）。
/// 三元组 R12 隔离：F_TenantId + F_ProjectId + F_PipelineId。
/// 同一 skill_run 支持多人独立评分（计算 inter-rater agreement）。
/// </summary>
[SugarTable("BASE_AI_SKILL_REVIEW", TableDescription = "Skill 人工抽检评审")]
public class SkillReviewEntity
{
    [SugarColumn(ColumnName = "F_Id", IsPrimaryKey = true)]
    public long F_Id { get; set; }

    /// <summary>被评审的 skill_run id（ai_skill_runs.F_Id, string/GUID）</summary>
    [SugarColumn(ColumnName = "F_SkillRunId")]
    public string F_SkillRunId { get; set; } = string.Empty;

    /// <summary>关联的 eval run（可选，Judge 校准时与 EvalRun join）</summary>
    [SugarColumn(ColumnName = "F_EvalRunId", IsNullable = true)]
    public long? F_EvalRunId { get; set; }

    [SugarColumn(ColumnName = "F_SkillId")]
    public string F_SkillId { get; set; } = string.Empty;

    /// <summary>评分 0-100（≥60 视为 PASS）</summary>
    [SugarColumn(ColumnName = "F_Score")]
    public int F_Score { get; set; }

    /// <summary>PASS / FAIL（二元，与 Judge 对齐）</summary>
    [SugarColumn(ColumnName = "F_Verdict")]
    public string F_Verdict { get; set; } = "PASS";

    [SugarColumn(ColumnName = "F_Comment", IsNullable = true)]
    public string? F_Comment { get; set; }

    [SugarColumn(ColumnName = "F_ReviewerId", IsNullable = true)]
    public long? F_ReviewerId { get; set; }

    [SugarColumn(ColumnName = "F_ReviewerName", IsNullable = true)]
    public string? F_ReviewerName { get; set; }

    // ─── 三元组 R12 隔离 ───
    [SugarColumn(ColumnName = "F_TenantId")]
    public string F_TenantId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_ProjectId")]
    public string F_ProjectId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_PipelineId")]
    public string F_PipelineId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_CreatorTime")]
    public DateTime F_CreatorTime { get; set; }

    [SugarColumn(ColumnName = "F_ModifyTime", IsNullable = true)]
    public DateTime? F_ModifyTime { get; set; }
}

/// <summary>Judge 校准报告（P7-E02 JudgeCalibrationService 输出）</summary>
public class JudgeCalibrationReport
{
    /// <summary>trusted / untrusted / insufficient_samples</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Cohen's kappa（二元 pass/fail）；<0.6 不可信</summary>
    public double? Kappa { get; set; }

    public int SampleCount { get; set; }

    public int AgreeCount { get; set; }

    public int DisagreeCount { get; set; }

    public string RecommendAction { get; set; } = string.Empty;

    public DateTime? CalibratedAt { get; set; }
}

/// <summary>Judge 校准对（Judge verdict vs Human verdict）</summary>
public class JudgeHumanPair
{
    public bool JudgePassed { get; set; }
    public bool HumanPassed { get; set; }
    public long EvalRunId { get; set; }
}
