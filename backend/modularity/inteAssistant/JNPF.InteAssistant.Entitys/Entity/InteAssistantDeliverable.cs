using SqlSugar;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// 流水线阶段交付物索引（文件落盘 + DB 登记）
/// </summary>
[SugarTable("inte_assistant_deliverable")]
public class InteAssistantDeliverable
{
    [SugarColumn(IsPrimaryKey = true)]
    public string F_Id { get; set; } = "";

    [SugarColumn(ColumnName = "F_PipelineId")]
    public string PipelineId { get; set; } = "";

    /// <summary>三元组：项目ID（一个 project 可对应多个 pipeline/迭代）。</summary>
    [SugarColumn(ColumnName = "F_ProjectId")]
    public string ProjectId { get; set; } = "";

    /// <summary>阶段编码：S0 / S1 / …</summary>
    [SugarColumn(ColumnName = "F_StageCode")]
    public string StageCode { get; set; } = "";

    [SugarColumn(ColumnName = "F_FileName")]
    public string FileName { get; set; } = "";

    /// <summary>相对 deliverables/ 的路径，如 00-gate-report.json</summary>
    [SugarColumn(ColumnName = "F_RelativePath")]
    public string RelativePath { get; set; } = "";

    [SugarColumn(ColumnName = "F_ContentType")]
    public string ContentType { get; set; } = "application/octet-stream";

    [SugarColumn(ColumnName = "F_FileSize")]
    public long FileSize { get; set; }

    [SugarColumn(ColumnName = "F_CreatorTime")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "F_TenantId")]
    public string TenantId { get; set; } = "";

    [SugarColumn(ColumnName = "F_DeleteMark")]
    public bool DeleteMark { get; set; }
}
