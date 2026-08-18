using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// AI 流水线消息
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-12
/// </summary>
[SugarTable("BASE_AI_PIPELINE_MESSAGE", TableDescription = "AI流水线消息")]
public class AiPipelineMessageEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 流水线 ID（FK → BASE_AI_PIPELINE）
    /// </summary>
    [SugarColumn(ColumnName = "F_PIPELINE_ID")]
    public string PipelineId { get; set; }

    /// <summary>
    /// 项目 ID(FK → ai_projects.F_Id)
    /// 三元组:tenantId + projectId + pipelineId,NOT NULL
    /// </summary>
    [SugarColumn(ColumnName = "F_PROJECT_ID")]
    public string ProjectId { get; set; } = "";

    /// <summary>
    /// 会话 ID(支持"开发任务对话冻结/恢复")。
    /// 同一 pipelineId 下可有多个 session(每次恢复生成新 session)。
    /// NULL 表示属于初始会话。
    /// </summary>
    [SugarColumn(ColumnName = "F_SESSION_ID", IsNullable = true)]
    public string? SessionId { get; set; }

    /// <summary>
    /// 该消息是否属于已冻结会话(冻结时刻及之后的消息置 1)。
    /// 恢复后新写入的消息为 0,从而实现"冻结点"边界。
    /// </summary>
    [SugarColumn(ColumnName = "F_IS_FROZEN")]
    public bool IsFrozen { get; set; } = false;

    /// <summary>
    /// 角色
    /// user / assistant / system / tool
    /// </summary>
    [SugarColumn(ColumnName = "F_ROLE")]
    public string Role { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    [SugarColumn(ColumnName = "F_CONTENT")]
    public string Content { get; set; }

    /// <summary>
    /// 所属阶段
    /// requirement / architecture / design / development / delivery
    /// 对齐 PipelineStage 常量
    /// </summary>
    [SugarColumn(ColumnName = "F_STAGE")]
    public string Stage { get; set; }

    /// <summary>
    /// 序号
    /// </summary>
    [SugarColumn(ColumnName = "F_SEQUENCE")]
    public int Sequence { get; set; }

    /// <summary>
    /// 通知对象JSON。格式：["admin","expert"] 或 [{"role":"admin","userId":"123"}]
    /// </summary>
    [SugarColumn(ColumnName = "F_NOTIFY_TARGETS")]
    public string? NotifyTargets { get; set; }
}
