using JNPF.Common.Contracts;
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
    /// draft / generating / validating / compiling / done
    /// </summary>
    [SugarColumn(ColumnName = "F_CURRENT_STAGE")]
    public string CurrentStage { get; set; }

    /// <summary>
    /// 运行状态
    /// running / completed / failed
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public string Status { get; set; }

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
}
