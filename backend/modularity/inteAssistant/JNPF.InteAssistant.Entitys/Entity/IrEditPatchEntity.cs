using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// 分治编辑补丁（ARC-3-006 / B-03）
/// 版 本：v5.2.0
/// 版 权：引迈信息技术有限公司（https://www.jnpfsoft.com）
/// 作 者：JNPF开发平台组
/// 日 期：2026-6-20
/// </summary>
[SugarTable("BASE_IR_EDIT_PATCH", TableDescription = "分治编辑补丁")]
public class IrEditPatchEntity : TenantCLDSEntityBase
{
    /// <summary>
    /// 关联流水线ID
    /// </summary>
    [SugarColumn(ColumnName = "F_PIPELINE_ID")]
    public long PipelineId { get; set; }

    /// <summary>
    /// 关联IR版本ID
    /// </summary>
    [SugarColumn(ColumnName = "F_VERSION_ID")]
    public long? VersionId { get; set; }

    /// <summary>
    /// 目标节点ID列表（JSON数组）
    /// </summary>
    [SugarColumn(ColumnName = "F_TARGET_NODE_IDS")]
    public string TargetNodeIds { get; set; }

    /// <summary>
    /// 编辑操作列表（RFC 6902 JSON Patch）
    /// </summary>
    [SugarColumn(ColumnName = "F_OPERATIONS")]
    public string Operations { get; set; }

    /// <summary>
    /// 修改说明
    /// </summary>
    [SugarColumn(ColumnName = "F_EXPLANATION")]
    public string? Explanation { get; set; }

    /// <summary>
    /// 状态：pending / applied / failed / rolled_back
    /// </summary>
    [SugarColumn(ColumnName = "F_STATUS")]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// 成功应用的操作数
    /// </summary>
    [SugarColumn(ColumnName = "F_APPLIED_COUNT")]
    public int AppliedCount { get; set; }

    /// <summary>
    /// 失败的操作数
    /// </summary>
    [SugarColumn(ColumnName = "F_FAILED_COUNT")]
    public int FailedCount { get; set; }

    /// <summary>
    /// 变更类型
    /// </summary>
    [SugarColumn(ColumnName = "F_CHANGE_TYPE")]
    public string ChangeType { get; set; } = "surgical_edit";
}
