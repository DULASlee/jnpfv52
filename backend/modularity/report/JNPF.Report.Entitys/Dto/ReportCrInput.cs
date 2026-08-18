namespace JNPF.Report.Entitys.Dto;

/// <summary>
/// 报表创建/更新输入.
/// </summary>
public class ReportCrInput
{
    public string? id { get; set; }
    public string fullName { get; set; }
    public string enCode { get; set; }
    public string? category { get; set; }
    public string? description { get; set; }
    public int? enabledMark { get; set; }
    public long? sortCode { get; set; }
    public string? reportFile { get; set; }
    public string? content { get; set; }
}
