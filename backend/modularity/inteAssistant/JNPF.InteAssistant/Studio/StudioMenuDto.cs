namespace JNPF.InteAssistant.Studio;

/// <summary>
/// 菜单项 DTO（返回给前端）
/// </summary>
public class StudioMenuDto
{
    public long Id { get; set; }
    public long ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public int Sort { get; set; }
    public string? Comment { get; set; }
    public string DataScope { get; set; } = "NONE";
    public string ExpandPhase { get; set; } = "A";

    /// <summary>红点数量</summary>
    public int BadgeCount { get; set; }

    /// <summary>子菜单列表</summary>
    public List<StudioMenuDto> Children { get; set; } = new();
}

/// <summary>
/// 标记菜单红点已读请求
/// </summary>
public class MarkBadgeReadInput
{
    public long MenuId { get; set; }
    public long? ProjectId { get; set; }
}
