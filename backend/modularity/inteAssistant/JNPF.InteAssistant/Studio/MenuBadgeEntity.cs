using SqlSugar;

namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 菜单红点提示实体 — 映射 BASE_MENU_BADGE 表 (Sprint 1)
/// </summary>
[SugarTable("BASE_MENU_BADGE")]
public class MenuBadgeEntity
{
    [SugarColumn(IsPrimaryKey = true, ColumnName = "F_Id")]
    public long F_Id { get; set; }

    [SugarColumn(ColumnName = "F_MenuId")]
    public long F_MenuId { get; set; }

    [SugarColumn(ColumnName = "F_UserId")]
    public long F_UserId { get; set; }

    [SugarColumn(ColumnName = "F_TenantId")]
    public string F_TenantId { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "F_Count")]
    public int F_Count { get; set; }

    [SugarColumn(ColumnName = "F_ExtraData")]
    public string? F_ExtraData { get; set; }

    [SugarColumn(ColumnName = "F_CreatorTime")]
    public DateTime F_CreatorTime { get; set; }

    [SugarColumn(ColumnName = "F_ModifyTime")]
    public DateTime? F_ModifyTime { get; set; }
}
