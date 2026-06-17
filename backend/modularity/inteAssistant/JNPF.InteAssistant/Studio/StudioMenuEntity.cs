using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// Studio 菜单实体 — 映射 BASE_STUDIO_MENU 表 (Sprint 1)
/// </summary>
[SugarTable("BASE_STUDIO_MENU")]
public class StudioMenuEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")]
    public long F_Id { get; set; }

    [SugarColumn(ColumnName = "F_ParentId")]
    public long F_ParentId { get; set; }

    [SugarColumn(ColumnName = "F_Name")]
    public string F_Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Icon")]
    public string? F_Icon { get; set; }

    [SugarColumn(ColumnName = "F_Url")]
    public string? F_Url { get; set; }

    [SugarColumn(ColumnName = "F_Sort")]
    public int F_Sort { get; set; }

    [SugarColumn(ColumnName = "F_Enabled")]
    public bool F_Enabled { get; set; } = true;

    [SugarColumn(ColumnName = "F_IsVisible")]
    public bool F_IsVisible { get; set; } = true;

    [SugarColumn(ColumnName = "F_IsPublic")]
    public bool F_IsPublic { get; set; }

    [SugarColumn(ColumnName = "F_Comment")]
    public string? F_Comment { get; set; }

    [SugarColumn(ColumnName = "F_RequiredRoles")]
    public string? F_RequiredRoles { get; set; }

    [SugarColumn(ColumnName = "F_DataScope")]
    public string F_DataScope { get; set; } = "NONE";

    [SugarColumn(ColumnName = "F_ExpandPhase")]
    public string F_ExpandPhase { get; set; } = "A";

    [SugarColumn(ColumnName = "F_TenantViewConfig")]
    public string? F_TenantViewConfig { get; set; }

    [SugarColumn(ColumnName = "F_CreatorTime")]
    public DateTime F_CreatorTime { get; set; }

    [SugarColumn(ColumnName = "F_CreatorUserId")]
    public long? F_CreatorUserId { get; set; }

    [SugarColumn(ColumnName = "F_ModifyTime")]
    public DateTime? F_ModifyTime { get; set; }

    [SugarColumn(ColumnName = "F_ModifyUserId")]
    public long? F_ModifyUserId { get; set; }

    [SugarColumn(ColumnName = "F_DeleteMark")]
    public bool F_DeleteMark { get; set; }
}
