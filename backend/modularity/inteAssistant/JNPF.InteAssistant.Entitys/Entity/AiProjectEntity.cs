using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// AI 项目主表（ai_projects），projectId ≡ pipelineId
/// </summary>
[SugarTable("ai_projects", TableDescription = "AI项目")]
public class AiProjectEntity
{
    [SugarColumn(ColumnName = "F_Id", IsPrimaryKey = true)]
    public string Id { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_TenantId")]
    public string TenantId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_ProjectName")]
    public string ProjectName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Status")]
    public string Status { get; set; } = "requirements";

    [SugarColumn(ColumnName = "F_CurrentPhase")]
    public string CurrentPhase { get; set; } = "pm-skill";

    [SugarColumn(ColumnName = "F_SandboxId", IsNullable = true)]
    public string? SandboxId { get; set; }

    [SugarColumn(ColumnName = "F_SkeletonId", IsNullable = true)]
    public string? SkeletonId { get; set; }

    [SugarColumn(ColumnName = "F_TokenConsumed")]
    public long TokenConsumed { get; set; }

    [SugarColumn(ColumnName = "F_TokenBudget")]
    public long TokenBudget { get; set; } = 500_000;

    [SugarColumn(ColumnName = "F_CreatorUserId")]
    public string CreatorUserId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "F_LastModifyTime", IsNullable = true)]
    public DateTime? LastModifyTime { get; set; }

    [SugarColumn(ColumnName = "F_DeleteMark")]
    public bool DeleteMark { get; set; }

    [SugarColumn(ColumnName = "F_AnalysisCompletedAt", IsNullable = true)]
    public DateTime? AnalysisCompletedAt { get; set; }

    [SugarColumn(ColumnName = "F_Ir0ConfirmedAt", IsNullable = true)]
    public DateTime? Ir0ConfirmedAt { get; set; }

    [SugarColumn(ColumnName = "F_DesignCompletedAt", IsNullable = true)]
    public DateTime? DesignCompletedAt { get; set; }

    /// <summary>阶段五 P5-B03：deploy-skill 验证通过时间。</summary>
    [SugarColumn(ColumnName = "F_DeploymentVerifiedAt", IsNullable = true)]
    public DateTime? DeploymentVerifiedAt { get; set; }

    /// <summary>阶段五 P5-B02：bugfix-skill 最近一次修复时间。</summary>
    [SugarColumn(ColumnName = "F_LastBugfixAt", IsNullable = true)]
    public DateTime? LastBugfixAt { get; set; }

    [SugarColumn(ColumnName = "F_LlmBudgetStatus")]
    public string LlmBudgetStatus { get; set; } = "green";
}
