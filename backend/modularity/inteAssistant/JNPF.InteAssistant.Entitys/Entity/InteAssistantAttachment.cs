using SqlSugar;
using System;

namespace JNPF.InteAssistant.Entitys.Entity;

/// <summary>
/// AI助手-附件表（企业级 v2）
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

    /// <summary>项目 ID(三元组补全,FK → ai_projects.F_Id,NOT NULL DEFAULT '')</summary>
    [SugarColumn(ColumnName = "F_PROJECT_ID")]
    public string ProjectId { get; set; } = "";

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

    /// <summary>文件内容SHA256哈希（去重用）</summary>
    [SugarColumn(ColumnName = "F_FileHash", IsNullable = true)]
    public string? FileHash { get; set; }

    /// <summary>解析后的纯文本（缓存，避免重复解析）</summary>
    [SugarColumn(ColumnName = "F_ExtractedText", ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ExtractedText { get; set; }

    /// <summary>处理状态：0待处理 1处理中 2已完成 3失败</summary>
    [SugarColumn(ColumnName = "F_ProcessStatus")]
    public int ProcessStatus { get; set; }

    /// <summary>处理失败原因（含堆栈）</summary>
    [SugarColumn(ColumnName = "F_ProcessError", ColumnDataType = "nvarchar(2000)", IsNullable = true)]
    public string? ProcessError { get; set; }

    /// <summary>上传人ID</summary>
    [SugarColumn(ColumnName = "F_CreatorUserId", IsNullable = true)]
    public string? CreatorUserId { get; set; }

    /// <summary>上传人姓名（冗余，查询用）</summary>
    [SugarColumn(ColumnName = "F_CreatorUserName", IsNullable = true)]
    public string? CreatorUserName { get; set; }

    [SugarColumn(ColumnName = "F_CreatorTime")]
    public DateTime CreateTime { get; set; }

    /// <summary>最后修改人ID</summary>
    [SugarColumn(ColumnName = "F_LastModifyUserId", IsNullable = true)]
    public string? LastModifyUserId { get; set; }

    /// <summary>最后修改时间</summary>
    [SugarColumn(ColumnName = "F_LastModifyTime", IsNullable = true)]
    public DateTime? LastModifyTime { get; set; }

    /// <summary>软删除标记：0正常 1已删除</summary>
    [SugarColumn(ColumnName = "F_DeleteMark")]
    public bool DeleteMark { get; set; }

    /// <summary>租户 ID(fail-closed 安全铁律,NOT NULL)</summary>
    [SugarColumn(ColumnName = "F_TenantId")]
    public string TenantId { get; set; } = "";
}
