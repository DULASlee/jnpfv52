using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// IR 版本快照实体
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-20
/// </summary>
[SugarTable("BASE_IR_VERSION", TableDescription = "IR版本快照")]
public class IrVersionEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 流水线 ID（FK → BASE_AI_PIPELINE）
    /// </summary>
    [SugarColumn(ColumnName = "F_PIPELINE_ID")]
    public string PipelineId { get; set; }

    /// <summary>
    /// 项目 ID(FK → ai_projects.F_Id)。三元组补全,NOT NULL DEFAULT ''
    /// </summary>
    [SugarColumn(ColumnName = "F_PROJECT_ID")]
    public string ProjectId { get; set; } = "";

    /// <summary>
    /// 版本号（递增）
    /// </summary>
    [SugarColumn(ColumnName = "F_VERSION")]
    public int Version { get; set; }

    /// <summary>
    /// 触发来源（用户ID / Agent名称 / 系统自动）
    /// </summary>
    [SugarColumn(ColumnName = "F_TRIGGERED_BY")]
    public string? TriggeredBy { get; set; }

    /// <summary>
    /// 变更摘要
    /// </summary>
    [SugarColumn(ColumnName = "F_CHANGE_SUMMARY")]
    public string? ChangeSummary { get; set; }

    /// <summary>
    /// 父版本号（版本树）
    /// </summary>
    [SugarColumn(ColumnName = "F_PARENT_VERSION_ID")]
    public int? ParentVersion { get; set; }

    /// <summary>
    /// 与上一版本的差异（RFC 6902 JSON Patch）
    /// </summary>
    [SugarColumn(ColumnName = "F_DIFF")]
    public string? Diff { get; set; }

    /// <summary>
    /// IR 完整快照（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "F_IR_SNAPSHOT")]
    public string? IrSnapshot { get; set; }

    /// <summary>
    /// 变更类型：full_rollback / surgical_edit / stage_advance
    /// </summary>
    [SugarColumn(ColumnName = "F_CHANGE_TYPE")]
    public string? ChangeType { get; set; }

    /// <summary>
    /// 关联编辑补丁ID（分治编辑时有值）
    /// </summary>
    [SugarColumn(ColumnName = "F_EDIT_PATCH_ID")]
    public long? EditPatchId { get; set; }

    /// <summary>
    /// 校验链结果JSON。格式：{"cleanSchema":true,"validateIR":true,"vueTsc":{"passed":true,"errors":[]}}
    /// </summary>
    [SugarColumn(ColumnName = "F_VALIDATION_RESULT")]
    public string? ValidationResult { get; set; }

    /// <summary>
    /// 快照时间
    /// </summary>
    [SugarColumn(ColumnName = "F_SNAPSHOT_AT")]
    public DateTime? SnapshotAt { get; set; }
}
