using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.WorkFlow.Entitys.Entity;

/// <summary>
/// 流程评论.
/// </summary>
[SugarTable("FLOW_COMMENT")]
public class FlowCommentEntity : CLDSEntityBase
{
    /// <summary>
    /// 任务id.
    /// </summary>
    [SugarColumn(ColumnName = "F_TASK_ID")]
    public string? TaskId { get; set; }

    /// <summary>
    /// 文本.
    /// </summary>
    [SugarColumn(ColumnName = "F_TEXT")]
    public string? Text { get; set; }

    /// <summary>
    /// 图片.
    /// </summary>
    [SugarColumn(ColumnName = "F_IMAGE")]
    public string? Image { get; set; }

    /// <summary>
    /// 附件.
    /// </summary>
    [SugarColumn(ColumnName = "F_FILE")]
    public string? File { get; set; }
}
