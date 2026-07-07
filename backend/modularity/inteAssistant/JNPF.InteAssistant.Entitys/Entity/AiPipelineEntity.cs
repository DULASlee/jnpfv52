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
    /// 项目 ID(FK → ai_projects.F_Id)
    /// 解除 pipeline≡project 绑定,支持一个 Project 下多个 Pipeline(迭代/BUG 修复)
    /// 首次创建时 ProjectId = PipelineId(自锚定);二次开发时继承原始 PipelineId
    /// </summary>
    [SugarColumn(ColumnName = "F_PROJECT_ID")]
    public string ProjectId { get; set; } = "";

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

    // ─── 冻结/恢复(checkpoint)— 支持开发任务对话冻结与重新拉起 ───

    /// <summary>是否已冻结(0=运行中,1=已冻结)</summary>
    [SugarColumn(ColumnName = "F_FROZEN")]
    public bool Frozen { get; set; } = false;

    /// <summary>冻结时间</summary>
    [SugarColumn(ColumnName = "F_FROZEN_AT")]
    public DateTime? FrozenAt { get; set; }

    /// <summary>冻结操作人</summary>
    [SugarColumn(ColumnName = "F_FROZEN_BY")]
    public string? FrozenBy { get; set; }

    /// <summary>冻结原因(如"用户离开""BUG修复中途暂停""等待人工介入")</summary>
    [SugarColumn(ColumnName = "F_FROZEN_REASON")]
    public string? FrozenReason { get; set; }

    /// <summary>累计恢复次数(运维指标)</summary>
    [SugarColumn(ColumnName = "F_RESUME_COUNT")]
    public int ResumeCount { get; set; } = 0;

    /// <summary>最后恢复时间</summary>
    [SugarColumn(ColumnName = "F_LAST_RESUMED_AT")]
    public DateTime? LastResumedAt { get; set; }

    /// <summary>
    /// 全量 checkpoint JSON。结构:{currentStage, stages[], lastMessageIds[], irVersion, irSnapshot}
    /// 冻结时序列化,恢复时反序列化重建内存状态
    /// </summary>
    [SugarColumn(ColumnName = "F_CHECKPOINT", ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? Checkpoint { get; set; }

    /// <summary>任务意图：greenfield / bugfix / enhancement</summary>
    [SugarColumn(ColumnName = "F_WORK_MODE")]
    public string WorkMode { get; set; } = PipelineWorkMode.Greenfield;

    /// <summary>关联源流水线（bugfix / 二次开发）</summary>
    [SugarColumn(ColumnName = "F_SOURCE_PIPELINE_ID", IsNullable = true)]
    public string? SourcePipelineId { get; set; }

    /// <summary>Debug 目标页面路由</summary>
    [SugarColumn(ColumnName = "F_TARGET_PAGE_ROUTE", IsNullable = true)]
    public string? TargetPageRoute { get; set; }

    /// <summary>Debug 目标页面显示名</summary>
    [SugarColumn(ColumnName = "F_TARGET_PAGE_LABEL", IsNullable = true)]
    public string? TargetPageLabel { get; set; }
}
