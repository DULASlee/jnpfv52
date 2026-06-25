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
