using SqlSugar;
using System;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// AI助手-附件表
/// 存储Pipeline关联的附件信息和解析后的文本缓存
/// </summary>
[SugarTable("inte_assistant_attachment")]
public class InteAssistantAttachment
{
    [SugarColumn(IsPrimaryKey = true)]
    public string F_Id { get; set; } = "";

    /// <summary>关联的Pipeline ID</summary>
    [SugarColumn(ColumnName = "F_PipelineId")]
    public string PipelineId { get; set; } = "";

    /// <summary>文件名</summary>
    [SugarColumn(ColumnName = "F_FileName")]
    public string FileName { get; set; } = "";

    /// <summary>文件存储地址</summary>
    [SugarColumn(ColumnName = "F_FileUrl")]
    public string FileUrl { get; set; } = "";

    /// <summary>文件大小（字节）</summary>
    [SugarColumn(ColumnName = "F_FileSize")]
    public long FileSize { get; set; }

    /// <summary>文件类型（docx/xlsx/pdf/png）</summary>
    [SugarColumn(ColumnName = "F_FileType")]
    public string FileType { get; set; } = "";

    /// <summary>解析后的纯文本（缓存，避免重复解析）</summary>
    [SugarColumn(ColumnName = "F_ExtractedText", ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ExtractedText { get; set; }

    /// <summary>处理状态：0待处理 1处理中 2已完成 3失败</summary>
    [SugarColumn(ColumnName = "F_ProcessStatus")]
    public int ProcessStatus { get; set; }

    /// <summary>处理失败原因</summary>
    [SugarColumn(ColumnName = "F_ProcessError", IsNullable = true)]
    public string? ProcessError { get; set; }

    [SugarColumn(ColumnName = "F_CreatorTime")]
    public DateTime CreateTime { get; set; }

    [SugarColumn(ColumnName = "F_TenantId", IsNullable = true)]
    public string? TenantId { get; set; }
}
