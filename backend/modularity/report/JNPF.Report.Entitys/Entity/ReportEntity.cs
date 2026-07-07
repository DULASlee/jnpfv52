using JNPF.Common.Contracts;
using SqlSugar;

namespace JNPF.Report.Entitys;

/// <summary>
/// 报表元数据实体.
/// </summary>
[SugarTable("BASE_REPORT")]
public class ReportEntity : CLDSEntityBase
{
    /// <summary>
    /// 报表名称.
    /// </summary>
    [SugarColumn(ColumnName = "F_FULL_NAME")]
    public string FullName { get; set; }

    /// <summary>
    /// 报表编码.
    /// </summary>
    [SugarColumn(ColumnName = "F_EN_CODE")]
    public string EnCode { get; set; }

    /// <summary>
    /// 分类ID (字典 ReportSort).
    /// </summary>
    [SugarColumn(ColumnName = "F_CATEGORY")]
    public string Category { get; set; }

    /// <summary>
    /// 说明.
    /// </summary>
    [SugarColumn(ColumnName = "F_DESCRIPTION")]
    public string? Description { get; set; }

    /// <summary>
    /// 启用标记 (1-启用, 0-禁用).
    /// </summary>
    [SugarColumn(ColumnName = "F_ENABLED_MARK")]
    public int EnabledMark { get; set; } = 1;

    /// <summary>
    /// 报表文件名 (.ureport.xml), 对应 UReport2 FileReportProvider 存储.
    /// </summary>
    [SugarColumn(ColumnName = "F_REPORT_FILE")]
    public string? ReportFile { get; set; }

    /// <summary>
    /// 报表 XML 内容 (冗余存储, 用于快速读取).
    /// </summary>
    [SugarColumn(ColumnName = "F_CONTENT")]
    public string? Content { get; set; }
}
