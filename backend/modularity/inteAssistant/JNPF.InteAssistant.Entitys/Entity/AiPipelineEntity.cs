using JNPF.Common.Contracts;
using JNPF.InteAssistant.Entitys.Enum;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// AI 五阶段流水线
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-12
/// </summary>
[SugarTable("BASE_AI_PIPELINE", TableDescription = "AI五阶段流水线")]
public class AiPipelineEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 流水线名称
    /// </summary>
    [SugarColumn(ColumnName = "F_NAME")]
    public string Name { get; set; }

    /// <summary>
    /// 当前阶段
    /// requirement / architecture / design / development / delivery
    /// 对齐 PipelineStage 常量 + 前端 stages.ts
    /// </summary>
    [SugarColumn(ColumnName = "F_CURRENT_STAGE")]
    public string CurrentStage { get; set; }

    /// <summary>
    /// 运行状态（字符串，兼容旧逻辑）
    /// running / completed / failed
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public string Status { get; set; }

    /// <summary>
    /// 阶段状态（枚举，裁决书新增）
    /// 对齐 PipelineStatus 枚举
    /// </summary>
    [SugarColumn(ColumnName = "F_STAGE_STATUS")]
    public PipelineStatus? StageStatus { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [SugarColumn(ColumnName = "F_STARTED_TIME")]
    public DateTime? StartedTime { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    [SugarColumn(ColumnName = "F_FINISHED_TIME")]
    public DateTime? FinishedTime { get; set; }

    /// <summary>
    /// 校验任务 ID（validating 过渡态时使用）
    /// </summary>
    [SugarColumn(ColumnName = "F_VALIDATION_ID")]
    public string? ValidationId { get; set; }

    /// <summary>
    /// 进入 stale 状态时的阶段
    /// </summary>
    [SugarColumn(ColumnName = "F_STALE_FROM_STAGE")]
    public string? StaleFromStage { get; set; }

    /// <summary>
    /// 否决次数（3 轮否决后升级）
    /// </summary>
    [SugarColumn(ColumnName = "F_REJECT_COUNT")]
    public int RejectCount { get; set; }

    /// <summary>
    /// 放弃时间
    /// </summary>
    [SugarColumn(ColumnName = "F_ABANDONED_AT")]
    public DateTime? AbandonedAt { get; set; }

    /// <summary>
    /// 放弃操作人
    /// </summary>
    [SugarColumn(ColumnName = "F_ABANDONED_BY")]
    public string? AbandonedBy { get; set; }

    /// <summary>
    /// 放弃原因
    /// </summary>
    [SugarColumn(ColumnName = "F_ABANDON_REASON")]
    public string? AbandonReason { get; set; }

    /// <summary>
    /// 进入 stale 的时间
    /// </summary>
    [SugarColumn(ColumnName = "F_STALE_SINCE")]
    public DateTime? StaleSince { get; set; }

    /// <summary>
    /// 失败计数JSON（唯一真源）。格式：{"llm_timeout":2,"compile_failure":0,"sandbox_error":0}
    /// 更新使用SQL JSON_MODIFY原子操作，无并发覆盖风险
    /// </summary>
    [SugarColumn(ColumnName = "F_FAILURE_COUNTS")]
    public string? FailureCounts { get; set; }

    /// <summary>
    /// 进入stale的时间（议题3补充，与F_STALE_SINCE互补）
    /// </summary>
    [SugarColumn(ColumnName = "F_STALE_AT")]
    public DateTime? StaleAt { get; set; }
}
