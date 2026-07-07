namespace JNPF.Report.Entitys.Dto;

/// <summary>
/// 报表列表输出.
/// </summary>
public class ReportListOutput
{
    public string id { get; set; }
    public string fullName { get; set; }
    public string enCode { get; set; }
    public string? category { get; set; }
    public string? categoryId { get; set; }
    public string? description { get; set; }
    public int enabledMark { get; set; }
    public long? sortCode { get; set; }
    public string? creatorUser { get; set; }
    public DateTime? creatorTime { get; set; }
    public DateTime? lastModifyTime { get; set; }
    public string? reportFile { get; set; }
}
