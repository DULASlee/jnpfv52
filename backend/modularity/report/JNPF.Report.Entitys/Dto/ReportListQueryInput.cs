namespace JNPF.Report.Entitys.Dto;

/// <summary>
/// 报表列表查询输入.
/// </summary>
public class ReportListQueryInput
{
    public string? keyword { get; set; }
    public string? category { get; set; }
    public int? enabledMark { get; set; }
    public int currentPage { get; set; } = 1;
    public int pageSize { get; set; } = 20;
}
