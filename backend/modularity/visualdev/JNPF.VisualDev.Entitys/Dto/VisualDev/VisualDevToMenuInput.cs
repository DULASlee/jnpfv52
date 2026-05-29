namespace JNPF.VisualDev.Entitys.Dto.VisualDev;

/// <summary>
/// 可视化开发同步到菜单输入.
/// </summary>
public class VisualDevToMenuInput
{
    /// <summary>
    /// 同步App菜单 1 同步.
    /// </summary>
    public int? app { get; set; }

    /// <summary>
    /// 同步PC菜单 1 同步.
    /// </summary>
    public int? pc { get; set; }

    /// <summary>
    /// Pc端同步菜单父级ID.
    /// </summary>
    public List<string> pcModuleParentId { get; set; }

    /// <summary>
    /// App端同步菜单父级ID.
    /// </summary>
    public List<string> appModuleParentId { get; set; }

    /// <summary>
    /// 发布选中平台.
    /// </summary>
    public string platformRelease { get; set; }
}